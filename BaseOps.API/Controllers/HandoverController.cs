using BaseOps.Application.Interfaces;
using BaseOps.Domain.Entities;
using BaseOps.Domain.Enums;
using BaseOps.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;

namespace BaseOps.API.Controllers;

/// <summary>
/// API controller for managing Handover Logbook operations.
/// Supports creation, submission, acceptance, rejection, and tracking of handovers between team leaders.
/// </summary>
[ApiController]
[Authorize]
[Route("api/handover")]
public sealed class HandoverController(BaseOpsDbContext dbContext, IAuditService auditService) : ControllerBase
{
    private static bool IsValidSignatureData(string signatureData)
    {
        if (string.IsNullOrWhiteSpace(signatureData))
            return false;

        // Check if it's a valid Base64 string (for image data)
        try
        {
            var bytes = Convert.FromBase64String(signatureData);
            return bytes.Length > 0 && bytes.Length <= 100 * 1024; // Max 100KB
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidStateTransition(HandoverStatus currentStatus, HandoverStatus newStatus)
    {
        return (currentStatus, newStatus) switch
        {
            (HandoverStatus.Draft, HandoverStatus.Pending) => true,    // Submit
            (HandoverStatus.Pending, HandoverStatus.Accepted) => true,  // Accept
            (HandoverStatus.Pending, HandoverStatus.Rejected) => true,   // Reject
            (HandoverStatus.Rejected, HandoverStatus.Draft) => true,   // Allow correction and resubmit
            _ => false
        };
    }

    /// <summary>
    /// Calculates manning status availability percentage based on scheduled manpower and losses.
    /// </summary>
    /// <param name="request">Manning status input data</param>
    /// <returns>Calculated availability metrics</returns>
    [HttpPost("manning-calculation")]
    public IActionResult CalculateManningStatus([FromBody] ManningStatusRequest request)
    {
        // Validate input values to prevent negative numbers
        if (request.TotalScheduledManpower < 0)
            return BadRequest(new { message = "Total scheduled manpower cannot be negative", field = "TotalScheduledManpower" });
        if (request.SickLeave < 0)
            return BadRequest(new { message = "Sick leave cannot be negative", field = "SickLeave" });
        if (request.Absent < 0)
            return BadRequest(new { message = "Absent cannot be negative", field = "Absent" });
        if (request.Vacation < 0)
            return BadRequest(new { message = "Vacation cannot be negative", field = "Vacation" });
        if (request.Training < 0)
            return BadRequest(new { message = "Training cannot be negative", field = "Training" });
        if (request.BorrowedManpower < 0)
            return BadRequest(new { message = "Borrowed manpower cannot be negative", field = "BorrowedManpower" });

        var totalScheduledManpower = request.TotalScheduledManpower;
        var sickLeave = request.SickLeave;
        var absent = request.Absent;
        var vacation = request.Vacation;
        var training = request.Training;
        var borrowedManpower = request.BorrowedManpower;

        var totalLostManpower = sickLeave + absent + vacation + training;
        var totalAvailableManpower = Math.Max(0, totalScheduledManpower + borrowedManpower - totalLostManpower);
        var totalEffectiveManpower = totalScheduledManpower + borrowedManpower;
        var availability = totalEffectiveManpower > 0 ? Math.Round((double)totalAvailableManpower / totalEffectiveManpower * 100) : 0;

        return Ok(new
        {
            totalAvailableManpower,
            totalLostManpower,
            availability
        });
    }

    /// <summary>
    /// Creates a new handover logbook entry.
    /// Only Team Leaders assigned to a Hangar or Shop can create handovers.
    /// Hangar team leaders must use Hangar template and include at least one aircraft.
    /// Shop team leaders must use Shop template.
    /// </summary>
    /// <param name="request">Handover creation request</param>
    /// <returns>Created handover ID and status</returns>
    [HttpPost]
    [EnableRateLimiting("HandoverCreation")]
    public async Task<IActionResult> CreateHandover([FromBody] CreateHandoverRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        // Template validation: Hangar team leaders can only use Hangar template, Shop team leaders can only use Shop template
        var currentUser = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

        if (currentUser is null) return Unauthorized();

        // User must be a Team Leader assigned to either a hangar or shop to create handovers
        if (currentUser.Role != UserRole.TeamLeader)
        {
            return BadRequest(new { message = "Only Team Leaders can create handovers", userRole = currentUser.Role.ToString() });
        }

        bool hasHangar = currentUser.HangarId.HasValue;
        bool hasShop = currentUser.ShopId.HasValue;

        // User must be assigned to at least a hangar or shop
        if (!hasHangar && !hasShop)
        {
            return BadRequest(new { message = "You must be assigned to a hangar or shop to create handovers", hangarId = currentUser.HangarId, shopId = currentUser.ShopId });
        }

        // Template type must match workspace assignment
        if (request.TemplateType == HandoverTemplateType.Hangar && !hasHangar)
        {
            return BadRequest(new { message = "You must be assigned to a hangar to create Hangar handovers", templateType = request.TemplateType.ToString() });
        }

        if (request.TemplateType == HandoverTemplateType.Shop && !hasShop)
        {
            return BadRequest(new { message = "You must be assigned to a shop to create Shop handovers", templateType = request.TemplateType.ToString() });
        }

        // Validate aircraft types for Hangar template
        if (request.TemplateType == HandoverTemplateType.Hangar)
        {
            if (request.Aircrafts == null || request.Aircrafts.Count == 0)
            {
                return BadRequest(new { message = "Hangar handovers must include at least one aircraft" });
            }

            var validAircraftTypes = new[] { "B777", "A350", "B767", "B787", "B737", "Q400" };
            foreach (var aircraft in request.Aircrafts)
            {
                if (!validAircraftTypes.Contains(aircraft.AircraftType))
                {
                    return BadRequest(new { 
                        message = $"Invalid aircraft type: {aircraft.AircraftType}. Must be one of: {string.Join(", ", validAircraftTypes)}",
                        aircraftType = aircraft.AircraftType
                    });
                }
            }
        }

        var handover = new Handover
        {
            TemplateType = request.TemplateType,
            Status = HandoverStatus.Draft,
            Date = request.Date,
            ShiftType = Enum.Parse<ShiftType>(request.ShiftType),
            DutyTeamLeaderName = request.DutyTeamLeaderName,
            SectionId = currentUser.SectionId ?? Guid.Empty,
            HangarId = currentUser.HangarId,
            ShopId = currentUser.ShopId,
            OutgoingTeamLeaderId = userId.Value
        };

        dbContext.Handovers.Add(handover);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Create ManningStatus after handover is saved to get the HandoverId
        var manningStatus = new HandoverManningStatus
        {
            HandoverId = handover.Id,
            TotalScheduledManpower = request.ManningStatus.TotalScheduledManpower,
            SickLeave = request.ManningStatus.SickLeave,
            Absent = request.ManningStatus.Absent,
            Vacation = request.ManningStatus.Vacation,
            Training = request.ManningStatus.Training,
            BorrowedManpower = request.ManningStatus.BorrowedManpower,
            TotalAvailableManpower = request.ManningStatus.TotalAvailableManpower,
            TotalLostManpower = request.ManningStatus.TotalLostManpower,
            AvailabilityPercentage = request.ManningStatus.AvailabilityPercentage
        };

        dbContext.HandoverManningStatuses.Add(manningStatus);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Add aircrafts if provided (for Hangar template)
        if (request.Aircrafts != null && request.Aircrafts.Count > 0)
        {
            foreach (var aircraftDto in request.Aircrafts)
            {
                dbContext.HandoverAircrafts.Add(new HandoverAircraft
                {
                    HandoverId = handover.Id,
                    AircraftType = aircraftDto.AircraftType,
                    AircraftRegistration = aircraftDto.AircraftRegistration,
                    MaintenanceType = aircraftDto.MaintenanceType,
                    MaintenanceStartTime = aircraftDto.MaintenanceStartTime,
                    MaintenanceEndTime = aircraftDto.MaintenanceEndTime
                });
            }
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var correlationId = HttpContext.TraceIdentifier;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await auditService.WriteAsync(userId, "Create", "Handover", handover.Id.ToString(), null, new { TemplateType = handover.TemplateType, Status = handover.Status, Date = handover.Date }, false, ipAddress, correlationId, cancellationToken);

        return Ok(new { id = handover.Id, status = handover.Status });
    }

    /// <summary>
    /// Retrieves a specific handover by ID with all related entities.
    /// Users can only view handovers within their section/hangar/shop.
    /// </summary>
    /// <param name="id">Handover ID</param>
    /// <returns>Complete handover details including tasks, defects, issues, signatures, and audit logs</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetHandover(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var handover = await dbContext.Handovers
            .Include(h => h.Signatures)
            .Include(h => h.Tasks)
            .Include(h => h.Defects)
            .Include(h => h.Issues)
            .Include(h => h.WorkStatuses)
            .Include(h => h.ManningStatus)
            .Include(h => h.Aircrafts)
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (handover is null) return NotFound();

        // RBAC: User can only view handovers in their section/hangar/shop
        var currentUser = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

        if (currentUser is null) return Unauthorized();

        if (currentUser.Role == UserRole.Employee || currentUser.Role == UserRole.TeamLeader)
        {
            // User can view if handover matches their section OR hangar OR shop
            var hasAccess = (currentUser.SectionId.HasValue && handover.SectionId == currentUser.SectionId.Value) ||
                           (currentUser.HangarId.HasValue && handover.HangarId == currentUser.HangarId.Value) ||
                           (currentUser.ShopId.HasValue && handover.ShopId == currentUser.ShopId.Value);
            
            if (!hasAccess) return Forbid();
        }
        else if (currentUser.Role == UserRole.Manager)
        {
            if (!currentUser.SectionId.HasValue || handover.SectionId != currentUser.SectionId.Value) return Forbid();
        }

        // Project to DTO to avoid JSON serialization cycles
        var dto = new
        {
            id = handover.Id,
            templateType = (int)handover.TemplateType,
            status = (int)handover.Status,
            date = handover.Date,
            shiftType = handover.ShiftType,
            sectionId = handover.SectionId,
            hangarId = handover.HangarId,
            shopId = handover.ShopId,
            outgoingTeamLeaderId = handover.OutgoingTeamLeaderId,
            incomingTeamLeaderId = handover.IncomingTeamLeaderId,
            submittedAt = handover.SubmittedAt,
            acceptedAt = handover.AcceptedAt,
            createdAt = handover.CreatedAt,
            createdBy = handover.CreatedBy,
            updatedAt = handover.UpdatedAt,
            updatedBy = handover.UpdatedBy,
            rowVersion = Convert.ToBase64String(handover.Version),
            generalInfo = new
            {
                dutyTeamLeaderName = handover.DutyTeamLeaderName
            },
            manningStatus = handover.ManningStatus == null ? null : new
            {
                id = handover.ManningStatus.Id,
                handoverId = handover.ManningStatus.HandoverId,
                totalScheduledManpower = handover.ManningStatus.TotalScheduledManpower,
                sickLeave = handover.ManningStatus.SickLeave,
                absent = handover.ManningStatus.Absent,
                vacation = handover.ManningStatus.Vacation,
                training = handover.ManningStatus.Training,
                borrowedManpower = handover.ManningStatus.BorrowedManpower,
                totalAvailableManpower = handover.ManningStatus.TotalAvailableManpower,
                totalLostManpower = handover.ManningStatus.TotalLostManpower,
                availabilityPercentage = handover.ManningStatus.AvailabilityPercentage
            },
            signatures = handover.Signatures.Select(s => new
            {
                id = s.Id,
                handoverId = s.HandoverId,
                userId = s.UserId,
                employeeId = dbContext.Users.Where(u => u.Id == s.UserId).Select(u => u.EmployeeId).FirstOrDefault(),
                signatureRole = (int)s.SignatureRole == 1 ? "Outgoing" : (int)s.SignatureRole == 2 ? "Incoming" : "Employee",
                signatureData = s.SignatureData,
                signatureName = s.SignatureName,
                signedAt = s.SignedAt
            }),
            tasks = handover.Tasks.Select(t => new
            {
                id = t.Id,
                handoverId = t.HandoverId,
                taskType = (int)t.TaskType,
                aircraftRegistration = t.AircraftRegistration,
                taskCardCode = t.TaskCardCode,
                description = t.Description,
                createdByUserId = t.CreatedByUserId,
                createdAt = t.CreatedAt,
                createdByUserName = dbContext.Users.Where(u => u.Id == t.CreatedByUserId).Select(u => u.FullName).FirstOrDefault()
            }),
            defects = handover.Defects.Select(d => new
            {
                id = d.Id,
                handoverId = d.HandoverId,
                aircraftRegistration = d.AircraftRegistration,
                defectDescription = d.DefectDescription,
                nonRoutineCardNumber = d.NonRoutineCardNumber,
                defectLoginTime = d.DefectLoginTime,
                itemStatus = d.ItemStatus
            }),
            issues = handover.Issues.Select(i => new
            {
                id = i.Id,
                handoverId = i.HandoverId,
                issueType = (int)i.IssueType == 1 ? "Tools" : (int)i.IssueType == 2 ? "Equipment" : (int)i.IssueType == 3 ? "Parts" : "Other",
                description = i.Description,
                createdBy = i.CreatedBy,
                createdAt = i.CreatedAt,
                createdByUserName = dbContext.Users.Where(u => u.Id == i.CreatedBy).Select(u => u.FullName).FirstOrDefault()
            }),
            workStatuses = handover.WorkStatuses.Select(w => new
            {
                id = w.Id,
                handoverId = w.HandoverId,
                mfgPartNumber = w.MfgPartNumber,
                serialNumber = w.SerialNumber,
                workCarriedOut = w.WorkCarriedOut,
                workToBeDone = w.WorkToBeDone,
                outstandingIssue = w.OutstandingIssue
            }),
            aircrafts = handover.Aircrafts.Select(a => new
            {
                id = a.Id,
                handoverId = a.HandoverId,
                aircraftType = a.AircraftType,
                aircraftRegistration = a.AircraftRegistration,
                maintenanceType = a.MaintenanceType,
                maintenanceStartTime = a.MaintenanceStartTime,
                maintenanceEndTime = a.MaintenanceEndTime
            }),
            auditLogs = dbContext.AuditLogs
                .Where(a => a.EntityName == "Handover" && a.EntityId == handover.Id.ToString())
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    id = a.Id,
                    action = a.Action,
                    performedByUserId = a.UserId,
                    timestamp = a.CreatedAt,
                    oldValue = a.BeforeValues,
                    newValue = a.AfterValues,
                    ipAddress = a.IpAddress,
                    correlationId = a.CorrelationId
                }).ToList()
        };

        return Ok(dto);
    }

    /// <summary>
    /// Lists handovers with pagination and filtering.
    /// Supports filtering by status and date range.
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="dateFrom">Optional start date filter</param>
    /// <param name="dateTo">Optional end date filter</param>
    /// <returns>Paginated list of handovers</returns>
    [HttpGet]
    [ResponseCache(Duration = 30)]
    public async Task<IActionResult> ListHandovers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] HandoverStatus? status = null,
        [FromQuery] Guid? sectionId = null,
        [FromQuery] Guid? hangarId = null,
        [FromQuery] Guid? shopId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var currentUser = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

        if (currentUser is null) return Unauthorized();

        var query = dbContext.Handovers
            .Include(h => h.OutgoingTeamLeader)
            .Include(h => h.IncomingTeamLeader)
            .AsNoTracking();

        // RBAC filtering
        if (currentUser.Role == UserRole.Employee || currentUser.Role == UserRole.TeamLeader)
        {
            // Employees/TeamLeaders see handovers in their section OR hangar OR shop
            var currentSectionId = currentUser.SectionId;
            var currentHangarId = currentUser.HangarId;
            var currentShopId = currentUser.ShopId;

            Console.WriteLine($"[HandoverController] User {currentUser.Id} ({currentUser.Role}) - SectionId: {currentSectionId}, HangarId: {currentHangarId}, ShopId: {currentShopId}");

            // If user has no assignments, return empty result
            if (!currentSectionId.HasValue && !currentHangarId.HasValue && !currentShopId.HasValue)
            {
                query = query.Where(h => false);
            }
            else
            {
                query = query.Where(h =>
                    (currentSectionId.HasValue && h.SectionId == currentSectionId.Value) ||
                    (currentHangarId.HasValue && h.HangarId == currentHangarId.Value) ||
                    (currentShopId.HasValue && h.ShopId == currentShopId.Value)
                );
            }
        }
        else if (currentUser.Role == UserRole.Manager)
        {
            if (currentUser.SectionId.HasValue)
            {
                query = query.Where(h => h.SectionId == currentUser.SectionId.Value);
            }
            else
            {
                query = query.Where(h => false);
            }
        }
        // Directors can see all handovers

        // Additional filters
        if (status.HasValue)
            query = query.Where(h => h.Status == status.Value);
        if (sectionId.HasValue)
            query = query.Where(h => h.SectionId == sectionId.Value);
        if (hangarId.HasValue)
            query = query.Where(h => h.HangarId == hangarId.Value);
        if (shopId.HasValue)
            query = query.Where(h => h.ShopId == shopId.Value);
        if (dateFrom.HasValue)
            query = query.Where(h => h.Date >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(h => h.Date <= dateTo.Value);

        var total = await query.CountAsync(cancellationToken);
        Console.WriteLine($"[HandoverController] Total handovers after filtering: {total}");

        var items = await query
            .OrderByDescending(h => h.Date)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        Console.WriteLine($"[HandoverController] Returning {items.Count} handovers");
        foreach (var item in items)
        {
            Console.WriteLine($"[HandoverController] Handover {item.Id} - SectionId: {item.SectionId}, HangarId: {item.HangarId}, ShopId: {item.ShopId}, Status: {item.Status}");
        }

        // Project to DTO to avoid JSON serialization cycles
        var dtoItems = items.Select(h => new
        {
            id = h.Id,
            templateType = (int)h.TemplateType,
            status = (int)h.Status,
            date = h.Date,
            shiftType = h.ShiftType,
            dutyTeamLeaderName = h.DutyTeamLeaderName,
            sectionId = h.SectionId,
            hangarId = h.HangarId,
            shopId = h.ShopId,
            outgoingTeamLeaderId = h.OutgoingTeamLeaderId,
            incomingTeamLeaderId = h.IncomingTeamLeaderId,
            submittedAt = h.SubmittedAt,
            acceptedAt = h.AcceptedAt,
            createdAt = h.CreatedAt,
            createdBy = h.CreatedBy,
            createdByUserName = h.OutgoingTeamLeader?.FullName,
            incomingTeamLeaderUserName = h.IncomingTeamLeader?.FullName,
            updatedAt = h.UpdatedAt,
            updatedBy = h.UpdatedBy
        });

        return Ok(new { items = dtoItems, total, pageNumber, pageSize });
    }

    /// <summary>
    /// Updates an existing draft handover.
    /// Only the creator (outgoing team leader) can edit draft handovers.
    /// Requires rowVersion for optimistic concurrency control.
    /// </summary>
    /// <param name="id">Handover ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated handover ID and status</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateHandover(Guid id, [FromBody] UpdateHandoverRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var handover = await dbContext.Handovers
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (handover is null) return NotFound();

        // Validate rowVersion for optimistic concurrency
        if (!string.IsNullOrEmpty(request.RowVersion))
        {
            var providedVersion = Convert.FromBase64String(request.RowVersion);
            if (!handover.Version.SequenceEqual(providedVersion))
                return Conflict("Handover was modified by another user. Please refresh and try again.");
        }

        // Only creator can edit draft
        if (handover.OutgoingTeamLeaderId != userId.Value) return Forbid();
        if (handover.Status != HandoverStatus.Draft) return BadRequest("Can only edit draft handovers");

        handover.Date = request.Date;
        handover.ShiftType = request.ShiftType;
        handover.DutyTeamLeaderName = request.DutyTeamLeaderName;

        if (request.ManningStatus is not null)
        {
            var manningStatus = await dbContext.HandoverManningStatuses
                .FirstOrDefaultAsync(ms => ms.HandoverId == handover.Id, cancellationToken);
            
            if (manningStatus is not null)
            {
                manningStatus.TotalScheduledManpower = request.ManningStatus.TotalScheduledManpower;
                manningStatus.SickLeave = request.ManningStatus.SickLeave;
                manningStatus.Absent = request.ManningStatus.Absent;
                manningStatus.Vacation = request.ManningStatus.Vacation;
                manningStatus.Training = request.ManningStatus.Training;
                manningStatus.BorrowedManpower = request.ManningStatus.BorrowedManpower;
                manningStatus.TotalAvailableManpower = request.ManningStatus.TotalAvailableManpower;
                manningStatus.TotalLostManpower = request.ManningStatus.TotalLostManpower;
                manningStatus.AvailabilityPercentage = request.ManningStatus.AvailabilityPercentage;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var correlationId = HttpContext.TraceIdentifier;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await auditService.WriteAsync(userId, "Update", "Handover", handover.Id.ToString(), null, new { Date = handover.Date, ShiftType = handover.ShiftType, DutyTeamLeaderName = handover.DutyTeamLeaderName }, false, ipAddress, correlationId, cancellationToken);

        return Ok(new { id = handover.Id, status = handover.Status });
    }

    /// <summary>
    /// Soft deletes a draft handover.
    /// Only the creator (outgoing team leader) can delete draft handovers.
    /// </summary>
    /// <param name="id">Handover ID</param>
    /// <returns>Confirmation of deletion</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHandover(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var handover = await dbContext.Handovers
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (handover is null) return NotFound();

        // Only creator can delete draft handovers
        if (handover.OutgoingTeamLeaderId != userId.Value) return Forbid();
        if (handover.Status != HandoverStatus.Draft) return BadRequest("Can only delete draft handovers");

        // Soft delete
        handover.IsDeleted = true;
        handover.DeletedAt = DateTime.UtcNow;
        handover.DeletedBy = userId.Value;

        await dbContext.SaveChangesAsync(cancellationToken);

        var correlationId = HttpContext.TraceIdentifier;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await auditService.WriteAsync(userId, "Delete", "Handover", handover.Id.ToString(), new { status = handover.Status }, new { isDeleted = true }, false, ipAddress, correlationId, cancellationToken);

        return Ok(new { id = handover.Id, message = "Handover deleted successfully" });
    }

    /// <summary>
    /// Submits a draft handover for review, changing status to Pending.
    /// Only the creator (outgoing team leader) can submit draft handovers.
    /// </summary>
    /// <param name="id">Handover ID</param>
    /// <returns>Submitted handover ID and status</returns>
    [HttpPost("{id}/submit")]
    public async Task<IActionResult> SubmitHandover(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var handover = await dbContext.Handovers
            .Include(h => h.Signatures)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (handover is null) return NotFound();

        // Only creator can submit
        if (handover.OutgoingTeamLeaderId != userId.Value) return Forbid();
        if (handover.Status != HandoverStatus.Draft) return BadRequest("Can only submit draft handovers");

        // Validate state transition
        if (!IsValidStateTransition(handover.Status, HandoverStatus.Pending))
            return BadRequest("Invalid state transition");

        // Check for outgoing signature
        var outgoingSignature = handover.Signatures.FirstOrDefault(s => s.SignatureRole == SignatureRole.Outgoing);
        if (outgoingSignature is null)
            return BadRequest("Outgoing signature required before submission");

        handover.Status = HandoverStatus.Pending;
        handover.SubmittedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        var correlationId = HttpContext.TraceIdentifier;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await auditService.WriteAsync(userId, "Submit", "Handover", handover.Id.ToString(), new { status = "Draft" }, new { status = "Pending" }, false, ipAddress, correlationId, cancellationToken);

        return Ok(new { id = handover.Id, status = handover.Status });
    }

    /// <summary>
    /// Accepts a pending handover, changing status to Accepted.
    /// Only the incoming team leader in the same hangar/shop can accept.
    /// Requires signature and rowVersion for concurrency control.
    /// </summary>
    /// <param name="id">Handover ID</param>
    /// <param name="request">Accept request with signature</param>
    /// <returns>Accepted handover ID and status</returns>
    [HttpPost("{id}/accept")]
    public async Task<IActionResult> AcceptHandover(Guid id, [FromBody] AcceptHandoverRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        // Load entity for validation and concurrency control
        var handover = await dbContext.Handovers
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (handover is null) return NotFound();

        // Validate rowVersion for optimistic concurrency
        if (!string.IsNullOrEmpty(request.RowVersion))
        {
            var providedVersion = Convert.FromBase64String(request.RowVersion);
            if (!handover.Version.SequenceEqual(providedVersion))
                return Conflict("Handover was modified by another user. Please refresh and try again.");
        }

        // Check for duplicate incoming signature
        var existingSignature = await dbContext.HandoverSignatures
            .FirstOrDefaultAsync(s => s.HandoverId == id && s.SignatureRole == SignatureRole.Incoming, cancellationToken);
        if (existingSignature != null)
            return BadRequest("Incoming signature already exists for this handover");

        // Only incoming team leader in same hangar/shop can accept
        var currentUser = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

        if (currentUser is null) return Unauthorized();
        if (currentUser.Role != UserRole.TeamLeader) return Forbid();

        if (handover.HangarId.HasValue && currentUser.HangarId != handover.HangarId) return Forbid();
        if (handover.ShopId.HasValue && currentUser.ShopId != handover.ShopId) return Forbid();

        if (handover.Status != HandoverStatus.Pending) return BadRequest("Can only accept pending handovers");

        // Validate state transition
        if (!IsValidStateTransition(handover.Status, HandoverStatus.Accepted))
            return BadRequest("Invalid state transition");

        // Validate signature data
        if (!IsValidSignatureData(request.SignatureData))
            return BadRequest("Invalid signature data");

        // Add signature
        var signature = new HandoverSignature
        {
            HandoverId = handover.Id,
            UserId = userId.Value,
            SignatureRole = SignatureRole.Incoming,
            SignatureData = request.SignatureData,
            SignatureName = request.SignatureName,
            SignedAt = DateTime.UtcNow
        };
        dbContext.HandoverSignatures.Add(signature);

        // Update handover status with proper concurrency control
        handover.Status = HandoverStatus.Accepted;
        handover.AcceptedAt = DateTime.UtcNow;
        handover.IncomingTeamLeaderId = userId.Value;

        await dbContext.SaveChangesAsync(cancellationToken);

        var correlationId = HttpContext.TraceIdentifier;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await auditService.WriteAsync(userId, "Accept", "Handover", handover.Id.ToString(), new { status = "Pending" }, new { status = "Accepted", incomingTeamLeaderId = userId.Value }, false, ipAddress, correlationId, cancellationToken);

        return Ok(new { id = handover.Id, status = HandoverStatus.Accepted });
    }

    /// <summary>
    /// Rejects a pending handover, changing status to Rejected.
    /// Only the incoming team leader in the same hangar/shop can reject.
    /// Requires signature, rejection reason, and rowVersion for concurrency control.
    /// </summary>
    /// <param name="id">Handover ID</param>
    /// <param name="request">Reject request with signature and reason</param>
    /// <returns>Rejected handover ID and status</returns>
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> RejectHandover(Guid id, [FromBody] RejectHandoverRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        // Load entity for validation and concurrency control
        var handover = await dbContext.Handovers
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (handover is null) return NotFound();

        // Validate rowVersion for optimistic concurrency
        if (!string.IsNullOrEmpty(request.RowVersion))
        {
            var providedVersion = Convert.FromBase64String(request.RowVersion);
            if (!handover.Version.SequenceEqual(providedVersion))
                return Conflict("Handover was modified by another user. Please refresh and try again.");
        }

        // Check for duplicate incoming signature
        var existingSignature = await dbContext.HandoverSignatures
            .FirstOrDefaultAsync(s => s.HandoverId == id && s.SignatureRole == SignatureRole.Incoming, cancellationToken);
        if (existingSignature != null)
            return BadRequest("Incoming signature already exists for this handover");

        // Only incoming team leader in same hangar/shop can reject
        var currentUser = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

        if (currentUser is null) return Unauthorized();
        if (currentUser.Role != UserRole.TeamLeader) return Forbid();

        if (handover.HangarId.HasValue && currentUser.HangarId != handover.HangarId) return Forbid();
        if (handover.ShopId.HasValue && currentUser.ShopId != handover.ShopId) return Forbid();

        if (handover.Status != HandoverStatus.Pending) return BadRequest("Can only reject pending handovers");

        // Validate state transition
        if (!IsValidStateTransition(handover.Status, HandoverStatus.Rejected))
            return BadRequest("Invalid state transition");

        // Validate signature data
        if (!IsValidSignatureData(request.SignatureData))
            return BadRequest("Invalid signature data");

        // Add signature
        var signature = new HandoverSignature
        {
            HandoverId = handover.Id,
            UserId = userId.Value,
            SignatureRole = SignatureRole.Incoming,
            SignatureData = request.SignatureData,
            SignatureName = request.SignatureName,
            SignedAt = DateTime.UtcNow
        };
        dbContext.HandoverSignatures.Add(signature);

        // Update handover status with proper concurrency control
        handover.Status = HandoverStatus.Rejected;
        handover.IncomingTeamLeaderId = userId.Value;

        await dbContext.SaveChangesAsync(cancellationToken);

        var correlationId = HttpContext.TraceIdentifier;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await auditService.WriteAsync(userId, "Reject", "Handover", handover.Id.ToString(), new { status = "Pending" }, new { status = "Rejected", rejectionReason = request.RejectionReason, incomingTeamLeaderId = userId.Value }, false, ipAddress, correlationId, cancellationToken);

        return Ok(new { id = handover.Id, status = HandoverStatus.Rejected });
    }

    [HttpPost("{id}/signature")]
    public async Task<IActionResult> AddSignature(Guid id, [FromBody] AddSignatureRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var handover = await dbContext.Handovers
            .Include(h => h.Signatures)
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (handover is null) return NotFound();

        // Only creator can add outgoing signature to draft
        if (handover.OutgoingTeamLeaderId != userId.Value) return Forbid();
        if (handover.Status != HandoverStatus.Draft) return BadRequest("Can only add signature to draft handovers");

        // Validate signature data
        if (!IsValidSignatureData(request.SignatureData))
            return BadRequest("Invalid signature data");

        // Check if signature already exists for this role
        if (handover.Signatures.Any(s => s.SignatureRole == request.SignatureRole))
            return BadRequest("Signature for this role already exists");

        // Create signature as a new entity (not attached to tracked handover)
        var signature = new HandoverSignature
        {
            HandoverId = handover.Id,
            UserId = userId.Value,
            SignatureRole = request.SignatureRole,
            SignatureData = request.SignatureData,
            SignatureName = request.SignatureName,
            SignedAt = DateTime.UtcNow
        };

        dbContext.HandoverSignatures.Add(signature);
        await dbContext.SaveChangesAsync(cancellationToken);

        var correlationId = HttpContext.TraceIdentifier;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await auditService.WriteAsync(userId, "AddSignature", "HandoverSignature", handover.Id.ToString(), null, new { request.SignatureRole, request.SignatureName }, false, ipAddress, correlationId, cancellationToken);

        return Ok(new { id = handover.Id });
    }

    [HttpPost("{id}/tasks")]
    public async Task<IActionResult> AddTask(Guid id, [FromBody] AddTaskRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var handover = await dbContext.Handovers
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (handover is null) return NotFound();

        // Only creator can add tasks to draft
        if (handover.OutgoingTeamLeaderId != userId.Value) return Forbid();
        if (handover.Status != HandoverStatus.Draft) return BadRequest(new { message = "Can only add tasks to draft handovers", status = handover.Status.ToString() });

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new { message = "Task description is required", description = request.Description });
        }

        var task = new HandoverTask
        {
            HandoverId = handover.Id,
            TaskType = request.TaskType,
            AircraftRegistration = request.AircraftRegistration,
            TaskCardCode = request.TaskCardCode,
            Description = request.Description,
            CreatedByUserId = userId.Value
        };

        dbContext.HandoverTasks.Add(task);
        await dbContext.SaveChangesAsync(cancellationToken);

        var correlationId = HttpContext.TraceIdentifier;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await auditService.WriteAsync(userId, "AddTask", "HandoverTask", handover.Id.ToString(), null, new { request.TaskType, request.AircraftRegistration, request.Description }, false, ipAddress, correlationId, cancellationToken);

        return Ok(new { id = handover.Id });
    }

    [HttpPut("{id}/tasks/{taskId}")]
    public async Task<IActionResult> UpdateTask(Guid id, Guid taskId, [FromBody] UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var handover = await dbContext.Handovers
            .Include(h => h.Tasks)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (handover is null) return NotFound();

        // Only creator can update tasks in draft
        if (handover.OutgoingTeamLeaderId != userId.Value) return Forbid();
        if (handover.Status != HandoverStatus.Draft) return BadRequest("Can only update tasks in draft handovers");

        var task = handover.Tasks.FirstOrDefault(t => t.Id == taskId);
        if (task is null) return NotFound();

        task.TaskType = request.TaskType;
        task.AircraftRegistration = request.AircraftRegistration;
        task.TaskCardCode = request.TaskCardCode;
        task.Description = request.Description;

        await dbContext.SaveChangesAsync(cancellationToken);

        var correlationId = HttpContext.TraceIdentifier;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await auditService.WriteAsync(userId, "UpdateTask", "HandoverTask", taskId.ToString(), null, new { request.TaskType, request.AircraftRegistration, request.Description }, false, ipAddress, correlationId, cancellationToken);

        return Ok(new { id = handover.Id });
    }

    [HttpDelete("{id}/tasks/{taskId}")]
    public async Task<IActionResult> DeleteTask(Guid id, Guid taskId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var handover = await dbContext.Handovers
            .Include(h => h.Tasks)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (handover is null) return NotFound();

        // Only creator can delete tasks from draft
        if (handover.OutgoingTeamLeaderId != userId.Value) return Forbid();
        if (handover.Status != HandoverStatus.Draft) return BadRequest("Can only delete tasks from draft handovers");

        var task = handover.Tasks.FirstOrDefault(t => t.Id == taskId);
        if (task is null) return NotFound();

        dbContext.HandoverTasks.Remove(task);
        await dbContext.SaveChangesAsync(cancellationToken);

        var correlationId = HttpContext.TraceIdentifier;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await auditService.WriteAsync(userId, "DeleteTask", "HandoverTask", taskId.ToString(), null, null, false, ipAddress, correlationId, cancellationToken);

        return Ok(new { id = handover.Id });
    }

    [HttpPost("{id}/defects")]
    public async Task<IActionResult> AddDefect(Guid id, [FromBody] AddDefectRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var handover = await dbContext.Handovers
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (handover is null) return NotFound();

        // Only creator can add defects to draft
        if (handover.OutgoingTeamLeaderId != userId.Value) return Forbid();
        if (handover.Status != HandoverStatus.Draft) return BadRequest("Can only add defects to draft handovers");

        var defect = new HandoverDefect
        {
            HandoverId = handover.Id,
            AircraftRegistration = request.AircraftRegistration,
            DefectDescription = request.DefectDescription,
            NonRoutineCardNumber = request.NonRoutineCardNumber,
            DefectLoginTime = request.DefectLoginTime,
            ItemStatus = request.ItemStatus
        };

        dbContext.HandoverDefects.Add(defect);
        await dbContext.SaveChangesAsync(cancellationToken);

        var correlationId = HttpContext.TraceIdentifier;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await auditService.WriteAsync(userId, "AddDefect", "HandoverDefect", handover.Id.ToString(), null, new { request.AircraftRegistration, request.DefectDescription }, false, ipAddress, correlationId, cancellationToken);

        return Ok(new { id = handover.Id });
    }

    [HttpPut("{id}/defects/{defectId}")]
    public async Task<IActionResult> UpdateDefect(Guid id, Guid defectId, [FromBody] UpdateDefectRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var handover = await dbContext.Handovers
            .Include(h => h.Defects)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (handover is null) return NotFound();

        // Only creator can update defects in draft
        if (handover.OutgoingTeamLeaderId != userId.Value) return Forbid();
        if (handover.Status != HandoverStatus.Draft) return BadRequest("Can only update defects in draft handovers");

        var defect = handover.Defects.FirstOrDefault(d => d.Id == defectId);
        if (defect is null) return NotFound();

        defect.AircraftRegistration = request.AircraftRegistration;
        defect.DefectDescription = request.DefectDescription;
        defect.NonRoutineCardNumber = request.NonRoutineCardNumber;
        defect.DefectLoginTime = request.DefectLoginTime;
        defect.ItemStatus = request.ItemStatus;

        await dbContext.SaveChangesAsync(cancellationToken);

        var correlationId = HttpContext.TraceIdentifier;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await auditService.WriteAsync(userId, "UpdateDefect", "HandoverDefect", defectId.ToString(), null, new { request.AircraftRegistration, request.DefectDescription }, false, ipAddress, correlationId, cancellationToken);

        return Ok(new { id = handover.Id });
    }

    [HttpDelete("{id}/defects/{defectId}")]
    public async Task<IActionResult> DeleteDefect(Guid id, Guid defectId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var handover = await dbContext.Handovers
            .Include(h => h.Defects)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (handover is null) return NotFound();

        // Only creator can delete defects from draft
        if (handover.OutgoingTeamLeaderId != userId.Value) return Forbid();
        if (handover.Status != HandoverStatus.Draft) return BadRequest("Can only delete defects from draft handovers");

        var defect = handover.Defects.FirstOrDefault(d => d.Id == defectId);
        if (defect is null) return NotFound();

        dbContext.HandoverDefects.Remove(defect);
        await dbContext.SaveChangesAsync(cancellationToken);

        var correlationId = HttpContext.TraceIdentifier;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await auditService.WriteAsync(userId, "DeleteDefect", "HandoverDefect", defectId.ToString(), null, null, false, ipAddress, correlationId, cancellationToken);

        return Ok(new { id = handover.Id });
    }

    [HttpPost("{id}/issues")]
    public async Task<IActionResult> AddIssue(Guid id, [FromBody] AddIssueRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var handover = await dbContext.Handovers
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (handover is null) return NotFound();

        // Only creator can add issues to draft
        if (handover.OutgoingTeamLeaderId != userId.Value) return Forbid();
        if (handover.Status != HandoverStatus.Draft) return BadRequest("Can only add issues to draft handovers");

        var issue = new HandoverIssue
        {
            HandoverId = handover.Id,
            IssueType = request.IssueType,
            Description = request.Description
        };

        dbContext.HandoverIssues.Add(issue);
        await dbContext.SaveChangesAsync(cancellationToken);

        var correlationId = HttpContext.TraceIdentifier;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await auditService.WriteAsync(userId, "AddIssue", "HandoverIssue", handover.Id.ToString(), null, new { request.IssueType, request.Description }, false, ipAddress, correlationId, cancellationToken);

        return Ok(new { id = handover.Id });
    }

    [HttpPut("{id}/issues/{issueId}")]
    public async Task<IActionResult> UpdateIssue(Guid id, Guid issueId, [FromBody] UpdateIssueRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var handover = await dbContext.Handovers
            .Include(h => h.Issues)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (handover is null) return NotFound();

        // Only creator can update issues in draft
        if (handover.OutgoingTeamLeaderId != userId.Value) return Forbid();
        if (handover.Status != HandoverStatus.Draft) return BadRequest("Can only update issues in draft handovers");

        var issue = handover.Issues.FirstOrDefault(i => i.Id == issueId);
        if (issue is null) return NotFound();

        issue.IssueType = request.IssueType;
        issue.Description = request.Description;

        await dbContext.SaveChangesAsync(cancellationToken);

        var correlationId = HttpContext.TraceIdentifier;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await auditService.WriteAsync(userId, "UpdateIssue", "HandoverIssue", issueId.ToString(), null, new { request.IssueType, request.Description }, false, ipAddress, correlationId, cancellationToken);

        return Ok(new { id = handover.Id });
    }

    [HttpDelete("{id}/issues/{issueId}")]
    public async Task<IActionResult> DeleteIssue(Guid id, Guid issueId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var handover = await dbContext.Handovers
            .Include(h => h.Issues)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (handover is null) return NotFound();

        // Only creator can delete issues from draft
        if (handover.OutgoingTeamLeaderId != userId.Value) return Forbid();
        if (handover.Status != HandoverStatus.Draft) return BadRequest("Can only delete issues from draft handovers");

        var issue = handover.Issues.FirstOrDefault(i => i.Id == issueId);
        if (issue is null) return NotFound();

        dbContext.HandoverIssues.Remove(issue);
        await dbContext.SaveChangesAsync(cancellationToken);

        var correlationId = HttpContext.TraceIdentifier;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await auditService.WriteAsync(userId, "DeleteIssue", "HandoverIssue", issueId.ToString(), null, null, false, ipAddress, correlationId, cancellationToken);

        return Ok(new { id = handover.Id });
    }

    [HttpPost("{id}/work-statuses")]
    public async Task<IActionResult> AddWorkStatus(Guid id, [FromBody] AddWorkStatusRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var handover = await dbContext.Handovers
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (handover is null) return NotFound();

        // Only creator can add work statuses to draft
        if (handover.OutgoingTeamLeaderId != userId.Value) return Forbid();
        if (handover.Status != HandoverStatus.Draft) return BadRequest("Can only add work statuses to draft handovers");

        var workStatus = new HandoverWorkStatus
        {
            HandoverId = handover.Id,
            MfgPartNumber = request.MfgPartNumber,
            SerialNumber = request.SerialNumber,
            WorkCarriedOut = request.WorkCarriedOut,
            WorkToBeDone = request.WorkToBeDone,
            OutstandingIssue = request.OutstandingIssue
        };

        dbContext.HandoverWorkStatuses.Add(workStatus);
        await dbContext.SaveChangesAsync(cancellationToken);

        var correlationId = HttpContext.TraceIdentifier;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await auditService.WriteAsync(userId, "AddWorkStatus", "HandoverWorkStatus", handover.Id.ToString(), null, new { request.MfgPartNumber, request.SerialNumber }, false, ipAddress, correlationId, cancellationToken);

        return Ok(new { id = handover.Id });
    }

    [HttpPut("{id}/work-statuses/{workStatusId}")]
    public async Task<IActionResult> UpdateWorkStatus(Guid id, Guid workStatusId, [FromBody] UpdateWorkStatusRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var handover = await dbContext.Handovers
            .Include(h => h.WorkStatuses)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (handover is null) return NotFound();

        // Only creator can update work statuses in draft
        if (handover.OutgoingTeamLeaderId != userId.Value) return Forbid();
        if (handover.Status != HandoverStatus.Draft) return BadRequest("Can only update work statuses in draft handovers");

        var workStatus = handover.WorkStatuses.FirstOrDefault(w => w.Id == workStatusId);
        if (workStatus is null) return NotFound();

        workStatus.MfgPartNumber = request.MfgPartNumber;
        workStatus.SerialNumber = request.SerialNumber;
        workStatus.WorkCarriedOut = request.WorkCarriedOut;
        workStatus.WorkToBeDone = request.WorkToBeDone;
        workStatus.OutstandingIssue = request.OutstandingIssue;

        await dbContext.SaveChangesAsync(cancellationToken);

        var correlationId = HttpContext.TraceIdentifier;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await auditService.WriteAsync(userId, "UpdateWorkStatus", "HandoverWorkStatus", workStatusId.ToString(), null, new { request.MfgPartNumber, request.SerialNumber }, false, ipAddress, correlationId, cancellationToken);

        return Ok(new { id = handover.Id });
    }

    [HttpDelete("{id}/work-statuses/{workStatusId}")]
    public async Task<IActionResult> DeleteWorkStatus(Guid id, Guid workStatusId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var handover = await dbContext.Handovers
            .Include(h => h.WorkStatuses)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (handover is null) return NotFound();

        // Only creator can delete work statuses from draft
        if (handover.OutgoingTeamLeaderId != userId.Value) return Forbid();
        if (handover.Status != HandoverStatus.Draft) return BadRequest("Can only delete work statuses from draft handovers");

        var workStatus = handover.WorkStatuses.FirstOrDefault(w => w.Id == workStatusId);
        if (workStatus is null) return NotFound();

        dbContext.HandoverWorkStatuses.Remove(workStatus);
        await dbContext.SaveChangesAsync(cancellationToken);

        var correlationId = HttpContext.TraceIdentifier;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await auditService.WriteAsync(userId, "DeleteWorkStatus", "HandoverWorkStatus", workStatusId.ToString(), null, null, false, ipAddress, correlationId, cancellationToken);

        return Ok(new { id = handover.Id });
    }

    /// <summary>
    /// Allows employees to append tasks, defects, issues, or work statuses to a handover.
    /// Hangar employees can only add tasks/defects/issues.
    /// Shop employees can only add work statuses.
    /// </summary>
    /// <param name="id">Handover ID</param>
    /// <param name="request">Employee append request</param>
    /// <returns>Updated handover ID</returns>
    [HttpPost("{id}/employee-append")]
    public async Task<IActionResult> EmployeeAppend(Guid id, [FromBody] EmployeeAppendRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var handover = await dbContext.Handovers
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (handover is null) return NotFound();

        // Employees can append to pending handovers in their hangar/shop
        var currentUser = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

        if (currentUser is null) return Unauthorized();
        if (currentUser.Role != UserRole.Employee) return Forbid();

        if (handover.SectionId != currentUser.SectionId) return Forbid();
        if (handover.HangarId.HasValue && currentUser.HangarId != handover.HangarId) return Forbid();
        if (handover.ShopId.HasValue && currentUser.ShopId != handover.ShopId) return Forbid();

        if (handover.Status != HandoverStatus.Draft && handover.Status != HandoverStatus.Pending) return BadRequest("Can only append to draft or pending handovers");

        // Workspace-based permission enforcement: Hangar employees can only add tasks/defects/issues, Shop employees can only add work statuses
        if (currentUser.HangarId.HasValue)
        {
            // Hangar employees can only add tasks, defects, issues
            if (request.WorkStatuses.Any())
                return BadRequest("Hangar employees cannot add work statuses");
        }
        else if (currentUser.ShopId.HasValue)
        {
            // Shop employees can only add work statuses
            if (request.Tasks.Any() || request.Defects.Any())
                return BadRequest("Shop employees cannot add tasks or defects");
        }

        // Template-based filtering: Shop handovers should not have aircraft-related entities
        if (handover.TemplateType == HandoverTemplateType.Shop)
        {
            if (request.Tasks.Any()) return BadRequest("Shop handovers cannot have tasks");
            if (request.Defects.Any()) return BadRequest("Shop handovers cannot have defects");
            if (request.Issues.Any()) return BadRequest("Shop handovers cannot have issues");
        }
        // Hangar handovers typically use work statuses less frequently, but allow for flexibility

        // Add employee's contributions within a transaction
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var task in request.Tasks)
            {
                dbContext.HandoverTasks.Add(new HandoverTask
                {
                    HandoverId = handover.Id,
                    TaskType = task.TaskType,
                    AircraftRegistration = task.AircraftRegistration,
                    TaskCardCode = task.TaskCardCode,
                    Description = task.Description,
                    CreatedByUserId = userId.Value
                });
            }

            foreach (var defect in request.Defects)
            {
                dbContext.HandoverDefects.Add(new HandoverDefect
                {
                    HandoverId = handover.Id,
                    AircraftRegistration = defect.AircraftRegistration,
                    DefectDescription = defect.DefectDescription,
                    NonRoutineCardNumber = defect.NonRoutineCardNumber,
                    DefectLoginTime = defect.DefectLoginTime,
                    ItemStatus = defect.ItemStatus
                });
            }

            foreach (var issue in request.Issues)
            {
                dbContext.HandoverIssues.Add(new HandoverIssue
                {
                    HandoverId = handover.Id,
                    IssueType = issue.IssueType,
                    Description = issue.Description,
                    CreatedBy = userId.Value
                });
            }

            foreach (var workStatus in request.WorkStatuses)
            {
                dbContext.HandoverWorkStatuses.Add(new HandoverWorkStatus
                {
                    HandoverId = handover.Id,
                    MfgPartNumber = workStatus.MfgPartNumber,
                    SerialNumber = workStatus.SerialNumber,
                    WorkCarriedOut = workStatus.WorkCarriedOut,
                    WorkToBeDone = workStatus.WorkToBeDone,
                    OutstandingIssue = workStatus.OutstandingIssue
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var correlationId = HttpContext.TraceIdentifier;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await auditService.WriteAsync(userId, "EmployeeAppend", "Handover", handover.Id.ToString(), null, new { tasksCount = request.Tasks.Length, defectsCount = request.Defects.Length, issuesCount = request.Issues.Length, workStatusesCount = request.WorkStatuses.Length }, false, ipAddress, correlationId, cancellationToken);

            return Ok(new { id = handover.Id });
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Retrieves pending handovers that are overdue (older than 16 hours).
    /// Only Managers, Directors, and SystemAdmins can view overdue handovers.
    /// Managers can only see overdue handovers in their section.
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <returns>Paginated list of overdue pending handovers</returns>
    [HttpGet("overdue-pending")]
    public async Task<IActionResult> GetOverduePendingHandovers(
        CancellationToken cancellationToken,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var currentUser = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

        if (currentUser is null) return Unauthorized();

        // Only Managers, Directors, and SystemAdmins can view overdue handovers
        if (currentUser.Role != UserRole.Manager && currentUser.Role != UserRole.Director && currentUser.Role != UserRole.SystemAdmin)
            return Forbid();

        var sixteenHoursAgo = DateTime.UtcNow.AddHours(-16);

        var query = dbContext.Handovers
            .Include(h => h.OutgoingTeamLeader)
            .Include(h => h.IncomingTeamLeader)
            .AsNoTracking()
            .Where(h => h.Status == HandoverStatus.Pending && h.SubmittedAt.HasValue && h.SubmittedAt.Value < sixteenHoursAgo);

        // Managers can only see overdue handovers in their section
        if (currentUser.Role == UserRole.Manager)
        {
            query = query.Where(h => h.SectionId == currentUser.SectionId);
        }
        // Directors and SystemAdmins can see all overdue handovers

        var total = await query.CountAsync(cancellationToken);

        var overdueHandovers = await query
            .OrderByDescending(h => h.SubmittedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtoItems = overdueHandovers.Select(h => new
        {
            id = h.Id,
            templateType = (int)h.TemplateType,
            status = (int)h.Status,
            date = h.Date,
            shiftType = h.ShiftType,
            dutyTeamLeaderName = h.DutyTeamLeaderName,
            sectionId = h.SectionId,
            hangarId = h.HangarId,
            shopId = h.ShopId,
            outgoingTeamLeaderId = h.OutgoingTeamLeaderId,
            incomingTeamLeaderId = h.IncomingTeamLeaderId,
            submittedAt = h.SubmittedAt,
            hoursPending = h.SubmittedAt.HasValue ? (DateTime.UtcNow - h.SubmittedAt.Value).TotalHours : 0,
            createdByUserName = h.OutgoingTeamLeader?.FullName,
            incomingTeamLeaderUserName = h.IncomingTeamLeader?.FullName
        });

        return Ok(new { items = dtoItems, total, pageNumber, pageSize });
    }

    [HttpGet("pending-signatures")]
    public async Task<IActionResult> GetPendingSignatures([FromQuery] Guid? userId, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        if (currentUserId is null) return Unauthorized();

        // If userId is provided, use it; otherwise use current user
        var targetUserId = userId ?? currentUserId.Value;

        var currentUser = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUserId.Value, cancellationToken);

        if (currentUser is null) return Unauthorized();

        // Users can only view their own pending signatures unless they are managers/directors
        if (targetUserId != currentUserId.Value && currentUser.Role != UserRole.Manager && currentUser.Role != UserRole.Director && currentUser.Role != UserRole.SystemAdmin)
            return Forbid();

        var targetUser = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == targetUserId, cancellationToken);

        if (targetUser is null) return NotFound();

        // Get pending handovers where the user is the incoming team leader (same hangar/shop)
        var query = dbContext.Handovers
            .Include(h => h.OutgoingTeamLeader)
            .Include(h => h.Signatures)
            .AsNoTracking()
            .Where(h => h.Status == HandoverStatus.Pending);

        // Filter by hangar/shop for the target user
        if (targetUser.HangarId.HasValue)
            query = query.Where(h => h.HangarId == targetUser.HangarId);
        if (targetUser.ShopId.HasValue)
            query = query.Where(h => h.ShopId == targetUser.ShopId);

        // Filter by section for non-directors
        if (currentUser.Role != UserRole.Director && currentUser.Role != UserRole.SystemAdmin)
            query = query.Where(h => h.SectionId == currentUser.SectionId);

        var pendingHandovers = await query
            .OrderByDescending(h => h.SubmittedAt)
            .ToListAsync(cancellationToken);

        var dtoItems = pendingHandovers.Select(h => new
        {
            id = h.Id,
            templateType = (int)h.TemplateType,
            status = (int)h.Status,
            date = h.Date,
            shiftType = h.ShiftType,
            dutyTeamLeaderName = h.DutyTeamLeaderName,
            sectionId = h.SectionId,
            hangarId = h.HangarId,
            shopId = h.ShopId,
            outgoingTeamLeaderId = h.OutgoingTeamLeaderId,
            incomingTeamLeaderId = h.IncomingTeamLeaderId,
            submittedAt = h.SubmittedAt,
            hoursPending = h.SubmittedAt.HasValue ? (DateTime.UtcNow - h.SubmittedAt.Value).TotalHours : 0,
            createdByUserName = h.OutgoingTeamLeader?.FullName,
            hasIncomingSignature = h.Signatures.Any(s => s.SignatureRole == SignatureRole.Incoming)
        });

        return Ok(dtoItems);
    }

    /// <summary>
    /// Exports a handover to PDF format.
    /// Only accepted handovers can be exported.
    /// </summary>
    /// <param name="id">Handover ID</param>
    /// <returns>PDF file</returns>
    [HttpGet("{id}/export")]
    public async Task<IActionResult> ExportHandover(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var handover = await dbContext.Handovers
            .Include(h => h.Signatures)
            .Include(h => h.Tasks)
            .Include(h => h.Defects)
            .Include(h => h.Issues)
            .Include(h => h.WorkStatuses)
            .Include(h => h.ManningStatus)
            .Include(h => h.Aircrafts)
            .Include(h => h.OutgoingTeamLeader)
            .Include(h => h.IncomingTeamLeader)
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (handover is null) return NotFound();

        // RBAC: User can only export handovers in their section/hangar/shop
        var currentUser = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

        if (currentUser is null) return Unauthorized();

        if (currentUser.Role == UserRole.Employee || currentUser.Role == UserRole.TeamLeader)
        {
            if (handover.SectionId != currentUser.SectionId) return Forbid();
            if (currentUser.HangarId.HasValue && handover.HangarId != currentUser.HangarId) return Forbid();
            if (currentUser.ShopId.HasValue && handover.ShopId != currentUser.ShopId) return Forbid();
        }
        else if (currentUser.Role == UserRole.Manager)
        {
            if (handover.SectionId != currentUser.SectionId) return Forbid();
        }

        // Only accepted handovers can be exported
        if (handover.Status != HandoverStatus.Accepted)
            return BadRequest("Only accepted handovers can be exported");

        var correlationId = HttpContext.TraceIdentifier;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await auditService.WriteAsync(userId, "Export", "Handover", handover.Id.ToString(), null, new { format = "PDF" }, false, ipAddress, correlationId, cancellationToken);

        // Generate PDF
        QuestPDF.Settings.License = LicenseType.Community;
        var document = new HandoverPdfDocument(handover);
        var pdfBytes = document.GeneratePdf();

        return File(pdfBytes, "application/pdf", $"handover_{handover.Id}.pdf");
    }

    public sealed record ManningStatusRequest(
        int TotalScheduledManpower,
        int SickLeave,
        int Absent,
        int Vacation,
        int Training,
        int BorrowedManpower
    );

    public sealed record CreateHandoverRequest(
        HandoverTemplateType TemplateType,
        DateTime Date,
        string ShiftType,
        string DutyTeamLeaderName,
        ManningStatusDto ManningStatus,
        List<AircraftDto>? Aircrafts
    );

    public sealed record AircraftDto(
        string AircraftType,
        string AircraftRegistration,
        string MaintenanceType,
        DateTime MaintenanceStartTime,
        DateTime? MaintenanceEndTime
    );

    public sealed record ManningStatusDto(
        int TotalScheduledManpower,
        int SickLeave,
        int Absent,
        int Vacation,
        int Training,
        int BorrowedManpower,
        int TotalAvailableManpower,
        int TotalLostManpower,
        double AvailabilityPercentage
    );

    public sealed record UpdateHandoverRequest(
        DateTime Date,
        ShiftType ShiftType,
        string DutyTeamLeaderName,
        string? RowVersion,
        ManningStatusDto? ManningStatus
    );

    public sealed record AcceptHandoverRequest(
        string SignatureData,
        string SignatureName,
        string? RowVersion
    );

    public sealed record RejectHandoverRequest(
        string RejectionReason,
        string SignatureData,
        string SignatureName,
        string? RowVersion
    );

    public sealed record AddSignatureRequest(
        SignatureRole SignatureRole,
        string SignatureData,
        string SignatureName
    );

    public sealed record AddTaskRequest(
        TaskType TaskType,
        string? AircraftRegistration,
        string? TaskCardCode,
        string Description
    );

    public sealed record UpdateTaskRequest(
        TaskType TaskType,
        string? AircraftRegistration,
        string? TaskCardCode,
        string Description
    );

    public sealed record AddDefectRequest(
        string AircraftRegistration,
        string DefectDescription,
        string NonRoutineCardNumber,
        DateTime DefectLoginTime,
        string ItemStatus
    );

    public sealed record UpdateDefectRequest(
        string AircraftRegistration,
        string DefectDescription,
        string NonRoutineCardNumber,
        DateTime DefectLoginTime,
        string ItemStatus
    );

    public sealed record AddIssueRequest(
        IssueType IssueType,
        string Description
    );

    public sealed record UpdateIssueRequest(
        IssueType IssueType,
        string Description
    );

    public sealed record AddWorkStatusRequest(
        string MfgPartNumber,
        string SerialNumber,
        string WorkCarriedOut,
        string WorkToBeDone,
        string OutstandingIssue
    );

    public sealed record UpdateWorkStatusRequest(
        string MfgPartNumber,
        string SerialNumber,
        string WorkCarriedOut,
        string WorkToBeDone,
        string OutstandingIssue
    );

    public sealed record EmployeeAppendRequest(
        TaskDto[] Tasks,
        DefectDto[] Defects,
        IssueDto[] Issues,
        WorkStatusDto[] WorkStatuses
    );

    public sealed record TaskDto(
        TaskType TaskType,
        string? AircraftRegistration,
        string? TaskCardCode,
        string Description
    );

    public sealed record DefectDto(
        string AircraftRegistration,
        string DefectDescription,
        string NonRoutineCardNumber,
        DateTime DefectLoginTime,
        string ItemStatus
    );

    public sealed record IssueDto(
        IssueType IssueType,
        string Description
    );

    public sealed record WorkStatusDto(
        string MfgPartNumber,
        string SerialNumber,
        string WorkCarriedOut,
        string WorkToBeDone,
        string OutstandingIssue
    );
}

public class HandoverPdfDocument(Handover handover) : IDocument
{
    public DocumentMetadata GetMetadata() => new DocumentMetadata { Title = "Digital Handover Logbook", Author = "BaseOps" };

    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Margin(30);
                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                });
            });
    }

    void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("Digital Handover Logbook").Bold().FontSize(20).FontColor(Colors.Blue.Darken2);
                column.Item().Text($"Template: {handover.TemplateType}").FontSize(10).FontColor(Colors.Grey.Darken1);
            });
        });
    }

    void ComposeContent(IContainer container)
    {
        container.PaddingVertical(20).Column(column =>
        {
            column.Item().Element(ComposeGeneralInfo);
            column.Item().Element(ComposeManningStatus);
            column.Item().Element(ComposeAircrafts);
            column.Item().Element(ComposeTasks);
            column.Item().Element(ComposeDefects);
            column.Item().Element(ComposeIssues);
            column.Item().Element(ComposeWorkStatuses);
            column.Item().Element(ComposeSignatures);
        });
    }

    void ComposeGeneralInfo(IContainer container)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Item().Text("General Information").Bold().FontSize(14);
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(150);
                    columns.RelativeColumn();
                });
                table.Cell().Text("Date:").Bold();
                table.Cell().Text(handover.Date.ToString("yyyy-MM-dd"));
                table.Cell().Text("Shift:").Bold();
                table.Cell().Text(handover.ShiftType.ToString());
                table.Cell().Text("Duty Team Leader:").Bold();
                table.Cell().Text(handover.DutyTeamLeaderName);
                table.Cell().Text("Status:").Bold();
                table.Cell().Text(handover.Status.ToString());
                table.Cell().Text("Outgoing TL:").Bold();
                table.Cell().Text(handover.OutgoingTeamLeader?.FullName ?? "N/A");
                table.Cell().Text("Incoming TL:").Bold();
                table.Cell().Text(handover.IncomingTeamLeader?.FullName ?? "N/A");
            });
        });
    }

    void ComposeManningStatus(IContainer container)
    {
        if (handover.ManningStatus == null) return;

        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Item().Text("Manning Status").Bold().FontSize(14);
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(150);
                    columns.RelativeColumn();
                });
                table.Cell().Text("Total Scheduled:").Bold();
                table.Cell().Text(handover.ManningStatus.TotalScheduledManpower.ToString());
                table.Cell().Text("Total Available:").Bold();
                table.Cell().Text(handover.ManningStatus.TotalAvailableManpower.ToString());
                table.Cell().Text("Total Lost:").Bold();
                table.Cell().Text(handover.ManningStatus.TotalLostManpower.ToString());
                table.Cell().Text("Sick Leave:").Bold();
                table.Cell().Text(handover.ManningStatus.SickLeave.ToString());
                table.Cell().Text("Absent:").Bold();
                table.Cell().Text(handover.ManningStatus.Absent.ToString());
                table.Cell().Text("Vacation:").Bold();
                table.Cell().Text(handover.ManningStatus.Vacation.ToString());
                table.Cell().Text("Training:").Bold();
                table.Cell().Text(handover.ManningStatus.Training.ToString());
                table.Cell().Text("Borrowed:").Bold();
                table.Cell().Text(handover.ManningStatus.BorrowedManpower.ToString());
                table.Cell().Text("Availability:").Bold();
                table.Cell().Text($"{handover.ManningStatus.AvailabilityPercentage:F1}%");
            });
        });
    }

    void ComposeAircrafts(IContainer container)
    {
        if (handover.Aircrafts == null || !handover.Aircrafts.Any()) return;

        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Item().Text("Aircraft Information").Bold().FontSize(14);
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(100);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Text("Registration").Bold();
                    header.Cell().Text("Type").Bold();
                    header.Cell().Text("Maintenance").Bold();
                    header.Cell().Text("Start Time").Bold();
                });

                foreach (var aircraft in handover.Aircrafts)
                {
                    table.Cell().Text(aircraft.AircraftRegistration);
                    table.Cell().Text(aircraft.AircraftType);
                    table.Cell().Text(aircraft.MaintenanceType);
                    table.Cell().Text(aircraft.MaintenanceStartTime.ToString("yyyy-MM-dd HH:mm"));
                }
            });
        });
    }

    void ComposeTasks(IContainer container)
    {
        if (handover.Tasks == null || !handover.Tasks.Any()) return;

        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Item().Text("Tasks").Bold().FontSize(14);
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(80);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Text("Type").Bold();
                    header.Cell().Text("Aircraft").Bold();
                    header.Cell().Text("Task Card").Bold();
                    header.Cell().Text("Description").Bold();
                });

                foreach (var task in handover.Tasks)
                {
                    table.Cell().Text(task.TaskType.ToString());
                    table.Cell().Text(task.AircraftRegistration ?? "N/A");
                    table.Cell().Text(task.TaskCardCode ?? "N/A");
                    table.Cell().Text(task.Description);
                }
            });
        });
    }

    void ComposeDefects(IContainer container)
    {
        if (handover.Defects == null || !handover.Defects.Any()) return;

        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Item().Text("Defects").Bold().FontSize(14);
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(100);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.ConstantColumn(60);
                });

                table.Header(header =>
                {
                    header.Cell().Text("Aircraft").Bold();
                    header.Cell().Text("Description").Bold();
                    header.Cell().Text("NR Card").Bold();
                    header.Cell().Text("Login Time").Bold();
                    header.Cell().Text("Status").Bold();
                });

                foreach (var defect in handover.Defects)
                {
                    table.Cell().Text(defect.AircraftRegistration);
                    table.Cell().Text(defect.DefectDescription);
                    table.Cell().Text(defect.NonRoutineCardNumber);
                    table.Cell().Text(defect.DefectLoginTime.ToString("yyyy-MM-dd HH:mm"));
                    table.Cell().Text(defect.ItemStatus);
                }
            });
        });
    }

    void ComposeIssues(IContainer container)
    {
        if (handover.Issues == null || !handover.Issues.Any()) return;

        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Item().Text("Issues").Bold().FontSize(14);
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(80);
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Text("Type").Bold();
                    header.Cell().Text("Description").Bold();
                });

                foreach (var issue in handover.Issues)
                {
                    table.Cell().Text(issue.IssueType.ToString());
                    table.Cell().Text(issue.Description);
                }
            });
        });
    }

    void ComposeWorkStatuses(IContainer container)
    {
        if (handover.WorkStatuses == null || !handover.WorkStatuses.Any()) return;

        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Item().Text("Work Statuses").Bold().FontSize(14);
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(100);
                    columns.ConstantColumn(100);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Text("MFG P/N").Bold();
                    header.Cell().Text("Serial").Bold();
                    header.Cell().Text("Carried Out").Bold();
                    header.Cell().Text("To Be Done").Bold();
                    header.Cell().Text("Issue").Bold();
                });

                foreach (var ws in handover.WorkStatuses)
                {
                    table.Cell().Text(ws.MfgPartNumber);
                    table.Cell().Text(ws.SerialNumber);
                    table.Cell().Text(ws.WorkCarriedOut);
                    table.Cell().Text(ws.WorkToBeDone);
                    table.Cell().Text(ws.OutstandingIssue ?? "N/A");
                }
            });
        });
    }

    void ComposeSignatures(IContainer container)
    {
        if (handover.Signatures == null || !handover.Signatures.Any()) return;

        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Item().Text("Digital Signatures").Bold().FontSize(14);
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            foreach (var signature in handover.Signatures)
            {
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(150);
                        columns.RelativeColumn();
                    });
                    table.Cell().Text($"{signature.SignatureRole}:").Bold();
                    table.Cell().Text(signature.SignatureName);
                    table.Cell().Text("Signed At:").Bold();
                    table.Cell().Text(signature.SignedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                });
                column.Item().PaddingTop(5);
            }
        });
    }
}
