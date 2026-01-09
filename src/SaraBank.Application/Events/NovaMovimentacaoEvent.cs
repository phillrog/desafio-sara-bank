using MediatR;

public record NovaMovimentacaoEvent(
    Guid ContaId,
    decimal Valor,
    string Tipo,
    string Descricao
) : INotification;