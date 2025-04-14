# nORM - A Lightweight .NET ORM Library

nORM is a lightweight, simple, and fast Object-Relational Mapping (ORM) library for .NET applications. It provides an easy way to interact with different database systems without the complexity of larger ORM frameworks.

> **Note**: This README is AI-generated and updated as the project evolves. It aims to keep documentation in sync with the rapidly changing codebase and emerging ideas during active development.

## Features

- **Lightweight Design**: Minimalist implementation with low memory footprint
- **Provider-Agnostic Architecture**: Designed to work with both SQL and NoSQL databases through a unified API
- **Multi-database Support**: Currently supports SQLite and MySQL databases with more providers coming soon
- **Statically Typed**: Maintain strong typing across all database providers
- **Fluent Configuration API**: Simple, chainable API for database connections
- **Automatic Table Management**: Creates and maintains database tables based on your entity model
- **CRUD Operations**: Simple API for Create, Read, Update, and Delete operations
- **Transaction Support**: Manage database transactions with ease
- **Expression-Based Querying**: Find data using lambda expressions
- **Schema Evolution**: Handles schema changes and column modifications
- **Performance Focused**: Optimized for high performance with minimal allocations

## Getting Started

### Installation

Will be available on NuGet soon, once it is fully tested and documented. For now, you can clone the repository and build the project to get the library. You can also add the project as a submodule to your existing project.

### Basic Usage

#### 1. Define your entity model:

```csharp
using nORM.Attributes;
using nORM.Models;

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
```

#### 2. Establish a database connection:

```csharp
// For SQLite
var sqliteConnection = new NormConnectionBuilder(DatabaseProviderType.Sqlite)
    .SetExplicitDataSource("database.sqlite")
    .BuildAndConnect();

// For MySQL
var mysqlConnection = new NormConnectionBuilder(DatabaseProviderType.MySql)
    .SetHostname("localhost")
    .SetUsername("username")
    .SetPassword("password")
    .SetDatabase("mydatabase")
    .BuildAndConnect();

// For in-memory SQLite
var inMemoryConnection = new NormConnectionBuilder(DatabaseProviderType.Sqlite)
    .UseInMemoryDataSource()
    .BuildAndConnect();
```

#### 3. Perform database operations:

```csharp
// Get a collection context
var postCollection = connection.Collection<Post>();

// Insert a new record
var post = new Post
{
    Title = "Hello nORM",
    Description = "My first post with nORM",
    AuthorId = 1,
    CreatedAt = DateTime.UtcNow
};

var insertedPost = postCollection.Insert(post);

// Find a post by ID
var fetchedPost = postCollection.FindOne(p => p.Id == insertedPost.Id);

// Remove a post
var result = postCollection.Remove(fetchedPost);

// Using transactions
using (var transaction = postCollection.BeginTransaction())
{
    try
    {
        // Perform multiple operations
        postCollection.Insert(post1, transaction);
        postCollection.Insert(post2, transaction);
        
        // Commit when done
        transaction.Commit();
    }
    catch
    {
        // Transaction will be automatically rolled back if not committed
    }
}
```

## Advanced Features

### Bulk Operations

```csharp
// Insert multiple entities
var posts = new List<Post> { post1, post2, post3 };
var insertedPosts = postCollection.InsertMany(posts);

// Truncate a table
var truncated = postCollection.Truncate();
```

## Performance Considerations

nORM is designed to be lightweight and efficient. The library includes features to minimize memory allocations and GC pressure:

- Connection pooling
- Optimized query generation
- Efficient entity tracking

When working with large datasets, consider using transactions for bulk operations to improve performance.

## Supported Types

The following .NET types are currently supported:

- `int` / `INT` (database)
- `string` / `TEXT` or `VARCHAR` (database)
- `bool` / `BOOLEAN` or `TINYINT` (database)
- `DateTime` / `TEXT` or `DATETIME` (database)
- `double` / `REAL` or `DOUBLE` (database)

## Database Providers

### Current Support
- **SQLite**: File-based and in-memory database support
- **MySQL**: Production-ready implementation

### Upcoming Providers
nORM is designed with a provider-agnostic architecture that will allow for easy integration with various database technologies while maintaining strong static typing:

- **MongoDB**: Support for document-based NoSQL storage with type-safe entity mapping
- **PostgreSQL**: Enterprise-grade relational database
- **Microsoft SQL Server**: Integration with Microsoft's database solution
- **Redis**: For high-performance caching scenarios
- **CosmosDB**: Microsoft's globally distributed multi-model database service

The provider architecture allows you to seamlessly switch between different databases without changing your application code, making it easy to transition between development, testing, and production environments with different database backends.

```csharp
// Example of provider swapping (current implementation)
// SQLite for development
var devConnection = new NormConnectionBuilder(DatabaseProviderType.Sqlite)
    .UseInMemoryDataSource()
    .BuildAndConnect();
    
// MySQL for production
var prodConnection = new NormConnectionBuilder(DatabaseProviderType.MySql)
    .SetHostname("production-server")
    .SetUsername("prod-user")
    .SetPassword("secure-password")
    .SetDatabase("production-db")
    .BuildAndConnect();

// Future NoSQL support (maintaining static typing)
// var mongoConnection = new NormConnectionBuilder(DatabaseProviderType.MongoDB)
//    .SetConnectionString("mongodb://localhost:27017")
//    .SetDatabase("my-documents")
//    .BuildAndConnect();
//
// // Still use your statically typed entity classes
// var postsCollection = mongoConnection.Collection<Post>();
```

## Project Status

nORM is currently in active development. Future plans include:

- Support for additional database providers including MongoDB, PostgreSQL, and SQL Server
- NoSQL database integration with strong static typing
- Enhanced type mapping for complex property types
- More complex querying capabilities with advanced filtering and sorting
- Async API support for better performance in web applications
- Migration tools for easy schema updates
- Documentation improvements
- Cross-platform testing and validation

## License

This project is licensed under a custom license that allows free use, modification, and distribution but restricts selling the software as a standalone product. See the LICENSE file for details.