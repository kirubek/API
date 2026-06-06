using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseOps.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAceAttachmentEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ace_attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    GeneratedFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UploadedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ace_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ace_attachments_operational_records_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "operational_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ace_attachments_users_UploadedBy",
                        column: x => x.UploadedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "daily_status_reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AircraftRegistration = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AircraftType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Fleet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MaintenanceVisit = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CheckType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReportDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_daily_status_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_daily_status_reports_hangars_HangarId",
                        column: x => x.HangarId,
                        principalTable: "hangars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_daily_status_reports_sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_daily_status_reports_users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_daily_status_reports_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_daily_status_reports_users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_daily_status_reports_users_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "import_histories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DailyStatusReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportType = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordCount = table.Column<int>(type: "int", nullable: false),
                    ImportStatus = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ColumnMapping = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_histories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_import_histories_daily_status_reports_DailyStatusReportId",
                        column: x => x.DailyStatusReportId,
                        principalTable: "daily_status_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_import_histories_users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "major_findings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DailyStatusReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FindingNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ATAChapter = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RaisedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Owner = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    TargetClosureDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosureDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_major_findings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_major_findings_daily_status_reports_DailyStatusReportId",
                        column: x => x.DailyStatusReportId,
                        principalTable: "daily_status_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "part_issues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DailyStatusReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssueType = table.Column<int>(type: "int", nullable: false),
                    ItemNumber = table.Column<int>(type: "int", nullable: false),
                    Task = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PartNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    DateRequested = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateReceived = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateRobbed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PONumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ResponsibleBuyer = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    Vendor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DonorAircraft = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RecipientAircraft = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EDD = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Resolution = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ClosedBy = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_part_issues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_part_issues_daily_status_reports_DailyStatusReportId",
                        column: x => x.DailyStatusReportId,
                        principalTable: "daily_status_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_statuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DailyStatusReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TaskId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TaskType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phase = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SerialNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_statuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_task_statuses_daily_status_reports_DailyStatusReportId",
                        column: x => x.DailyStatusReportId,
                        principalTable: "daily_status_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ace_attachments_ActivityId",
                table: "ace_attachments",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ace_attachments_UploadedBy",
                table: "ace_attachments",
                column: "UploadedBy");

            migrationBuilder.CreateIndex(
                name: "IX_daily_status_reports_AircraftRegistration",
                table: "daily_status_reports",
                column: "AircraftRegistration");

            migrationBuilder.CreateIndex(
                name: "IX_daily_status_reports_ApprovedByUserId",
                table: "daily_status_reports",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_status_reports_CreatedByUserId",
                table: "daily_status_reports",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_status_reports_HangarId",
                table: "daily_status_reports",
                column: "HangarId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_status_reports_ReportDate",
                table: "daily_status_reports",
                column: "ReportDate");

            migrationBuilder.CreateIndex(
                name: "IX_daily_status_reports_ReportDate_SectionId",
                table: "daily_status_reports",
                columns: new[] { "ReportDate", "SectionId" });

            migrationBuilder.CreateIndex(
                name: "IX_daily_status_reports_ReportNumber",
                table: "daily_status_reports",
                column: "ReportNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_daily_status_reports_ReviewedByUserId",
                table: "daily_status_reports",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_status_reports_SectionId",
                table: "daily_status_reports",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_status_reports_SectionId_Status",
                table: "daily_status_reports",
                columns: new[] { "SectionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_daily_status_reports_Status",
                table: "daily_status_reports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_daily_status_reports_SubmittedByUserId",
                table: "daily_status_reports",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_import_histories_DailyStatusReportId",
                table: "import_histories",
                column: "DailyStatusReportId");

            migrationBuilder.CreateIndex(
                name: "IX_import_histories_DailyStatusReportId_ImportType",
                table: "import_histories",
                columns: new[] { "DailyStatusReportId", "ImportType" });

            migrationBuilder.CreateIndex(
                name: "IX_import_histories_ImportStatus",
                table: "import_histories",
                column: "ImportStatus");

            migrationBuilder.CreateIndex(
                name: "IX_import_histories_ImportType",
                table: "import_histories",
                column: "ImportType");

            migrationBuilder.CreateIndex(
                name: "IX_import_histories_UploadDate",
                table: "import_histories",
                column: "UploadDate");

            migrationBuilder.CreateIndex(
                name: "IX_import_histories_UploadedByUserId",
                table: "import_histories",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_major_findings_ATAChapter",
                table: "major_findings",
                column: "ATAChapter");

            migrationBuilder.CreateIndex(
                name: "IX_major_findings_DailyStatusReportId",
                table: "major_findings",
                column: "DailyStatusReportId");

            migrationBuilder.CreateIndex(
                name: "IX_major_findings_DailyStatusReportId_Severity",
                table: "major_findings",
                columns: new[] { "DailyStatusReportId", "Severity" });

            migrationBuilder.CreateIndex(
                name: "IX_major_findings_DailyStatusReportId_Status",
                table: "major_findings",
                columns: new[] { "DailyStatusReportId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_major_findings_FindingNumber",
                table: "major_findings",
                column: "FindingNumber");

            migrationBuilder.CreateIndex(
                name: "IX_major_findings_Severity",
                table: "major_findings",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_major_findings_Status",
                table: "major_findings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_part_issues_DailyStatusReportId",
                table: "part_issues",
                column: "DailyStatusReportId");

            migrationBuilder.CreateIndex(
                name: "IX_part_issues_DailyStatusReportId_IssueType",
                table: "part_issues",
                columns: new[] { "DailyStatusReportId", "IssueType" });

            migrationBuilder.CreateIndex(
                name: "IX_part_issues_IssueType",
                table: "part_issues",
                column: "IssueType");

            migrationBuilder.CreateIndex(
                name: "IX_part_issues_PartNumber",
                table: "part_issues",
                column: "PartNumber");

            migrationBuilder.CreateIndex(
                name: "IX_part_issues_Status",
                table: "part_issues",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_task_statuses_DailyStatusReportId",
                table: "task_statuses",
                column: "DailyStatusReportId");

            migrationBuilder.CreateIndex(
                name: "IX_task_statuses_DailyStatusReportId_Phase",
                table: "task_statuses",
                columns: new[] { "DailyStatusReportId", "Phase" });

            migrationBuilder.CreateIndex(
                name: "IX_task_statuses_DailyStatusReportId_Status",
                table: "task_statuses",
                columns: new[] { "DailyStatusReportId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_task_statuses_Phase",
                table: "task_statuses",
                column: "Phase");

            migrationBuilder.CreateIndex(
                name: "IX_task_statuses_Status",
                table: "task_statuses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_task_statuses_TaskId",
                table: "task_statuses",
                column: "TaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ace_attachments");

            migrationBuilder.DropTable(
                name: "import_histories");

            migrationBuilder.DropTable(
                name: "major_findings");

            migrationBuilder.DropTable(
                name: "part_issues");

            migrationBuilder.DropTable(
                name: "task_statuses");

            migrationBuilder.DropTable(
                name: "daily_status_reports");
        }
    }
}
