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
            using (var scope = _serviceProvider.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                
                string json = message.Data.ToStringUtf8();

                var evento = JsonSerializer.Deserialize<MovimentacaoRealizadaEvent>(json);

                if (evento != null)
                {
                    // Envia para o Handler (ProcessarNotificacaoMovimentacaoHandler)
                    await mediator.Publish(evento, ct);
                }
                return SubscriberClient.Reply.Ack;
            }
        });
    }
}