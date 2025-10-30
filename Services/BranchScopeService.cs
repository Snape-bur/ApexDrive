using System.Security.Claims;
using System.Threading.Tasks;
using ApexDrive.Data;
using ApexDrive.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ApexDrive.Services
{
    public class BranchScopeService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BranchScopeService(
            UserManager<AppUser> userManager,
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // ✅ Get logged-in user
        private async Task<AppUser?> GetCurrentUserAsync()
        {
            var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return null;
            return await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }

        // ✅ Return current user’s branch (if any)
        public async Task<int?> GetCurrentBranchIdAsync()
        {
            var user = await GetCurrentUserAsync();
            return user?.BranchId;
        }

        // ✅ Role checks
        public bool IsSuperAdmin()
            => _httpContextAccessor.HttpContext?.User.IsInRole("SuperAdmin") ?? false;

        public bool IsAdmin()
            => _httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;
    }
}
