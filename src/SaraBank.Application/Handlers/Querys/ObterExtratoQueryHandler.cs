using MediatR;
using SaraBank.Application.Queries;
using SaraBank.Domain.Entities;
using SaraBank.Domain.Interfaces;

namespace SaraBank.Application.Handlers.Queries;

public class ObterExtratoQueryHandler : IRequestHandler<ObterExtratoQuery, IEnumerable<Movimentacao>>
{
    private readonly IMovimentacaoRepository _movimentacaoRepository;

    public ObterExtratoQueryHandler(IMovimentacaoRepository movimentacaoRepository)
    {
        _movimentacaoRepository = movimentacaoRepository;
    }

    public async Task<IEnumerable<Movimentacao>> Handle(ObterExtratoQuery request, CancellationToken ct)
    {
        var movimentacoes = await _movimentacaoRepository.ObterPorContaIdAsync(request.contaId);

        return movimentacoes.OrderByDescending(m => m.Data);
    }
}