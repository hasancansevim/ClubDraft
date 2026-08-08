using ClubCraft.BuildingBlocks.Contracts.Events;
using ClubCraft.Draft.Application.Commands.StartDraft;
using MassTransit;
using MediatR;

namespace ClubCraft.Draft.API.Consumers;

public class AllParticipantsReadyForDraftEventConsumer : IConsumer<IAllParticipantsReadyForDraftEvent>
{
    private readonly IMediator _mediator;

    public AllParticipantsReadyForDraftEventConsumer(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<IAllParticipantsReadyForDraftEvent> context)
    {
        var message = context.Message;
        
        // Snake draft algoritması: 1-2-3-4 -> 4-3-2-1 -> 1-2-3-4
        // Draft'ta seçilecek oyuncu sayısı ClubManagement'taki MaxRosterSize (20) ile eşleştirildi.
        int totalRounds = 20;
        var baseOrder = message.ParticipantClubIds.ToList();
        
        // Randomize initial order to make it fair
        var rng = new Random(message.RoomId.GetHashCode()); 
        baseOrder = baseOrder.OrderBy(x => rng.Next()).ToList();

        var snakeOrder = new List<Guid>();
        for (int i = 0; i < totalRounds; i++)
        {
            if (i % 2 == 0)
            {
                // Çift turlar: İleri
                snakeOrder.AddRange(baseOrder);
            }
            else
            {
                // Tek turlar: Geri
                var reversed = baseOrder.ToList();
                reversed.Reverse();
                snakeOrder.AddRange(reversed);
            }
        }

        var command = new StartDraftCommand
        {
            DraftSessionId = message.RoomId,
            TurnOrder = snakeOrder
        };

        await _mediator.Send(command);
    }
}
