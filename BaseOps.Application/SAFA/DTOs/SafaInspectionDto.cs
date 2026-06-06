namespace BaseOps.Application.SAFA.DTOs;

using BaseOps.Domain.Entities;

public record CreateSafaInspectionDto
{
    public SafaInspectionType InspectionType { get; init; }
    public string FleetType { get; init; } = string.Empty;
    public string AircraftRegistration { get; init; } = string.Empty;
    public string? FlightInfo { get; init; }
    public DateTime InspectionDate { get; init; }
    public Guid SectionId { get; init; }
    public Guid? HangarId { get; init; }
    public Guid? ShopId { get; init; }
    public string Shift { get; init; } = string.Empty;
    public string? Conclusion { get; init; }
    public List<CreateSafaDefectDto> Defects { get; init; } = new();
}

public record UpdateSafaInspectionDto
{
    public string? FleetType { get; init; }
    public string? AircraftRegistration { get; init; }
    public string? FlightInfo { get; init; }
    public DateTime? InspectionDate { get; init; }
    public string? Shift { get; init; }
    public string? Conclusion { get; init; }
}

public record SafaInspectionDto
{
    public Guid Id { get; init; }
    public SafaInspectionType InspectionType { get; init; }
    public string FleetType { get; init; } = string.Empty;
    public string AircraftRegistration { get; init; } = string.Empty;
    public string? FlightInfo { get; init; }
    public DateTime InspectionDate { get; init; }
    public Guid SectionId { get; init; }
    public Guid? HangarId { get; init; }
    public Guid? ShopId { get; init; }
    public Guid InspectorId { get; init; }
    public string Shift { get; init; } = string.Empty;
    public InspectionStatus Status { get; init; }
    public string? Conclusion { get; init; }
    public string? SubmittedBy { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public List<SafaDefectDto> Defects { get; init; } = new();
}

public record SafaInspectionListDto
{
    public Guid Id { get; init; }
    public SafaInspectionType InspectionType { get; init; }
    public string FleetType { get; init; } = string.Empty;
    public string AircraftRegistration { get; init; } = string.Empty;
    public DateTime InspectionDate { get; init; }
    public string InspectorName { get; init; } = string.Empty;
    public InspectionStatus Status { get; init; }
    public int TotalDefects { get; init; }
    public int ActiveDefects { get; init; }
    public DateTime CreatedAt { get; init; }
}
