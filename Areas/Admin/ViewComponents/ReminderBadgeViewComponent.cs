using System;
using System.Linq;
using System.Threading.Tasks;
using ApexDrive.Data;
using ApexDrive.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApexDrive.Areas.Admin.ViewComponents
{
    public class ReminderBadgeViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public ReminderBadgeViewComponent(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var today = DateTime.UtcNow.Date;

            var reminders = _context.CarReminders.AsQueryable();

            if (HttpContext.User.IsInRole("Admin") && user?.BranchId != null)
                reminders = reminders.Where(r => r.BranchId == user.BranchId);

            int overdue = await reminders.CountAsync(r => !r.IsCompleted && r.ReminderDate < today);
            int dueToday = await reminders.CountAsync(r => !r.IsCompleted && r.ReminderDate == today);

            ViewBag.OverdueCount = overdue;
            ViewBag.DueTodayCount = dueToday;

            return View();
        }
    }
}
