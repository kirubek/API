using BaseOps.Application.Bulletins.DTOs;
using BaseOps.Domain.Enums;
using FluentValidation;

namespace BaseOps.Application.Bulletins.Validators;

public sealed class UpdateBulletinDtoValidator : AbstractValidator<UpdateBulletinDto>
{
    public UpdateBulletinDtoValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters")
            .When(x => x.Title != null);

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Invalid category value")
            .When(x => x.Category.HasValue);

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority value")
            .When(x => x.Priority.HasValue);

        RuleFor(x => x.ExpiryDate)
            .Must(BeInFuture).WithMessage("Expiry date must be in the future")
            .When(x => x.ExpiryDate.HasValue);

        RuleFor(x => x.ExpiryDate)
            .Must(BeWithinOneYear).WithMessage("Expiry date must be within one year from now")
            .When(x => x.ExpiryDate.HasValue);
    }

    private bool BeInFuture(DateTime? expiryDate)
    {
        return expiryDate > DateTime.UtcNow;
    }

    private bool BeWithinOneYear(DateTime? expiryDate)
    {
        return expiryDate <= DateTime.UtcNow.AddYears(1);
    }
}
