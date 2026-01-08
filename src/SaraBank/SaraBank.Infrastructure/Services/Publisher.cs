using Google.Cloud.PubSub.V1;
using SaraBank.Application.Interfaces;

namespace SaraBank.Infrastructure.Services
{
    public class Publisher : IPublisher
    {
        private readonly PublisherClient _client;

        public Publisher(PublisherClient client) => _client = client;

        public async Task<string> PublicarAsync(string payload, CancellationToken ct = default)
        {
            return await _client.PublishAsync(payload);
        }
    }
}
