using HomeFix.API.Data;
using HomeFix.API.DTOs;
using HomeFix.API.Interfaces;
using HomeFix.API.Middleware;
using HomeFix.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeFix.API.Services;

public class AuthService(HomeFixDbContext db, IPasswordService hasher, IJwtTokenService jwt) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest req)
    {
        if (!Enum.TryParse<UserRole>(req.Role, true, out var role) || role == UserRole.Admin)
            throw new AppException(400, "Role must be Customer or Provider.");

        var email = req.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email))
            throw new AppException(409, "An account with this email already exists.");

        var user = new User
        {
            FullName = req.FullName.Trim(), Email = email, PasswordHash = hasher.Hash(req.Password),
            Role = role, Phone = req.Phone?.Trim(), City = req.City?.Trim()
        };

        if (role == UserRole.Provider)
        {
            if (string.IsNullOrWhiteSpace(req.BusinessName) || req.CategoryId is null ||
                string.IsNullOrWhiteSpace(req.ServiceArea) || req.HourlyRate is null || req.HourlyRate <= 0)
                throw new AppException(400, "Providers must enter business name, category, service area and hourly rate.");
            if (!await db.ServiceCategories.AnyAsync(c => c.Id == req.CategoryId && c.IsActive))
                throw new AppException(400, "The selected service category does not exist.");

            // Providers start as Pending and must be approved by an Admin
            user.ProviderProfile = new ProviderProfile
            {
                BusinessName = req.BusinessName.Trim(), CategoryId = req.CategoryId.Value,
                ServiceArea = req.ServiceArea.Trim(), HourlyRate = req.HourlyRate.Value,
                ExperienceYears = req.ExperienceYears ?? 0, Bio = req.Bio?.Trim(),
                ApprovalStatus = ApprovalStatus.Pending
            };
        }

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return Build(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var user = await db.Users.Include(u => u.ProviderProfile).FirstOrDefaultAsync(u => u.Email == email);
        if (user is null || !hasher.Verify(req.Password, user.PasswordHash))
            throw new AppException(401, "Invalid email or password.");
        if (!user.IsActive)
            throw new AppException(403, "This account has been deactivated. Please contact support.");
        return Build(user);
    }

    public async Task<UserDto> GetMeAsync(int userId)
    {
        var user = await db.Users.AsNoTracking().Include(u => u.ProviderProfile).FirstOrDefaultAsync(u => u.Id == userId)
                   ?? throw new AppException(404, "User not found.");
        return Mapper.ToDto(user);
    }

    public async Task<UserDto> UpdateMeAsync(int userId, UpdateProfileRequest req)
    {
        var user = await db.Users.Include(u => u.ProviderProfile).FirstOrDefaultAsync(u => u.Id == userId)
                   ?? throw new AppException(404, "User not found.");
        user.FullName = req.FullName.Trim();
        user.Phone = req.Phone?.Trim();
        user.City = req.City?.Trim();
        user.Address = req.Address?.Trim();
        await db.SaveChangesAsync();
        return Mapper.ToDto(user);
    }

    private AuthResponse Build(User user)
    {
        var (token, expires) = jwt.Create(user);
        return new AuthResponse { Token = token, ExpiresAt = expires, User = Mapper.ToDto(user) };
    }
}
