using ClubCraft.BuildingBlocks.Common.SeedWork;
using ClubCraft.ClubManagement.Domain.Enums;
using ClubCraft.ClubManagement.Domain.ValueObjects;

namespace ClubCraft.ClubManagement.Domain.Events;

public record ClubInitializedEvent(Guid ClubId, Guid RoomId, Guid PresidentUserId, string Name, decimal InitialBudget, Guid ParticipantId) : IDomainEvent { public DateTime OccurredOn { get; } = DateTime.UtcNow; }

public record PlayerAddedToRosterEvent(Guid ClubId, Guid RoomId, Guid PlayerId, int Overall, Guid PickAttemptId) : IDomainEvent { public DateTime OccurredOn { get; } = DateTime.UtcNow; }

public record PlayerRemovedFromRosterEvent(Guid ClubId, Guid PlayerId) : IDomainEvent { public DateTime OccurredOn { get; } = DateTime.UtcNow; }

public record PlayerRosterAdditionFailedEvent(Guid ClubId, Guid PlayerId, Guid PickAttemptId, string Reason) : IDomainEvent { public DateTime OccurredOn { get; } = DateTime.UtcNow; }

public record WeeklyDecisionMadeEvent(Guid ClubId, int Week, WeeklyDecisionType Type, decimal Cost) : IDomainEvent { public DateTime OccurredOn { get; } = DateTime.UtcNow; }

public record WeeklyDecisionRejectedEvent(Guid ClubId, int Week, WeeklyDecisionType Type, string Reason) : IDomainEvent { public DateTime OccurredOn { get; } = DateTime.UtcNow; }

public record BudgetDebitedEvent(Guid ClubId, decimal Amount, string Reason) : IDomainEvent { public DateTime OccurredOn { get; } = DateTime.UtcNow; }

public record BudgetCreditedEvent(Guid ClubId, decimal Amount, string Reason) : IDomainEvent { public DateTime OccurredOn { get; } = DateTime.UtcNow; }
