using Microsoft.EntityFrameworkCore;
using QTKD.Api.Domain;

namespace QTKD.Api.Infrastructure;

public sealed class QtkdDbContext(DbContextOptions<QtkdDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Contract> Contracts => Set<Contract>();

    public DbSet<PriceList> PriceLists => Set<PriceList>();

    public DbSet<PriceItem> PriceItems => Set<PriceItem>();

    public DbSet<ContractSubmission> ContractSubmissions => Set<ContractSubmission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(customer => customer.Id);
            entity.Property(customer => customer.Code).HasMaxLength(30).IsRequired();
            entity.Property(customer => customer.Name).HasMaxLength(200).IsRequired();
            entity.Property(customer => customer.TaxCode).HasMaxLength(30).IsRequired();
            entity.Property(customer => customer.Address).HasMaxLength(300).IsRequired();
            entity.Property(customer => customer.RepresentativeName).HasMaxLength(150).IsRequired();
            entity.Property(customer => customer.ContactInfo).HasMaxLength(200).IsRequired();
            entity.Property(customer => customer.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(customer => customer.Code).IsUnique();
            entity.HasIndex(customer => customer.TaxCode).IsUnique();
            entity.HasData(
                new Customer
                {
                    Id = 1,
                    Code = "CUS-001",
                    Name = "Công ty Minh Phát",
                    TaxCode = "0310000001",
                    Address = "TP. Hồ Chí Minh",
                    RepresentativeName = "Nguyễn Minh",
                    ContactInfo = "minhphat@example.test",
                    Status = "Active"
                },
                new Customer
                {
                    Id = 2,
                    Code = "CUS-002",
                    Name = "Công ty An Bình",
                    TaxCode = "0310000002",
                    Address = "TP. Hồ Chí Minh",
                    RepresentativeName = "Trần Bình",
                    ContactInfo = "anbinh@example.test",
                    Status = "Active"
                });
        });

        modelBuilder.Entity<Contract>(entity =>
        {
            entity.ToTable("Contracts");
            entity.HasKey(contract => contract.Id);
            entity.Property(contract => contract.ContractNo).HasMaxLength(50).IsRequired();
            entity.Property(contract => contract.ContractValue).HasPrecision(18, 2);
            entity.Property(contract => contract.PaymentTerms).HasMaxLength(1000);
            entity.Property(contract => contract.ServiceTerms).HasMaxLength(4000);
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
                ContractValue = 10000000m,
                PaymentTerms = "Thanh toán theo tháng",
                ServiceTerms = "Dịch vụ khai thác cảng theo thỏa thuận.",
                Status = "Draft"
            });
        });

        modelBuilder.Entity<PriceList>(entity =>
        {
            entity.ToTable("PriceLists");
            entity.HasKey(priceList => priceList.Id);
            entity.Property(priceList => priceList.Name).HasMaxLength(200).IsRequired();
            entity.HasOne<Contract>()
                .WithMany()
                .HasForeignKey(priceList => priceList.ContractId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasData(new PriceList
            {
                Id = 1,
                Name = "Bảng giá dịch vụ 2026",
                ContractId = 1,
                EffectiveDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 12, 31)
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

        modelBuilder.Entity<ContractSubmission>(entity =>
        {
            entity.ToTable("ContractSubmissions");
            entity.HasKey(submission => submission.Id);
            entity.Property(submission => submission.IdempotencyKey).HasMaxLength(100);
            entity.HasIndex(submission => submission.ContractId).IsUnique();
            entity.HasIndex(submission => submission.IdempotencyKey)
                .IsUnique()
                .HasFilter("\"IdempotencyKey\" IS NOT NULL");
            entity.HasOne<Contract>()
                .WithMany()
                .HasForeignKey(submission => submission.ContractId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
