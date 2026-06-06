using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseOps.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteToHandovers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_handovers_SubmittedAt",
                table: "handovers");

            migrationBuilder.AlterColumn<int>(
                name: "ShiftType",
                table: "handovers",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "handovers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "handovers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "handovers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_handovers_IsDeleted",
                table: "handovers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_handovers_SubmittedAt",
                table: "handovers",
                column: "SubmittedAt",
                filter: "Status = 2 AND SubmittedAt IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_handovers_IsDeleted",
                table: "handovers");

            migrationBuilder.DropIndex(
                name: "IX_handovers_SubmittedAt",
                table: "handovers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "handovers");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "handovers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "handovers");

            migrationBuilder.AlterColumn<string>(
                name: "ShiftType",
                table: "handovers",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_handovers_SubmittedAt",
                table: "handovers",
                column: "SubmittedAt");
        }
    }
}
