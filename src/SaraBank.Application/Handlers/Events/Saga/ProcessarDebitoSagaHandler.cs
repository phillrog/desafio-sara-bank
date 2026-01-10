using MediatR;
using Microsoft.Extensions.Logging;
using SaraBank.Application.Events;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Entities;
using SaraBank.Domain.Interfaces;
using System.Text.Json;

namespace SaraBank.Application.Handlers.Events;

public class ProcessarDebitoSagaHandler : INotificationHandler<TransferenciaIniciadaEvent>
{
    private readonly IUnitOfWork _uow;
    private readonly IContaRepository _contaRepository;
    private readonly IMovimentacaoRepository _movimentacaoRepository;
    private readonly ILogger<ProcessarDebitoSagaHandler> _logger;
    private readonly IOutboxRepository _outboxRepository;

    public ProcessarDebitoSagaHandler(
        IUnitOfWork uow,
        IContaRepository contaRepository,
        IMovimentacaoRepository movimentacaoRepository,
        ILogger<ProcessarDebitoSagaHandler> logger,
        IOutboxRepository outboxRepository)
    {
        _uow = uow;
        _contaRepository = contaRepository;
        _movimentacaoRepository = movimentacaoRepository;
        _logger = logger;
        _outboxRepository = outboxRepository;
    }

    public async Task Handle(TransferenciaIniciadaEvent notification, CancellationToken ct)
    {
        try
        {

            await _uow.ExecutarAsync(async () =>
            {
                var jaProcessado = await _movimentacaoRepository.ExisteMovimentacaoParaSagaAsync(notification.SagaId, "DEBITO");
                if (jaProcessado)
                {
                    _logger.LogInformation($" [SAGA-IDEMPOTENCY] {notification.SagaId}: Débito já realizado anteriormente.");
                    return;
                }

                var origem = await _contaRepository.ObterPorIdAsync(notification.ContaOrigemId);

                if (origem == null)
                {
                    _logger.LogError($"Conta {notification.ContaOrigemId} não encontrada.");
                    await CancelarSaga(notification.SagaId, notification.ContaOrigemId, "Conta de origem inexistente.", ct);
                    return;
                }

                // Validação de Segurança (Necessária devido ao delay da fila)
                if (origem.Saldo < notification.Valor)
                {
                    _logger.LogWarning($"[SAGA-CANCEL] {notification.SagaId}: Saldo insuficiente.");

                    await CancelarSaga(notification.SagaId, notification.ContaOrigemId, "Saldo insuficiente.", ct);
                    return;
                }

                origem.Sacar(notification.Valor);
                await _contaRepository.AtualizarAsync(origem);

                var mov = new Movimentacao(origem.Id, notification.Valor, "DEBITO", $"Transferência Saga: {notification.SagaId}", notification.SagaId);
                await _movimentacaoRepository.AdicionarAsync(mov);

                var proximoEvento = new SaldoDebitadoEvent(
                    notification.SagaId,
                    notification.ContaOrigemId,
                    notification.ContaDestinoId,
                    notification.Valor
                );

                var envelope = new
                {
                    TipoEvento = "SaldoDebitado",
                    SagaId = notification.SagaId,
                    Payload = JsonSerializer.Serialize(proximoEvento)
                };

                var outboxMessage = new OutboxMessage(
                    Guid.NewGuid(),
                    JsonSerializer.Serialize(envelope),
                    "SaldoDebitado",
                    "sara-bank-transferencias-debitadas"
                );

                await _outboxRepository.AdicionarAsync(outboxMessage, ct);

                return;
            });
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, $"[ERRO-TECNICO] Saga {notification.SagaId}: Falha na infraestrutura.");
            throw;
        }
    }

    private async Task CancelarSaga(Guid sagaId, Guid contaId, string motivo, CancellationToken ct)
    {
        _logger.LogWarning($"[SAGA-CANCEL] {sagaId}: {motivo}");

        var erroEvento = new TransferenciaCanceladaEvent(sagaId, contaId, motivo);
        var envelope = new
        {
            TipoEvento = "TransferenciaCancelada",
            SagaId = sagaId,
            Payload = JsonSerializer.Serialize(erroEvento)
        };
        
        var outboxMessage = new OutboxMessage(
                    Guid.NewGuid(),
                    JsonSerializer.Serialize(envelope),
                    "TransferenciaCancelada",
                    "sara-bank-transferencias-erros"
                );

        await _outboxRepository.AdicionarAsync(outboxMessage, ct);
    }
}