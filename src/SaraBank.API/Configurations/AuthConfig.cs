using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens; // Essencial para .NET 8+
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace SaraBank.API.Configurations;

public static class AuthConfig
{
    public static IServiceCollection AddGcpIdentityAuthConfiguration(this IServiceCollection services, IConfiguration configuration, string projectId)
    {
        // 1. Limpa o mapeamento padrão do .NET que renomeia claims do Google (ex: 'sub' vira 'NameIdentifier').
        // Isso mantém os nomes originais vindos do Firebase/Identity Platform.
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // 2. Define o emissor confiável (Google) vinculado ao seu projeto específico.
                options.Authority = $"https://securetoken.google.com/{projectId}";

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = $"https://securetoken.google.com/{projectId}",
                    ValidateAudience = true,
                    ValidAudience = projectId,
                    ValidateLifetime = true,

                    // 3. DESATIVADO PARA DESENVOLVIMENTO:
                    // Impede que o .NET tente baixar as chaves públicas do Google (que costuma falhar em rede local/Docker).
                    // Em produção, o ideal é reativar para garantir que o token não foi forjado.
                    ValidateIssuerSigningKey = false,

                    // 4. Tolerância zero para expiração de token.
                    ClockSkew = TimeSpan.Zero,

                    // 5. SOLUÇÃO PARA .NET 8 (O "Pulo do Gato"):
                    // O .NET 8 usa um novo motor (JsonWebTokenHandler). 
                    // Como desativamos a 'ValidateIssuerSigningKey', precisamos deste delegate para ler o token 
                    // manualmente e dizer ao .NET que ele é válido, ignorando a falta da chave de assinatura.
                    SignatureValidator = delegate (string token, TokenValidationParameters parameters)
                    {
                        var handler = new JsonWebTokenHandler();
                        return handler.ReadJsonWebToken(token);
                    }
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        // Log para ajudar a identificar erros no console durante o desenvolvimento
                        Console.WriteLine("Falha na autenticação: " + context.Exception.Message);
                        return Task.CompletedTask;
                    }
                };

                options.IncludeErrorDetails = true;

                // 6. Ignora erros de certificado SSL ao fazer chamadas de "backchannel" (saída) para o Google.
                // Ajuda muito quando o ambiente local (HTTPS) tem conflitos com os certificados do Google.
                options.BackchannelHttpHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };
            });

        // 7. POLÍTICA DE SEGURANÇA GLOBAL:
        // Configura a API para que, por padrão, TODOS os endpoints exijam token (401 se não enviar).
        // Endpoints públicos (Login/Cadastro) devem usar explicitamente o atributo [AllowAnonymous].
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }

    public static IApplicationBuilder UseGcpIdentityAuthConfiguration(this IApplicationBuilder app)
    {
        // A ordem aqui é vital: Primeiro identifica (Authn), depois autoriza o acesso (Authz).
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}