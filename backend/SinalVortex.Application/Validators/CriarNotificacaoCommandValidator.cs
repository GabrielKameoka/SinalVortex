using FluentValidation;
using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Domain.Enums;

namespace SinalVortex.Application.Validators.Notificacoes;

public class CriarNotificacaoCommandValidator : AbstractValidator<CriarNotificacaoCommand>
{
    public CriarNotificacaoCommandValidator()
    {
        RuleFor(x => x.AplicacaoId)
            .NotEmpty().WithMessage("O AplicacaoId é obrigatório.");

        RuleFor(x => x.Destinatario)
            .NotEmpty().WithMessage("O destinatário não pode ser vazio.");

        RuleFor(x => x.Canal)
            .IsInEnum().WithMessage("Canal de notificação inválido.");

        RuleFor(x => x.Prioridade)
            .IsInEnum().WithMessage("Prioridade inválida.");

        RuleFor(x => x.Conteudo)
            .NotEmpty().WithMessage("O conteúdo da notificação não pode ser vazio.")
            .MaximumLength(4000).WithMessage("O conteúdo ultrapassa o limite permitido de 4000 caracteres.");

        RuleFor(x => x.Assunto)
            .MaximumLength(200).WithMessage("O assunto deve ter no máximo 200 caracteres.");

        // Regra condicional: E-mail exige assunto
        When(x => x.Canal == CanalNotificacao.Email, () =>
        {
            RuleFor(x => x.Assunto)
                .NotEmpty().WithMessage("O assunto é obrigatório para notificações do tipo E-mail.");
        });
    }
}