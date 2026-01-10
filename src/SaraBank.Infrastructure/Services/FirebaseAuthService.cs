using FirebaseAdmin.Auth;
using Microsoft.Extensions.Configuration;
using SaraBank.Domain.Interfaces;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace SaraBank.Infrastructure.Services;



public interface IFirebaseAuthWrapper
{
    Task DeleteUserAsync(string uid);
    Task CreateUserAsync(UserRecordArgs args);
}

public class FirebaseAuthWrapper : IFirebaseAuthWrapper
{
    public Task DeleteUserAsync(string uid) => FirebaseAuth.DefaultInstance.DeleteUserAsync(uid);
    public Task CreateUserAsync(UserRecordArgs args) => FirebaseAuth.DefaultInstance.CreateUserAsync(args);
}
public class FirebaseAuthService : IIdentityService
{
    private readonly IFirebaseAuthWrapper _authWrapper;
    private readonly HttpClient _httpClient;
    private readonly string _firebaseApiKey;
    public FirebaseAuthService(IFirebaseAuthWrapper authWrapper, HttpClient httpClient, IConfiguration config)
    {
        _authWrapper = authWrapper;
        _httpClient = httpClient;
        _firebaseApiKey = config["Firebase:ApiKey"];
    }
    public async Task CriarUsuarioAsync(Guid id, string email, string senha, string nome)
    {
        try
        {
            var userArgs = new UserRecordArgs
            {
                Uid = id.ToString(),
                Email = email,
                Password = senha,
                DisplayName = nome,
            };

            await _authWrapper.CreateUserAsync(userArgs);
        }
        catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.EmailAlreadyExists)
        {
            throw new Exception("Este e-mail já está sendo utilizado.");
        }
    }

    public async Task<string> AutenticarAsync(string email, string senha)
    {
        var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_firebaseApiKey}";

        var payload = new { email, password = senha, returnSecureToken = true };
        var response = await _httpClient.PostAsJsonAsync(url, payload);

        if (!response.IsSuccessStatusCode)
            throw new Exception("E-mail ou senha inválidos.");

        var data = await response.Content.ReadFromJsonAsync<FirebaseRestResponse>();
        return data.IdToken;
    }

    public async Task DeletarUsuarioAsync(Guid id) =>
        await _authWrapper.DeleteUserAsync(id.ToString());

    internal record FirebaseRestResponse(
        [property: JsonPropertyName("idToken")] string IdToken,
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("localId")] string LocalId
    );
}