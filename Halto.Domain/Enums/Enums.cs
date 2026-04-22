namespace Halto.Domain.Enums;

public enum BusinessType
{
    Hostel = 1,
    Tuition = 2,
    Gym = 3,
    Other = 4
}

public enum UserRole
{
    SuperAdmin = 1,
    OrganizationOwner = 2,
    OrganizationStaff = 3,
    Member = 4
}

public enum DueStatus
{
    Due = 1,
    Partial = 2,
    Paid = 3
}

public enum PaymentMethod
{
    Manual = 1,
    Cash = 2,
    BankTransfer = 3,
    UPI = 4,
    Card = 5,
    Other = 6
}
