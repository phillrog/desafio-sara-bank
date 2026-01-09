using MediatR;
using Microsoft.AspNetCore.Mvc;
using SaraBank.API.DTOs;
using SaraBank.Application.Commands;

namespace SaraBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MovimentacaoController : ControllerBase
{
    private readonly IMediator _mediator;

    public MovimentacaoController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("deposito")]
    public async Task<IActionResult> RealizarDeposito([FromBody] SolicitarMovimentacaoRequest request)
    {
        var command = new SolicitarMovimentacaoCommand(request.ContaId, request.Valor, "Deposito");
        var resultado = await _mediator.Send(command);

        if (resultado)
            return Accepted(new { mensagem = "Solicitação de depósito enviada para processamento." });

        return BadRequest();
    }

    [HttpPost("saque")]
    public async Task<IActionResult> RealizarSaque([FromBody] SolicitarMovimentacaoRequest request)
    {
        var command = new SolicitarMovimentacaoCommand(request.ContaId, request.Valor, "Saque");
        var resultado = await _mediator.Send(command);

        if (resultado)
            return Accepted(new { mensagem = "Solicitação de saque enviada para processamento." });

        return BadRequest();
    }
}