using FluentValidation;
using SinalVortex.Application.Commands.Contatos;
using SinalVortex.Application.Commands.Templates;

namespace SinalVortex.Application.Validators;

public sealed class AtualizarContatoValidator : AbstractValidator<AtualizarContatoCommand>
{
    public AtualizarContatoValidator() { RuleFor(x => x.Nome).NotEmpty().MaximumLength(200); RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email)); RuleFor(x => x.Telefone).MaximumLength(40); }
}
public sealed class CriarTemplateValidator : AbstractValidator<CriarTemplateCommand>
{
    public CriarTemplateValidator() { RuleFor(x => x.AplicacaoId).NotEmpty(); RuleFor(x => x.Chave).NotEmpty().MaximumLength(100); RuleFor(x => x.Conteudo).NotEmpty().MaximumLength(100_000); }
}
public sealed class AtualizarTemplateValidator : AbstractValidator<AtualizarTemplateCommand>
{
    public AtualizarTemplateValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Conteudo).NotEmpty().MaximumLength(100_000); }
}
