using MediatR;

namespace SaraBank.Application.Events;

public record UsuarioCadastradoEvent(
Guid UsuarioId,
string Nome,
string Email,
Guid ContaId,
DateTime DataCriacao
) : INotification;

