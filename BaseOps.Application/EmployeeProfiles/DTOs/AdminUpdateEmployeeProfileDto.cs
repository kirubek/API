using FluentValidation;
using BaseOps.Domain.Enums;

namespace BaseOps.Application.EmployeeProfiles.DTOs;

public sealed class AdminUpdateEmployeeProfileDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public UserRole? Role { get; set; }
    public Guid? SectionId { get; set; }
    public Guid? HangarId { get; set; }
    public Guid? ShopId { get; set; }
    public Guid? ReportsToUserId { get; set; }
    public bool? IsActive { get; set; }
    public bool? MustChangePassword { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhoneNumber { get; set; }
    public string? ProfilePhotoUrl { get; set; }
}

public sealed class AdminUpdateEmployeeProfileDtoValidator : AbstractValidator<AdminUpdateEmployeeProfileDto>
{
    public AdminUpdateEmployeeProfileDtoValidator()
    {
        RuleFor(x => x.FullName)
            .MaximumLength(180).WithMessage("Full name cannot exceed 180 characters.")
            .NotEmpty().When(x => x.FullName != null)
            .WithMessage("Full name cannot be empty when provided.");

        RuleFor(x => x.Email)
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.")
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Email must be a valid email address.");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(50).WithMessage("Phone number cannot exceed 50 characters.")
            .Matches(@"^[\d\s\+\-\(\)]*$").When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("Phone number contains invalid characters.");

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Address cannot exceed 500 characters.");

        RuleFor(x => x.EmergencyContactName)
            .MaximumLength(180).WithMessage("Emergency contact name cannot exceed 180 characters.")
            .NotEmpty().When(x => !string.IsNullOrEmpty(x.EmergencyContactPhoneNumber))
            .WithMessage("Emergency contact name is required when emergency contact phone is provided.");

        RuleFor(x => x.EmergencyContactPhoneNumber)
            .MaximumLength(50).WithMessage("Emergency contact phone cannot exceed 50 characters.")
            .Matches(@"^[\d\s\+\-\(\)]*$").When(x => !string.IsNullOrEmpty(x.EmergencyContactPhoneNumber))
            .WithMessage("Emergency contact phone contains invalid characters.")
            .NotEmpty().When(x => !string.IsNullOrEmpty(x.EmergencyContactName))
            .WithMessage("Emergency contact phone is required when emergency contact name is provided.");

        RuleFor(x => x.ProfilePhotoUrl)
            .MaximumLength(500).WithMessage("Profile photo URL cannot exceed 500 characters.")
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _)).When(x => !string.IsNullOrEmpty(x.ProfilePhotoUrl))
            .WithMessage("Profile photo URL must be a valid URL.");
    }
}
