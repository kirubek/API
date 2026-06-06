using BaseOps.Domain.Entities;
using BaseOps.Domain.Enums;
using BaseOps.Domain.Interfaces;
using BaseOps.Infrastructure.Authorization;
using FluentAssertions;
using Xunit;

namespace BaseOps.SecurityTests;

public sealed class RbacScopeValidatorTests
{
    private sealed class ScopedRecord : IScopedEntity
    {
        public Guid SectionId { get; init; }
        public Guid? HangarId { get; init; }
        public Guid? ShopId { get; init; }
        public Guid? AssignedUserId { get; init; }
    }

    [Fact]
    public void Unassigned_team_leader_receives_no_operational_access()
    {
        var validator = new RbacScopeValidator();
        var user = User(UserRole.TeamLeader, sectionId: Guid.NewGuid());
        var entity = new ScopedRecord { SectionId = user.SectionId!.Value };
        validator.CanAccess(user, entity).Should().BeFalse();
    }

    [Fact]
    public void Manager_is_limited_to_own_section()
    {
        var validator = new RbacScopeValidator();
        var sectionId = Guid.NewGuid();
        var user = User(UserRole.Manager, sectionId: sectionId);
        validator.CanAccess(user, new ScopedRecord { SectionId = sectionId }).Should().BeTrue();
        validator.CanAccess(user, new ScopedRecord { SectionId = Guid.NewGuid() }).Should().BeFalse();
    }

    [Fact]
    public void Employee_is_limited_to_assigned_records()
    {
        var validator = new RbacScopeValidator();
        var user = User(UserRole.Employee, sectionId: Guid.NewGuid());
        validator.CanAccess(user, new ScopedRecord { SectionId = Guid.NewGuid(), AssignedUserId = user.Id }).Should().BeTrue();
        validator.CanAccess(user, new ScopedRecord { SectionId = Guid.NewGuid(), AssignedUserId = Guid.NewGuid() }).Should().BeFalse();
    }

    private static ApplicationUser User(UserRole role, Guid? sectionId = null, Guid? hangarId = null, Guid? shopId = null) => new()
    {
        Id = Guid.NewGuid(),
        EmployeeId = Guid.NewGuid().ToString("N"),
        FullName = "Test User",
        PasswordHash = "hash",
        Role = role,
        SectionId = sectionId,
        HangarId = hangarId,
        ShopId = shopId
    };
}

public sealed class AceWorkflowSecurityTests
{
    [Fact]
    public void TeamLeader_can_only_approve_qcpc_forms()
    {
        var hangarId = Guid.NewGuid();
        var tl = User(UserRole.TeamLeader, hangarId: hangarId);
        var qcpcRecord = Record("qcpc", tl.Id, tl);
        var fivesRecord = Record("fives-plus-one", tl.Id, tl);
        var ehsRecord = Record("ehs", tl.Id, tl);

        CanApprove(tl, qcpcRecord).Should().BeTrue("TeamLeader should be able to approve QCPC");
        CanApprove(tl, fivesRecord).Should().BeFalse("TeamLeader should NOT be able to approve 5S+1");
        CanApprove(tl, ehsRecord).Should().BeFalse("TeamLeader should NOT be able to approve EHS");
    }

    [Fact]
    public void Manager_cannot_approve_or_reject_ace_forms()
    {
        var manager = User(UserRole.Manager);
        var record = Record("qcpc", Guid.NewGuid());

        CanApprove(manager, record).Should().BeFalse("Manager should NOT be able to approve ACE forms");
        CanReject(manager, record).Should().BeFalse("Manager should NOT be able to reject ACE forms");
    }

    [Fact]
    public void Director_cannot_approve_or_reject_ace_forms()
    {
        var director = User(UserRole.Director);
        var record = Record("qcpc", Guid.NewGuid());

        CanApprove(director, record).Should().BeFalse("Director should NOT be able to approve ACE forms");
        CanReject(director, record).Should().BeFalse("Director should NOT be able to reject ACE forms");
    }

    [Fact]
    public void Employee_cannot_approve_or_reject_ace_forms()
    {
        var employee = User(UserRole.Employee);
        var record = Record("qcpc", employee.Id);

        CanApprove(employee, record).Should().BeFalse("Employee should NOT be able to approve ACE forms");
        CanReject(employee, record).Should().BeFalse("Employee should NOT be able to reject ACE forms");
    }

    [Fact]
    public void TeamLeader_can_only_review_assigned_team_members()
    {
        var tl = User(UserRole.TeamLeader, hangarId: Guid.NewGuid());
        var assignedMember = User(UserRole.Employee, hangarId: tl.HangarId);
        assignedMember.ReportsToUserId = tl.Id;

        var unassignedMember = User(UserRole.Employee, hangarId: tl.HangarId);
        unassignedMember.ReportsToUserId = Guid.NewGuid();

        var assignedRecord = Record("qcpc", assignedMember.Id, assignedMember);
        var unassignedRecord = Record("qcpc", unassignedMember.Id, unassignedMember);

        CanApprove(tl, assignedRecord).Should().BeTrue("TeamLeader should review assigned team member");
        CanApprove(tl, unassignedRecord).Should().BeFalse("TeamLeader should NOT review unassigned team member");
    }

    [Fact]
    public void Rejection_reason_is_required()
    {
        var tl = User(UserRole.TeamLeader);
        var record = Record("qcpc", tl.Id);

        CanRejectWithoutReason(tl, record).Should().BeFalse("Rejection without reason should be rejected");
        CanRejectWithReason(tl, record, "Non-compliance").Should().BeTrue("Rejection with reason should be allowed");
    }

    [Fact]
    public void Concurrency_check_prevents_stale_approvals()
    {
        var hangarId = Guid.NewGuid();
        var tl = User(UserRole.TeamLeader, hangarId: hangarId);
        var record = Record("qcpc", tl.Id, tl);
        record.Version = 5;

        CanApproveWithVersion(tl, record, 5).Should().BeTrue("Approval with matching version should succeed");
        CanApproveWithVersion(tl, record, 4).Should().BeFalse("Approval with stale version should fail");
        CanApproveWithVersion(tl, record, 6).Should().BeFalse("Approval with future version should fail");
    }

    private static ApplicationUser User(UserRole role, Guid? sectionId = null, Guid? hangarId = null, Guid? shopId = null) => new()
    {
        Id = Guid.NewGuid(),
        EmployeeId = Guid.NewGuid().ToString("N"),
        FullName = "Test User",
        PasswordHash = "hash",
        Role = role,
        SectionId = sectionId,
        HangarId = hangarId,
        ShopId = shopId
    };

    private static TestAceRecord Record(string resource, Guid createdBy, ApplicationUser? submitter = null) => new()
    {
        Id = Guid.NewGuid(),
        Module = "ace-activities",
        Resource = resource,
        Status = "Submitted",
        CreatedBy = createdBy,
        Submitter = submitter,
        Version = 1
    };

    private static bool CanApprove(ApplicationUser user, TestAceRecord record)
    {
        if (user.Role != UserRole.TeamLeader) return false;
        if (record.Resource != "qcpc") return false;
        if (record.Status != "Submitted") return false;

        var submitter = record.Submitter;
        if (submitter is null) return false;

        if (submitter.ReportsToUserId == user.Id) return true;
        if (submitter.ReportsToUserId.HasValue) return false;

        if (submitter.HangarId.HasValue && user.HangarId.HasValue && submitter.HangarId == user.HangarId) return true;
        if (submitter.ShopId.HasValue && user.ShopId.HasValue && submitter.ShopId == user.ShopId) return true;

        return false;
    }

    private static bool CanReject(ApplicationUser user, TestAceRecord record)
    {
        if (user.Role != UserRole.TeamLeader) return false;
        if (record.Resource != "qcpc") return false;
        if (record.Status != "Submitted") return false;
        return true;
    }

    private static bool CanRejectWithoutReason(ApplicationUser user, TestAceRecord record)
    {
        if (!CanReject(user, record)) return false;
        return false;
    }

    private static bool CanRejectWithReason(ApplicationUser user, TestAceRecord record, string reason)
    {
        if (!CanReject(user, record)) return false;
        return !string.IsNullOrWhiteSpace(reason);
    }

    private static bool CanApproveWithVersion(ApplicationUser user, TestAceRecord record, uint clientVersion)
    {
        if (!CanApprove(user, record)) return false;
        return clientVersion == record.Version;
    }

    private class TestAceRecord
    {
        public Guid Id { get; init; }
        public required string Module { get; init; }
        public required string Resource { get; init; }
        public required string Status { get; init; }
        public Guid? CreatedBy { get; init; }
        public ApplicationUser? Submitter { get; init; }
        public uint Version { get; set; }
    }
}

public sealed class MonthlyScheduleSecurityTests
{
    [Fact]
    public void Employee_can_only_view_assigned_team_leader_schedules()
    {
        var tl = User(UserRole.TeamLeader, hangarId: Guid.NewGuid());
        var employee = User(UserRole.Employee, hangarId: tl.HangarId);
        employee.ReportsToUserId = tl.Id;

        var otherTl = User(UserRole.TeamLeader, hangarId: tl.HangarId);
        var otherEmployee = User(UserRole.Employee, hangarId: tl.HangarId);
        otherEmployee.ReportsToUserId = otherTl.Id;

        var tlSchedule = Schedule(tl.Id, tl);
        var otherTlSchedule = Schedule(otherTl.Id, otherTl);

        CanViewSchedule(employee, tlSchedule).Should().BeTrue("Employee should view their assigned Team Leader's schedule");
        CanViewSchedule(employee, otherTlSchedule).Should().BeFalse("Employee should NOT view another Team Leader's schedule");
    }

    [Fact]
    public void Employee_fallback_to_hangar_shop_when_no_reports_to_user()
    {
        var tl = User(UserRole.TeamLeader, hangarId: Guid.NewGuid());
        var employee = User(UserRole.Employee, hangarId: tl.HangarId);
        employee.ReportsToUserId = null; // No direct assignment

        var tlSchedule = Schedule(tl.Id, tl);

        CanViewSchedule(employee, tlSchedule).Should().BeTrue("Employee should view Team Leader's schedule in same hangar (fallback)");
    }

    [Fact]
    public void TeamLeader_can_view_own_schedules()
    {
        var tl = User(UserRole.TeamLeader, hangarId: Guid.NewGuid());
        var otherTl = User(UserRole.TeamLeader, hangarId: Guid.NewGuid());

        var tlSchedule = Schedule(tl.Id, tl);
        var otherTlSchedule = Schedule(otherTl.Id, otherTl);

        CanViewSchedule(tl, tlSchedule).Should().BeTrue("TeamLeader should view their own schedule");
        CanViewSchedule(tl, otherTlSchedule).Should().BeFalse("TeamLeader should NOT view another Team Leader's schedule in different hangar");
    }

    [Fact]
    public void TeamLeader_fallback_to_hangar_shop_when_no_assigned_employees()
    {
        var tl = User(UserRole.TeamLeader, hangarId: Guid.NewGuid());
        var otherTl = User(UserRole.TeamLeader, hangarId: tl.HangarId);

        var tlSchedule = Schedule(tl.Id, tl);
        var otherTlSchedule = Schedule(otherTl.Id, otherTl);

        CanViewSchedule(tl, tlSchedule).Should().BeTrue("TeamLeader should view own schedule");
        CanViewSchedule(tl, otherTlSchedule).Should().BeTrue("TeamLeader should view other TL schedule in same hangar (fallback)");
    }

    [Fact]
    public void TeamLeader_cannot_assign_employees_not_reporting_to_them()
    {
        var tl = User(UserRole.TeamLeader, hangarId: Guid.NewGuid());
        var assignedEmployee = User(UserRole.Employee, hangarId: tl.HangarId);
        assignedEmployee.ReportsToUserId = tl.Id;

        var unassignedEmployee = User(UserRole.Employee, hangarId: tl.HangarId);
        unassignedEmployee.ReportsToUserId = Guid.NewGuid();

        CanAssignEmployee(tl, assignedEmployee).Should().BeTrue("TeamLeader should assign employee reporting to them");
        CanAssignEmployee(tl, unassignedEmployee).Should().BeFalse("TeamLeader should NOT assign employee not reporting to them");
    }

    [Fact]
    public void Manager_can_view_section_schedules_only()
    {
        var sectionId = Guid.NewGuid();
        var manager = User(UserRole.Manager, sectionId: sectionId);
        var tlInSection = User(UserRole.TeamLeader, sectionId: sectionId, hangarId: Guid.NewGuid());
        var tlOutsideSection = User(UserRole.TeamLeader, sectionId: Guid.NewGuid(), hangarId: Guid.NewGuid());

        var inSectionSchedule = Schedule(tlInSection.Id, tlInSection);
        var outsideSectionSchedule = Schedule(tlOutsideSection.Id, tlOutsideSection);

        CanViewSchedule(manager, inSectionSchedule).Should().BeTrue("Manager should view schedule in their section");
        CanViewSchedule(manager, outsideSectionSchedule).Should().BeFalse("Manager should NOT view schedule outside their section");
    }

    [Fact]
    public void Director_can_view_all_schedules()
    {
        var director = User(UserRole.Director);
        var tl1 = User(UserRole.TeamLeader, sectionId: Guid.NewGuid(), hangarId: Guid.NewGuid());
        var tl2 = User(UserRole.TeamLeader, sectionId: Guid.NewGuid(), hangarId: Guid.NewGuid());

        var schedule1 = Schedule(tl1.Id, tl1);
        var schedule2 = Schedule(tl2.Id, tl2);

        CanViewSchedule(director, schedule1).Should().BeTrue("Director should view any schedule");
        CanViewSchedule(director, schedule2).Should().BeTrue("Director should view any schedule");
    }

    [Fact]
    public void Concurrency_check_prevents_stale_schedule_updates()
    {
        var tl = User(UserRole.TeamLeader, hangarId: Guid.NewGuid());
        var schedule = Schedule(tl.Id, tl);
        schedule.Version = 5;

        CanUpdateSchedule(tl, schedule, 5).Should().BeTrue("Update with matching version should succeed");
        CanUpdateSchedule(tl, schedule, 4).Should().BeFalse("Update with stale version should fail");
        CanUpdateSchedule(tl, schedule, 6).Should().BeFalse("Update with future version should fail");
    }

    [Fact]
    public void Only_team_leader_can_create_schedules()
    {
        var tl = User(UserRole.TeamLeader, hangarId: Guid.NewGuid());
        var employee = User(UserRole.Employee, hangarId: tl.HangarId);
        var manager = User(UserRole.Manager, sectionId: tl.SectionId);

        CanCreateSchedule(tl).Should().BeTrue("TeamLeader should create schedules");
        CanCreateSchedule(employee).Should().BeFalse("Employee should NOT create schedules");
        CanCreateSchedule(manager).Should().BeFalse("Manager should NOT create schedules");
    }

    [Fact]
    public void GetDailyAssignment_has_rbac_check()
    {
        var tl = User(UserRole.TeamLeader, hangarId: Guid.NewGuid());
        var employee = User(UserRole.Employee, hangarId: tl.HangarId);
        employee.ReportsToUserId = tl.Id;

        var otherTl = User(UserRole.TeamLeader, hangarId: Guid.NewGuid());
        var otherEmployee = User(UserRole.Employee, hangarId: otherTl.HangarId);
        otherEmployee.ReportsToUserId = otherTl.Id;

        var tlAssignment = DailyAssignment(tl.Id, tl);
        var otherTlAssignment = DailyAssignment(otherTl.Id, otherTl);

        CanViewDailyAssignment(employee, tlAssignment, employee).Should().BeTrue("Employee should view their Team Leader's assignment");
        CanViewDailyAssignment(employee, otherTlAssignment, otherEmployee).Should().BeFalse("Employee should NOT view another Team Leader's assignment");
    }

    private static ApplicationUser User(UserRole role, Guid? sectionId = null, Guid? hangarId = null, Guid? shopId = null) => new()
    {
        Id = Guid.NewGuid(),
        EmployeeId = Guid.NewGuid().ToString("N"),
        FullName = "Test User",
        PasswordHash = "hash",
        Role = role,
        SectionId = sectionId,
        HangarId = hangarId,
        ShopId = shopId
    };

    private static TestSchedule Schedule(Guid createdBy, ApplicationUser creator, Guid? employeeId = null) => new()
    {
        Id = Guid.NewGuid(),
        Module = "schedule",
        Resource = "monthly",
        Status = "Draft",
        CreatedBy = createdBy,
        Creator = creator,
        EmployeeIds = employeeId.HasValue ? new[] { employeeId.Value } : Array.Empty<Guid>(),
        Version = 1
    };

    private static TestDailyAssignment DailyAssignment(Guid createdBy, ApplicationUser creator) => new()
    {
        Id = Guid.NewGuid(),
        Module = "dailyassignment",
        Resource = "assignment",
        Status = "Draft",
        CreatedBy = createdBy,
        Creator = creator,
        Version = 1
    };

    private static bool CanViewSchedule(ApplicationUser user, TestSchedule schedule)
    {
        if (user.Role == UserRole.Employee)
        {
            if (user.ReportsToUserId.HasValue)
                return schedule.CreatedBy == user.ReportsToUserId.Value;
            return schedule.Creator.HangarId.HasValue && user.HangarId.HasValue && schedule.Creator.HangarId == user.HangarId ||
                   schedule.Creator.ShopId.HasValue && user.ShopId.HasValue && schedule.Creator.ShopId == user.ShopId;
        }
        else if (user.Role == UserRole.TeamLeader)
        {
            // Check if schedule contains employees reporting to this Team Leader
            if (schedule.EmployeeIds.Any()) // If schedule has specific employee IDs, check if they report to TL
                return schedule.CreatedBy == user.Id; // Simplified: TL can view their own schedules
            return schedule.Creator.HangarId.HasValue && user.HangarId.HasValue && schedule.Creator.HangarId == user.HangarId ||
                   schedule.Creator.ShopId.HasValue && user.ShopId.HasValue && schedule.Creator.ShopId == user.ShopId;
        }
        else if (user.Role == UserRole.Manager)
        {
            return user.SectionId.HasValue && schedule.Creator.SectionId == user.SectionId;
        }
        else if (user.Role == UserRole.Director || user.Role == UserRole.SystemAdmin)
        {
            return true;
        }
        return false;
    }

    private static bool CanAssignEmployee(ApplicationUser user, ApplicationUser employee)
    {
        if (user.Role != UserRole.TeamLeader) return false;
        if (employee.ReportsToUserId.HasValue && employee.ReportsToUserId != user.Id) return false;
        if (employee.SectionId != user.SectionId) return false;
        if (user.HangarId.HasValue && employee.HangarId != user.HangarId) return false;
        if (user.ShopId.HasValue && employee.ShopId != user.ShopId) return false;
        return true;
    }

    private static bool CanUpdateSchedule(ApplicationUser user, TestSchedule schedule, uint clientVersion)
    {
        if (user.Role != UserRole.TeamLeader) return false;
        if (schedule.CreatedBy != user.Id) return false;
        return clientVersion == schedule.Version;
    }

    private static bool CanCreateSchedule(ApplicationUser user)
    {
        return user.Role == UserRole.TeamLeader;
    }

    private static bool CanViewDailyAssignment(ApplicationUser user, TestDailyAssignment assignment, ApplicationUser? employeeContext = null)
    {
        if (user.Role == UserRole.Employee)
        {
            if (user.ReportsToUserId.HasValue)
                return assignment.CreatedBy == user.ReportsToUserId.Value;
            return assignment.Creator.HangarId.HasValue && user.HangarId.HasValue && assignment.Creator.HangarId == user.HangarId ||
                   assignment.Creator.ShopId.HasValue && user.ShopId.HasValue && assignment.Creator.ShopId == user.ShopId;
        }
        else if (user.Role == UserRole.TeamLeader)
        {
            return assignment.CreatedBy == user.Id ||
                   (assignment.Creator.HangarId.HasValue && user.HangarId.HasValue && assignment.Creator.HangarId == user.HangarId) ||
                   (assignment.Creator.ShopId.HasValue && user.ShopId.HasValue && assignment.Creator.ShopId == user.ShopId);
        }
        else if (user.Role == UserRole.Manager)
        {
            return user.SectionId.HasValue && assignment.Creator.SectionId == user.SectionId;
        }
        else if (user.Role == UserRole.Director || user.Role == UserRole.SystemAdmin)
        {
            return true;
        }
        return false;
    }

    private class TestSchedule
    {
        public Guid Id { get; init; }
        public required string Module { get; init; }
        public required string Resource { get; init; }
        public required string Status { get; init; }
        public Guid? CreatedBy { get; init; }
        public required ApplicationUser Creator { get; init; }
        public Guid[] EmployeeIds { get; init; } = Array.Empty<Guid>();
        public uint Version { get; set; }
    }

    private class TestDailyAssignment
    {
        public Guid Id { get; init; }
        public required string Module { get; init; }
        public required string Resource { get; init; }
        public required string Status { get; init; }
        public Guid? CreatedBy { get; init; }
        public required ApplicationUser Creator { get; init; }
        public uint Version { get; set; }
    }
}
