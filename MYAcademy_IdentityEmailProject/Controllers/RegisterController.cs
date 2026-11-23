using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MYAcademy_IdentityEmailProject.Entities;
using MYAcademy_IdentityEmailProject.Models;
using MYAcademy_IdentityEmailProject.Services;   // IMailService'i koyduğun klasör

namespace MYAcademy_IdentityEmailProject.Controllers
{
    public class RegisterController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IMailService _mail;

        public RegisterController(UserManager<AppUser> userManager, IMailService mail)
        {
            _userManager = userManager;
            _mail = mail;
        }

        [HttpGet]
        public IActionResult SignUp() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUp(UserRegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var validCode = RandomNumberGenerator.GetInt32(100000, 1000000);

            var appUser = new AppUser
            {
                Name = model.Name,
                Surname = model.Surname,
                UserName = model.UserName,
                Email = model.Email,
                ValidCode = validCode
            };

            var result = await _userManager.CreateAsync(appUser, model.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError("", e.Description);
                return View(model);
            }

            try
            {
                await _mail.SendAsync(
                    appUser.Email!,
                    "Mailora doğrulama kodu",
                    $"<p>Mailora hesabınızı doğrulamak için kodunuz:</p><h2>{validCode}</h2>");
            }
            catch
            {
                // Mail gitmediyse kullanıcı doğrulayamaz; yarım kayıt kalmasın
                await _userManager.DeleteAsync(appUser);
                ModelState.AddModelError("", "Doğrulama e-postası gönderilemedi. Lütfen daha sonra tekrar deneyin.");
                return View(model);
            }

            return RedirectToAction("ConfirmEmail", new { email = appUser.Email });
        }

        // ... ConfirmEmail GET ve POST metotları aynen kalıyor


        [HttpGet]
        public IActionResult ConfirmEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction("SignUp");
            return View(new ConfirmEmailViewModel{ Email = email });
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmEmail(ConfirmEmailViewModel model) {
            if (string.IsNullOrWhiteSpace(model.Email))
                return RedirectToAction("SignUp");

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null || string.IsNullOrWhiteSpace(model.Code) || user.ValidCode?.ToString() != model.Code) {

                ModelState.AddModelError("", "Girdiğiniz kod hatalı. Lütfen tekrar deneyin.");
                return View(model);

            }

            user.EmailConfirmed = true;
            user.ValidCode = null;
            await _userManager.UpdateAsync(user);

            return RedirectToAction("SignIn", "Login");

        }


    }
}
