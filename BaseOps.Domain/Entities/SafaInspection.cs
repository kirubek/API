using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public enum SafaInspectionType
{
    AircraftCabin,
    AMT
}

public enum InspectionStatus
{
    Draft,
    InProgress,
    Submitted,
    Completed
}

public sealed class SafaInspection : AuditableEntity
{
    public SafaInspectionType InspectionType { get; set; }
    public string FleetType { get; set; } = string.Empty;
    public string AircraftRegistration { get; set; } = string.Empty;
    public string? FlightInfo { get; set; }
    public DateTime InspectionDate { get; set; }
    public Guid SectionId { get; set; }
    public Guid? HangarId { get; set; }
    public Guid? ShopId { get; set; }
    public Guid InspectorId { get; set; }
    public string Shift { get; set; } = string.Empty;
    public InspectionStatus Status { get; set; } = InspectionStatus.Draft;
    public string? Conclusion { get; set; }
    public string? SubmittedBy { get; set; }
    public DateTime? SubmittedAt { get; set; }
    
    // Navigation properties
    public Section? Section { get; set; }
    public Hangar? Hangar { get; set; }
    public Shop? Shop { get; set; }
    public ApplicationUser Inspector { get; set; } = null!;
    public ICollection<SafaDefect> Defects { get; set; } = new List<SafaDefect>();
}
