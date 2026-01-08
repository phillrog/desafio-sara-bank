using MediatR;
using Microsoft.AspNetCore.Mvc;
using SaraBank.Application.Commands;

namespace SaraBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContasController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContasController(IMediator mediator) => _mediator = mediator;

    [HttpPost("depositar")]
    public async Task<IActionResult> Depositar([FromBody] DepositarCommand command)
    {
        await _mediator.Send(command);
        return Ok(new { mensagem = "Depósito processado com sucesso!" });
    }

    [HttpPost("sacar")]
    public async Task<IActionResult> Sacar([FromBody] SacarCommand command)
    {
        await _mediator.Send(command);
        return Ok(new { mensagem = "Saque processado com sucesso!" });
    }

    [HttpPost("transferir")]
    public async Task<IActionResult> Transferir([FromBody] RealizarTransferenciaCommand command)
    {
        await _mediator.Send(command);
        return Ok(new { mensagem = "Transferência realizada com sucesso!" });
    }
}