using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseOps.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVersionToRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "annual_leave_requests");

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "annual_leave_requests",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[8]);

            migrationBuilder.DropColumn(
                name: "Version",
                table: "annual_leave_plans");

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "annual_leave_plans",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[8]);

            migrationBuilder.DropColumn(
                name: "Version",
                table: "annual_leave_plan_entries");

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "annual_leave_plan_entries",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[8]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "annual_leave_requests");

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "annual_leave_requests",
                type: "bigint",
                rowVersion: true,
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.DropColumn(
                name: "Version",
                table: "annual_leave_plans");

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "annual_leave_plans",
                type: "bigint",
                rowVersion: true,
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.DropColumn(
                name: "Version",
                table: "annual_leave_plan_entries");

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "annual_leave_plan_entries",
                type: "bigint",
                rowVersion: true,
                nullable: false,
                defaultValue: 0L);
        }
    }
}
