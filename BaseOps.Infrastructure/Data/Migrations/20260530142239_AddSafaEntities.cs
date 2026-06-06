using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseOps.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSafaEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "safa_inspections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InspectionType = table.Column<int>(type: "int", nullable: false),
                    FleetType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AircraftRegistration = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FlightInfo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    InspectionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HangarId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InspectorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Shift = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Conclusion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SubmittedBy = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_safa_inspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_safa_inspections_hangars_HangarId",
                        column: x => x.HangarId,
                        principalTable: "hangars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_safa_inspections_sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_safa_inspections_shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_safa_inspections_users_InspectorId",
                        column: x => x.InspectorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "safa_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InspectionType = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TemplateJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_safa_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "safa_defects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InspectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StandardDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObservationFinding = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NeedToFix = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CorrectiveAction = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TaskCardCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PartRequestId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ActionTakenByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActionTakenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_safa_defects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_safa_defects_safa_inspections_InspectionId",
                        column: x => x.InspectionId,
                        principalTable: "safa_inspections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_safa_defects_users_ActionTakenByUserId",
                        column: x => x.ActionTakenByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_safa_defects_ActionTakenByUserId",
                table: "safa_defects",
                column: "ActionTakenByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_safa_defects_Category",
                table: "safa_defects",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_safa_defects_Category_Status",
                table: "safa_defects",
                columns: new[] { "Category", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_safa_defects_InspectionId",
                table: "safa_defects",
                column: "InspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_safa_defects_InspectionId_Status",
                table: "safa_defects",
                columns: new[] { "InspectionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_safa_defects_Status",
                table: "safa_defects",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_safa_inspections_AircraftRegistration",
                table: "safa_inspections",
                column: "AircraftRegistration");

            migrationBuilder.CreateIndex(
                name: "IX_safa_inspections_FleetType",
                table: "safa_inspections",
                column: "FleetType");

            migrationBuilder.CreateIndex(
                name: "IX_safa_inspections_HangarId",
                table: "safa_inspections",
                column: "HangarId");

            migrationBuilder.CreateIndex(
                name: "IX_safa_inspections_InspectionDate",
                table: "safa_inspections",
                column: "InspectionDate");

            migrationBuilder.CreateIndex(
                name: "IX_safa_inspections_InspectionDate_Status",
                table: "safa_inspections",
                columns: new[] { "InspectionDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_safa_inspections_InspectionType",
                table: "safa_inspections",
                column: "InspectionType");

            migrationBuilder.CreateIndex(
                name: "IX_safa_inspections_InspectorId",
                table: "safa_inspections",
                column: "InspectorId");

            migrationBuilder.CreateIndex(
                name: "IX_safa_inspections_InspectorId_Status",
                table: "safa_inspections",
                columns: new[] { "InspectorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_safa_inspections_SectionId",
                table: "safa_inspections",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_safa_inspections_SectionId_HangarId",
                table: "safa_inspections",
                columns: new[] { "SectionId", "HangarId" });

            migrationBuilder.CreateIndex(
                name: "IX_safa_inspections_SectionId_ShopId",
                table: "safa_inspections",
                columns: new[] { "SectionId", "ShopId" });

            migrationBuilder.CreateIndex(
                name: "IX_safa_inspections_ShopId",
                table: "safa_inspections",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_safa_inspections_Status",
                table: "safa_inspections",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_safa_templates_InspectionType",
                table: "safa_templates",
                column: "InspectionType");

            migrationBuilder.CreateIndex(
                name: "IX_safa_templates_InspectionType_IsActive",
                table: "safa_templates",
                columns: new[] { "InspectionType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_safa_templates_IsActive",
                table: "safa_templates",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "safa_defects");

            migrationBuilder.DropTable(
                name: "safa_templates");

            migrationBuilder.DropTable(
                name: "safa_inspections");
        }
    }
}
