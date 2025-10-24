using FluentValidation;
using Tasky.Application.DTOs;

namespace Tasky.Application.Validation;

public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MinimumLength(1).MaximumLength(200);
    }
}
public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MinimumLength(1).MaximumLength(200);
    }
}
