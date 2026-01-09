namespace SaraBank.Application.Events
{    
    public record EnvelopeEvent(string TipoEvento, string Payload);
}
