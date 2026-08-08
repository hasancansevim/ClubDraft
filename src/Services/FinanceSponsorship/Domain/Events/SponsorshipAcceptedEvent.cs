using ClubCraft.BuildingBlocks.Common.SeedWork;
using ClubCraft.BuildingBlocks.Contracts.Events;

namespace ClubCraft.FinanceSponsorship.Domain.Events;

public record SponsorshipAcceptedEvent(Guid ClubId, decimal Amount) : ISponsorshipAcceptedEvent;
