using ClubCraft.FinanceSponsorship.Domain.Events;

namespace ClubCraft.FinanceSponsorship.Domain.Aggregates;

public class SponsorshipOffer
{
    public Guid Id { get; private set; }
    public Guid ClubId { get; private set; }
    public int ThresholdReached { get; private set; }
    public decimal Amount { get; private set; }
    public OfferStatus Status { get; private set; }
    public DateTime OfferedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    private readonly List<object> _domainEvents = new();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    private SponsorshipOffer() { } // EF Core

    public SponsorshipOffer(Guid id, Guid clubId, int thresholdReached, decimal amount, DateTime offeredAt, DateTime expiresAt)
    {
        Id = id;
        ClubId = clubId;
        ThresholdReached = thresholdReached;
        Amount = amount;
        Status = OfferStatus.Pending;
        OfferedAt = offeredAt;
        ExpiresAt = expiresAt;
    }

    public void Accept()
    {
        if (ExpiresAt < DateTime.UtcNow)
        {
            Status = OfferStatus.Expired;
            throw new InvalidOperationException("Offer has expired.");
        }

        if (Status != OfferStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot accept offer in status {Status}.");
        }

        Status = OfferStatus.Accepted;
        AddDomainEvent(new SponsorshipAcceptedEvent(ClubId, Amount));
    }

    public void Reject()
    {
        if (ExpiresAt < DateTime.UtcNow)
        {
            Status = OfferStatus.Expired;
            throw new InvalidOperationException("Offer has expired.");
        }

        if (Status != OfferStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot reject offer in status {Status}.");
        }

        Status = OfferStatus.Rejected;
    }

    public void Expire()
    {
        if (Status == OfferStatus.Pending && ExpiresAt < DateTime.UtcNow)
        {
            Status = OfferStatus.Expired;
        }
    }

    private void AddDomainEvent(object domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
