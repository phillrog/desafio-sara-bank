using MediatR;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> RealizarDeposito([FromBody] CriarMovimentacaoCommand command)
    {
        command.Tipo = "Deposito";

        var resultado = await _mediator.Send(command);

        if (resultado)
            return Ok(new { mensagem = "Depósito processado com sucesso!" });

        return BadRequest(new { mensagem = "Não foi possível processar o depósito." });
    }

    [HttpPost("saque")]
    public async Task<IActionResult> RealizarSaque([FromBody] CriarMovimentacaoCommand command)
    {
        command.Tipo = "Saque";

        var resultado = await _mediator.Send(command);

        if (resultado)
            return Ok(new { mensagem = "Saque realizado com sucesso!" });

        return BadRequest(new { mensagem = "Falha no saque. Verifique o saldo ou a conta." });
    }
}