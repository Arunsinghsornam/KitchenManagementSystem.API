using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KitchenManagementSystem.API.Data;
using KitchenManagementSystem.API.DTOs;
using KitchenManagementSystem.API.Models;
using KitchenManagementSystem.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KitchenManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "power_admin")] // Only Power Admin can manage organizations onboarding
public class OrganizationsController : BaseApiController
{
    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;

    public OrganizationsController(AppDbContext db, IEmailService emailService)
    {
        _db = db;
        _emailService = emailService;
    }

    // GET: api/organizations
    [HttpGet]
    public async Task<IActionResult> GetOrganizations()
    {
        var orgs = await _db.Organizations.OrderByDescending(o => o.CreatedAt).ToListAsync();
        
        // Let's also include the owner user info for each organization
        var resultList = new List<object>();
        foreach (var org in orgs)
        {
            var owner = await _db.Users
                .Where(u => u.OrganizationId == org.Id && u.Role == "super_admin")
                .FirstOrDefaultAsync();

            var firstOutlet = await _db.Outlets
                .Where(o => o.OrganizationId == org.Id)
                .FirstOrDefaultAsync();

            resultList.Add(new
            {
                org.Id,
                org.Name,
                org.Status,
                org.LogoUrl,
                org.CreatedAt,
                OwnerName = owner?.FullName ?? "N/A",
                OwnerEmail = owner?.Email ?? "N/A",
                OutletName = firstOutlet?.Name ?? "N/A"
            });
        }

        return Ok(ApiResponse<IEnumerable<object>>.Ok(resultList));
    }

    // POST: api/organizations/{id}/approve
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var org = await _db.Organizations.FindAsync(id);
        if (org == null)
            return NotFound(ApiResponse<object>.Fail("Organization not found."));

        if (org.Status == "Approved")
            return BadRequest(ApiResponse<object>.Fail("Organization is already approved."));

        org.Status = "Approved";

        // Find the owner user and activate their account
        var owner = await _db.Users
            .Where(u => u.OrganizationId == org.Id && u.Role == "super_admin")
            .FirstOrDefaultAsync();

        if (owner != null)
        {
            owner.IsActive = true;
        }

        await _db.SaveChangesAsync();

        // Send email to owner
        if (owner != null)
        {
            try
            {
                var subject = "Your Organization Has Been Approved!";
                var body = $@"
                    <h3>Congratulations! Your Onboarding is Approved</h3>
                    <p>Hello {owner.FullName},</p>
                    <p>We are pleased to inform you that your organization <strong>{org.Name}</strong> has been approved for the Kitchen Operations Portal.</p>
                    <p>You can now sign in with your email address: <strong>{owner.Email}</strong>.</p>
                    <p><a href='http://localhost:4200/login'>Go to Login Page</a></p>
                    <p>Best regards,<br/>Platform Administrator</p>";

                await _emailService.SendEmailAsync(owner.Email, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Warning] Failed to send approval email to {owner.Email}: {ex.Message}");
            }
        }

        return Ok(ApiResponse<object>.Ok(null!, $"Organization '{org.Name}' approved successfully."));
    }

    // POST: api/organizations/{id}/reject
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id)
    {
        var org = await _db.Organizations.FindAsync(id);
        if (org == null)
            return NotFound(ApiResponse<object>.Fail("Organization not found."));

        if (org.Status == "Approved")
            return BadRequest(ApiResponse<object>.Fail("Cannot reject an already approved organization."));

        org.Status = "Rejected";

        // Find the owner user
        var owner = await _db.Users
            .Where(u => u.OrganizationId == org.Id && u.Role == "super_admin")
            .FirstOrDefaultAsync();

        if (owner != null)
        {
            owner.IsActive = false; // Ensure they remain deactivated
        }

        await _db.SaveChangesAsync();

        // Send email to owner
        if (owner != null)
        {
            try
            {
                var subject = "Onboarding Request Update";
                var body = $@"
                    <h3>Organization Onboarding Request Status</h3>
                    <p>Hello {owner.FullName},</p>
                    <p>We regret to inform you that your registration request for organization <strong>{org.Name}</strong> was not approved at this time.</p>
                    <p>If you have any questions, please contact our support team.</p>
                    <p>Best regards,<br/>Platform Administrator</p>";

                await _emailService.SendEmailAsync(owner.Email, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Warning] Failed to send rejection email to {owner.Email}: {ex.Message}");
            }
        }

        return Ok(ApiResponse<object>.Ok(null!, $"Organization '{org.Name}' rejected."));
    }
}
