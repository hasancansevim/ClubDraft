using ClubCraft.BuildingBlocks.Contracts.Events;
using ClubCraft.MatchEngine.Application.Repositories;
using MassTransit;

namespace ClubCraft.MatchEngine.Application.Consumers;

public class PlayerRemovedFromRosterCommandConsumer : IConsumer<IPlayerRemovedFromRosterEvent>
{
    private readonly IClubPowerRatingRepository _powerRepository;

    public PlayerRemovedFromRosterCommandConsumer(IClubPowerRatingRepository powerRepository)
    {
        _powerRepository = powerRepository;
    }

    public async Task Consume(ConsumeContext<IPlayerRemovedFromRosterEvent> context)
    {
        var msg = context.Message;

        var power = await _powerRepository.GetByIdAsync(msg.ClubId, context.CancellationToken);
        if (power == null)
            return;

        // ClubPowerRating artik roster'i per-player snapshot (PlayerId, Overall,
        // Position) olarak tuttugu icin PlayerId ile dogrudan cikartabiliyoruz —
        // eskiden burada "Overall'i bilmiyoruz" diye yorumlanan kisitlama artik yok.
        power.RemovePlayer(msg.PlayerId);

        await _powerRepository.SaveAsync(power, context.CancellationToken);
    }
}
