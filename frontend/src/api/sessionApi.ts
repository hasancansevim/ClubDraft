import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5056';

const api = axios.create({
  baseURL: API_BASE_URL,
});

export interface Participant {
  id: string;
  userId: string;
  clubName: string;
  isReady: boolean;
  clubId?: string;
}

export interface GameRoom {
  id: string;
  shortCode: string;
  status: number;
  participants: Participant[];
}

export interface CreateRoomResponse {
  roomId: string;
  status: string;
  shortCode?: string;
}

export interface JoinRoomResponse {
  participantId: string;
}

export const sessionApi = {
  createRoom: async (hostUserId: string, maxParticipants: number = 6) => {
    const response = await api.post<CreateRoomResponse>('/api/sessions', { hostUserId, maxParticipants });
    return response.data;
  },

  getRoomByCode: async (shortCode: string) => {
    const response = await api.get<GameRoom>(`/api/sessions/by-code/${shortCode}`);
    return response.data;
  },

  getRoom: async (roomId: string) => {
    const response = await api.get<GameRoom>(`/api/sessions/${roomId}`);
    return response.data;
  },

  joinRoom: async (roomId: string, userId: string, clubName: string) => {
    const response = await api.post<JoinRoomResponse>(`/api/sessions/${roomId}/join`, { userId, clubName });
    return response.data;
  },

  markReady: async (roomId: string, participantId: string, phase: 'Draft' | 'WeekAdvance') => {
    const response = await api.post<{ allReady: boolean }>(`/api/sessions/${roomId}/ready`, { participantId, phase });
    return response.data;
  }
};
