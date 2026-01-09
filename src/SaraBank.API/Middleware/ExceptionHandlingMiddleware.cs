using FluentValidation;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace SaraBank.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Um erro não tratado ocorreu no Sara Bank.");

        // Define o Status Code baseado no tipo de exceção
        var statusCode = exception switch
        {
            ValidationException => (int)HttpStatusCode.BadRequest,
            _ => (int)HttpStatusCode.InternalServerError
        };

        // Cria o objeto de resposta
        var response = new
        {
            status = statusCode,
            titulo = exception switch
            {
                ValidationException => "Erro de Validação",
                _ => "Erro Interno no Servidor"
            },
            erros = exception switch
            {
                ValidationException vex => vex.Errors.Select(e => new
                {
                    campo = e.PropertyName,
                    mensagem = e.ErrorMessage
                }),
                _ => null
            }
        };

        // CONFIGURAÇÃO DO SERIALIZADOR
        var options = new JsonSerializerOptions
        {
            // JavaScriptEncoder.Create(UnicodeRanges.All) permite que acentos como 'ç' e 'ã' 
            // apareçam como texto normal no JSON em vez de códigos Unicode.
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true // indenta o JSON para melhor leitura
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var jsonResponse = JsonSerializer.Serialize(response, options);
        await context.Response.WriteAsync(jsonResponse);
    }
}