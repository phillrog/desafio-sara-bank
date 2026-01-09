using Google.Cloud.Firestore;

namespace SaraBank.Infrastructure.Persistence.Converters
{
    public class GuidConverter : IFirestoreConverter<Guid>
    {
        public object ToFirestore(Guid value) => value.ToString();

        public Guid FromFirestore(object value)
        {
            return value switch
            {
                string s => Guid.Parse(s),
                _ => throw new ArgumentException($"Tipo inesperado para Guid: {value.GetType()}")
            };
        }
    }
}
