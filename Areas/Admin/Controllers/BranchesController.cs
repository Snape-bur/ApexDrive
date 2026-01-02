using ApexDrive.Data;
using ApexDrive.Models;
using ApexDrive.ViewModels.BranchViewModels;
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

        public BranchesController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

     
        // INDEX
     
        public async Task<IActionResult> Index()
        {
            var branches = await _context.Branches
                .Include(b => b.Manager)
                .AsNoTracking()
                .ToListAsync();

            return View(branches);
        }



     
        // CREATE
       
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Branch branch)
        {
            if (!ModelState.IsValid)
                return View(branch);

            branch.CreatedAt = DateTime.UtcNow;

            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // EDIT
        // =========================
        public async Task<IActionResult> Edit(int id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null)
                return NotFound();

            return View(branch);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Branch branch)
        {
            if (id != branch.BranchId)
                return NotFound();

            if (!ModelState.IsValid)
                return View(branch);

            var existingBranch = await _context.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.BranchId == id);

            if (existingBranch == null)
                return NotFound();

            // Preserve immutable fields
            branch.CreatedAt = existingBranch.CreatedAt;
            branch.ManagerId = existingBranch.ManagerId;

            _context.Update(branch);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DELETE
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null)
                return NotFound();

            try
            {
                _context.Branches.Remove(branch);
                await _context.SaveChangesAsync();
            }
            catch
            {
                TempData["Error"] = "Cannot delete branch. It may be linked to other records.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // ASSIGN MANAGER (GET)
        // =========================
        public async Task<IActionResult> AssignManager(int id)
        {
            var branch = await _context.Branches
                .Include(b => b.Manager)
                .FirstOrDefaultAsync(b => b.BranchId == id);

            if (branch == null)
                return NotFound();

            // ✅ Correct Identity role usage
            var managers = await _userManager.GetUsersInRoleAsync("Manager");

            var viewModel = new AssignManagerViewModel
            {
                Branch = branch,
                Managers = managers.ToList(),
                SelectedManagerId = branch.ManagerId
            };

            return View(viewModel);
        }

        // =========================
        // ASSIGN MANAGER (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignManager(AssignManagerViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var managers = await _userManager.GetUsersInRoleAsync("Manager");
                model.Managers = managers.ToList();
                return View(model);
            }

            var branch = await _context.Branches
                .FirstOrDefaultAsync(b => b.BranchId == model.Branch.BranchId);

            if (branch == null)
                return NotFound();

            branch.ManagerId = model.SelectedManagerId;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // HELPERS
        // =========================
        private bool BranchExists(int id)
        {
            return _context.Branches.Any(b => b.BranchId == id);
        }
    }
}
