using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore.Internal;
using Testcontainers.PostgreSql;

namespace Lab.OptimisticLock.PostgresTests;

public class PostgresTestFixture : IAsyncLifetime
{
    public PostgreSqlContainer DBContainer { get; private set; }

    public string GetConnectionString() => DBContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        DBContainer = new PostgreSqlBuilder()
            .WithDatabase("Test")
            .Build();
        await DBContainer.StartAsync();

        await using var context = new PostgresTestDbContext(DBContainer.GetConnectionString());
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await DBContainer.StopAsync();
    }
}