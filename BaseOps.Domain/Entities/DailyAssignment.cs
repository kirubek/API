using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public sealed class DailyAssignment : AuditableEntity
{
    public DateTime Date { get; set; }
    public string? AircraftType { get; set; }
    public string? AircraftRegistration { get; set; }
    public int ExpectedManpower { get; set; }
    public string Status { get; set; } = "Draft";
    public Guid SectionId { get; set; }
    public Guid? HangarId { get; set; }
    public Guid? ShopId { get; set; }
    public Guid TeamLeaderId { get; set; }
    public string? Shift { get; set; }
    
    // Navigation properties
    public Section Section { get; set; } = null!;
    public Hangar? Hangar { get; set; }
    public Shop? Shop { get; set; }
    public ApplicationUser TeamLeader { get; set; } = null!;
    public ICollection<AssignmentDetail> Details { get; set; } = new List<AssignmentDetail>();
}
