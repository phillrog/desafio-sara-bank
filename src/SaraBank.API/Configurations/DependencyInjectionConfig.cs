using FluentValidation;
using Google.Cloud.Firestore;
using Google.Cloud.PubSub.V1;
using SaraBank.Application.Handlers.Commands;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Interfaces;
using SaraBank.Infrastructure.Persistence;
using SaraBank.Infrastructure.Persistence.Converters;
using SaraBank.Infrastructure.Repositories;
using SaraBank.Infrastructure.Services;
using SaraBank.Infrastructure.Workers;

namespace SaraBank.API.Configurations;

public static class DependencyInjectionConfig
{
    public static IServiceCollection AddDependencyInjection(this IServiceCollection services, IConfiguration configuration)
    {
        var projectId = configuration["Firestore:ProjectId"] ?? "sara-bank";
        var topicId = configuration["PubSub:TopicId"] ?? "sara-bank-transacoes-topic";
        var subscriptionId = configuration["PubSub:SubscriptionId"] ?? "sara-bank-notificacoes-sub";

        // --- BANCO DE DADOS (FIRESTORE COM CONVERSORES) ---
        services.AddSingleton(sp =>
        {
            var builder = new FirestoreDbBuilder
            {
                ProjectId = projectId,
                ConverterRegistry = new ConverterRegistry
                {
                    new GuidConverter(),
                    new DecimalConverter()
                }
            };

            return builder.Build();
        });

        // --- MENSAGERIA: PUBLISHER (LADO DO ENVIO) ---
        services.AddSingleton(sp =>
        {
            var topicName = TopicName.FromProjectTopic(projectId, topicId);
            return PublisherClient.Create(topicName);
        });

        // --- MENSAGERIA: SUBSCRIBER (LADO DO CONSUMO) ---
        services.AddSingleton(sp =>
        {
            var subscriptionName = SubscriptionName.FromProjectSubscription(projectId, subscriptionId);
            return SubscriberClient.Create(subscriptionName);
        });

        // Interface que encapsula o PublisherClient do Google
        services.AddSingleton<IPublisher, SaraBank.Infrastructure.Services.Publisher>();

        // ---  PERSISTÊNCIA E TRANSAÇÃO (SCOPED) ---
        services.AddScoped<IUnitOfWork, FirestoreUnitOfWork>();

        // Repositórios
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IContaRepository, ContaRepository>();
        services.AddScoped<IMovimentacaoRepository, MovimentacaoRepository>();
        services.AddScoped<IOutboxRepository, FirestoreOutboxRepository>();

        // --- MEDIATR E VALIDAÇÃO ---
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(typeof(CriarMovimentacaoHandler).Assembly);
        });

        services.AddValidatorsFromAssembly(typeof(CriarMovimentacaoHandler).Assembly);

        // --- BACKGROUND SERVICES (WORKERS) ---
        // O OutboxWorker envia para o Pub/Sub, o PubSubConsumerService recebe do Pub/Sub
        services.AddHostedService<OutboxWorker>();
        services.AddHostedService<PubSubConsumerService>();

        return services;
    }
}