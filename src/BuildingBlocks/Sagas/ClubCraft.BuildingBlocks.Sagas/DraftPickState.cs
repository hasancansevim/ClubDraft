using MassTransit;

namespace ClubCraft.BuildingBlocks.Sagas;

public class DraftPickState : SagaStateMachineInstance, ISagaVersion
{
    public Guid CorrelationId { get; set; } // This will be PickAttemptId
    public int CurrentState { get; set; }
    public int Version { get; set; }

    public Guid DraftSessionId { get; set; }
    public Guid ClubId { get; set; }
    public Guid PlayerId { get; set; }

    public Guid? TimeoutTokenId { get; set; }
}
