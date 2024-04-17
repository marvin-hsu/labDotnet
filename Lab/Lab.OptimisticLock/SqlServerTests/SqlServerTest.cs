using Dapper;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Lab.OptimisticLock.SqlServerTests;

public class SqlServerTest(SqlServerTestFixture fixture) : IClassFixture<SqlServerTestFixture>
{
    [Fact]
    public async Task EFCore_WithLock()
    {
        await using var contextOne = new SqlServerDbContext(fixture.GetConnectionString());
        await using var contextTwo = new SqlServerDbContext(fixture.GetConnectionString());

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
        await using var contextOne = new SqlServerDbContext(fixture.GetConnectionString());
        await using var contextTwo = new SqlServerDbContext(fixture.GetConnectionString());

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
        await using var connection = new SqlConnection(fixture.GetConnectionString());

        const string insertSql = "INSERT INTO ExampleRow (Name) OUTPUT INSERTED.Id VALUES (@Name);";

        var id = await connection.ExecuteScalarAsync<Guid>(insertSql,new { Name = "initial name" });

        const string findByIdSql = "SELECT Id, Name, RowVersion FROM ExampleRow WHERE Id = @Id";
        var row = (await connection.QueryFirstOrDefaultAsync<ExampleRow>(findByIdSql, new { Id = id }))!;
        
        const string updateWithoutLockSql = "UPDATE ExampleRow SET Name = @Name WHERE Id = @Id";
        var affectRowCount = await connection.ExecuteAsync(updateWithoutLockSql, new { Name = "change name", Id = id });
        affectRowCount.Should().Be(1);

        const string updateWithLockSql = "UPDATE ExampleRow SET Name = @Name WHERE Id = @Id AND RowVersion = @RowVersion";
        affectRowCount = await connection.ExecuteAsync(updateWithLockSql, new { Name = "expect concurrency", Id = id, RowVersion = row.RowVersion });
        affectRowCount.Should().Be(0);
    }
}