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
  }, [shortCode]);

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
          isReady: false
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
            disabled={participants.find(p => p.id === myParticipantId)?.isReady}
          >
            {participants.find(p => p.id === myParticipantId)?.isReady ? 'Hazırsın ✓' : 'Hazırım!'}
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
  
  // Odanın gerçek kimliğini almak ve MyUserId vb. state yönetimi (Basit mock)
  const [myUserId] = useState(() => localStorage.getItem('myUserId') || crypto.randomUUID());
  useEffect(() => localStorage.setItem('myUserId', myUserId), [myUserId]);
  
  // RealRoomId and MyClubId mock for now (in real app, use Context or Redux)
  // We can fetch realRoomId from shortCode
  const [realRoomId, setRealRoomId] = useState<string>('');
  const [myParticipantId, setMyParticipantId] = useState<string | null>(null);
  
  useEffect(() => {
    const init = async () => {
      try {
        if (!shortCode) return;
        const participantId = localStorage.getItem(`joined_${shortCode}`);
        if (participantId) setMyParticipantId(participantId);

        const room = await sessionApi.getRoomByCode(shortCode);
        if (room && room.id) {
          setRealRoomId(room.id);
          // Draft Session ID is same as Room ID in this domain design
          setDraftSessionId(room.id);
          
          const poolData = await draftApi.getPool(room.id);
          setPool(poolData || []);
          
          const stateData = await draftApi.getState(room.id);
          setDraftState(stateData);
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
    }
  });

  const handleClaim = async (playerId: string) => {
    if (!draftSessionId || !myParticipantId) return;
    try {
      await draftApi.claimPlayer(draftSessionId, myParticipantId, playerId);
      
      // Update local state for immediate feedback
      setPool(prev => prev.map(p => 
        p.playerId === playerId ? { ...p, isClaimed: true } : p
      ));
    } catch (err: any) {
      console.error(err);
      const reason = err.response?.data?.reason || 'Bilinmeyen hata';
      alert(`Oyuncu seçilemedi: ${reason}`);
    }
  };

  if (loading) return <div style={{ textAlign: 'center', padding: '4rem' }}>Yükleniyor...</div>;
  if (error) return <div style={{ textAlign: 'center', padding: '4rem', color: 'var(--danger)' }}>{error}</div>;

  return (
    <div style={{ padding: '2rem', maxWidth: '1000px', margin: '0 auto' }}>
      <div className="cc-card" style={{ marginBottom: '2rem', textAlign: 'center' }}>
        <h2>Draft Odası</h2>
        <p style={{ color: 'var(--text-secondary)', marginTop: '1rem' }}>
          Sıra: <span style={{ color: 'var(--accent)', fontWeight: 'bold' }}>
            {draftState?.currentClubId === myParticipantId ? 'Sende!' : (draftState?.currentClubId ? 'Bekleniyor...' : 'Bilinmiyor')}
          </span>
        </p>
        <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem' }}>
          SignalR Bağlantısı: {isConnected ? <span style={{ color: 'var(--accent)' }}>Aktif</span> : <span style={{ color: 'var(--danger)' }}>Bağlanıyor...</span>}
        </p>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: '1rem' }}>
        {pool.map(player => (
          <div key={player.playerId} style={{ 
            backgroundColor: player.isClaimed ? 'rgba(0,0,0,0.5)' : 'var(--bg-card)',
            border: '1px solid rgba(255,255,255,0.05)',
            borderRadius: '8px',
            padding: '1rem',
            textAlign: 'center',
            opacity: player.isClaimed ? 0.5 : 1,
            position: 'relative'
          }}>
            <div style={{ fontSize: '1.5rem', fontWeight: 'bold', marginBottom: '0.5rem', color: 'var(--accent)' }}>
              {player.overall}
            </div>
            <h3 style={{ marginBottom: '0.5rem' }}>{player.name}</h3>
            <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem', marginBottom: '1rem' }}>
              {player.position} | Yaş: {player.age}
            </p>
            <button 
              className="cc-btn" 
              onClick={() => handleClaim(player.playerId)}
              disabled={player.isClaimed || draftState?.currentClubId !== myParticipantId}
              style={{ 
                width: '100%', 
                padding: '0.5rem',
                backgroundColor: player.isClaimed ? 'rgba(255,255,255,0.1)' : (draftState?.currentClubId !== myParticipantId ? 'rgba(255,255,255,0.05)' : 'var(--accent)'),
                color: player.isClaimed || draftState?.currentClubId !== myParticipantId ? 'var(--text-secondary)' : '#000',
                cursor: player.isClaimed || draftState?.currentClubId !== myParticipantId ? 'not-allowed' : 'pointer'
              }}
            >
              {player.isClaimed ? 'Seçildi' : 'Seç'}
            </button>
          </div>
        ))}
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
