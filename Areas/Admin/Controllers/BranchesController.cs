using ApexDrive.Data;
using ApexDrive.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApexDrive.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin")]
    public class BranchesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public BranchesController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ✅ List all branches
        public async Task<IActionResult> Index()
        {
            var branches = await _context.Branches
                .Include(b => b.Manager)
                .OrderBy(b => b.BranchName)
                .ToListAsync();

            return View(branches);
        }

        // ✅ Create branch (GET)
        public IActionResult Create() => View();

        // ✅ Create branch (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Branch branch)
        {
            if (ModelState.IsValid)
            {
                branch.CreatedAt = DateTime.UtcNow;
                _context.Branches.Add(branch);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(branch);
        }

        // ✅ Edit branch (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null) return NotFound();
            return View(branch);
        }

        // ✅ Edit branch (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Branch branch)
        {
            if (id != branch.BranchId) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(branch);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(branch);
        }

        // ✅ Assign Manager (GET)
        public async Task<IActionResult> AssignManager(int id)
        {
            var branch = await _context.Branches
                .Include(b => b.Manager)
                .FirstOrDefaultAsync(b => b.BranchId == id);

            if (branch == null) return NotFound();

            // ✅ Fetch all admins (without branch or same branch)
            var allUsers = await _userManager.Users.ToListAsync();
            var admins = new List<AppUser>();

            foreach (var user in allUsers)
            {
                if (await _userManager.IsInRoleAsync(user, "Admin") &&
                    (user.BranchId == null || user.BranchId == id))
                {
                    admins.Add(user);
                }
            }

            ViewBag.Admins = admins;
            return View(branch);
        }

        // ✅ Assign Manager (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignManager(int id, string managerId)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null) return NotFound();

            branch.ManagerId = managerId;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
