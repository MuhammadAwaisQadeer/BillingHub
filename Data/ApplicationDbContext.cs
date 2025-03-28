using Client_Invoice_System.Components.Pages.Invoice_Pages;
using Client_Invoice_System.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Client_Invoice_System.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<OwnerProfile> Owners { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<ActiveClient> ActiveClients { get; set; }
    public DbSet<Resource> Resources { get; set; }
    public DbSet<ClientProfileCrossTable> ClientProfileCrosses { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<CountryCurrency> CountryCurrencies { get; set; }
    public DbSet<InvoiceItem> InvoiceItems { get; set; } 

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(ConvertFilterExpression(entityType.ClrType));
            }
        }


        // ✅ Invoice & InvoiceItem Relationship
        modelBuilder.Entity<InvoiceItem>()
           .HasOne(ii => ii.Invoice)
           .WithMany(i => i.InvoiceItems)
           .HasForeignKey(ii => ii.InvoiceId)
           .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InvoiceItem>()
            .HasOne(ii => ii.Resource)
            .WithMany()
            .HasForeignKey(ii => ii.ResourceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Invoice>()
            .Property(i => i.RemainingAmount)
            .HasComputedColumnSql("[TotalAmount] - [PaidAmount]");

        modelBuilder.Entity<CountryCurrency>().HasData(
            new CountryCurrency { Id = 1, CountryName = "United States", CurrencyName = "US Dollar", Symbol = "$", CurrencyCode = "USD" },
            new CountryCurrency { Id = 2, CountryName = "United Kingdom", CurrencyName = "Pound Sterling", Symbol = "£", CurrencyCode = "GBP" },
            new CountryCurrency { Id = 3, CountryName = "European Union", CurrencyName = "Euro", Symbol = "€", CurrencyCode = "EUR" },
            new CountryCurrency { Id = 4, CountryName = "Japan", CurrencyName = "Japanese Yen", Symbol = "¥", CurrencyCode = "JPY" },
            new CountryCurrency { Id = 5, CountryName = "India", CurrencyName = "Indian Rupee", Symbol = "₹", CurrencyCode = "INR" },
            new CountryCurrency { Id = 6, CountryName = "Canada", CurrencyName = "Canadian Dollar", Symbol = "C$", CurrencyCode = "CAD" },
            new CountryCurrency { Id = 7, CountryName = "Australia", CurrencyName = "Australian Dollar", Symbol = "A$", CurrencyCode = "AUD" }
        );

        // ✅ ActiveClient & Client Relationship
        modelBuilder.Entity<ActiveClient>()
            .HasOne(ac => ac.Client)
            .WithOne(c => c.ActiveClient)
            .HasForeignKey<ActiveClient>(ac => ac.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        // ✅ Client & Resources Relationship
        modelBuilder.Entity<Client>()
            .HasMany(c => c.Resources)
            .WithOne(r => r.Client)
            .HasForeignKey(r => r.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        // ✅ Employee & Resources Relationship
        modelBuilder.Entity<Employee>()
            .HasMany(e => e.Resources)
            .WithOne(r => r.Employee)
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        // ✅ Client Profile Cross Table (Many-to-Many)
        modelBuilder.Entity<ClientProfileCrossTable>()
            .HasKey(cpc => new { cpc.ClientId, cpc.EmployeeId });

        modelBuilder.Entity<ClientProfileCrossTable>()
            .HasOne(cpc => cpc.Client)
            .WithMany(c => c.ClientProfileCrosses)
            .HasForeignKey(cpc => cpc.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ClientProfileCrossTable>()
            .HasOne(cpc => cpc.Employee)
            .WithMany()
            .HasForeignKey(cpc => cpc.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        // ✅ CountryCurrency Relationships
        modelBuilder.Entity<Client>()
            .HasOne(c => c.CountryCurrency)
            .WithMany()
            .HasForeignKey(c => c.CountryCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OwnerProfile>()
            .HasOne(o => o.CountryCurrency)
            .WithMany()
            .HasForeignKey(o => o.CountryCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.CountryCurrency)
            .WithMany()
            .HasForeignKey(i => i.CountryCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Resource>()
            .HasOne(r => r.OwnerProfile)
            .WithMany(o => o.Resources)
            .HasForeignKey(r => r.OwnerProfileId)
            .OnDelete(DeleteBehavior.Restrict);




    }
    public override int SaveChanges()
    {
        HandleSoftDelete();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        HandleSoftDelete();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void HandleSoftDelete()
    {
        foreach (var entry in ChangeTracker.Entries()
                 .Where(e => e.State == EntityState.Deleted && e.Entity is ISoftDeletable))
        {
            entry.State = EntityState.Modified;
            ((ISoftDeletable)entry.Entity).IsDeleted = true;
        }
    }

    private static LambdaExpression ConvertFilterExpression(Type entityType)
    {
        var parameter = Expression.Parameter(entityType, "e");
        var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
        var condition = Expression.Equal(property, Expression.Constant(false));
        return Expression.Lambda(condition, parameter);
    }
}
