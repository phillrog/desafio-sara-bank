using MediatR;
using SaraBank.Application.Commands;
using SaraBank.Domain.Interfaces;

namespace SaraBank.Application.Handlers.Commands;

public class LoginHandler : IRequestHandler<LoginCommand, string>
{
    private readonly IIdentityService _identityService;

    public LoginHandler(IIdentityService identityService) => _identityService = identityService;

    public async Task<string> Handle(LoginCommand request, CancellationToken ct)
    {
        return await _identityService.AutenticarAsync(request.Email, request.Senha);
    }
}