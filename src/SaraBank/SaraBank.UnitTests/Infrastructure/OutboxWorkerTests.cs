using Moq;
using Xunit;
using FluentAssertions;
using SaraBank.Application.Interfaces;
using SaraBank.Infrastructure.Workers;
using Microsoft.Extensions.DependencyInjection;

namespace SaraBank.UnitTests.Infrastructure;

public class OutboxWorkerTests
{
    [Fact]
    public async Task Deve_Tentar_Enviar_Novamente_Com_Polly_Quando_Falhar_E_Marcar_Como_Processado_No_Sucesso()
    {
        // Arrange - mocks necessários
        var mockRepo = new Mock<IOutboxRepository>();
        var mockPublisher = new Mock<IPublisher>();
        var mockScopeFactory = new Mock<IServiceScopeFactory>();

        int tentativasRealizadas = 0;
        const string mensagemId = "msg-999";
        const string payloadEsperado = "{\"valor\": 500.00, \"moeda\": \"BRL\"}";

        // uma mensagem pendente no "banco"
        var mensagensPendentes = new List<OutboxMessage>
        {
            new OutboxMessage(mensagemId, payloadEsperado, "TransferenciaRealizada")
        };

        mockRepo
            .Setup(r => r.ObterNaoProcessadosAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mensagensPendentes);

        // falha no Publisher: Falha 2 vezes e acerta na 3ª (Polly em ação)
        mockPublisher
            .Setup(p => p.PublicarAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string p, CancellationToken c) => {
                tentativasRealizadas++;
                if (tentativasRealizadas < 3)
                    throw new Exception("Erro temporário de conexão com Pub/Sub");

                return await Task.FromResult("SUCCESS_ID_123");
            });

        // Instancia o Worker 
        var worker = new OutboxWorker(mockScopeFactory.Object, mockPublisher.Object);

        // Act
        // chama o método principal de processamento
        await worker.ProcessarEventosOutbox(mockRepo.Object, CancellationToken.None);

        // Assert
        // Verificação 1: O Polly deve ter garantido 3 tentativas no total
        tentativasRealizadas.Should().Be(3);

        // Verificação 2: O Publisher deve ter recebido o payload correto
        mockPublisher.Verify(p => p.PublicarAsync(payloadEsperado, It.IsAny<CancellationToken>()), Times.Exactly(3));

        // Verificação 3: O Repositório deve ter marcado como processado APENAS UMA VEZ após o sucesso
        mockRepo.Verify(r => r.MarcarComoProcessadoAsync(mensagemId, It.IsAny<CancellationToken>()), Times.Once);

        // Verificação 4: Garantir que não tentou marcar como processado se tivesse falhado tudo (implícito no Times.Once)
        mockPublisher.Verify(p => p.PublicarAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
        Times.Exactly(3));

        mockRepo.Verify(r => r.MarcarComoProcessadoAsync(mensagemId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Nao_Deve_Marcar_Como_Processado_Se_Todas_As_Tentativas_Do_Polly_Falharem()
    {
        // Arrange
        var mockRepo = new Mock<IOutboxRepository>();
        var mockPublisher = new Mock<IPublisher>();
        var mockScopeFactory = new Mock<IServiceScopeFactory>();

        mockRepo.Setup(r => r.ObterNaoProcessadosAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OutboxMessage> { new OutboxMessage("1", "{}", "Tipo") });

        // Simular falha perpétua
        mockPublisher.Setup(p => p.PublicarAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("Falha Crítica"));

        var worker = new OutboxWorker(mockScopeFactory.Object, mockPublisher.Object);

        // Act
        await worker.ProcessarEventosOutbox(mockRepo.Object, CancellationToken.None);

        // Assert
        // O repositório NUNCA deve ser chamado para marcar como processado se o Pub/Sub falhou
        mockRepo.Verify(r => r.MarcarComoProcessadoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Deve_Desistir_Apos_3_Tentativas_E_NAO_Marcar_Como_Processado()
    {
        // Arrange
        var mockRepo = new Mock<IOutboxRepository>();
        var mockPublisher = new Mock<IPublisher>();
        var mockScopeFactory = new Mock<IServiceScopeFactory>();

        mockRepo.Setup(r => r.ObterNaoProcessadosAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OutboxMessage> { new OutboxMessage("1", "{}", "Tipo") });

        // Simula que o Pub/Sub está fora do ar permanentemente
        mockPublisher
            .Setup(p => p.PublicarAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Erro Crítico Persistente"));

        var worker = new OutboxWorker(mockScopeFactory.Object, mockPublisher.Object);

        // Act
        await worker.ProcessarEventosOutbox(mockRepo.Object, CancellationToken.None);

        // Assert
        // O Repositório NUNCA deve ter sido chamado para atualizar o status para 'true'
        mockRepo.Verify(r => r.MarcarComoProcessadoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never, "O registro não deve ser marcado como processado se o envio falhou.");

        // O Publisher deve ter sido tentado exatamente 4 vezes (1 original + 3 retentativas do Polly)
        mockPublisher.Verify(p => p.PublicarAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(4));
    }
}