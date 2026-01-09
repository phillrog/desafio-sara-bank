
namespace SaraBank.Domain.Interfaces
{
    public interface IIdempotentCommand
    {
        Guid RequestId { get; }
    }
}
