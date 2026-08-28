import React, { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { sessionApi } from '../api/sessionApi';
import { seasonApi, type TeamStanding } from '../api/seasonApi';

const TOTAL_WEEKS = 14;

interface ClubScoreRow {
  clubId: string;
  clubName: string;
  played: number;
  points: number;
  reputation: number;
  budget: number;
  rosterSize: number;
  avgOverall: number;
  presidencyScore: number;
}

// Başkanlık Skoru = (Lig Puanı × 10) + İtibar Skoru + (Bütçe ÷ 50.000)
const calcPresidencyScore = (points: number, reputation: number, budget: number) =>
  points * 10 + reputation + budget / 50000;

export const SummaryDashboard = () => {
  const { roomId: shortCode } = useParams();
  const [realRoomId, setRealRoomId] = useState<string | null>(null);
  const [clubId, setClubId] = useState<string | null>(null);
  const [currentWeek, setCurrentWeek] = useState<number>(1);

  const [rows, setRows] = useState<ClubScoreRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const load = async () => {
      try {
        if (!shortCode) return;

        const myParticipantId = localStorage.getItem(`joined_${shortCode}`);
        if (!myParticipantId) {
          setError('Odaya katılım bilginiz bulunamadı.');
          setLoading(false);
          return;
        }

        // Kısa kod → gerçek RoomId çözümlemesi — bu proje boyunca üç kez atlanıp
        // düzeltilen bir hata oldu, bu sayfada da ilk adım olarak yapılıyor.
        const room = await sessionApi.getRoomByCode(shortCode).catch(() => null) || await sessionApi.getRoom(shortCode);
        if (!room || !room.id) {
          setError('Oda bulunamadı.');
          setLoading(false);
          return;
        }
        setRealRoomId(room.id);
        setCurrentWeek(room.currentWeek || 1);

        const fullRoom = await sessionApi.getRoom(room.id);
        const me = fullRoom.participants?.find(p => p.id === myParticipantId);
        if (me?.clubId) setClubId(me.clubId);

        const standings: TeamStanding[] = await seasonApi.getStandings(room.id).catch(() => []);

        // Her kulüp için itibar + bütçe + roster ayrı endpoint'ler — küçük lig
        // boyutunda (4-6 takım) bunları paralel çekmek kabul edilebilir bir maliyet.
        const detailRows = await Promise.all(standings.map(async (s) => {
          const participant = fullRoom.participants?.find(p => p.clubId === s.clubId);
          const [club, reputation] = await Promise.all([
            seasonApi.getClub(s.clubId).catch(() => null),
            seasonApi.getReputation(s.clubId).catch(() => 0),
          ]);
          const roster = club?.roster || [];
          const avgOverall = roster.length > 0
            ? roster.reduce((sum, p) => sum + p.overall, 0) / roster.length
            : 0;
          const budget = club?.budget ?? 0;

          const row: ClubScoreRow = {
            clubId: s.clubId,
            clubName: participant?.clubName || 'Bilinmeyen Kulüp',
            played: s.played,
            points: s.points,
            reputation,
            budget,
            rosterSize: roster.length,
            avgOverall,
            presidencyScore: calcPresidencyScore(s.points, reputation, budget),
          };
          return row;
        }));

        detailRows.sort((a, b) => b.presidencyScore - a.presidencyScore);
        setRows(detailRows);
      } catch (err) {
        console.error(err);
        setError('Özet verileri yüklenirken hata oluştu.');
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [shortCode]);

  if (loading) {
    return (
      <div className="cc-loader-overlay">
        <div className="cc-loader">
          <div className="cc-loader-ring-outer" />
          <div className="cc-loader-ring-inner" />
        </div>
        <span className="cc-loader-text">Özet Hazırlanıyor...</span>
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

  const seasonOver = currentWeek >= TOTAL_WEEKS;
  const champion = rows[0];
  const me = rows.find(r => r.clubId === clubId);

  return (
    <div style={{ padding: '1.5rem', maxWidth: '1000px', margin: '0 auto' }}>
      {seasonOver && champion && (
        <div className="cc-card" style={{
          padding: '2rem', marginBottom: '1.5rem', textAlign: 'center',
          background: 'linear-gradient(135deg, rgba(255,215,0,0.12) 0%, rgba(10,14,23,1) 100%)',
          border: '1px solid #FFD700'
        }}>
          <div style={{ fontSize: '3rem', marginBottom: '0.5rem' }}>🏆</div>
          <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', textTransform: 'uppercase', letterSpacing: '2px' }}>Şampiyon</div>
          <div style={{ fontSize: '2rem', fontFamily: 'Orbitron, sans-serif', color: '#FFD700', marginTop: '0.25rem' }}>{champion.clubName}</div>
          <div style={{ fontSize: '0.9rem', color: 'var(--text-secondary)', marginTop: '0.5rem' }}>
            Başkanlık Skoru: <strong style={{ color: '#FFD700' }}>{champion.presidencyScore.toFixed(1)}</strong>
          </div>
        </div>
      )}

      {!seasonOver && (
        <div style={{ textAlign: 'center', marginBottom: '1.5rem', color: 'var(--text-secondary)', fontSize: '0.9rem' }}>
          Sezon devam ediyor ({currentWeek} / {TOTAL_WEEKS} hafta) — aşağıda şu ana kadarki durum gösteriliyor.
        </div>
      )}

      {me && (
        <div className="cc-card" style={{ padding: '1.5rem', marginBottom: '1.5rem' }}>
          <h3 style={{ fontSize: '1.1rem', marginBottom: '1.25rem', borderBottom: '1px solid var(--border)', paddingBottom: '0.75rem' }}>
            {me.clubName} — Başkanlık Skoru
          </h3>
          <div style={{ display: 'flex', gap: '1rem', marginBottom: '1.25rem', flexWrap: 'wrap' }}>
            <div style={{ flex: '1 1 140px', textAlign: 'center', padding: '1rem', background: 'var(--bg-secondary)', borderRadius: '8px' }}>
              <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', textTransform: 'uppercase' }}>Lig Puanı × 10</div>
              <div style={{ fontSize: '1.4rem', fontFamily: 'Orbitron, sans-serif' }}>{me.points * 10}</div>
            </div>
            <div style={{ flex: '1 1 140px', textAlign: 'center', padding: '1rem', background: 'var(--bg-secondary)', borderRadius: '8px' }}>
              <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', textTransform: 'uppercase' }}>İtibar Skoru</div>
              <div style={{ fontSize: '1.4rem', fontFamily: 'Orbitron, sans-serif' }}>{me.reputation}</div>
            </div>
            <div style={{ flex: '1 1 140px', textAlign: 'center', padding: '1rem', background: 'var(--bg-secondary)', borderRadius: '8px' }}>
              <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', textTransform: 'uppercase' }}>Bütçe ÷ 50.000</div>
              <div style={{ fontSize: '1.4rem', fontFamily: 'Orbitron, sans-serif' }}>{(me.budget / 50000).toFixed(1)}</div>
            </div>
            <div style={{ flex: '1 1 140px', textAlign: 'center', padding: '1rem', background: 'rgba(57,255,136,0.08)', border: '1px solid var(--accent)', borderRadius: '8px' }}>
              <div style={{ fontSize: '0.75rem', color: 'var(--accent)', textTransform: 'uppercase' }}>Toplam</div>
              <div style={{ fontSize: '1.8rem', fontFamily: 'Orbitron, sans-serif', color: 'var(--accent)', fontWeight: 900 }}>{me.presidencyScore.toFixed(1)}</div>
            </div>
          </div>
          <div style={{ display: 'flex', gap: '2rem', fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
            <span>Kadro: <strong style={{ color: 'var(--text-primary)' }}>{me.rosterSize} oyuncu</strong></span>
            <span>Ortalama Overall: <strong style={{ color: 'var(--text-primary)' }}>{me.avgOverall.toFixed(1)}</strong></span>
          </div>
        </div>
      )}

      <div className="cc-card" style={{ padding: '1.5rem' }}>
        <h3 style={{ fontSize: '1.2rem', marginBottom: '1rem', borderBottom: '1px solid var(--border)', paddingBottom: '0.75rem' }}>
          Lig Sıralaması — Başkanlık Skoru
        </h3>
        <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.85rem' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid var(--border)', color: 'var(--text-secondary)', textAlign: 'left' }}>
                <th style={{ padding: '0.5rem' }}>Sıra</th>
                <th style={{ padding: '0.5rem' }}>Takım</th>
                <th style={{ padding: '0.5rem' }}>O</th>
                <th style={{ padding: '0.5rem' }}>Lig P.</th>
                <th style={{ padding: '0.5rem' }}>İtibar</th>
                <th style={{ padding: '0.5rem' }}>Bütçe</th>
                <th style={{ padding: '0.5rem' }}>Başkanlık Skoru</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r, idx) => (
                <tr key={r.clubId} style={{
                  borderBottom: '1px solid var(--bg-primary)',
                  background: r.clubId === clubId ? 'rgba(57,255,136,0.05)' : 'transparent'
                }}>
                  <td style={{ padding: '0.5rem' }}>{idx === 0 && seasonOver ? '🏆' : idx + 1}</td>
                  <td style={{ padding: '0.5rem', fontWeight: 600 }}>{r.clubName}</td>
                  <td style={{ padding: '0.5rem' }}>{r.played}</td>
                  <td style={{ padding: '0.5rem' }}>{r.points}</td>
                  <td style={{ padding: '0.5rem' }}>{r.reputation}</td>
                  <td style={{ padding: '0.5rem' }}>€{r.budget.toLocaleString()}</td>
                  <td style={{ padding: '0.5rem', fontWeight: 'bold', color: 'var(--accent)' }}>{r.presidencyScore.toFixed(1)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
