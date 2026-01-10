using FirebaseAdmin;
using FluentValidation;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.PubSub.V1;
using MediatR;
using SaraBank.Application.Behaviors;
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
    public static IServiceCollection AddDependencyInjection(this IServiceCollection services, IConfiguration configuration, string projectId)
    {
        if (FirebaseApp.DefaultInstance == null)
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.GetApplicationDefault(),
                ProjectId = projectId
            });
        }

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
                { "TransferenciaIniciada", "sara-bank-transferencias-iniciadas" },
                { "TransferenciaCancelada", "sara-bank-transferencias-erros" },
                { "SaldoDebitado", "sara-bank-transferencias-debitadas" },
                { "FalhaNoCredito", "sara-bank-transferencias-compensar" },
                { "TransferenciaConcluida", "sara-bank-transferencias-concluidas" }
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
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(AppDomain.CurrentDomain.Load("SaraBank.Application"));

            // Checa se é duplicado (Idempotência)
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));

            // Valida os dados (Validation)
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        #region WORKERS
        
        // --- BACKGROUND SERVICES (WORKERS) ---
        // Worker de Usuários
        services.AddHostedService<UsuarioConsumerService>(sp =>
        {
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var logger = sp.GetRequiredService<ILogger<UsuarioConsumerService>>();

            var subscriptionName = SubscriptionName.FromProjectSubscription(projectId, "sara-bank-usuarios-sub");
            var client = SubscriberClient.Create(subscriptionName);

            return new UsuarioConsumerService(sp, client, logger);
        });

        // Worker de Movimentações
        services.AddHostedService<MovimentacaoConsumerService>(sp =>
        {
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var logger = sp.GetRequiredService<ILogger<MovimentacaoConsumerService>>();

            var subscriptionName = SubscriptionName.FromProjectSubscription(projectId, "sara-bank-movimentacoes-sub");
            var client = SubscriberClient.Create(subscriptionName);

            return new MovimentacaoConsumerService(sp, client, logger);
        });

        // Worker de TransferenciaIniciada
        services.AddHostedService<TransferenciaIniciadaConsumerService>(sp =>
        {
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var logger = sp.GetRequiredService<ILogger<TransferenciaIniciadaConsumerService>>();

            var subscriptionName = SubscriptionName.FromProjectSubscription(projectId, "sara-bank-transferencias-iniciadas-sub");
            var client = SubscriberClient.Create(subscriptionName);

            return new TransferenciaIniciadaConsumerService(sp, client, logger);
        });

        // Worker de SaldoDebitado
        services.AddHostedService<SaldoDebitadoConsumerService>(sp =>
        {
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var logger = sp.GetRequiredService<ILogger<SaldoDebitadoConsumerService>>();

            var subscriptionName = SubscriptionName.FromProjectSubscription(projectId, "sara-bank-transferencias-debitadas-sub");
            var client = SubscriberClient.Create(subscriptionName);

            return new SaldoDebitadoConsumerService(sp, client, logger);
        });

        // Worker de Compensação (Estorno)
        services.AddHostedService<FalhaNoCreditoConsumerService>(sp =>
        {
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var logger = sp.GetRequiredService<ILogger<FalhaNoCreditoConsumerService>>();

            var subscriptionName = SubscriptionName.FromProjectSubscription(projectId, "sara-bank-transferencias-compensar-sub");
            var client = SubscriberClient.Create(subscriptionName);

            return new FalhaNoCreditoConsumerService(sp, client, logger);
        });

        // Worker de Conclusão (Sucesso Total)
        services.AddHostedService<TransferenciaConcluidaConsumerService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<TransferenciaConcluidaConsumerService>>();

            var subscriptionName = SubscriptionName.FromProjectSubscription(projectId, "sara-bank-transferencias-concluidas-sub");
            var client = SubscriberClient.Create(subscriptionName);

            return new TransferenciaConcluidaConsumerService(sp, client, logger);
        });

        #endregion

        // Motor (Outbox)
        services.AddHostedService<OutboxWorker>();

        // IDENTIDADE (GOOGLE IDENTITY PLATFORM / FIREBASE)
        // Wrapper: SDK do Google
        services.AddSingleton<IFirebaseAuthWrapper, FirebaseAuthWrapper>();
        services.AddHttpClient<IIdentityService, FirebaseAuthService>();


        return services;
    }
}