using FluentValidation;

namespace ToDoListAPI.Validators
{
    public class CreateItemRequestValidator : AbstractValidator<SharedLibreries.DTOs.CreateItemRequest>
    {
        public CreateItemRequestValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId is required");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(500).WithMessage("Title cannot exceed 500 characters");

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters");
        }
    }

    public class UpdateItemRequestValidator : AbstractValidator<SharedLibreries.DTOs.UpdateItemRequest>
    {
        public UpdateItemRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(500).WithMessage("Title cannot exceed 500 characters");

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters");
        }
    }
}
