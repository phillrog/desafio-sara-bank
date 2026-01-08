using Google.Cloud.PubSub.V1;
using SaraBank.Application.Interfaces;

namespace SaraBank.Infrastructure.Messaging;

public class GooglePubSubPublisher : IPublisher
{
    private readonly PublisherClient _publisherClient;

    public GooglePubSubPublisher(PublisherClient publisherClient)
    {
        _publisherClient = publisherClient;
    }

    public async Task<string> PublicarAsync(string payload, CancellationToken ct = default)
    {
        try
        {
            string messageId = await _publisherClient.PublishAsync(payload);

            return messageId;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao publicar no Google Pub/Sub: {ex.Message}", ex);
        }
    }
}