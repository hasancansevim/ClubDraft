using MediatR;
using ClubCraft.Draft.Application.Repositories;

namespace ClubCraft.Draft.Application.Commands.StartDraft;

public class StartDraftCommandHandler : IRequestHandler<StartDraftCommand, bool>
{
    private readonly IDraftSessionRepository _repository;

    public StartDraftCommandHandler(IDraftSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(StartDraftCommand request, CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdAsync(request.DraftSessionId, cancellationToken);
        
        if (session == null)
        {
            // ODA (Room) YENİ OLUŞTURULUYOR - Sadece Swagger'da test edebilmek için geçici olarak sahte oyuncularla oluşturuyoruz
            var fakePlayers = new List<ClubCraft.Draft.Domain.Entities.DraftPlayerPoolItem>
            {
                new (Guid.NewGuid(), new ClubCraft.Draft.Domain.ValueObjects.PlayerSnapshot("Lionel Messi", "RW", 90, 36, 50000000)),
                new (Guid.NewGuid(), new ClubCraft.Draft.Domain.ValueObjects.PlayerSnapshot("Cristiano Ronaldo", "ST", 88, 39, 30000000)),
                new (Guid.NewGuid(), new ClubCraft.Draft.Domain.ValueObjects.PlayerSnapshot("Kylian Mbappe", "LW", 92, 25, 180000000)),
                new (Guid.NewGuid(), new ClubCraft.Draft.Domain.ValueObjects.PlayerSnapshot("Erling Haaland", "ST", 91, 23, 180000000)),
                new (Guid.NewGuid(), new ClubCraft.Draft.Domain.ValueObjects.PlayerSnapshot("Jude Bellingham", "CM", 90, 20, 150000000))
            };

            // Not: Gerçekte DraftSession ID'si RoomId ile aynı tutulabilir veya Create komutu ile dışarıdan verilir. 
            // Burada testi kolaylaştırmak için RoomId = request.DraftSessionId yapıyoruz.
            session = new ClubCraft.Draft.Domain.Aggregates.DraftSession(request.DraftSessionId, fakePlayers);
            
            // Yeni oluşturduğumuz için ID'yi gelen request'teki ile zorla ezerek testin çalışmasını sağlıyoruz (Normalde constructor atıyor)
            var idProperty = typeof(ClubCraft.BuildingBlocks.Common.SeedWork.Entity<Guid>).GetProperty("Id");
            idProperty?.SetValue(session, request.DraftSessionId);

            await _repository.AddAsync(session, cancellationToken);
        }

        session.StartDraft(request.TurnOrder);

        await _repository.SaveAsync(session, cancellationToken);

        return true;
    }
}
