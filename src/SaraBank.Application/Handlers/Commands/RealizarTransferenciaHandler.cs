using FluentValidation;
using FluentValidation.Results;
using MediatR;
using SaraBank.Application.Commands;
using SaraBank.Application.Events;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Entities;
using SaraBank.Domain.Interfaces;
using System.Text.Json;

namespace SaraBank.Application.Handlers.Commands;

public class RealizarTransferenciaHandler : IRequestHandler<RealizarTransferenciaCommand, bool>
{
    private readonly IContaRepository _contaRepository;
    private readonly IMovimentacaoRepository _movimentacaoRepository;
    private readonly IUnitOfWork _uow;
    private readonly IOutboxRepository _outboxRepository;

    public RealizarTransferenciaHandler(
        IContaRepository contaRepository,
        IMovimentacaoRepository movimentacaoRepository,
        IUnitOfWork uow,
        IOutboxRepository outboxRepository)
    {
        _contaRepository = contaRepository;
        _movimentacaoRepository = movimentacaoRepository;
        _uow = uow;
        _outboxRepository = outboxRepository;
    }

    public async Task<bool> Handle(RealizarTransferenciaCommand request, CancellationToken ct)
    {
        var origem = await _contaRepository.ObterPorIdAsync(request.ContaOrigemId);
        var destino = await _contaRepository.ObterPorIdAsync(request.ContaDestinoId);

        if (origem == null || destino == null)
        {
            throw new ValidationException(new[] {
                new ValidationFailure("Contas", "Uma ou ambas as contas não foram encontradas.")
            });
        }

        return await _uow.ExecutarAsync(async () =>
        {
            var sagaId = Guid.NewGuid(); 

            var evento = new TransferenciaIniciadaEvent(
                sagaId,
                request.ContaOrigemId,
                request.ContaDestinoId,
                request.Valor
            );

            var envelope = new
            {
                TipoEvento = "TransferenciaIniciada",
                SagaId = sagaId,
                Payload = JsonSerializer.Serialize(evento)
            };

            var outboxMessage = new OutboxMessage(
                Guid.NewGuid(),
                JsonSerializer.Serialize(envelope),
                "TransferenciaIniciada",
                "sara-bank-transferencias-iniciadas"
            );

            await _outboxRepository.AdicionarAsync(outboxMessage, ct);

            return true;
        });
    }
}