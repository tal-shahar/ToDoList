using FluentValidation;

namespace ToDoListAPI.Validators
{
    public class CreateUserRequestValidator : AbstractValidator<SharedLibreries.DTOs.CreateUserRequest>
    {
        public CreateUserRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Email must be a valid email address")
                .MaximumLength(320).WithMessage("Email cannot exceed 320 characters");
        }
    }

    public class UpdateUserRequestValidator : AbstractValidator<SharedLibreries.DTOs.UpdateUserRequest>
    {
        public UpdateUserRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Email must be a valid email address")
                .MaximumLength(320).WithMessage("Email cannot exceed 320 characters");
        }
    }
}
