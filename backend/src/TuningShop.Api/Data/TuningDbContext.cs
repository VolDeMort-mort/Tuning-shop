using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TuningShop.Api.Domain;

namespace TuningShop.Api.Data;

public sealed class TuningDbContext(DbContextOptions<TuningDbContext> options) : DbContext(options)
{
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<StatusChange> StatusChanges => Set<StatusChange>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // Усі моменти часу зберігаються в UTC; після читання з SQLite повертаємо Kind = Utc,
        // щоб у відповіді API час мав кінцеве Z.
        builder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        builder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
        builder.Properties<OrderStatus>().HaveConversion<string>().HaveMaxLength(20);
        builder.Properties<WorkStatus>().HaveConversion<string>().HaveMaxLength(20);
        builder.Properties<PartStatus>().HaveConversion<string>().HaveMaxLength(20);
        builder.Properties<WorkCategory>().HaveConversion<string>().HaveMaxLength(20);
        builder.Properties<Role>().HaveConversion<string>().HaveMaxLength(20);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Client>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            e.Property(x => x.FullNameSearch).HasMaxLength(150).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(13).IsRequired();
            e.Property(x => x.Email).HasMaxLength(254);
            e.HasIndex(x => x.Phone).IsUnique();
            e.HasIndex(x => x.FullNameSearch);
            e.HasMany(x => x.Cars).WithOne(x => x.Client).HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Car>(e =>
        {
            e.Property(x => x.Make).HasMaxLength(60).IsRequired();
            e.Property(x => x.Model).HasMaxLength(60).IsRequired();
            e.Property(x => x.Vin).HasMaxLength(17).IsRequired();
            e.Property(x => x.PlateNumber).HasMaxLength(12).IsRequired();
            e.HasIndex(x => x.Vin).IsUnique();
            e.HasIndex(x => x.PlateNumber);
            e.HasMany(x => x.Orders).WithOne(x => x.Car).HasForeignKey(x => x.CarId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Order>(e =>
        {
            e.Property(x => x.Number).HasMaxLength(20).IsRequired();
            e.Property(x => x.ClientRequest).HasMaxLength(2000);
            e.Property(x => x.CancelReason).HasMaxLength(500);
            e.HasIndex(x => x.Number).IsUnique();
            e.HasIndex(x => x.Status);
            // Композиція: роботи й історія не існують окремо від замовлення.
            e.HasMany(x => x.WorkItems).WithOne(x => x.Order).HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.StatusChanges).WithOne().HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<WorkItem>(e =>
        {
            e.Property(x => x.Description).HasMaxLength(1000).IsRequired();
            e.HasOne(x => x.Master).WithMany().HasForeignKey(x => x.MasterId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Parts).WithOne(x => x.WorkItem).HasForeignKey(x => x.WorkItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Part>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        b.Entity<Employee>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            e.Property(x => x.Login).HasMaxLength(50).IsRequired();
            e.Property(x => x.PasswordHash).IsRequired();
            e.HasIndex(x => x.Login).IsUnique();
        });

        b.Entity<StatusChange>(e =>
        {
            e.Property(x => x.Comment).HasMaxLength(500);
            e.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
}
