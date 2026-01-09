using FluentValidation;
using Google.Cloud.Firestore;
using Google.Cloud.PubSub.V1;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
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
        services.AddSingleton<SaraBank.Application.Interfaces.IPublisher, SaraBank.Infrastructure.Services.Publisher>(sp =>
        {
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var logger = sp.GetRequiredService<ILogger<SaraBank.Infrastructure.Services.Publisher>>();
            // Mapeamento: Tipo de Evento no Outbox -> Nome do Tópico no GCP
            var mapeamento = new Dictionary<string, string>
            {
                { "UsuarioCadastrado", "sara-bank-usuarios" },
                { "NovaMovimentacao", "sara-bank-movimentacoes" },
                { "TransferenciaEntreContas", "sara-bank-transferencias" }
            };
            return new SaraBank.Infrastructure.Services.Publisher(projectId, mapeamento, logger);
        });

        // ---  PERSISTÊNCIA E TRANSAÇÃO (SCOPED) ---
        services.AddScoped<IUnitOfWork, FirestoreUnitOfWork>();

        // Repositórios
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IContaRepository, ContaRepository>();
        services.AddScoped<IMovimentacaoRepository, MovimentacaoRepository>();
        services.AddScoped<IOutboxRepository, FirestoreOutboxRepository>();
        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();

        // --- MEDIATR E VALIDAÇÃO ---
        // Registra todos os Validators do FluentValidation que estão no Assembly
        services.AddValidatorsFromAssembly(AppDomain.CurrentDomain.Load("SaraBank.Application"));

        // Registra o Behavior no MediatR
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(AppDomain.CurrentDomain.Load("SaraBank.Application"));

            // Checa se é duplicado (Idempotência)
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));

            // Valida os dados (Validation)
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        // --- BACKGROUND SERVICES (WORKERS) ---
        // Worker de Usuários
        services.AddHostedService<UsuarioConsumerService>(sp =>
        {
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var logger = sp.GetRequiredService<ILogger<UsuarioConsumerService>>();

            // Criar o client específico para a subscription de usuários
            var subscriptionName = SubscriptionName.FromProjectSubscription(projectId, "sara-bank-usuarios-sub");
            var client = SubscriberClient.Create(subscriptionName);

            return new UsuarioConsumerService(sp, client, logger);
        });

        // Worker de Movimentações
        services.AddHostedService<MovimentacaoConsumerService>(sp =>
        {
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var logger = sp.GetRequiredService<ILogger<MovimentacaoConsumerService>>();

            // Criar o client específico para a subscription de movimentações
            var subscriptionName = SubscriptionName.FromProjectSubscription(projectId, "sara-bank-movimentacoes-sub");
            var client = SubscriberClient.Create(subscriptionName);

            return new MovimentacaoConsumerService(sp, client, logger);
        });

        // Motor (Outbox)
        services.AddHostedService<OutboxWorker>();

        return services;
    }
}