using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseOps.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeProfileContactFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactName",
                table: "users",
                type: "nvarchar(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactPhoneNumber",
                table: "users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePhotoUrl",
                table: "users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "users",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_users_FullName",
                table: "users",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_users_HangarId_Role",
                table: "users",
                columns: new[] { "HangarId", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_users_SectionId_Role",
                table: "users",
                columns: new[] { "SectionId", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_users_ShopId_Role",
                table: "users",
                columns: new[] { "ShopId", "Role" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_FullName",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_HangarId_Role",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_SectionId_Role",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_ShopId_Role",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EmergencyContactName",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EmergencyContactPhoneNumber",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ProfilePhotoUrl",
                table: "users");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "users");
        }
    }
}
