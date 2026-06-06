using BaseOps.Domain.Entities;
using BaseOps.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace BaseOps.Infrastructure.Data.SeedData;

public static class OrganizationalSeedData
{
    // Pre-computed BCrypt hash for "BaseOps@2026" (work factor 12)
    private static readonly string DefaultPasswordHash = "$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYkP5PqZ0iK";

    // Static timestamp for seed data to avoid model changes
    private static readonly DateTimeOffset SeedTimestamp = new DateTimeOffset(2026, 5, 30, 0, 0, 0, TimeSpan.Zero);

    // Workspace
    public static readonly Workspace EthiopianAirlinesWorkspace = new()
    {
        Id = Guid.Parse("8A7B9C1D-2E3F-4A5B-6C7D-8E9F0A1B2C3D"),
        Name = "Ethiopian Airlines MRO Base Maintenance",
        Code = "ET-MRO-BASE",
        Description = "Ethiopian Airlines Maintenance, Repair, and Overhaul Base Maintenance Operations",
        IsActive = true,
        CreatedAt = SeedTimestamp,
        UpdatedAt = SeedTimestamp
    };

    // Sections
    public static readonly Section AircraftCabinMaintenance = new()
    {
        Id = Guid.Parse("A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D"),
        Name = "Aircraft Cabin Maintenance",
        Code = "ACM",
        CreatedAt = SeedTimestamp,
        UpdatedAt = SeedTimestamp
    };

    public static readonly Section AircraftStructureMaintenance = new()
    {
        Id = Guid.Parse("B2C3D4E5-F6A7-4B5C-9D0E-1F2A3B4C5D6E"),
        Name = "Aircraft Structure Maintenance",
        Code = "ASM",
        CreatedAt = SeedTimestamp,
        UpdatedAt = SeedTimestamp
    };

    public static readonly Section AircraftAvionicsSystems = new()
    {
        Id = Guid.Parse("C3D4E5F6-A7B8-4C5D-0E1F-2A3B4C5D6E7F"),
        Name = "Aircraft Avionics Systems",
        Code = "AAS",
        CreatedAt = SeedTimestamp,
        UpdatedAt = SeedTimestamp
    };

    public static readonly Section PaintServices = new()
    {
        Id = Guid.Parse("D4E5F6A7-B8C9-4D0E-1F2A-3B4C5D6E7F8A"),
        Name = "Paint Services",
        Code = "PS",
        CreatedAt = SeedTimestamp,
        UpdatedAt = SeedTimestamp
    };

    public static readonly Section SchMntB777A350Hangar = new()
    {
        Id = Guid.Parse("E5F6A7B8-C9D0-4E1F-2A3B-4C5D6E7F8A9B"),
        Name = "Sch Mnt B777/A350 Hangar",
        Code = "SM777",
        CreatedAt = SeedTimestamp,
        UpdatedAt = SeedTimestamp
    };

    public static readonly Section SchMntB787B767Hangar = new()
    {
        Id = Guid.Parse("F6A7B8C9-D0E1-4F2A-3B4C-5D6E7F8A9B0C"),
        Name = "Sch Mnt B787/B767 Hangar",
        Code = "SM787",
        CreatedAt = SeedTimestamp,
        UpdatedAt = SeedTimestamp
    };

    public static readonly Section SchMntB737Hangar = new()
    {
        Id = Guid.Parse("A7B8C9D0-E1F2-4A3B-4C5D-6E7F8A9B0C1D"),
        Name = "Sch Mnt B737 Hangar",
        Code = "SM737",
        CreatedAt = SeedTimestamp,
        UpdatedAt = SeedTimestamp
    };

    public static readonly Section TechnicalSupportBase = new()
    {
        Id = Guid.Parse("B8C9D0E1-F2A3-4B4C-5D6E-7F8A9B0C1D2E"),
        Name = "Technical Support-Base",
        Code = "TSB",
        CreatedAt = SeedTimestamp,
        UpdatedAt = SeedTimestamp
    };

    public static readonly Section SchMntQ400Hangar = new()
    {
        Id = Guid.Parse("C9D0E1F2-A3B4-4C5D-6E7F-8A9B0C1D2E3F"),
        Name = "Sch Mnt Q400 Hangar",
        Code = "SMQ400",
        CreatedAt = SeedTimestamp,
        UpdatedAt = SeedTimestamp
    };

    // Director
    public static readonly ApplicationUser Director = new()
    {
        Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
        EmployeeId = "DIR001",
        FullName = "Abebe Bikila",
        Email = "abebe.bikila@ethiopianairlines.com",
        PasswordHash = DefaultPasswordHash,
        Role = UserRole.Director,
        SectionId = null,
        HangarId = null,
        ShopId = null,
        ReportsToUserId = null,
        IsActive = true,
        MustChangePassword = false,
        CreatedAt = SeedTimestamp,
        UpdatedAt = SeedTimestamp
    };

    // Managers
    public static readonly ApplicationUser ManagerAircraftCabinMaintenance = new()
    {
        Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
        EmployeeId = "MGR001",
        FullName = "Tirunesh Dibaba",
        Email = "tirunesh.dibaba@ethiopianairlines.com",
        PasswordHash = DefaultPasswordHash,
        Role = UserRole.Manager,
        SectionId = AircraftCabinMaintenance.Id,
        HangarId = null,
        ShopId = null,
        ReportsToUserId = Director.Id,
        IsActive = true,
        MustChangePassword = false,
        CreatedAt = SeedTimestamp,
        UpdatedAt = SeedTimestamp
    };

    public static readonly ApplicationUser ManagerAircraftStructureMaintenance = new()
    {
        Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
        EmployeeId = "MGR002",
        FullName = "Kenenisa Bekele",
        Email = "kenenisa.bekele@ethiopianairlines.com",
        PasswordHash = DefaultPasswordHash,
        Role = UserRole.Manager,
        SectionId = AircraftStructureMaintenance.Id,
        HangarId = null,
        ShopId = null,
        ReportsToUserId = Director.Id,
        IsActive = true,
        MustChangePassword = false,
        CreatedAt = SeedTimestamp,
        UpdatedAt = SeedTimestamp
    };

    public static readonly ApplicationUser ManagerAircraftAvionicsSystems = new()
    {
        Id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
        EmployeeId = "MGR003",
        FullName = "Haile Gebrselassie",
        Email = "haile.gebrselassie@ethiopianairlines.com",
        PasswordHash = DefaultPasswordHash,
        Role = UserRole.Manager,
        SectionId = AircraftAvionicsSystems.Id,
        HangarId = null,
        ShopId = null,
        ReportsToUserId = Director.Id,
        IsActive = true,
        MustChangePassword = false,
        CreatedAt = SeedTimestamp,
        UpdatedAt = SeedTimestamp
    };

    public static readonly ApplicationUser ManagerPaintServices = new()
    {
        Id = Guid.Parse("00000000-0000-0000-0000-000000000005"),
        EmployeeId = "MGR004",
        FullName = "Derartu Tulu",
        Email = "derartu.tulu@ethiopianairlines.com",
        PasswordHash = DefaultPasswordHash,
        Role = UserRole.Manager,
        SectionId = PaintServices.Id,
        HangarId = null,
        ShopId = null,
        ReportsToUserId = Director.Id,
        IsActive = true,
        MustChangePassword = false,
        CreatedAt = SeedTimestamp,
        UpdatedAt = SeedTimestamp
    };

    public static readonly ApplicationUser ManagerSchMntB777A350Hangar = new()
    {
        Id = Guid.Parse("00000000-0000-0000-0000-000000000006"),
        EmployeeId = "MGR005",
        FullName = "Meseret Defar",
        Email = "meseret.defar@ethiopianairlines.com",
        PasswordHash = DefaultPasswordHash,
        Role = UserRole.Manager,
        SectionId = SchMntB777A350Hangar.Id,
        HangarId = null,
        ShopId = null,
        ReportsToUserId = Director.Id,
        IsActive = true,
        MustChangePassword = false,
        CreatedAt = SeedTimestamp,
        UpdatedAt = SeedTimestamp
    };

    public static readonly ApplicationUser ManagerSchMntB787B767Hangar = new()
    {
        Id = Guid.Parse("00000000-0000-0000-0000-000000000007"),
        EmployeeId = "MGR006",
        FullName = "Ejegayehu Dibaba",
        Email = "ejegayehu.dibaba@ethiopianairlines.com",
        PasswordHash = DefaultPasswordHash,
        Role = UserRole.Manager,
        SectionId = SchMntB787B767Hangar.Id,
        HangarId = null,
        ShopId = null,
        ReportsToUserId = Director.Id,
        IsActive = true,
        MustChangePassword = false,
        CreatedAt = SeedTimestamp,
        UpdatedAt = SeedTimestamp
    };

    public static readonly ApplicationUser ManagerSchMntB737Hangar = new()
    {
        Id = Guid.Parse("00000000-0000-0000-0000-000000000008"),
        EmployeeId = "MGR007",
        FullName = "Genzebe Dibaba",
        Email = "genzebe.dibaba@ethiopianairlines.com",
        PasswordHash = DefaultPasswordHash,
        Role = UserRole.Manager,
        SectionId = SchMntB737Hangar.Id,
        HangarId = null,
        ShopId = null,
        ReportsToUserId = Director.Id,
        IsActive = true,
        MustChangePassword = false,
        CreatedAt = SeedTimestamp,
        UpdatedAt = SeedTimestamp
    };

    public static readonly ApplicationUser ManagerTechnicalSupportBase = new()
    {
        Id = Guid.Parse("00000000-0000-0000-0000-000000000009"),
        EmployeeId = "MGR008",
        FullName = "Almaz Ayana",
        Email = "almaz.ayana@ethiopianairlines.com",
        PasswordHash = DefaultPasswordHash,
        Role = UserRole.Manager,
        SectionId = TechnicalSupportBase.Id,
        HangarId = null,
        ShopId = null,
        ReportsToUserId = Director.Id,
        IsActive = true,
        MustChangePassword = false,
        CreatedAt = SeedTimestamp,
        UpdatedAt = SeedTimestamp
    };

    public static readonly ApplicationUser ManagerSchMntQ400Hangar = new()
    {
        Id = Guid.Parse("00000000-0000-0000-0000-000000000010"),
        EmployeeId = "MGR009",
        FullName = "Feyisa Lilesa",
        Email = "feyisa.lilesa@ethiopianairlines.com",
        PasswordHash = DefaultPasswordHash,
        Role = UserRole.Manager,
        SectionId = SchMntQ400Hangar.Id,
        HangarId = null,
        ShopId = null,
        ReportsToUserId = Director.Id,
        IsActive = true,
        MustChangePassword = false,
        CreatedAt = SeedTimestamp,
        UpdatedAt = SeedTimestamp
    };

    public static void Seed(ModelBuilder modelBuilder)
    {
        // NOTE: All organizational data (Workspace, Sections, Director, Managers, Hangars, Shops, Team Leaders, Employees)
        // is seeded via SQL migration in 20260530080338_SeedOrganizationalData.cs
        // This class only holds the data definitions for reference
    }

    private static void SeedTeamLeadersAndEmployees(ModelBuilder modelBuilder)
    {
        var teamLeaders = new List<ApplicationUser>();
        var employees = new List<ApplicationUser>();
        int employeeCounter = 100;
        int tlGuidCounter = 1;
        int empGuidCounter = 1;

        // Helper to create team leaders and employees
        void CreateTeamLeaderWithEmployees(
            Guid sectionId,
            Guid? hangarId,
            Guid? shopId,
            Guid managerId,
            string tlPrefix,
            string empPrefix,
            string[] tlNames,
            string[][] empNames)
        {
            for (int i = 0; i < tlNames.Length; i++)
            {
                var tlId = Guid.Parse($"30000000-0000-0000-0000-{tlGuidCounter++:D12}");
                var tl = new ApplicationUser
                {
                    Id = tlId,
                    EmployeeId = $"TL{employeeCounter++}",
                    FullName = tlNames[i],
                    Email = $"{tlNames[i].ToLower().Replace(" ", ".")}@ethiopianairlines.com",
                    PasswordHash = DefaultPasswordHash,
                    Role = UserRole.TeamLeader,
                    SectionId = sectionId,
                    HangarId = hangarId,
                    ShopId = shopId,
                    ReportsToUserId = managerId,
                    IsActive = true,
                    MustChangePassword = false,
                    CreatedAt = SeedTimestamp,
                    UpdatedAt = SeedTimestamp
                };
                teamLeaders.Add(tl);

                // Create 4 employees for each team leader
                for (int j = 0; j < empNames[i].Length; j++)
                {
                    var empId = Guid.Parse($"40000000-0000-0000-0000-{empGuidCounter++:D12}");
                    var emp = new ApplicationUser
                    {
                        Id = empId,
                        EmployeeId = $"EMP{employeeCounter++}",
                        FullName = empNames[i][j],
                        Email = $"{empNames[i][j].ToLower().Replace(" ", ".")}@ethiopianairlines.com",
                        PasswordHash = DefaultPasswordHash,
                        Role = UserRole.Employee,
                        SectionId = sectionId,
                        HangarId = hangarId,
                        ShopId = shopId,
                        ReportsToUserId = tlId,
                        IsActive = true,
                        MustChangePassword = false,
                        CreatedAt = SeedTimestamp,
                        UpdatedAt = SeedTimestamp
                    };
                    employees.Add(emp);
                }
            }
        }

        // Aircraft Cabin Maintenance - Q400 Hangar
        CreateTeamLeaderWithEmployees(
            AircraftCabinMaintenance.Id,
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            null,
            ManagerAircraftCabinMaintenance.Id,
            "ACM-Q4",
            "ACM-Q4",
            new[] { "Mulugeta Wendimu", "Tadesse Mekonnen" },
            new[] {
                new[] { "Dawit Abebe", "Kaleb Tesfaye", "Yohannes Haile", "Michael Tamiru" },
                new[] { "Samuel Girma", "Nathan Alemu", "Daniel Kifle", "David Assefa" }
            }
        );

        // Aircraft Cabin Maintenance - B777 Hangar
        CreateTeamLeaderWithEmployees(
            AircraftCabinMaintenance.Id,
            Guid.Parse("10000000-0000-0000-0000-000000000002"),
            null,
            ManagerAircraftCabinMaintenance.Id,
            "ACM-77",
            "ACM-77",
            new[] { "Belete Assefa", "Girma Wolde" },
            new[] {
                new[] { "Abel Tekle", "Biniam Tadesse", "Caleb Mekonnen", "Dawit Kasa" },
                new[] { "Elias Bekele", "Fikadu Tefera", "Gediyon Asfaw", "Henok Tsegaye" }
            }
        );

        // Aircraft Cabin Maintenance - B787 Hangar
        CreateTeamLeaderWithEmployees(
            AircraftCabinMaintenance.Id,
            Guid.Parse("10000000-0000-0000-0000-000000000003"),
            null,
            ManagerAircraftCabinMaintenance.Id,
            "ACM-78",
            "ACM-78",
            new[] { "Kassahun Abebe", "Mekonnen Tadesse" },
            new[] {
                new[] { "Isaac Yohannes", "Jeremiah Kifle", "Kidanemariam Abebe", "Leul Mekonnen" },
                new[] { "Mikael Assefa", "Nahom Bekele", "Oscar Tadesse", "Paulos Haile" }
            }
        );

        // Aircraft Cabin Maintenance - B737 Hangar
        CreateTeamLeaderWithEmployees(
            AircraftCabinMaintenance.Id,
            Guid.Parse("10000000-0000-0000-0000-000000000004"),
            null,
            ManagerAircraftCabinMaintenance.Id,
            "ACM-73",
            "ACM-73",
            new[] { "Yosef Tekle", "Zewdu Kifle" },
            new[] {
                new[] { "Quincy Alemu", "Raphael Tesfaye", "Solomon Asfaw", "Theodore Bekele" },
                new[] { "Urial Mekonnen", "Victor Tadesse", "Wolde Haile", "Xavier Kasa" }
            }
        );

        // Aircraft Cabin Maintenance - Cabin Shop
        CreateTeamLeaderWithEmployees(
            AircraftCabinMaintenance.Id,
            null,
            Guid.Parse("20000000-0000-0000-0000-000000000001"),
            ManagerAircraftCabinMaintenance.Id,
            "ACM-CS",
            "ACM-CS",
            new[] { "Alemayehu Girma", "Birhanu Tesfaye" },
            new[] {
                new[] { "Yonas Abebe", "Zerihun Mekonnen", "Amha Kifle", "Bereket Assefa" },
                new[] { "Chernet Bekele", "Demeke Tadesse", "Ephrem Haile", "Fasil Alemu" }
            }
        );

        // Aircraft Structure Maintenance - Q400 Hangar
        CreateTeamLeaderWithEmployees(
            AircraftStructureMaintenance.Id,
            Guid.Parse("10000000-0000-0000-0000-000000000005"),
            null,
            ManagerAircraftStructureMaintenance.Id,
            "ASM-Q4",
            "ASM-Q4",
            new[] { "Gashaw Mekonnen", "Hailu Bekele" },
            new[] {
                new[] { "Israel Tesfaye", "Jemal Kifle", "Kiros Assefa", "Lemma Haile" },
                new[] { "Mammo Alemu", "Nigusu Tadesse", "Oqubay Mekonnen", "Petros Bekele" }
            }
        );

        // Aircraft Structure Maintenance - B777 Hangar
        CreateTeamLeaderWithEmployees(
            AircraftStructureMaintenance.Id,
            Guid.Parse("10000000-0000-0000-0000-000000000006"),
            null,
            ManagerAircraftStructureMaintenance.Id,
            "ASM-77",
            "ASM-77",
            new[] { "Tewodros Kassa", "Wondwosen Abebe" },
            new[] {
                new[] { "Robel Tesfaye", "Sisay Kifle", "Tadesse Assefa", "Worku Haile" },
                new[] { "Yidnekachew Alemu", "Zelalem Tadesse", "Abiy Mekonnen", "Belay Bekele" }
            }
        );

        // Aircraft Structure Maintenance - B787 Hangar
        CreateTeamLeaderWithEmployees(
            AircraftStructureMaintenance.Id,
            Guid.Parse("10000000-0000-0000-0000-000000000007"),
            null,
            ManagerAircraftStructureMaintenance.Id,
            "ASM-78",
            "ASM-78",
            new[] { "Adanech Abebe", "Birtukan Mekonnen" },
            new[] {
                new[] { "Caleb Kifle", "Dawit Assefa", "Ephrem Haile", "Fikir Alemu" },
                new[] { "Gedion Tadesse", "Henok Mekonnen", "Isaac Bekele", "Jeremiah Kasa" }
            }
        );

        // Aircraft Structure Maintenance - B737 Hangar
        CreateTeamLeaderWithEmployees(
            AircraftStructureMaintenance.Id,
            Guid.Parse("10000000-0000-0000-0000-000000000008"),
            null,
            ManagerAircraftStructureMaintenance.Id,
            "ASM-73",
            "ASM-73",
            new[] { "Kefyalew Tesfaye", "Legesse Kifle" },
            new[] {
                new[] { "Mikias Assefa", "Nahom Haile", "Oqubay Alemu", "Petros Tadesse" },
                new[] { "Quincy Mekonnen", "Raphael Bekele", "Solomon Kasa", "Theodore Tesfaye" }
            }
        );

        // Aircraft Structure Maintenance - Composite Shop
        CreateTeamLeaderWithEmployees(
            AircraftStructureMaintenance.Id,
            null,
            Guid.Parse("20000000-0000-0000-0000-000000000002"),
            ManagerAircraftStructureMaintenance.Id,
            "ASM-CO",
            "ASM-CO",
            new[] { "Tilahun Abebe", "Yohannes Mekonnen" },
            new[] {
                new[] { "Urial Kifle", "Victor Assefa", "Wolde Haile", "Xavier Alemu" },
                new[] { "Yidnekachew Tadesse", "Zelalem Mekonnen", "Abiy Bekele", "Belay Kasa" }
            }
        );

        // Aircraft Structure Maintenance - Sheet Metal Shop
        CreateTeamLeaderWithEmployees(
            AircraftStructureMaintenance.Id,
            null,
            Guid.Parse("20000000-0000-0000-0000-000000000003"),
            ManagerAircraftStructureMaintenance.Id,
            "ASM-SM",
            "ASM-SM",
            new[] { "Abebech Tesfaye", "Kidan Kifle" },
            new[] {
                new[] { "Chernet Assefa", "Demeke Haile", "Ephrem Alemu", "Fasil Tadesse" },
                new[] { "Gashaw Mekonnen", "Hailu Bekele", "Israel Kasa", "Jemal Tesfaye" }
            }
        );

        // Aircraft Avionics Systems - Q400 Hangar
        CreateTeamLeaderWithEmployees(
            AircraftAvionicsSystems.Id,
            Guid.Parse("10000000-0000-0000-0000-000000000009"),
            null,
            ManagerAircraftAvionicsSystems.Id,
            "AAS-Q4",
            "AAS-Q4",
            new[] { "Kiros Assefa", "Lemma Haile" },
            new[] {
                new[] { "Mammo Alemu", "Nigusu Tadesse", "Oqubay Mekonnen", "Petros Bekele" },
                new[] { "Robel Tesfaye", "Sisay Kifle", "Tadesse Assefa", "Worku Haile" }
            }
        );

        // Paint Services - Paint Hangar
        CreateTeamLeaderWithEmployees(
            PaintServices.Id,
            Guid.Parse("10000000-0000-0000-0000-000000000010"),
            null,
            ManagerPaintServices.Id,
            "PS-PN",
            "PS-PN",
            new[] { "Yidnekachew Alemu", "Zelalem Tadesse" },
            new[] {
                new[] { "Abiy Mekonnen", "Belay Bekele", "Caleb Kifle", "Dawit Assefa" },
                new[] { "Ephrem Haile", "Fikir Alemu", "Gedion Tadesse", "Henok Mekonnen" }
            }
        );

        // Sch Mnt B777/A350 - B777 Hangar
        CreateTeamLeaderWithEmployees(
            SchMntB777A350Hangar.Id,
            Guid.Parse("10000000-0000-0000-0000-000000000011"),
            null,
            ManagerSchMntB777A350Hangar.Id,
            "SM77-7",
            "SM77-7",
            new[] { "Isaac Bekele", "Jeremiah Kasa" },
            new[] {
                new[] { "Kefyalew Tesfaye2", "Legesse Kifle2", "Mikias Assefa2", "Nahom Haile2" },
                new[] { "Oqubay Alemu2", "Petros Tadesse2", "Quincy Mekonnen2", "Raphael Bekele2" }
            }
        );

        // Sch Mnt B787/B767 - B787 Hangar
        CreateTeamLeaderWithEmployees(
            SchMntB787B767Hangar.Id,
            Guid.Parse("10000000-0000-0000-0000-000000000012"),
            null,
            ManagerSchMntB787B767Hangar.Id,
            "SM78-7",
            "SM78-7",
            new[] { "Solomon Kasa", "Theodore Tesfaye" },
            new[] {
                new[] { "Urial Kifle2", "Victor Assefa2", "Wolde Haile2", "Xavier Alemu2" },
                new[] { "Yidnekachew Tadesse2", "Zelalem Mekonnen2", "Abiy Bekele2", "Belay Kasa2" }
            }
        );

        // Sch Mnt B737 - B737 Hangar
        CreateTeamLeaderWithEmployees(
            SchMntB737Hangar.Id,
            Guid.Parse("10000000-0000-0000-0000-000000000013"),
            null,
            ManagerSchMntB737Hangar.Id,
            "SM73-7",
            "SM73-7",
            new[] { "Chernet Assefa2", "Demeke Haile2" },
            new[] {
                new[] { "Ephrem Alemu2", "Fasil Tadesse2", "Gashaw Mekonnen2", "Hailu Bekele2" },
                new[] { "Israel Kasa2", "Jemal Tesfaye2", "Kiros Assefa2", "Lemma Haile2" }
            }
        );

        // Sch Mnt Q400 - Q400 Hangar
        CreateTeamLeaderWithEmployees(
            SchMntQ400Hangar.Id,
            Guid.Parse("10000000-0000-0000-0000-000000000014"),
            null,
            ManagerSchMntQ400Hangar.Id,
            "SMQ4-Q",
            "SMQ4-Q",
            new[] { "Mammo Alemu2", "Nigusu Tadesse2" },
            new[] {
                new[] { "Oqubay Mekonnen2", "Petros Bekele2", "Robel Tesfaye2", "Sisay Kifle2" },
                new[] { "Tadesse Assefa2", "Worku Haile2", "Yidnekachew Alemu2", "Zelalem Tadesse2" }
            }
        );

        // Technical Support-Base - Q400 Hangar
        CreateTeamLeaderWithEmployees(
            TechnicalSupportBase.Id,
            Guid.Parse("10000000-0000-0000-0000-000000000015"),
            null,
            ManagerTechnicalSupportBase.Id,
            "TSB-Q4",
            "TSB-Q4",
            new[] { "Abiy Mekonnen3", "Belay Bekele3" },
            new[] {
                new[] { "Caleb Kifle2", "Dawit Assefa2", "Ephrem Haile2", "Fikir Alemu2" },
                new[] { "Gedion Tadesse2", "Henok Mekonnen2", "Isaac Bekele2", "Jeremiah Kasa2" }
            }
        );

        // Technical Support-Base - B777 Hangar
        CreateTeamLeaderWithEmployees(
            TechnicalSupportBase.Id,
            Guid.Parse("10000000-0000-0000-0000-000000000016"),
            null,
            ManagerTechnicalSupportBase.Id,
            "TSB-77",
            "TSB-77",
            new[] { "Kefyalew Tesfaye3", "Legesse Kifle3" },
            new[] {
                new[] { "Mikias Assefa3", "Nahom Haile3", "Oqubay Alemu3", "Petros Tadesse3" },
                new[] { "Quincy Mekonnen3", "Raphael Bekele3", "Solomon Kasa2", "Theodore Tesfaye2" }
            }
        );

        // Technical Support-Base - B787 Hangar
        CreateTeamLeaderWithEmployees(
            TechnicalSupportBase.Id,
            Guid.Parse("10000000-0000-0000-0000-000000000017"),
            null,
            ManagerTechnicalSupportBase.Id,
            "TSB-78",
            "TSB-78",
            new[] { "Urial Kifle3", "Victor Assefa3" },
            new[] {
                new[] { "Wolde Haile3", "Xavier Alemu3", "Yidnekachew Tadesse3", "Zelalem Mekonnen3" },
                new[] { "Abiy Bekele3", "Belay Kasa3", "Chernet Assefa3", "Demeke Haile3" }
            }
        );

        // Technical Support-Base - Paint Hangar
        CreateTeamLeaderWithEmployees(
            TechnicalSupportBase.Id,
            Guid.Parse("10000000-0000-0000-0000-000000000018"),
            null,
            ManagerTechnicalSupportBase.Id,
            "TSB-PN",
            "TSB-PN",
            new[] { "Ephrem Alemu3", "Fasil Tadesse3" },
            new[] {
                new[] { "Gashaw Mekonnen3", "Hailu Bekele3", "Israel Kasa3", "Jemal Tesfaye3" },
                new[] { "Kiros Assefa3", "Lemma Haile3", "Mammo Alemu3", "Nigusu Tadesse3" }
            }
        );

        // Technical Support-Base - B737 Hangar
        CreateTeamLeaderWithEmployees(
            TechnicalSupportBase.Id,
            Guid.Parse("10000000-0000-0000-0000-000000000019"),
            null,
            ManagerTechnicalSupportBase.Id,
            "TSB-73",
            "TSB-73",
            new[] { "Oqubay Mekonnen3", "Petros Bekele3" },
            new[] {
                new[] { "Robel Tesfaye3", "Sisay Kifle3", "Tadesse Assefa3", "Worku Haile3" },
                new[] { "Yidnekachew Alemu3", "Zelalem Tadesse3", "Abiy Mekonnen4", "Belay Bekele4" }
            }
        );

        modelBuilder.Entity<ApplicationUser>().HasData(teamLeaders);
        modelBuilder.Entity<ApplicationUser>().HasData(employees);
    }

    private static void SeedSpecialTestUsers(ModelBuilder modelBuilder)
    {
        // Unassigned Team Leader
        var unassignedTeamLeader = new ApplicationUser
        {
            Id = Guid.Parse("99990000-0000-0000-0000-000000000001"),
            EmployeeId = "TL999",
            FullName = "Test Unassigned Team Leader",
            Email = "test.unassigned.tl@ethiopianairlines.com",
            PasswordHash = DefaultPasswordHash,
            Role = UserRole.TeamLeader,
            SectionId = AircraftCabinMaintenance.Id,
            HangarId = null,
            ShopId = null,
            ReportsToUserId = ManagerAircraftCabinMaintenance.Id,
            IsActive = true,
            MustChangePassword = false,
            CreatedAt = SeedTimestamp,
            UpdatedAt = SeedTimestamp
        };

        // Unassigned Employee
        var unassignedEmployee = new ApplicationUser
        {
            Id = Guid.Parse("99990000-0000-0000-0000-000000000002"),
            EmployeeId = "EMP999",
            FullName = "Test Unassigned Employee",
            Email = "test.unassigned.emp@ethiopianairlines.com",
            PasswordHash = DefaultPasswordHash,
            Role = UserRole.Employee,
            SectionId = AircraftCabinMaintenance.Id,
            HangarId = null,
            ShopId = null,
            ReportsToUserId = unassignedTeamLeader.Id,
            IsActive = true,
            MustChangePassword = false,
            CreatedAt = SeedTimestamp,
            UpdatedAt = SeedTimestamp
        };

        modelBuilder.Entity<ApplicationUser>().HasData(unassignedTeamLeader, unassignedEmployee);
    }
}
