using System.Threading.Tasks;

namespace KitchenManagementSystem.API.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, string? replyTo = null, string? fromDisplayName = null);
}
