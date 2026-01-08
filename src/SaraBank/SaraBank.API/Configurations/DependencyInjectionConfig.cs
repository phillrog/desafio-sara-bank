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

            // Banco
            services.AddSingleton(sp => FirestoreDb.Create(projectId));

            // Persistência (Scoped para transações por requisição)
            services.AddScoped<IUnitOfWork, FirestoreUnitOfWork>();
            services.AddScoped<IContaRepository, ContaRepository>();
            services.AddScoped<IOutboxRepository, FirestoreOutboxRepository>();

            // MediatR e FluentValidation
            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(typeof(RealizarTransferenciaHandler).Assembly);
            });
            services.AddValidatorsFromAssembly(typeof(RealizarTransferenciaHandler).Assembly);

            // Mensageria (PublisherClient deve ser Singleton)
            services.AddSingleton(sp => {
                var topicName = TopicName.FromProjectTopic(projectId, "sara-bank-eventos");
                return PublisherClient.Create(topicName);
            });

            // Wrapper do Publisher
            services.AddSingleton<IPublisher, SaraBank.Infrastructure.Services.Publisher>();

            // Workers
            services.AddHostedService<OutboxWorker>();

            return services;
        }
    }
}