import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5056';

export interface Player {
  playerId: string;
  name: string;
  position: string;
  overall: number;
  age: number;
  marketValue: number;
  isClaimed: boolean;
}

export interface DraftState {
  currentPickIndex: number;
  currentClubId: string | null;
  picks: any[];
}

const fetchWithRetry = async <T>(url: string, retries = 6, delay = 500): Promise<T> => {
  try {
    const response = await axios.get(url);
    return response.data;
  } catch (err: any) {
    if (retries > 0 && err.response && err.response.status === 404) {
      console.log(`404 received for ${url}, retrying in ${delay}ms... (${retries} left)`);
      await new Promise(resolve => setTimeout(resolve, delay));
      return fetchWithRetry<T>(url, retries - 1, delay + 500);
    }
    throw err;
  }
};

export const draftApi = {
  getPool: async (draftSessionId: string) => {
    return await fetchWithRetry<Player[]>(`${API_BASE_URL}/api/draft-sessions/${draftSessionId}/pool`);
  },

  getState: async (draftSessionId: string) => {
    return await fetchWithRetry<DraftState>(`${API_BASE_URL}/api/draft-sessions/${draftSessionId}/state`);
  },

  claimPlayer: async (draftSessionId: string, clubId: string, playerId: string) => {
    const response = await axios.post(`${API_BASE_URL}/api/draft-sessions/${draftSessionId}/claim`, {
      clubId,
      playerId
    });
    return response.data;
  }
};
