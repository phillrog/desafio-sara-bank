using Google.Cloud.PubSub.V1;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaraBank.Application.Events;
using System.Text.Json;

namespace SaraBank.Infrastructure.Workers;

public class SaldoDebitadoConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SubscriberClient _subscriberClient;
    private readonly ILogger<SaldoDebitadoConsumerService> _logger;

    public SaldoDebitadoConsumerService(
        IServiceProvider serviceProvider,
        SubscriberClient subscriberClient,
        ILogger<SaldoDebitadoConsumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _subscriberClient = subscriberClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(" [START] Saga SARA Bank: Aguardando saldos debitados para crédito...");

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

                if (tipo == "SaldoDebitado")
                {
                    _logger.LogInformation($" [SAGA-STEP-2] {sagaId}: Processando crédito no destino.");

                    var evento = JsonSerializer.Deserialize<SaldoDebitadoEvent>(payload);

                    // Publica para o ProcessarCreditoSagaHandler
                    await mediator.Publish(evento, ct);
                }

                return SubscriberClient.Reply.Ack;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " [ERRO-SAGA] Falha no passo de crédito. Reenfileirando...");
                return SubscriberClient.Reply.Nack;
            }
        });
    }
}