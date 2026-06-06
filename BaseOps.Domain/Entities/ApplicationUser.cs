using BaseOps.Domain.Common;
using BaseOps.Domain.Enums;

namespace BaseOps.Domain.Entities;

public sealed class ApplicationUser : AuditableEntity
{
    public string? Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string EmployeeId { get; set; }
    public required string FullName { get; set; }
    public UserRole Role { get; set; }
    public Guid? SectionId { get; set; }
    public Guid? HangarId { get; set; }
    public Guid? ShopId { get; set; }
    public Guid? ReportsToUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; } = true;
    public DateTimeOffset? LastLoginAt { get; set; }
    public CompanyAuthorizationType? CompanyAuthorizationType { get; set; }
    public string? CompanyLicenses { get; set; }
    
    // Employee profile contact fields
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhoneNumber { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public string? Position { get; set; }
    
    // Concurrency token
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    
    public Section? Section { get; set; }
    public Hangar? Hangar { get; set; }
    public Shop? Shop { get; set; }
    public ApplicationUser? ReportsToUser { get; set; }
}
