using ApexDrive.Data;
using ApexDrive.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ApexDrive.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin")] // Only SuperAdmins should change prices
    public class PricingRulesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PricingRulesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // List all rules
        public async Task<IActionResult> Index()
        {
            var rules = await _context.PricingRules.ToListAsync();
            return View(rules);
        }

        // GET: Create Rule
        public IActionResult Create() => View();

        // POST: Create Rule
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PricingRule rule)
        {
            if (ModelState.IsValid)
            {
                _context.Add(rule);
                await _context.SaveChangesAsync();
                TempData["Success"] = "New dynamic price rule added!";
                return RedirectToAction(nameof(Index));
            }
            return View(rule);
        }

        // POST: Delete Rule
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var rule = await _context.PricingRules.FindAsync(id);
            if (rule != null)
            {
                _context.PricingRules.Remove(rule);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}