using MediatR;
using SaraBank.Application.DTOs;
using SaraBank.Application.Queries;
using SaraBank.Domain.Interfaces;

namespace SaraBank.Application.Handlers.Queries;

public class ContaCorrenteQueryHandler : IRequestHandler<ObterContaCorrentePorIdQuery, ContaResponse?>
{
    private readonly IContaRepository _contaRepository;

    public ContaCorrenteQueryHandler(IContaRepository contaRepository)
    {
        _contaRepository = contaRepository;
    }

    public async Task<ContaResponse?> Handle(ObterContaCorrentePorIdQuery request, CancellationToken ct)
    {
        var conta = await _contaRepository.ObterPorIdAsync(request.Id);

        if (conta == null) return null;

        return new ContaResponse(
            conta.Id,
            conta.UsuarioId,
            conta.Saldo,
            DateTime.UtcNow
        );
    }
}