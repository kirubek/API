using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseOps.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentsAndOperationalRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "operational_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Module = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Resource = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operational_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_workspace_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HangarId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkspaceType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AssignmentType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    StartAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_workspace_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_workspace_assignments_hangars_HangarId",
                        column: x => x.HangarId,
                        principalTable: "hangars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_workspace_assignments_sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_workspace_assignments_shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_workspace_assignments_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_operational_records_Module",
                table: "operational_records",
                column: "Module");

            migrationBuilder.CreateIndex(
                name: "IX_operational_records_Resource",
                table: "operational_records",
                column: "Resource");

            migrationBuilder.CreateIndex(
                name: "IX_operational_records_Status",
                table: "operational_records",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_user_workspace_assignments_HangarId",
                table: "user_workspace_assignments",
                column: "HangarId");

            migrationBuilder.CreateIndex(
                name: "IX_user_workspace_assignments_SectionId",
                table: "user_workspace_assignments",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_user_workspace_assignments_ShopId",
                table: "user_workspace_assignments",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_user_workspace_assignments_UserId",
                table: "user_workspace_assignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_workspace_assignments_UserId_WorkspaceType_SectionId_HangarId_ShopId",
                table: "user_workspace_assignments",
                columns: new[] { "UserId", "WorkspaceType", "SectionId", "HangarId", "ShopId" },
                unique: true,
                filter: "[SectionId] IS NOT NULL AND [HangarId] IS NOT NULL AND [ShopId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "operational_records");

            migrationBuilder.DropTable(
                name: "user_workspace_assignments");
        }
    }
}
