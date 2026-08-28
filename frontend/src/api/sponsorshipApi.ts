import axios from 'axios';

const API_BASE = 'http://localhost:5056/api';

// Backend enum ClubCraft.FinanceSponsorship.Domain.Aggregates.OfferStatus
// (WeeklyDecisionType'da oldugu gibi int olarak serialize ediliyor)
export const OfferStatus = {
  Pending: 0,
  Accepted: 1,
  Rejected: 2,
  Expired: 3,
} as const;

export interface SponsorshipOffer {
  id: string;
  clubId: string;
  thresholdReached: number;
  amount: number;
  status: number;
  offeredAt: string;
  expiresAt: string;
}

export const sponsorshipApi = {
  getOffers: async (clubId: string): Promise<SponsorshipOffer[]> => {
    const res = await axios.get(`${API_BASE}/finances/${clubId}/offers`, { withCredentials: true });
    return res.data;
  },

  respond: async (clubId: string, offerId: string, response: 'Accept' | 'Reject'): Promise<SponsorshipOffer> => {
    const res = await axios.post(
      `${API_BASE}/finances/${clubId}/offers/${offerId}/respond`,
      { response },
      { withCredentials: true }
    );
    return res.data;
  }
};
