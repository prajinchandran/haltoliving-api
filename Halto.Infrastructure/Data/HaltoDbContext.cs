using Halto.Domain.Entities;
using Halto.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Halto.Infrastructure.Data;

public class HaltoDbContext : DbContext
{
    public HaltoDbContext(DbContextOptions<HaltoDbContext> options) : base(options) { }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<MemberCategory> MemberCategories => Set<MemberCategory>();
    public DbSet<Due> Dues => Set<Due>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Only configure warnings — connection string comes from DI
        if (optionsBuilder.IsConfigured)
        {
            optionsBuilder.ConfigureWarnings(w =>
                w.Ignore(RelationalEventId.PendingModelChangesWarning));
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Organization
        modelBuilder.Entity<Organization>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.Name).HasMaxLength(200).IsRequired();
            e.Property(o => o.BusinessType).HasConversion<int>();
            e.HasIndex(o => o.Name);
        });

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).HasMaxLength(256).IsRequired();
            e.Property(u => u.FullName).HasMaxLength(200).IsRequired();
            e.Property(u => u.Phone).HasMaxLength(20);
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.Role).HasConversion<int>();
            e.HasIndex(u => u.Email).IsUnique();
            e.HasOne(u => u.Organization)
             .WithMany(o => o.Users)
             .HasForeignKey(u => u.OrganizationId)
             .OnDelete(DeleteBehavior.Restrict)
             .IsRequired(false);
        });

        // MemberCategory
        modelBuilder.Entity<MemberCategory>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
            e.Property(c => c.Description).HasMaxLength(500);
            e.Property(c => c.MonthlyRent).HasColumnType("decimal(18,2)");
            e.Property(c => c.AdmissionFee).HasColumnType("decimal(18,2)");
            e.Property(c => c.DepositAmount).HasColumnType("decimal(18,2)");
            e.HasOne(c => c.Organization)
             .WithMany(o => o.Categories)
             .HasForeignKey(c => c.OrganizationId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Member
        modelBuilder.Entity<Member>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.FullName).HasMaxLength(200).IsRequired();
            e.Property(m => m.Email).HasMaxLength(256);
            e.Property(m => m.Phone).HasMaxLength(20);
            e.Property(m => m.Designation).HasMaxLength(100);
            e.Property(m => m.IdDocumentUrl).HasMaxLength(500);
            e.Property(m => m.IdDocumentType).HasMaxLength(50);
            e.Property(m => m.DiscontinuedReason).HasMaxLength(500);
            e.Property(m => m.ExtraFieldsJson).HasColumnType("nvarchar(max)");
            e.HasOne(m => m.Organization)
             .WithMany(o => o.Members)
             .HasForeignKey(m => m.OrganizationId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.User)
             .WithOne(u => u.Member)
             .HasForeignKey<Member>(m => m.UserId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);
            e.HasOne(m => m.Category)
             .WithMany(c => c.Members)
             .HasForeignKey(m => m.CategoryId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);
        });

        // Due
        modelBuilder.Entity<Due>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Amount).HasColumnType("decimal(18,2)");
            e.Property(d => d.Status).HasConversion<int>();
            e.Property(d => d.Notes).HasMaxLength(500);
            // Enforce unique due per member per month/year
            e.HasIndex(d => new { d.MemberId, d.Year, d.Month }).IsUnique();
            e.HasOne(d => d.Organization)
             .WithMany(o => o.Dues)
             .HasForeignKey(d => d.OrganizationId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.Member)
             .WithMany(m => m.Dues)
             .HasForeignKey(d => d.MemberId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Payment
        modelBuilder.Entity<Payment>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.AmountPaid).HasColumnType("decimal(18,2)");
            e.Property(p => p.Method).HasConversion<int>();
            e.Property(p => p.Notes).HasMaxLength(500);
            e.HasOne(p => p.Organization)
             .WithMany(o => o.Payments)
             .HasForeignKey(p => p.OrganizationId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Member)
             .WithMany(m => m.Payments)
             .HasForeignKey(p => p.MemberId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Due)
             .WithMany(d => d.Payments)
             .HasForeignKey(p => p.DueId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.MarkedByUser)
             .WithMany()
             .HasForeignKey(p => p.MarkedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
