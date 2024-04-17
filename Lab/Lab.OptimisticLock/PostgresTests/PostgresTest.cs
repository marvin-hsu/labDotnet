using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lab.OptimisticLock.PostgresTests;

public class PostgresTest(PostgresTestFixture fixture) : IClassFixture<PostgresTestFixture>
{
    [Fact]
    public async Task EFCore_WithLock()
    {
        await using var contextOne = new PostgresTestDbContext(fixture.GetConnectionString());
        await using var contextTwo = new PostgresTestDbContext(fixture.GetConnectionString());

        var example = new LockedExampleRow { Name = "initial name"  };
        await contextOne.AddAsync(example);
        await contextOne.SaveChangesAsync();

        var exampleShadow = (await contextTwo.LockedExampleRow.FindAsync(example.Id))!;
        exampleShadow.Name = "change name";
        await contextTwo.SaveChangesAsync();

        var act = async () =>
        {
            example.Name = "expect concurrency";
            await contextOne.SaveChangesAsync();
        };
        
        await act.Should().ThrowExactlyAsync<DbUpdateConcurrencyException>();
    }
    
    [Fact]
    public async Task EFCore_NotLock()
    {
        await using var contextOne = new PostgresTestDbContext(fixture.GetConnectionString());
        await using var contextTwo = new PostgresTestDbContext(fixture.GetConnectionString());

        var example = new NotLockedExampleRow { Name = "initial name"  };
        await contextOne.AddAsync(example);
        await contextOne.SaveChangesAsync();

        var exampleShadow = (await contextTwo.NotLockedExampleRow.FindAsync(example.Id))!;
        exampleShadow.Name = "change name";
        await contextTwo.SaveChangesAsync();

        var act = async () =>
        {
            example.Name = "expect no concurrency";
            await contextOne.SaveChangesAsync();
        };
        
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Dapper_WithLock()
    {
        await using var connection = new NpgsqlConnection(fixture.GetConnectionString());

        const string insertSql = "INSERT INTO example_row (name) VALUES (@name) returning id;";

        var id = await connection.QueryFirstOrDefaultAsync<Guid>(insertSql,new { Name = "initial name" });

        const string findByIdSql = "select id, name, xmin as RowVersion from example_row where id = @Id";
        var row = (await connection.QueryFirstOrDefaultAsync<ExampleRow>(findByIdSql, new { Id = id }))!;
        
        const string updateWithoutLockSql = "update example_row set name = @Name where id = @Id";
        var affectRowCount = await connection.ExecuteAsync(updateWithoutLockSql, new { Name = "change name", Id = id });
        affectRowCount.Should().Be(1);

        const string updateWithLockSql = "update example_row set name = @Name where id = @Id and xmin = @RowVersion";
        affectRowCount = await connection.ExecuteAsync(updateWithLockSql, new { Name = "expect concurrency", Id = id, RowVersion = (int)row.RowVersion });
        affectRowCount.Should().Be(0);
    }
}