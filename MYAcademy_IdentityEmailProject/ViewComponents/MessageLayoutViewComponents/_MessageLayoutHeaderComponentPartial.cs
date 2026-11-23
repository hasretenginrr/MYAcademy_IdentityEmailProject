using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MYAcademy_IdentityEmailProject.Entities;

namespace MYAcademy_IdentityEmailProject.ViewComponents.MessageLayoutViewComponents
{
    public class _MessageLayoutHeaderComponentPartial : ViewComponent
    {
        private readonly UserManager<AppUser> _userManager;

        public _MessageLayoutHeaderComponentPartial(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user= await _userManager.GetUserAsync(UserClaimsPrincipal); //viewcomponent içinde User kullanılmaz, bunun yerine UserClaimsPrincipal kullanılır.  yani şuan giriş yapmış kişi demek 
            return View(user);
        }
    }
}
