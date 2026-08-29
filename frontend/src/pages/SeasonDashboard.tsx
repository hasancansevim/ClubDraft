import React, { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { sessionApi } from '../api/sessionApi';
import { seasonApi, type ClubDetails, type TeamStanding, type FixtureMatch } from '../api/seasonApi';
import { toast } from '../App';
import { useSignalR } from '../hooks/useSignalR';
import { FORMATIONS, FORMATION_NAMES, POSITION_GROUP } from '../constants/formations';

// Shared overall color logic
const overallColor = (ov: number) => {
  if (ov >= 85) return '#FFD700';
  if (ov >= 80) return 'var(--pos-mid)';
  if (ov >= 75) return 'var(--info)';
  return 'var(--text-secondary)';
};

const PosBadge = ({ pos }: { pos: string }) => (
  <span className={`cc-pos-badge ${POSITION_GROUP[pos] || pos}`}>{pos}</span>
);

export const SeasonDashboard = () => {
  // URL param is the human-readable short code (e.g. "TIGER42"), NOT the real RoomId (GUID).
  // Every API call below needs the real RoomId, resolved once via getRoomByCode and cached in `realRoomId`.
  const { roomId: shortCode } = useParams();
  const [realRoomId, setRealRoomId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [clubId, setClubId] = useState<string | null>(null);
  const [currentWeek, setCurrentWeek] = useState<number>(1);
  const [clubDetails, setClubDetails] = useState<ClubDetails | null>(null);
  const [reputation, setReputation] = useState<number>(0);
  const [standings, setStandings] = useState<TeamStanding[]>([]);
  const [fixture, setFixture] = useState<FixtureMatch[]>([]);
  const [lineup, setLineup] = useState<Record<string, string | null>>({});
  const [formation, setFormation] = useState<string>('4-4-2');

  const [participants, setParticipants] = useState<any[]>([]);
  const [isReady, setIsReady] = useState(false);
  const [myParticipantId, setMyParticipantId] = useState<string | null>(null);
  const [myUserId, setMyUserId] = useState<string | null>(null);
  const [matchResult, setMatchResult] = useState<any | null>(null);

  useEffect(() => {
    const uid = localStorage.getItem("myUserId");
    if (uid) setMyUserId(uid);
    if (shortCode) {
      const pid = localStorage.getItem(`joined_${shortCode}`);
      if (pid) setMyParticipantId(pid);
    }
  }, [shortCode]);

  useSignalR({
    roomId: realRoomId || "",
    userId: myUserId || "",
    onMatchResult: (data) => {
      setMatchResult(data);
    },
    onWeekAdvanced: (data) => {
      toast("info", `Hafta ${data.week} simülasyonu tamamlandı!`);
      setIsReady(false);
      setMatchResult(null);
      fetchDashboardData();
    }
  });


  // Weekly decisions enum values based on backend:
  // HireCoach = 1, StadiumInvestment = 2, MoraleBonus = 3
  const WEEKLY_DECISIONS = [
    { type: 1, title: 'Antrenör Kirala', cost: 500000, desc: '+10 Taktik Gücü', icon: '👨‍💼' },
    { type: 2, title: 'Stadyum Yatırımı', cost: 2000000, desc: 'Gelir ve Taraftar Artışı', icon: '🏟️' },
    { type: 3, title: 'Moral Primi', cost: 100000, desc: 'Geçici Performans Artışı', icon: '🔥' }
  ];

  const fetchDashboardData = async () => {
    try {
      if (!shortCode) return;

      const myParticipantId = localStorage.getItem(`joined_${shortCode}`);
      if (!myParticipantId) {
        setError('Odaya katılım bilginiz bulunamadı.');
        setLoading(false);
        return;
      }

      // 1. Resolve the short code to the real RoomId (GUID) — every subsequent call needs this, not the short code.
      const room = await sessionApi.getRoomByCode(shortCode).catch(() => null) || await sessionApi.getRoom(shortCode);
      if (!room || !room.id) {
        setError('Oda bulunamadı.');
        setLoading(false);
        return;
      }
      setRealRoomId(room.id);

      setCurrentWeek(room.currentWeek || 1);

      const fullRoom = await sessionApi.getRoom(room.id);
      setParticipants(fullRoom.participants || []);
      const me = fullRoom.participants?.find(p => p.id === myParticipantId);
      if (me) setIsReady(me.isReady);
      const myP = fullRoom.participants?.find(p => p.id === myParticipantId);
      
      if (!myP || !myP.clubId) {
        setError('Kulüp ID bulunamadı. Draft tamamlanmamış olabilir.');
        setLoading(false);
        return;
      }
      
      const cId = myP.clubId;
      setClubId(cId);

      // 2. Fetch all other required data
      const [club, rep, stds, fix] = await Promise.all([
        seasonApi.getClub(cId),
        seasonApi.getReputation(cId).catch(() => 0),
        seasonApi.getStandings(room.id).catch(() => []),
        seasonApi.getFixture(room.id).catch(() => [])
      ]);
      setFixture(fix);

      setClubDetails(club);
      setReputation(rep);
      setFormation(club.formation || '4-4-2');
      try {
        const parsedLineup = club.lineupJson ? JSON.parse(club.lineupJson) : {};
        setLineup(parsedLineup);
      } catch (e) {
        setLineup({});
      }

      
      // If backend mapped name it would be in stds, else we map it here
      const mappedStandings = stds.map(s => {
        const matchingP = fullRoom.participants?.find(p => p.clubId === s.clubId);
        return {
          ...s,
          clubName: matchingP ? matchingP.clubName : 'Bilinmeyen Kulüp'
        };
      });
      setStandings(mappedStandings);
      
    } catch (err) {
      console.error(err);
      setError('Dashboard verileri yüklenirken hata oluştu.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDashboardData();
  }, [shortCode]);


  const handleReadyClick = async () => {
    if (!realRoomId || !myParticipantId) return;
    try {
      await sessionApi.markReady(realRoomId, myParticipantId, "WeekAdvance");
      setIsReady(true);
      toast("success", "Haftayı ilerletmek için hazır durumdasınız.");
    } catch (err) {
      toast("error", "Hazır durumu gönderilemedi.");
    }
  };

  const getClubName = (cId: string) => {
    const p = participants.find(x => x.clubId === cId);
    return p ? p.clubName : "Bilinmeyen";
  };

  const handleDecision = async (type: number) => {
    if (!clubId) return;
    try {
      await seasonApi.makeWeeklyDecision(clubId, currentWeek, type);
      toast('success', 'Karar başarıyla alındı!');
      // Refresh to update budget and decisions
      await fetchDashboardData();
    } catch (err: any) {
      const reason = err.response?.data || err.message;
      toast('error', `Karar alınamadı: ${reason}`);
    }
  };

  const handleDragStart = (e: React.DragEvent, playerId: string) => {
    e.dataTransfer.setData('playerId', playerId);
  };

  const handleFormationChange = async (newFormation: string) => {
    if (!clubId || newFormation === formation) return;
    const previous = formation;
    setFormation(newFormation);
    setLineup({}); // Backend de formasyon degisince lineup'i sifirliyor (bkz. Club.UpdateFormation) — UI'i onunla senkron tut.
    try {
      await seasonApi.updateFormation(clubId, newFormation);
      toast('success', `Formasyon ${newFormation} olarak değiştirildi. İlk 11'i yeniden dizmeniz gerekiyor.`);
    } catch (err) {
      setFormation(previous);
      toast('error', 'Formasyon değiştirilemedi!');
    }
  };

  const handleDropToSlot = async (e: React.DragEvent, slotId: string) => {
    e.preventDefault();
    e.currentTarget.classList.remove('drag-over');
    const playerId = e.dataTransfer.getData('playerId');
    if (!playerId || !clubId) return;
    
    const newLineup = { ...lineup };
    Object.keys(newLineup).forEach(k => { if (newLineup[k] === playerId) newLineup[k] = null; });
    newLineup[slotId] = playerId;
    setLineup(newLineup);
    
    try {
      await seasonApi.updateLineup(clubId, JSON.stringify(newLineup));
      toast('success', 'İlk 11 güncellendi.');
    } catch (err) {
      toast('error', 'Kadro kaydedilemedi!');
    }
  };

  const handleDropToBench = async (e: React.DragEvent) => {
    e.preventDefault();
    const playerId = e.dataTransfer.getData('playerId');
    if (!playerId || !clubId) return;
    
    const newLineup = { ...lineup };
    Object.keys(newLineup).forEach(k => { if (newLineup[k] === playerId) newLineup[k] = null; });
    setLineup(newLineup);
    
    try {
      await seasonApi.updateLineup(clubId, JSON.stringify(newLineup));
      toast('success', 'Oyuncu yedeğe çekildi.');
    } catch (err) {
      toast('error', 'Kadro kaydedilemedi!');
    }
  };

  if (loading) {
    return (
      <div className="cc-loader-overlay">
        <div className="cc-loader">
          <div className="cc-loader-ring-outer" />
          <div className="cc-loader-ring-inner" />
        </div>
        <span className="cc-loader-text">Sezon Yükleniyor...</span>
      </div>
    );
  }

  if (error || !clubDetails) {
    return (
      <div className="cc-error-state">
        <div style={{ fontSize: '3rem' }}>⚠</div>
        <p>{error}</p>
        <Link to="/" className="cc-btn">Ana Sayfaya Dön</Link>
      </div>
    );
  }

  // Find my league position
  const myPosition = standings.findIndex(s => s.clubId === clubId) + 1;

  // Render Roster view (readonly)
  const renderRoster = () => {
    const FORMATION_SLOTS = FORMATIONS[formation] || FORMATIONS['4-4-2'];

    // Convert array of roster to lookup map
    const rosterMap = Object.fromEntries(clubDetails.roster.map(p => [p.id, p]));

    return (
      <div style={{ display: 'flex', gap: '1.5rem' }}>
        {/* Pitch */}
        <div className="cc-card" style={{ padding: '1.25rem 1rem', width: '340px', flexShrink: 0 }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.85rem', borderBottom: '1px solid var(--border)', paddingBottom: '0.75rem' }}>
            <h3 style={{ fontSize: '1.1rem' }}>İlk 11</h3>
            <select
              value={formation}
              onChange={e => handleFormationChange(e.target.value)}
              className="cc-input"
              id="formation-select"
              style={{ fontSize: '0.8rem', padding: '0.3rem 0.5rem', width: 'auto' }}
            >
              {FORMATION_NAMES.map(f => <option key={f} value={f}>{f}</option>)}
            </select>
          </div>
          <div className="pitch-container">
            <div className="pitch-lines" />
            <div className="penalty-box-top" />
            <div className="penalty-box-bottom" />
            {FORMATION_SLOTS.map(slot => {
              const filledPlayerId = lineup[slot.id];
              const player = filledPlayerId ? rosterMap[filledPlayerId] : null;
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
                      onDragStart={e => handleDragStart(e, player.id)}
                      className="player-draggable"
                      style={{ width: '100%', height: '100%', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', cursor: 'grab' }}
                    >
                      <div style={{ fontFamily: 'var(--font-display)', color: overallColor(player.overall), fontWeight: 900, fontSize: '1rem', lineHeight: 1 }}>
                        {player.overall}
                      </div>
                      <div style={{ fontSize: '0.6rem', fontWeight: 700, overflow: 'hidden', whiteSpace: 'nowrap', width: '90%', textAlign: 'center', marginTop: '2px', color: 'var(--text-primary)' }}>
                        {player.name.split(' ').pop()}
                      </div>
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
          style={{ padding: '1.25rem 1rem', flex: 1 }}
          onDragOver={e => e.preventDefault()}
          onDrop={handleDropToBench}
        >
          <h3 style={{ fontSize: '1.1rem', marginBottom: '0.85rem', borderBottom: '1px solid var(--border)', paddingBottom: '0.75rem' }}>
            Kadro <span style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', fontWeight: 'normal' }}>(Yedeğe çekmek için buraya sürükle)</span>
          </h3>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.75rem' }}>
            {clubDetails.roster.map(player => {
              const isOnPitch = Object.values(lineup).includes(player.id);
              if (isOnPitch) return null;
              return (
                <div 
                  key={player.id} 
                  className="cc-bench-item" 
                  style={{ minWidth: '160px', flex: '1 1 calc(50% - 0.75rem)', cursor: 'grab' }}
                  draggable
                  onDragStart={e => handleDragStart(e, player.id)}
                >
                  <span className="cc-bench-overall" style={{ color: overallColor(player.overall) }}>{player.overall}</span>
                  <div style={{ flex: 1 }}>
                    <div style={{ fontWeight: 600, fontSize: '0.85rem' }}>{player.name}</div>
                    <div style={{ fontSize: '0.7rem', display: 'flex', gap: '0.5rem', alignItems: 'center', marginTop: '0.2rem' }}>
                      <PosBadge pos={player.position} />
                      <span style={{ color: 'var(--text-secondary)' }}>€{(player.marketValue / 1000000).toFixed(1)}M</span>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </div>
    );
  };

  return (
    <div className="cc-season-dashboard" style={{ padding: '1.5rem', maxWidth: '1200px', margin: '0 auto' }}>
      
      {/* 1. TOP SUMMARY BAR */}
      <div className="cc-summary-bar" style={{ display: 'flex', gap: '1rem', marginBottom: '1.5rem' }}>
        <div className="cc-card" style={{ flex: 1, padding: '1.25rem', display: 'flex', alignItems: 'center', gap: '1rem' }}>
          <div style={{ fontSize: '2.5rem' }}>💰</div>
          <div>
            <div style={{ color: 'var(--text-secondary)', fontSize: '0.85rem', textTransform: 'uppercase', letterSpacing: '1px' }}>Bütçe</div>
            <div style={{ fontSize: '1.5rem', fontFamily: 'var(--font-display)', color: 'var(--accent)' }}>
              €{clubDetails.budget.toLocaleString()}
            </div>
          </div>
        </div>
        
        <div className="cc-card" style={{ flex: 1, padding: '1.25rem', display: 'flex', alignItems: 'center', gap: '1rem' }}>
          <div style={{ fontSize: '2.5rem' }}>⭐</div>
          <div>
            <div style={{ color: 'var(--text-secondary)', fontSize: '0.85rem', textTransform: 'uppercase', letterSpacing: '1px' }}>İtibar Skoru</div>
            <div style={{ fontSize: '1.5rem', fontFamily: 'var(--font-display)' }}>
              {reputation}
            </div>
          </div>
        </div>

        <div className="cc-card" style={{ flex: 1, padding: '1.25rem', display: 'flex', alignItems: 'center', gap: '1rem' }}>
          <div style={{ fontSize: '2.5rem' }}>📅</div>
          <div>
            <div style={{ color: 'var(--text-secondary)', fontSize: '0.85rem', textTransform: 'uppercase', letterSpacing: '1px' }}>Hafta</div>
            <div style={{ fontSize: '1.5rem', fontFamily: 'var(--font-display)' }}>
              {currentWeek} / 14
            </div>
          </div>
        </div>

        <div className="cc-card" style={{ flex: 1, padding: '1.25rem', display: 'flex', alignItems: 'center', gap: '1rem' }}>
          <div style={{ fontSize: '2.5rem' }}>📈</div>
          <div>
            <div style={{ color: 'var(--text-secondary)', fontSize: '0.85rem', textTransform: 'uppercase', letterSpacing: '1px' }}>Lig Sırası</div>
            <div style={{ fontSize: '1.5rem', fontFamily: 'var(--font-display)', color: myPosition === 1 ? '#FFD700' : 'inherit' }}>
              {myPosition > 0 ? `${myPosition}.` : '-'}
            </div>
          </div>
        </div>
      </div>


      {matchResult && (
        <div className="cc-card" style={{ marginBottom: "1.5rem", padding: "1.5rem", background: "linear-gradient(135deg, rgba(var(--accent-rgb),0.1) 0%, rgba(13,40,24,1) 100%)", border: "1px solid var(--accent)", textAlign: "center" }}>
          <h2 style={{ fontSize: "1.2rem", color: "var(--accent)", marginBottom: "1rem" }}>Haftanın Maç Sonucu</h2>
          <div style={{ display: "flex", justifyContent: "center", alignItems: "center", gap: "2rem", fontSize: "2rem", fontFamily: "var(--font-display)" }}>
            <div style={{ flex: 1, textAlign: "right" }}>{getClubName(matchResult.homeClubId)}</div>
            <div style={{ padding: "0.5rem 1rem", background: "var(--bg-primary)", borderRadius: "8px" }}>
              {matchResult.homeScore} - {matchResult.awayScore}
            </div>
            <div style={{ flex: 1, textAlign: "left" }}>{getClubName(matchResult.awayClubId)}</div>
          </div>
        </div>
      )}

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 350px', gap: '1.5rem' }}>
        {/* LEFT COLUMN: Roster */}
        <div>
          {renderRoster()}
        </div>

        {/* RIGHT COLUMN: Weekly Decisions */}
        <div>
          <div className="cc-card" style={{ padding: '1.5rem' }}>
            <h3 style={{ fontSize: '1.2rem', marginBottom: '1.5rem', borderBottom: '1px solid var(--border)', paddingBottom: '0.75rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <span>📝</span> Haftalık Kararlar
            </h3>
            
            <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              {WEEKLY_DECISIONS.map(dec => {
                const alreadyTaken = clubDetails.weeklyDecisions.some(d => d.week === currentWeek && d.type === dec.type);
                const canAfford = clubDetails.budget >= dec.cost;

                return (
                  <div key={dec.type} style={{ border: '1px solid var(--border)', borderRadius: '8px', padding: '1rem', background: 'var(--bg-secondary)' }}>
                    <div style={{ display: 'flex', gap: '0.75rem', marginBottom: '0.75rem' }}>
                      <div style={{ fontSize: '2rem' }}>{dec.icon}</div>
                      <div>
                        <div style={{ fontWeight: '600' }}>{dec.title}</div>
                        <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', marginTop: '0.2rem' }}>{dec.desc}</div>
                      </div>
                    </div>
                    
                    <button
                      className="cc-btn"
                      style={{ width: '100%', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}
                      disabled={alreadyTaken || !canAfford}
                      onClick={() => handleDecision(dec.type)}
                    >
                      <span>{alreadyTaken ? 'Karar Alındı' : 'Kararı Uygula'}</span>
                      <span style={{ fontFamily: 'var(--font-display)' }}>€{dec.cost.toLocaleString()}</span>
                    </button>
                    
                    {!canAfford && !alreadyTaken && (
                      <div style={{ color: '#ff4a4a', fontSize: '0.75rem', textAlign: 'center', marginTop: '0.5rem' }}>Bütçe yetersiz</div>
                    )}
                  </div>
                );
              })}
            </div>
          </div>

          {/* Standings */}
          <div className="cc-card" style={{ padding: "1.5rem", marginTop: "1.5rem" }}>
            <h3 style={{ fontSize: "1.2rem", marginBottom: "1rem", borderBottom: "1px solid var(--border)", paddingBottom: "0.75rem", display: "flex", alignItems: "center", gap: "0.5rem" }}>
              <span>🏆</span> Lig Tablosu
            </h3>
            <table style={{ width: "100%", borderCollapse: "collapse", fontSize: "0.85rem" }}>
              <thead>
                <tr style={{ borderBottom: "1px solid var(--border)", color: "var(--text-secondary)", textAlign: "left" }}>
                  <th style={{ padding: "0.5rem" }}>Sıra</th>
                  <th style={{ padding: "0.5rem" }}>Takım</th>
                  <th style={{ padding: "0.5rem" }}>O</th>
                  <th style={{ padding: "0.5rem" }}>G</th>
                  <th style={{ padding: "0.5rem" }}>B</th>
                  <th style={{ padding: "0.5rem" }}>M</th>
                  <th style={{ padding: "0.5rem" }}>Av</th>
                  <th style={{ padding: "0.5rem" }}>P</th>
                </tr>
              </thead>
              <tbody>
                {standings.map((s: any, idx) => (
                  <tr key={s.clubId} style={{ borderBottom: "1px solid var(--bg-primary)", background: s.clubId === clubId ? "rgba(var(--accent-rgb),0.05)" : "transparent" }}>
                    <td style={{ padding: "0.5rem" }}>{idx + 1}</td>
                    <td style={{ padding: "0.5rem", fontWeight: "600" }}>{s.clubName || getClubName(s.clubId)}</td>
                    <td style={{ padding: "0.5rem" }}>{s.played}</td>
                    <td style={{ padding: "0.5rem" }}>{s.won}</td>
                    <td style={{ padding: "0.5rem" }}>{s.drawn}</td>
                    <td style={{ padding: "0.5rem" }}>{s.lost}</td>
                    <td style={{ padding: "0.5rem" }}>{s.goalDifference}</td>
                    <td style={{ padding: "0.5rem", fontWeight: "bold", color: "var(--accent)" }}>{s.points}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Maç Geçmişi */}
          <div className="cc-card" style={{ padding: "1.5rem", marginTop: "1.5rem" }}>
            <h3 style={{ fontSize: "1.2rem", marginBottom: "1rem", borderBottom: "1px solid var(--border)", paddingBottom: "0.75rem", display: "flex", alignItems: "center", gap: "0.5rem" }}>
              <span>📜</span> Maç Geçmişi
            </h3>
            {(() => {
              const myMatches = fixture
                .filter(m => m.isPlayed && (m.homeClubId === clubId || m.awayClubId === clubId))
                .sort((a, b) => b.week - a.week);
              if (myMatches.length === 0) {
                return <p style={{ color: 'var(--text-secondary)', fontSize: '0.85rem', textAlign: 'center', padding: '0.5rem 0' }}>Henüz oynanmış maçınız yok.</p>;
              }
              return (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                  {myMatches.map(m => {
                    const isHome = m.homeClubId === clubId;
                    const myScore = isHome ? m.homeScore : m.awayScore;
                    const oppScore = isHome ? m.awayScore : m.homeScore;
                    const oppId = isHome ? m.awayClubId : m.homeClubId;
                    const outcome = myScore > oppScore ? 'G' : myScore < oppScore ? 'M' : 'B';
                    const outcomeColor = outcome === 'G' ? 'var(--accent)' : outcome === 'M' ? '#ff4a4a' : 'var(--text-secondary)';
                    return (
                      <div key={m.id} style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', fontSize: '0.85rem', padding: '0.4rem 0' }}>
                        <span style={{ color: 'var(--text-secondary)', minWidth: '54px' }}>Hafta {m.week}</span>
                        <span style={{ flex: 1 }}>{isHome ? 'vs' : '@'} {getClubName(oppId)}</span>
                        <span style={{ fontFamily: 'var(--font-display)' }}>{myScore} - {oppScore}</span>
                        <span style={{ color: outcomeColor, fontWeight: 700, minWidth: '14px', textAlign: 'center' }}>{outcome}</span>
                      </div>
                    );
                  })}
                </div>
              );
            })()}
          </div>

          {/* Fikstür */}
          <div className="cc-card" style={{ padding: "1.5rem", marginTop: "1.5rem" }}>
            <h3 style={{ fontSize: "1.2rem", marginBottom: "1rem", borderBottom: "1px solid var(--border)", paddingBottom: "0.75rem", display: "flex", alignItems: "center", gap: "0.5rem" }}>
              <span>📅</span> Fikstür
            </h3>
            {(() => {
              const upcoming = fixture.filter(m => !m.isPlayed).sort((a, b) => a.week - b.week).slice(0, 10);
              if (upcoming.length === 0) {
                return <p style={{ color: 'var(--text-secondary)', fontSize: '0.85rem', textAlign: 'center', padding: '0.5rem 0' }}>Kalan maç yok.</p>;
              }
              return (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.4rem', maxHeight: '220px', overflowY: 'auto' }}>
                  {upcoming.map(m => (
                    <div key={m.id} style={{
                      display: 'flex', alignItems: 'center', gap: '0.75rem', fontSize: '0.85rem', padding: '0.35rem 0',
                      background: (m.homeClubId === clubId || m.awayClubId === clubId) ? 'rgba(var(--accent-rgb),0.05)' : 'transparent'
                    }}>
                      <span style={{ color: 'var(--text-secondary)', minWidth: '54px' }}>Hafta {m.week}</span>
                      <span style={{ flex: 1 }}>{getClubName(m.homeClubId)} <span style={{ color: 'var(--text-secondary)' }}>vs</span> {getClubName(m.awayClubId)}</span>
                    </div>
                  ))}
                </div>
              );
            })()}
          </div>

          {/* Ready Button */}
          <div className="cc-card" style={{ padding: "1.5rem", marginTop: "1.5rem", textAlign: "center" }}>
            <button 
              className="cc-btn" 
              style={{ width: "100%", padding: "1rem", fontSize: "1.2rem", background: isReady ? "var(--bg-secondary)" : "var(--accent)", color: isReady ? "var(--text-secondary)" : "#000" }}
              disabled={isReady}
              onClick={handleReadyClick}
            >
              {isReady ? "Bekleniyor..." : "Maça Çık / Hazırım"}
            </button>
          </div>

        </div>
      </div>

    </div>
  );
};
