using Donut.Source;

namespace Donut.Integration
{
    public interface IFieldExtraRepository
    {
        FieldExtras Get(long id);
        FieldExtra GetByKey(long fieldExtrasId, string key);
    }
}