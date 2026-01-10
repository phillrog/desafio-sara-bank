using MediatR;
using Microsoft.Extensions.Logging;
using SaraBank.Application.Events;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Entities;
using SaraBank.Domain.Interfaces;

namespace SaraBank.Application.Handlers.Events;

public class EstornarDebitoSagaHandler : INotificationHandler<FalhaNoCreditoEvent>
{
    private readonly IUnitOfWork _uow;
    private readonly IContaRepository _contaRepository;
    private readonly IMovimentacaoRepository _movimentacaoRepository;
    private readonly ILogger<EstornarDebitoSagaHandler> _logger;

    public EstornarDebitoSagaHandler(
        IUnitOfWork uow,
        IContaRepository contaRepository,
        IMovimentacaoRepository movimentacaoRepository,
        ILogger<EstornarDebitoSagaHandler> logger)
    {
        _uow = uow;
        _contaRepository = contaRepository;
        _movimentacaoRepository = movimentacaoRepository;
        _logger = logger;
    }

    public async Task Handle(FalhaNoCreditoEvent notification, CancellationToken ct)
    {
        _logger.LogWarning($" [SAGA-COMPENSATION] Iniciando estorno da Saga {notification.SagaId}. Motivo: {notification.Motivo}");

        try
        {
            await _uow.ExecutarAsync<bool>(async () =>
            {
                var jaEstornado = await _movimentacaoRepository.ExisteEstornoParaSagaAsync(notification.SagaId);
                if (jaEstornado)
                {
                    _logger.LogInformation($" [SAGA-IDEMPOTENCY] {notification.SagaId}: Estorno já realizado. Ignorando duplicidade.");
                    return false;
                }

                var origem = await _contaRepository.ObterPorIdAsync(notification.ContaOrigemId);

                if (origem == null)
                {
                    _logger.LogCritical($" [SAGA-CRITICAL] Conta de origem {notification.ContaOrigemId} não encontrada para estorno!");
                    return false;
                }

                // Devolver o dinheiro (Creditar o valor estornado)
                origem.Creditar(notification.Valor);
                await _contaRepository.AtualizarAsync(origem);

                // Registrar a movimentação de ESTORNO
                var mov = new Movimentacao(
                    origem.Id,
                    notification.Valor,
                    "ESTORNO",
                    $"Estorno de transferência: {notification.SagaId}. Motivo: {notification.Motivo}"
                );
                await _movimentacaoRepository.AdicionarAsync(mov);

                _logger.LogInformation($" [SAGA-ROLLBACK-OK] {notification.SagaId}: Dinheiro devolvido à conta {origem.Id}.");

                return true;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $" [ERRO-ROLLBACK] Falha ao estornar Saga {notification.SagaId}.");
            throw;
        }
    }
}