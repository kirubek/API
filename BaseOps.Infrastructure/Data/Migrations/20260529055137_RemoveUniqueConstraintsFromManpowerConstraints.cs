using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseOps.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueConstraintsFromManpowerConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_manpower_constraints_HangarId_Year",
                table: "manpower_constraints");

            migrationBuilder.DropIndex(
                name: "IX_manpower_constraints_SectionId_Year",
                table: "manpower_constraints");

            migrationBuilder.CreateIndex(
                name: "IX_manpower_constraints_HangarId_Year",
                table: "manpower_constraints",
                columns: new[] { "HangarId", "Year" });

            migrationBuilder.CreateIndex(
                name: "IX_manpower_constraints_SectionId_Year",
                table: "manpower_constraints",
                columns: new[] { "SectionId", "Year" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_manpower_constraints_HangarId_Year",
                table: "manpower_constraints");

            migrationBuilder.DropIndex(
                name: "IX_manpower_constraints_SectionId_Year",
                table: "manpower_constraints");

            migrationBuilder.CreateIndex(
                name: "IX_manpower_constraints_HangarId_Year",
                table: "manpower_constraints",
                columns: new[] { "HangarId", "Year" },
                unique: true,
                filter: "[HangarId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_manpower_constraints_SectionId_Year",
                table: "manpower_constraints",
                columns: new[] { "SectionId", "Year" },
                unique: true);
        }
    }
}
