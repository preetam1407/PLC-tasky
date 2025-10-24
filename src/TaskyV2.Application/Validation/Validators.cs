using FluentValidation;
using TaskyV2.Application.DTOs;

namespace TaskyV2.Application.Validation;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
public class ProjectCreateRequestValidator : AbstractValidator<ProjectCreateRequest>
{
    public ProjectCreateRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MinimumLength(3).MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
public class ProjectUpdateRequestValidator : AbstractValidator<ProjectUpdateRequest>
{
    public ProjectUpdateRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MinimumLength(3).MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
public class TaskCreateRequestValidator : AbstractValidator<TaskCreateRequest>
{
    public TaskCreateRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
    }
}
public class TaskUpdateRequestValidator : AbstractValidator<TaskUpdateRequest>
{
    public TaskUpdateRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
    }
}
