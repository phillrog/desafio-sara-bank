using FluentValidation;
using MediatR;
using SaraBank.Application.Commands;
using SaraBank.Application.Events;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Interfaces;
using System.Text.Json;

namespace SaraBank.Application.Handlers.Commands;

public class CriarMovimentacaoHandler : IRequestHandler<CriarMovimentacaoCommand, bool>
{
    private readonly IContaRepository _contaRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _uow;
    private readonly IValidator<CriarMovimentacaoCommand> _validator;

    public CriarMovimentacaoHandler(
        IContaRepository contaRepository,
        IOutboxRepository outboxRepository,
        IUnitOfWork uow,
        IValidator<CriarMovimentacaoCommand> validator)
    {
        _contaRepository = contaRepository;
        _outboxRepository = outboxRepository;
        _uow = uow;
        _validator = validator;
    }

    public async Task<bool> Handle(CriarMovimentacaoCommand request, CancellationToken ct)
    {
        var validationResult = await _validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);

        return await _uow.ExecutarAsync(async () =>
        {
            // Busca a conta
            var conta = await _contaRepository.ObterPorIdAsync(request.ContaId);
            if (conta == null) return false;

            // Valida operação
            if (request.Tipo.Equals("Deposito", StringComparison.OrdinalIgnoreCase))
                conta.Creditar(request.Valor);
            else
                conta.Debitar(request.Valor);

            await _contaRepository.AtualizarAsync(conta);

            // Gerar o Evento para o Outbox
            var evento = new MovimentacaoRealizadaEvent(
                contaId: request.ContaId,
                valor: request.Valor,
                tipo: request.Tipo
            );            

            var outboxMessage = new OutboxMessageDTO(
                Id: Guid.NewGuid().ToString(),
                Payload: JsonSerializer.Serialize(evento),
                Tipo: nameof(MovimentacaoRealizadaEvent)
            );

            // Salva o evento na mesma transação do saldo
            await _outboxRepository.AdicionarAsync(outboxMessage, ct);

            return true;
        });
    }
}