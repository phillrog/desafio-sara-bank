using MediatR;
using SaraBank.Application.Commands;

namespace SaraBank.Application.Handlers.Events;

public class GravarMovimentacaoNoBancoHandler : INotificationHandler<NovaMovimentacaoEvent>
{
    private readonly IMediator _mediator;

    public GravarMovimentacaoNoBancoHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Handle(NovaMovimentacaoEvent notification, CancellationToken ct)
    {
        var command = new CriarMovimentacaoCommand(
            notification.ContaId,
            notification.Valor,
            notification.Tipo
        );

        await _mediator.Send(command, ct);
    }
}