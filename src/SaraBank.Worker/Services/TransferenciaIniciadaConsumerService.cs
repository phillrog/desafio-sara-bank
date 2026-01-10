using Google.Cloud.PubSub.V1;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaraBank.Application.Events;
using System.Text.Json;

namespace SaraBank.Infrastructure.Workers;

public class TransferenciaIniciadaConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SubscriberClient _subscriberClient;
    private readonly ILogger<TransferenciaIniciadaConsumerService> _logger;

    public TransferenciaIniciadaConsumerService(
        IServiceProvider serviceProvider,
        SubscriberClient subscriberClient,
        ILogger<TransferenciaIniciadaConsumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _subscriberClient = subscriberClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(" [START] Saga SARA Bank: Aguardando início de transferências...");

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

                if (tipo == "TransferenciaIniciada") 
                {
                    _logger.LogInformation($" [SAGA-START] {sagaId}: Processando início de transferência.");
                    
                    var evento = JsonSerializer.Deserialize<TransferenciaIniciadaEvent>(payload);
                    
                    // Publica para o Handler de Domínio que fará o débito
                    await mediator.Publish(evento, ct);
                }

                return SubscriberClient.Reply.Ack;                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " [ERRO-SAGA] Falha crítica no passo de início. Reenfileirando...");
                return SubscriberClient.Reply.Nack;
            }
        });
    }
}