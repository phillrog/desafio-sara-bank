using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using SaraBank.Application.Interfaces;

namespace SaraBank.Infrastructure.Workers;

public class OutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPublisher _publisher;
    private readonly ILogger<OutboxWorker> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public OutboxWorker(
        IServiceScopeFactory scopeFactory,
        IPublisher publisher,
        ILogger<OutboxWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _publisher = publisher;
        _logger = logger;

        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                (ex, time, retryCount, context) =>
                {
                    _logger.LogWarning(ex, " [RETRY] Tentativa {Count} falhou. Aguardando {Time}s...", retryCount, time.TotalSeconds);
                });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(" [START] OutboxWorker iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
                    await ProcessarEventosOutbox(repository, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, " [ERRO CRÍTICO] Falha no ciclo de processamento do Outbox.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    public async Task ProcessarEventosOutbox(IOutboxRepository repository, CancellationToken ct)
    {
        var mensagens = await repository.ObterNaoProcessadosAsync(10, ct);

        if (mensagens.Any())
            _logger.LogInformation(" [OUTBOX] {Count} mensagens encontradas para processamento.", mensagens.Count());

        foreach (var msg in mensagens)
        {
            try
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    _logger.LogDebug(" [DESPACHANDO] Enviando evento {Id} do tipo {Tipo} para o Publisher.", msg.Id, msg.Tipo);

                    await _publisher.PublicarAsync(msg.Payload, msg.Tipo, ct);
                });

                await repository.MarcarComoProcessadoAsync(msg.Id, ct);

                _logger.LogInformation(" [SUCESSO] Evento {Id} ({Tipo}) processado e marcado como concluído.", msg.Id, msg.Tipo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " [FALHA DEFINITIVA] Não foi possível despachar o evento {Id} após retentativas.", msg.Id);
            }
        }
    }
}