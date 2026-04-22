namespace Halto.Application.DTOs.Categories;

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal AdmissionFee { get; set; }
    public decimal DepositAmount { get; set; }
}

public class UpdateCategoryRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? MonthlyRent { get; set; }
    public decimal? AdmissionFee { get; set; }
    public decimal? DepositAmount { get; set; }
    public bool? IsActive { get; set; }
}

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal AdmissionFee { get; set; }
    public decimal DepositAmount { get; set; }
    public bool IsActive { get; set; }
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
