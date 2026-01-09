using MediatR;
using SaraBank.Application.Commands;
using SaraBank.Application.Events;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Entities;
using SaraBank.Domain.Interfaces;
using System.Text.Json;

namespace SaraBank.Application.Handlers.Commands;

public class CriarMovimentacaoHandler : IRequestHandler<CriarMovimentacaoCommand, bool>
{
    private readonly IContaRepository _contaRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _uow;
    private readonly IMovimentacaoRepository _movimentacaoRepository;
    private readonly IMediator _mediator;

    public CriarMovimentacaoHandler(
        IContaRepository contaRepository,
        IOutboxRepository outboxRepository,
        IUnitOfWork uow,
        IMovimentacaoRepository movimentacaoRepository,
        IMediator mediator)
    {
        _contaRepository = contaRepository;
        _outboxRepository = outboxRepository;
        _uow = uow;
        _movimentacaoRepository = movimentacaoRepository;
        _mediator = mediator;
    }

    public async Task<bool> Handle(CriarMovimentacaoCommand request, CancellationToken ct)
    {
        return await _uow.ExecutarAsync(async () =>
        {
            // Busca a conta
            var conta = await _contaRepository.ObterPorIdAsync(request.ContaId);
            if (conta == null) return false;

            // Valida operação
            if (request.Tipo.Equals("Credito", StringComparison.OrdinalIgnoreCase))
                conta.Creditar(request.Valor);
            else
                conta.Debitar(request.Valor);

            await _contaRepository.AtualizarAsync(conta);

            // Criamos a entidade de domínio de movimentação
            var movimentacao = new Movimentacao(
                Guid.NewGuid(),
                request.ContaId,
                request.Valor,
                request.Tipo,
                "Processamento de movimentação",
                DateTime.UtcNow
            );
            
            await _movimentacaoRepository.AdicionarAsync(movimentacao);

            var evento = new MovimentacaoRealizadaEvent(
                contaId: request.ContaId,
                valor: request.Valor,
                tipo: request.Tipo
            );

            await _mediator.Publish(evento, ct);

            return true;
        });
    }
}