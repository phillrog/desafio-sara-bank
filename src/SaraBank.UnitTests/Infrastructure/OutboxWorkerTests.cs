using FluentAssertions;
using Google.Cloud.PubSub.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Entities;
using SaraBank.Infrastructure.Workers;
using System;
using System.Net;
using System.Text.RegularExpressions;
using Xunit;
using static Google.Cloud.Firestore.V1.StructuredQuery.Types;

namespace SaraBank.UnitTests.Infrastructure;

public class OutboxWorkerTests
{
    private readonly Mock<IOutboxRepository> _mockRepo;
    private readonly Mock<IPublisher> _mockPublisher;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<ILogger<OutboxWorker>> _mockLogger;
    private const string TopicoTeste = "sara-bank-teste";

    public OutboxWorkerTests()
    {
        _mockRepo = new Mock<IOutboxRepository>();
        _mockPublisher = new Mock<IPublisher>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockLogger = new Mock<ILogger<OutboxWorker>>();
    }

    [Fact]
    public async Task Deve_Tentar_Enviar_Novamente_Com_Polly_Quando_Falhar_E_Marcar_Como_Processado_No_Sucesso()
    {
        // Arrange
        int tentativasRealizadas = 0;
        var mensagemId = Guid.NewGuid();
        const string payloadEsperado = "{\"valor\": 500.00, \"moeda\": \"BRL\"}";
        const string tipoEsperado = "TransferenciaRealizada";

        var mensagensPendentes = new List<OutboxMessage>
        {
            new OutboxMessage(mensagemId, payloadEsperado, tipoEsperado, "TopicoTeste")
        };

        _mockRepo
            .Setup(r => r.ObterNaoProcessadosAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mensagensPendentes);

        _mockPublisher
            .Setup(p => p.PublicarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string payload, string tipo, CancellationToken c) =>
            {
                tentativasRealizadas++;
                if (tentativasRealizadas < 3)
                    throw new Exception("Erro temporário");

                return await Task.FromResult("SUCCESS_ID_123");
            });

        var worker = new OutboxWorker(_mockScopeFactory.Object, _mockPublisher.Object, _mockLogger.Object);

        // Act
        await worker.ProcessarEventosOutbox(_mockRepo.Object, CancellationToken.None);

        // Assert
        tentativasRealizadas.Should().Be(3);

        _mockPublisher.Verify(p => p.PublicarAsync(
            It.Is<string>(s => s == payloadEsperado),
            It.Is<string>(s => s == tipoEsperado),
            It.IsAny<CancellationToken>()),
            Times.Exactly(3));

        _mockRepo.Verify(r => r.MarcarComoProcessadoAsync(mensagemId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Nao_Deve_Marcar_Como_Processado_Se_Todas_As_Tentativas_Do_Polly_Falharem()
    {
        // Arrange
        var msgId = Guid.NewGuid();
        var msg = new OutboxMessage(msgId, "{}", "Tipo", TopicoTeste, 0, false, DateTime.UtcNow);

        _mockRepo.Setup(r => r.ObterNaoProcessadosAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OutboxMessage> { msg });

        _mockPublisher.Setup(p => p.PublicarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("Falha Crítica"));

        var worker = new OutboxWorker(_mockScopeFactory.Object, _mockPublisher.Object, _mockLogger.Object);

        // Act
        await worker.ProcessarEventosOutbox(_mockRepo.Object, CancellationToken.None);

        // Assert
        _mockRepo.Verify(r => r.MarcarComoProcessadoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Deve_Chamar_IncrementarFalha_No_Repositorio_Quando_Polly_Esgotar_Tentativas()
    {
        // Arrange
        var msgId = Guid.NewGuid();
        var mensagens = new List<OutboxMessage> { new OutboxMessage(msgId, "{}", "Tipo", TopicoTeste, 0, false, DateTime.UtcNow) };

        _mockRepo.Setup(r => r.ObterNaoProcessadosAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(mensagens);

        _mockPublisher.Setup(p => p.PublicarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .ThrowsAsync(new Exception("Erro persistente"));

        var worker = new OutboxWorker(_mockScopeFactory.Object, _mockPublisher.Object, _mockLogger.Object);

        // Act
        await worker.ProcessarEventosOutbox(_mockRepo.Object, CancellationToken.None);

        // Assert
        _mockRepo.Verify(r => r.IncrementarFalhaAsync(msgId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.MarcarComoProcessadoAsync(msgId, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Deve_Continuar_Processando_Proxima_Mensagem_Se_A_Primeira_Falhar_Definitivamente()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var msg1 = new OutboxMessage(id1, "{}", "Tipo1", "topico1");
        var msg2 = new OutboxMessage(id2, "{}", "Tipo2", "topico2");
        var mensagens = new List<OutboxMessage> { msg1, msg2 };

        _mockRepo.Setup(r => r.ObterNaoProcessadosAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(mensagens);
        
        _mockPublisher.Setup(p => p.PublicarAsync(
                            It.Is<string>(s => s == "{}"), // Payload
                            It.Is<string>(s => s == "Tipo1"), // Tipo
                            It.IsAny<CancellationToken>()))
                      .ThrowsAsync(new Exception("Falha na primeira"));

        _mockPublisher.Setup(p => p.PublicarAsync(
                            It.Is<string>(s => s == "{}"),
                            It.Is<string>(s => s == "Tipo2"),
                            It.IsAny<CancellationToken>()))
                      .ReturnsAsync("SUCCESS");

        var worker = new OutboxWorker(_mockScopeFactory.Object, _mockPublisher.Object, _mockLogger.Object);

        // Act
        await worker.ProcessarEventosOutbox(_mockRepo.Object, CancellationToken.None);

        // Assert
        _mockRepo.Verify(r => r.IncrementarFalhaAsync(id1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.MarcarComoProcessadoAsync(id2, It.IsAny<CancellationToken>()), Times.Once);
    }
}