using ClipboardManager.Data.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ClipboardManager.Data.Tests;

/// <summary>
/// In-memory SQLite context factory for isolated unit tests.
/// Keeps the in-memory connection open across multiple DbContext instances.
/// </summary>
public sealed class TestDbContextFactory : IDbContextFactory<ClipboardDbContext>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ClipboardDbContext> _options;

    public TestDbContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<ClipboardDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var initContext = new ClipboardDbContext(_options);
        initContext.Database.EnsureCreated();
    }

    public ClipboardDbContext CreateDbContext()
    {
        return new ClipboardDbContext(_options);
    }

    public Task<ClipboardDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateDbContext());
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}
