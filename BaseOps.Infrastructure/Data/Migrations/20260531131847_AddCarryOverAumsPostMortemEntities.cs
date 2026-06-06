using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseOps.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCarryOverAumsPostMortemEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "carry_over_reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AircraftRegistration = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AircraftType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProjectType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HangarId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CarryOverTasks = table.Column<int>(type: "int", nullable: false),
                    CarryOverPercentage = table.Column<int>(type: "int", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SubmittedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewComments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FinalizedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FinalizedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carry_over_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_carry_over_reports_hangars_HangarId",
                        column: x => x.HangarId,
                        principalTable: "hangars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_carry_over_reports_sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_carry_over_reports_users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_carry_over_reports_users_FinalizedByUserId",
                        column: x => x.FinalizedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_carry_over_reports_users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_carry_over_reports_users_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "maintenance_projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AircraftRegistration = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AircraftType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FleetType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ScheduledStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScheduledEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletionPercentage = table.Column<int>(type: "int", nullable: false),
                    SectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HangarId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProjectManagerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsDelayed = table.Column<bool>(type: "bit", nullable: false),
                    DelayReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_maintenance_projects_hangars_HangarId",
                        column: x => x.HangarId,
                        principalTable: "hangars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_maintenance_projects_sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_maintenance_projects_shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_maintenance_projects_users_ProjectManagerUserId",
                        column: x => x.ProjectManagerUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "post_mortem_reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WorkPackageId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WorkPackageDescription = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AircraftRegistration = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AircraftType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HangaringStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeHangaringStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TatStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CheckType = table.Column<int>(type: "int", nullable: false),
                    ScheduledIn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualIn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduledOut = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualOut = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IncomingDeviationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DeviationReasonDeHangaring = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ScheduleTATHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActualTATHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HangarId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubmittedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_mortem_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_post_mortem_reports_hangars_HangarId",
                        column: x => x.HangarId,
                        principalTable: "hangars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_post_mortem_reports_sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_post_mortem_reports_users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_post_mortem_reports_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_post_mortem_reports_users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_post_mortem_reports_users_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "carry_over_reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CarryOverReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Approved = table.Column<bool>(type: "bit", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carry_over_reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_carry_over_reviews_carry_over_reports_CarryOverReportId",
                        column: x => x.CarryOverReportId,
                        principalTable: "carry_over_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_carry_over_reviews_users_ReviewerUserId",
                        column: x => x.ReviewerUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "carry_over_tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CarryOverReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TaskType = table.Column<int>(type: "int", nullable: false),
                    DeferralReason = table.Column<int>(type: "int", nullable: false),
                    DeferralDetails = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeferredTaskOrigin = table.Column<int>(type: "int", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstimatedHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActualHours = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TaskCardNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PartRequestId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carry_over_tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_carry_over_tasks_carry_over_reports_CarryOverReportId",
                        column: x => x.CarryOverReportId,
                        principalTable: "carry_over_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_carry_over_tasks_users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "daily_progress_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LogDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WorkPerformed = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    PlannedHours = table.Column<int>(type: "int", nullable: false),
                    ActualHours = table.Column<int>(type: "int", nullable: false),
                    ManpowerCount = table.Column<int>(type: "int", nullable: false),
                    IssuesEncountered = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NextDayPlan = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsSubmitted = table.Column<bool>(type: "bit", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_progress_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_daily_progress_logs_maintenance_projects_MaintenanceProjectId",
                        column: x => x.MaintenanceProjectId,
                        principalTable: "maintenance_projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_daily_progress_logs_users_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "part_follow_ups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PartName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequiredBy = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrderedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Supplier = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_part_follow_ups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_part_follow_ups_maintenance_projects_MaintenanceProjectId",
                        column: x => x.MaintenanceProjectId,
                        principalTable: "maintenance_projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_part_follow_ups_users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "post_mortem_carry_over_tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostMortemReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AircraftRegistration = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AircraftType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TaskType = table.Column<int>(type: "int", nullable: false),
                    DeferralReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TargetDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_mortem_carry_over_tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_post_mortem_carry_over_tasks_post_mortem_reports_PostMortemReportId",
                        column: x => x.PostMortemReportId,
                        principalTable: "post_mortem_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_post_mortem_carry_over_tasks_users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "post_mortem_crs_completions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostMortemReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CrsNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_mortem_crs_completions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_post_mortem_crs_completions_post_mortem_reports_PostMortemReportId",
                        column: x => x.PostMortemReportId,
                        principalTable: "post_mortem_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "post_mortem_plan_stability",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostMortemReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangeCount = table.Column<int>(type: "int", nullable: false),
                    ChangeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_mortem_plan_stability", x => x.Id);
                    table.ForeignKey(
                        name: "FK_post_mortem_plan_stability_post_mortem_reports_PostMortemReportId",
                        column: x => x.PostMortemReportId,
                        principalTable: "post_mortem_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "post_mortem_sla_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostMortemReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SlaType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Target = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Actual = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Variance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_mortem_sla_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_post_mortem_sla_records_post_mortem_reports_PostMortemReportId",
                        column: x => x.PostMortemReportId,
                        principalTable: "post_mortem_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "post_mortem_tat_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostMortemReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PlannedHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActualHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Variance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DelayReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HangaringNumber = table.Column<int>(type: "int", nullable: false),
                    HangaringAC = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DehangaringNumber = table.Column<int>(type: "int", nullable: false),
                    DehangaringAC = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_mortem_tat_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_post_mortem_tat_records_post_mortem_reports_PostMortemReportId",
                        column: x => x.PostMortemReportId,
                        principalTable: "post_mortem_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_carry_over_reports_AssignedToUserId",
                table: "carry_over_reports",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_carry_over_reports_DueDate",
                table: "carry_over_reports",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_carry_over_reports_FinalizedByUserId",
                table: "carry_over_reports",
                column: "FinalizedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_carry_over_reports_HangarId",
                table: "carry_over_reports",
                column: "HangarId");

            migrationBuilder.CreateIndex(
                name: "IX_carry_over_reports_ReportNumber",
                table: "carry_over_reports",
                column: "ReportNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_carry_over_reports_ReviewedByUserId",
                table: "carry_over_reports",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_carry_over_reports_SectionId",
                table: "carry_over_reports",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_carry_over_reports_SectionId_Status",
                table: "carry_over_reports",
                columns: new[] { "SectionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_carry_over_reports_Status",
                table: "carry_over_reports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_carry_over_reports_Status_DueDate",
                table: "carry_over_reports",
                columns: new[] { "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_carry_over_reports_SubmittedByUserId",
                table: "carry_over_reports",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_carry_over_reviews_CarryOverReportId",
                table: "carry_over_reviews",
                column: "CarryOverReportId");

            migrationBuilder.CreateIndex(
                name: "IX_carry_over_reviews_CarryOverReportId_ReviewerUserId",
                table: "carry_over_reviews",
                columns: new[] { "CarryOverReportId", "ReviewerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_carry_over_reviews_ReviewedAt",
                table: "carry_over_reviews",
                column: "ReviewedAt");

            migrationBuilder.CreateIndex(
                name: "IX_carry_over_reviews_ReviewerUserId",
                table: "carry_over_reviews",
                column: "ReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_carry_over_tasks_AssignedToUserId",
                table: "carry_over_tasks",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_carry_over_tasks_CarryOverReportId",
                table: "carry_over_tasks",
                column: "CarryOverReportId");

            migrationBuilder.CreateIndex(
                name: "IX_carry_over_tasks_CarryOverReportId_Status",
                table: "carry_over_tasks",
                columns: new[] { "CarryOverReportId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_carry_over_tasks_DueDate",
                table: "carry_over_tasks",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_carry_over_tasks_Status",
                table: "carry_over_tasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_carry_over_tasks_Status_DueDate",
                table: "carry_over_tasks",
                columns: new[] { "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_carry_over_tasks_TaskType",
                table: "carry_over_tasks",
                column: "TaskType");

            migrationBuilder.CreateIndex(
                name: "IX_daily_progress_logs_IsSubmitted",
                table: "daily_progress_logs",
                column: "IsSubmitted");

            migrationBuilder.CreateIndex(
                name: "IX_daily_progress_logs_LogDate",
                table: "daily_progress_logs",
                column: "LogDate");

            migrationBuilder.CreateIndex(
                name: "IX_daily_progress_logs_LogDate_IsSubmitted",
                table: "daily_progress_logs",
                columns: new[] { "LogDate", "IsSubmitted" });

            migrationBuilder.CreateIndex(
                name: "IX_daily_progress_logs_MaintenanceProjectId",
                table: "daily_progress_logs",
                column: "MaintenanceProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_progress_logs_MaintenanceProjectId_LogDate",
                table: "daily_progress_logs",
                columns: new[] { "MaintenanceProjectId", "LogDate" });

            migrationBuilder.CreateIndex(
                name: "IX_daily_progress_logs_SubmittedByUserId",
                table: "daily_progress_logs",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_projects_HangarId",
                table: "maintenance_projects",
                column: "HangarId");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_projects_IsDelayed",
                table: "maintenance_projects",
                column: "IsDelayed");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_projects_ProjectManagerUserId",
                table: "maintenance_projects",
                column: "ProjectManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_projects_ProjectNumber",
                table: "maintenance_projects",
                column: "ProjectNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_projects_ScheduledEndDate",
                table: "maintenance_projects",
                column: "ScheduledEndDate");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_projects_ScheduledStartDate",
                table: "maintenance_projects",
                column: "ScheduledStartDate");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_projects_SectionId",
                table: "maintenance_projects",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_projects_SectionId_Status",
                table: "maintenance_projects",
                columns: new[] { "SectionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_projects_ShopId",
                table: "maintenance_projects",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_projects_Status",
                table: "maintenance_projects",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_projects_Status_ScheduledEndDate",
                table: "maintenance_projects",
                columns: new[] { "Status", "ScheduledEndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_part_follow_ups_AssignedToUserId",
                table: "part_follow_ups",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_part_follow_ups_MaintenanceProjectId",
                table: "part_follow_ups",
                column: "MaintenanceProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_part_follow_ups_MaintenanceProjectId_Status",
                table: "part_follow_ups",
                columns: new[] { "MaintenanceProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_part_follow_ups_PartNumber",
                table: "part_follow_ups",
                column: "PartNumber");

            migrationBuilder.CreateIndex(
                name: "IX_part_follow_ups_RequiredBy",
                table: "part_follow_ups",
                column: "RequiredBy");

            migrationBuilder.CreateIndex(
                name: "IX_part_follow_ups_Status",
                table: "part_follow_ups",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_part_follow_ups_Status_RequiredBy",
                table: "part_follow_ups",
                columns: new[] { "Status", "RequiredBy" });

            migrationBuilder.CreateIndex(
                name: "IX_post_mortem_carry_over_tasks_AssignedToUserId",
                table: "post_mortem_carry_over_tasks",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_post_mortem_carry_over_tasks_PostMortemReportId",
                table: "post_mortem_carry_over_tasks",
                column: "PostMortemReportId");

            migrationBuilder.CreateIndex(
                name: "IX_post_mortem_carry_over_tasks_PostMortemReportId_Status",
                table: "post_mortem_carry_over_tasks",
                columns: new[] { "PostMortemReportId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_post_mortem_carry_over_tasks_Status",
                table: "post_mortem_carry_over_tasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_post_mortem_carry_over_tasks_TargetDate",
                table: "post_mortem_carry_over_tasks",
                column: "TargetDate");

            migrationBuilder.CreateIndex(
                name: "IX_post_mortem_carry_over_tasks_TaskType",
                table: "post_mortem_carry_over_tasks",
                column: "TaskType");

            migrationBuilder.CreateIndex(
                name: "IX_post_mortem_crs_completions_CrsNumber",
                table: "post_mortem_crs_completions",
                column: "CrsNumber");

            migrationBuilder.CreateIndex(
                name: "IX_post_mortem_crs_completions_PostMortemReportId",
                table: "post_mortem_crs_completions",
                column: "PostMortemReportId");

            migrationBuilder.CreateIndex(
                name: "IX_post_mortem_crs_completions_Status",
                table: "post_mortem_crs_completions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_post_mortem_plan_stability_EffectiveDate",
                table: "post_mortem_plan_stability",
                column: "EffectiveDate");

            migrationBuilder.CreateIndex(
                name: "IX_post_mortem_plan_stability_PostMortemReportId",
                table: "post_mortem_plan_stability",
                column: "PostMortemReportId");

            migrationBuilder.CreateIndex(
                name: "IX_post_mortem_reports_ApprovedByUserId",
                table: "post_mortem_reports",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_post_mortem_reports_CreatedByUserId",
                table: "post_mortem_reports",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_post_mortem_reports_HangarId",
                table: "post_mortem_reports",
                column: "HangarId");

            migrationBuilder.CreateIndex(
                name: "IX_post_mortem_reports_ReportNumber",
                table: "post_mortem_reports",
                column: "ReportNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_post_mortem_reports_ReviewedByUserId",
                table: "post_mortem_reports",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_post_mortem_reports_ScheduledIn",
                table: "post_mortem_reports",
                column: "ScheduledIn");

            migrationBuilder.CreateIndex(
                name: "IX_post_mortem_reports_SectionId",
                table: "post_mortem_reports",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_post_mortem_reports_SectionId_Status",
                table: "post_mortem_reports",
                columns: new[] { "SectionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_post_mortem_reports_Status",
                table: "post_mortem_reports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_post_mortem_reports_Status_ScheduledIn",
                table: "post_mortem_reports",
                columns: new[] { "Status", "ScheduledIn" });

            migrationBuilder.CreateIndex(
                name: "IX_post_mortem_reports_SubmittedByUserId",
                table: "post_mortem_reports",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_post_mortem_sla_records_PostMortemReportId",
                table: "post_mortem_sla_records",
                column: "PostMortemReportId");

            migrationBuilder.CreateIndex(
                name: "IX_post_mortem_sla_records_SlaType",
                table: "post_mortem_sla_records",
                column: "SlaType");

            migrationBuilder.CreateIndex(
                name: "IX_post_mortem_sla_records_Status",
                table: "post_mortem_sla_records",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_post_mortem_tat_records_PostMortemReportId",
                table: "post_mortem_tat_records",
                column: "PostMortemReportId");

            migrationBuilder.CreateIndex(
                name: "IX_post_mortem_tat_records_Status",
                table: "post_mortem_tat_records",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "carry_over_reviews");

            migrationBuilder.DropTable(
                name: "carry_over_tasks");

            migrationBuilder.DropTable(
                name: "daily_progress_logs");

            migrationBuilder.DropTable(
                name: "part_follow_ups");

            migrationBuilder.DropTable(
                name: "post_mortem_carry_over_tasks");

            migrationBuilder.DropTable(
                name: "post_mortem_crs_completions");

            migrationBuilder.DropTable(
                name: "post_mortem_plan_stability");

            migrationBuilder.DropTable(
                name: "post_mortem_sla_records");

            migrationBuilder.DropTable(
                name: "post_mortem_tat_records");

            migrationBuilder.DropTable(
                name: "carry_over_reports");

            migrationBuilder.DropTable(
                name: "maintenance_projects");

            migrationBuilder.DropTable(
                name: "post_mortem_reports");
        }
    }
}
