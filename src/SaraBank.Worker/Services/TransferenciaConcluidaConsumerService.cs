using Google.Cloud.PubSub.V1;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaraBank.Application.Events;
using System.Text.Json;

namespace SaraBank.Infrastructure.Workers;

public class TransferenciaConcluidaConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SubscriberClient _subscriberClient;
    private readonly ILogger<TransferenciaConcluidaConsumerService> _logger;

    public TransferenciaConcluidaConsumerService(
        IServiceProvider serviceProvider,
        SubscriberClient subscriberClient,
        ILogger<TransferenciaConcluidaConsumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _subscriberClient = subscriberClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(" [START] Saga SARA Bank: Monitorando conclusões de transferência.");

        await _subscriberClient.StartAsync(async (PubsubMessage message, CancellationToken ct) =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var options = new JsonSerializerOptions { PropertyNamingPolicy = null };
                var messageBody = message.Data.ToStringUtf8();

                var envelope = JsonSerializer.Deserialize<JsonElement>(messageBody, options);

                string tipo = envelope.GetProperty("TipoEvento").GetString();
                string payload = envelope.GetProperty("Payload").GetString();
                Guid sagaId = Guid.Parse(envelope.GetProperty("SagaId").GetString());

                if (tipo == "TransferenciaConcluida")
                {
                    _logger.LogInformation($" [SAGA-SUCCESS] {sagaId}: Transferência finalizada com sucesso em todos os nós.");

                    var evento = JsonSerializer.Deserialize<TransferenciaConcluidaEvent>(payload);

                    // Publica para um possível Handler de Notificação/Comprovante
                    await mediator.Publish(evento, ct);
                }

                return SubscriberClient.Reply.Ack;
            }
            catch (Exception ex)
            {
                // log ou notificação.
                _logger.LogError(ex, " [ERRO-AUDITORIA] Falha ao processar evento de conclusão. Reenfileirando...");
                return SubscriberClient.Reply.Nack;
            }
        });
    }
}