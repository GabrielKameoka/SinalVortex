using FluentValidation;
using SinalVortex.Application.Commands.Autenticacao;

namespace SinalVortex.Application.Validators;

public sealed class RegistrarTenantCommandValidator : AbstractValidator<RegistrarTenantCommand>
{
    public RegistrarTenantCommandValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Senha).NotEmpty().MinimumLength(12).MaximumLength(128);
    }
}

public sealed class AutenticarCommandValidator : AbstractValidator<AutenticarCommand>
{
    public AutenticarCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Senha).NotEmpty();
    }
}
