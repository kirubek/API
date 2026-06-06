using BaseOps.Domain.Entities;
using BaseOps.Infrastructure.Data.SeedData;
using Microsoft.EntityFrameworkCore;
using DomainDailyStatusReport = BaseOps.Domain.Entities.DailyStatusReport;
using DomainTaskStatus = BaseOps.Domain.Entities.TaskStatus;

namespace BaseOps.Infrastructure.Data;

public sealed class BaseOpsDbContext(DbContextOptions<BaseOpsDbContext> options) : DbContext(options)
{
    // Note: Global query filters for ApplicationUser require user context injection
    // Current implementation uses service-layer RBAC filtering which provides equivalent security
    // Global filters can be added later with ICurrentUserContext service for defense-in-depth

    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Hangar> Hangars => Set<Hangar>();
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<UserWorkspaceAssignment> UserWorkspaceAssignments => Set<UserWorkspaceAssignment>();
    public DbSet<OperationalRecord> OperationalRecords => Set<OperationalRecord>();
    public DbSet<DailyAssignment> DailyAssignments => Set<DailyAssignment>();
    public DbSet<AssignmentDetail> AssignmentDetails => Set<AssignmentDetail>();
    public DbSet<Handover> Handovers => Set<Handover>();
    public DbSet<HandoverSignature> HandoverSignatures => Set<HandoverSignature>();
    public DbSet<HandoverTask> HandoverTasks => Set<HandoverTask>();
    public DbSet<HandoverDefect> HandoverDefects => Set<HandoverDefect>();
    public DbSet<HandoverIssue> HandoverIssues => Set<HandoverIssue>();
    public DbSet<HandoverWorkStatus> HandoverWorkStatuses => Set<HandoverWorkStatus>();
    public DbSet<HandoverManningStatus> HandoverManningStatuses => Set<HandoverManningStatus>();
    public DbSet<HandoverAircraft> HandoverAircrafts => Set<HandoverAircraft>();
    public DbSet<AnnualLeaveRequest> AnnualLeaveRequests => Set<AnnualLeaveRequest>();
    public DbSet<AnnualLeavePlan> AnnualLeavePlans => Set<AnnualLeavePlan>();
    public DbSet<AnnualLeavePlanEntry> AnnualLeavePlanEntries => Set<AnnualLeavePlanEntry>();
    public DbSet<LeaveChoice> LeaveChoices => Set<LeaveChoice>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<ManpowerConstraint> ManpowerConstraints => Set<ManpowerConstraint>();
    public DbSet<ManpowerConstraintPeriod> ManpowerConstraintPeriods => Set<ManpowerConstraintPeriod>();
    public DbSet<Bulletin> Bulletins => Set<Bulletin>();
    public DbSet<BulletinAttachment> BulletinAttachments => Set<BulletinAttachment>();
    public DbSet<BulletinReadStatus> BulletinReadStatuses => Set<BulletinReadStatus>();
    public DbSet<AceAttachment> AceAttachments => Set<AceAttachment>();
    public DbSet<SafaInspection> SafaInspections => Set<SafaInspection>();
    public DbSet<SafaDefect> SafaDefects => Set<SafaDefect>();
    public DbSet<SafaTemplate> SafaTemplates => Set<SafaTemplate>();
    public DbSet<CarryOverReport> CarryOverReports => Set<CarryOverReport>();
    public DbSet<CarryOverTask> CarryOverTasks => Set<CarryOverTask>();
    public DbSet<CarryOverReview> CarryOverReviews => Set<CarryOverReview>();
    public DbSet<MaintenanceProject> MaintenanceProjects => Set<MaintenanceProject>();
    public DbSet<DailyProgressLog> DailyProgressLogs => Set<DailyProgressLog>();
    public DbSet<PartFollowUp> PartFollowUps => Set<PartFollowUp>();
    public DbSet<PostMortemReport> PostMortemReports => Set<PostMortemReport>();
    public DbSet<PostMortemSlaRecord> PostMortemSlaRecords => Set<PostMortemSlaRecord>();
    public DbSet<PostMortemCrsCompletion> PostMortemCrsCompletions => Set<PostMortemCrsCompletion>();
    public DbSet<PostMortemTatRecord> PostMortemTatRecords => Set<PostMortemTatRecord>();
    public DbSet<PostMortemPlanStability> PostMortemPlanStabilityRecords => Set<PostMortemPlanStability>();
    public DbSet<PostMortemCarryOverTask> PostMortemCarryOverTasks => Set<PostMortemCarryOverTask>();
    public DbSet<DomainDailyStatusReport> DailyStatusReports => Set<DomainDailyStatusReport>();
    public DbSet<DomainTaskStatus> TaskStatuses => Set<DomainTaskStatus>();
    public DbSet<PartIssue> PartIssues => Set<PartIssue>();
    public DbSet<MajorFinding> MajorFindings => Set<MajorFinding>();
    public DbSet<ImportHistory> ImportHistories => Set<ImportHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Workspace>(b =>
        {
            b.ToTable("workspaces");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Code).HasMaxLength(40).IsRequired();
            b.Property(x => x.Description).HasMaxLength(500);
            b.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Section>(b =>
        {
            b.ToTable("sections");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(160).IsRequired();
            b.Property(x => x.Code).HasMaxLength(40).IsRequired();
            b.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Hangar>(b =>
        {
            b.ToTable("hangars");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(160).IsRequired();
            b.Property(x => x.Code).HasMaxLength(40).IsRequired();
            b.HasIndex(x => new { x.SectionId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.SectionId, x.Name }).IsUnique();
            b.HasOne(x => x.Section).WithMany(x => x.Hangars).HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Shop>(b =>
        {
            b.ToTable("shops");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(160).IsRequired();
            b.Property(x => x.Code).HasMaxLength(40).IsRequired();
            b.HasIndex(x => new { x.SectionId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.SectionId, x.Name }).IsUnique();
            b.HasOne(x => x.Section).WithMany(x => x.Shops).HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ApplicationUser>(b =>
        {
            b.ToTable("users");
            b.HasKey(x => x.Id);
            b.Property(x => x.EmployeeId).HasMaxLength(40).IsRequired();
            b.Property(x => x.FullName).HasMaxLength(180).IsRequired();
            b.Property(x => x.Email).HasMaxLength(256);
            b.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            b.Property(x => x.Role).HasConversion<string>().HasMaxLength(40).IsRequired();
            b.Property(x => x.CompanyAuthorizationType).HasConversion<string>();
            
            // Employee profile contact fields
            b.Property(x => x.PhoneNumber).HasMaxLength(50);
            b.Property(x => x.Address).HasMaxLength(500);
            b.Property(x => x.EmergencyContactName).HasMaxLength(180);
            b.Property(x => x.EmergencyContactPhoneNumber).HasMaxLength(50);
            b.Property(x => x.ProfilePhotoUrl).HasMaxLength(500);
            
            // Concurrency token
            b.Property(x => x.RowVersion).IsRowVersion();
            
            // Performance indexes
            b.HasIndex(x => x.EmployeeId).IsUnique();
            b.HasIndex(x => x.Email).IsUnique();
            b.HasIndex(x => x.Role);
            b.HasIndex(x => x.FullName);
            b.HasIndex(x => x.SectionId);
            b.HasIndex(x => x.HangarId);
            b.HasIndex(x => x.ShopId);
            b.HasIndex(x => x.ReportsToUserId);
            b.HasIndex(x => new { x.SectionId, x.HangarId });
            b.HasIndex(x => new { x.SectionId, x.ShopId });
            b.HasIndex(x => new { x.SectionId, x.Role });
            b.HasIndex(x => new { x.HangarId, x.Role });
            b.HasIndex(x => new { x.ShopId, x.Role });
            
            b.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Hangar).WithMany().HasForeignKey(x => x.HangarId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Shop).WithMany().HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.ReportsToUser).WithMany().HasForeignKey(x => x.ReportsToUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserWorkspaceAssignment>(b =>
        {
            b.ToTable("user_workspace_assignments");
            b.HasKey(x => x.Id);
            b.Property(x => x.WorkspaceType).HasMaxLength(40).IsRequired();
            b.Property(x => x.AssignmentType).HasMaxLength(40).IsRequired();
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.SectionId);
            b.HasIndex(x => x.HangarId);
            b.HasIndex(x => x.ShopId);
            b.HasIndex(x => new { x.UserId, x.WorkspaceType, x.SectionId, x.HangarId, x.ShopId }).IsUnique();
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Hangar).WithMany().HasForeignKey(x => x.HangarId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Shop).WithMany().HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefreshToken>(b =>
        {
            b.ToTable("refresh_tokens");
            b.HasKey(x => x.Id);
            b.Property(x => x.TokenHash).HasMaxLength(512).IsRequired();
            b.HasIndex(x => x.TokenHash).IsUnique();
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RevokedToken>(b =>
        {
            b.ToTable("revoked_tokens");
            b.HasKey(x => x.Id);
            b.Property(x => x.JwtId).HasMaxLength(128).IsRequired();
            b.HasIndex(x => x.JwtId).IsUnique();
        });

        modelBuilder.Entity<UserSession>(b =>
        {
            b.ToTable("user_sessions");
            b.HasKey(x => x.Id);
            b.Property(x => x.CorrelationId).HasMaxLength(128).IsRequired();
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(b =>
        {
            b.ToTable("audit_logs");
            b.HasKey(x => x.Id);
            b.Property(x => x.Action).HasMaxLength(120).IsRequired();
            b.Property(x => x.EntityName).HasMaxLength(160).IsRequired();
            b.Property(x => x.CorrelationId).HasMaxLength(128).IsRequired();
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.CorrelationId);
            b.HasIndex(x => x.Action);
        });

        modelBuilder.Entity<OperationalRecord>(b =>
        {
            b.ToTable("operational_records");
            b.HasKey(x => x.Id);
            b.Property(x => x.Module).HasMaxLength(80).IsRequired();
            b.Property(x => x.Resource).HasMaxLength(120);
            b.Property(x => x.Action).HasMaxLength(120);
            b.Property(x => x.PayloadJson).IsRequired();
            b.Property(x => x.Status).HasMaxLength(40).IsRequired();
            b.Property(x => x.Version).IsConcurrencyToken();
            b.HasIndex(x => x.Module);
            b.HasIndex(x => x.Resource);
            b.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<DailyAssignment>(b =>
        {
            b.ToTable("daily_assignments");
            b.HasKey(x => x.Id);
            b.Property(x => x.AircraftType).HasMaxLength(40);
            b.Property(x => x.AircraftRegistration).HasMaxLength(20);
            b.Property(x => x.Status).HasMaxLength(40).IsRequired();
            b.Property(x => x.Shift).HasMaxLength(40);
            b.Property(x => x.Version).IsConcurrencyToken();
            
            // Performance indexes for common queries
            b.HasIndex(x => x.Date);
            b.HasIndex(x => x.SectionId);
            b.HasIndex(x => x.HangarId);
            b.HasIndex(x => x.ShopId);
            b.HasIndex(x => x.TeamLeaderId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => new { x.Date, x.SectionId });
            b.HasIndex(x => new { x.Date, x.HangarId });
            b.HasIndex(x => new { x.Date, x.ShopId });
            b.HasIndex(x => new { x.Date, x.TeamLeaderId });
            
            b.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Hangar).WithMany().HasForeignKey(x => x.HangarId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Shop).WithMany().HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.TeamLeader).WithMany().HasForeignKey(x => x.TeamLeaderId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AssignmentDetail>(b =>
        {
            b.ToTable("assignment_details");
            b.HasKey(x => x.Id);
            b.Property(x => x.AssignedAircraft).HasMaxLength(20).IsRequired();
            b.Property(x => x.TaskDescription).HasMaxLength(500).IsRequired();

            // Performance indexes
            b.HasIndex(x => x.DailyAssignmentId);
            b.HasIndex(x => x.EmployeeId);
            b.HasIndex(x => new { x.DailyAssignmentId, x.EmployeeId });

            b.HasOne(x => x.DailyAssignment).WithMany(x => x.Details).HasForeignKey(x => x.DailyAssignmentId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        // Handover entity configuration
        modelBuilder.Entity<Handover>(b =>
        {
            b.ToTable("handovers");
            b.HasKey(x => x.Id);
            b.Property(x => x.TemplateType).HasConversion<int>().IsRequired();
            b.Property(x => x.Status).HasConversion<int>().IsRequired();
            b.Property(x => x.ShiftType).HasConversion<int>().IsRequired();
            b.Property(x => x.DutyTeamLeaderName).HasMaxLength(180).IsRequired();
            b.Property(x => x.IsDeleted).IsRequired();
            b.Property(x => x.Version).IsConcurrencyToken();

            // Performance indexes for common queries
            b.HasIndex(x => x.Date);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.SectionId);
            b.HasIndex(x => x.HangarId);
            b.HasIndex(x => x.ShopId);
            b.HasIndex(x => x.OutgoingTeamLeaderId);
            b.HasIndex(x => x.IncomingTeamLeaderId);
            b.HasIndex(x => x.SubmittedAt);
            b.HasIndex(x => x.AcceptedAt);
            b.HasIndex(x => x.IsDeleted);
            b.HasIndex(x => new { x.Date, x.SectionId });
            b.HasIndex(x => new { x.Date, x.HangarId });
            b.HasIndex(x => new { x.Date, x.ShopId });
            b.HasIndex(x => new { x.Status, x.SubmittedAt });

            // Filtered index for overdue pending handovers query optimization
            b.HasIndex(x => x.SubmittedAt)
                .HasFilter("Status = 2 AND SubmittedAt IS NOT NULL");

            // Global query filter for soft delete
            b.HasQueryFilter(x => !x.IsDeleted);

            b.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Hangar).WithMany().HasForeignKey(x => x.HangarId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Shop).WithMany().HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.OutgoingTeamLeader).WithMany().HasForeignKey(x => x.OutgoingTeamLeaderId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.IncomingTeamLeader).WithMany().HasForeignKey(x => x.IncomingTeamLeaderId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HandoverSignature>(b =>
        {
            b.ToTable("handover_signatures");
            b.HasKey(x => x.Id);
            b.Property(x => x.SignatureRole).HasConversion<int>().IsRequired();
            b.Property(x => x.SignatureData).IsRequired();
            b.Property(x => x.SignatureName).HasMaxLength(180).IsRequired();

            b.HasIndex(x => x.HandoverId);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => new { x.HandoverId, x.SignatureRole }).IsUnique();
            b.HasQueryFilter(x => !x.Handover.IsDeleted);

            b.HasOne(x => x.Handover).WithMany(x => x.Signatures).HasForeignKey(x => x.HandoverId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HandoverTask>(b =>
        {
            b.ToTable("handover_tasks");
            b.HasKey(x => x.Id);
            b.Property(x => x.TaskType).HasConversion<int>().IsRequired();
            b.Property(x => x.AircraftRegistration).HasMaxLength(20);
            b.Property(x => x.TaskCardCode).HasMaxLength(40);
            b.Property(x => x.Description).IsRequired();

            b.HasIndex(x => x.HandoverId);
            b.HasIndex(x => x.TaskType);
            b.HasIndex(x => x.CreatedByUserId);
            b.HasQueryFilter(x => !x.Handover.IsDeleted);

            b.HasOne(x => x.Handover).WithMany(x => x.Tasks).HasForeignKey(x => x.HandoverId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HandoverDefect>(b =>
        {
            b.ToTable("handover_defects");
            b.HasKey(x => x.Id);
            b.Property(x => x.AircraftRegistration).HasMaxLength(20).IsRequired();
            b.Property(x => x.DefectDescription).IsRequired();
            b.Property(x => x.NonRoutineCardNumber).HasMaxLength(40).IsRequired();
            b.Property(x => x.ItemStatus).HasMaxLength(40).IsRequired();

            b.HasIndex(x => x.HandoverId);
            b.HasIndex(x => x.AircraftRegistration);
            b.HasIndex(x => x.ItemStatus);
            b.HasQueryFilter(x => !x.Handover.IsDeleted);

            b.HasOne(x => x.Handover).WithMany(x => x.Defects).HasForeignKey(x => x.HandoverId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HandoverIssue>(b =>
        {
            b.ToTable("handover_issues");
            b.HasKey(x => x.Id);
            b.Property(x => x.IssueType).HasConversion<int>().IsRequired();
            b.Property(x => x.Description).IsRequired();

            b.HasIndex(x => x.HandoverId);
            b.HasIndex(x => x.IssueType);
            b.HasQueryFilter(x => !x.Handover.IsDeleted);

            b.HasOne(x => x.Handover).WithMany(x => x.Issues).HasForeignKey(x => x.HandoverId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HandoverWorkStatus>(b =>
        {
            b.ToTable("handover_work_statuses");
            b.HasKey(x => x.Id);
            b.Property(x => x.MfgPartNumber).HasMaxLength(40).IsRequired();
            b.Property(x => x.SerialNumber).HasMaxLength(40).IsRequired();
            b.Property(x => x.WorkCarriedOut).IsRequired();
            b.Property(x => x.WorkToBeDone).IsRequired();
            b.Property(x => x.OutstandingIssue).IsRequired();

            b.HasIndex(x => x.HandoverId);
            b.HasQueryFilter(x => !x.Handover.IsDeleted);

            b.HasOne(x => x.Handover).WithMany(x => x.WorkStatuses).HasForeignKey(x => x.HandoverId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HandoverManningStatus>(b =>
        {
            b.ToTable("handover_manning_statuses");
            b.HasKey(x => x.Id);

            b.HasIndex(x => x.HandoverId).IsUnique();
            b.HasQueryFilter(x => !x.Handover.IsDeleted);

            b.HasOne(x => x.Handover).WithOne(x => x.ManningStatus).HasForeignKey<HandoverManningStatus>(x => x.HandoverId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HandoverAircraft>(b =>
        {
            b.ToTable("handover_aircrafts");
            b.HasKey(x => x.Id);
            b.Property(x => x.AircraftType).HasMaxLength(100).IsRequired();
            b.Property(x => x.AircraftRegistration).HasMaxLength(20).IsRequired();
            b.Property(x => x.MaintenanceType).HasMaxLength(100).IsRequired();
            b.HasIndex(x => x.HandoverId);
            b.HasQueryFilter(x => !x.Handover.IsDeleted);
            b.HasOne(x => x.Handover).WithMany(x => x.Aircrafts).HasForeignKey(x => x.HandoverId).OnDelete(DeleteBehavior.Cascade);
        });

        // Annual Leave Request configuration
        modelBuilder.Entity<AnnualLeaveRequest>(b =>
        {
            b.ToTable("annual_leave_requests");
            b.HasKey(x => x.Id);
            b.Property(x => x.RoleAtSubmission).HasConversion<int>();
            b.Property(x => x.LeaveType).HasConversion<int>();
            b.Property(x => x.Status).HasConversion<int>();
            b.Property(x => x.RejectionReason).HasMaxLength(500);
            b.Property(x => x.Version).IsRowVersion();
            
            // Performance indexes
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.SectionId);
            b.HasIndex(x => x.HangarId);
            b.HasIndex(x => x.ShopId);
            b.HasIndex(x => x.SubmittedToUserId);
            b.HasIndex(x => x.Year);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => new { x.UserId, x.Year }).IsUnique();
            b.HasIndex(x => new { x.SectionId, x.Year });
            b.HasIndex(x => new { x.HangarId, x.Year });
            
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Hangar).WithMany().HasForeignKey(x => x.HangarId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Shop).WithMany().HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.SubmittedToUser).WithMany().HasForeignKey(x => x.SubmittedToUserId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.ReviewedByUser).WithMany().HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // Annual Leave Plan configuration
        modelBuilder.Entity<AnnualLeavePlan>(b =>
        {
            b.ToTable("annual_leave_plans");
            b.HasKey(x => x.Id);
            b.Property(x => x.Level).HasConversion<int>();
            b.Property(x => x.Status).HasConversion<int>();
            b.Property(x => x.GenerationNotes).HasMaxLength(1000);
            b.Property(x => x.Version).IsRowVersion();
            
            // Performance indexes
            b.HasIndex(x => x.SectionId);
            b.HasIndex(x => x.HangarId);
            b.HasIndex(x => x.ShopId);
            b.HasIndex(x => x.TeamLeaderId);
            b.HasIndex(x => x.Year);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.Level);
            b.HasIndex(x => new { x.SectionId, x.Year, x.Level }).IsUnique();
            b.HasIndex(x => new { x.HangarId, x.Year, x.Level }).IsUnique();
            
            b.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Hangar).WithMany().HasForeignKey(x => x.HangarId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Shop).WithMany().HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.TeamLeader).WithMany().HasForeignKey(x => x.TeamLeaderId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.FinalizedByUser).WithMany().HasForeignKey(x => x.FinalizedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // Annual Leave Plan Entry configuration
        modelBuilder.Entity<AnnualLeavePlanEntry>(b =>
        {
            b.ToTable("annual_leave_plan_entries");
            b.HasKey(x => x.Id);
            b.Property(x => x.SourceChoice).HasConversion<int>();
            b.Property(x => x.AdjustmentReason).HasMaxLength(500);
            b.Property(x => x.Version).IsRowVersion();
            
            // Performance indexes
            b.HasIndex(x => x.AnnualLeavePlanId);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.AnnualLeaveRequestId);
            b.HasIndex(x => x.PriorityScore);
            b.HasIndex(x => x.IsManuallyAdjusted);
            b.HasIndex(x => new { x.AnnualLeavePlanId, x.UserId });
            
            b.HasOne(x => x.AnnualLeavePlan).WithMany(x => x.Entries).HasForeignKey(x => x.AnnualLeavePlanId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.AnnualLeaveRequest).WithOne(x => x.ApprovedPlanEntry).HasForeignKey<AnnualLeavePlanEntry>(x => x.AnnualLeaveRequestId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.ManuallyAdjustedByUser).WithMany().HasForeignKey(x => x.ManuallyAdjustedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // Leave Choice configuration
        modelBuilder.Entity<LeaveChoice>(b =>
        {
            b.ToTable("leave_choices");
            b.HasKey(x => x.Id);
            
            // Performance indexes
            b.HasIndex(x => x.AnnualLeaveRequestId);
            b.HasIndex(x => x.ChoiceNumber);
            b.HasIndex(x => new { x.AnnualLeaveRequestId, x.ChoiceNumber }).IsUnique();
            
            b.HasOne(x => x.AnnualLeaveRequest).WithMany(x => x.LeaveChoices).HasForeignKey(x => x.AnnualLeaveRequestId).OnDelete(DeleteBehavior.Cascade);
        });

        // Leave Balance configuration
        modelBuilder.Entity<LeaveBalance>(b =>
        {
            b.ToTable("leave_balances");
            b.HasKey(x => x.Id);
            
            // Performance indexes
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.Year);
            b.HasIndex(x => new { x.UserId, x.Year }).IsUnique();
            
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        // Manpower Constraint configuration
        modelBuilder.Entity<ManpowerConstraint>(b =>
        {
            b.ToTable("manpower_constraints");
            b.HasKey(x => x.Id);
            b.Property(x => x.MaxLeavePercentage).HasPrecision(5, 2);
            b.Property(x => x.MinCoveragePercentage).HasPrecision(5, 2);

            // Performance indexes
            b.HasIndex(x => x.SectionId);
            b.HasIndex(x => x.HangarId);
            b.HasIndex(x => x.ShopId);
            b.HasIndex(x => x.Year);
            b.HasIndex(x => x.IsActive);
            // Remove unique constraints to allow multiple constraint levels per section/year
            b.HasIndex(x => new { x.SectionId, x.Year });
            b.HasIndex(x => new { x.HangarId, x.Year });

            b.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Hangar).WithMany().HasForeignKey(x => x.HangarId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Shop).WithMany().HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
        });

        // Manpower Constraint Period configuration
        modelBuilder.Entity<ManpowerConstraintPeriod>(b =>
        {
            b.ToTable("manpower_constraint_periods");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(200);
            b.Property(x => x.MaxLeavePercentage).HasPrecision(5, 2);
            b.Property(x => x.MinCoveragePercentage).HasPrecision(5, 2);
            
            // Performance indexes
            b.HasIndex(x => x.ManpowerConstraintId);
            b.HasIndex(x => x.StartDate);
            b.HasIndex(x => x.EndDate);
            
            b.HasOne(x => x.ManpowerConstraint).WithMany(x => x.SpecialPeriods).HasForeignKey(x => x.ManpowerConstraintId).OnDelete(DeleteBehavior.Cascade);
        });

        // Bulletin configuration
        modelBuilder.Entity<Bulletin>(b =>
        {
            b.ToTable("bulletins");
            b.HasKey(x => x.Id);
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Content).IsRequired();
            b.Property(x => x.Category).HasConversion<int>().IsRequired();
            b.Property(x => x.Priority).HasConversion<int>().IsRequired();
            b.Property(x => x.Scope).HasConversion<int>().IsRequired();
            b.Property(x => x.ExpiryDate).IsRequired();
            b.Property(x => x.IsActive).IsRequired();
            b.Property(x => x.Pinned).IsRequired();
            b.Property(x => x.Version).IsConcurrencyToken();

            // Performance indexes for common queries
            b.HasIndex(x => x.ExpiryDate);
            b.HasIndex(x => x.Priority);
            b.HasIndex(x => x.Scope);
            b.HasIndex(x => x.SectionId);
            b.HasIndex(x => x.HangarId);
            b.HasIndex(x => x.ShopId);
            b.HasIndex(x => x.CreatedBy);
            b.HasIndex(x => x.IsActive);
            b.HasIndex(x => new { x.Scope, x.SectionId });
            b.HasIndex(x => new { x.Scope, x.HangarId });
            b.HasIndex(x => new { x.Scope, x.ShopId });
            b.HasIndex(x => new { x.IsActive, x.ExpiryDate });

            b.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Hangar).WithMany().HasForeignKey(x => x.HangarId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Shop).WithMany().HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // Bulletin Attachment configuration
        modelBuilder.Entity<BulletinAttachment>(b =>
        {
            b.ToTable("bulletin_attachments");
            b.HasKey(x => x.Id);
            b.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
            b.Property(x => x.GeneratedFileName).HasMaxLength(255).IsRequired();
            b.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            b.Property(x => x.FileSize).IsRequired();
            b.Property(x => x.StoragePath).HasMaxLength(500).IsRequired();

            // Performance indexes
            b.HasIndex(x => x.BulletinId);
            b.HasIndex(x => x.UploadedBy);

            b.HasOne(x => x.Bulletin).WithMany(x => x.Attachments).HasForeignKey(x => x.BulletinId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Uploader).WithMany().HasForeignKey(x => x.UploadedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // Bulletin Read Status configuration
        modelBuilder.Entity<BulletinReadStatus>(b =>
        {
            b.ToTable("bulletin_read_status");
            b.HasKey(x => x.Id);
            b.Property(x => x.ReadAt).IsRequired();

            // Performance indexes
            b.HasIndex(x => x.BulletinId);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => new { x.BulletinId, x.UserId }).IsUnique();
            b.HasIndex(x => x.ReadAt);

            b.HasOne(x => x.Bulletin).WithMany(x => x.ReadStatuses).HasForeignKey(x => x.BulletinId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // ACE Attachment configuration
        modelBuilder.Entity<AceAttachment>(b =>
        {
            b.ToTable("ace_attachments");
            b.HasKey(x => x.Id);
            b.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
            b.Property(x => x.GeneratedFileName).HasMaxLength(255).IsRequired();
            b.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            b.Property(x => x.FileSize).IsRequired();
            b.Property(x => x.StoragePath).HasMaxLength(500).IsRequired();

            // Performance indexes
            b.HasIndex(x => x.ActivityId);
            b.HasIndex(x => x.UploadedBy);

            // Configure foreign key without navigation property relationship
            b.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UploadedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // SAFA Inspection configuration
        modelBuilder.Entity<SafaInspection>(b =>
        {
            b.ToTable("safa_inspections");
            b.HasKey(x => x.Id);
            b.Property(x => x.InspectionType).HasConversion<int>().IsRequired();
            b.Property(x => x.FleetType).HasMaxLength(50).IsRequired();
            b.Property(x => x.AircraftRegistration).HasMaxLength(20).IsRequired();
            b.Property(x => x.FlightInfo).HasMaxLength(20);
            b.Property(x => x.Shift).HasMaxLength(40).IsRequired();
            b.Property(x => x.Status).HasConversion<int>().IsRequired();
            b.Property(x => x.Conclusion).HasMaxLength(1000);
            b.Property(x => x.SubmittedBy).HasMaxLength(180);
            b.Property(x => x.Version).IsConcurrencyToken();

            // Performance indexes for common queries
            b.HasIndex(x => x.InspectionType);
            b.HasIndex(x => x.AircraftRegistration);
            b.HasIndex(x => x.FleetType);
            b.HasIndex(x => x.InspectionDate);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.SectionId);
            b.HasIndex(x => x.HangarId);
            b.HasIndex(x => x.ShopId);
            b.HasIndex(x => x.InspectorId);
            b.HasIndex(x => new { x.SectionId, x.HangarId });
            b.HasIndex(x => new { x.SectionId, x.ShopId });
            b.HasIndex(x => new { x.InspectionDate, x.Status });
            b.HasIndex(x => new { x.InspectorId, x.Status });

            b.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Hangar).WithMany().HasForeignKey(x => x.HangarId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Shop).WithMany().HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Inspector).WithMany().HasForeignKey(x => x.InspectorId).OnDelete(DeleteBehavior.Restrict);
        });

        // SAFA Defect configuration
        modelBuilder.Entity<SafaDefect>(b =>
        {
            b.ToTable("safa_defects");
            b.HasKey(x => x.Id);
            b.Property(x => x.Category).HasMaxLength(100).IsRequired();
            b.Property(x => x.SubCategory).HasMaxLength(100);
            b.Property(x => x.StandardDescription).IsRequired();
            b.Property(x => x.ObservationFinding).IsRequired();
            b.Property(x => x.Status).HasConversion<int>().IsRequired();
            b.Property(x => x.CorrectiveAction).HasMaxLength(1000);
            b.Property(x => x.TaskCardCode).HasMaxLength(50);
            b.Property(x => x.PartRequestId).HasMaxLength(50);
            b.Property(x => x.Remarks).HasMaxLength(500);
            b.Property(x => x.Version).IsConcurrencyToken();

            // Performance indexes for common queries
            b.HasIndex(x => x.InspectionId);
            b.HasIndex(x => x.Category);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.ActionTakenByUserId);
            b.HasIndex(x => new { x.InspectionId, x.Status });
            b.HasIndex(x => new { x.Category, x.Status });

            b.HasOne(x => x.Inspection).WithMany(x => x.Defects).HasForeignKey(x => x.InspectionId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.ActionTakenByUser).WithMany().HasForeignKey(x => x.ActionTakenByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // SAFA Template configuration
        modelBuilder.Entity<SafaTemplate>(b =>
        {
            b.ToTable("safa_templates");
            b.HasKey(x => x.Id);
            b.Property(x => x.InspectionType).HasConversion<int>().IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(500);
            b.Property(x => x.TemplateJson).IsRequired();
            b.Property(x => x.IsActive).IsRequired();
            b.Property(x => x.Version).IsConcurrencyToken();

            // Performance indexes
            b.HasIndex(x => x.InspectionType);
            b.HasIndex(x => x.IsActive);
            b.HasIndex(x => new { x.InspectionType, x.IsActive });
        });

        // CarryOverReport configuration
        modelBuilder.Entity<CarryOverReport>(b =>
        {
            b.ToTable("carry_over_reports");
            b.HasKey(x => x.Id);
            b.Property(x => x.ReportNumber).HasMaxLength(50).IsRequired();
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(1000);
            b.Property(x => x.AircraftRegistration).HasMaxLength(20).IsRequired();
            b.Property(x => x.AircraftType).HasMaxLength(100).IsRequired();
            b.Property(x => x.ProjectType).HasMaxLength(50).IsRequired();
            b.Property(x => x.Priority).HasMaxLength(20).IsRequired();
            b.Property(x => x.Status).HasConversion<int>().IsRequired();
            b.Property(x => x.Remarks).HasMaxLength(1000);
            b.Property(x => x.ReviewComments).HasMaxLength(1000);
            b.Property(x => x.Version).IsConcurrencyToken();

            // Performance indexes
            b.HasIndex(x => x.ReportNumber).IsUnique();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.SectionId);
            b.HasIndex(x => x.HangarId);
            b.HasIndex(x => x.AssignedToUserId);
            b.HasIndex(x => x.DueDate);
            b.HasIndex(x => new { x.SectionId, x.Status });
            b.HasIndex(x => new { x.Status, x.DueDate });

            b.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Hangar).WithMany().HasForeignKey(x => x.HangarId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.AssignedToUser).WithMany().HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.SubmittedByUser).WithMany().HasForeignKey(x => x.SubmittedByUserId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.ReviewedByUser).WithMany().HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.FinalizedByUser).WithMany().HasForeignKey(x => x.FinalizedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // CarryOverTask configuration
        modelBuilder.Entity<CarryOverTask>(b =>
        {
            b.ToTable("carry_over_tasks");
            b.HasKey(x => x.Id);
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(1000);
            b.Property(x => x.Category).HasMaxLength(100).IsRequired();
            b.Property(x => x.Priority).HasMaxLength(20).IsRequired();
            b.Property(x => x.Status).HasConversion<int>().IsRequired();
            b.Property(x => x.TaskType).HasConversion<int>().IsRequired();
            b.Property(x => x.DeferralReason).HasConversion<int>().IsRequired();
            b.Property(x => x.DeferralDetails).HasMaxLength(500);
            b.Property(x => x.DeferredTaskOrigin).HasConversion<int>().IsRequired();
            b.Property(x => x.Notes).HasMaxLength(1000);
            b.Property(x => x.TaskCardNumber).HasMaxLength(50);
            b.Property(x => x.PartRequestId).HasMaxLength(50);
            b.Property(x => x.EstimatedHours).HasPrecision(18, 2);
            b.Property(x => x.ActualHours).HasPrecision(18, 2);
            b.Property(x => x.Version).IsConcurrencyToken();

            // Performance indexes
            b.HasIndex(x => x.CarryOverReportId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.TaskType);
            b.HasIndex(x => x.AssignedToUserId);
            b.HasIndex(x => x.DueDate);
            b.HasIndex(x => new { x.CarryOverReportId, x.Status });
            b.HasIndex(x => new { x.Status, x.DueDate });

            b.HasOne(x => x.Report).WithMany(x => x.Tasks).HasForeignKey(x => x.CarryOverReportId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.AssignedToUser).WithMany().HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // CarryOverReview configuration
        modelBuilder.Entity<CarryOverReview>(b =>
        {
            b.ToTable("carry_over_reviews");
            b.HasKey(x => x.Id);
            b.Property(x => x.ReviewerUserId).IsRequired();
            b.Property(x => x.Comments).HasMaxLength(1000);
            b.Property(x => x.RejectionReason).HasMaxLength(500);
            b.Property(x => x.Version).IsConcurrencyToken();

            // Performance indexes
            b.HasIndex(x => x.CarryOverReportId);
            b.HasIndex(x => x.ReviewerUserId);
            b.HasIndex(x => x.ReviewedAt);
            b.HasIndex(x => new { x.CarryOverReportId, x.ReviewerUserId });

            b.HasOne(x => x.Report).WithMany(x => x.Reviews).HasForeignKey(x => x.CarryOverReportId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.ReviewerUser).WithMany().HasForeignKey(x => x.ReviewerUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // MaintenanceProject configuration
        modelBuilder.Entity<MaintenanceProject>(b =>
        {
            b.ToTable("maintenance_projects");
            b.HasKey(x => x.Id);
            b.Property(x => x.ProjectNumber).HasMaxLength(50).IsRequired();
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(1000);
            b.Property(x => x.AircraftRegistration).HasMaxLength(20).IsRequired();
            b.Property(x => x.AircraftType).HasMaxLength(100).IsRequired();
            b.Property(x => x.FleetType).HasMaxLength(100).IsRequired();
            b.Property(x => x.Type).HasConversion<int>().IsRequired();
            b.Property(x => x.Status).HasConversion<int>().IsRequired();
            b.Property(x => x.Remarks).HasMaxLength(1000);
            b.Property(x => x.DelayReason).HasMaxLength(500);
            b.Property(x => x.Version).IsConcurrencyToken();

            // Performance indexes
            b.HasIndex(x => x.ProjectNumber).IsUnique();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.SectionId);
            b.HasIndex(x => x.HangarId);
            b.HasIndex(x => x.ShopId);
            b.HasIndex(x => x.ProjectManagerUserId);
            b.HasIndex(x => x.ScheduledStartDate);
            b.HasIndex(x => x.ScheduledEndDate);
            b.HasIndex(x => x.IsDelayed);
            b.HasIndex(x => new { x.SectionId, x.Status });
            b.HasIndex(x => new { x.Status, x.ScheduledEndDate });

            b.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Hangar).WithMany().HasForeignKey(x => x.HangarId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Shop).WithMany().HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.ProjectManagerUser).WithMany().HasForeignKey(x => x.ProjectManagerUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // DailyProgressLog configuration
        modelBuilder.Entity<DailyProgressLog>(b =>
        {
            b.ToTable("daily_progress_logs");
            b.HasKey(x => x.Id);
            b.Property(x => x.WorkPerformed).HasMaxLength(2000).IsRequired();
            b.Property(x => x.IssuesEncountered).HasMaxLength(1000);
            b.Property(x => x.NextDayPlan).HasMaxLength(1000);
            b.Property(x => x.Version).IsConcurrencyToken();

            // Performance indexes
            b.HasIndex(x => x.MaintenanceProjectId);
            b.HasIndex(x => x.LogDate);
            b.HasIndex(x => x.IsSubmitted);
            b.HasIndex(x => x.SubmittedByUserId);
            b.HasIndex(x => new { x.MaintenanceProjectId, x.LogDate });
            b.HasIndex(x => new { x.LogDate, x.IsSubmitted });

            b.HasOne(x => x.Project).WithMany(x => x.ProgressLogs).HasForeignKey(x => x.MaintenanceProjectId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.SubmittedByUser).WithMany().HasForeignKey(x => x.SubmittedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // PartFollowUp configuration
        modelBuilder.Entity<PartFollowUp>(b =>
        {
            b.ToTable("part_follow_ups");
            b.HasKey(x => x.Id);
            b.Property(x => x.PartNumber).HasMaxLength(50).IsRequired();
            b.Property(x => x.PartName).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(1000);
            b.Property(x => x.Status).HasMaxLength(50).IsRequired();
            b.Property(x => x.Supplier).HasMaxLength(200);
            b.Property(x => x.Remarks).HasMaxLength(500);
            b.Property(x => x.Cost).HasPrecision(18, 2);
            b.Property(x => x.Version).IsConcurrencyToken();

            // Performance indexes
            b.HasIndex(x => x.MaintenanceProjectId);
            b.HasIndex(x => x.PartNumber);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.RequiredBy);
            b.HasIndex(x => x.AssignedToUserId);
            b.HasIndex(x => new { x.MaintenanceProjectId, x.Status });
            b.HasIndex(x => new { x.Status, x.RequiredBy });

            b.HasOne(x => x.Project).WithMany(x => x.PartFollowUps).HasForeignKey(x => x.MaintenanceProjectId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.AssignedToUser).WithMany().HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // PostMortemReport configuration
        modelBuilder.Entity<PostMortemReport>(b =>
        {
            b.ToTable("post_mortem_reports");
            b.HasKey(x => x.Id);
            b.Property(x => x.ReportNumber).HasMaxLength(50).IsRequired();
            b.Property(x => x.WorkPackageId).HasMaxLength(50).IsRequired();
            b.Property(x => x.WorkPackageDescription).HasMaxLength(200).IsRequired();
            b.Property(x => x.AircraftRegistration).HasMaxLength(20).IsRequired();
            b.Property(x => x.AircraftType).HasMaxLength(100).IsRequired();
            b.Property(x => x.HangaringStatus).HasMaxLength(50).IsRequired();
            b.Property(x => x.DeHangaringStatus).HasMaxLength(50).IsRequired();
            b.Property(x => x.TatStatus).HasMaxLength(50).IsRequired();
            b.Property(x => x.CheckType).HasConversion<int>().IsRequired();
            b.Property(x => x.IncomingDeviationReason).HasMaxLength(500).IsRequired();
            b.Property(x => x.DeviationReasonDeHangaring).HasMaxLength(500).IsRequired();
            b.Property(x => x.Remarks).HasMaxLength(1000);
            b.Property(x => x.Status).HasConversion<int>().IsRequired();
            b.Property(x => x.RejectionReason).HasMaxLength(500);
            b.Property(x => x.ScheduleTATHours).HasPrecision(18, 2);
            b.Property(x => x.ActualTATHours).HasPrecision(18, 2);
            b.Property(x => x.Version).IsConcurrencyToken();

            // Performance indexes
            b.HasIndex(x => x.ReportNumber).IsUnique();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.SectionId);
            b.HasIndex(x => x.HangarId);
            b.HasIndex(x => x.CreatedByUserId);
            b.HasIndex(x => x.ScheduledIn);
            b.HasIndex(x => new { x.SectionId, x.Status });
            b.HasIndex(x => new { x.Status, x.ScheduledIn });

            b.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Hangar).WithMany().HasForeignKey(x => x.HangarId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.SubmittedByUser).WithMany().HasForeignKey(x => x.SubmittedByUserId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.ReviewedByUser).WithMany().HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.ApprovedByUser).WithMany().HasForeignKey(x => x.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // PostMortemSlaRecord configuration
        modelBuilder.Entity<PostMortemSlaRecord>(b =>
        {
            b.ToTable("post_mortem_sla_records");
            b.HasKey(x => x.Id);
            b.Property(x => x.SlaType).HasMaxLength(100).IsRequired();
            b.Property(x => x.Target).HasMaxLength(200).IsRequired();
            b.Property(x => x.Actual).HasMaxLength(200).IsRequired();
            b.Property(x => x.Status).HasMaxLength(50).IsRequired();
            b.Property(x => x.Remarks).HasMaxLength(500);
            b.Property(x => x.Variance).HasPrecision(18, 2);
            b.Property(x => x.Version).IsConcurrencyToken();

            // Performance indexes
            b.HasIndex(x => x.PostMortemReportId);
            b.HasIndex(x => x.SlaType);
            b.HasIndex(x => x.Status);

            b.HasOne(x => x.Report).WithMany(x => x.SlaRecords).HasForeignKey(x => x.PostMortemReportId).OnDelete(DeleteBehavior.Cascade);
        });

        // PostMortemCrsCompletion configuration
        modelBuilder.Entity<PostMortemCrsCompletion>(b =>
        {
            b.ToTable("post_mortem_crs_completions");
            b.HasKey(x => x.Id);
            b.Property(x => x.CrsNumber).HasMaxLength(50).IsRequired();
            b.Property(x => x.Description).HasMaxLength(500).IsRequired();
            b.Property(x => x.Status).HasMaxLength(50).IsRequired();
            b.Property(x => x.Remarks).HasMaxLength(500);
            b.Property(x => x.Version).IsConcurrencyToken();

            // Performance indexes
            b.HasIndex(x => x.PostMortemReportId);
            b.HasIndex(x => x.CrsNumber);
            b.HasIndex(x => x.Status);

            b.HasOne(x => x.Report).WithMany(x => x.CrsCompletions).HasForeignKey(x => x.PostMortemReportId).OnDelete(DeleteBehavior.Cascade);
        });

        // PostMortemTatRecord configuration
        modelBuilder.Entity<PostMortemTatRecord>(b =>
        {
            b.ToTable("post_mortem_tat_records");
            b.HasKey(x => x.Id);
            b.Property(x => x.TaskDescription).HasMaxLength(500).IsRequired();
            b.Property(x => x.Status).HasMaxLength(50).IsRequired();
            b.Property(x => x.DelayReason).HasMaxLength(500).IsRequired();
            b.Property(x => x.HangaringAC).HasMaxLength(20).IsRequired();
            b.Property(x => x.DehangaringAC).HasMaxLength(20).IsRequired();
            b.Property(x => x.Remarks).HasMaxLength(1000);
            b.Property(x => x.PlannedHours).HasPrecision(18, 2);
            b.Property(x => x.ActualHours).HasPrecision(18, 2);
            b.Property(x => x.Variance).HasPrecision(18, 2);
            b.Property(x => x.Version).IsConcurrencyToken();

            // Performance indexes
            b.HasIndex(x => x.PostMortemReportId);
            b.HasIndex(x => x.Status);

            b.HasOne(x => x.Report).WithMany(x => x.TatRecords).HasForeignKey(x => x.PostMortemReportId).OnDelete(DeleteBehavior.Cascade);
        });

        // PostMortemPlanStability configuration
        modelBuilder.Entity<PostMortemPlanStability>(b =>
        {
            b.ToTable("post_mortem_plan_stability");
            b.HasKey(x => x.Id);
            b.Property(x => x.PlanVersion).HasMaxLength(50).IsRequired();
            b.Property(x => x.ChangeReason).HasMaxLength(500).IsRequired();
            b.Property(x => x.Remarks).HasMaxLength(500);
            b.Property(x => x.Version).IsConcurrencyToken();

            // Performance indexes
            b.HasIndex(x => x.PostMortemReportId);
            b.HasIndex(x => x.EffectiveDate);

            b.HasOne(x => x.Report).WithMany(x => x.PlanStabilityRecords).HasForeignKey(x => x.PostMortemReportId).OnDelete(DeleteBehavior.Cascade);
        });

        // PostMortemCarryOverTask configuration
        modelBuilder.Entity<PostMortemCarryOverTask>(b =>
        {
            b.ToTable("post_mortem_carry_over_tasks");
            b.HasKey(x => x.Id);
            b.Property(x => x.Description).HasMaxLength(500).IsRequired();
            b.Property(x => x.AircraftRegistration).HasMaxLength(20).IsRequired();
            b.Property(x => x.AircraftType).HasMaxLength(100).IsRequired();
            b.Property(x => x.TaskType).HasConversion<int>().IsRequired();
            b.Property(x => x.DeferralReason).HasMaxLength(200).IsRequired();
            b.Property(x => x.Status).HasConversion<int>().IsRequired();
            b.Property(x => x.Remarks).HasMaxLength(500);
            b.Property(x => x.Version).IsConcurrencyToken();

            // Performance indexes
            b.HasIndex(x => x.PostMortemReportId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.TaskType);
            b.HasIndex(x => x.AssignedToUserId);
            b.HasIndex(x => x.TargetDate);
            b.HasIndex(x => new { x.PostMortemReportId, x.Status });

            b.HasOne(x => x.Report).WithMany(x => x.CarryOverTasks).HasForeignKey(x => x.PostMortemReportId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.AssignedToUser).WithMany().HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // Daily Status Report configuration
        modelBuilder.Entity<DomainDailyStatusReport>(b =>
        {
            b.ToTable("daily_status_reports");
            b.HasKey(x => x.Id);
            b.Property(x => x.ReportNumber).HasMaxLength(50).IsRequired();
            b.Property(x => x.AircraftRegistration).HasMaxLength(20).IsRequired();
            b.Property(x => x.AircraftType).HasMaxLength(100).IsRequired();
            b.Property(x => x.Fleet).HasMaxLength(100);
            b.Property(x => x.MaintenanceVisit).HasMaxLength(100);
            b.Property(x => x.CheckType).HasMaxLength(100);
            b.Property(x => x.Status).HasConversion<int>().IsRequired();
            b.Property(x => x.RejectionReason).HasMaxLength(500);
            b.Property(x => x.Version).IsConcurrencyToken();

            // Performance indexes
            b.HasIndex(x => x.ReportNumber).IsUnique();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.SectionId);
            b.HasIndex(x => x.HangarId);
            b.HasIndex(x => x.CreatedByUserId);
            b.HasIndex(x => x.ReportDate);
            b.HasIndex(x => x.AircraftRegistration);
            b.HasIndex(x => new { x.SectionId, x.Status });
            b.HasIndex(x => new { x.ReportDate, x.SectionId });

            b.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Hangar).WithMany().HasForeignKey(x => x.HangarId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.SubmittedByUser).WithMany().HasForeignKey(x => x.SubmittedByUserId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.ReviewedByUser).WithMany().HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.ApprovedByUser).WithMany().HasForeignKey(x => x.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // Task Status configuration
        modelBuilder.Entity<DomainTaskStatus>(b =>
        {
            b.ToTable("task_statuses");
            b.HasKey(x => x.Id);
            b.Property(x => x.TaskName).HasMaxLength(500).IsRequired();
            b.Property(x => x.TaskId).HasMaxLength(100).IsRequired();
            b.Property(x => x.TaskType).HasMaxLength(100).IsRequired();
            b.Property(x => x.Phase).HasConversion<int>().IsRequired();
            b.Property(x => x.Status).HasConversion<int>().IsRequired();
            b.Property(x => x.Version).IsConcurrencyToken();

            // Performance indexes
            b.HasIndex(x => x.DailyStatusReportId);
            b.HasIndex(x => x.TaskId);
            b.HasIndex(x => x.Phase);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => new { x.DailyStatusReportId, x.Phase });
            b.HasIndex(x => new { x.DailyStatusReportId, x.Status });

            b.HasOne(x => x.Report).WithMany(x => x.TaskStatuses).HasForeignKey(x => x.DailyStatusReportId).OnDelete(DeleteBehavior.Cascade);
        });

        // Part Issue configuration
        modelBuilder.Entity<PartIssue>(b =>
        {
            b.ToTable("part_issues");
            b.HasKey(x => x.Id);
            b.Property(x => x.IssueType).HasConversion<int>().IsRequired();
            b.Property(x => x.Task).HasMaxLength(500);
            b.Property(x => x.PartNumber).HasMaxLength(100).IsRequired();
            b.Property(x => x.Description).HasMaxLength(500).IsRequired();
            b.Property(x => x.RID).HasMaxLength(50);
            b.Property(x => x.PONumber).HasMaxLength(50);
            b.Property(x => x.ResponsibleBuyer).HasMaxLength(180);
            b.Property(x => x.Vendor).HasMaxLength(200);
            b.Property(x => x.DonorAircraft).HasMaxLength(20);
            b.Property(x => x.RecipientAircraft).HasMaxLength(20);
            b.Property(x => x.Status).HasMaxLength(50);
            b.Property(x => x.Remark).HasMaxLength(500);
            b.Property(x => x.EDD).HasMaxLength(50);
            b.Property(x => x.Resolution).HasMaxLength(500);
            b.Property(x => x.ClosedBy).HasMaxLength(180);
            b.Property(x => x.Version).IsConcurrencyToken();

            // Performance indexes
            b.HasIndex(x => x.DailyStatusReportId);
            b.HasIndex(x => x.IssueType);
            b.HasIndex(x => x.PartNumber);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => new { x.DailyStatusReportId, x.IssueType });

            b.HasOne(x => x.Report).WithMany(x => x.PartIssues).HasForeignKey(x => x.DailyStatusReportId).OnDelete(DeleteBehavior.Cascade);
        });

        // Major Finding configuration
        modelBuilder.Entity<MajorFinding>(b =>
        {
            b.ToTable("major_findings");
            b.HasKey(x => x.Id);
            b.Property(x => x.FindingNumber).HasMaxLength(50).IsRequired();
            b.Property(x => x.ATAChapter).HasMaxLength(20);
            b.Property(x => x.Description).HasMaxLength(1000).IsRequired();
            b.Property(x => x.Severity).HasConversion<int>().IsRequired();
            b.Property(x => x.Status).HasConversion<int>().IsRequired();
            b.Property(x => x.Owner).HasMaxLength(180);
            b.Property(x => x.Remarks).HasMaxLength(500);
            b.Property(x => x.Version).IsConcurrencyToken();

            // Performance indexes
            b.HasIndex(x => x.DailyStatusReportId);
            b.HasIndex(x => x.FindingNumber);
            b.HasIndex(x => x.ATAChapter);
            b.HasIndex(x => x.Severity);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => new { x.DailyStatusReportId, x.Severity });
            b.HasIndex(x => new { x.DailyStatusReportId, x.Status });

            b.HasOne(x => x.Report).WithMany(x => x.MajorFindings).HasForeignKey(x => x.DailyStatusReportId).OnDelete(DeleteBehavior.Cascade);
        });

        // Import History configuration
        modelBuilder.Entity<ImportHistory>(b =>
        {
            b.ToTable("import_histories");
            b.HasKey(x => x.Id);
            b.Property(x => x.ImportType).HasConversion<int>().IsRequired();
            b.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            b.Property(x => x.ImportStatus).HasConversion<int>().IsRequired();
            b.Property(x => x.ErrorMessage).HasMaxLength(1000);
            b.Property(x => x.ColumnMapping).HasMaxLength(2000);
            b.Property(x => x.Version).IsConcurrencyToken();

            // Performance indexes
            b.HasIndex(x => x.DailyStatusReportId);
            b.HasIndex(x => x.ImportType);
            b.HasIndex(x => x.UploadedByUserId);
            b.HasIndex(x => x.UploadDate);
            b.HasIndex(x => x.ImportStatus);
            b.HasIndex(x => new { x.DailyStatusReportId, x.ImportType });

            b.HasOne(x => x.Report).WithMany(x => x.ImportHistories).HasForeignKey(x => x.DailyStatusReportId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.UploadedByUser).WithMany().HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
