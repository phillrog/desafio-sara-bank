using MediatR;
using SaraBank.Application.Commands;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Entities;
using SaraBank.Domain.Interfaces;

namespace SaraBank.Application.Handlers.Commands;

public class DepositarHandler : IRequestHandler<DepositarCommand, bool>
{
    private readonly IContaRepository _contaRepository;
    private readonly IMovimentacaoRepository _movimentacaoRepository;
    private readonly IUnitOfWork _uow;

    public DepositarHandler(IContaRepository contaRepository, IMovimentacaoRepository movimentacaoRepository, IUnitOfWork uow)
    {
        _contaRepository = contaRepository;
        _movimentacaoRepository = movimentacaoRepository;
        _uow = uow;
    }

    public async Task<bool> Handle(DepositarCommand request, CancellationToken ct)
    {
        return await _uow.ExecutarAsync(async () =>
        {
            var conta = await _contaRepository.ObterPorIdAsync(request.ContaId);

            conta.Depositar(request.Valor);
            await _contaRepository.AtualizarAsync(conta);

            var movimentacao = new Movimentacao(conta.Id, request.Valor, "CREDITO", "Depósito em conta");
            await _movimentacaoRepository.AdicionarAsync(movimentacao);

            await _uow.AdicionarAoOutboxAsync($"{{\"ContaId\":\"{conta.Id}\", \"Valor\":{request.Valor}}}", "DepositoRealizado");

            return true;
        });
    }
}