namespace SaraBank.Domain.Entities;

public class OutboxMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TipoEvento { get; set; }
    public string Conteudo { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public bool Processado { get; set; } = false;
}