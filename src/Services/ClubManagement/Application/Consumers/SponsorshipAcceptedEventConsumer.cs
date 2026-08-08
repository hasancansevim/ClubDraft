using ClubCraft.BuildingBlocks.Contracts.Events;
using ClubCraft.ClubManagement.Application.Repositories;
using MassTransit;

namespace ClubCraft.ClubManagement.Application.Consumers;

public class SponsorshipAcceptedEventConsumer : IConsumer<ISponsorshipAcceptedEvent>
{
    private readonly IClubRepository _clubRepository;

    public SponsorshipAcceptedEventConsumer(IClubRepository clubRepository)
    {
        _clubRepository = clubRepository;
    }

    public async Task Consume(ConsumeContext<ISponsorshipAcceptedEvent> context)
    {
        var message = context.Message;

        var club = await _clubRepository.GetByIdAsync(message.ClubId);
        if (club == null)
        {
            return; // Club might have been deleted, or it's an invalid ID
        }

        club.ReceiveSponsorship(message.Amount);

        await _clubRepository.SaveAsync(club);
    }
}
