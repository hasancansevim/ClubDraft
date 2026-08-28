import React, { useState, useEffect, useCallback } from 'react';
import { useParams, Link } from 'react-router-dom';
import { sessionApi } from '../api/sessionApi';
import { seasonApi } from '../api/seasonApi';
import { sponsorshipApi, OfferStatus, type SponsorshipOffer } from '../api/sponsorshipApi';
import { toast } from '../App';

const STATUS_LABEL: Record<number, string> = {
  [OfferStatus.Pending]: 'Bekliyor',
  [OfferStatus.Accepted]: 'Kabul Edildi',
  [OfferStatus.Rejected]: 'Reddedildi',
  [OfferStatus.Expired]: 'Süresi Doldu',
};

const STATUS_COLOR: Record<number, string> = {
  [OfferStatus.Pending]: 'var(--accent)',
  [OfferStatus.Accepted]: '#39FF88',
  [OfferStatus.Rejected]: '#ff4a4a',
  [OfferStatus.Expired]: 'var(--text-secondary)',
};

const formatDate = (iso: string) => {
  try {
    return new Date(iso).toLocaleDateString('tr-TR', { day: '2-digit', month: 'long', year: 'numeric' });
  } catch {
    return iso;
  }
};

export const SponsorshipDashboard = () => {
  // URL param sadece kisa kod (orn. TIGER42) tasir. Lobi/Draft/Sezon Dashboard'daki
  // gibi, tum API cagrilarindan once gercek RoomId'ye cozulmesi sart — aksi halde
  // {id:guid} route constraint'i tasiyan endpoint'ler 404 doner (bu hata bu projede
  // iki kez tekrarlandi, bkz. spec.md).
  const { roomId: shortCode } = useParams();
  const [realRoomId, setRealRoomId] = useState<string | null>(null);
  const [clubId, setClubId] = useState<string | null>(null);
  const [budget, setBudget] = useState<number | null>(null);

  const [offers, setOffers] = useState<SponsorshipOffer[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [respondingId, setRespondingId] = useState<string | null>(null);

  const fetchOffers = useCallback(async (cId: string) => {
    try {
      const data = await sponsorshipApi.getOffers(cId);
      setOffers(data);
    } catch {
      // sessiz gec — polling bir sonraki turda tekrar dener
    }
  }, []);

  const fetchBudget = useCallback(async (cId: string) => {
    try {
      const club = await seasonApi.getClub(cId);
      setBudget(club.budget);
    } catch {
      // sessiz gec — polling bir sonraki turda tekrar dener
    }
  }, []);

  const fetchInitial = useCallback(async () => {
    try {
      if (!shortCode) return;

      const myParticipantId = localStorage.getItem(`joined_${shortCode}`);
      if (!myParticipantId) {
        setError('Odaya katılım bilginiz bulunamadı.');
        setLoading(false);
        return;
      }

      const room = await sessionApi.getRoomByCode(shortCode).catch(() => null) || await sessionApi.getRoom(shortCode);
      if (!room || !room.id) {
        setError('Oda bulunamadı.');
        setLoading(false);
        return;
      }
      setRealRoomId(room.id);

      const fullRoom = await sessionApi.getRoom(room.id);
      const me = fullRoom.participants?.find(p => p.id === myParticipantId);
      if (!me || !me.clubId) {
        setError('Kulüp ID bulunamadı. Draft tamamlanmamış olabilir.');
        setLoading(false);
        return;
      }
      setClubId(me.clubId);

      const [club] = await Promise.all([
        seasonApi.getClub(me.clubId),
        fetchOffers(me.clubId),
      ]);
      setBudget(club.budget);
    } catch (err) {
      console.error(err);
      setError('Sponsorluk teklifleri yüklenirken hata oluştu.');
    } finally {
      setLoading(false);
    }
  }, [shortCode, fetchOffers]);

  useEffect(() => {
    fetchInitial();
  }, [fetchInitial]);

  // RealtimeHub'da su an bir "SponsorshipOffered" event'i / consumer'i tanimli
  // degil (ReputationThresholdReachedEventConsumer teklifi DB'ye yaziyor ama
  // yeni bir integration event yayinlamiyor) — bu yuzden SignalR yerine kisa
  // araliklarla polling ile yeni tekliflerin/son kullanma tarihi gecmislerin
  // yakalanmasi saglaniyor. Butce de ayni pollinge dahil edildi: kabul sonrasi
  // gercek kredi RabbitMQ/Outbox uzerinden asenkron islendigi icin (bkz.
  // ISponsorshipAcceptedEvent -> ClubManagement), tek seferlik hemen-sonrasi
  // fetch bazen krediden once yetisip eski butceyi gosterebiliyordu — polling
  // birkac saniye icinde kendini duzeltiyor.
  useEffect(() => {
    if (!clubId) return;
    const interval = setInterval(() => {
      fetchOffers(clubId);
      fetchBudget(clubId);
    }, 4000);
    return () => clearInterval(interval);
  }, [clubId, fetchOffers, fetchBudget]);

  const handleRespond = async (offer: SponsorshipOffer, response: 'Accept' | 'Reject') => {
    if (!clubId || respondingId) return;
    setRespondingId(offer.id);
    try {
      await sponsorshipApi.respond(clubId, offer.id, response);
      toast('success', response === 'Accept' ? 'Sponsorluk teklifi kabul edildi!' : 'Teklif reddedildi.');
      await fetchOffers(clubId);
      if (response === 'Accept') {
        // Budget kredisi asenkron (event-driven) islendigi icin hemen taze
        // olmayabilir — birkac kez kisa aralikla tekrar dene.
        await fetchBudget(clubId);
        setTimeout(() => fetchBudget(clubId), 1500);
        setTimeout(() => fetchBudget(clubId), 3500);
      }
    } catch (err: any) {
      const reason = err.response?.data || err.message;
      toast('error', `İşlem başarısız: ${reason}`);
    } finally {
      setRespondingId(null);
    }
  };

  if (loading) {
    return (
      <div className="cc-loader-overlay">
        <div className="cc-loader">
          <div className="cc-loader-ring-outer" />
          <div className="cc-loader-ring-inner" />
        </div>
        <span className="cc-loader-text">Sponsorluk Teklifleri Yükleniyor...</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className="cc-error-state">
        <div style={{ fontSize: '3rem' }}>⚠</div>
        <p>{error}</p>
        <Link to="/" className="cc-btn">Ana Sayfaya Dön</Link>
      </div>
    );
  }

  const pending = offers.filter(o => o.status === OfferStatus.Pending);
  const history = offers.filter(o => o.status !== OfferStatus.Pending)
    .sort((a, b) => new Date(b.offeredAt).getTime() - new Date(a.offeredAt).getTime());

  return (
    <div style={{ padding: '1.5rem', maxWidth: '900px', margin: '0 auto' }}>
      <div className="cc-card" style={{ padding: '1.25rem', marginBottom: '1.5rem', display: 'flex', alignItems: 'center', gap: '1rem' }}>
        <div style={{ fontSize: '2.5rem' }}>💰</div>
        <div>
          <div style={{ color: 'var(--text-secondary)', fontSize: '0.85rem', textTransform: 'uppercase', letterSpacing: '1px' }}>Güncel Bütçe</div>
          <div style={{ fontSize: '1.5rem', fontFamily: 'Orbitron, sans-serif', color: 'var(--accent)' }}>
            €{(budget ?? 0).toLocaleString()}
          </div>
        </div>
      </div>

      <div className="cc-card" style={{ padding: '1.5rem', marginBottom: '1.5rem' }}>
        <h3 style={{ fontSize: '1.2rem', marginBottom: '1.25rem', borderBottom: '1px solid var(--border)', paddingBottom: '0.75rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <span>🤝</span> Bekleyen Teklifler {pending.length > 0 && `(${pending.length})`}
        </h3>

        {pending.length === 0 ? (
          <p style={{ color: 'var(--text-secondary)', textAlign: 'center', padding: '1.5rem 0' }}>
            Şu anda bekleyen bir sponsorluk teklifi yok. İtibarınız yeterli bir eşiği aştığında burada görünecek.
          </p>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
            {pending.map(offer => (
              <div key={offer.id} style={{ border: '1px solid var(--border)', borderRadius: '8px', padding: '1.25rem', background: 'var(--bg-secondary)' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '1rem', marginBottom: '1rem' }}>
                  <div>
                    <div style={{ fontSize: '1.4rem', fontFamily: 'Orbitron, sans-serif', color: 'var(--accent)' }}>
                      €{offer.amount.toLocaleString()}
                    </div>
                    <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginTop: '0.35rem' }}>
                      İtibar eşiği: <strong>{offer.thresholdReached}</strong> · Son geçerlilik: {formatDate(offer.expiresAt)}
                    </div>
                  </div>
                </div>
                <div style={{ display: 'flex', gap: '0.75rem' }}>
                  <button
                    className="cc-btn"
                    style={{ flex: 1 }}
                    disabled={respondingId === offer.id}
                    onClick={() => handleRespond(offer, 'Accept')}
                  >
                    {respondingId === offer.id ? '...' : 'Kabul Et'}
                  </button>
                  <button
                    className="cc-btn"
                    style={{ flex: 1, background: 'transparent', border: '1px solid var(--border)', color: 'var(--text-secondary)' }}
                    disabled={respondingId === offer.id}
                    onClick={() => handleRespond(offer, 'Reject')}
                  >
                    {respondingId === offer.id ? '...' : 'Reddet'}
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      <div className="cc-card" style={{ padding: '1.5rem' }}>
        <h3 style={{ fontSize: '1.2rem', marginBottom: '1.25rem', borderBottom: '1px solid var(--border)', paddingBottom: '0.75rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <span>📜</span> Geçmiş Kararlar
        </h3>

        {history.length === 0 ? (
          <p style={{ color: 'var(--text-secondary)', textAlign: 'center', padding: '1rem 0' }}>Henüz karar verilmiş bir teklif yok.</p>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.6rem' }}>
            {history.map(offer => (
              <div key={offer.id} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '0.75rem 1rem', borderRadius: '6px', background: 'var(--bg-secondary)', fontSize: '0.85rem' }}>
                <span>€{offer.amount.toLocaleString()} <span style={{ color: 'var(--text-secondary)' }}>(eşik {offer.thresholdReached})</span></span>
                <span style={{ color: STATUS_COLOR[offer.status], fontWeight: 600 }}>{STATUS_LABEL[offer.status]}</span>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};
