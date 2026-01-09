using MediatR;
using Microsoft.Extensions.Logging;
using SaraBank.Application.Events;

namespace SaraBank.Application.Handlers.Notifications;

public class MovimentacaoLogHandler : INotificationHandler<MovimentacaoRealizadaEvent>
{
    private readonly ILogger<MovimentacaoLogHandler> _logger;

    public MovimentacaoLogHandler(ILogger<MovimentacaoLogHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(MovimentacaoRealizadaEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(" [SUCESSO] Notificação processada para a Conta: {ContaId}", notification.Id);
        _logger.LogInformation(" >>> Valor: {Valor} | Tipo: {Tipo}", notification.Valor, notification.Tipo);

        return Task.CompletedTask;
    }
}