using FluentValidation;
using Google.Cloud.Firestore;
using Google.Cloud.PubSub.V1;
using MediatR;
using SaraBank.Application.Behaviors;
using SaraBank.Application.Handlers.Commands;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Interfaces;
using SaraBank.Infrastructure.Persistence;
using SaraBank.Infrastructure.Persistence.Converters;
using SaraBank.Infrastructure.Repositories;
using SaraBank.Infrastructure.Services;
using SaraBank.Infrastructure.Workers;
using System.Reflection;

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
        services.AddSingleton<SaraBank.Application.Interfaces.IPublisher, SaraBank.Infrastructure.Services.Publisher>();

        // ---  PERSISTÊNCIA E TRANSAÇÃO (SCOPED) ---
        services.AddScoped<IUnitOfWork, FirestoreUnitOfWork>();

        // Repositórios
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IContaRepository, ContaRepository>();
        services.AddScoped<IMovimentacaoRepository, MovimentacaoRepository>();
        services.AddScoped<IOutboxRepository, FirestoreOutboxRepository>();

        // --- MEDIATR E VALIDAÇÃO ---
        // Registra todos os Validators do FluentValidation que estão no Assembly
        services.AddValidatorsFromAssembly(AppDomain.CurrentDomain.Load("SaraBank.Application"));

        // Registra o Behavior no MediatR
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(AppDomain.CurrentDomain.Load("SaraBank.Application"));

            // Adiciona o behavior de validação na fila do pipeline
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        // --- BACKGROUND SERVICES (WORKERS) ---
        // O OutboxWorker envia para o Pub/Sub, o PubSubConsumerService recebe do Pub/Sub
        services.AddHostedService<OutboxWorker>();
        services.AddHostedService<PubSubConsumerService>();

        return services;
    }
}