using MediatR;
using Microsoft.AspNetCore.Mvc;
using SaraBank.Application.Commands;
using SaraBank.Application.DTOs;
using SaraBank.Application.Queries;

namespace SaraBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ContasController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContasController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Obtém os detalhes de uma conta corrente pelo ID.
    /// </summary>
    /// <param name="id">Identificador único da conta.</param>
    /// <response code="200">Retorna os dados da conta corrente.</response>
    /// <response code="404">Conta não encontrada.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ContaResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var query = new ObterContaCorrentePorIdQuery(id);
        var resultado = await _mediator.Send(query);

        if (resultado == null)
            return NotFound(new { mensagem = "Conta não encontrada." });

        return Ok(resultado);
    }

    /// <summary>
    /// Solicita uma transferência entre contas.
    /// </summary>
    /// <remarks>
    /// A operação é assíncrona. O retorno 202 indica que a solicitação foi aceita e entrou na fila de processamento.
    /// </remarks>
    /// <response code="202">Solicitação aceita com sucesso.</response>
    /// <response code="400">Dados inválidos ou saldo insuficiente.</response>
    [HttpPost("transferir")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(bool), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Transferir([FromBody] RealizarTransferenciaCommand command)
    {
        var resultado = await _mediator.Send(command);

        if (resultado)
            return Accepted(new { mensagem = "Transferência enviada para processamento." });

        return BadRequest();
    }
}