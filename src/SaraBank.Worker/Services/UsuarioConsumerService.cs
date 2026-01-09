using Google.Cloud.PubSub.V1;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaraBank.Application.Events;
using System.Text.Json;

namespace SaraBank.Infrastructure.Workers;

public class UsuarioConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SubscriberClient _subscriberClient;
    private readonly ILogger<UsuarioConsumerService> _logger;

    public UsuarioConsumerService(
        IServiceProvider serviceProvider,
        SubscriberClient subscriberClient,
        ILogger<UsuarioConsumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _subscriberClient = subscriberClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(" [START] Aguardando mensagens de cadastro de usuários...");

        await _subscriberClient.StartAsync(async (PubsubMessage message, CancellationToken ct) =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                string rawJson = message.Data.ToStringUtf8();
                _logger.LogInformation(" [RECEBIDO] Payload de cadastro recebido.");
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null
                };
                var envelope = JsonSerializer.Deserialize<JsonElement>(rawJson, options);
                string tipo = envelope.GetProperty("tipoEvento").GetString();
                string payload = envelope.GetProperty("payload").GetString();

                if (tipo == "UsuarioCadastrado")
                {
                    var evento = JsonSerializer.Deserialize<UsuarioCadastradoEvent>(payload,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (evento != null)
                    {
                        _logger.LogInformation(" [PROCESSANDO] Disparando evento de saldo inicial para Conta: {ContaId}", evento.ContaId);
                        await mediator.Publish(evento, ct);
                    }
                }

                // Confirma o processamento com sucesso
                return SubscriberClient.Reply.Ack;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " [ERRO] Falha crítica ao processar cadastro de usuário. Enviando Nack...");

                // Retorna Nack para que o Pub/Sub coloque a mensagem na fila novamente
                return SubscriberClient.Reply.Nack;
            }
        });
    }
}