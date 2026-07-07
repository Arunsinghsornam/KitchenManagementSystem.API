using Microsoft.AspNetCore.Http;

namespace KitchenManagementSystem.API.DTOs;

public class UpdateOrgProfileDto
{
    public string? Name { get; set; }
    public IFormFile? Logo { get; set; }
}
