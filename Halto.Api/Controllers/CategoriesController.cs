using Halto.Application.DTOs.Categories;
using Halto.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halto.Api.Controllers;

[Route("api/categories")]
[Authorize(Roles = "OrganizationOwner,OrganizationStaff")]
public class CategoriesController : HaltoControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>List all categories for this organisation</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _categoryService.GetAllAsync(CurrentOrgId);
        return ToActionResult(result);
    }

    /// <summary>Get a single category</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _categoryService.GetByIdAsync(CurrentOrgId, id);
        return ToActionResult(result);
    }

    /// <summary>Create a new category (Owner only)</summary>
    [HttpPost]
    [Authorize(Roles = "OrganizationOwner")]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        var result = await _categoryService.CreateAsync(CurrentOrgId, request);
        return ToActionResult(result);
    }

    /// <summary>Update a category (Owner only)</summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "OrganizationOwner")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest request)
    {
        var result = await _categoryService.UpdateAsync(CurrentOrgId, id, request);
        return ToActionResult(result);
    }

    /// <summary>Delete a category — fails if active members are assigned (Owner only)</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "OrganizationOwner")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _categoryService.DeleteAsync(CurrentOrgId, id);
        return ToActionResult(result);
    }
}
