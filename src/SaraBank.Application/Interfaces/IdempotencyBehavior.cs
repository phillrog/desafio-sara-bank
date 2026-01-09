using MediatR;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Interfaces;

public class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IIdempotentCommand
{
    private readonly IIdempotencyRepository _repository;

    public IdempotencyBehavior(IIdempotencyRepository repository)
    {
        _repository = repository;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (await _repository.ChaveJaExisteAsync(request.RequestId))
        {
            throw new InvalidOperationException($"Esta operação (ID: {request.RequestId}) já foi processada.");
        }

        var response = await next();
        await _repository.SalvarChaveAsync(request.RequestId, typeof(TRequest).Name);

        return response;
    }
}