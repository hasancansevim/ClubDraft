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

// Pick tipini backend GetState response'uyla birebir eşleştir (camelCase)
export interface DraftPick {
  pickNumber: number;
  clubId: string;
  playerId: string;
  claimedAt: string;
}

// Backend /state endpoint'inin tam response'u
export interface DraftState {
  id: string;
  roomId: string;
  status: string;
  currentPickIndex: number;
  currentClubId: string | null;
  turnOrder: string[];
  picks: DraftPick[];
}

// Hem 404 hem de 5xx (502, 503, 504 gibi gateway hataları) için retry
const fetchWithRetry = async <T>(
  url: string,
  retries = 6,
  delay = 500
): Promise<T> => {
  try {
    const response = await axios.get(url);
    return response.data;
  } catch (err: any) {
    const status = err.response?.status;
    const isRetryable = status === 404 || status === 502 || status === 503 || status === 504 || !status;
    if (retries > 0 && isRetryable) {
      console.warn(`[draftApi] ${status ?? 'network'} error for ${url}, retrying in ${delay}ms... (${retries} left)`);
      await new Promise(resolve => setTimeout(resolve, delay));
      return fetchWithRetry<T>(url, retries - 1, delay + 500);
    }
    throw err;
  }
};

export const draftApi = {
  getPool: async (draftSessionId: string): Promise<Player[]> => {
    return await fetchWithRetry<Player[]>(`${API_BASE_URL}/api/draft-sessions/${draftSessionId}/pool`);
  },

  getState: async (draftSessionId: string): Promise<DraftState> => {
    return await fetchWithRetry<DraftState>(`${API_BASE_URL}/api/draft-sessions/${draftSessionId}/state`);
  },

  // POST — idempotency olmadığı için retry YOK
  claimPlayer: async (draftSessionId: string, clubId: string, playerId: string) => {
    const response = await axios.post(`${API_BASE_URL}/api/draft-sessions/${draftSessionId}/claim`, {
      clubId,
      playerId
    });
    return response.data;
  }
};
