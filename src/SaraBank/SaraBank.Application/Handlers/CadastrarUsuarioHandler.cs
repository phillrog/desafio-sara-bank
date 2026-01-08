using MediatR;
using SaraBank.Application.Commands;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Entities;
using SaraBank.Domain.Interfaces;

namespace SaraBank.Application.Handlers;

public class CadastrarUsuarioHandler : IRequestHandler<CadastrarUsuarioCommand, string>
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IContaRepository _contaRepository;
    private readonly IUnitOfWork _uow;

    public CadastrarUsuarioHandler(
        IUsuarioRepository usuarioRepository,
        IContaRepository contaRepository,
        IUnitOfWork uow)
    {
        _usuarioRepository = usuarioRepository;
        _contaRepository = contaRepository;
        _uow = uow;
    }

    public async Task<string> Handle(CadastrarUsuarioCommand request, CancellationToken ct)
    {
        var usuarioExistente = await _usuarioRepository.ObterPorCPFAsync(request.CPF);
        if (usuarioExistente != null)
        {
            throw new InvalidOperationException("Já existe um usuário cadastrado com este CPF no SARA Bank.");
        }

        return await _uow.ExecutarAsync(async () =>
        {
            var novoUsuario = new Usuario(request.Nome, request.CPF, request.Email);

            await _usuarioRepository.AdicionarAsync(novoUsuario);

            var novaConta = new ContaCorrente(novoUsuario.Id, request.SaldoInicial);

            await _contaRepository.AdicionarAsync(novaConta);
            
            var payload = $@"{{
                ""UsuarioId"": ""{novoUsuario.Id}"",
                ""Nome"": ""{novoUsuario.Nome}"",
                ""Email"": ""{novoUsuario.Email}"",
                ""ContaId"": ""{novaConta.Id}"",
                ""DataCriacao"": ""{DateTime.UtcNow:o}""
            }}";

            await _uow.AdicionarAoOutboxAsync(payload, "UsuarioCadastrado");

            return novoUsuario.Id.ToString();
        });
    }
}