using FluentValidation;
using Google.Cloud.Firestore;
using Google.Cloud.PubSub.V1;
using SaraBank.Application.Handlers;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Interfaces;
using SaraBank.Infrastructure.Persistence;
using SaraBank.Infrastructure.Repositories;
using SaraBank.Infrastructure.Workers;

namespace SaraBank.API.Configurations
{
    public static class DependencyInjectionConfig
    {
        public static IServiceCollection AddDependencyInjection(this IServiceCollection services, IConfiguration configuration)
        {
            var projectId = configuration["Firestore:ProjectId"];

            // --- Banco de Dados ---
            services.AddSingleton(sp => FirestoreDb.Create(projectId));

            // --- Persistência e Transação ---
            services.AddScoped<IUnitOfWork, FirestoreUnitOfWork>();

            // Repositórios
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<IContaRepository, ContaRepository>();
            services.AddScoped<IMovimentacaoRepository, MovimentacaoRepository>();
            services.AddScoped<IOutboxRepository, FirestoreOutboxRepository>();

            // --- MediaTr e Validação ---
            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(typeof(RealizarTransferenciaHandler).Assembly);
            });
            services.AddValidatorsFromAssembly(typeof(RealizarTransferenciaHandler).Assembly);

            // --- Mensageria (Pub/Sub) ---
            services.AddSingleton(sp => {
                var topicName = TopicName.FromProjectTopic(projectId, "sara-bank-transacoes-topic");
                return PublisherClient.Create(topicName);
            });

            services.AddSingleton<IPublisher, SaraBank.Infrastructure.Services.Publisher>();

            // --- Background Services (Workers) ---
            services.AddHostedService<OutboxWorker>();

            return services;
        }
    }
}