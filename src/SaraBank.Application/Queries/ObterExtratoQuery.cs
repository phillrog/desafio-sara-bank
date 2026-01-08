using MediatR;
using SaraBank.Domain.Entities;

namespace SaraBank.Application.Queries;
public record ObterExtratoQuery(string ContaId) : IRequest<IEnumerable<Movimentacao>>;