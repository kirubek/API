using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseOps.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnualLeaveEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "annual_leave_plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    SectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HangarId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TeamLeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FinalizedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FinalizedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TotalEmployees = table.Column<int>(type: "int", nullable: false),
                    TotalOnLeave = table.Column<int>(type: "int", nullable: false),
                    TotalAvailable = table.Column<int>(type: "int", nullable: false),
                    GenerationNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_annual_leave_plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_annual_leave_plans_hangars_HangarId",
                        column: x => x.HangarId,
                        principalTable: "hangars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_annual_leave_plans_sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_annual_leave_plans_shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_annual_leave_plans_users_FinalizedByUserId",
                        column: x => x.FinalizedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_annual_leave_plans_users_TeamLeaderId",
                        column: x => x.TeamLeaderId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "annual_leave_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleAtSubmission = table.Column<int>(type: "int", nullable: false),
                    SectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HangarId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubmittedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaveType = table.Column<int>(type: "int", nullable: true),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_annual_leave_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_annual_leave_requests_hangars_HangarId",
                        column: x => x.HangarId,
                        principalTable: "hangars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_annual_leave_requests_sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_annual_leave_requests_shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_annual_leave_requests_users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_annual_leave_requests_users_SubmittedToUserId",
                        column: x => x.SubmittedToUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_annual_leave_requests_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "leave_balances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    TotalEntitled = table.Column<int>(type: "int", nullable: false),
                    Taken = table.Column<int>(type: "int", nullable: false),
                    Pending = table.Column<int>(type: "int", nullable: false),
                    CarryOverFromPrevious = table.Column<int>(type: "int", nullable: false),
                    CarryOverToNext = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_balances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_leave_balances_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "manpower_constraints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HangarId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Year = table.Column<int>(type: "int", nullable: false),
                    MaxLeavePercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MinCoveragePercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MaxLeaveCount = table.Column<int>(type: "int", nullable: true),
                    MinCoverageCount = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manpower_constraints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_manpower_constraints_hangars_HangarId",
                        column: x => x.HangarId,
                        principalTable: "hangars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_manpower_constraints_sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_manpower_constraints_shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "annual_leave_plan_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnnualLeavePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnnualLeaveRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedStartDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApprovedEndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SourceChoice = table.Column<int>(type: "int", nullable: false),
                    PriorityScore = table.Column<int>(type: "int", nullable: false),
                    IsManuallyAdjusted = table.Column<bool>(type: "bit", nullable: false),
                    ManuallyAdjustedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ManuallyAdjustedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AdjustmentReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SplitIndex = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_annual_leave_plan_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_annual_leave_plan_entries_annual_leave_plans_AnnualLeavePlanId",
                        column: x => x.AnnualLeavePlanId,
                        principalTable: "annual_leave_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_annual_leave_plan_entries_annual_leave_requests_AnnualLeaveRequestId",
                        column: x => x.AnnualLeaveRequestId,
                        principalTable: "annual_leave_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_annual_leave_plan_entries_users_ManuallyAdjustedByUserId",
                        column: x => x.ManuallyAdjustedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_annual_leave_plan_entries_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "leave_choices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnnualLeaveRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChoiceNumber = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Days = table.Column<int>(type: "int", nullable: false),
                    SplitIndex = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_choices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_leave_choices_annual_leave_requests_AnnualLeaveRequestId",
                        column: x => x.AnnualLeaveRequestId,
                        principalTable: "annual_leave_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "manpower_constraint_periods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManpowerConstraintId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    MaxLeavePercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    MinCoveragePercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    MaxLeaveCount = table.Column<int>(type: "int", nullable: true),
                    MinCoverageCount = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manpower_constraint_periods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_manpower_constraint_periods_manpower_constraints_ManpowerConstraintId",
                        column: x => x.ManpowerConstraintId,
                        principalTable: "manpower_constraints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_plan_entries_AnnualLeavePlanId",
                table: "annual_leave_plan_entries",
                column: "AnnualLeavePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_plan_entries_AnnualLeavePlanId_UserId",
                table: "annual_leave_plan_entries",
                columns: new[] { "AnnualLeavePlanId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_plan_entries_AnnualLeaveRequestId",
                table: "annual_leave_plan_entries",
                column: "AnnualLeaveRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_plan_entries_IsManuallyAdjusted",
                table: "annual_leave_plan_entries",
                column: "IsManuallyAdjusted");

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_plan_entries_ManuallyAdjustedByUserId",
                table: "annual_leave_plan_entries",
                column: "ManuallyAdjustedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_plan_entries_PriorityScore",
                table: "annual_leave_plan_entries",
                column: "PriorityScore");

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_plan_entries_UserId",
                table: "annual_leave_plan_entries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_plans_FinalizedByUserId",
                table: "annual_leave_plans",
                column: "FinalizedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_plans_HangarId",
                table: "annual_leave_plans",
                column: "HangarId");

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_plans_HangarId_Year_Level",
                table: "annual_leave_plans",
                columns: new[] { "HangarId", "Year", "Level" },
                unique: true,
                filter: "[HangarId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_plans_Level",
                table: "annual_leave_plans",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_plans_SectionId",
                table: "annual_leave_plans",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_plans_SectionId_Year_Level",
                table: "annual_leave_plans",
                columns: new[] { "SectionId", "Year", "Level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_plans_ShopId",
                table: "annual_leave_plans",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_plans_Status",
                table: "annual_leave_plans",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_plans_TeamLeaderId",
                table: "annual_leave_plans",
                column: "TeamLeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_plans_Year",
                table: "annual_leave_plans",
                column: "Year");

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_requests_HangarId",
                table: "annual_leave_requests",
                column: "HangarId");

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_requests_HangarId_Year",
                table: "annual_leave_requests",
                columns: new[] { "HangarId", "Year" });

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_requests_ReviewedByUserId",
                table: "annual_leave_requests",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_requests_SectionId",
                table: "annual_leave_requests",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_requests_SectionId_Year",
                table: "annual_leave_requests",
                columns: new[] { "SectionId", "Year" });

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_requests_ShopId",
                table: "annual_leave_requests",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_requests_Status",
                table: "annual_leave_requests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_requests_SubmittedToUserId",
                table: "annual_leave_requests",
                column: "SubmittedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_requests_UserId",
                table: "annual_leave_requests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_requests_UserId_Year",
                table: "annual_leave_requests",
                columns: new[] { "UserId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_annual_leave_requests_Year",
                table: "annual_leave_requests",
                column: "Year");

            migrationBuilder.CreateIndex(
                name: "IX_leave_balances_UserId",
                table: "leave_balances",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_leave_balances_UserId_Year",
                table: "leave_balances",
                columns: new[] { "UserId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_leave_balances_Year",
                table: "leave_balances",
                column: "Year");

            migrationBuilder.CreateIndex(
                name: "IX_leave_choices_AnnualLeaveRequestId",
                table: "leave_choices",
                column: "AnnualLeaveRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_leave_choices_AnnualLeaveRequestId_ChoiceNumber",
                table: "leave_choices",
                columns: new[] { "AnnualLeaveRequestId", "ChoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_leave_choices_ChoiceNumber",
                table: "leave_choices",
                column: "ChoiceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_manpower_constraint_periods_EndDate",
                table: "manpower_constraint_periods",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_manpower_constraint_periods_ManpowerConstraintId",
                table: "manpower_constraint_periods",
                column: "ManpowerConstraintId");

            migrationBuilder.CreateIndex(
                name: "IX_manpower_constraint_periods_StartDate",
                table: "manpower_constraint_periods",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_manpower_constraints_HangarId",
                table: "manpower_constraints",
                column: "HangarId");

            migrationBuilder.CreateIndex(
                name: "IX_manpower_constraints_HangarId_Year",
                table: "manpower_constraints",
                columns: new[] { "HangarId", "Year" },
                unique: true,
                filter: "[HangarId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_manpower_constraints_IsActive",
                table: "manpower_constraints",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_manpower_constraints_SectionId",
                table: "manpower_constraints",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_manpower_constraints_SectionId_Year",
                table: "manpower_constraints",
                columns: new[] { "SectionId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_manpower_constraints_ShopId",
                table: "manpower_constraints",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_manpower_constraints_Year",
                table: "manpower_constraints",
                column: "Year");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "annual_leave_plan_entries");

            migrationBuilder.DropTable(
                name: "leave_balances");

            migrationBuilder.DropTable(
                name: "leave_choices");

            migrationBuilder.DropTable(
                name: "manpower_constraint_periods");

            migrationBuilder.DropTable(
                name: "annual_leave_plans");

            migrationBuilder.DropTable(
                name: "annual_leave_requests");

            migrationBuilder.DropTable(
                name: "manpower_constraints");
        }
    }
}
