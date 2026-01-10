using FluentValidation;
using FluentValidation.Results;
using MediatR;
using SaraBank.Application.Commands;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Entities;
using SaraBank.Domain.Interfaces;
using System.Text.Json;

namespace SaraBank.Application.Handlers.Commands;

public class SolicitarMovimentacaoHandler : IRequestHandler<SolicitarMovimentacaoCommand, bool>
{
    private readonly IUnitOfWork _uow;
    private readonly IContaRepository _contaRepository;
    private readonly IOutboxRepository _outboxRepository;

    public SolicitarMovimentacaoHandler(IUnitOfWork uow, 
        IContaRepository contaRepository,
        IOutboxRepository outboxRepository)
    {
        _uow = uow;
        _contaRepository = contaRepository;
        _outboxRepository = outboxRepository;
    }

    public async Task<bool> Handle(SolicitarMovimentacaoCommand request, CancellationToken ct)
    {
        #region [ VALIDAÇÕES ]
        
        var conta = await _contaRepository.ObterPorIdAsync(request.ContaId);
        if (conta == null)
        {
            throw new ValidationException(new[] {
                new ValidationFailure("ContaId", "A conta informada não foi encontrada no SARA Bank.")
            });
        }

        #endregion

        return await _uow.ExecutarAsync(async () =>
        {
            var eventoIntegracao = new NovaMovimentacaoEvent(
                request.ContaId,
                request.Valor,
                request.Tipo,
                "Solicitação via API"
            );

            var envelope = new
            {
                TipoEvento = "NovaMovimentacao",
                Payload = JsonSerializer.Serialize(eventoIntegracao)
            };

            var outboxMessage = new OutboxMessage(
                Guid.NewGuid(),
                JsonSerializer.Serialize(envelope),
                "NovaMovimentacao",
                "sara-bank-movimentacoes"
            );

            await _outboxRepository.AdicionarAsync(outboxMessage, ct);

            return true;
        });
    }
}