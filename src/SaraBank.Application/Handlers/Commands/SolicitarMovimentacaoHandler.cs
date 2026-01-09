using FluentValidation;
using FluentValidation.Results;
using MediatR;
using SaraBank.Application.Commands;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Interfaces;
using System.Text.Json;

namespace SaraBank.Application.Handlers.Commands;

public class SolicitarMovimentacaoHandler : IRequestHandler<SolicitarMovimentacaoCommand, bool>
{
    private readonly IUnitOfWork _uow;
    private readonly IContaRepository _contaRepository;

    public SolicitarMovimentacaoHandler(IUnitOfWork uow, IContaRepository contaRepository)
    {
        _uow = uow;
        _contaRepository = contaRepository;
    }

    public async Task<bool> Handle(SolicitarMovimentacaoCommand request, CancellationToken ct)
    {
        #region [ VALIDAÇÕES]

        if (request.Valor <= 0)
        {
            throw new ValidationException(new[] {
                new ValidationFailure("Valor", "O valor da movimentação deve ser superior a zero.")
            });
        }

        var conta = await _contaRepository.ObterPorIdAsync(request.ContaId);
        if (conta == null)
        {
            throw new ValidationException(new[] {
                new ValidationFailure("ContaId", "A conta informada não foi encontrada no SARA Bank.")
            });
        }
        #endregion

        return await _uow.ExecutarAsync(async () =>
        {
            var eventoIntegracao = new NovaMovimentacaoEvent(
                request.ContaId,
                request.Valor,
                request.Tipo,
                "Solicitação via API"
            );

            var envelope = new
            {
                TipoEvento = "NovaMovimentacao",
                Payload = JsonSerializer.Serialize(eventoIntegracao)
            };

            await _uow.AdicionarAoOutboxAsync(
                JsonSerializer.Serialize(envelope),
                "NovaMovimentacao"
            );

            return true;
        });
    }
}