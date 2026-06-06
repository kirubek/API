using BaseOps.Application.Interfaces;
using BaseOps.Application.SAFA;
using BaseOps.Application.SAFA.DTOs;
using BaseOps.Domain.Entities;
using BaseOps.Domain.Enums;
using BaseOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BaseOps.Infrastructure.SAFA;

public class SafaService : ISafaService
{
    private readonly BaseOpsDbContext _dbContext;
    private readonly ILogger<SafaService> _logger;
    private readonly IAuditService _auditService;

    public SafaService(
        BaseOpsDbContext dbContext,
        ILogger<SafaService> logger,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _auditService = auditService;
    }

    // Inspection operations
    public async Task<SafaInspectionDto> CreateInspectionAsync(CreateSafaInspectionDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }
        
        _logger.LogInformation("Creating inspection with SectionId: {SectionId}, FleetType: {FleetType}, Defects: {DefectCount}", 
            dto.SectionId, dto.FleetType, dto.Defects?.Count ?? 0);
        
        // Validate SectionId exists (required field)
        if (dto.SectionId == Guid.Empty)
        {
            throw new InvalidOperationException("SectionId is required");
        }
        
        var sectionExists = await _dbContext.Sections.AnyAsync(s => s.Id == dto.SectionId, cancellationToken);
        if (!sectionExists)
        {
            throw new InvalidOperationException($"Section with ID {dto.SectionId} not found");
        }

        // Validate HangarId exists if provided
        if (dto.HangarId.HasValue)
        {
            var hangarExists = await _dbContext.Hangars.AnyAsync(h => h.Id == dto.HangarId.Value, cancellationToken);
            if (!hangarExists)
            {
                throw new InvalidOperationException($"Hangar with ID {dto.HangarId.Value} not found");
            }
        }

        // Validate ShopId exists if provided
        if (dto.ShopId.HasValue)
        {
            var shopExists = await _dbContext.Shops.AnyAsync(s => s.Id == dto.ShopId.Value, cancellationToken);
            if (!shopExists)
            {
                throw new InvalidOperationException($"Shop with ID {dto.ShopId.Value} not found");
            }
        }

        var inspection = new SafaInspection
        {
            InspectionType = dto.InspectionType,
            FleetType = dto.FleetType,
            AircraftRegistration = dto.AircraftRegistration,
            FlightInfo = dto.FlightInfo,
            InspectionDate = dto.InspectionDate,
            SectionId = dto.SectionId,
            HangarId = dto.HangarId,
            ShopId = dto.ShopId,
            InspectorId = userId,
            Shift = dto.Shift,
            Status = InspectionStatus.Draft,
            Conclusion = dto.Conclusion,
            CreatedBy = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.SafaInspections.Add(inspection);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Create defects if provided
        if (dto.Defects != null && dto.Defects.Count > 0)
        {
            foreach (var defectDto in dto.Defects)
            {
                // Skip invalid defects
                if (string.IsNullOrEmpty(defectDto.Category) || string.IsNullOrEmpty(defectDto.ObservationFinding))
                {
                    continue;
                }
                
                var defect = new SafaDefect
                {
                    InspectionId = inspection.Id,
                    Category = defectDto.Category,
                    SubCategory = defectDto.SubCategory,
                    StandardDescription = defectDto.StandardDescription,
                    ObservationFinding = defectDto.ObservationFinding,
                    NeedToFix = defectDto.NeedToFix,
                    Status = DefectStatus.Active,
                    CreatedBy = userId,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _dbContext.SafaDefects.Add(defect);
            }
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var defectsCount = dto.Defects?.Count ?? 0;
        await _auditService.WriteAsync(
            userId,
            "InspectionCreated",
            "SafaInspection",
            inspection.Id.ToString(),
            null,
            System.Text.Json.JsonSerializer.Serialize(new { inspection.Id, inspection.InspectionType, inspection.FleetType, inspection.AircraftRegistration, DefectsCount = defectsCount }),
            false,
            null,
            Guid.NewGuid().ToString(),
            cancellationToken);

        _logger.LogInformation("Created SAFA inspection {InspectionId} for aircraft {Aircraft}", inspection.Id, dto.AircraftRegistration);

        return MapToDto(inspection);
    }

    public async Task<SafaInspectionDto?> GetInspectionAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var inspection = await _dbContext.SafaInspections
            .Include(i => i.Defects)
            .Include(i => i.Inspector)
            .Include(i => i.Section)
            .Include(i => i.Hangar)
            .Include(i => i.Shop)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (inspection == null)
            return null;

        // Apply RBAC: Inspectors can only see their own inspections
        var user = await _dbContext.Users.FindAsync(userId, cancellationToken);
        if (user != null && user.Role == UserRole.SafaInspector && inspection.InspectorId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to access inspection {InspectionId} owned by another inspector", userId, id);
            return null;
        }

        // Apply scope: Team Leaders can only see inspections in their assigned hangar/shop
        if (user != null && user.Role == UserRole.TeamLeader)
        {
            if (inspection.HangarId != user.HangarId && inspection.ShopId != user.ShopId)
            {
                _logger.LogWarning("Team Leader {UserId} attempted to access inspection {InspectionId} outside their scope", userId, id);
                return null;
            }
        }

        // Apply scope: Managers can only see inspections in their section
        if (user != null && user.Role == UserRole.Manager && inspection.SectionId != user.SectionId)
        {
            _logger.LogWarning("Manager {UserId} attempted to access inspection {InspectionId} outside their section", userId, id);
            return null;
        }

        return MapToDto(inspection);
    }

    public async Task<SafaInspectionDto> UpdateInspectionAsync(Guid id, UpdateSafaInspectionDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        var inspection = await _dbContext.SafaInspections.FindAsync(id, cancellationToken);
        if (inspection == null)
            throw new InvalidOperationException($"Inspection {id} not found");

        // CRITICAL: Immutability enforcement - cannot modify submitted/completed inspections
        if (inspection.Status == InspectionStatus.Submitted || inspection.Status == InspectionStatus.Completed)
        {
            throw new InvalidOperationException($"Cannot modify inspection with status {inspection.Status}");
        }

        // Ownership validation
        if (inspection.InspectorId != userId)
        {
            throw new UnauthorizedAccessException("You can only modify your own inspections");
        }

        var beforeValues = System.Text.Json.JsonSerializer.Serialize(inspection);

        if (dto.FleetType != null) inspection.FleetType = dto.FleetType;
        if (dto.AircraftRegistration != null) inspection.AircraftRegistration = dto.AircraftRegistration;
        if (dto.FlightInfo != null) inspection.FlightInfo = dto.FlightInfo;
        if (dto.InspectionDate.HasValue) inspection.InspectionDate = dto.InspectionDate.Value;
        if (dto.Shift != null) inspection.Shift = dto.Shift;
        if (dto.Conclusion != null) inspection.Conclusion = dto.Conclusion;

        inspection.UpdatedBy = userId;
        inspection.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.WriteAsync(
            userId,
            "InspectionUpdated",
            "SafaInspection",
            inspection.Id.ToString(),
            beforeValues,
            System.Text.Json.JsonSerializer.Serialize(inspection),
            false,
            null,
            Guid.NewGuid().ToString(),
            cancellationToken);

        _logger.LogInformation("Updated SAFA inspection {InspectionId}", inspection.Id);

        return MapToDto(inspection);
    }

    public async Task DeleteInspectionAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var inspection = await _dbContext.SafaInspections.FindAsync(id, cancellationToken);
        if (inspection == null)
            throw new InvalidOperationException($"Inspection {id} not found");

        // CRITICAL: Cannot delete submitted/completed inspections
        if (inspection.Status == InspectionStatus.Submitted || inspection.Status == InspectionStatus.Completed)
        {
            throw new InvalidOperationException($"Cannot delete inspection with status {inspection.Status}");
        }

        // Ownership validation
        if (inspection.InspectorId != userId)
        {
            throw new UnauthorizedAccessException("You can only delete your own inspections");
        }

        var beforeValues = System.Text.Json.JsonSerializer.Serialize(inspection);

        _dbContext.SafaInspections.Remove(inspection);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.WriteAsync(
            userId,
            "InspectionDeleted",
            "SafaInspection",
            inspection.Id.ToString(),
            beforeValues,
            null,
            false,
            null,
            Guid.NewGuid().ToString(),
            cancellationToken);

        _logger.LogInformation("Deleted SAFA inspection {InspectionId}", id);
    }

    public async Task<SafaInspectionDto> SubmitInspectionAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var inspection = await _dbContext.SafaInspections.FindAsync(id, cancellationToken);
        if (inspection == null)
            throw new InvalidOperationException($"Inspection {id} not found");

        // Ownership validation
        if (inspection.InspectorId != userId)
        {
            throw new UnauthorizedAccessException("You can only submit your own inspections");
        }

        // Status transition validation
        if (!InspectionStatusTransitions.IsValidTransition(inspection.Status, InspectionStatus.Submitted))
        {
            throw new InvalidOperationException($"Cannot transition from {inspection.Status} to Submitted");
        }

        var beforeValues = System.Text.Json.JsonSerializer.Serialize(inspection);

        inspection.Status = InspectionStatus.Submitted;
        inspection.SubmittedBy = (await _dbContext.Users.FindAsync(userId, cancellationToken))?.FullName;
        inspection.SubmittedAt = DateTime.UtcNow;
        inspection.UpdatedBy = userId;
        inspection.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.WriteAsync(
            userId,
            "InspectionSubmitted",
            "SafaInspection",
            inspection.Id.ToString(),
            beforeValues,
            System.Text.Json.JsonSerializer.Serialize(inspection),
            false,
            null,
            Guid.NewGuid().ToString(),
            cancellationToken);

        _logger.LogInformation("Submitted SAFA inspection {InspectionId}", inspection.Id);

        return MapToDto(inspection);
    }

    public async Task<(List<SafaInspectionListDto> Items, int TotalCount, int TotalPages)> GetInspectionsAsync(
        int page, int pageSize, Guid userId,
        SafaInspectionType? inspectionType = null,
        InspectionStatus? status = null,
        Guid? sectionId = null,
        Guid? hangarId = null,
        Guid? shopId = null,
        string? aircraftRegistration = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync(userId, cancellationToken);
        var query = _dbContext.SafaInspections
            .Include(i => i.Defects)
            .Include(i => i.Inspector)
            .AsNoTracking();

        // Apply RBAC scope filtering
        if (user != null)
        {
            switch (user.Role)
            {
                case UserRole.SafaInspector:
                    // Inspectors see only their own inspections
                    query = query.Where(i => i.InspectorId == userId);
                    break;
                case UserRole.TeamLeader:
                    // Team Leaders see inspections in their assigned hangar OR shop OR their own created inspections
                    if (user.HangarId.HasValue || user.ShopId.HasValue)
                    {
                        query = query.Where(i => i.HangarId == user.HangarId || i.ShopId == user.ShopId || i.InspectorId == userId);
                    }
                    else
                    {
                        // Unassigned team leaders see only their own created inspections
                        query = query.Where(i => i.InspectorId == userId);
                    }
                    break;
                case UserRole.Manager:
                    // Managers see inspections in their section OR their own created inspections
                    if (user.SectionId.HasValue)
                    {
                        query = query.Where(i => i.SectionId == user.SectionId || i.InspectorId == userId);
                    }
                    else
                    {
                        // Unassigned managers see only their own created inspections
                        query = query.Where(i => i.InspectorId == userId);
                    }
                    break;
                case UserRole.Director:
                    // Directors see all inspections (no filtering)
                    break;
            }
        }

        // Apply filters
        if (inspectionType.HasValue)
            query = query.Where(i => i.InspectionType == inspectionType.Value);
        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);
        if (sectionId.HasValue)
            query = query.Where(i => i.SectionId == sectionId.Value);
        if (hangarId.HasValue)
            query = query.Where(i => i.HangarId == hangarId.Value);
        if (shopId.HasValue)
            query = query.Where(i => i.ShopId == shopId.Value);
        if (!string.IsNullOrEmpty(aircraftRegistration))
            query = query.Where(i => i.AircraftRegistration.Contains(aircraftRegistration));
        if (startDate.HasValue)
            query = query.Where(i => i.InspectionDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(i => i.InspectionDate <= endDate.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var inspections = await query
            .OrderByDescending(i => i.InspectionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new SafaInspectionListDto
            {
                Id = i.Id,
                InspectionType = i.InspectionType,
                FleetType = i.FleetType,
                AircraftRegistration = i.AircraftRegistration,
                InspectionDate = i.InspectionDate,
                InspectorName = i.Inspector.FullName,
                Status = i.Status,
                TotalDefects = i.Defects.Count,
                ActiveDefects = i.Defects.Count(d => d.Status == DefectStatus.Active),
                CreatedAt = i.CreatedAt.DateTime
            })
            .ToListAsync(cancellationToken);

        return (inspections, totalCount, totalPages);
    }

    // Defect operations
    public async Task<SafaDefectDto> CreateDefectAsync(CreateSafaDefectDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        var inspection = await _dbContext.SafaInspections.FindAsync(dto.InspectionId, cancellationToken);
        if (inspection == null)
            throw new InvalidOperationException($"Inspection {dto.InspectionId} not found");

        // CRITICAL: Cannot add defects to submitted/completed inspections
        if (inspection.Status == InspectionStatus.Submitted || inspection.Status == InspectionStatus.Completed)
        {
            throw new InvalidOperationException($"Cannot add defects to inspection with status {inspection.Status}");
        }

        // Ownership validation
        if (inspection.InspectorId != userId)
        {
            throw new UnauthorizedAccessException("You can only add defects to your own inspections");
        }

        var defect = new SafaDefect
        {
            InspectionId = dto.InspectionId,
            Category = dto.Category,
            SubCategory = dto.SubCategory,
            StandardDescription = dto.StandardDescription,
            ObservationFinding = dto.ObservationFinding,
            NeedToFix = dto.NeedToFix,
            Status = DefectStatus.Active,
            CreatedBy = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.SafaDefects.Add(defect);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.WriteAsync(
            userId,
            "DefectCreated",
            "SafaDefect",
            defect.Id.ToString(),
            null,
            System.Text.Json.JsonSerializer.Serialize(defect),
            false,
            null,
            Guid.NewGuid().ToString(),
            cancellationToken);

        _logger.LogInformation("Created defect {DefectId} for inspection {InspectionId}", defect.Id, dto.InspectionId);

        return MapToDefectDto(defect);
    }

    public async Task<SafaDefectDto?> GetDefectAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var defect = await _dbContext.SafaDefects
            .Include(d => d.Inspection)
            .Include(d => d.ActionTakenByUser)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (defect == null)
            return null;

        // Apply RBAC scope validation
        var user = await _dbContext.Users.FindAsync(userId, cancellationToken);
        if (user != null)
        {
            switch (user.Role)
            {
                case UserRole.SafaInspector:
                    // Inspectors can only see defects in their own inspections
                    if (defect.Inspection.InspectorId != userId)
                        return null;
                    break;
                case UserRole.TeamLeader:
                    // Team Leaders can only see defects in their assigned hangar OR shop
                    if (defect.Inspection.HangarId != user.HangarId && defect.Inspection.ShopId != user.ShopId)
                        return null;
                    break;
                case UserRole.Manager:
                    // Managers can only see defects in their section
                    if (defect.Inspection.SectionId != user.SectionId)
                        return null;
                    break;
            }
        }

        return MapToDefectDto(defect);
    }

    public async Task<SafaDefectDto> UpdateDefectAsync(Guid id, UpdateSafaDefectDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        var defect = await _dbContext.SafaDefects
            .Include(d => d.Inspection)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (defect == null)
            throw new InvalidOperationException($"Defect {id} not found");

        // CRITICAL: Cannot modify defects in submitted/completed inspections
        if (defect.Inspection.Status == InspectionStatus.Submitted || defect.Inspection.Status == InspectionStatus.Completed)
        {
            throw new InvalidOperationException($"Cannot modify defects in inspection with status {defect.Inspection.Status}");
        }

        // Ownership validation
        if (defect.Inspection.InspectorId != userId)
        {
            throw new UnauthorizedAccessException("You can only modify defects in your own inspections");
        }

        var beforeValues = System.Text.Json.JsonSerializer.Serialize(defect);

        if (dto.ObservationFinding != null) defect.ObservationFinding = dto.ObservationFinding;
        if (dto.NeedToFix.HasValue) defect.NeedToFix = dto.NeedToFix.Value;

        defect.UpdatedBy = userId;
        defect.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.WriteAsync(
            userId,
            "DefectUpdated",
            "SafaDefect",
            defect.Id.ToString(),
            beforeValues,
            System.Text.Json.JsonSerializer.Serialize(defect),
            false,
            null,
            Guid.NewGuid().ToString(),
            cancellationToken);

        _logger.LogInformation("Updated defect {DefectId}", defect.Id);

        return MapToDefectDto(defect);
    }

    public async Task DeleteDefectAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var defect = await _dbContext.SafaDefects
            .Include(d => d.Inspection)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (defect == null)
            throw new InvalidOperationException($"Defect {id} not found");

        // CRITICAL: Cannot delete defects in submitted/completed inspections
        if (defect.Inspection.Status == InspectionStatus.Submitted || defect.Inspection.Status == InspectionStatus.Completed)
        {
            throw new InvalidOperationException($"Cannot delete defects in inspection with status {defect.Inspection.Status}");
        }

        // Ownership validation
        if (defect.Inspection.InspectorId != userId)
        {
            throw new UnauthorizedAccessException("You can only delete defects in your own inspections");
        }

        var beforeValues = System.Text.Json.JsonSerializer.Serialize(defect);

        _dbContext.SafaDefects.Remove(defect);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.WriteAsync(
            userId,
            "DefectDeleted",
            "SafaDefect",
            defect.Id.ToString(),
            beforeValues,
            null,
            false,
            null,
            Guid.NewGuid().ToString(),
            cancellationToken);

        _logger.LogInformation("Deleted defect {DefectId}", id);
    }

    public async Task<SafaDefectDto> TakeCorrectiveActionAsync(Guid id, TakeCorrectiveActionDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        var defect = await _dbContext.SafaDefects
            .Include(d => d.Inspection)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (defect == null)
            throw new InvalidOperationException($"Defect {id} not found");

        // Parse status string to enum
        if (!Enum.TryParse<DefectStatus>(dto.Status, true, out var status))
        {
            throw new InvalidOperationException($"Invalid defect status: {dto.Status}");
        }

        // CRITICAL: Validation - WaitingForPart requires PartRequestId
        if (status == DefectStatus.WaitingForPart && string.IsNullOrEmpty(dto.PartRequestId))
        {
            throw new InvalidOperationException("PartRequestId is required when status is WaitingForPart");
        }

        // Status transition validation
        if (!DefectStatusTransitions.IsValidTransition(defect.Status, status))
        {
            throw new InvalidOperationException($"Cannot transition from {defect.Status} to {status}");
        }

        // Scope validation: Team Leaders can only take action on defects in their assigned hangar OR shop
        var user = await _dbContext.Users.FindAsync(userId, cancellationToken);
        if (user != null && user.Role == UserRole.TeamLeader)
        {
            if (defect.Inspection.HangarId != user.HangarId && defect.Inspection.ShopId != user.ShopId)
            {
                throw new UnauthorizedAccessException("You can only take action on defects in your assigned hangar or shop");
            }
        }

        var beforeValues = System.Text.Json.JsonSerializer.Serialize(new { 
            defect.Id, 
            defect.Status, 
            defect.CorrectiveAction, 
            defect.TaskCardCode, 
            defect.PartRequestId, 
            defect.Remarks 
        });

        defect.CorrectiveAction = dto.CorrectiveAction;
        defect.TaskCardCode = dto.TaskCardCode;
        defect.PartRequestId = dto.PartRequestId;
        defect.Status = status;
        defect.Remarks = dto.Remarks;
        defect.ActionTakenByUserId = userId;
        defect.ActionTakenAt = DateTime.UtcNow;
        defect.UpdatedBy = userId;
        defect.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var afterValues = System.Text.Json.JsonSerializer.Serialize(new { 
            defect.Id, 
            defect.Status, 
            defect.CorrectiveAction, 
            defect.TaskCardCode, 
            defect.PartRequestId, 
            defect.Remarks,
            defect.ActionTakenByUserId,
            defect.ActionTakenAt 
        });

        await _auditService.WriteAsync(
            userId,
            "CorrectiveActionTaken",
            "SafaDefect",
            defect.Id.ToString(),
            beforeValues,
            afterValues,
            false,
            null,
            Guid.NewGuid().ToString(),
            cancellationToken);

        _logger.LogInformation("Team Leader {UserId} took corrective action on defect {DefectId}", userId, defect.Id);

        return MapToDefectDto(defect);
    }

    public async Task<SafaDefectDto> UpdateDefectStatusAsync(Guid id, UpdateDefectStatusDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        var defect = await _dbContext.SafaDefects
            .Include(d => d.Inspection)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (defect == null)
            throw new InvalidOperationException($"Defect {id} not found");

        // Parse status string to enum
        if (!Enum.TryParse<DefectStatus>(dto.Status, true, out var status))
        {
            throw new InvalidOperationException($"Invalid defect status: {dto.Status}");
        }

        // CRITICAL: Validation - WaitingForPart requires PartRequestId
        if (status == DefectStatus.WaitingForPart && string.IsNullOrEmpty(dto.PartRequestId))
        {
            throw new InvalidOperationException("PartRequestId is required when status is WaitingForPart");
        }

        // Status transition validation
        if (!DefectStatusTransitions.IsValidTransition(defect.Status, status))
        {
            throw new InvalidOperationException($"Cannot transition from {defect.Status} to {status}");
        }

        // Scope validation
        var user = await _dbContext.Users.FindAsync(userId, cancellationToken);
        if (user != null && user.Role == UserRole.TeamLeader)
        {
            if (defect.Inspection.HangarId != user.HangarId && defect.Inspection.ShopId != user.ShopId)
            {
                throw new UnauthorizedAccessException("You can only update defects in your assigned hangar or shop");
            }
        }

        var beforeValues = System.Text.Json.JsonSerializer.Serialize(new { 
            defect.Id, 
            defect.Status, 
            defect.PartRequestId 
        });

        defect.Status = status;
        defect.PartRequestId = dto.PartRequestId;
        defect.UpdatedBy = userId;
        defect.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var afterValues = System.Text.Json.JsonSerializer.Serialize(new { 
            defect.Id, 
            defect.Status, 
            defect.PartRequestId 
        });

        await _auditService.WriteAsync(
            userId,
            "DefectStatusChanged",
            "SafaDefect",
            defect.Id.ToString(),
            beforeValues,
            afterValues,
            false,
            null,
            Guid.NewGuid().ToString(),
            cancellationToken);

        _logger.LogInformation("Updated defect {DefectId} status to {Status}", defect.Id, dto.Status);

        return MapToDefectDto(defect);
    }

    public async Task<List<SafaDefectDto>> GetDefectsByInspectionAsync(Guid inspectionId, Guid userId, CancellationToken cancellationToken = default)
    {
        var inspection = await _dbContext.SafaInspections.FindAsync(inspectionId, cancellationToken);
        if (inspection == null)
            throw new InvalidOperationException($"Inspection {inspectionId} not found");

        // Apply RBAC scope validation
        var user = await _dbContext.Users.FindAsync(userId, cancellationToken);
        if (user != null)
        {
            switch (user.Role)
            {
                case UserRole.SafaInspector:
                    if (inspection.InspectorId != userId)
                        throw new UnauthorizedAccessException("You can only view defects in your own inspections");
                    break;
                case UserRole.TeamLeader:
                    if (inspection.HangarId != user.HangarId && inspection.ShopId != user.ShopId)
                        throw new UnauthorizedAccessException("You can only view defects in your assigned hangar or shop");
                    break;
                case UserRole.Manager:
                    if (inspection.SectionId != user.SectionId)
                        throw new UnauthorizedAccessException("You can only view defects in your section");
                    break;
            }
        }

        var defects = await _dbContext.SafaDefects
            .Include(d => d.ActionTakenByUser)
            .Where(d => d.InspectionId == inspectionId)
            .OrderBy(d => d.Category)
            .ToListAsync(cancellationToken);

        return defects.Select(MapToDefectDto).ToList();
    }

    // Template operations
    public async Task<SafaTemplateDto> CreateTemplateAsync(CreateSafaTemplateDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        var template = new SafaTemplate
        {
            InspectionType = dto.InspectionType,
            Name = dto.Name,
            Description = dto.Description,
            TemplateJson = dto.TemplateJson,
            IsActive = true,
            Version = 1,
            CreatedBy = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.SafaTemplates.Add(template);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.WriteAsync(
            userId,
            "TemplateCreated",
            "SafaTemplate",
            template.Id.ToString(),
            null,
            System.Text.Json.JsonSerializer.Serialize(template),
            false,
            null,
            Guid.NewGuid().ToString(),
            cancellationToken);

        _logger.LogInformation("Created SAFA template {TemplateId}", template.Id);

        return MapToTemplateDto(template);
    }

    public async Task<SafaTemplateDto?> GetTemplateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await _dbContext.SafaTemplates.FindAsync(id, cancellationToken);
        return template == null ? null : MapToTemplateDto(template);
    }

    public async Task<SafaTemplateDto> UpdateTemplateAsync(Guid id, UpdateSafaTemplateDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        var template = await _dbContext.SafaTemplates.FindAsync(id, cancellationToken);
        if (template == null)
            throw new InvalidOperationException($"Template {id} not found");

        var beforeValues = System.Text.Json.JsonSerializer.Serialize(template);

        if (dto.Name != null) template.Name = dto.Name;
        if (dto.Description != null) template.Description = dto.Description;
        if (dto.TemplateJson != null) template.TemplateJson = dto.TemplateJson;
        if (dto.IsActive.HasValue) template.IsActive = dto.IsActive.Value;
        template.Version++;
        template.UpdatedBy = userId;
        template.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.WriteAsync(
            userId,
            "TemplateUpdated",
            "SafaTemplate",
            template.Id.ToString(),
            beforeValues,
            System.Text.Json.JsonSerializer.Serialize(template),
            false,
            null,
            Guid.NewGuid().ToString(),
            cancellationToken);

        _logger.LogInformation("Updated SAFA template {TemplateId}", template.Id);

        return MapToTemplateDto(template);
    }

    public async Task<SafaTemplateDto?> GetActiveTemplateAsync(SafaInspectionType inspectionType, CancellationToken cancellationToken = default)
    {
        var template = await _dbContext.SafaTemplates
            .Where(t => t.InspectionType == inspectionType && t.IsActive)
            .OrderByDescending(t => t.Version)
            .FirstOrDefaultAsync(cancellationToken);

        return template == null ? null : MapToTemplateDto(template);
    }

    public async Task<List<SafaTemplateDto>> GetTemplatesAsync(SafaInspectionType? inspectionType = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SafaTemplates.AsNoTracking();

        if (inspectionType.HasValue)
            query = query.Where(t => t.InspectionType == inspectionType.Value);

        var templates = await query
            .OrderByDescending(t => t.Version)
            .ToListAsync(cancellationToken);

        return templates.Select(MapToTemplateDto).ToList();
    }

    // Analytics operations
    public async Task<SafaDashboardDto> GetDashboardAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync(userId, cancellationToken);
        var query = _dbContext.SafaInspections.AsNoTracking();

        // Apply RBAC scope filtering
        if (user != null)
        {
            switch (user.Role)
            {
                case UserRole.SafaInspector:
                    query = query.Where(i => i.InspectorId == userId);
                    break;
                case UserRole.TeamLeader:
                    if (user.HangarId.HasValue || user.ShopId.HasValue)
                        query = query.Where(i => i.HangarId == user.HangarId || i.ShopId == user.ShopId);
                    else
                        query = query.Where(i => false);
                    break;
                case UserRole.Manager:
                    if (user.SectionId.HasValue)
                        query = query.Where(i => i.SectionId == user.SectionId);
                    break;
            }
        }

        if (startDate.HasValue)
            query = query.Where(i => i.InspectionDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(i => i.InspectionDate <= endDate.Value);

        var inspections = await query.ToListAsync(cancellationToken);

        var satisfactory = inspections.Count(i => i.Status == InspectionStatus.Completed);
        var unsatisfactory = inspections.Count(i => i.Status == InspectionStatus.Submitted && i.Defects.Any(d => d.Status == DefectStatus.Active));
        var total = inspections.Count;
        var complianceRate = total > 0 ? (double)satisfactory / total * 100 : 0;

        var byType = inspections
            .GroupBy(i => i.InspectionType.ToString())
            .Select(g => new InspectionByTypeDto { Type = g.Key, Count = g.Count() })
            .ToList();

        var recentInspections = inspections
            .OrderByDescending(i => i.CreatedAt)
            .Take(10)
            .Select(i => new RecentInspectionDto
            {
                Id = i.Id,
                InspectionType = i.InspectionType.ToString(),
                Status = i.Status.ToString(),
                CreatedAt = i.CreatedAt.DateTime,
                AircraftRegistration = i.AircraftRegistration
            })
            .ToList();

        var openDefects = await _dbContext.SafaDefects
            .Include(d => d.Inspection)
            .AsNoTracking()
            .Where(d => d.Status == DefectStatus.Active)
            .CountAsync(cancellationToken);

        return new SafaDashboardDto
        {
            TotalInspections = total,
            Satisfactory = satisfactory,
            Unsatisfactory = unsatisfactory,
            OpenDefects = openDefects,
            ComplianceRate = complianceRate,
            ByType = byType,
            RecentInspections = recentInspections
        };
    }

    public async Task<SafaAnalyticsDto> GetAnalyticsAsync(Guid userId, SafaInspectionType? inspectionType = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync(userId, cancellationToken);
        var query = _dbContext.SafaInspections.AsNoTracking();

        // Apply RBAC scope filtering
        if (user != null)
        {
            switch (user.Role)
            {
                case UserRole.SafaInspector:
                    query = query.Where(i => i.InspectorId == userId);
                    break;
                case UserRole.TeamLeader:
                    if (user.HangarId.HasValue || user.ShopId.HasValue)
                        query = query.Where(i => i.HangarId == user.HangarId || i.ShopId == user.ShopId);
                    else
                        query = query.Where(i => false);
                    break;
                case UserRole.Manager:
                    if (user.SectionId.HasValue)
                        query = query.Where(i => i.SectionId == user.SectionId);
                    break;
            }
        }

        if (inspectionType.HasValue)
            query = query.Where(i => i.InspectionType == inspectionType.Value);
        if (startDate.HasValue)
            query = query.Where(i => i.InspectionDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(i => i.InspectionDate <= endDate.Value);

        var inspections = await query.ToListAsync(cancellationToken);

        var byStatus = inspections
            .GroupBy(i => i.Status.ToString())
            .Select(g => new StatusCountDto { Status = g.Key, Count = g.Count() })
            .ToList();

        var byInspectionType = inspections
            .GroupBy(i => i.InspectionType.ToString())
            .Select(g => new InspectionByTypeDto { Type = g.Key, Count = g.Count() })
            .ToList();

        var bySection = inspections
            .GroupBy(i => i.SectionId.ToString())
            .Select(g => new SectionCountDto { Section = g.Key, Count = g.Count() })
            .ToList();

        var findingSeverity = inspections
            .SelectMany(i => i.Defects)
            .GroupBy(d => d.Category)
            .Select(g => new SeverityCountDto { Severity = g.Key, Count = g.Count() })
            .ToList();

        var monthlyTrends = inspections
            .GroupBy(i => new { i.InspectionDate.Year, i.InspectionDate.Month })
            .Select(g => new MonthlyTrendDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count(),
                Completed = g.Count(i => i.Status == InspectionStatus.Completed)
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToList();

        return new SafaAnalyticsDto
        {
            ByStatus = byStatus,
            ByInspectionType = byInspectionType,
            BySection = bySection,
            FindingSeverity = findingSeverity,
            MonthlyTrends = monthlyTrends
        };
    }

    public async Task<DetailedAnalyticsDto> GetDetailedAnalyticsAsync(Guid userId, SafaInspectionType inspectionType, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync(userId, cancellationToken);
        var query = _dbContext.SafaInspections
            .Include(i => i.Defects)
            .AsNoTracking();

        // Apply RBAC scope filtering
        if (user != null)
        {
            switch (user.Role)
            {
                case UserRole.SafaInspector:
                    query = query.Where(i => i.InspectorId == userId);
                    break;
                case UserRole.TeamLeader:
                    if (user.HangarId.HasValue || user.ShopId.HasValue)
                        query = query.Where(i => i.HangarId == user.HangarId || i.ShopId == user.ShopId);
                    else
                        query = query.Where(i => false);
                    break;
                case UserRole.Manager:
                    if (user.SectionId.HasValue)
                        query = query.Where(i => i.SectionId == user.SectionId);
                    break;
            }
        }

        query = query.Where(i => i.InspectionType == inspectionType);
        if (startDate.HasValue)
            query = query.Where(i => i.InspectionDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(i => i.InspectionDate <= endDate.Value);

        var inspections = await query.ToListAsync(cancellationToken);
        var allDefects = inspections.SelectMany(i => i.Defects).ToList();

        var totalDefects = allDefects.Count;
        var activeDefects = allDefects.Count(d => d.Status == DefectStatus.Active);
        var clearedDefects = allDefects.Count(d => d.Status == DefectStatus.Cleared);
        var waitingForPartDefects = allDefects.Count(d => d.Status == DefectStatus.WaitingForPart);
        var defectClosureRate = totalDefects > 0 ? (double)clearedDefects / totalDefects * 100 : 0;

        var avgResolutionTime = allDefects
            .Where(d => d.ActionTakenAt.HasValue)
            .Select(d => (d.ActionTakenAt.Value - d.CreatedAt.DateTime).TotalDays)
            .DefaultIfEmpty(0)
            .Average();

        var defectsByCategory = allDefects
            .GroupBy(d => d.Category)
            .Select(g => new CategoryCountDto { Category = g.Key, Count = g.Count() })
            .ToList();

        var defectsByStatus = allDefects
            .GroupBy(d => d.Status.ToString())
            .Select(g => new StatusCountDto { Status = g.Key, Count = g.Count() })
            .ToList();

        var fleetComparison = inspections
            .GroupBy(i => i.FleetType)
            .Select(g => new FleetComparisonDto
            {
                Fleet = g.Key,
                Defects = g.SelectMany(i => i.Defects).Count(),
                ClosureRate = g.SelectMany(i => i.Defects).Count() > 0
                    ? (double)g.SelectMany(i => i.Defects).Count(d => d.Status == DefectStatus.Cleared) / g.SelectMany(i => i.Defects).Count() * 100
                    : 0
            })
            .ToList();

        var hangarComparison = inspections
            .GroupBy(i => i.HangarId?.ToString() ?? "Unassigned")
            .Select(g => new HangarComparisonDto
            {
                Hangar = g.Key,
                Active = g.SelectMany(i => i.Defects).Count(d => d.Status == DefectStatus.Active),
                Cleared = g.SelectMany(i => i.Defects).Count(d => d.Status == DefectStatus.Cleared),
                Waiting = g.SelectMany(i => i.Defects).Count(d => d.Status == DefectStatus.WaitingForPart),
                Total = g.SelectMany(i => i.Defects).Count()
            })
            .ToList();

        var monthlyTrend = inspections
            .GroupBy(i => new { i.InspectionDate.Year, i.InspectionDate.Month })
            .Select(g => new MonthlyTrendDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.SelectMany(i => i.Defects).Count(),
                Completed = g.SelectMany(i => i.Defects).Count(d => d.Status == DefectStatus.Cleared)
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToList();

        var categoryFleetHeatmap = allDefects
            .GroupBy(d => d.Category)
            .Select(g => new CategoryFleetHeatmapDto
            {
                Category = g.Key,
                A350 = g.Count(d => d.Inspection.FleetType == "A350"),
                B787 = g.Count(d => d.Inspection.FleetType == "B787"),
                B777 = g.Count(d => d.Inspection.FleetType == "B777"),
                B737 = g.Count(d => d.Inspection.FleetType == "B737")
            })
            .ToList();

        return new DetailedAnalyticsDto
        {
            TotalDefects = totalDefects,
            ActiveDefects = activeDefects,
            ClearedDefects = clearedDefects,
            WaitingForPartDefects = waitingForPartDefects,
            DefectClosureRate = defectClosureRate,
            AvgResolutionTime = avgResolutionTime,
            DefectsByCategory = defectsByCategory,
            DefectsByStatus = defectsByStatus,
            FleetComparison = fleetComparison,
            HangarComparison = hangarComparison,
            MonthlyTrend = monthlyTrend,
            CategoryFleetHeatmap = categoryFleetHeatmap
        };
    }

    // Mapping helpers
    private static SafaInspectionDto MapToDto(SafaInspection inspection)
    {
        return new SafaInspectionDto
        {
            Id = inspection.Id,
            InspectionType = inspection.InspectionType,
            FleetType = inspection.FleetType,
            AircraftRegistration = inspection.AircraftRegistration,
            FlightInfo = inspection.FlightInfo,
            InspectionDate = inspection.InspectionDate,
            SectionId = inspection.SectionId,
            HangarId = inspection.HangarId,
            ShopId = inspection.ShopId,
            InspectorId = inspection.InspectorId,
            Shift = inspection.Shift,
            Status = inspection.Status,
            Conclusion = inspection.Conclusion,
            SubmittedBy = inspection.SubmittedBy,
            SubmittedAt = inspection.SubmittedAt,
            CreatedAt = inspection.CreatedAt.UtcDateTime,
            UpdatedAt = inspection.UpdatedAt?.UtcDateTime,
            Defects = inspection.Defects.Select(MapToDefectDto).ToList()
        };
    }

    private static SafaDefectDto MapToDefectDto(SafaDefect defect)
    {
        return new SafaDefectDto
        {
            Id = defect.Id,
            InspectionId = defect.InspectionId,
            Category = defect.Category,
            SubCategory = defect.SubCategory,
            StandardDescription = defect.StandardDescription,
            ObservationFinding = defect.ObservationFinding,
            NeedToFix = defect.NeedToFix,
            Status = defect.Status,
            CorrectiveAction = defect.CorrectiveAction,
            TaskCardCode = defect.TaskCardCode,
            PartRequestId = defect.PartRequestId,
            Remarks = defect.Remarks,
            ActionTakenByUserId = defect.ActionTakenByUserId,
            ActionTakenByName = defect.ActionTakenByUser?.FullName,
            ActionTakenAt = defect.ActionTakenAt,
            CreatedAt = defect.CreatedAt.UtcDateTime,
            UpdatedAt = defect.UpdatedAt?.UtcDateTime
        };
    }

    private static SafaTemplateDto MapToTemplateDto(SafaTemplate template)
    {
        return new SafaTemplateDto
        {
            Id = template.Id,
            InspectionType = template.InspectionType,
            Name = template.Name,
            Description = template.Description,
            TemplateJson = template.TemplateJson,
            IsActive = template.IsActive,
            Version = template.Version,
            CreatedAt = template.CreatedAt.UtcDateTime,
            UpdatedAt = template.UpdatedAt?.UtcDateTime
        };
    }
}
