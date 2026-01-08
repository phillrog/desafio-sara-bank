using MediatR;
using Microsoft.AspNetCore.Mvc;
using SaraBank.Application.Queries;

[ApiController]
[Route("api/[controller]")]
public class ExtratosController : ControllerBase
{
    private readonly IMediator _mediator;
    public ExtratosController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{contaId}")]
    public async Task<IActionResult> ObterExtrato(string contaId)
    {
        var extrato = await _mediator.Send(new ObterExtratoQuery(contaId));
        return Ok(extrato);
    }
}