using ClubCraft.Draft.Domain.Aggregates;

namespace ClubCraft.Draft.Application.Repositories;

public interface IDraftSessionRepository
{
    Task<DraftSession?> GetByIdAsync(Guid draftSessionId, CancellationToken cancellationToken = default);
    Task AddAsync(DraftSession draftSession, CancellationToken cancellationToken = default);
    Task SaveAsync(DraftSession draftSession, CancellationToken cancellationToken = default);
}
