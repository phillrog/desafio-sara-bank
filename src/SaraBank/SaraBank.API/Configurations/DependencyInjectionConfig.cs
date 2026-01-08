using FluentValidation;
using Google.Cloud.Firestore;
using SaraBank.Application.Handlers;
using SaraBank.Application.Repositories;
using SaraBank.Domain.Interfaces;
using SaraBank.Infrastructure.Persistence;

namespace SaraBank.API.Configurations
{

    public static class DependencyInjectionConfig
    {
        public static IServiceCollection AddDependencyInjection(this IServiceCollection services, IConfiguration configuration)
        {
            var projectId = configuration["Firestore:ProjectId"];
            services.AddSingleton(sp => FirestoreDb.Create(projectId));
            services.AddScoped<IUnitOfWork, FirestoreUnitOfWork>();
            services.AddScoped<IContaRepository, ContaRepository>();

            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(typeof(RealizarTransferenciaHandler).Assembly);
            });

            services.AddValidatorsFromAssembly(typeof(RealizarTransferenciaHandler).Assembly);

            return services;
        }
    }
}