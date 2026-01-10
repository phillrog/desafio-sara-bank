using MediatR;
using Microsoft.Extensions.Logging;
using SaraBank.Application.Events;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Entities;
using SaraBank.Domain.Interfaces;
using System.Text.Json;

namespace SaraBank.Application.Handlers.Events;

public class ProcessarCreditoSagaHandler : INotificationHandler<SaldoDebitadoEvent>
{
    private readonly IUnitOfWork _uow;
    private readonly IContaRepository _contaRepository;
    private readonly IMovimentacaoRepository _movimentacaoRepository;
    private readonly ILogger<ProcessarCreditoSagaHandler> _logger;

    public ProcessarCreditoSagaHandler(
        IUnitOfWork uow,
        IContaRepository contaRepository,
        IMovimentacaoRepository movimentacaoRepository,
        ILogger<ProcessarCreditoSagaHandler> logger)
    {
        _uow = uow;
        _contaRepository = contaRepository;
        _movimentacaoRepository = movimentacaoRepository;
        _logger = logger;
    }

    public async Task Handle(SaldoDebitadoEvent notification, CancellationToken ct)
    {
        try // try para não ficar tentando indefinidamente em caso de falha
        {
            await _uow.ExecutarAsync<bool>(async () =>
            {
                var jaProcessado = await _movimentacaoRepository.ExisteMovimentacaoParaSagaAsync(notification.SagaId, "CREDITO");
                if (jaProcessado)
                {
                    _logger.LogInformation($" [SAGA-IDEMPOTENCY] {notification.SagaId}: Crédito já realizado.");
                    return true;
                }

                var destino = await _contaRepository.ObterPorIdAsync(notification.ContaDestinoId);

                if (destino == null)
                    throw new InvalidOperationException($"Conta destino {notification.ContaDestinoId} não encontrada.");

                destino.Creditar(notification.Valor);
                await _contaRepository.AtualizarAsync(destino);

                //Faz a Movimentação
                var mov = new Movimentacao(
                    destino.Id,
                    notification.Valor,
                    "CREDITO",
                    $"Recebido via Saga (Ref: {notification.SagaId})",
                    notification.SagaId
                );
                await _movimentacaoRepository.AdicionarAsync(mov);

                // Notifica que a Saga acabou com sucesso
                var concluido = new TransferenciaConcluidaEvent(notification.SagaId, DateTime.UtcNow);
                var envelope = new
                {
                    TipoEvento = "TransferenciaConcluida",
                    SagaId = notification.SagaId,
                    Payload = JsonSerializer.Serialize(concluido)
                };

                await _uow.AdicionarAoOutboxAsync(JsonSerializer.Serialize(envelope), "TransferenciaConcluida");

                _logger.LogInformation($" [SAGA-SUCCESS] {notification.SagaId}: Crédito realizado.");
                return true;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($" [SAGA-FAILURE] {notification.SagaId}: Falha ao creditar. Erro: {ex.Message}");

            // Estorna 
            await IniciarCompensacao(notification);
        }
    }

    private async Task IniciarCompensacao(SaldoDebitadoEvent evt)
    {
        var falhaEnvelope = new
        {
            TipoEvento = "FalhaNoCredito",
            SagaId = evt.SagaId,
            Payload = JsonSerializer.Serialize(new
            {
                evt.SagaId,
                evt.ContaOrigemId,
                evt.Valor,
                Motivo = "Conta destino inválida ou inexistente"
            })
        };

        await _uow.AdicionarAoOutboxAsync(JsonSerializer.Serialize(falhaEnvelope), "FalhaNoCredito");
    }
}