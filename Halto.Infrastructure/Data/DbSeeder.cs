using Halto.Domain.Entities;
using Halto.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halto.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(HaltoDbContext db, ILogger logger)
    {
        if (await db.Users.AnyAsync())
        {
            logger.LogInformation("Database already seeded. Skipping.");
            return;
        }

        logger.LogInformation("Seeding database...");

        // SuperAdmin
        var superAdmin = new User
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Email = "admin@halto.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            FullName = "Super Administrator",
            Role = UserRole.SuperAdmin,
            IsActive = true,
            OrganizationId = null
        };
        db.Users.Add(superAdmin);

        // // Organization
        // var org = new Organization
        // {
        //     Id = Guid.Parse("00000000-0000-0000-0000-000000000010"),
        //     Name = "Green Valley Hostel",
        //     BusinessType = BusinessType.Hostel,
        //     IsActive = true
        // };
        // db.Organizations.Add(org);

        // // Owner
        // var owner = new User
        // {
        //     Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
        //     Email = "owner@greenvalley.local",
        //     PasswordHash = BCrypt.Net.BCrypt.HashPassword("Owner@123"),
        //     FullName = "Rajesh Kumar",
        //     Role = UserRole.OrganizationOwner,
        //     IsActive = true,
        //     OrganizationId = org.Id
        // };
        // db.Users.Add(owner);

        // // Staff x2
        // var staff1 = new User
        // {
        //     Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
        //     Email = "staff1@greenvalley.local",
        //     PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@123"),
        //     FullName = "Priya Sharma",
        //     Role = UserRole.OrganizationStaff,
        //     IsActive = true,
        //     OrganizationId = org.Id
        // };
        // var staff2 = new User
        // {
        //     Id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
        //     Email = "staff2@greenvalley.local",
        //     PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@123"),
        //     FullName = "Amit Patel",
        //     Role = UserRole.OrganizationStaff,
        //     IsActive = true,
        //     OrganizationId = org.Id
        // };
        // db.Users.AddRange(staff1, staff2);

        // // Members x3
        // var member1 = new Member
        // {
        //     Id = Guid.Parse("00000000-0000-0000-0000-000000000020"),
        //     FullName = "Suresh Reddy",
        //     Email = "suresh@example.com",
        //     Phone = "9876543210",
        //     OrganizationId = org.Id,
        //     ExtraFieldsJson = """{"room":"101","bed":"A","monthlyRent":6000,"advance":12000,"deposit":5000}"""
        // };
        // var member2 = new Member
        // {
        //     Id = Guid.Parse("00000000-0000-0000-0000-000000000021"),
        //     FullName = "Neha Singh",
        //     Email = "neha@example.com",
        //     Phone = "9876543211",
        //     OrganizationId = org.Id,
        //     ExtraFieldsJson = """{"room":"102","bed":"B","monthlyRent":5500,"advance":11000,"deposit":4500}"""
        // };
        // var member3 = new Member
        // {
        //     Id = Guid.Parse("00000000-0000-0000-0000-000000000022"),
        //     FullName = "Vikram Joshi",
        //     Email = "vikram@example.com",
        //     Phone = "9876543212",
        //     OrganizationId = org.Id,
        //     ExtraFieldsJson = """{"room":"103","bed":"A","monthlyRent":6500,"advance":13000,"deposit":5500}"""
        // };
        // db.Members.AddRange(member1, member2, member3);

        // await db.SaveChangesAsync();

        // // Sample dues for current month
        // var now = DateTime.UtcNow;
        // var due1 = new Due
        // {
        //     Id = Guid.NewGuid(),
        //     OrganizationId = org.Id,
        //     MemberId = member1.Id,
        //     Year = now.Year, Month = now.Month,
        //     Amount = 6000, Status = DueStatus.Paid
        // };
        // var due2 = new Due
        // {
        //     Id = Guid.NewGuid(),
        //     OrganizationId = org.Id,
        //     MemberId = member2.Id,
        //     Year = now.Year, Month = now.Month,
        //     Amount = 5500, Status = DueStatus.Partial
        // };
        // var due3 = new Due
        // {
        //     Id = Guid.NewGuid(),
        //     OrganizationId = org.Id,
        //     MemberId = member3.Id,
        //     Year = now.Year, Month = now.Month,
        //     Amount = 6500, Status = DueStatus.Due
        // };
        // db.Dues.AddRange(due1, due2, due3);
        // await db.SaveChangesAsync();

        // // Sample payments
        // var pay1 = new Payment
        // {
        //     OrganizationId = org.Id, MemberId = member1.Id, DueId = due1.Id,
        //     AmountPaid = 6000, PaidOn = now.AddDays(-5),
        //     Method = PaymentMethod.Cash, MarkedByUserId = staff1.Id,
        //     Notes = "Full payment received"
        // };
        // var pay2 = new Payment
        // {
        //     OrganizationId = org.Id, MemberId = member2.Id, DueId = due2.Id,
        //     AmountPaid = 3000, PaidOn = now.AddDays(-3),
        //     Method = PaymentMethod.UPI, MarkedByUserId = owner.Id,
        //     Notes = "Partial payment"
        // };
        // db.Payments.AddRange(pay1, pay2);
        await db.SaveChangesAsync();

        logger.LogInformation("Seeding complete.");
    }
}
