using JobPortalApi.DTOs.AdminUser;
using JobPortalApi.Services.Interface;
using Microsoft.EntityFrameworkCore;
using JobPortalApi.Models;
using JobPortalApi.DTOs.shared;
using JobPortalApi.Services.Interface.Admin;
using JobPortalApi.DTOs.Shared;
namespace JobPortalApi.Services.Admin
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            return await _context.Users
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    Role = u.Role
                })
                .ToListAsync();
        }

        public async Task<PagedResultDto<UserDto>> GetAllUsersAsync(PagedQuery request)
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var query = _context.Users.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();
                query = query.Where(user => user.Email.Contains(search) || user.FullName.Contains(search));
            }
            var ordered = string.Equals(request.SortDir, "asc", StringComparison.OrdinalIgnoreCase)
                ? request.SortBy?.ToLowerInvariant() switch
                {
                    "email" => query.OrderBy(user => user.Email),
                    "name" => query.OrderBy(user => user.FullName),
                    "createdat" => query.OrderBy(user => user.CreatedAt),
                    _ => query.OrderBy(user => user.Id)
                }
                : request.SortBy?.ToLowerInvariant() switch
                {
                    "email" => query.OrderByDescending(user => user.Email),
                    "name" => query.OrderByDescending(user => user.FullName),
                    "createdat" => query.OrderByDescending(user => user.CreatedAt),
                    _ => query.OrderByDescending(user => user.Id)
                };
            var total = await query.CountAsync();
            var items = await ordered.ThenBy(user => user.Id)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(u => new UserDto { Id = u.Id, Email = u.Email, FullName = u.FullName, Role = u.Role })
                .ToListAsync();
            return new PagedResultDto<UserDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid id)
        {
            var u = await _context.Users.FindAsync(id);
            if (u == null) return null;
            return new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                Role = u.Role
            };
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
        {
            var user = new Models.User
            {
                Email = dto.Email,
                FullName = dto.FullName,
                Role = dto.Role
            };
            // Tài khoản mới dùng cùng định dạng BCrypt với luồng đăng nhập.
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role
            };
        }

        public async Task<bool> UpdateUserAsync(Guid id, UpdateUserDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;
            if (!string.IsNullOrWhiteSpace(dto.Email)) user.Email = dto.Email;
            if (!string.IsNullOrWhiteSpace(dto.FullName)) user.FullName = dto.FullName;
            if (dto.Role.HasValue && user.Role != dto.Role.Value)
            {
                user.Role = dto.Role.Value;
                // JWT đang mang role cũ sẽ bị từ chối từ request kế tiếp.
                user.PasswordVersion++;
            }
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;
            var deletedAt = DateTime.UtcNow;
            user.DeletedAt = deletedAt;
            user.PasswordVersion++;

            var linkedPosts = await _context.JobPosts
                .Where(job => job.EmployerId == id)
                .ToListAsync();
            foreach (var post in linkedPosts)
            {
                post.DeletedAt = deletedAt;
                post.Status = Models.Enums.JobPostStatus.Closed;
            }

            var linkedCompanies = await _context.Companies
                .Where(company => company.UserId == id)
                .ToListAsync();
            foreach (var company in linkedCompanies)
                company.DeletedAt = deletedAt;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
