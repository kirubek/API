using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseOps.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionToApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Position",
                table: "users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Position",
                table: "users");
        }
    }
}
