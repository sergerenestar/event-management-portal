using EventPortal.Api.Modules.Auth.Dtos;
using FluentValidation;

namespace EventPortal.Api.Modules.Auth.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    private static readonly HashSet<string> AllowedProviders =
        new(StringComparer.OrdinalIgnoreCase) { "Microsoft", "Google" };

    public LoginRequestValidator()
    {
        RuleFor(x => x.EntraIdToken)
            .NotEmpty()
            .WithMessage("EntraIdToken is required.");

        RuleFor(x => x.Provider)
            .Must(p => AllowedProviders.Contains(p))
            .WithMessage("Provider must be 'Microsoft' or 'Google'.");
    }
}
