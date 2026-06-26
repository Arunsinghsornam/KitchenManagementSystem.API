using KitchenManagementSystem.API.DTOs;

namespace KitchenManagementSystem.API.Services;

public interface IUserService
{
    Task<IEnumerable<UserResponseDto>> GetAllAsync(Guid? outletId = null);
    Task<UserResponseDto?> GetByIdAsync(Guid id);
    Task<UserResponseDto> CreateAsync(CreateUserDto dto);
    Task<UserResponseDto?> UpdateAsync(Guid id, UpdateUserDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(string email);

    /// <summary> SuperAdmin password reset — no current-password check. </summary>
    Task SetPasswordAsync(Guid id, string newPassword);
}
