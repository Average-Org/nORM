using nORM.Attributes;
using nORM.Models;

namespace nORM.Tests.Models;

[CollectionName("Users")]
public class User : NormEntity
{
    [PrimaryKey]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("name")]
    public string Name { get; set; }
    
    [Column("email")]
    public string Email { get; set; }
}