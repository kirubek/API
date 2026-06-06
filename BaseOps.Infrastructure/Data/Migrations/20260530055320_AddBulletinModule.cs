using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseOps.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBulletinModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bulletins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    SectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HangarId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Pinned = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bulletins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bulletins_hangars_HangarId",
                        column: x => x.HangarId,
                        principalTable: "hangars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bulletins_sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bulletins_shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bulletins_users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bulletin_attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BulletinId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    GeneratedFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UploadedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bulletin_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bulletin_attachments_bulletins_BulletinId",
                        column: x => x.BulletinId,
                        principalTable: "bulletins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bulletin_attachments_users_UploadedBy",
                        column: x => x.UploadedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bulletin_read_status",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BulletinId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bulletin_read_status", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bulletin_read_status_bulletins_BulletinId",
                        column: x => x.BulletinId,
                        principalTable: "bulletins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bulletin_read_status_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bulletin_attachments_BulletinId",
                table: "bulletin_attachments",
                column: "BulletinId");

            migrationBuilder.CreateIndex(
                name: "IX_bulletin_attachments_UploadedBy",
                table: "bulletin_attachments",
                column: "UploadedBy");

            migrationBuilder.CreateIndex(
                name: "IX_bulletin_read_status_BulletinId",
                table: "bulletin_read_status",
                column: "BulletinId");

            migrationBuilder.CreateIndex(
                name: "IX_bulletin_read_status_BulletinId_UserId",
                table: "bulletin_read_status",
                columns: new[] { "BulletinId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bulletin_read_status_ReadAt",
                table: "bulletin_read_status",
                column: "ReadAt");

            migrationBuilder.CreateIndex(
                name: "IX_bulletin_read_status_UserId",
                table: "bulletin_read_status",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_bulletins_CreatedBy",
                table: "bulletins",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_bulletins_ExpiryDate",
                table: "bulletins",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_bulletins_HangarId",
                table: "bulletins",
                column: "HangarId");

            migrationBuilder.CreateIndex(
                name: "IX_bulletins_IsActive",
                table: "bulletins",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_bulletins_IsActive_ExpiryDate",
                table: "bulletins",
                columns: new[] { "IsActive", "ExpiryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_bulletins_Priority",
                table: "bulletins",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_bulletins_Scope",
                table: "bulletins",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_bulletins_Scope_HangarId",
                table: "bulletins",
                columns: new[] { "Scope", "HangarId" });

            migrationBuilder.CreateIndex(
                name: "IX_bulletins_Scope_SectionId",
                table: "bulletins",
                columns: new[] { "Scope", "SectionId" });

            migrationBuilder.CreateIndex(
                name: "IX_bulletins_Scope_ShopId",
                table: "bulletins",
                columns: new[] { "Scope", "ShopId" });

            migrationBuilder.CreateIndex(
                name: "IX_bulletins_SectionId",
                table: "bulletins",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_bulletins_ShopId",
                table: "bulletins",
                column: "ShopId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bulletin_attachments");

            migrationBuilder.DropTable(
                name: "bulletin_read_status");

            migrationBuilder.DropTable(
                name: "bulletins");
        }
    }
}
