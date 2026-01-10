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
    private readonly IOutboxRepository _outboxRepository;

    public CadastrarUsuarioHandler(
        IUsuarioRepository usuarioRepository,
        IContaRepository contaRepository,
        IUnitOfWork uow,
        IOutboxRepository outboxRepository)
    {
        _usuarioRepository = usuarioRepository;
        _contaRepository = contaRepository;
        _uow = uow;
        _outboxRepository = outboxRepository;
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

            var novaConta = new ContaCorrente(novoUsuario.Id, 0); // Deve iniciar com zero se não haverá duplicidade
            await _contaRepository.AdicionarAsync(novaConta);

            var evento = new UsuarioCadastradoEvent
            (
                UsuarioId: novoUsuario.Id,
                Nome: novoUsuario.Nome,
                Email: novoUsuario.Email,
                ContaId: novaConta.Id,
                DataCriacao: DateTime.UtcNow,
                SaldoInicial: request.SaldoInicial
            );

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            var envelope = new
            {
                TipoEvento = "UsuarioCadastrado",
                Payload = JsonSerializer.Serialize(evento, options)
            };

            var payload = JsonSerializer.Serialize(envelope, options);

            var outboxMessage = new OutboxMessage(
                Guid.NewGuid(),
                payload,
                "UsuarioCadastrado",
                "sara-bank-usuarios"
            );

            await _outboxRepository.AdicionarAsync(outboxMessage, ct);

            return novoUsuario.Id.ToString();
        });
    }
}