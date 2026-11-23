using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using MYAcademy_IdentityEmailProject.Context;
using MYAcademy_IdentityEmailProject.Entities;
using MYAcademy_IdentityEmailProject.Extensions;
using MYAcademy_IdentityEmailProject.Models;
using static MYAcademy_IdentityEmailProject.Models.SidebarCountsViewModel;

namespace MYAcademy_IdentityEmailProject.ViewComponents.MessageLayoutViewComponents
{
    public class _MessageLayoutSidebarComponentPartial : ViewComponent
    {
        private readonly EmailContext _context;
        private readonly UserManager<AppUser> _userManager;

        public _MessageLayoutSidebarComponentPartial(EmailContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = new SidebarCountsViewModel();
            

            var user = await _userManager.GetUserAsync(UserClaimsPrincipal);
            if (user?.Email != null)
            {
                var email = user.Email;

                var unreadByCategory = await _context.Messages
                  .InboxOf(email)
                  .Where(m => !m.IsRead && m.Category != null)
                  .GroupBy(m => m.Category!.CategoryName)
                  .Select(g => new { Name = g.Key, Count = g.Count() })
                  .ToDictionaryAsync(x => x.Name, x => x.Count);

                var categories = await _context.Categories
                     .AsNoTracking()
                     .OrderBy(c => c.CategoryName)
                     .ToListAsync();

                //  Tüm kategorileri çek ve sayıyla eşleştir

               
                model.UnreadCount = await _context.Messages.InboxOf(email).CountAsync(m => !m.IsRead);
                model.DraftCount = await _context.Messages.DraftsOf(email).CountAsync();
                model.StarredCount = await _context.Messages.StarredOf(email).CountAsync();
                model.TrashCount = await _context.Messages.TrashOf(email).CountAsync();
                model.Categories = categories
                .Select(c => new SidebarCategoryItem
                {
                    Name = c.CategoryName,
                    UnreadCount = unreadByCategory.GetValueOrDefault(c.CategoryName)
                })
                .ToList();          // AsNoTracking() bu veriyi değiştirmeyeceğiz, sadece göstereceğiz. EF'in değişiklik takibi yapmasına gerek yok, bu yüzden daha hafif çalışır.
            }

            return View(model);
        }
    }
}