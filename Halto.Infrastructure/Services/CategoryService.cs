using Halto.Application.Common;
using Halto.Application.DTOs.Categories;
using Halto.Application.Interfaces;
using Halto.Domain.Entities;
using Halto.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halto.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly HaltoDbContext _db;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(HaltoDbContext db, ILogger<CategoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<CategoryDto>> CreateAsync(Guid organizationId, CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<CategoryDto>.Failure("Category name is required.");

        var category = new MemberCategory
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            MonthlyRent = request.MonthlyRent,
            AdmissionFee = request.AdmissionFee,
            DepositAmount = request.DepositAmount,
            OrganizationId = organizationId
        };

        _db.MemberCategories.Add(category);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Category created: {Name} in org: {OrgId}", category.Name, organizationId);

        return Result<CategoryDto>.Success(MapToDto(category, 0), 201);
    }

    public async Task<Result<List<CategoryDto>>> GetAllAsync(Guid organizationId)
    {
        var categories = await _db.MemberCategories
            .Where(c => c.OrganizationId == organizationId)
            .OrderBy(c => c.Name)
            .ToListAsync();

        var memberCounts = await _db.Members
            .Where(m => m.OrganizationId == organizationId && m.CategoryId != null)
            .GroupBy(m => m.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToListAsync();

        var result = categories.Select(c =>
        {
            var count = memberCounts.FirstOrDefault(x => x.CategoryId == c.Id)?.Count ?? 0;
            return MapToDto(c, count);
        }).ToList();

        return Result<List<CategoryDto>>.Success(result);
    }

    public async Task<Result<CategoryDto>> GetByIdAsync(Guid organizationId, Guid id)
    {
        var category = await _db.MemberCategories
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId);

        if (category is null)
            return Result<CategoryDto>.NotFound("Category not found.");

        var count = await _db.Members.CountAsync(m => m.CategoryId == id && m.IsActive);
        return Result<CategoryDto>.Success(MapToDto(category, count));
    }

    public async Task<Result<CategoryDto>> UpdateAsync(Guid organizationId, Guid id, UpdateCategoryRequest request)
    {
        var category = await _db.MemberCategories
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId);

        if (category is null)
            return Result<CategoryDto>.NotFound("Category not found.");

        if (!string.IsNullOrWhiteSpace(request.Name)) category.Name = request.Name.Trim();
        if (request.Description is not null) category.Description = request.Description.Trim();
        if (request.MonthlyRent.HasValue) category.MonthlyRent = request.MonthlyRent.Value;
        if (request.AdmissionFee.HasValue) category.AdmissionFee = request.AdmissionFee.Value;
        if (request.DepositAmount.HasValue) category.DepositAmount = request.DepositAmount.Value;
        if (request.IsActive.HasValue) category.IsActive = request.IsActive.Value;
        category.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        var count = await _db.Members.CountAsync(m => m.CategoryId == id && m.IsActive);
        return Result<CategoryDto>.Success(MapToDto(category, count));
    }

    public async Task<Result<bool>> DeleteAsync(Guid organizationId, Guid id)
    {
        var category = await _db.MemberCategories
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId);

        if (category is null)
            return Result<bool>.NotFound("Category not found.");

        var inUse = await _db.Members.AnyAsync(m => m.CategoryId == id && m.IsActive);
        if (inUse)
            return Result<bool>.Failure("Cannot delete category that has active members. Deactivate it instead.");

        _db.MemberCategories.Remove(category);
        await _db.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    private static CategoryDto MapToDto(MemberCategory c, int memberCount) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description,
        MonthlyRent = c.MonthlyRent,
        AdmissionFee = c.AdmissionFee,
        DepositAmount = c.DepositAmount,
        IsActive = c.IsActive,
        MemberCount = memberCount,
        CreatedAt = c.CreatedAt
    };
}
