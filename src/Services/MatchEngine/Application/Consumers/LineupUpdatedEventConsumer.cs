using ClubCraft.BuildingBlocks.Contracts.Events;
using ClubCraft.MatchEngine.Application.Repositories;
using ClubCraft.MatchEngine.Domain.Entities;
using MassTransit;

namespace ClubCraft.MatchEngine.Application.Consumers;

/// <summary>
/// ClubManagement'ta formasyon/lineup her degistiginde (Club.UpdateLineup /
/// Club.UpdateFormation) yayinlanan tek olay. MatchEngine'in "Ilk 11 hangi
/// slotta kim" bilgisine ulasabilecegi TEK yol budur — senkron sorgu yok
/// (bkz. spec.md §4.6).
/// </summary>
public class LineupUpdatedEventConsumer : IConsumer<ILineupUpdatedEvent>
{
    private readonly IClubPowerRatingRepository _powerRepository;

    public LineupUpdatedEventConsumer(IClubPowerRatingRepository powerRepository)
    {
        _powerRepository = powerRepository;
    }

    public async Task Consume(ConsumeContext<ILineupUpdatedEvent> context)
    {
        var msg = context.Message;

        var power = await _powerRepository.GetByIdAsync(msg.ClubId, context.CancellationToken);
        if (power == null)
        {
            // Lazily create if not exists (ayni desen: PlayerAddedToRosterCommandConsumer)
            power = new ClubPowerRating(msg.ClubId, msg.RoomId);
        }

        power.UpdateLineup(msg.Formation, msg.Slots);

        await _powerRepository.SaveAsync(power, context.CancellationToken);
    }
}
