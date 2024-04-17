using Microsoft.EntityFrameworkCore;

namespace Lab.OptimisticLock.PostgresTests;

public class PostgresTestDbContext(string connectionString) : DbContext
{
    public DbSet<LockedExampleRow> LockedExampleRow { get; set; }
    public DbSet<NotLockedExampleRow> NotLockedExampleRow { get; set; }
    
    public DbSet<ExampleRow> ExampleRow { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LockedExampleRow>(e =>
        {
            e.ToTable("locked_example_row");
            e.Property(p => p.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");
            
            e.Property(p => p.Name)
                .HasColumnName("name");

            e.Property(p => p.RowVersion)
                .IsRowVersion();
        });

        modelBuilder.Entity<NotLockedExampleRow>(e =>
        {
            e.ToTable("not_locked_example_row");
            e.Property(p => p.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            e.Property(p => p.Name)
                .HasColumnName("name");
        });
        
        modelBuilder.Entity<ExampleRow>(e =>
        {
            e.ToTable("example_row");
            e.Property(p => p.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            e.Property(p => p.Name)
                .HasColumnName("name");

            e.Property(p => p.RowVersion)
                .IsRowVersion();
        });
    }
}

public class LockedExampleRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public uint RowVersion { get; set; }
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
    public uint RowVersion { get; set; }
}