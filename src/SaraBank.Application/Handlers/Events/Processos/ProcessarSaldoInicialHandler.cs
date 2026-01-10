using MediatR;
using SaraBank.Application.Events;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Entities;
using System.Text.Json;

namespace SaraBank.Application.Handlers.Events;

public class ProcessarSaldoInicialHandler : INotificationHandler<UsuarioCadastradoEvent>
{
    private readonly IUnitOfWork _uow;
    private readonly IOutboxRepository _outboxRepository;

    public ProcessarSaldoInicialHandler(IUnitOfWork uow,
        IOutboxRepository outboxRepository)
    {
        _uow = uow;
        _outboxRepository = outboxRepository;
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
            
            var outboxMessage = new OutboxMessage(
                Guid.NewGuid(),
                JsonSerializer.Serialize(envelope),
                "NovaMovimentacao",
                "sara-bank-movimentacoes"
            );

            await _outboxRepository.AdicionarAsync(outboxMessage, ct);
        }
    }
}