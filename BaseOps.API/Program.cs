using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BaseOps.API;
using BaseOps.API.Converters;
using BaseOps.API.Middleware;
using BaseOps.API.Services;
using BaseOps.Application;
using BaseOps.Infrastructure;
using BaseOps.Infrastructure.Authentication;
using BaseOps.Infrastructure.Handovers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration).WriteTo.Console());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        // Configure DateTime to use UTC timezone
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IWorkflowValidator, WorkflowValidator>();
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:8080", "http://127.0.0.1:8080"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtOptions.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ProductionPlannerAccess", policy => policy.RequireClaim("hasProductionPlannerAccess", "true"));
    
    // Annual Leave Management Authorization Policies
    options.AddPolicy("AnnualLeaveSubmit", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("AnnualLeaveViewOwn", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("AnnualLeaveViewTeam", policy => policy.RequireRole("TeamLeader", "Manager", "Director"));
    options.AddPolicy("AnnualLeaveViewSection", policy => policy.RequireRole("Manager", "Director"));
    options.AddPolicy("AnnualLeaveViewAll", policy => policy.RequireRole("Director"));
    options.AddPolicy("AnnualLeaveGenerate", policy => policy.RequireRole("TeamLeader", "Manager", "Director"));
    options.AddPolicy("AnnualLeaveAdjust", policy => policy.RequireRole("TeamLeader", "Manager", "Director"));
    options.AddPolicy("AnnualLeaveFinalize", policy => policy.RequireRole("TeamLeader", "Manager", "Director"));
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("api", limiter =>
    {
        limiter.PermitLimit = 200;
        limiter.Window = TimeSpan.FromMinutes(1);
    });
    options.AddFixedWindowLimiter("HandoverCreation", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
    });
});

builder.Services.AddHostedService<HandoverAlertBackgroundService>();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    await DevelopmentDataSeeder.SeedAsync(app.Services);
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers().RequireRateLimiting("api");
app.Run();



