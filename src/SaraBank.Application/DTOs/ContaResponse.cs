namespace SaraBank.Application.DTOs;

public record ContaResponse(
    Guid Id,
    Guid UsuarioId,
    decimal Saldo,
    DateTime DataCriacao
);