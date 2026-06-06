using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public enum IssueType
{
    Tools = 1,
    Equipment = 2,
    Parts = 3,
    Other = 4
}

public sealed class HandoverIssue : AuditableEntity
{
    public Guid HandoverId { get; set; }
    public IssueType IssueType { get; set; }
    public string Description { get; set; } = string.Empty;
    
    // Navigation properties
    public Handover Handover { get; set; } = null!;
}
