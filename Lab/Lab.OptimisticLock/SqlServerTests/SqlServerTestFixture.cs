using Testcontainers.MsSql;

namespace Lab.OptimisticLock.SqlServerTests;

public class SqlServerTestFixture: IAsyncLifetime
{
    private MsSqlContainer DBContainer { get; set; }

    public string GetConnectionString() => _connectionString;
    private string _connectionString = string.Empty;

    public async Task InitializeAsync()
    {
        DBContainer = new MsSqlBuilder()
            .Build();
        await DBContainer.StartAsync();

        _connectionString = DBContainer.GetConnectionString();

        await using var context = new SqlServerDbContext(DBContainer.GetConnectionString());
        // await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await DBContainer.StopAsync();
    }
}