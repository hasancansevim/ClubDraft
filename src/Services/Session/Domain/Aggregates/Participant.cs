using System;

namespace ClubCraft.Session.Domain.Aggregates
{
    public class Participant
    {
        public Guid Id { get; private set; }
        public string UserId { get; private set; }
        public string ClubName { get; private set; }
        public bool IsReady { get; set; }
        public Guid? ClubId { get; private set; }

        private Participant() { }

        public Participant(Guid id, string userId, string clubName)
        {
            Id = id;
            UserId = userId;
            ClubName = clubName;
            IsReady = false;
        }

        public void AssignClub(Guid clubId)
        {
            ClubId = clubId;
        }
    }
}
