using ApexDrive.Data;
using ApexDrive.Models;
using ApexDrive.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexDrive.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin")] // ✅ Only SuperAdmin can manage Admin accounts
    public class AdminUsersController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AdminUsersController(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // ✅ List all Admins & SuperAdmins
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users
                .Include(u => u.Branch) // include branch for display
                .ToList();

            var adminList = new List<(AppUser user, IList<string> roles)>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Admin") || roles.Contains("SuperAdmin"))
                    adminList.Add((user, roles));
            }

            return View(adminList);
        }

        // ✅ Create (GET)
        public async Task<IActionResult> Create()
        {
            ViewBag.Branches = await _context.Branches.ToListAsync();
            return View();
        }

        // ✅ Create (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddAdminUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Branches = await _context.Branches.ToListAsync();
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email already exists.");
                ViewBag.Branches = await _context.Branches.ToListAsync();
                return View(model);
            }

            var user = new AppUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,     // ✅ NEW
                BranchId = model.BranchId,     // ✅ Assign branch
                CreatedAt = System.DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Admin");
                TempData["Success"] = "✅ Admin created successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            ViewBag.Branches = await _context.Branches.ToListAsync();
            return View(model);
        }

        // ✅ Edit (GET)
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
                return NotFound();

            var user = await _userManager.Users
                .Include(u => u.Branch)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            var model = new EditAdminUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,         // ✅ NEW
                BranchId = user.BranchId ?? 0
            };

            ViewBag.Branches = await _context.Branches.ToListAsync();
            return View(model);
        }

        // ✅ Edit (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditAdminUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Branches = await _context.Branches.ToListAsync();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
                return NotFound();

            user.Email = model.Email;
            user.UserName = model.Email;
            user.FullName = model.FullName;     // ✅ NEW
            user.BranchId = model.BranchId;     // ✅ NEW

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "✅ Admin updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            ViewBag.Branches = await _context.Branches.ToListAsync();
            return View(model);
        }

        // ✅ Delete (GET)
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            return View(user);
        }

        // ✅ Delete (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
                TempData["Success"] = "🗑️ Admin deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ✅ Details View
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
                return NotFound();

            var user = await _context.Users
                .Include(u => u.Branch)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.Roles = roles;

            return View(user);
        }
    }
}
