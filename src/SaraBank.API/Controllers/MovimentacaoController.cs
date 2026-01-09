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

    [HttpPost("creditar")]
    public async Task<IActionResult> RealizarCredito([FromBody] SolicitarMovimentacaoRequest request)
    {
        var command = new SolicitarMovimentacaoCommand(request.ContaId, request.Valor, "Credito");
        var resultado = await _mediator.Send(command);

        if (resultado)
            return Accepted(new { mensagem = "Solicitação de crédito enviada para processamento." });

        return BadRequest();
    }

    [HttpPost("debitar")]
    public async Task<IActionResult> RealizarDebito([FromBody] SolicitarMovimentacaoRequest request)
    {
        var command = new SolicitarMovimentacaoCommand(request.ContaId, request.Valor, "Debito");
        var resultado = await _mediator.Send(command);

        if (resultado)
            return Accepted(new { mensagem = "Solicitação de débito enviada para processamento." });

        return BadRequest();
    }
}