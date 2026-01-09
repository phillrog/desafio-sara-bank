namespace SaraBank.API.DTOs
{
    public record SolicitarMovimentacaoRequest(Guid ContaId, decimal Valor);
}
