using MediatR;
using Microsoft.AspNetCore.Mvc;
using SaraBank.Application.Commands;

namespace SaraBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsuariosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Realiza o cadastro de um novo usuário e abre uma conta automaticamente.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Cadastrar([FromBody] CadastrarUsuarioCommand command)
    {
        // O Handler gerencia a criação do usuário e da conta na mesma transação
        var usuarioId = await _mediator.Send(command);

        return CreatedAtAction(nameof(Cadastrar), new { id = usuarioId }, new
        {
            mensagem = "Usuário cadastrado e conta aberta com sucesso!",
            id = usuarioId
        });
    }
}