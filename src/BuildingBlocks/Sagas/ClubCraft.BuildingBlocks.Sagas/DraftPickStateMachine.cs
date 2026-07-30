using MassTransit;
using ClubCraft.BuildingBlocks.Contracts.Events;
using ClubCraft.BuildingBlocks.Contracts.Commands;

namespace ClubCraft.BuildingBlocks.Sagas;

public class DraftPickStateMachine : MassTransitStateMachine<DraftPickState>
{
    public State AddingToRoster { get; private set; } = null!;
    public State RevertingDraftClaim { get; private set; } = null!;

    public Event<IPlayerClaimedEvent> PlayerClaimed { get; private set; } = null!;
    public Event<IPlayerAddedToRosterEvent> PlayerAddedToRoster { get; private set; } = null!;
    public Event<IPlayerRosterAdditionFailedEvent> PlayerRosterAdditionFailed { get; private set; } = null!;
    public Event<IPlayerClaimRevertedEvent> PlayerClaimReverted { get; private set; } = null!;
    public Schedule<DraftPickState, IPickTimeoutEvent> PickTimeout { get; private set; } = null!;

    public DraftPickStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => PlayerClaimed, x => x.CorrelateById(context => context.Message.PickAttemptId));
        Event(() => PlayerAddedToRoster, x => x.CorrelateById(context => context.Message.PickAttemptId));
        Event(() => PlayerRosterAdditionFailed, x => x.CorrelateById(context => context.Message.PickAttemptId));
        Event(() => PlayerClaimReverted, x => x.CorrelateById(context => context.Message.PickAttemptId));

        Schedule(() => PickTimeout, instance => instance.TimeoutTokenId, s =>
        {
            s.Delay = TimeSpan.FromSeconds(30);
            s.Received = r => r.CorrelateById(context => context.Message.PickAttemptId);
        });

        Initially(
            When(PlayerClaimed)
                .Then(context =>
                {
                    context.Saga.DraftSessionId = context.Message.DraftSessionId;
                    context.Saga.ClubId = context.Message.ClubId;
                    context.Saga.PlayerId = context.Message.PlayerId;
                })
                .Schedule(PickTimeout, context => context.Init<IPickTimeoutEvent>(new { PickAttemptId = context.Saga.CorrelationId }))
                .SendAsync(new Uri("queue:club-management-commands"), context => context.Init<IAddPlayerToRosterCommand>(new
                {
                    PickAttemptId = context.Saga.CorrelationId,
                    ClubId = context.Saga.ClubId,
                    PlayerId = context.Saga.PlayerId,
                    Name = context.Message.Name,
                    Position = context.Message.Position,
                    Overall = context.Message.Overall,
                    Age = context.Message.Age,
                    MarketValue = context.Message.MarketValue
                }))
                .TransitionTo(AddingToRoster)
        );

        During(AddingToRoster,
            When(PlayerAddedToRoster)
                .Unschedule(PickTimeout)
                .Finalize(),

            When(PlayerRosterAdditionFailed)
                .Unschedule(PickTimeout)
                .SendAsync(new Uri("queue:draft-commands"), context => context.Init<IReleasePlayerClaimCommand>(new
                {
                    PickAttemptId = context.Saga.CorrelationId,
                    DraftSessionId = context.Saga.DraftSessionId,
                    PlayerId = context.Saga.PlayerId
                }))
                .TransitionTo(RevertingDraftClaim),

            When(PickTimeout.Received)
                .SendAsync(new Uri("queue:draft-commands"), context => context.Init<IReleasePlayerClaimCommand>(new
                {
                    PickAttemptId = context.Saga.CorrelationId,
                    DraftSessionId = context.Saga.DraftSessionId,
                    PlayerId = context.Saga.PlayerId
                }))
                .TransitionTo(RevertingDraftClaim)
        );

        During(RevertingDraftClaim,
            When(PlayerAddedToRoster) // Late success event
                .SendAsync(new Uri("queue:club-management-commands"), context => context.Init<IReleasePlayerFromRosterCommand>(new
                {
                    PickAttemptId = context.Saga.CorrelationId,
                    ClubId = context.Saga.ClubId,
                    PlayerId = context.Saga.PlayerId
                })),

            When(PlayerClaimReverted)
                .Finalize()
        );

        // Ignore late or duplicate events in Final state
        SetCompletedWhenFinalized();
    }
}
