using FluentValidation;
using FluentValidation.Results;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using SaraBank.API.Middleware;
using System.Text.Json;
using Xunit;

namespace SaraBank.Tests.API.Middlewares;

public class ExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _mockLogger;
    private readonly DefaultHttpContext _context;

    public ExceptionHandlingMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        _context = new DefaultHttpContext();
        _context.Response.Body = new MemoryStream();
    }

    [Fact]
    public async Task Deve_Retornar_Status_400_Quando_ValidationException_For_Lancada()
    {
        // Arrange
        var failure = new ValidationFailure("Valor", "O valor deve ser positivo");
        RequestDelegate next = (innerContext) => throw new ValidationException(new[] { failure });

        var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, _context.Response.StatusCode);

        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        // Em vez de Assert.Contains na string bruta, vamos deserializar
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var resultado = JsonSerializer.Deserialize<ErroResponseTest>(responseBody, jsonOptions);

        Assert.NotNull(resultado);
        Assert.Equal("Erro de Validação", resultado.Titulo);
        Assert.Contains(resultado.Erros, e => e.Mensagem == "O valor deve ser positivo");
    }

    [Fact]
    public async Task Deve_Retornar_Status_500_Quando_Erro_Generico_For_Lancado()
    {
        // Arrange
        RequestDelegate next = (innerContext) => throw new Exception("Erro catastrófico!");
        var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, _context.Response.StatusCode);

        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        Assert.Contains("Erro Interno no Servidor", responseBody);
    }

    // Classe auxiliar apenas para o teste conseguir ler o JSON
    private class ErroResponseTest
    {
        public int Status { get; set; }
        public string Titulo { get; set; }
        public List<ErroDetalheTest> Erros { get; set; }
    }

    private class ErroDetalheTest
    {
        public string Campo { get; set; }
        public string Mensagem { get; set; }
    }
}