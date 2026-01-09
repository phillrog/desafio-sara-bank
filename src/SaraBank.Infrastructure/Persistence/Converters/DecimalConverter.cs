using Google.Cloud.Firestore;

namespace SaraBank.Infrastructure.Persistence.Converters;

public class DecimalConverter : IFirestoreConverter<decimal>
{
    public object ToFirestore(decimal value) => (double)value;    
    public decimal FromFirestore(object value)
    {
        return value switch
        {
            double d => (decimal)d,
            long l => (decimal)l,
            float f => (decimal)f,
            _ => 0m
        };
    }
}