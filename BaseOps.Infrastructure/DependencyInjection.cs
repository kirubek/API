using BaseOps.Application.Interfaces;
using BaseOps.Application.Bulletins;
using BaseOps.Application.DailyStatusReport;
using BaseOps.Application.EmployeeProfiles;
using BaseOps.Application.SAFA;
using BaseOps.Infrastructure.Authentication;
using BaseOps.Infrastructure.Authorization;
using BaseOps.Infrastructure.Bulletins;
using BaseOps.Infrastructure.Data;
using BaseOps.Infrastructure.DailyStatusReport;
using BaseOps.Infrastructure.EmployeeProfiles;
using BaseOps.Infrastructure.SAFA;
using BaseOps.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BaseOps.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddDbContext<BaseOpsDbContext>(options => 
            options.UseSqlServer(configuration.GetConnectionString("BaseOpsConnection"), 
                sqlOptions => sqlOptions.CommandTimeout(30)));
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserScopeResolver, UserScopeResolver>();
        services.AddScoped<IRbacScopeValidator, RbacScopeValidator>();
        services.AddScoped<IHierarchyAccessService, HierarchyAccessService>();
        services.AddScoped<IAuditService, AuditService>();
        
        // Annual Leave Management Services
        services.AddScoped<ICompletenessValidator, CompletenessValidator>();
        services.AddScoped<IPriorityCalculator, PriorityCalculator>();
        services.AddScoped<IAllocationEngine, AllocationEngine>();
        services.AddScoped<IManpowerConstraintService, ManpowerConstraintService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        
        // Bulletin Management Services
        services.AddScoped<IBulletinService, BulletinService>();
        services.AddScoped<IAttachmentSecurityService, AttachmentSecurityService>();
        services.AddHostedService<BulletinExpiryBackgroundService>();
        
        // Employee Profile Services
        services.AddScoped<IEmployeeProfileService, EmployeeProfileService>();
        
        // SAFA Inspection Services
        services.AddScoped<ISafaService, SafaService>();
        services.AddScoped<ISafaExportService, SafaExportService>();
        
        // Daily Status Report Services
        services.AddScoped<IDailyStatusReportService, DailyStatusReportService>();
        
        services.AddHealthChecks().AddDbContextCheck<BaseOpsDbContext>("sqlserver");
        return services;
    }
}

