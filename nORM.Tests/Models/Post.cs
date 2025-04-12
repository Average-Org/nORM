using nORM.Attributes;
using nORM.Models;

namespace nORM.Tests.Models;

[CollectionName("Posts")]
public class Post : NormEntity
{
    [PrimaryKey]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("title")]
    public string Title { get; set; }
    
    [Column("description")]
    public string Description { get; set; }
    
    [Column("author_id")]
    public int AuthorId { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}