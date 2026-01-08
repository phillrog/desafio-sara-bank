using MediatR;

namespace SaraBank.Application.Commands;

public record DepositarCommand(
    string ContaId,
    decimal Valor) : IRequest<bool>;