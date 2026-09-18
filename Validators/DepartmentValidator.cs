using FluentValidation;
using StudentPortalAPI.DTOs;

namespace StudentPortalAPI.Validators;

public class DepartmentValidator : AbstractValidator<CreateDepartmentRequest>
{
    public DepartmentValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Department name is required")
            .MaximumLength(100).WithMessage("Department name must not exceed 100 characters");

        RuleFor(x => x.DurationYears)
            .InclusiveBetween(1, 10).WithMessage("Duration must be between 1 and 10 years");
    }
}
