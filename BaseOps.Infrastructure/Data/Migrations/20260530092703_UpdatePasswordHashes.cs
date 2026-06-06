using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseOps.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePasswordHashes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update password hashes for all organizational users to the correct BCrypt hash for "BaseOps@2026"
            migrationBuilder.Sql(@"
                UPDATE users 
                SET PasswordHash = '$2a$12$0qO8pQ1Qk3vTqvkrFduX9OI.fDC55QJ4QwJtkaXKb4rUQnGlWjQhm'
                WHERE EmployeeId IN ('DIR001', 'MGR001', 'MGR002', 'MGR003', 'MGR004', 'MGR005', 'MGR006', 'MGR007', 'MGR008', 'MGR009')
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
