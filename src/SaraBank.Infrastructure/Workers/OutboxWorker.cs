using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Retry;
using SaraBank.Application.Interfaces;

namespace SaraBank.Infrastructure.Workers;

public class OutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPublisher _publisher;
    private readonly AsyncRetryPolicy _retryPolicy;

    public OutboxWorker(IServiceScopeFactory scopeFactory, IPublisher publisher)
    {
        _scopeFactory = scopeFactory;
        _publisher = publisher;

        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Escopo temporário para usar os serviços Scoped
                using (var scope = _scopeFactory.CreateScope())
                {
                    var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
                    await ProcessarEventosOutbox(repository, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro crítico no Worker: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
    
    public async Task ProcessarEventosOutbox(IOutboxRepository repository, CancellationToken ct)
    {
        var mensagens = await repository.ObterNaoProcessadosAsync(10, ct);

        foreach (var msg in mensagens)
        {
            try
            {
                await _retryPolicy.ExecuteAsync(async () => {
                    await _publisher.PublicarAsync(msg.Payload);
                });

                await repository.MarcarComoProcessadoAsync(msg.Id, ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Evento {msg.Id} falhou: {ex.Message}");
            }
        }
    }
}