using MediatR;

namespace SaraBank.Application.Events
{
    public abstract record EventoSaga : INotification
    {
        public Guid EventoId { get; init; } = Guid.NewGuid();
        public DateTime OcorridoEm { get; init; } = DateTime.UtcNow;
    }

    public record TransferenciaIniciadaEvent(
        Guid SagaId,
        Guid ContaOrigemId,
        Guid ContaDestinoId,
        decimal Valor
    ) : EventoSaga;

    public record TransferenciaCanceladaEvent(
        Guid SagaId,
        Guid ContaOrigemId,
        string Motivo
    ) : EventoSaga;

    public record SaldoDebitadoEvent(
        Guid SagaId,
        Guid ContaOrigemId,
        Guid ContaDestinoId,
        decimal Valor
    ) : EventoSaga;

    public record FalhaNoCreditoEvent(
        Guid SagaId,
        Guid ContaOrigemId,
        decimal Valor,
        string Motivo
    ) : EventoSaga;

    public record TransferenciaConcluidaEvent(
        Guid SagaId,
        DateTime ConcluidoEm
    ) : EventoSaga;

}
