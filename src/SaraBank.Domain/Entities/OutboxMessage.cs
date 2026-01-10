using Google.Cloud.Firestore;

namespace SaraBank.Domain.Entities
{
    [FirestoreData]
    public class OutboxMessage
    {
        [FirestoreDocumentId]
        public Guid Id { get; private set; }

        [FirestoreProperty]
        public string Payload { get; private set; }

        [FirestoreProperty]
        public string Tipo { get; private set; }

        [FirestoreProperty]
        public bool Processado { get; private set; }

        [FirestoreProperty]
        public int Tentativas { get; private set; }

        [FirestoreProperty]
        public DateTime CriadoEm { get; private set; }

        [FirestoreProperty]
        public string Topico { get; private set; }

        public OutboxMessage() { }

        public OutboxMessage(Guid id, string payload, string tipo, string topico, int tentativas, bool processado, DateTime criadoEm)
        {
            Id = id;
            Payload = payload;
            Tipo = tipo;
            Topico = topico; 
            Tentativas = tentativas;
            Processado = processado;
            CriadoEm = criadoEm;
        }

        public OutboxMessage(Guid id, string payload, string tipo, string topico)
        {
            Id = id;
            Payload = payload;
            Tipo = tipo;
            Topico = topico;
            Tentativas = 0;
            Processado = false;
            CriadoEm = DateTime.UtcNow;
        }
        public void MarcarComoProcessado() => Processado = true;

        public void IncrementarFalha() => Tentativas++;
    }
}
