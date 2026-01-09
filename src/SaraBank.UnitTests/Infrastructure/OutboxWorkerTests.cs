using Moq;
using Xunit;
using FluentAssertions;
using SaraBank.Application.Interfaces;
using SaraBank.Infrastructure.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SaraBank.UnitTests.Infrastructure;

public class OutboxWorkerTests
{
    private readonly Mock<IOutboxRepository> _mockRepo;
    private readonly Mock<IPublisher> _mockPublisher;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<ILogger<OutboxWorker>> _mockLogger;

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
        // Arrange - mocks necessários
        int tentativasRealizadas = 0;
        const string mensagemId = "msg-999";
        const string payloadEsperado = "{\"valor\": 500.00, \"moeda\": \"BRL\"}";
        const string tipoEsperado = "TransferenciaRealizada";

        // uma mensagem pendente no "banco"
        var mensagensPendentes = new List<OutboxMessageDTO>
        {
            new OutboxMessageDTO(mensagemId, payloadEsperado, tipoEsperado)
        };

        _mockRepo
            .Setup(r => r.ObterNaoProcessadosAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mensagensPendentes);

        // falha no Publisher: Falha 2 vezes e acerta na 3ª (Polly em ação)
        // AJUSTE: Incluído o parâmetro de tipo no Mock
        _mockPublisher
            .Setup(p => p.PublicarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string p, string t, CancellationToken c) => {
                tentativasRealizadas++;
                if (tentativasRealizadas < 3)
                    throw new Exception("Erro temporário de conexão com Pub/Sub");

                return await Task.FromResult("SUCCESS_ID_123");
            });

        // Instancia o Worker - AJUSTE: Adicionado o Mock do Logger
        var worker = new OutboxWorker(_mockScopeFactory.Object, _mockPublisher.Object, _mockLogger.Object);

        // Act
        // chama o método principal de processamento
        await worker.ProcessarEventosOutbox(_mockRepo.Object, CancellationToken.None);

        // Assert
        // Verificação 1: O Polly deve ter garantido 3 tentativas no total
        tentativasRealizadas.Should().Be(3);

        // Verificação 2: O Publisher deve ter recebido o payload e tipo corretos
        _mockPublisher.Verify(p => p.PublicarAsync(payloadEsperado, tipoEsperado, It.IsAny<CancellationToken>()), Times.Exactly(3));

        // Verificação 3: O Repositório deve ter marcado como processado APENAS UMA VEZ após o sucesso
        _mockRepo.Verify(r => r.MarcarComoProcessadoAsync(mensagemId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Nao_Deve_Marcar_Como_Processado_Se_Todas_As_Tentativas_Do_Polly_Falharem()
    {
        // Arrange
        _mockRepo.Setup(r => r.ObterNaoProcessadosAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OutboxMessageDTO> { new OutboxMessageDTO("1", "{}", "Tipo") });

        // Simular falha perpétua - AJUSTE: Parâmetro adicional de string
        _mockPublisher.Setup(p => p.PublicarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("Falha Crítica"));

        var worker = new OutboxWorker(_mockScopeFactory.Object, _mockPublisher.Object, _mockLogger.Object);

        // Act
        await worker.ProcessarEventosOutbox(_mockRepo.Object, CancellationToken.None);

        // Assert
        // O repositório NUNCA deve ser chamado para marcar como processado se o Pub/Sub falhou
        _mockRepo.Verify(r => r.MarcarComoProcessadoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Deve_Desistir_Apos_3_Tentativas_E_NAO_Marcar_Como_Processado()
    {
        // Arrange
        _mockRepo.Setup(r => r.ObterNaoProcessadosAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OutboxMessageDTO> { new OutboxMessageDTO("1", "{}", "Tipo") });

        // Simula que o Pub/Sub está fora do ar permanentemente
        _mockPublisher
            .Setup(p => p.PublicarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Erro Crítico Persistente"));

        var worker = new OutboxWorker(_mockScopeFactory.Object, _mockPublisher.Object, _mockLogger.Object);

        // Act
        await worker.ProcessarEventosOutbox(_mockRepo.Object, CancellationToken.None);

        // Assert
        // O Repositório NUNCA deve ter sido chamado para atualizar o status para 'true'
        _mockRepo.Verify(r => r.MarcarComoProcessadoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never, "O registro não deve ser marcado como processado se o envio falhou.");

        // O Publisher deve ter sido tentado exatamente 4 vezes (1 original + 3 retentativas do Polly)
        _mockPublisher.Verify(p => p.PublicarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(4));
    }
}