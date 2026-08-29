import React, { useState, useEffect, useRef, useCallback } from 'react';
import { BrowserRouter, Routes, Route, Link, useParams, useLocation, useNavigate } from 'react-router-dom';
import './index.css';
import { sessionApi } from './api/sessionApi';
import type { Participant } from './api/sessionApi';
import { useSignalR } from './hooks/useSignalR';
import { draftApi, type Player, type DraftState } from './api/draftApi';
import { seasonApi } from './api/seasonApi';
import { FORMATIONS, FORMATION_NAMES, POSITION_GROUP } from './constants/formations';
import { SeasonDashboard } from './pages/SeasonDashboard';
import { SponsorshipDashboard } from './pages/SponsorshipDashboard';
import { SummaryDashboard } from './pages/SummaryDashboard';

// ─── TOAST SYSTEM ────────────────────────────────────────────────────────────
type ToastType = 'success' | 'error' | 'warning' | 'info';
interface Toast { id: number; type: ToastType; msg: string; }

const toastIcons: Record<ToastType, string> = {
  success: '✓', error: '✕', warning: '⚠', info: 'ℹ',
};

let toastIdCounter = 0;
let globalAddToast: ((type: ToastType, msg: string) => void) | null = null;

export const toast = (type: ToastType, msg: string) => { globalAddToast?.(type, msg); };

const ToastContainer = () => {
  const [toasts, setToasts] = useState<Toast[]>([]);
  const removingRef = useRef<Set<number>>(new Set());

  useEffect(() => {
    globalAddToast = (type, msg) => {
      const id = ++toastIdCounter;
      setToasts(prev => [...prev, { id, type, msg }]);
      setTimeout(() => dismiss(id), 3500);
    };
    return () => { globalAddToast = null; };
  }, []);

  const dismiss = (id: number) => {
    if (removingRef.current.has(id)) return;
    removingRef.current.add(id);
    // Give animation time to play
    setTimeout(() => {
      setToasts(prev => prev.filter(t => t.id !== id));
      removingRef.current.delete(id);
    }, 280);
  };

  return (
    <div className="cc-toast-container">
      {toasts.map(t => (
        <div
          key={t.id}
          className={`cc-toast cc-toast-${t.type}`}
          onClick={() => dismiss(t.id)}
        >
          <span className="cc-toast-icon">{toastIcons[t.type]}</span>
          <span className="cc-toast-msg">{t.msg}</span>
        </div>
      ))}
    </div>
  );
};

// ─── LOADER ──────────────────────────────────────────────────────────────────
const Loader = ({ text = 'Yükleniyor...' }: { text?: string }) => (
  <div className="cc-loader-overlay">
    <div className="cc-loader">
      <div className="cc-loader-ring-outer" />
      <div className="cc-loader-ring-inner" />
      <div className="cc-loader-center">CC</div>
    </div>
    <span className="cc-loader-text">{text}</span>
  </div>
);

// ─── POSITION BADGE ───────────────────────────────────────────────────────────
// POSITION_GROUP artik constants/formations.ts'te tek yerde tutuluyor (Sezon
// Dashboard'la paylasiliyor) — bkz. import.
const PosBadge = ({ pos }: { pos: string }) => (
  <span className={`cc-pos-badge ${POSITION_GROUP[pos] || pos}`}>{pos}</span>
);

// ─── OVERALL COLOR ────────────────────────────────────────────────────────────
const overallColor = (ov: number) => {
  if (ov >= 85) return '#FFD700';
  if (ov >= 80) return 'var(--pos-mid)';
  if (ov >= 75) return 'var(--info)';
  return 'var(--text-secondary)';
};

// ─── HOME PAGE ────────────────────────────────────────────────────────────────
const Home = () => {
  const [roomCode, setRoomCode] = useState('');
  const [isCreating, setIsCreating] = useState(false);
  const [isJoining, setIsJoining] = useState(false);
  const navigate = useNavigate();

  const handleCreateRoom = async () => {
    try {
      setIsCreating(true);
      const hostUserId = crypto.randomUUID();
      const response = await sessionApi.createRoom(hostUserId, 6);
      navigate(`/lobby/${response.shortCode || response.roomId}`);
    } catch {
      toast('error', 'Oda oluşturulurken bir hata oluştu.');
    } finally {
      setIsCreating(false);
    }
  };

  const handleJoin = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!roomCode.trim()) return;
    try {
      setIsJoining(true);
      const code = roomCode.trim().toUpperCase();
      const response = await sessionApi.getRoomByCode(code);
      if (response?.id) {
        navigate(`/lobby/${response.shortCode}`);
      } else {
        toast('error', 'Oda bulunamadı.');
      }
    } catch {
      toast('error', 'Oda bulunamadı veya bağlantı hatası.');
    } finally {
      setIsJoining(false);
    }
  };

  return (
    <div className="cc-home">
      {/* Pitch background decoration */}
      <div className="cc-home-pitch-lines" />

      <div className="cc-home-logo">
        CLUB<span className="accent">CRAFT</span>
      </div>
      <p className="cc-home-tagline">Draft · Manage · Dominate</p>

      <div className="cc-home-cards">
        {/* Create card */}
        <div className="cc-home-card">
          <div className="cc-home-card-icon">🏆</div>
          <h2>Yeni Sezon Başlat</h2>
          <p>Arkadaşlarını davet etmek için yeni bir lig oluştur. Oyunun host'u sen ol.</p>
          <button
            className="cc-btn"
            style={{ width: '100%', marginTop: 'auto' }}
            onClick={handleCreateRoom}
            disabled={isCreating}
            id="btn-create-room"
          >
            {isCreating ? <><span className="cc-spinner" />Oluşturuluyor...</> : 'Oda Kur'}
          </button>
        </div>

        {/* Join card */}
        <div className="cc-home-card">
          <div className="cc-home-card-icon">⚽</div>
          <h2>Odaya Katıl</h2>
          <p>Arkadaşından aldığın 6 haneli kısa kodu girerek lige dahil ol.</p>
          <form onSubmit={handleJoin} style={{ width: '100%', display: 'flex', flexDirection: 'column', gap: '0.75rem', marginTop: 'auto' }}>
            <input
              type="text"
              className="cc-input"
              placeholder="Örn: TIGER42"
              value={roomCode}
              onChange={e => setRoomCode(e.target.value)}
              style={{ textAlign: 'center', textTransform: 'uppercase', letterSpacing: '4px', fontFamily: 'var(--font-display)', fontWeight: '700' }}
              maxLength={6}
              id="input-room-code"
            />
            <button
              type="submit"
              className="cc-btn cc-btn-ghost"
              style={{ width: '100%' }}
              disabled={isJoining || !roomCode.trim()}
              id="btn-join-room"
            >
              {isJoining ? <><span className="cc-spinner" style={{ borderTopColor: 'var(--accent)' }} />Bağlanıyor...</> : 'Katıl'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
};

// ─── LOBBY ────────────────────────────────────────────────────────────────────
const Lobby = () => {
  const { roomId: shortCode } = useParams();
  const navigate = useNavigate();

  const [realRoomId, setRealRoomId] = useState('');
  const [participants, setParticipants] = useState<Participant[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [myUserId] = useState(() => {
    const existing = localStorage.getItem('myUserId');
    if (existing) return existing;
    const id = crypto.randomUUID();
    localStorage.setItem('myUserId', id);
    return id;
  });
  const [myParticipantId, setMyParticipantId] = useState<string | null>(() =>
    localStorage.getItem(`joined_${shortCode}`) || null
  );
  const [clubName, setClubName] = useState('');

  useEffect(() => {
    const fetchRoom = async () => {
      try {
        if (!shortCode) return;
        const room = await sessionApi.getRoomByCode(shortCode);
        if (room?.id) {
          setRealRoomId(room.id);
          const fullRoom = await sessionApi.getRoom(room.id);
          setParticipants(fullRoom.participants || []);
        } else {
          setError('Oda bulunamadı.');
        }
      } catch {
        setError('Oda bilgileri alınamadı.');
      } finally {
        setLoading(false);
      }
    };
    fetchRoom();

    const interval = setInterval(() => {
      if (realRoomId) {
        sessionApi.getRoom(realRoomId)
          .then(r => setParticipants(r.participants || []))
          .catch(() => {});
      }
    }, 2000);
    return () => clearInterval(interval);
  }, [shortCode, realRoomId]);

  const { isConnected } = useSignalR({
    roomId: realRoomId,
    userId: myUserId,
    onParticipantJoined: (data) => {
      setParticipants(prev => {
        const p = { id: data.participantId, userId: data.userId, clubName: data.clubName, isReady: false, clubId: data.clubId };
        return prev.some(x => x.id === p.id) ? prev.map(x => x.id === p.id ? p : x) : [...prev, p];
      });
    },
    onParticipantReady: (data) => {
      setParticipants(prev => prev.map(p => p.id === data.participantId ? { ...p, isReady: true } : p));
    },
    onDraftReady: () => {
      navigate(`/draft/${shortCode}`);
    },
  });

  const handleJoinLobby = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!clubName.trim() || !realRoomId) return;
    try {
      const response = await sessionApi.joinRoom(realRoomId, myUserId, clubName);
      setMyParticipantId(response.participantId);
      localStorage.setItem(`joined_${shortCode}`, response.participantId);
    } catch {
      toast('error', 'Katılım başarısız. Tekrar dene.');
    }
  };

  const handleReady = async () => {
    if (!realRoomId || !myParticipantId) return;
    try {
      await sessionApi.markReady(realRoomId, myParticipantId, 'Draft');
    } catch {
      toast('error', 'Hazır durumu işaretlenemedi.');
    }
  };

  const handleCopyCode = () => {
    navigator.clipboard.writeText(shortCode || '').then(() => toast('success', 'Oda kodu kopyalandı!')).catch(() => {});
  };

  if (loading) return <Loader text="Lobi Yükleniyor..." />;
  if (error) return (
    <div className="cc-error-state">
      <div style={{ fontSize: '3rem' }}>⚠</div>
      <p>{error}</p>
      <Link to="/" className="cc-btn">Ana Sayfaya Dön</Link>
    </div>
  );

  const myP = participants.find(p => p.id === myParticipantId);
  const readyCount = participants.filter(p => p.isReady).length;
  const readyPct = participants.length > 0 ? (readyCount / participants.length) * 100 : 0;

  return (
    <div className="cc-lobby">
      {/* Header */}
      <div className="cc-lobby-header">
        <div>
          <h2 style={{ fontSize: '1.8rem', marginBottom: '0.25rem' }}>Lobi</h2>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            <div className={`cc-signal-dot ${isConnected ? 'connected' : ''}`} />
            <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)', fontFamily: 'var(--font-display)', letterSpacing: '0.5px' }}>
              {isConnected ? 'Gerçek Zamanlı Aktif' : 'Bağlanıyor...'}
            </span>
          </div>
        </div>
        <div
          className="cc-room-code"
          onClick={handleCopyCode}
          title="Kopyala"
          id="lobby-room-code"
        >
          <span className="cc-room-code-text">{shortCode}</span>
          <span className="cc-room-code-copy">📋 Kopyala</span>
        </div>
      </div>

      {/* Join form */}
      {!myParticipantId ? (
        <div className="cc-card" style={{ marginBottom: '1.5rem' }}>
          <h3 style={{ marginBottom: '0.5rem', fontSize: '1.3rem' }}>Kulübünü Oluştur</h3>
          <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem', marginBottom: '1.25rem' }}>
            Draft'a katılmak için bir kulüp adı belirle.
          </p>
          <form onSubmit={handleJoinLobby} style={{ display: 'flex', gap: '0.75rem' }}>
            <input
              type="text"
              className="cc-input"
              placeholder="Kulüp Adı (Örn: FC Kaplanlar)"
              value={clubName}
              onChange={e => setClubName(e.target.value)}
              id="input-club-name"
            />
            <button type="submit" className="cc-btn" disabled={!clubName.trim()} id="btn-join-lobby">
              Katıl
            </button>
          </form>
        </div>
      ) : (
        <div style={{ marginBottom: '1.5rem', display: 'flex', justifyContent: 'flex-end' }}>
          <button
            className={`cc-btn ${myP?.isReady ? '' : 'cc-btn-ready'}`}
            onClick={handleReady}
            disabled={myP?.isReady || !myP?.clubId}
            id="btn-mark-ready"
          >
            {myP?.isReady ? '✓ Hazırsın' : (!myP?.clubId ? <><span className="cc-spinner" />Kulüp Kuruluyor...</> : 'Hazırım!')}
          </button>
        </div>
      )}

      {/* Ready progress */}
      <div className="cc-card" style={{ padding: '1.5rem' }}>
        <div className="cc-ready-bar-wrap">
          <div className="cc-ready-bar-label">
            <span>Katılımcılar</span>
            <span style={{ color: readyCount === participants.length && participants.length > 0 ? 'var(--accent)' : 'var(--text-secondary)' }}>
              {readyCount} / {participants.length} Hazır
            </span>
          </div>
          <div className="cc-ready-bar">
            <div className="cc-ready-bar-fill" style={{ width: `${readyPct}%` }} />
          </div>
        </div>

        {participants.length === 0 ? (
          <div className="cc-empty">
            <div className="cc-empty-icon">👥</div>
            <p className="cc-empty-text">Henüz kimse katılmadı</p>
          </div>
        ) : (
          <ul className="cc-participant-list">
            {participants.map(p => (
              <li key={p.id} className={`cc-participant-item ${p.isReady ? 'ready' : ''}`}>
                <div className="cc-participant-avatar">
                  {p.clubName.charAt(0).toUpperCase()}
                </div>
                <span className="cc-participant-name">{p.clubName}</span>
                <div className={`cc-participant-badge ${p.isReady ? 'ready' : 'waiting'}`}>
                  <div className={`cc-badge-dot ${p.isReady ? 'ready' : 'waiting'}`} />
                  {p.isReady ? 'Hazır' : 'Bekleniyor'}
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
};

// ─── DRAFT PAGE ───────────────────────────────────────────────────────────────
const Draft = () => {
  const { roomId: shortCode } = useParams();
  const [draftSessionId, setDraftSessionId] = useState<string | null>(null);
  const [pool, setPool] = useState<Player[]>([]);
  const [draftState, setDraftState] = useState<DraftState | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isClaiming, setIsClaiming] = useState(false);

  const [searchQuery, setSearchQuery] = useState('');
  const [positionFilter, setPositionFilter] = useState('ALL');
  const [sortBy, setSortBy] = useState('OVERALL_DESC');
  const [currentPage, setCurrentPage] = useState(1);
  const ITEMS_PER_PAGE = 24;
  const MAX_ROSTER_SIZE = 20;

  const [myUserId] = useState(() => localStorage.getItem('myUserId') || crypto.randomUUID());
  useEffect(() => localStorage.setItem('myUserId', myUserId), [myUserId]);

  const [realRoomId, setRealRoomId] = useState('');
  const [myClubId, setMyClubId] = useState<string | null>(null);

  const [lineup, setLineup] = useState<Record<string, string | null>>({});

  // Formasyon icin YENİ bir state kavrami icat edilmedi — Sezon Dashboard'daki
  // AYNI Club.Formation alani okunuyor/yaziliyor (bkz. seasonApi.getClub /
  // updateFormation). Bu component'teki `formation` sadece o backend degerinin
  // yerel bir kopyasi (Sezon Dashboard'daki desenin birebir aynisi).
  const [formation, setFormation] = useState<string>('4-4-2');
  const FORMATION_SLOTS = FORMATIONS[formation] || FORMATIONS['4-4-2'];

  const handleDragStart = (e: React.DragEvent, playerId: string) => {
    e.dataTransfer.setData('playerId', playerId);
  };

  const handleDropToSlot = (e: React.DragEvent, slotId: string) => {
    e.preventDefault();
    e.currentTarget.classList.remove('drag-over');
    const playerId = e.dataTransfer.getData('playerId');
    if (!playerId || !draftSessionId) return;
    setLineup(prev => {
      const nl = { ...prev };
      Object.keys(nl).forEach(k => { if (nl[k] === playerId) nl[k] = null; });
      nl[slotId] = playerId;
      localStorage.setItem(`draft_lineup_${draftSessionId}`, JSON.stringify(nl));
      return nl;
    });
  };

  const handleDropToBench = (e: React.DragEvent) => {
    e.preventDefault();
    const playerId = e.dataTransfer.getData('playerId');
    if (!playerId || !draftSessionId) return;
    setLineup(prev => {
      const nl = { ...prev };
      Object.keys(nl).forEach(k => { if (nl[k] === playerId) nl[k] = null; });
      localStorage.setItem(`draft_lineup_${draftSessionId}`, JSON.stringify(nl));
      return nl;
    });
  };

  const handleFormationChange = async (newFormation: string) => {
    if (!myClubId || newFormation === formation) return;
    const previous = formation;
    setFormation(newFormation);
    // Backend de formasyon degisince lineup'i sifirliyor (bkz. Club.UpdateFormation)
    // — yerel draft-lineup onizlemesini de onunla senkron tut.
    setLineup({});
    if (draftSessionId) localStorage.removeItem(`draft_lineup_${draftSessionId}`);
    try {
      await seasonApi.updateFormation(myClubId, newFormation);
      toast('success', `Formasyon ${newFormation} olarak değiştirildi. İlk 11'i yeniden dizmeniz gerekiyor.`);
    } catch (err) {
      setFormation(previous);
      toast('error', 'Formasyon değiştirilemedi!');
    }
  };

  useEffect(() => {
    const init = async () => {
      if (!shortCode) return;
      const participantId = localStorage.getItem(`joined_${shortCode}`);

      const fetchWithRetry = async <T,>(op: () => Promise<T>, retries = 3): Promise<T> => {
        const delays = [500, 1000, 1500];
        for (let i = 0; i < retries; i++) {
          try { return await op(); }
          catch (err: any) {
            if (i === retries - 1) throw err;
            await new Promise(r => setTimeout(r, delays[i]));
          }
        }
        throw new Error('Unreachable');
      };

      try {
        const room = await fetchWithRetry(() => sessionApi.getRoomByCode(shortCode));
        if (room?.id) {
          setRealRoomId(room.id);
          setDraftSessionId(room.id);

          const fullRoom = await fetchWithRetry(() => sessionApi.getRoom(room.id));
          if (fullRoom?.participants) {
            const myP = fullRoom.participants.find((p: Participant) => p.id === participantId);
            if (myP?.clubId) {
              setMyClubId(myP.clubId);
              // Formasyon Sezon Dashboard'daki AYNI Club.Formation alanindan okunuyor
              // (ClubInitializedEvent henuz isleneli cok az olmus olabilir, o yuzden
              // sessiz gec — varsayilan '4-4-2' kalir, kullanici degistirebilir).
              seasonApi.getClub(myP.clubId)
                .then(club => setFormation(club.formation || '4-4-2'))
                .catch(() => {});
            }
          }

          const poolData = await fetchWithRetry(() => draftApi.getPool(room.id));
          setPool(poolData || []);

          const stateData = await fetchWithRetry(() => draftApi.getState(room.id));
          setDraftState(stateData);

          const savedLineup = localStorage.getItem(`draft_lineup_${room.id}`);
          if (savedLineup) setLineup(JSON.parse(savedLineup));
        } else {
          setError('Oda bulunamadı.');
        }
      } catch {
        setError('Draft verileri yüklenemedi. Lütfen sayfayı yenileyin.');
      } finally {
        setLoading(false);
      }
    };
    init();
  }, [shortCode]);

  const draftSessionIdRef = useRef<string | null>(null);
  useEffect(() => { draftSessionIdRef.current = draftSessionId; }, [draftSessionId]);

  const myClubIdRef = useRef<string | null>(null);
  useEffect(() => { myClubIdRef.current = myClubId; }, [myClubId]);

  const refreshFromBackend = useCallback(async () => {
    const sessionId = draftSessionIdRef.current;
    if (!sessionId) return;
    try {
      const [stateData, poolData] = await Promise.all([
        draftApi.getState(sessionId),
        draftApi.getPool(sessionId),
      ]);
      setDraftState(stateData);
      setPool(poolData || []);
      setIsClaiming(false);
    } catch {
      // Silent — next SignalR event will retry
    }
  }, []);

  const prevMyClubIdRef = useRef<string | null>(null);
  useEffect(() => {
    if (myClubId && prevMyClubIdRef.current === null) {
      refreshFromBackend();
    }
    prevMyClubIdRef.current = myClubId;
  }, [myClubId, refreshFromBackend]);

  const { isConnected } = useSignalR({
    roomId: realRoomId,
    userId: myUserId,
    onDraftTurnAdvanced: () => refreshFromBackend(),
    onPlayerClaimed: () => refreshFromBackend(),
  });

  const handleClaim = async (playerId: string) => {
    if (!draftSessionId || !myClubId || isClaiming || rosterCount >= MAX_ROSTER_SIZE) return;
    setIsClaiming(true);
    try {
      await draftApi.claimPlayer(draftSessionId, myClubId, playerId);
    } catch (err: any) {
      const reason = err.response?.data?.reason || 'Bilinmeyen hata';
      toast('error', `Oyuncu seçilemedi: ${reason}`);
      setIsClaiming(false);
    }
  };

  // ─── DERIVED STATE (single source of truth) ───────────────────────────────
  const myPicks = draftState?.picks?.filter(p => p.clubId === myClubId) || [];
  const rosterCount = myPicks.length;
  const allPickedPlayerIds = new Set((draftState?.picks || []).map(p => p.playerId));
  const myPickedPlayerIds = new Set(myPicks.map(p => p.playerId));
  const validLineup = Object.fromEntries(
    Object.entries(lineup).map(([slot, pid]) => [slot, pid && myPickedPlayerIds.has(pid) ? pid : null])
  );
  const lineupCount = Object.values(validLineup).filter(Boolean).length;

  // ─── FILTERING ────────────────────────────────────────────────────────────
  const processedPool = (() => {
    let f = pool;
    if (searchQuery.trim()) f = f.filter(p => p.name.toLowerCase().includes(searchQuery.toLowerCase()));
    if (positionFilter !== 'ALL') f = f.filter(p => p.position === positionFilter);
    f = [...f];
    switch (sortBy) {
      case 'OVERALL_DESC': f.sort((a, b) => b.overall - a.overall); break;
      case 'OVERALL_ASC':  f.sort((a, b) => a.overall - b.overall); break;
      case 'AGE_ASC':      f.sort((a, b) => a.age - b.age); break;
      case 'AGE_DESC':     f.sort((a, b) => b.age - a.age); break;
      case 'VALUE_DESC':   f.sort((a, b) => b.marketValue - a.marketValue); break;
      default:             f.sort((a, b) => b.overall - a.overall);
    }
    return f;
  })();

  const totalPages = Math.ceil(processedPool.length / ITEMS_PER_PAGE);
  const paginatedPool = processedPool.slice((currentPage - 1) * ITEMS_PER_PAGE, currentPage * ITEMS_PER_PAGE);

  useEffect(() => { setCurrentPage(1); }, [searchQuery, positionFilter, sortBy]);

  if (loading) return <Loader text="Draft Ekranı Hazırlanıyor..." />;
  if (error) return (
    <div className="cc-error-state">
      <div style={{ fontSize: '3rem' }}>⚠</div>
      <p>{error}</p>
      <button className="cc-btn" onClick={() => window.location.reload()}>Yeniden Dene</button>
    </div>
  );

  const isMyTurn = draftState?.currentClubId === myClubId && !!myClubId;
  const isDraftComplete = rosterCount >= MAX_ROSTER_SIZE;

  const turnText = isDraftComplete
    ? 'Draft Tamamlandı'
    : isMyTurn ? '🟢 Sende!'
    : draftState?.currentClubId ? '⏳ Bekleniyor...'
    : '🔌 Bağlanıyor...';

  const turnClass = isDraftComplete ? 'complete' : isMyTurn ? 'my-turn' : draftState?.currentClubId ? 'waiting' : 'connecting';

  return (
    <div className="cc-draft">
      {/* Turn Banner */}
      <div className={`cc-turn-banner ${isMyTurn && !isDraftComplete ? 'my-turn' : isDraftComplete ? '' : 'waiting'}`}>
        <div className="cc-turn-status">
          <div className="cc-turn-icon">{isMyTurn && !isDraftComplete ? '⚡' : isDraftComplete ? '🏁' : '⏳'}</div>
          <div>
            <div className="cc-turn-label">Sıra Durumu</div>
            <div className={`cc-turn-text ${turnClass}`}>{turnText}</div>
          </div>
        </div>

        <div style={{ display: 'flex', gap: '1.5rem', alignItems: 'center' }}>
          <div className="cc-roster-counter">
            <div>
              <div className="cc-roster-label">Kadron</div>
              <div style={{ display: 'flex', alignItems: 'baseline', gap: '4px' }}>
                <span className="cc-roster-big" style={{ color: isDraftComplete ? 'var(--accent)' : 'var(--accent)' }}>
                  {rosterCount}
                </span>
                <span style={{ color: 'var(--text-muted)', fontFamily: 'var(--font-display)', fontWeight: 600 }}>/ {MAX_ROSTER_SIZE}</span>
              </div>
            </div>
          </div>
          <div className="cc-roster-counter">
            <div>
              <div className="cc-roster-label">İlk 11</div>
              <div style={{ display: 'flex', alignItems: 'baseline', gap: '4px' }}>
                <span className="cc-roster-big" style={{ color: lineupCount === FORMATION_SLOTS.length ? 'var(--accent)' : 'var(--text-secondary)', fontSize: '1.4rem' }}>
                  {lineupCount}
                </span>
                <span style={{ color: 'var(--text-muted)', fontFamily: 'var(--font-display)', fontWeight: 600 }}>/ {FORMATION_SLOTS.length}</span>
              </div>
            </div>
          </div>
          <div className="cc-nav-signal">
            <div className={`cc-signal-dot ${isConnected ? 'connected' : ''}`} />
          </div>
        </div>
      </div>

      <div style={{ display: 'flex', gap: '1.5rem', alignItems: 'flex-start' }}>
        {/* ─── LEFT: POOL ─── */}
        <div style={{ flex: '1', minWidth: 0 }}>
          {/* Filters */}
          <div className="cc-filters">
            <input
              type="text"
              className="cc-input"
              placeholder="🔍  Oyuncu ara..."
              value={searchQuery}
              onChange={e => setSearchQuery(e.target.value)}
              style={{ flex: '1', minWidth: '180px' }}
              id="draft-search"
            />
            <div className="cc-pos-filter-group">
              {/* 'ALL' + detayli pozisyon kodlari, GK/DEF/MID/FWD gruplarina gore siralanmis.
                  Farkli formasyonlarin farkli slot ihtiyaclari oldugu icin (orn. 3-5-2 oynayan
                  birinin CB ihtiyaci 4-4-2'den farkli) kaba kategori yerine tam kod filtreleniyor. */}
              {(['ALL', 'GK', 'CB', 'RB', 'LB', 'RWB', 'LWB', 'CDM', 'CM', 'CAM', 'RM', 'LM', 'RW', 'LW', 'ST', 'CF'] as const).map(pos => (
                <button
                  key={pos}
                  onClick={() => setPositionFilter(pos)}
                  className={`cc-pos-pill ${positionFilter === pos ? `active-${pos === 'ALL' ? 'ALL' : POSITION_GROUP[pos]}` : ''}`}
                  id={`filter-${pos}`}
                >
                  {pos === 'ALL' ? 'Tümü' : pos}
                </button>
              ))}
            </div>
            <select
              className="cc-input"
              value={sortBy}
              onChange={e => setSortBy(e.target.value)}
              style={{ width: 'auto', flex: 'none' }}
              id="draft-sort"
            >
              <option value="OVERALL_DESC">Overall ↓</option>
              <option value="OVERALL_ASC">Overall ↑</option>
              <option value="AGE_ASC">Yaş ↑</option>
              <option value="AGE_DESC">Yaş ↓</option>
              <option value="VALUE_DESC">Değer ↓</option>
            </select>
          </div>

          {/* Grid */}
          {processedPool.length === 0 ? (
            <div className="cc-card cc-empty">
              <div className="cc-empty-icon">🔍</div>
              <p className="cc-empty-text">Kriterlere uygun oyuncu bulunamadı.</p>
            </div>
          ) : (
            <>
              <div className="cc-player-grid">
                {paginatedPool.map(player => {
                  const claimed = allPickedPlayerIds.has(player.playerId);
                  const canClaim = isMyTurn && !claimed && !isClaiming && rosterCount < MAX_ROSTER_SIZE;

                  return (
                    <div
                      key={player.playerId}
                      className={`cc-player-card ${claimed ? 'claimed' : ''}`}
                    >
                      <div
                        className="cc-player-overall"
                        style={{ color: overallColor(player.overall) }}
                      >
                        {player.overall}
                      </div>
                      <PosBadge pos={player.position} />
                      <div className="cc-player-name">{player.name}</div>
                      <div className="cc-player-meta">
                        <span>{player.age}</span> yaş &nbsp;·&nbsp; €<span>{(player.marketValue / 1_000_000).toFixed(1)}M</span>
                      </div>
                      <button
                        className={`cc-claim-btn ${
                          claimed ? 'taken' :
                          isClaiming ? 'loading' :
                          canClaim ? 'available' : 'not-turn'
                        }`}
                        onClick={() => canClaim && handleClaim(player.playerId)}
                        disabled={claimed || !isMyTurn || isClaiming || rosterCount >= MAX_ROSTER_SIZE}
                        id={`claim-${player.playerId}`}
                      >
                        {claimed ? 'Seçildi' : isClaiming ? 'Bekle...' : canClaim ? 'Seç' : '—'}
                      </button>
                    </div>
                  );
                })}
              </div>

              {totalPages > 1 && (
                <div className="cc-pagination">
                  <button
                    className="cc-btn cc-btn-ghost"
                    disabled={currentPage === 1}
                    onClick={() => setCurrentPage(p => p - 1)}
                    style={{ padding: '0.5rem 1rem' }}
                  >← Önceki</button>
                  <span className="cc-pagination-info">Sayfa {currentPage} / {totalPages}</span>
                  <button
                    className="cc-btn cc-btn-ghost"
                    disabled={currentPage === totalPages}
                    onClick={() => setCurrentPage(p => p + 1)}
                    style={{ padding: '0.5rem 1rem' }}
                  >Sonraki →</button>
                </div>
              )}
            </>
          )}
        </div>

        {/* ─── RIGHT: PITCH & BENCH ─── */}
        <div style={{ width: '340px', flexShrink: 0, position: 'sticky', top: '1.5rem', display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          {/* Pitch */}
          <div className="cc-card" style={{ padding: '1.25rem 1rem' }}>
            <h3 style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.85rem', fontSize: '1.1rem', borderBottom: '1px solid var(--border)', paddingBottom: '0.75rem', gap: '0.5rem' }}>
              <span style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                <span>İlk 11</span>
                <select
                  value={formation}
                  onChange={e => handleFormationChange(e.target.value)}
                  className="cc-input"
                  id="formation-select"
                  style={{ fontSize: '0.75rem', padding: '0.2rem 0.4rem', width: 'auto' }}
                >
                  {FORMATION_NAMES.map(f => <option key={f} value={f}>{f}</option>)}
                </select>
              </span>
              <span style={{ fontFamily: 'var(--font-display)', fontSize: '0.85rem', color: lineupCount === FORMATION_SLOTS.length ? 'var(--accent)' : 'var(--text-secondary)' }}>
                {lineupCount}/{FORMATION_SLOTS.length}
              </span>
            </h3>
            <div className="pitch-container">
              <div className="pitch-lines" />
              <div className="penalty-box-top" />
              <div className="penalty-box-bottom" />
              {FORMATION_SLOTS.map(slot => {
                const filledPlayerId = validLineup[slot.id];
                const player = pool.find(p => p.playerId === filledPlayerId);
                return (
                  <div
                    key={slot.id}
                    className={`pitch-slot ${player ? 'filled' : ''}`}
                    style={{ top: slot.top, left: slot.left }}
                    onDragOver={e => { e.preventDefault(); e.currentTarget.classList.add('drag-over'); }}
                    onDragLeave={e => e.currentTarget.classList.remove('drag-over')}
                    onDrop={e => handleDropToSlot(e, slot.id)}
                  >
                    {player ? (
                      <div
                        draggable
                        onDragStart={e => handleDragStart(e, player.playerId)}
                        className="player-draggable"
                        style={{ width: '100%', height: '100%', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center' }}
                      >
                        <div style={{ fontFamily: 'var(--font-display)', color: overallColor(player.overall), fontWeight: 900, fontSize: '1rem', lineHeight: 1 }}>
                          {player.overall}
                        </div>
                        <div style={{ fontSize: '0.6rem', fontWeight: 700, overflow: 'hidden', whiteSpace: 'nowrap', width: '90%', textAlign: 'center', marginTop: '2px', color: 'var(--text-primary)' }}>
                          {player.name.split(' ').pop()}
                        </div>
                        <div className="slot-label" style={{ marginTop: '2px' }}>{slot.label}</div>
                      </div>
                    ) : (
                      <span className="slot-label">{slot.label}</span>
                    )}
                  </div>
                );
              })}
            </div>
          </div>

          {/* Bench */}
          <div
            className="cc-card"
            style={{ padding: '1.25rem 1rem', minHeight: '120px' }}
            onDragOver={e => e.preventDefault()}
            onDrop={handleDropToBench}
          >
            <h3 style={{ fontSize: '1.1rem', marginBottom: '0.85rem', borderBottom: '1px solid var(--border)', paddingBottom: '0.75rem' }}>
              Yedekler
            </h3>
            {rosterCount === 0 ? (
              <div className="cc-empty" style={{ padding: '1.5rem 0' }}>
                <p className="cc-empty-text">Henüz oyuncu seçmedin</p>
              </div>
            ) : (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem', maxHeight: '280px', overflowY: 'auto', paddingRight: '4px' }}>
                {myPicks.map(pick => {
                  const isPlaced = Object.values(validLineup).includes(pick.playerId);
                  if (isPlaced) return null;
                  const player = pool.find(p => p.playerId === pick.playerId);
                  if (!player) return null;
                  return (
                    <div
                      key={pick.playerId}
                      draggable
                      onDragStart={e => handleDragStart(e, player.playerId)}
                      className="cc-bench-item player-draggable"
                    >
                      <span className="cc-bench-overall">{player.overall}</span>
                      <div style={{ flex: 1 }}>
                        <div style={{ fontWeight: 600, fontSize: '0.85rem' }}>{player.name}</div>
                        <div style={{ fontSize: '0.7rem' }}><PosBadge pos={player.position} /></div>
                      </div>
                    </div>
                  );
                })}
                {myPicks.length > 0 && myPicks.every(pick => Object.values(lineup).includes(pick.playerId)) && (
                  <p style={{ color: 'var(--accent)', textAlign: 'center', fontSize: '0.85rem', padding: '0.5rem 0' }}>
                    ✓ Tüm oyuncular sahada
                  </p>
                )}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

// ─── PLACEHOLDER PAGES ────────────────────────────────────────────────────────
const PlaceholderPage = ({ title, icon, desc }: { title: string; icon: string; desc: string }) => (
  <div style={{ maxWidth: '700px', margin: '4rem auto', padding: '0 1rem' }}>
    <div className="cc-card" style={{ textAlign: 'center', padding: '4rem 2rem' }}>
      <div style={{ fontSize: '4rem', marginBottom: '1.5rem' }}>{icon}</div>
      <h2 style={{ fontSize: '2rem', marginBottom: '0.75rem' }}>{title}</h2>
      <p style={{ color: 'var(--text-secondary)' }}>{desc}</p>
    </div>
  </div>
);

// ─── NAVIGATION ──────────────────────────────────────────────────────────────
const Navigation = () => {
  const location = useLocation();
  const isActive = (path: string) => location.pathname.startsWith(path) && path !== '/';
  
  // Extract roomId from the current path (e.g. /draft/123456 -> 123456)
  const pathParts = location.pathname.split('/');
  const currentRoomId = pathParts.length > 2 ? pathParts[2] : '';

  return (
    <nav className="cc-nav">
      <div className="cc-nav-inner">
        <Link to="/" className="cc-nav-logo">
          CLUB<span>CRAFT</span>
        </Link>
        <ul className="cc-nav-links">
          {[
            { to: '/lobby', label: 'Lobi' },
            { to: '/draft', label: 'Draft' },
            { to: '/season', label: 'Sezon' },
            { to: '/sponsorship', label: 'Sponsorluk' },
            { to: '/summary', label: 'Özet' },
          ].map(({ to, label }) => {
            // Only append roomId if we are inside a room
            const linkPath = currentRoomId ? `${to}/${currentRoomId}` : to;
            return (
              <li key={to}>
                <Link to={linkPath} className={`cc-nav-link ${isActive(to) ? 'active' : ''}`}>
                  {label}
                </Link>
              </li>
            );
          })}
        </ul>
      </div>
    </nav>
  );
};

// ─── APP ──────────────────────────────────────────────────────────────────────
function App() {
  return (
    <BrowserRouter>
      <Navigation />
      <ToastContainer />
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/lobby/:roomId" element={<Lobby />} />
        <Route path="/draft/:roomId" element={<Draft />} />
        <Route path="/season/:roomId" element={<SeasonDashboard />} />
        <Route path="/sponsorship/:roomId" element={<SponsorshipDashboard />} />
        <Route path="/summary/:roomId" element={<SummaryDashboard />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
