namespace nORM.Models.Data;

public class DatabaseColumn
{
    public string Name { get; set; }
    public string Type { get; set; }

    public DatabaseColumn()
    {
        Name = string.Empty;
        Type = string.Empty;
    }

    public DatabaseColumn(string name, string type)
    {
        Name = name;
        Type = type;
    }
}