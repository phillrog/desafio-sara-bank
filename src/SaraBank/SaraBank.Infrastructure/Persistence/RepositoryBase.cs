using Google.Cloud.Firestore;
using SaraBank.Infrastructure.Persistence;

namespace SaraBank.Infrastructure.Repositories;

public abstract class RepositoryBase
{
    protected readonly IUnitOfWork _uow;
    protected readonly FirestoreDb _db;

    protected RepositoryBase(IUnitOfWork uow, FirestoreDb db)
    {
        _uow = uow;
        _db = db;
    }
}