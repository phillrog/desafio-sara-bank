using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text;
using System.Xml.XPath;

namespace SaraBank.API.Configurations;

public static class SwaggerConfig
{
    /// <summary>
    /// Configura dinamicamente o SwaggerGen para o Sara Bank.
    /// </summary>
    public static IServiceCollection AddCustomizedSwagger(this IServiceCollection services, params Type[] assemblyAnchorTypes)
    {
        services.AddSwaggerGen(c =>
        {
            // Coleta os assemblies para buscar os arquivos de documentação XML
            var assembliesToDocument = assemblyAnchorTypes
                .Select(t => t.Assembly)
                .Distinct()
                .ToList();

            // Inclui o assembly da própria API caso não tenha sido passado
            if (!assembliesToDocument.Contains(Assembly.GetExecutingAssembly()))
            {
                assembliesToDocument.Add(Assembly.GetExecutingAssembly());
            }

            foreach (var assembly in assembliesToDocument)
            {
                var xmlFile = $"{assembly.GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

                if (File.Exists(xmlPath))
                {
                    // Forçamos a leitura em UTF-8 para evitar problemas com acentuação brasileira
                    using var fileStream = new FileStream(xmlPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var streamReader = new StreamReader(fileStream, Encoding.UTF8);
                    c.IncludeXmlComments(() => new XPathDocument(streamReader), true);
                }
            }

            // Configuração de Segurança JWT
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Exemplo: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer",
                        },
                        Scheme = "oauth2",
                        Name = "Bearer",
                        In = ParameterLocation.Header,
                    },
                    new List<string>()
                }
            });
        });

        return services;
    }

    /// <summary>
    /// Ativa o middleware do Swagger no pipeline do Sara Bank.
    /// </summary>
    public static IApplicationBuilder UseCustomizedSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger();

        app.UseSwaggerUI(c =>
        {            
            c.RoutePrefix = "swagger"; // Acessível em /swagger
            c.DefaultModelsExpandDepth(-1); // Oculta a seção de Schemas por padrão para uma UI mais limpa
        });

        return app;
    }
}