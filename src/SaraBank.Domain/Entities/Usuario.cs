using Google.Cloud.Firestore;

namespace SaraBank.Domain.Entities
{
    [FirestoreData]
    public class Usuario
    {
        [FirestoreDocumentId]
        public Guid Id { get; private set; }

        [FirestoreProperty]
        public string Nome { get; private set; }

        [FirestoreProperty]
        public string Email { get; private set; }

        [FirestoreProperty]
        public string CPF { get; private set; }

        public Usuario() { }

        public Usuario(string nome, string cpf, string email)
        {
            Id = Guid.NewGuid();
            Nome = nome;
            CPF = cpf;
            Email = email;
        }
    }
}
