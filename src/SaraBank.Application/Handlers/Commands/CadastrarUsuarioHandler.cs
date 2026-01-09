using FluentValidation;
using FluentValidation.Results;
using MediatR;
using SaraBank.Application.Commands;
using SaraBank.Application.Events;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Entities;
using SaraBank.Domain.Interfaces;
using System.Text.Json;

namespace SaraBank.Application.Handlers.Commands;

public class CadastrarUsuarioHandler : IRequestHandler<CadastrarUsuarioCommand, string>
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IContaRepository _contaRepository;
    private readonly IUnitOfWork _uow;
    private readonly IMovimentacaoRepository _movimentacaoRepository;

    public CadastrarUsuarioHandler(
        IUsuarioRepository usuarioRepository,
        IContaRepository contaRepository,
        IUnitOfWork uow,
        IMovimentacaoRepository movimentacaoRepository)
    {
        _usuarioRepository = usuarioRepository;
        _contaRepository = contaRepository;
        _uow = uow;
        _movimentacaoRepository = movimentacaoRepository;
    }

    public async Task<string> Handle(CadastrarUsuarioCommand request, CancellationToken ct)
    {
        var usuarioExistente = await _usuarioRepository.ObterPorCPFAsync(request.CPF);
        if (usuarioExistente != null)
        {
            throw new ValidationException(new[] {new ValidationFailure("CPF", "Este CPF já está cadastrado no SARA Bank.")});
        }

        return await _uow.ExecutarAsync(async () =>
        {
            var novoUsuario = new Usuario(request.Nome, request.CPF, request.Email);
            await _usuarioRepository.AdicionarAsync(novoUsuario);

            var novaConta = new ContaCorrente(novoUsuario.Id, request.SaldoInicial);
            await _contaRepository.AdicionarAsync(novaConta);

            var evento = new
            {
                UsuarioId = novoUsuario.Id,
                novoUsuario.Nome,
                novoUsuario.Email,
                ContaId = novaConta.Id,
                DataCriacao = DateTime.UtcNow
            };

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            var envelope = new
            {
                TipoEvento = "UsuarioCadastrado",
                Payload = JsonSerializer.Serialize(evento, options)
            };

            var payload = JsonSerializer.Serialize(envelope, options);
            
            // Persistência Atômica no Outbox
            await _uow.AdicionarAoOutboxAsync(payload, "UsuarioCadastrado");

            if (request.SaldoInicial > 0)
            {
                var movimentacaoInicial = new Movimentacao(
                    novaConta.Id,
                    request.SaldoInicial,
                    "Credito",
                    "Depósito Inicial"
                );

                await _movimentacaoRepository.AdicionarAsync(movimentacaoInicial);

                var envelopeMov = new
                {
                    TipoEvento = "MovimentacaoRealizada",
                    Payload = JsonSerializer.Serialize(new MovimentacaoRealizadaEvent(novaConta.Id, request.SaldoInicial, "Depósito Inicial"), options)
                };

                await _uow.AdicionarAoOutboxAsync(JsonSerializer.Serialize(envelopeMov), "MovimentacaoRealizada");
            }

            return novoUsuario.Id.ToString();
        });
    }
}