using MediatR;
using SaraBank.Application.Events;
using SaraBank.Application.Interfaces;
using System.Text.Json;

public class ProcessarSaldoInicialHandler : INotificationHandler<UsuarioCadastradoEvent>
{
    private readonly IUnitOfWork _uow;

    public ProcessarSaldoInicialHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(UsuarioCadastradoEvent notification, CancellationToken ct)
    {
        if (notification.SaldoInicial > 0)
        {
            var evento = new NovaMovimentacaoEvent(
                notification.ContaId,
                notification.SaldoInicial,
                "Credito",
                "Saldo Inicial de Abertura"
            );

            var envelope = new
            {
                TipoEvento = "NovaMovimentacao",
                Payload = JsonSerializer.Serialize(evento)
            };

            // 2. Grava no Outbox em vez de enviar direto
            // O Outbox Worker vai ler isso e jogar no tópico 'sara-bank-movimentacoes'
            await _uow.AdicionarAoOutboxAsync(JsonSerializer.Serialize(envelope), "NovaMovimentacao");
        }
    }
}