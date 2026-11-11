namespace Avemel.HR.Infrastructure.Persistence;

public sealed class AuditEvent
{
    public long Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ActingUserId { get; set; } = string.Empty;
    public int? EmployeeId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
