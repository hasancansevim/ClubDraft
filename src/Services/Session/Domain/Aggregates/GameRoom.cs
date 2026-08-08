using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ClubCraft.Session.Domain.Events;
using ClubCraft.BuildingBlocks.Common.SeedWork;

namespace ClubCraft.Session.Domain.Aggregates
{
    public class GameRoom : AggregateRoot<Guid>
    {
        public string HostUserId { get; private set; } = string.Empty;
        public string ShortCode { get; private set; } = string.Empty;
        public RoomStatus Status { get; private set; }
        public int CurrentWeek { get; private set; }
        public int MaxParticipants { get; private set; }
        
        private readonly List<Participant> _participants = new();
        public IReadOnlyCollection<Participant> Participants => _participants.AsReadOnly();

        public uint Version { get; set; }
        
        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        private GameRoom() { }

        public static GameRoom Create(string hostUserId, int maxParticipants)
        {
            var room = new GameRoom
            {
                Id = Guid.NewGuid(),
                HostUserId = hostUserId,
                ShortCode = GenerateShortCode(),
                Status = RoomStatus.Lobby,
                CurrentWeek = 0,
                MaxParticipants = maxParticipants
            };
            
            room.AddDomainEvent(new RoomCreatedEvent(room.Id, hostUserId));
            return room;
        }

        private static string GenerateShortCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 6)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public Guid Join(string userId, string clubName)
        {
            if (_participants.Count >= MaxParticipants)
                throw new InvalidOperationException("Room is full");

            if (_participants.Any(p => p.UserId == userId))
                throw new InvalidOperationException("User already in room");

            var participant = new Participant(Guid.NewGuid(), userId, clubName);
            _participants.Add(participant);
            
            AddDomainEvent(new ParticipantJoinedEvent(Id, participant.Id, userId, clubName));
            return participant.Id;
        }

        public void MarkReady(Guid participantId, string phase)
        {
            var participant = _participants.FirstOrDefault(p => p.Id == participantId);
            if (participant == null)
                throw new InvalidOperationException("Participant not found");

            participant.IsReady = true;
            
            AddDomainEvent(new ParticipantReadyEvent(Id, participantId, phase));

            if (_participants.All(p => p.IsReady))
            {
                if (phase == "Draft")
                {
                    AddDomainEvent(new AllParticipantsReadyForDraftEvent(Id, _participants.Select(p => p.Id)));
                    AdvanceToDraft();
                }
                else if (phase == "WeekAdvance")
                {
                    AddDomainEvent(new AllParticipantsReadyForNextWeekEvent(Id, CurrentWeek));
                }
            }
        }


        public void AdvanceToDraft()
        {
            Status = RoomStatus.DraftPhase;
            ResetReadyStatus();
        }

        public void AdvanceToSeason()
        {
            Status = RoomStatus.SeasonPhase;
            CurrentWeek = 1;
            ResetReadyStatus();
        }

        public void AdvanceWeek()
        {
            CurrentWeek++;
            ResetReadyStatus();
        }

        private void ResetReadyStatus()
        {
            foreach (var participant in _participants)
            {
                participant.IsReady = false;
            }
        }
    }
}
