using BaseOps.Domain.Entities;
using BaseOps.Domain.Enums;
using System.Text;

namespace BaseOps.Infrastructure.Data.SeedData;

public static class SeedSqlGenerator
{
    private static readonly string DefaultPasswordHash = "$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYkP5PqZ0iK";

    public static string GenerateCompleteSeedSql()
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("-- BaseOps Complete Organizational Seed Data");
        sb.AppendLine("-- Ethiopian Airlines MRO Base Maintenance");
        sb.AppendLine("-- Password for all users: BaseOps@2026");
        sb.AppendLine("-- Generated from OrganizationalSeedData.cs");
        sb.AppendLine();
        
        // Delete existing data
        sb.AppendLine("-- Clear existing organizational data");
        sb.AppendLine("DELETE FROM users WHERE role IN ('Director', 'Manager', 'TeamLeader', 'Employee');");
        sb.AppendLine("DELETE FROM shops;");
        sb.AppendLine("DELETE FROM hangars;");
        sb.AppendLine("DELETE FROM sections;");
        sb.AppendLine("DELETE FROM workspaces;");
        sb.AppendLine();
        
        // Workspace
        sb.AppendLine("-- Workspace");
        sb.AppendLine("INSERT INTO workspaces (id, name, code, description, is_active, created_at, updated_at)");
        sb.AppendLine("VALUES ('8A7B9C1D-2E3F-4A5B-6C7D-8E9F0A1B2C3D', 'Ethiopian Airlines MRO Base Maintenance', 'ET-MRO-BASE',");
        sb.AppendLine("        'Ethiopian Airlines Maintenance, Repair, and Overhaul Base Maintenance Operations', true, NOW(), NOW())");
        sb.AppendLine("ON CONFLICT (id) DO NOTHING;");
        sb.AppendLine();
        
        // Sections
        sb.AppendLine("-- Sections");
        sb.AppendLine("INSERT INTO sections (id, name, code, created_at, updated_at) VALUES");
        sb.AppendLine("    ('A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D', 'Aircraft Cabin Maintenance', 'ACM', NOW(), NOW()),");
        sb.AppendLine("    ('B2C3D4E5-F6A7-4B5C-9D0E-1F2A3B4C5D6E', 'Aircraft Structure Maintenance', 'ASM', NOW(), NOW()),");
        sb.AppendLine("    ('C3D4E5F6-A7B8-4C5D-0E1F-2A3B4C5D6E7F', 'Aircraft Avionics Systems', 'AAS', NOW(), NOW()),");
        sb.AppendLine("    ('D4E5F6A7-B8C9-4D0E-1F2A-3B4C5D6E7F8A', 'Paint Services', 'PS', NOW(), NOW()),");
        sb.AppendLine("    ('E5F6A7B8-C9D0-4E1F-2A3B-4C5D6E7F8A9B', 'Sch Mnt B777/A350 Hangar', 'SM777', NOW(), NOW()),");
        sb.AppendLine("    ('F6A7B8C9-D0E1-4F2A-3B4C-5D6E7F8A9B0C', 'Sch Mnt B787/B767 Hangar', 'SM787', NOW(), NOW()),");
        sb.AppendLine("    ('A7B8C9D0-E1F2-4A3B-4C5D-6E7F8A9B0C1D', 'Sch Mnt B737 Hangar', 'SM737', NOW(), NOW()),");
        sb.AppendLine("    ('B8C9D0E1-F2A3-4B4C-5D6E-7F8A9B0C1D2E', 'Technical Support-Base', 'TSB', NOW(), NOW()),");
        sb.AppendLine("    ('C9D0E1F2-A3B4-4C5D-6E7F-8A9B0C1D2E3F', 'Sch Mnt Q400 Hangar', 'SMQ400', NOW(), NOW())");
        sb.AppendLine("ON CONFLICT (id) DO NOTHING;");
        sb.AppendLine();
        
        // Director
        sb.AppendLine("-- Director");
        sb.AppendLine("INSERT INTO users (id, employee_id, full_name, email, password_hash, role, section_id, hangar_id, shop_id, reports_to_user_id, is_active, must_change_password, created_at, updated_at)");
        sb.AppendLine("VALUES ('DIRECTOR-0000-0000-0000-000000000001', 'DIR001', 'Abebe Bikila', 'abebe.bikila@ethiopianairlines.com',");
        sb.AppendLine("        '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYkP5PqZ0iK', 'Director', NULL, NULL, NULL, NULL, true, false, NOW(), NOW())");
        sb.AppendLine("ON CONFLICT (id) DO NOTHING;");
        sb.AppendLine();
        
        // Managers
        sb.AppendLine("-- Managers");
        sb.AppendLine("INSERT INTO users (id, employee_id, full_name, email, password_hash, role, section_id, hangar_id, shop_id, reports_to_user_id, is_active, must_change_password, created_at, updated_at) VALUES");
        sb.AppendLine("    ('MANAGER-ACM0-0000-0000-000000000001', 'MGR001', 'Tirunesh Dibaba', 'tirunesh.dibaba@ethiopianairlines.com', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYkP5PqZ0iK', 'Manager', 'A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D', NULL, NULL, 'DIRECTOR-0000-0000-0000-000000000001', true, false, NOW(), NOW()),");
        sb.AppendLine("    ('MANAGER-ASM0-0000-0000-000000000001', 'MGR002', 'Kenenisa Bekele', 'kenenisa.bekele@ethiopianairlines.com', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYkP5PqZ0iK', 'Manager', 'B2C3D4E5-F6A7-4B5C-9D0E-1F2A3B4C5D6E', NULL, NULL, 'DIRECTOR-0000-0000-0000-000000000001', true, false, NOW(), NOW()),");
        sb.AppendLine("    ('MANAGER-AAS0-0000-0000-000000000001', 'MGR003', 'Haile Gebrselassie', 'haile.gebrselassie@ethiopianairlines.com', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYkP5PqZ0iK', 'Manager', 'C3D4E5F6-A7B8-4C5D-0E1F-2A3B4C5D6E7F', NULL, NULL, 'DIRECTOR-0000-0000-0000-000000000001', true, false, NOW(), NOW()),");
        sb.AppendLine("    ('MANAGER-PS00-0000-0000-000000000001', 'MGR004', 'Derartu Tulu', 'derartu.tulu@ethiopianairlines.com', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYkP5PqZ0iK', 'Manager', 'D4E5F6A7-B8C9-4D0E-1F2A-3B4C5D6E7F8A', NULL, NULL, 'DIRECTOR-0000-0000-0000-000000000001', true, false, NOW(), NOW()),");
        sb.AppendLine("    ('MANAGER-SM77-0000-0000-000000000001', 'MGR005', 'Meseret Defar', 'meseret.defar@ethiopianairlines.com', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYkP5PqZ0iK', 'Manager', 'E5F6A7B8-C9D0-4E1F-2A3B-4C5D6E7F8A9B', NULL, NULL, 'DIRECTOR-0000-0000-0000-000000000001', true, false, NOW(), NOW()),");
        sb.AppendLine("    ('MANAGER-SM78-0000-0000-000000000001', 'MGR006', 'Ejegayehu Dibaba', 'ejegayehu.dibaba@ethiopianairlines.com', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYkP5PqZ0iK', 'Manager', 'F6A7B8C9-D0E1-4F2A-3B4C-5D6E7F8A9B0C', NULL, NULL, 'DIRECTOR-0000-0000-0000-000000000001', true, false, NOW(), NOW()),");
        sb.AppendLine("    ('MANAGER-SM73-0000-0000-000000000001', 'MGR007', 'Genzebe Dibaba', 'genzebe.dibaba@ethiopianairlines.com', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYkP5PqZ0iK', 'Manager', 'A7B8C9D0-E1F2-4A3B-4C5D-6E7F8A9B0C1D', NULL, NULL, 'DIRECTOR-0000-0000-0000-000000000001', true, false, NOW(), NOW()),");
        sb.AppendLine("    ('MANAGER-TSB0-0000-0000-000000000001', 'MGR008', 'Almaz Ayana', 'almaz.ayana@ethiopianairlines.com', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYkP5PqZ0iK', 'Manager', 'B8C9D0E1-F2A3-4B4C-5D6E-7F8A9B0C1D2E', NULL, NULL, 'DIRECTOR-0000-0000-0000-000000000001', true, false, NOW(), NOW()),");
        sb.AppendLine("    ('MANAGER-SMQ4-0000-0000-000000000001', 'MGR009', 'Feyisa Lilesa', 'feyisa.lilesa@ethiopianairlines.com', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYkP5PqZ0iK', 'Manager', 'C9D0E1F2-A3B4-4C5D-6E7F-8A9B0C1D2E3F', NULL, NULL, 'DIRECTOR-0000-0000-0000-000000000001', true, false, NOW(), NOW())");
        sb.AppendLine("ON CONFLICT (id) DO NOTHING;");
        sb.AppendLine();
        
        // Hangars
        sb.AppendLine("-- Hangars");
        sb.AppendLine("INSERT INTO hangars (id, name, code, section_id, created_at, updated_at) VALUES");
        sb.AppendLine("    -- Aircraft Cabin Maintenance");
        sb.AppendLine("    ('HANGAR-ACM-Q4-0000-0000-000000000001', 'Q400 HANGAR', 'Q400-ACM', 'A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D', NOW(), NOW()),");
        sb.AppendLine("    ('HANGAR-ACM-77-0000-0000-000000000001', 'B777/A350 HANGAR', 'B777-ACM', 'A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D', NOW(), NOW()),");
        sb.AppendLine("    ('HANGAR-ACM-78-0000-0000-000000000001', 'B787/B767 HANGAR', 'B787-ACM', 'A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D', NOW(), NOW()),");
        sb.AppendLine("    ('HANGAR-ACM-73-0000-0000-000000000001', 'B737 HANGAR', 'B737-ACM', 'A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D', NOW(), NOW()),");
        sb.AppendLine("    -- Aircraft Structure Maintenance");
        sb.AppendLine("    ('HANGAR-ASM-Q4-0000-0000-000000000001', 'Q400 HANGAR', 'Q400-ASM', 'B2C3D4E5-F6A7-4B5C-9D0E-1F2A3B4C5D6E', NOW(), NOW()),");
        sb.AppendLine("    ('HANGAR-ASM-77-0000-0000-000000000001', 'B777/A350 HANGAR', 'B777-ASM', 'B2C3D4E5-F6A7-4B5C-9D0E-1F2A3B4C5D6E', NOW(), NOW()),");
        sb.AppendLine("    ('HANGAR-ASM-78-0000-0000-000000000001', 'B787/B767 HANGAR', 'B787-ASM', 'B2C3D4E5-F6A7-4B5C-9D0E-1F2A3B4C5D6E', NOW(), NOW()),");
        sb.AppendLine("    ('HANGAR-ASM-73-0000-0000-000000000001', 'B737 HANGAR', 'B737-ASM', 'B2C3D4E5-F6A7-4B5C-9D0E-1F2A3B4C5D6E', NOW(), NOW()),");
        sb.AppendLine("    -- Aircraft Avionics Systems");
        sb.AppendLine("    ('HANGAR-AAS-Q4-0000-0000-000000000001', 'Q400 HANGAR', 'Q400-AAS', 'C3D4E5F6-A7B8-4C5D-0E1F-2A3B4C5D6E7F', NOW(), NOW()),");
        sb.AppendLine("    -- Paint Services");
        sb.AppendLine("    ('HANGAR-PS-PNT-0000-0000-000000000001', 'PAINT HANGAR', 'PAINT-PS', 'D4E5F6A7-B8C9-4D0E-1F2A-3B4C5D6E7F8A', NOW(), NOW()),");
        sb.AppendLine("    -- Sch Mnt B777/A350");
        sb.AppendLine("    ('HANGAR-SM77-77-0000-0000-000000000001', 'B777/A350 HANGAR', 'B777-SM77', 'E5F6A7B8-C9D0-4E1F-2A3B-4C5D6E7F8A9B', NOW(), NOW()),");
        sb.AppendLine("    -- Sch Mnt B787/B767");
        sb.AppendLine("    ('HANGAR-SM78-78-0000-0000-000000000001', 'B787/B767 HANGAR', 'B787-SM78', 'F6A7B8C9-D0E1-4F2A-3B4C-5D6E7F8A9B0C', NOW(), NOW()),");
        sb.AppendLine("    -- Sch Mnt B737");
        sb.AppendLine("    ('HANGAR-SM73-73-0000-0000-000000000001', 'B737 HANGAR', 'B737-SM73', 'A7B8C9D0-E1F2-4A3B-4C5D-6E7F8A9B0C1D', NOW(), NOW()),");
        sb.AppendLine("    -- Sch Mnt Q400");
        sb.AppendLine("    ('HANGAR-SMQ4-Q4-0000-0000-000000000001', 'Q400 HANGAR', 'Q400-SMQ4', 'C9D0E1F2-A3B4-4C5D-6E7F-8A9B0C1D2E3F', NOW(), NOW()),");
        sb.AppendLine("    -- Technical Support-Base");
        sb.AppendLine("    ('HANGAR-TSB-Q4-0000-0000-000000000001', 'Q400 HANGAR', 'Q400-TSB', 'B8C9D0E1-F2A3-4B4C-5D6E-7F8A9B0C1D2E', NOW(), NOW()),");
        sb.AppendLine("    ('HANGAR-TSB-77-0000-0000-000000000001', 'B777/A350 HANGAR', 'B777-TSB', 'B8C9D0E1-F2A3-4B4C-5D6E-7F8A9B0C1D2E', NOW(), NOW()),");
        sb.AppendLine("    ('HANGAR-TSB-78-0000-0000-000000000001', 'B787/B767 HANGAR', 'B787-TSB', 'B8C9D0E1-F2A3-4B4C-5D6E-7F8A9B0C1D2E', NOW(), NOW()),");
        sb.AppendLine("    ('HANGAR-TSB-PN-0000-0000-000000000001', 'PAINT HANGAR', 'PAINT-TSB', 'B8C9D0E1-F2A3-4B4C-5D6E-7F8A9B0C1D2E', NOW(), NOW()),");
        sb.AppendLine("    ('HANGAR-TSB-73-0000-0000-000000000001', 'B737 HANGAR', 'B737-TSB', 'B8C9D0E1-F2A3-4B4C-5D6E-7F8A9B0C1D2E', NOW(), NOW())");
        sb.AppendLine("ON CONFLICT (id) DO NOTHING;");
        sb.AppendLine();
        
        // Shops
        sb.AppendLine("-- Shops");
        sb.AppendLine("INSERT INTO shops (id, name, code, section_id, created_at, updated_at) VALUES");
        sb.AppendLine("    ('SHOP-ACM-CAB-0000-0000-000000000001', 'CABIN COMPONENTS MAINTENANCE SHOP', 'CCM-ACM', 'A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D', NOW(), NOW()),");
        sb.AppendLine("    ('SHOP-ASM-COM-0000-0000-000000000001', 'COMPOSITE SHOP', 'COMP-ASM', 'B2C3D4E5-F6A7-4B5C-9D0E-1F2A3B4C5D6E', NOW(), NOW()),");
        sb.AppendLine("    ('SHOP-ASM-SMT-0000-0000-000000000001', 'SHEET METAL SHOP', 'SMTL-ASM', 'B2C3D4E5-F6A7-4B5C-9D0E-1F2A3B4C5D6E', NOW(), NOW())");
        sb.AppendLine("ON CONFLICT (id) DO NOTHING;");
        sb.AppendLine();
        
        sb.AppendLine("-- Note: Team Leaders and Employees are seeded via EF Core HasData in OrganizationalSeedData.cs");
        sb.AppendLine("-- Run the application startup to automatically seed all 200+ users");
        sb.AppendLine("-- Alternatively, use the TeamLeadersAndEmployees.sql script for manual seeding");
        
        return sb.ToString();
    }
}
