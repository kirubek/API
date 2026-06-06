using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseOps.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHandoverEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "handovers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ShiftType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DutyTeamLeaderName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    SectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HangarId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OutgoingTeamLeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncomingTeamLeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_handovers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_handovers_hangars_HangarId",
                        column: x => x.HangarId,
                        principalTable: "hangars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_handovers_sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_handovers_shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_handovers_users_IncomingTeamLeaderId",
                        column: x => x.IncomingTeamLeaderId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_handovers_users_OutgoingTeamLeaderId",
                        column: x => x.OutgoingTeamLeaderId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "handover_defects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandoverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AircraftRegistration = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DefectDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NonRoutineCardNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DefectLoginTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ItemStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_handover_defects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_handover_defects_handovers_HandoverId",
                        column: x => x.HandoverId,
                        principalTable: "handovers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "handover_issues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandoverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssueType = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_handover_issues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_handover_issues_handovers_HandoverId",
                        column: x => x.HandoverId,
                        principalTable: "handovers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "handover_manning_statuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandoverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalScheduledManpower = table.Column<int>(type: "int", nullable: false),
                    SickLeave = table.Column<int>(type: "int", nullable: false),
                    Absent = table.Column<int>(type: "int", nullable: false),
                    Vacation = table.Column<int>(type: "int", nullable: false),
                    Training = table.Column<int>(type: "int", nullable: false),
                    BorrowedManpower = table.Column<int>(type: "int", nullable: false),
                    TotalAvailableManpower = table.Column<int>(type: "int", nullable: false),
                    TotalLostManpower = table.Column<int>(type: "int", nullable: false),
                    AvailabilityPercentage = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_handover_manning_statuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_handover_manning_statuses_handovers_HandoverId",
                        column: x => x.HandoverId,
                        principalTable: "handovers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "handover_signatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandoverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignatureRole = table.Column<int>(type: "int", nullable: false),
                    SignatureData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SignatureName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    SignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_handover_signatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_handover_signatures_handovers_HandoverId",
                        column: x => x.HandoverId,
                        principalTable: "handovers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_handover_signatures_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "handover_tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandoverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskType = table.Column<int>(type: "int", nullable: false),
                    AircraftRegistration = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TaskCardCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_handover_tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_handover_tasks_handovers_HandoverId",
                        column: x => x.HandoverId,
                        principalTable: "handovers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_handover_tasks_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "handover_work_statuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandoverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MfgPartNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    WorkCarriedOut = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WorkToBeDone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OutstandingIssue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_handover_work_statuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_handover_work_statuses_handovers_HandoverId",
                        column: x => x.HandoverId,
                        principalTable: "handovers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_handover_defects_AircraftRegistration",
                table: "handover_defects",
                column: "AircraftRegistration");

            migrationBuilder.CreateIndex(
                name: "IX_handover_defects_HandoverId",
                table: "handover_defects",
                column: "HandoverId");

            migrationBuilder.CreateIndex(
                name: "IX_handover_defects_ItemStatus",
                table: "handover_defects",
                column: "ItemStatus");

            migrationBuilder.CreateIndex(
                name: "IX_handover_issues_HandoverId",
                table: "handover_issues",
                column: "HandoverId");

            migrationBuilder.CreateIndex(
                name: "IX_handover_issues_IssueType",
                table: "handover_issues",
                column: "IssueType");

            migrationBuilder.CreateIndex(
                name: "IX_handover_manning_statuses_HandoverId",
                table: "handover_manning_statuses",
                column: "HandoverId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_handover_signatures_HandoverId",
                table: "handover_signatures",
                column: "HandoverId");

            migrationBuilder.CreateIndex(
                name: "IX_handover_signatures_HandoverId_SignatureRole",
                table: "handover_signatures",
                columns: new[] { "HandoverId", "SignatureRole" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_handover_signatures_UserId",
                table: "handover_signatures",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_handover_tasks_CreatedByUserId",
                table: "handover_tasks",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_handover_tasks_HandoverId",
                table: "handover_tasks",
                column: "HandoverId");

            migrationBuilder.CreateIndex(
                name: "IX_handover_tasks_TaskType",
                table: "handover_tasks",
                column: "TaskType");

            migrationBuilder.CreateIndex(
                name: "IX_handover_work_statuses_HandoverId",
                table: "handover_work_statuses",
                column: "HandoverId");

            migrationBuilder.CreateIndex(
                name: "IX_handovers_AcceptedAt",
                table: "handovers",
                column: "AcceptedAt");

            migrationBuilder.CreateIndex(
                name: "IX_handovers_Date",
                table: "handovers",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_handovers_Date_HangarId",
                table: "handovers",
                columns: new[] { "Date", "HangarId" });

            migrationBuilder.CreateIndex(
                name: "IX_handovers_Date_SectionId",
                table: "handovers",
                columns: new[] { "Date", "SectionId" });

            migrationBuilder.CreateIndex(
                name: "IX_handovers_Date_ShopId",
                table: "handovers",
                columns: new[] { "Date", "ShopId" });

            migrationBuilder.CreateIndex(
                name: "IX_handovers_HangarId",
                table: "handovers",
                column: "HangarId");

            migrationBuilder.CreateIndex(
                name: "IX_handovers_IncomingTeamLeaderId",
                table: "handovers",
                column: "IncomingTeamLeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_handovers_OutgoingTeamLeaderId",
                table: "handovers",
                column: "OutgoingTeamLeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_handovers_SectionId",
                table: "handovers",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_handovers_ShopId",
                table: "handovers",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_handovers_Status",
                table: "handovers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_handovers_Status_SubmittedAt",
                table: "handovers",
                columns: new[] { "Status", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_handovers_SubmittedAt",
                table: "handovers",
                column: "SubmittedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "handover_defects");

            migrationBuilder.DropTable(
                name: "handover_issues");

            migrationBuilder.DropTable(
                name: "handover_manning_statuses");

            migrationBuilder.DropTable(
                name: "handover_signatures");

            migrationBuilder.DropTable(
                name: "handover_tasks");

            migrationBuilder.DropTable(
                name: "handover_work_statuses");

            migrationBuilder.DropTable(
                name: "handovers");
        }
    }
}
