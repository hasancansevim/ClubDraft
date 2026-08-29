using ClubCraft.BuildingBlocks.Common.Enums;

namespace ClubCraft.BuildingBlocks.Contracts.Events;

public interface IPlayerAddedToRosterEvent
{
    Guid PickAttemptId { get; }
    Guid ClubId { get; }
    Guid RoomId { get; }
    Guid PlayerId { get; }
    int Overall { get; }
    PlayerPosition Position { get; }
}

public interface IPlayerRemovedFromRosterEvent
{
    Guid ClubId { get; }
    Guid PlayerId { get; }
}

/// <summary>
/// Club.LineupJson/Club.Formation (ClubManagement'ta yasiyor) degistiginde
/// yayinlanir — MatchEngine'in Ilk 11/pozisyon uyumu hesaplayabilmesi icin
/// tek veri kaynagi budur (MatchEngine'e senkron sorgu atilmiyor, bkz.
/// spec.md §4.6). Slots: slot ID (orn. "CB1") -> o slota atanmis oyuncunun
/// PlayerId'si (bos slot icin null). Slot ID -> gerekli pozisyon eslemesi
/// MatchEngine tarafinda FormationCatalog ile cozulur.
/// </summary>
public interface ILineupUpdatedEvent
{
    Guid ClubId { get; }
    Guid RoomId { get; }
    string Formation { get; }
    Dictionary<string, Guid?> Slots { get; }
}

public interface IPlayerRosterAdditionFailedEvent
{
    Guid PickAttemptId { get; }
    Guid ClubId { get; }
    Guid PlayerId { get; }
    string Reason { get; }
}

public interface IWeeklyDecisionMadeEvent
{
    Guid ClubId { get; }
    int Week { get; }
    int Type { get; } // Enum as int
    decimal Cost { get; }
}

public interface IClubInitializedEvent
{
    Guid ParticipantId { get; }
    Guid ClubId { get; }
    Guid RoomId { get; }
}
