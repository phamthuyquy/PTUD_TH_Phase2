using Microsoft.EntityFrameworkCore;
using QTKD.Api.Domain;

namespace QTKD.Api.Infrastructure;

public sealed class QtkdDbContext(DbContextOptions<QtkdDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Contract> Contracts => Set<Contract>();

    public DbSet<PriceList> PriceLists => Set<PriceList>();

    public DbSet<PriceItem> PriceItems => Set<PriceItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(customer => customer.Id);
            entity.Property(customer => customer.Name).HasMaxLength(200).IsRequired();
            entity.Property(customer => customer.Status).HasMaxLength(30).IsRequired();
            entity.HasData(
                new Customer { Id = 1, Name = "Công ty Minh Phát", Status = "Active" },
                new Customer { Id = 2, Name = "Công ty An Bình", Status = "Active" });
        });

        modelBuilder.Entity<Contract>(entity =>
        {
            entity.ToTable("Contracts");
            entity.HasKey(contract => contract.Id);
            entity.Property(contract => contract.ContractNo).HasMaxLength(50).IsRequired();
            entity.Property(contract => contract.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(contract => contract.ContractNo).IsUnique();
            entity.HasOne(contract => contract.Customer)
                .WithMany()
                .HasForeignKey(contract => contract.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasData(new Contract
            {
                Id = 1,
                ContractNo = "HD-2026-018",
                CustomerId = 1,
                StartDate = new DateOnly(2026, 10, 1),
                EndDate = new DateOnly(2027, 9, 30),
                Status = "Draft"
            });
        });

        modelBuilder.Entity<PriceList>(entity =>
        {
            entity.ToTable("PriceLists");
            entity.HasKey(priceList => priceList.Id);
            entity.Property(priceList => priceList.Name).HasMaxLength(200).IsRequired();
            entity.HasData(new PriceList
            {
                Id = 1,
                Name = "Bảng giá dịch vụ 2026",
                EffectiveDate = new DateOnly(2026, 1, 1)
            });
        });

        modelBuilder.Entity<PriceItem>(entity =>
        {
            entity.ToTable("PriceItems");
            entity.HasKey(item => new { item.PriceListId, item.ServiceCode });
            entity.Property(item => item.ServiceCode).HasMaxLength(50).IsRequired();
            entity.Property(item => item.UnitPrice).HasPrecision(18, 2);
            entity.HasOne(item => item.PriceList)
                .WithMany(priceList => priceList.Items)
                .HasForeignKey(item => item.PriceListId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasData(
                new PriceItem { PriceListId = 1, ServiceCode = "PORT-001", UnitPrice = 1500000m },
                new PriceItem { PriceListId = 1, ServiceCode = "PORT-002", UnitPrice = 2250000m });
        });
    }
}
