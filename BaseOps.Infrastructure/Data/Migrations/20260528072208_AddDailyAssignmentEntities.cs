using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseOps.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyAssignmentEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AircraftType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    AircraftRegistration = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ExpectedManpower = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HangarId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TeamLeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Shift = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_daily_assignments_hangars_HangarId",
                        column: x => x.HangarId,
                        principalTable: "hangars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_daily_assignments_sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_daily_assignments_shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_daily_assignments_users_TeamLeaderId",
                        column: x => x.TeamLeaderId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "assignment_details",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DailyAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAircraft = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TaskDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assignment_details", x => x.Id);
                    table.ForeignKey(
                        name: "FK_assignment_details_daily_assignments_DailyAssignmentId",
                        column: x => x.DailyAssignmentId,
                        principalTable: "daily_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_assignment_details_users_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_assignment_details_DailyAssignmentId",
                table: "assignment_details",
                column: "DailyAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_assignment_details_DailyAssignmentId_EmployeeId",
                table: "assignment_details",
                columns: new[] { "DailyAssignmentId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_assignment_details_EmployeeId",
                table: "assignment_details",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_assignments_Date",
                table: "daily_assignments",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_daily_assignments_Date_HangarId",
                table: "daily_assignments",
                columns: new[] { "Date", "HangarId" });

            migrationBuilder.CreateIndex(
                name: "IX_daily_assignments_Date_SectionId",
                table: "daily_assignments",
                columns: new[] { "Date", "SectionId" });

            migrationBuilder.CreateIndex(
                name: "IX_daily_assignments_Date_ShopId",
                table: "daily_assignments",
                columns: new[] { "Date", "ShopId" });

            migrationBuilder.CreateIndex(
                name: "IX_daily_assignments_Date_TeamLeaderId",
                table: "daily_assignments",
                columns: new[] { "Date", "TeamLeaderId" });

            migrationBuilder.CreateIndex(
                name: "IX_daily_assignments_HangarId",
                table: "daily_assignments",
                column: "HangarId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_assignments_SectionId",
                table: "daily_assignments",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_assignments_ShopId",
                table: "daily_assignments",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_assignments_Status",
                table: "daily_assignments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_daily_assignments_TeamLeaderId",
                table: "daily_assignments",
                column: "TeamLeaderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assignment_details");

            migrationBuilder.DropTable(
                name: "daily_assignments");
        }
    }
}
