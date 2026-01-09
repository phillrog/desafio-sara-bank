using MediatR;
using SaraBank.Application.Commands;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Entities;
using SaraBank.Domain.Interfaces;
using FluentValidation;

namespace SaraBank.Application.Handlers.Commands;

public class RealizarTransferenciaHandler : IRequestHandler<RealizarTransferenciaCommand, bool>
{
    private readonly IContaRepository _contaRepository;
    private readonly IMovimentacaoRepository _movimentacaoRepository; // Adicionado
    private readonly IUnitOfWork _unitOfWork;

    public RealizarTransferenciaHandler(
        IContaRepository contaRepository,
        IMovimentacaoRepository movimentacaoRepository,
        IUnitOfWork unitOfWork)
    {
        _contaRepository = contaRepository;
        _movimentacaoRepository = movimentacaoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(RealizarTransferenciaCommand request, CancellationToken ct)
    {        
        return await _unitOfWork.ExecutarAsync(async () =>
        {
            var origem = await _contaRepository.ObterPorIdAsync(request.ContaOrigemId);
            var destino = await _contaRepository.ObterPorIdAsync(request.ContaDestinoId);

            origem.Debitar(request.Valor);
            destino.Creditar(request.Valor);

            await _contaRepository.AtualizarAsync(origem);
            await _contaRepository.AtualizarAsync(destino);

            await _movimentacaoRepository.AdicionarAsync(new Movimentacao(origem.Id, request.Valor, "DEBITO", "Transferência"));
            await _movimentacaoRepository.AdicionarAsync(new Movimentacao(destino.Id, request.Valor, "CREDITO", "Recebido"));

            await _unitOfWork.AdicionarAoOutboxAsync("...", "TransferenciaRealizada");

            return true;
        });
    }
}