import React, { useState, useEffect } from 'react';
import { BrowserRouter, Routes, Route, Link, useParams, useLocation, useNavigate } from 'react-router-dom';
import './index.css';
import { sessionApi } from './api/sessionApi';
import type { Participant } from './api/sessionApi';
import { useSignalR } from './hooks/useSignalR';

const Home = () => {
  const [roomCode, setRoomCode] = useState('');
  const [isCreating, setIsCreating] = useState(false);
  const [isJoining, setIsJoining] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();

  const handleCreateRoom = async () => {
    try {
      setIsCreating(true);
      setError(null);
      const hostUserId = crypto.randomUUID();
      const response = await sessionApi.createRoom(hostUserId, 6);
      navigate(`/lobby/${response.shortCode || response.roomId}`);
    } catch (err: any) {
      console.error(err);
      setError('Oda oluşturulurken bir hata oluştu: ' + (err.response?.data || err.message));
    } finally {
      setIsCreating(false);
    }
  };

  const handleJoin = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!roomCode.trim()) return;
    
    try {
      setIsJoining(true);
      setError(null);
      const code = roomCode.trim().toUpperCase();
      const response = await sessionApi.getRoomByCode(code);
      if (response && response.id) {
        navigate(`/lobby/${response.shortCode}`);
      } else {
        setError('Oda bulunamadı.');
      }
    } catch (err: any) {
      console.error(err);
      setError('Oda bulunamadı veya bağlantı hatası.');
    } finally {
      setIsJoining(false);
    }
  };

  return (
    <div style={{ maxWidth: '900px', margin: '0 auto', padding: '2rem 1rem', display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
      <div style={{ textAlign: 'center', marginBottom: '4rem', marginTop: '2rem' }}>
        <h1 style={{ fontSize: '4rem', fontWeight: '700', marginBottom: '0.5rem', letterSpacing: '2px', textTransform: 'uppercase' }}>
          CLUB<span style={{ color: 'var(--accent)' }}>CRAFT</span>
        </h1>
        <p style={{ color: 'var(--text-secondary)', fontSize: '1.2rem', maxWidth: '600px' }}>
          Kendi futbol kulübünü kur, kadronu draft et ve arkadaşlarına karşı zekanı konuştur.
        </p>
      </div>

      {error && (
        <div style={{ backgroundColor: 'var(--danger)', color: 'white', padding: '1rem', borderRadius: '8px', marginBottom: '2rem', width: '100%', maxWidth: '830px', textAlign: 'center' }}>
          {error}
        </div>
      )}

      <div style={{ display: 'flex', gap: '2rem', width: '100%', flexWrap: 'wrap', justifyContent: 'center' }}>
        {/* Create Room Card */}
        <div className="cc-card" style={{ flex: '1', minWidth: '300px', maxWidth: '400px', display: 'flex', flexDirection: 'column', alignItems: 'center', textAlign: 'center' }}>
          <h2 style={{ fontSize: '2rem', marginBottom: '1rem' }}>YENİ SEZON BAŞLAT</h2>
          <p style={{ color: 'var(--text-secondary)', marginBottom: '2rem' }}>
            Arkadaşlarını davet etmek için yeni bir lig oluştur. Oyunun host'u sen ol.
          </p>
          <div style={{ marginTop: 'auto', width: '100%' }}>
            <button 
              className="cc-btn" 
              style={{ width: '100%', opacity: isCreating ? 0.7 : 1 }} 
              onClick={handleCreateRoom}
              disabled={isCreating}
            >
              {isCreating ? 'Oluşturuluyor...' : 'Oda Kur'}
            </button>
          </div>
        </div>

        {/* Join Room Card */}
        <div className="cc-card" style={{ flex: '1', minWidth: '300px', maxWidth: '400px', display: 'flex', flexDirection: 'column', alignItems: 'center', textAlign: 'center' }}>
          <h2 style={{ fontSize: '2rem', marginBottom: '1rem' }}>ODAYA KATIL</h2>
          <p style={{ color: 'var(--text-secondary)', marginBottom: '2rem' }}>
            Arkadaşından aldığın 6 haneli kısa kodu girerek lige dahil ol.
          </p>
          <form onSubmit={handleJoin} style={{ width: '100%', display: 'flex', flexDirection: 'column', gap: '1rem', marginTop: 'auto' }}>
            <input 
              type="text" 
              className="cc-input" 
              placeholder="Örn: TIGER42" 
              value={roomCode}
              onChange={(e) => setRoomCode(e.target.value)}
              style={{ textAlign: 'center', textTransform: 'uppercase', letterSpacing: '2px', fontWeight: 'bold' }}
              maxLength={6}
            />
            <button 
              type="submit" 
              className="cc-btn" 
              style={{ width: '100%', backgroundColor: 'transparent', border: '1px solid var(--accent)', color: 'var(--accent)', opacity: isJoining ? 0.7 : 1 }}
              disabled={isJoining || !roomCode.trim()}
            >
              {isJoining ? 'Bağlanıyor...' : 'Katıl'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
};

const Lobby = () => {
  const { roomId: shortCode } = useParams(); // URL'de roomId diye okuduğumuz şey aslında shortCode
  const navigate = useNavigate();
  
  const [realRoomId, setRealRoomId] = useState<string>('');
  const [participants, setParticipants] = useState<Participant[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  
  // Fake auth for now
  const [myUserId] = useState(() => {
    const existing = localStorage.getItem('myUserId');
    if (existing) return existing;
    const newId = crypto.randomUUID();
    localStorage.setItem('myUserId', newId);
    return newId;
  });
  const [myParticipantId, setMyParticipantId] = useState<string | null>(() => {
    return localStorage.getItem(`joined_${shortCode}`) || null;
  });
  const [clubName, setClubName] = useState('');
  
  // 1. Component mount olunca ShortCode'u çöz ve detayları çek
  useEffect(() => {
    const fetchRoom = async () => {
      try {
        if (!shortCode) return;
        const room = await sessionApi.getRoomByCode(shortCode);
        if (room && room.id) {
          setRealRoomId(room.id);
          // Odanın güncel katılımcı listesini al
          const fullRoom = await sessionApi.getRoom(room.id);
          setParticipants(fullRoom.participants || []);
        } else {
          setError('Oda bulunamadı.');
        }
      } catch (err) {
        console.error(err);
        setError('Oda bilgileri alınamadı.');
      } finally {
        setLoading(false);
      }
    };
    fetchRoom();
    
    // Auto-poll to fetch ClubId if it's missing for myself
    const interval = setInterval(() => {
        if (realRoomId) {
            sessionApi.getRoom(realRoomId).then(fullRoom => {
                setParticipants(fullRoom.participants || []);
            }).catch(console.error);
        }
    }, 2000);
    
    return () => clearInterval(interval);
  }, [shortCode, realRoomId]);

  // 2. RealRoomId çözüldükten sonra SignalR'a bağlan
  const { isConnected } = useSignalR({
    roomId: realRoomId, // Gerçek ID ile bağlan
    userId: myUserId,
    onParticipantJoined: (data) => {
      console.log('SIGNALR EVENT: onParticipantJoined', JSON.stringify(data));
      setParticipants(prev => {
        const newP = {
          id: data.participantId,
          userId: data.userId,
          clubName: data.clubName,
          isReady: false,
          clubId: data.clubId // SignalR update should ideally include it, but polling will fix it.
        };
        return prev.some(p => p.id === newP.id)
          ? prev.map(p => p.id === newP.id ? newP : p)
          : [...prev, newP];
      });
    },
    onParticipantReady: (data) => {
      console.log('SIGNALR EVENT: onParticipantReady', JSON.stringify(data));
      setParticipants(prev => prev.map(p => 
        p.id === data.participantId ? { ...p, isReady: true } : p
      ));
    },
    onDraftReady: (data) => {
      console.log('Draft is ready!', data);
      // Herkes hazır olduğunda otomatik draft'a yönlendir
      navigate(`/draft/${shortCode}`);
    }
  });

  const handleJoinLobby = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!clubName.trim() || !realRoomId) return;
    try {
      const response = await sessionApi.joinRoom(realRoomId, myUserId, clubName);
      setMyParticipantId(response.participantId);
      localStorage.setItem(`joined_${shortCode}`, response.participantId);
    } catch (err) {
      console.error(err);
      alert('Katılım başarısız');
    }
  };

  const handleReady = async () => {
    if (!realRoomId || !myParticipantId) return;
    try {
      await sessionApi.markReady(realRoomId, myParticipantId, 'Draft');
      // Optimistic update removed; letting SignalR handle the broadcast to all (including self)
    } catch (err) {
      console.error(err);
      alert('Hazır durumu işaretlenemedi');
    }
  };

  if (loading) return <div style={{ textAlign: 'center', padding: '4rem' }}>Yükleniyor...</div>;
  if (error) return <div style={{ textAlign: 'center', padding: '4rem', color: 'var(--danger)' }}>{error}</div>;

  return (
    <div style={{ padding: '2rem', maxWidth: '800px', margin: '0 auto' }}>
      <div className="cc-card" style={{ marginBottom: '2rem' }}>
        <h2 style={{ fontSize: '2.5rem', borderBottom: '1px solid rgba(255,255,255,0.1)', paddingBottom: '1rem', marginBottom: '1rem' }}>
          Lobi Kodun: <span style={{ color: 'var(--accent)', letterSpacing: '2px' }}>{shortCode}</span>
        </h2>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <p style={{ color: 'var(--text-secondary)' }}>
            SignalR Bağlantısı: {isConnected ? <span style={{ color: 'var(--accent)' }}>Aktif</span> : <span style={{ color: 'var(--danger)' }}>Bağlanıyor...</span>}
          </p>
          <p style={{ color: 'var(--text-secondary)' }}>
            Oda ID: <span style={{ fontSize: '0.8rem', opacity: 0.5 }}>{realRoomId}</span>
          </p>
        </div>
      </div>

      {!myParticipantId ? (
        <div className="cc-card" style={{ marginBottom: '2rem' }}>
          <h3 style={{ marginBottom: '1rem' }}>Kulübünü Oluştur</h3>
          <form onSubmit={handleJoinLobby} style={{ display: 'flex', gap: '1rem' }}>
            <input 
              type="text" 
              className="cc-input" 
              placeholder="Kulüp Adı (Örn: FC Kaplanlar)" 
              value={clubName}
              onChange={(e) => setClubName(e.target.value)}
            />
            <button type="submit" className="cc-btn" disabled={!clubName.trim()}>Katıl</button>
          </form>
        </div>
      ) : (
        <div style={{ textAlign: 'right', marginBottom: '2rem' }}>
          <button 
            className="cc-btn" 
            onClick={handleReady}
            disabled={participants.find(p => p.id === myParticipantId)?.isReady || !participants.find(p => p.id === myParticipantId)?.clubId}
          >
            {participants.find(p => p.id === myParticipantId)?.isReady ? 'Hazırsın ✓' : (!participants.find(p => p.id === myParticipantId)?.clubId ? 'Kulüp Kuruluyor...' : 'Hazırım!')}
          </button>
        </div>
      )}

      <div className="cc-card">
        <h3 style={{ marginBottom: '1.5rem' }}>Katılımcılar ({participants.length}/6)</h3>
        {participants.length === 0 ? (
          <p style={{ color: 'var(--text-secondary)', textAlign: 'center', padding: '2rem 0' }}>Henüz kimse katılmadı.</p>
        ) : (
          <ul style={{ listStyle: 'none', padding: 0, display: 'flex', flexDirection: 'column', gap: '1rem' }}>
            {participants.map(p => (
              <li key={p.id} style={{ display: 'flex', justifyContent: 'space-between', padding: '1rem', backgroundColor: 'rgba(0,0,0,0.2)', borderRadius: '8px', border: '1px solid rgba(255,255,255,0.05)' }}>
                <span style={{ fontWeight: 'bold', fontSize: '1.2rem' }}>{p.clubName}</span>
                {p.isReady ? (
                  <span style={{ color: 'var(--accent)', fontWeight: 'bold' }}>✓ Hazır</span>
                ) : (
                  <span style={{ color: 'var(--text-secondary)' }}>Bekleniyor...</span>
                )}
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
};

import { draftApi, type Player, type DraftState } from './api/draftApi';

const Draft = () => {
  const { roomId: shortCode } = useParams();
  const [draftSessionId, setDraftSessionId] = useState<string | null>(null);
  const [pool, setPool] = useState<Player[]>([]);
  const [draftState, setDraftState] = useState<DraftState | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isClaiming, setIsClaiming] = useState(false); // To prevent concurrent clicks
  
  // States for Filtering & Pagination
  const [searchQuery, setSearchQuery] = useState('');
  const [positionFilter, setPositionFilter] = useState('ALL');
  const [sortBy, setSortBy] = useState('OVERALL_DESC');
  const [currentPage, setCurrentPage] = useState(1);
  const ITEMS_PER_PAGE = 24;
  const MAX_ROSTER_SIZE = 20;
  
  // User & Room states
  const [myUserId] = useState(() => localStorage.getItem('myUserId') || crypto.randomUUID());
  useEffect(() => localStorage.setItem('myUserId', myUserId), [myUserId]);
  
  const [realRoomId, setRealRoomId] = useState<string>('');
  const [myParticipantId, setMyParticipantId] = useState<string | null>(null);
  const [myClubId, setMyClubId] = useState<string | null>(null);

  // Lineup & Drag-Drop States
  const [lineup, setLineup] = useState<Record<string, string | null>>({});
  const [draggedPlayerId, setDraggedPlayerId] = useState<string | null>(null);

  const FORMATION_SLOTS = [
    { id: 'ST1', label: 'ST', top: '20%', left: '35%' },
    { id: 'ST2', label: 'ST', top: '20%', left: '65%' },
    { id: 'LM', label: 'LM', top: '45%', left: '20%' },
    { id: 'CM1', label: 'CM', top: '48%', left: '40%' },
    { id: 'CM2', label: 'CM', top: '48%', left: '60%' },
    { id: 'RM', label: 'RM', top: '45%', left: '80%' },
    { id: 'LB', label: 'LB', top: '72%', left: '20%' },
    { id: 'CB1', label: 'CB', top: '75%', left: '40%' },
    { id: 'CB2', label: 'CB', top: '75%', left: '60%' },
    { id: 'RB', label: 'RB', top: '72%', left: '80%' },
    { id: 'GK', label: 'GK', top: '90%', left: '50%' },
  ];

  const handleDragStart = (e: React.DragEvent, playerId: string) => {
    e.dataTransfer.setData('playerId', playerId);
    setDraggedPlayerId(playerId);
  };

  const handleDropToSlot = (e: React.DragEvent, slotId: string) => {
    e.preventDefault();
    e.currentTarget.classList.remove('drag-over');
    const playerId = e.dataTransfer.getData('playerId');
    if (!playerId || !draftSessionId) return;
    
    setLineup(prev => {
        const newLineup = { ...prev };
        Object.keys(newLineup).forEach(k => { if (newLineup[k] === playerId) newLineup[k] = null; });
        newLineup[slotId] = playerId;
        localStorage.setItem(`draft_lineup_${draftSessionId}`, JSON.stringify(newLineup));
        return newLineup;
    });
    setDraggedPlayerId(null);
  };

  const handleDropToBench = (e: React.DragEvent) => {
    e.preventDefault();
    const playerId = e.dataTransfer.getData('playerId');
    if (!playerId || !draftSessionId) return;
    
    setLineup(prev => {
        const newLineup = { ...prev };
        Object.keys(newLineup).forEach(k => { if (newLineup[k] === playerId) newLineup[k] = null; });
        localStorage.setItem(`draft_lineup_${draftSessionId}`, JSON.stringify(newLineup));
        return newLineup;
    });
    setDraggedPlayerId(null);
  };

  useEffect(() => {
    const init = async () => {
      try {
        if (!shortCode) return;
        const participantId = localStorage.getItem(`joined_${shortCode}`);
        if (participantId) setMyParticipantId(participantId);

        const room = await sessionApi.getRoomByCode(shortCode);
        if (room && room.id) {
          setRealRoomId(room.id);
          setDraftSessionId(room.id);
          
          const fullRoom = await sessionApi.getRoom(room.id);
          if (fullRoom && fullRoom.participants) {
              const myP = fullRoom.participants.find(p => p.id === participantId);
              if (myP && myP.clubId) {
                  setMyClubId(myP.clubId);
              }
          }
          
          const poolData = await draftApi.getPool(room.id);
          setPool(poolData || []);
          
          const stateData = await draftApi.getState(room.id);
          setDraftState(stateData);
          
          const savedLineup = localStorage.getItem(`draft_lineup_${room.id}`);
          if (savedLineup) setLineup(JSON.parse(savedLineup));
          
        } else {
          setError('Oda bulunamadı.');
        }
      } catch (err) {
        console.error(err);
        setError('Draft verileri yüklenemedi.');
      } finally {
        setLoading(false);
      }
    };
    init();
  }, [shortCode]);

  const { isConnected } = useSignalR({
    roomId: realRoomId,
    userId: myUserId,
    onDraftTurnAdvanced: (data) => {
      console.log('SIGNALR EVENT: Turn advanced!', data);
      setDraftState(prev => prev ? {
        ...prev,
        currentPickIndex: data.nextPickIndex,
        currentClubId: data.nextClubId
      } : null);
    },
    onPlayerClaimed: (data) => {
      console.log('SIGNALR EVENT: Player claimed!', data);
      setPool(prev => prev.map(p => 
        p.playerId === data.playerId ? { ...p, isClaimed: true } : p
      ));
      
      // Add the pick to draftState so the roster updates instantly
      setDraftState(prev => {
        if (!prev) return prev;
        
        // Prevent adding duplicate pick if already exists
        if (prev.picks?.some(p => p.playerId === data.playerId)) {
            return prev;
        }

        const newPick = {
          pickNumber: data.pickNumber,
          clubId: data.clubId,
          playerId: data.playerId,
          claimedAt: data.occurredOn || new Date().toISOString()
        };
        return {
          ...prev,
          picks: [...(prev.picks || []), newPick]
        };
      });
    }
  });

  const handleClaim = async (playerId: string) => {
    if (!draftSessionId || !myClubId || isClaiming || rosterCount >= MAX_ROSTER_SIZE) return;
    
    setIsClaiming(true);
    try {
      await draftApi.claimPlayer(draftSessionId, myClubId, playerId);
      // We rely on SignalR `onPlayerClaimed` to update state to ensure consistency across clients.
    } catch (err: any) {
      console.error(err);
      const reason = err.response?.data?.reason || 'Bilinmeyen hata';
      alert(`Oyuncu seçilemedi: ${reason}`);
    } finally {
      // Small delay to allow SignalR event to arrive before unlocking buttons
      setTimeout(() => setIsClaiming(false), 300);
    }
  };
  
  // Calculate Roster
  const myPicks = draftState?.picks?.filter(p => p.clubId === myClubId) || [];
  const rosterCount = myPicks.length;
  
  // Filtering & Sorting Logic
  const getFilteredAndSortedPool = () => {
    let filtered = pool;
    
    if (searchQuery.trim() !== '') {
      filtered = filtered.filter(p => p.name.toLowerCase().includes(searchQuery.toLowerCase()));
    }
    
    if (positionFilter !== 'ALL') {
      filtered = filtered.filter(p => p.position === positionFilter);
    }
    
    // Create a copy of the array before sorting
    filtered = [...filtered];

    filtered.sort((a, b) => {
      switch (sortBy) {
        case 'OVERALL_DESC': return b.overall - a.overall;
        case 'OVERALL_ASC': return a.overall - b.overall;
        case 'AGE_ASC': return a.age - b.age;
        case 'AGE_DESC': return b.age - a.age;
        case 'VALUE_DESC': return b.marketValue - a.marketValue;
        case 'VALUE_ASC': return a.marketValue - b.marketValue;
        default: return b.overall - a.overall;
      }
    });
    
    return filtered;
  };

  const processedPool = getFilteredAndSortedPool();
  const totalPages = Math.ceil(processedPool.length / ITEMS_PER_PAGE);
  const paginatedPool = processedPool.slice((currentPage - 1) * ITEMS_PER_PAGE, currentPage * ITEMS_PER_PAGE);

  // Reset pagination when filters change
  useEffect(() => {
    setCurrentPage(1);
  }, [searchQuery, positionFilter, sortBy]);

  if (loading) return <div style={{ textAlign: 'center', padding: '4rem' }}>Yükleniyor...</div>;
  if (error) return <div style={{ textAlign: 'center', padding: '4rem', color: 'var(--danger)' }}>{error}</div>;

  return (
    <div style={{ padding: '2rem', maxWidth: '1400px', margin: '0 auto' }}>
      
      {/* HEADER PANEL */}
      <div className="cc-card" style={{ marginBottom: '2rem', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
            <h2>Draft Odası</h2>
            <p style={{ color: 'var(--text-secondary)', marginTop: '0.5rem' }}>
              Sıra: <span style={{ color: 'var(--accent)', fontWeight: 'bold' }}>
                {rosterCount >= MAX_ROSTER_SIZE ? 'Draft Tamamlandı' : (draftState?.currentClubId === myClubId ? 'Sende!' : (draftState?.currentClubId ? 'Bekleniyor...' : 'Bilinmiyor'))}
              </span>
            </p>
        </div>
        
        <div style={{ textAlign: 'right' }}>
            <h3 style={{ fontSize: '1.5rem', margin: 0 }}>
                Kadron: <span style={{ color: rosterCount >= MAX_ROSTER_SIZE ? 'var(--danger)' : 'var(--accent)' }}>
                    {rosterCount} / {MAX_ROSTER_SIZE}
                </span>
            </h3>
            <p style={{ color: 'var(--text-secondary)', fontSize: '0.8rem', marginTop: '0.5rem' }}>
              SignalR Bağlantısı: {isConnected ? <span style={{ color: 'var(--accent)' }}>Aktif</span> : <span style={{ color: 'var(--danger)' }}>Bağlanıyor...</span>}
            </p>
        </div>
      </div>

      <div style={{ display: 'flex', gap: '2rem', alignItems: 'flex-start' }}>
          
        {/* MAIN POOL AREA */}
        <div style={{ flex: '1', minWidth: 0 }}>
            {/* FILTERS */}
            <div className="cc-card" style={{ marginBottom: '1.5rem', display: 'flex', gap: '1rem', flexWrap: 'wrap', alignItems: 'center', padding: '1rem' }}>
                <input 
                    type="text" 
                    className="cc-input" 
                    placeholder="Oyuncu Ara..." 
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    style={{ flex: '1', minWidth: '200px' }}
                />
                
                <div style={{ display: 'flex', gap: '0.5rem' }}>
                    {['ALL', 'GK', 'DEF', 'MID', 'FWD'].map(pos => (
                        <button 
                            key={pos}
                            onClick={() => setPositionFilter(pos)}
                            style={{
                                padding: '0.5rem 1rem',
                                borderRadius: '4px',
                                backgroundColor: positionFilter === pos ? 'var(--accent)' : 'rgba(255,255,255,0.05)',
                                color: positionFilter === pos ? '#000' : 'var(--text-primary)',
                                border: '1px solid rgba(255,255,255,0.1)',
                                cursor: 'pointer',
                                fontWeight: positionFilter === pos ? 'bold' : 'normal'
                            }}
                        >
                            {pos === 'ALL' ? 'Tümü' : pos}
                        </button>
                    ))}
                </div>
                
                <select 
                    className="cc-input" 
                    value={sortBy} 
                    onChange={(e) => setSortBy(e.target.value)}
                    style={{ width: 'auto' }}
                >
                    <option value="OVERALL_DESC">Overall (Yüksek → Düşük)</option>
                    <option value="OVERALL_ASC">Overall (Düşük → Yüksek)</option>
                    <option value="AGE_ASC">Yaş (Genç → Yaşlı)</option>
                    <option value="AGE_DESC">Yaş (Yaşlı → Genç)</option>
                    <option value="VALUE_DESC">Değer (Yüksek → Düşük)</option>
                </select>
            </div>
            
            {/* GRID */}
            {processedPool.length === 0 ? (
                <div className="cc-card" style={{ textAlign: 'center', padding: '3rem' }}>
                    <p style={{ color: 'var(--text-secondary)' }}>Kriterlere uygun oyuncu bulunamadı.</p>
                </div>
            ) : (
                <>
                    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: '1rem' }}>
                        {paginatedPool.map(player => (
                          <div key={player.playerId} style={{ 
                            backgroundColor: player.isClaimed ? 'rgba(0,0,0,0.5)' : 'var(--bg-card)',
                            border: '1px solid rgba(255,255,255,0.05)',
                            borderRadius: '8px',
                            padding: '1rem',
                            textAlign: 'center',
                            opacity: player.isClaimed ? 0.4 : 1,
                            position: 'relative',
                            transition: 'all 0.2s',
                          }}>
                            {player.isClaimed && (
                                <div style={{
                                    position: 'absolute',
                                    top: '10px', right: '10px',
                                    backgroundColor: 'var(--danger)',
                                    color: '#fff',
                                    padding: '2px 8px',
                                    borderRadius: '12px',
                                    fontSize: '0.7rem',
                                    fontWeight: 'bold'
                                }}>
                                    SEÇİLDİ
                                </div>
                            )}
                            
                            <div style={{ fontSize: '1.8rem', fontWeight: '900', marginBottom: '0.5rem', color: 'var(--accent)' }}>
                              {player.overall}
                            </div>
                            <h3 style={{ marginBottom: '0.2rem', fontSize: '1.1rem' }}>{player.name}</h3>
                            <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem', marginBottom: '1rem' }}>
                              <span style={{ fontWeight: 'bold', color: 'var(--text-primary)' }}>{player.position}</span> | Yaş: {player.age} <br/>
                              <span style={{ fontSize: '0.8rem' }}>€{(player.marketValue / 1000000).toFixed(1)}M</span>
                            </p>
                            <button 
                              className="cc-btn" 
                              onClick={() => handleClaim(player.playerId)}
                              disabled={player.isClaimed || draftState?.currentClubId !== myClubId || rosterCount >= MAX_ROSTER_SIZE || isClaiming}
                              style={{ 
                                width: '100%', 
                                padding: '0.5rem',
                                backgroundColor: (player.isClaimed || rosterCount >= MAX_ROSTER_SIZE || isClaiming) ? 'rgba(255,255,255,0.1)' : (draftState?.currentClubId !== myClubId ? 'rgba(255,255,255,0.05)' : 'var(--accent)'),
                                color: (player.isClaimed || draftState?.currentClubId !== myClubId || rosterCount >= MAX_ROSTER_SIZE || isClaiming) ? 'var(--text-secondary)' : '#000',
                                cursor: (player.isClaimed || draftState?.currentClubId !== myClubId || rosterCount >= MAX_ROSTER_SIZE || isClaiming) ? 'not-allowed' : 'pointer',
                                border: 'none'
                              }}
                            >
                              {player.isClaimed ? 'Seçildi' : (isClaiming ? 'Bekle...' : 'Seç')}
                            </button>
                          </div>
                        ))}
                    </div>
                    
                    {/* PAGINATION */}
                    {totalPages > 1 && (
                        <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', gap: '1rem', marginTop: '2rem' }}>
                            <button 
                                className="cc-btn"
                                disabled={currentPage === 1}
                                onClick={() => setCurrentPage(p => p - 1)}
                                style={{ padding: '0.5rem 1rem' }}
                            >
                                Önceki
                            </button>
                            <span style={{ color: 'var(--text-secondary)' }}>
                                Sayfa {currentPage} / {totalPages}
                            </span>
                            <button 
                                className="cc-btn"
                                disabled={currentPage === totalPages}
                                onClick={() => setCurrentPage(p => p + 1)}
                                style={{ padding: '0.5rem 1rem' }}
                            >
                                Sonraki
                            </button>
                        </div>
                    )}
                </>
            )}
        </div>
        
        {/* RIGHT PANEL - LINEUP PITCH & BENCH */}
        <div style={{ width: '400px', flexShrink: 0, position: 'sticky', top: '2rem', display: 'flex', flexDirection: 'column', gap: '1rem' }}>
            
            {/* PITCH */}
            <div className="cc-card" style={{ padding: '1.5rem 1rem' }}>
                <h3 style={{ borderBottom: '1px solid rgba(255,255,255,0.1)', paddingBottom: '0.8rem', marginBottom: '1rem', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <span>İlk 11</span>
                    <span style={{ color: rosterCount >= MAX_ROSTER_SIZE ? 'var(--danger)' : 'var(--accent)', fontSize: '1rem' }}>{rosterCount}/{MAX_ROSTER_SIZE} Seçim</span>
                </h3>
                
                <div className="pitch-container">
                    <div className="pitch-lines"></div>
                    <div className="penalty-box-top"></div>
                    <div className="penalty-box-bottom"></div>
                    
                    {FORMATION_SLOTS.map(slot => {
                        const filledPlayerId = lineup[slot.id];
                        const player = pool.find(p => p.playerId === filledPlayerId);
                        
                        return (
                            <div 
                                key={slot.id}
                                className={`pitch-slot ${player ? 'filled' : ''}`}
                                style={{ top: slot.top, left: slot.left }}
                                onDragOver={(e) => {
                                    e.preventDefault();
                                    e.currentTarget.classList.add('drag-over');
                                }}
                                onDragLeave={(e) => {
                                    e.currentTarget.classList.remove('drag-over');
                                }}
                                onDrop={(e) => handleDropToSlot(e, slot.id)}
                            >
                                {player ? (
                                    <div 
                                        draggable
                                        onDragStart={(e) => handleDragStart(e, player.playerId)}
                                        className="player-draggable"
                                        style={{ width: '100%', height: '100%', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center' }}
                                    >
                                        <div style={{ color: 'var(--accent)', fontWeight: '900', fontSize: '1.2rem', lineHeight: '1' }}>{player.overall}</div>
                                        <div style={{ fontSize: '0.75rem', fontWeight: 'bold', textOverflow: 'ellipsis', overflow: 'hidden', whiteSpace: 'nowrap', width: '90%', textAlign: 'center', marginTop: '2px' }}>
                                            {player.name.split(' ').pop()}
                                        </div>
                                        <div className="slot-label" style={{ marginTop: '2px', opacity: 0.8 }}>{slot.label}</div>
                                    </div>
                                ) : (
                                    <span className="slot-label">{slot.label}</span>
                                )}
                            </div>
                        );
                    })}
                </div>
            </div>

            {/* BENCH */}
            <div 
                className="cc-card" 
                style={{ padding: '1rem', minHeight: '150px' }}
                onDragOver={(e) => e.preventDefault()}
                onDrop={handleDropToBench}
            >
                <h3 style={{ borderBottom: '1px solid rgba(255,255,255,0.1)', paddingBottom: '0.8rem', marginBottom: '1rem' }}>
                    Yedekler / Atanmamış
                </h3>
                
                {rosterCount === 0 ? (
                    <p style={{ color: 'var(--text-secondary)', textAlign: 'center', padding: '2rem 0', fontSize: '0.9rem' }}>
                        Henüz hiç oyuncu seçmedin.
                    </p>
                ) : (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem', maxHeight: '300px', overflowY: 'auto', paddingRight: '0.5rem' }}>
                        {myPicks.map((pick) => {
                            const isPlaced = Object.values(lineup).includes(pick.playerId);
                            if (isPlaced) return null; // Only show players not in the lineup
                            
                            const player = pool.find(p => p.playerId === pick.playerId);
                            if (!player) return null;
                            
                            return (
                                <div 
                                    key={pick.playerId}
                                    draggable
                                    onDragStart={(e) => handleDragStart(e, player.playerId)}
                                    className="player-draggable"
                                    style={{ 
                                        display: 'flex', 
                                        justifyContent: 'space-between',
                                        alignItems: 'center',
                                        padding: '0.6rem', 
                                        backgroundColor: 'rgba(255,255,255,0.03)',
                                        borderRadius: '6px',
                                        borderLeft: `3px solid var(--accent)`
                                    }}
                                >
                                    <div style={{ display: 'flex', flexDirection: 'column' }}>
                                        <span style={{ fontWeight: 'bold', fontSize: '0.9rem' }}>{player.name}</span>
                                        <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>{player.position}</span>
                                    </div>
                                    <div style={{ 
                                        backgroundColor: 'rgba(0,0,0,0.5)', 
                                        padding: '0.2rem 0.5rem', 
                                        borderRadius: '4px',
                                        fontWeight: 'bold',
                                        color: 'var(--accent)'
                                    }}>
                                        {player.overall}
                                    </div>
                                </div>
                            );
                        })}
                        
                        {myPicks.length > 0 && myPicks.every(pick => Object.values(lineup).includes(pick.playerId)) && (
                            <p style={{ color: 'var(--text-secondary)', textAlign: 'center', padding: '1rem 0', fontSize: '0.85rem' }}>
                                Tüm oyuncular sahada.
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

const SeasonDashboard = () => {
  const { roomId } = useParams();
  return (
    <div style={{ padding: '2rem', maxWidth: '1000px', margin: '0 auto' }}>
      <div className="cc-card">
        <h2>Sezon Dashboard</h2>
        <p style={{ color: 'var(--text-secondary)', marginTop: '1rem' }}>Haftalık kararlarınızı verin.</p>
      </div>
    </div>
  );
};

const Sponsorship = () => {
  const { roomId } = useParams();
  return (
    <div style={{ padding: '2rem', maxWidth: '800px', margin: '0 auto' }}>
      <div className="cc-card">
        <h2>Sponsorluk Teklifleri</h2>
        <p style={{ color: 'var(--text-secondary)', marginTop: '1rem' }}>Gelen teklifleri değerlendirin.</p>
      </div>
    </div>
  );
};

const Summary = () => {
  const { roomId } = useParams();
  return (
    <div style={{ padding: '2rem', maxWidth: '800px', margin: '0 auto' }}>
      <div className="cc-card">
        <h2>Sezon Sonu Özeti</h2>
        <p style={{ color: 'var(--text-secondary)', marginTop: '1rem' }}>Kazananlar ve istatistikler.</p>
      </div>
    </div>
  );
};

const Navigation = () => {
  const location = useLocation();
  const isActive = (path: string) => location.pathname.startsWith(path);

  const linkStyle = (path: string) => ({
    color: isActive(path) && path !== '/' ? 'var(--accent)' : 'var(--text-primary)',
    fontWeight: isActive(path) && path !== '/' ? '600' : '400',
    borderBottom: isActive(path) && path !== '/' ? '2px solid var(--accent)' : '2px solid transparent',
    paddingBottom: '0.25rem',
    transition: 'all 0.2s',
    fontFamily: 'Rajdhani, sans-serif',
    fontSize: '1.2rem',
    textTransform: 'uppercase' as const,
    letterSpacing: '1px'
  });

  return (
    <nav style={{ backgroundColor: 'var(--bg-card)', padding: '1rem 2rem', borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', maxWidth: '1200px', margin: '0 auto' }}>
        <Link to="/" style={{ fontSize: '1.5rem', fontWeight: 'bold', fontFamily: 'Rajdhani, sans-serif' }}>
          CLUB<span style={{ color: 'var(--accent)' }}>CRAFT</span>
        </Link>
        <ul style={{ display: 'flex', gap: '2rem', listStyle: 'none', margin: 0, padding: 0 }}>
          <li><Link to="/lobby/TIGER42" style={linkStyle('/lobby')}>Lobi</Link></li>
          <li><Link to="/draft/TIGER42" style={linkStyle('/draft')}>Draft</Link></li>
          <li><Link to="/season/TIGER42" style={linkStyle('/season')}>Sezon</Link></li>
          <li><Link to="/sponsorship/TIGER42" style={linkStyle('/sponsorship')}>Sponsorluk</Link></li>
          <li><Link to="/summary/TIGER42" style={linkStyle('/summary')}>Özet</Link></li>
        </ul>
      </div>
    </nav>
  );
};

function App() {
  return (
    <BrowserRouter>
      <Navigation />
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/lobby/:roomId" element={<Lobby />} />
        <Route path="/draft/:roomId" element={<Draft />} />
        <Route path="/season/:roomId" element={<SeasonDashboard />} />
        <Route path="/sponsorship/:roomId" element={<Sponsorship />} />
        <Route path="/summary/:roomId" element={<Summary />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
