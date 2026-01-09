using Google.Cloud.PubSub.V1;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SaraBank.Application.Events;
using System.Text.Json;

public class PubSubConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SubscriberClient _subscriberClient;

    public PubSubConsumerService(IServiceProvider serviceProvider, SubscriberClient subscriberClient)
    {
        _serviceProvider = serviceProvider;
        _subscriberClient = subscriberClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _subscriberClient.StartAsync(async (PubsubMessage message, CancellationToken ct) =>
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            string json = message.Data.ToStringUtf8();
            var envelope = JsonSerializer.Deserialize<JsonElement>(json);

            string tipo = envelope.GetProperty("TipoEvento").GetString();
            string payload = envelope.GetProperty("Payload").GetString();

            object eventoFinal = tipo switch
            {
                "UsuarioCadastrado" => JsonSerializer.Deserialize<UsuarioCadastradoEvent>(payload),
                "MovimentacaoRealizada" => JsonSerializer.Deserialize<MovimentacaoRealizadaEvent>(payload),
                _ => null
            };

            if (eventoFinal != null)
            {
                // Usamos Publish(object) para o MediatR encontrar o handler certo dinamicamente
                await mediator.Publish(eventoFinal, ct);
            }

            return SubscriberClient.Reply.Ack;
        });
    }
}