using MediatR;
using SaraBank.Application.Events;

namespace SaraBank.Application.Handlers.Events;

public class ProcessarNotificacaoMovimentacaoHandler : INotificationHandler<MovimentacaoRealizadaEvent>
{
    public Task Handle(MovimentacaoRealizadaEvent notification, CancellationToken ct)
    {
        Console.WriteLine($"[NOTIFICAÇÃO] Processando {notification.Tipo} de R$ {notification.Valor} para a conta {notification.ContaId}");

        return Task.CompletedTask;
    }
}