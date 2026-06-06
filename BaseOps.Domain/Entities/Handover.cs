using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public enum HandoverTemplateType
{
    Hangar = 1,
    Shop = 2
}

public enum HandoverStatus
{
    Draft = 1,
    Pending = 2,
    Accepted = 3,
    Rejected = 4
}

public enum ShiftType
{
    Day = 1,
    Night = 2,
    Evening = 3
}

public sealed class Handover : AuditableEntity
{
    public HandoverTemplateType TemplateType { get; set; }
    public HandoverStatus Status { get; set; } = HandoverStatus.Draft;
    public DateTime Date { get; set; }
    public ShiftType ShiftType { get; set; } = ShiftType.Day;
    public string DutyTeamLeaderName { get; set; } = string.Empty;
    
    // Organizational context
    public Guid SectionId { get; set; }
    public Guid? HangarId { get; set; }
    public Guid? ShopId { get; set; }
    public Guid OutgoingTeamLeaderId { get; set; }
    public Guid? IncomingTeamLeaderId { get; set; }
    
    // Workflow timestamps
    public DateTime? SubmittedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    
    // Soft delete support
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    
    // Optimistic concurrency
    public byte[] Version { get; set; } = Array.Empty<byte>();
    
    // Navigation properties
    public Section Section { get; set; } = null!;
    public Hangar? Hangar { get; set; }
    public Shop? Shop { get; set; }
    public ApplicationUser OutgoingTeamLeader { get; set; } = null!;
    public ApplicationUser? IncomingTeamLeader { get; set; }
    
    public ICollection<HandoverSignature> Signatures { get; set; } = new List<HandoverSignature>();
    public ICollection<HandoverTask> Tasks { get; set; } = new List<HandoverTask>();
    public ICollection<HandoverDefect> Defects { get; set; } = new List<HandoverDefect>();
    public ICollection<HandoverIssue> Issues { get; set; } = new List<HandoverIssue>();
    public ICollection<HandoverWorkStatus> WorkStatuses { get; set; } = new List<HandoverWorkStatus>();
    public ICollection<HandoverAircraft> Aircrafts { get; set; } = new List<HandoverAircraft>();
    public HandoverManningStatus ManningStatus { get; set; } = null!;
}
