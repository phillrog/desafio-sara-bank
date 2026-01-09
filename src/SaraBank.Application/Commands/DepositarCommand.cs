using MediatR;

namespace SaraBank.Application.Commands;

public record DepositarCommand(
    Guid ContaId,
    decimal Valor) : IRequest<bool>;