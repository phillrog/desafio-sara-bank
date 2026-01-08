namespace SaraBank.Domain.Entities
{
    public class Usuario
    {
        public Guid Id { get; private set; }
        public string Nome { get; private set; }
        public string CPF { get; private set; }
        public string Email { get; private set; }

        public Usuario(string nome, string cpf, string email)
        {
            Id = Guid.NewGuid();
            Nome = nome;
            CPF = cpf;
            Email = email;
        }
    }
}
