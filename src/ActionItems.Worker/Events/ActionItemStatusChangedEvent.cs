namespace ActionItems.Worker.Events;

public sealed record ActionItemStatusChangedEvent(
    Guid WorkAreaId,
    Guid ActionItemId,
    string NewStatus);
