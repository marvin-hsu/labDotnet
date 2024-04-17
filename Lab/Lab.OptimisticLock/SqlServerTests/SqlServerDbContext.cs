using Microsoft.EntityFrameworkCore;

namespace Lab.OptimisticLock.SqlServerTests;

public class SqlServerDbContext(string connectionString) : DbContext
{
    public DbSet<LockedExampleRow> LockedExampleRow { get; set; }
    public DbSet<NotLockedExampleRow> NotLockedExampleRow { get; set; }
    
    public DbSet<ExampleRow> ExampleRow { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LockedExampleRow>(e =>
        {
            e.Property(p => p.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("NEWID()");
            
            e.Property(p => p.Name);

            e.Property(p => p.RowVersion)
                .IsRowVersion();
        });

        modelBuilder.Entity<NotLockedExampleRow>(e =>
        {
            e.Property(p => p.Id)
                .HasDefaultValueSql("NEWID()");

            e.Property(p => p.Name);
        });
        
        modelBuilder.Entity<ExampleRow>(e =>
        {
            e.Property(p => p.Id)
                .HasDefaultValueSql("NEWID()");

            e.Property(p => p.Name);

            e.Property(p => p.RowVersion)
                .IsRowVersion();
        });
    }
}

public class LockedExampleRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; }
}

public class NotLockedExampleRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ExampleRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; }
}