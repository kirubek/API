using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseOps.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedOrganizationalStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed Workspace
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM workspaces WHERE Id = '8A7B9C1D-2E3F-4A5B-6C7D-8E9F0A1B2C3D')
                INSERT INTO workspaces (Id, Name, Code, Description, IsActive, CreatedAt, UpdatedAt, Version)
                VALUES ('8A7B9C1D-2E3F-4A5B-6C7D-8E9F0A1B2C3D', 'Ethiopian Airlines MRO Base Maintenance', 'ET-MRO-BASE', 
                        'Ethiopian Airlines Maintenance, Repair, and Overhaul Base Maintenance Operations', 1, GETUTCDATE(), GETUTCDATE(), 0)
            ");

            // Seed Sections
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sections WHERE Id = 'A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D')
                INSERT INTO sections (Id, Name, Code, CreatedAt, UpdatedAt, Version)
                VALUES 
                    ('A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D', 'Aircraft Cabin Maintenance', 'ACM', GETUTCDATE(), GETUTCDATE(), 0),
                    ('B2C3D4E5-F6A7-4B5C-9D0E-1F2A3B4C5D6E', 'Aircraft Structure Maintenance', 'ASM', GETUTCDATE(), GETUTCDATE(), 0),
                    ('C3D4E5F6-A7B8-4C5D-0E1F-2A3B4C5D6E7F', 'Aircraft Avionics Systems', 'AAS', GETUTCDATE(), GETUTCDATE(), 0),
                    ('D4E5F6A7-B8C9-4D0E-1F2A-3B4C5D6E7F8A', 'Paint Services', 'PS', GETUTCDATE(), GETUTCDATE(), 0),
                    ('E5F6A7B8-C9D0-4E1F-2A3B-4C5D6E7F8A9B', 'Sch Mnt B777/A350 Hangar', 'SM777', GETUTCDATE(), GETUTCDATE(), 0),
                    ('F6A7B8C9-D0E1-4F2A-3B4C-5D6E7F8A9B0C', 'Sch Mnt B787/B767 Hangar', 'SM787', GETUTCDATE(), GETUTCDATE(), 0),
                    ('A7B8C9D0-E1F2-4A3B-4C5D-6E7F8A9B0C1D', 'Sch Mnt B737 Hangar', 'SM737', GETUTCDATE(), GETUTCDATE(), 0),
                    ('B8C9D0E1-F2A3-4B4C-5D6E-7F8A9B0C1D2E', 'Technical Support-Base', 'TSB', GETUTCDATE(), GETUTCDATE(), 0),
                    ('C9D0E1F2-A3B4-4C5D-6E7F-8A9B0C1D2E3F', 'Sch Mnt Q400 Hangar', 'SMQ400', GETUTCDATE(), GETUTCDATE(), 0)
            ");

            // Seed Director
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM users WHERE Id = '00000000-0000-0000-0000-000000000001')
                INSERT INTO users (Id, EmployeeId, FullName, Email, PasswordHash, Role, SectionId, HangarId, ShopId, ReportsToUserId, IsActive, MustChangePassword, CreatedAt, UpdatedAt, Version)
                VALUES ('00000000-0000-0000-0000-000000000001', 'DIR001', 'Abebe Bikila', 'abebe.bikila@ethiopianairlines.com', 
                        '$2a$12$0qO8pQ1Qk3vTqvkrFduX9OI.fDC55QJ4QwJtkaXKb4rUQnGlWjQhm', 'Director', NULL, NULL, NULL, NULL, 1, 0, GETUTCDATE(), GETUTCDATE(), 0)
            ");

            // Seed Managers
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM users WHERE Id = '00000000-0000-0000-0000-000000000002')
                INSERT INTO users (Id, EmployeeId, FullName, Email, PasswordHash, Role, SectionId, HangarId, ShopId, ReportsToUserId, IsActive, MustChangePassword, CreatedAt, UpdatedAt, Version)
                VALUES 
                    ('00000000-0000-0000-0000-000000000002', 'MGR001', 'Tirunesh Dibaba', 'tirunesh.dibaba@ethiopianairlines.com', '$2a$12$0qO8pQ1Qk3vTqvkrFduX9OI.fDC55QJ4QwJtkaXKb4rUQnGlWjQhm', 'Manager', 'A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D', NULL, NULL, '00000000-0000-0000-0000-000000000001', 1, 0, GETUTCDATE(), GETUTCDATE(), 0),
                    ('00000000-0000-0000-0000-000000000003', 'MGR002', 'Kenenisa Bekele', 'kenenisa.bekele@ethiopianairlines.com', '$2a$12$0qO8pQ1Qk3vTqvkrFduX9OI.fDC55QJ4QwJtkaXKb4rUQnGlWjQhm', 'Manager', 'B2C3D4E5-F6A7-4B5C-9D0E-1F2A3B4C5D6E', NULL, NULL, '00000000-0000-0000-0000-000000000001', 1, 0, GETUTCDATE(), GETUTCDATE(), 0),
                    ('00000000-0000-0000-0000-000000000004', 'MGR003', 'Haile Gebrselassie', 'haile.gebrselassie@ethiopianairlines.com', '$2a$12$0qO8pQ1Qk3vTqvkrFduX9OI.fDC55QJ4QwJtkaXKb4rUQnGlWjQhm', 'Manager', 'C3D4E5F6-A7B8-4C5D-0E1F-2A3B4C5D6E7F', NULL, NULL, '00000000-0000-0000-0000-000000000001', 1, 0, GETUTCDATE(), GETUTCDATE(), 0),
                    ('00000000-0000-0000-0000-000000000005', 'MGR004', 'Derartu Tulu', 'derartu.tulu@ethiopianairlines.com', '$2a$12$0qO8pQ1Qk3vTqvkrFduX9OI.fDC55QJ4QwJtkaXKb4rUQnGlWjQhm', 'Manager', 'D4E5F6A7-B8C9-4D0E-1F2A-3B4C5D6E7F8A', NULL, NULL, '00000000-0000-0000-0000-000000000001', 1, 0, GETUTCDATE(), GETUTCDATE(), 0),
                    ('00000000-0000-0000-0000-000000000006', 'MGR005', 'Meseret Defar', 'meseret.defar@ethiopianairlines.com', '$2a$12$0qO8pQ1Qk3vTqvkrFduX9OI.fDC55QJ4QwJtkaXKb4rUQnGlWjQhm', 'Manager', 'E5F6A7B8-C9D0-4E1F-2A3B-4C5D6E7F8A9B', NULL, NULL, '00000000-0000-0000-0000-000000000001', 1, 0, GETUTCDATE(), GETUTCDATE(), 0),
                    ('00000000-0000-0000-0000-000000000007', 'MGR006', 'Ejegayehu Dibaba', 'ejegayehu.dibaba@ethiopianairlines.com', '$2a$12$0qO8pQ1Qk3vTqvkrFduX9OI.fDC55QJ4QwJtkaXKb4rUQnGlWjQhm', 'Manager', 'F6A7B8C9-D0E1-4F2A-3B4C-5D6E7F8A9B0C', NULL, NULL, '00000000-0000-0000-0000-000000000001', 1, 0, GETUTCDATE(), GETUTCDATE(), 0),
                    ('00000000-0000-0000-0000-000000000008', 'MGR007', 'Genzebe Dibaba', 'genzebe.dibaba@ethiopianairlines.com', '$2a$12$0qO8pQ1Qk3vTqvkrFduX9OI.fDC55QJ4QwJtkaXKb4rUQnGlWjQhm', 'Manager', 'A7B8C9D0-E1F2-4A3B-4C5D-6E7F8A9B0C1D', NULL, NULL, '00000000-0000-0000-0000-000000000001', 1, 0, GETUTCDATE(), GETUTCDATE(), 0),
                    ('00000000-0000-0000-0000-000000000009', 'MGR008', 'Almaz Ayana', 'almaz.ayana@ethiopianairlines.com', '$2a$12$0qO8pQ1Qk3vTqvkrFduX9OI.fDC55QJ4QwJtkaXKb4rUQnGlWjQhm', 'Manager', 'B8C9D0E1-F2A3-4B4C-5D6E-7F8A9B0C1D2E', NULL, NULL, '00000000-0000-0000-0000-000000000001', 1, 0, GETUTCDATE(), GETUTCDATE(), 0),
                    ('00000000-0000-0000-0000-000000000010', 'MGR009', 'Feyisa Lilesa', 'feyisa.lilesa@ethiopianairlines.com', '$2a$12$0qO8pQ1Qk3vTqvkrFduX9OI.fDC55QJ4QwJtkaXKb4rUQnGlWjQhm', 'Manager', 'C9D0E1F2-A3B4-4C5D-6E7F-8A9B0C1D2E3F', NULL, NULL, '00000000-0000-0000-0000-000000000001', 1, 0, GETUTCDATE(), GETUTCDATE(), 0)
            ");

            // Seed Hangars
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM hangars WHERE Id = '10000000-0000-0000-0000-000000000001')
                INSERT INTO hangars (Id, Name, Code, SectionId, CreatedAt, UpdatedAt, Version)
                VALUES 
                    -- Aircraft Cabin Maintenance
                    ('10000000-0000-0000-0000-000000000001', 'Q400 HANGAR', 'Q400-ACM', 'A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D', GETUTCDATE(), GETUTCDATE(), 0),
                    ('10000000-0000-0000-0000-000000000002', 'B777/A350 HANGAR', 'B777-ACM', 'A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D', GETUTCDATE(), GETUTCDATE(), 0),
                    ('10000000-0000-0000-0000-000000000003', 'B787/B767 HANGAR', 'B787-ACM', 'A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D', GETUTCDATE(), GETUTCDATE(), 0),
                    ('10000000-0000-0000-0000-000000000004', 'B737 HANGAR', 'B737-ACM', 'A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D', GETUTCDATE(), GETUTCDATE(), 0),
                    -- Aircraft Structure Maintenance
                    ('10000000-0000-0000-0000-000000000005', 'Q400 HANGAR', 'Q400-ASM', 'B2C3D4E5-F6A7-4B5C-9D0E-1F2A3B4C5D6E', GETUTCDATE(), GETUTCDATE(), 0),
                    ('10000000-0000-0000-0000-000000000006', 'B777/A350 HANGAR', 'B777-ASM', 'B2C3D4E5-F6A7-4B5C-9D0E-1F2A3B4C5D6E', GETUTCDATE(), GETUTCDATE(), 0),
                    ('10000000-0000-0000-0000-000000000007', 'B787/B767 HANGAR', 'B787-ASM', 'B2C3D4E5-F6A7-4B5C-9D0E-1F2A3B4C5D6E', GETUTCDATE(), GETUTCDATE(), 0),
                    ('10000000-0000-0000-0000-000000000008', 'B737 HANGAR', 'B737-ASM', 'B2C3D4E5-F6A7-4B5C-9D0E-1F2A3B4C5D6E', GETUTCDATE(), GETUTCDATE(), 0),
                    -- Aircraft Avionics Systems
                    ('10000000-0000-0000-0000-000000000009', 'Q400 HANGAR', 'Q400-AAS', 'C3D4E5F6-A7B8-4C5D-0E1F-2A3B4C5D6E7F', GETUTCDATE(), GETUTCDATE(), 0),
                    -- Paint Services
                    ('10000000-0000-0000-0000-000000000010', 'PAINT HANGAR', 'PAINT-PS', 'D4E5F6A7-B8C9-4D0E-1F2A-3B4C5D6E7F8A', GETUTCDATE(), GETUTCDATE(), 0),
                    -- Sch Mnt B777/A350
                    ('10000000-0000-0000-0000-000000000011', 'B777/A350 HANGAR', 'B777-SM77', 'E5F6A7B8-C9D0-4E1F-2A3B-4C5D6E7F8A9B', GETUTCDATE(), GETUTCDATE(), 0),
                    -- Sch Mnt B787/B767
                    ('10000000-0000-0000-0000-000000000012', 'B787/B767 HANGAR', 'B787-SM78', 'F6A7B8C9-D0E1-4F2A-3B4C-5D6E7F8A9B0C', GETUTCDATE(), GETUTCDATE(), 0),
                    -- Sch Mnt B737
                    ('10000000-0000-0000-0000-000000000013', 'B737 HANGAR', 'B737-SM73', 'A7B8C9D0-E1F2-4A3B-4C5D-6E7F8A9B0C1D', GETUTCDATE(), GETUTCDATE(), 0),
                    -- Sch Mnt Q400
                    ('10000000-0000-0000-0000-000000000014', 'Q400 HANGAR', 'Q400-SMQ4', 'C9D0E1F2-A3B4-4C5D-6E7F-8A9B0C1D2E3F', GETUTCDATE(), GETUTCDATE(), 0),
                    -- Technical Support-Base
                    ('10000000-0000-0000-0000-000000000015', 'Q400 HANGAR', 'Q400-TSB', 'B8C9D0E1-F2A3-4B4C-5D6E-7F8A9B0C1D2E', GETUTCDATE(), GETUTCDATE(), 0),
                    ('10000000-0000-0000-0000-000000000016', 'B777/A350 HANGAR', 'B777-TSB', 'B8C9D0E1-F2A3-4B4C-5D6E-7F8A9B0C1D2E', GETUTCDATE(), GETUTCDATE(), 0),
                    ('10000000-0000-0000-0000-000000000017', 'B787/B767 HANGAR', 'B787-TSB', 'B8C9D0E1-F2A3-4B4C-5D6E-7F8A9B0C1D2E', GETUTCDATE(), GETUTCDATE(), 0),
                    ('10000000-0000-0000-0000-000000000018', 'PAINT HANGAR', 'PAINT-TSB', 'B8C9D0E1-F2A3-4B4C-5D6E-7F8A9B0C1D2E', GETUTCDATE(), GETUTCDATE(), 0),
                    ('10000000-0000-0000-0000-000000000019', 'B737 HANGAR', 'B737-TSB', 'B8C9D0E1-F2A3-4B4C-5D6E-7F8A9B0C1D2E', GETUTCDATE(), GETUTCDATE(), 0)
            ");

            // Seed Shops
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM shops WHERE Id = '20000000-0000-0000-0000-000000000001')
                INSERT INTO shops (Id, Name, Code, SectionId, CreatedAt, UpdatedAt, Version)
                VALUES 
                    ('20000000-0000-0000-0000-000000000001', 'CABIN COMPONENTS MAINTENANCE SHOP', 'CCM-ACM', 'A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D', GETUTCDATE(), GETUTCDATE(), 0),
                    ('20000000-0000-0000-0000-000000000002', 'COMPOSITE SHOP', 'COMP-ASM', 'B2C3D4E5-F6A7-4B5C-9D0E-1F2A3B4C5D6E', GETUTCDATE(), GETUTCDATE(), 0),
                    ('20000000-0000-0000-0000-000000000003', 'SHEET METAL SHOP', 'SMTL-ASM', 'B2C3D4E5-F6A7-4B5C-9D0E-1F2A3B4C5D6E', GETUTCDATE(), GETUTCDATE(), 0)
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
