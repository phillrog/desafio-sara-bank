using Google.Cloud.PubSub.V1;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SaraBank.Infrastructure.Workers;

public class MovimentacaoConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SubscriberClient _subscriberClient;
    private readonly ILogger<MovimentacaoConsumerService> _logger;

    public MovimentacaoConsumerService(
        IServiceProvider serviceProvider,
        SubscriberClient subscriberClient,
        ILogger<MovimentacaoConsumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _subscriberClient = subscriberClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(" [START] Aguardando mensagens de movimentação...");

        await _subscriberClient.StartAsync(async (PubsubMessage message, CancellationToken ct) =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null
                };
                var messageBody = message.Data.ToStringUtf8();
                _logger.LogInformation($" [RECEBIDO] Payload: {messageBody}");

                var envelope = JsonSerializer.Deserialize<JsonElement>(message.Data.ToStringUtf8(), options);

                string tipo = envelope.GetProperty("TipoEvento").GetString();
                string payload = envelope.GetProperty("Payload").GetString();

                if (tipo == "NovaMovimentacao") 
                {
                    var evento = JsonSerializer.Deserialize<NovaMovimentacaoEvent>(payload);
                    await mediator.Publish(evento, ct);
                    // Isso vai disparar o GravarMovimentacaoNoBancoHandler que chama o Command!
                }

                return SubscriberClient.Reply.Ack;                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " [ERRO] Falha ao processar movimentação. A mensagem voltará para a fila.");
                return SubscriberClient.Reply.Nack;
            }
        });
    }
}