using BaseOps.Application.Bulletins.DTOs;
using BaseOps.Domain.Enums;
using FluentValidation;

namespace BaseOps.Application.Bulletins.Validators;

public sealed class CreateBulletinDtoValidator : AbstractValidator<CreateBulletinDto>
{
    public CreateBulletinDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Invalid category value");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority value");

        RuleFor(x => x.ExpiryDate)
            .NotEmpty().WithMessage("Expiry date is required")
            .Must(BeInFuture).WithMessage("Expiry date must be in the future")
            .Must(BeWithinOneYear).WithMessage("Expiry date must be within one year from now");
    }

    private bool BeInFuture(DateTime expiryDate)
    {
        return expiryDate > DateTime.UtcNow;
    }

    private bool BeWithinOneYear(DateTime expiryDate)
    {
        return expiryDate <= DateTime.UtcNow.AddYears(1);
    }
}
