using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MYAcademy_IdentityEmailProject.Context;
using MYAcademy_IdentityEmailProject.Entities;
using MYAcademy_IdentityEmailProject.Extensions;
using MYAcademy_IdentityEmailProject.Models;

namespace MYAcademy_IdentityEmailProject.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly EmailContext _context;
        private readonly UserManager<AppUser> _userManager;

        public DashboardController(EmailContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Email == null) return RedirectToAction("SignIn", "Login");
            var email = user.Email;

            var inbox = _context.Messages.InboxOf(email);
            var sent = _context.Messages.SentOf(email);

            var today = DateTime.Today;
            var weekStart = today.AddDays(-6);        // bugün dahil son 7 gün
            var prevWeekStart = today.AddDays(-13);   // önceki 7 gün

            var model = new DashboardViewModel
            {
                FullName = $"{user.Name} {user.Surname}".Trim(),
                InboxCount = await inbox.CountAsync(),
                UnreadCount = await inbox.CountAsync(m => !m.IsRead),
                SentCount = await sent.CountAsync(),
                DraftCount = await _context.Messages.DraftsOf(email).CountAsync(),
                StarredCount = await _context.Messages.StarredOf(email).CountAsync(),

                TodayReceived = await inbox.CountAsync(m => m.SendDate >= today),
                TodaySent = await sent.CountAsync(m => m.SendDate >= today),
                WeekReceived = await inbox.CountAsync(m => m.SendDate >= weekStart),
                PrevWeekReceived = await inbox.CountAsync(m => m.SendDate >= prevWeekStart && m.SendDate < weekStart),
                WeekSent = await sent.CountAsync(m => m.SendDate >= weekStart),
                PrevWeekSent = await sent.CountAsync(m => m.SendDate >= prevWeekStart && m.SendDate < weekStart)
            };

            model.Recent = await inbox
                .OrderByDescending(m => m.SendDate)
                .Take(6)
                .Select(m => new RecentMessageItem
                {
                    MessageId = m.MessageId,
                    Sender = m.SenderEmail,
                    Subject = m.Subject,
                    SendDate = m.SendDate,
                    IsRead = m.IsRead,
                    Category = m.Category != null ? m.Category.CategoryName : null
                })
                .ToListAsync();

            model.Categories = await inbox
                .GroupBy(m => m.Category == null ? "Kategorisiz" : m.Category.CategoryName)
                .Select(g => new CategoryShare { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            // Son 7 günlük aktivite (gün gün gelen / giden)
            var receivedDaily = await inbox
                .Where(m => m.SendDate >= weekStart)
                .GroupBy(m => m.SendDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var sentDaily = await sent
                .Where(m => m.SendDate >= weekStart)
                .GroupBy(m => m.SendDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            model.Activity = Enumerable.Range(0, 7)
                .Select(i => weekStart.AddDays(i))
                .Select(d => new DailyActivity
                {
                    Date = d,
                    Received = receivedDaily.FirstOrDefault(x => x.Date == d)?.Count ?? 0,
                    Sent = sentDaily.FirstOrDefault(x => x.Date == d)?.Count ?? 0
                })
                .ToList();

            // En çok mesaj gönderenler
            model.TopSenders = await inbox
                .GroupBy(m => m.SenderEmail)
                .Select(g => new SenderShare
                {
                    Email = g.Key,
                    Count = g.Count(),
                    LastDate = g.Max(x => x.SendDate)
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            return View(model);
        }
    }
}
