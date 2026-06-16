namespace ActionItems.Sdk.ActionItems.Entities;

public class ActionItem
{
    public Guid Id { get; set; }
    public Guid WorkAreaId { get; set; }
    public Guid EntityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
