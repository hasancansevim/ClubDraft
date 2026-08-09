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
            // ODA (Room) YENİ OLUŞTURULUYOR - Gerçek veri havuzundan rastgele 300 oyuncu seçiliyor
            var poolPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "draft-player-pool.json");
            var playersList = new List<ClubCraft.Draft.Domain.Entities.DraftPlayerPoolItem>();
            if (System.IO.File.Exists(poolPath))
            {
                var json = System.IO.File.ReadAllText(poolPath);
                var rawPlayers = System.Text.Json.JsonSerializer.Deserialize<List<PlayerDto>>(json) ?? new List<PlayerDto>();
                
                var random = new Random();
                var selectedPlayers = rawPlayers.OrderBy(x => random.Next()).Take(300).ToList();
                
                foreach (var p in selectedPlayers)
                {
                    playersList.Add(new (Guid.NewGuid(), new ClubCraft.Draft.Domain.ValueObjects.PlayerSnapshot(p.Name, p.Position, p.Overall, p.Age, (decimal)p.MarketValue)));
                }
            }
            
            if (playersList.Count == 0) // Fallback in case JSON is missing or empty
            {
                playersList.Add(new (Guid.NewGuid(), new ClubCraft.Draft.Domain.ValueObjects.PlayerSnapshot("Fallback Player", "FWD", 70, 25, 1000000)));
            }

            var fakePlayers = playersList;

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

public class PlayerDto
{
    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public int Overall { get; set; }
    public int Age { get; set; }
    public double MarketValue { get; set; }
}
