using Google.Cloud.PubSub.V1;
using Microsoft.Extensions.Logging;
using SaraBank.Application.Interfaces;
using System.Collections.Concurrent;

namespace SaraBank.Infrastructure.Services
{
    public class Publisher : IPublisher
    {
        private readonly string _projectId;
        private readonly Dictionary<string, string> _mapeamento;
        private readonly ILogger<Publisher> _logger;
        private static readonly ConcurrentDictionary<string, Task<PublisherClient>> _clients = new();

        public Publisher(string projectId, Dictionary<string, string> mapeamento, ILogger<Publisher> logger)
        {
            _projectId = projectId;
            _mapeamento = mapeamento;
            _logger = logger;
        }

        public async Task<string> PublicarAsync(string payload, string tipoEvento, CancellationToken ct = default)
        {
            if (!_mapeamento.TryGetValue(tipoEvento, out var topico))
            {
                _logger.LogError(" [PUBLISHER] Falha de mapeamento: Tipo de evento '{Tipo}' não possui tópico configurado.", tipoEvento);
                throw new Exception($"Tópico não encontrado para o evento: {tipoEvento}");
            }

            try
            {
                TopicName topicName = TopicName.FromProjectTopic(_projectId, topico);

                var clientTask = _clients.GetOrAdd(topico, t =>
                {
                    _logger.LogInformation(" [PUBLISHER] Criando novo PublisherClient para o tópico: {Topico}", t);
                    return PublisherClient.CreateAsync(topicName);
                });

                var client = await clientTask;

                _logger.LogDebug(" [PUBLISHER] Publicando mensagem no tópico: {Topico}", topico);

                string messageId = await client.PublishAsync(payload);

                _logger.LogInformation(" [PUBLISHER] Mensagem publicada com sucesso. ID: {MessageId} | Tópico: {Topico}", messageId, topico);

                return messageId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " [PUBLISHER] Erro ao publicar mensagem para o tipo: {Tipo} no tópico: {Topico}", tipoEvento, topico);
                throw;
            }
        }
    }
}