using nORM.Attributes;

namespace nORM.Models;

public abstract class NormEntity
{
    internal virtual void SetId(int id)
    {
        var primaryKey = GetType().GetProperties().FirstOrDefault(p => p.GetCustomAttributes(typeof(PrimaryKeyAttribute), true).Any());
        
        if (primaryKey != null)
        {
            primaryKey.SetValue(this, id);
        }
    }
}