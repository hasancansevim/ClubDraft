import axios from 'axios';

const API_BASE = 'http://localhost:5056/api';

export interface RosterPlayer {
  id: string;
  name: string;
  position: string;
  overall: number;
  age: number;
  marketValue: number;
}

export interface WeeklyDecision {
  week: number;
  type: number;
  cost: number;
}

export interface ClubDetails {
  id: string;
  name: string;
  budget: number;
  roster: RosterPlayer[];
  weeklyDecisions: WeeklyDecision[];
  lineupJson: string;
}

export interface TeamStanding {
  clubId: string;
  played: number;
  won: number;
  drawn: number;
  lost: number;
  goalsFor: number;
  goalsAgainst: number;
  goalDifference: number;
  points: number;
}

export const seasonApi = {
  getClub: async (clubId: string): Promise<ClubDetails> => {
    const res = await axios.get(`${API_BASE}/clubs/${clubId}`, { withCredentials: true });
    return res.data;
  },

  getReputation: async (clubId: string): Promise<number> => {
    const res = await axios.get(`${API_BASE}/reputation/${clubId}`, { withCredentials: true });
    return res.data.score;
  },

  getStandings: async (roomId: string): Promise<TeamStanding[]> => {
    const res = await axios.get(`${API_BASE}/matches/${roomId}/standings`, { withCredentials: true });
    return res.data;
  },

  makeWeeklyDecision: async (clubId: string, week: number, decisionType: number): Promise<{ cost: number }> => {
    const res = await axios.post(
      `${API_BASE}/clubs/${clubId}/weekly-decisions`,
      { clubId, week, type: decisionType },
      { withCredentials: true }
    );
    return res.data;
  },

  updateLineup: async (clubId: string, lineupJson: string): Promise<void> => {
    await axios.put(
      `${API_BASE}/clubs/${clubId}/lineup`,
      { clubId, lineupJson },
      { withCredentials: true }
    );
  }
};
