using MediatR;
using SaraBank.Application.DTOs;

namespace SaraBank.Application.Queries;

public record ObterContaCorrentePorIdQuery(Guid Id) : IRequest<ContaResponse?>;