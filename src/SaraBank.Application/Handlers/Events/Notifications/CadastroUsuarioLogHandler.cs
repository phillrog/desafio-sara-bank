using MediatR;
using Microsoft.Extensions.Logging;
using SaraBank.Application.Events;

namespace SaraBank.Application.Handlers.Events;

public class CadastroUsuarioLogHandler : INotificationHandler<UsuarioCadastradoEvent>
{
    private readonly ILogger<CadastroUsuarioLogHandler> _logger;

    public CadastroUsuarioLogHandler(ILogger<CadastroUsuarioLogHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(UsuarioCadastradoEvent notification, CancellationToken ct)
    {
        Console.WriteLine($"[AUDITORIA] Novo usuário no banco: {notification.Nome} (ID: {notification.UsuarioId})");
        return Task.CompletedTask;
    }
}