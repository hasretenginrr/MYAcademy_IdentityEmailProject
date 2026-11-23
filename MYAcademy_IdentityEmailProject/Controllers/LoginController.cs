using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MYAcademy_IdentityEmailProject.Entities;
using MYAcademy_IdentityEmailProject.Models;
using MYAcademy_IdentityEmailProject.Services;
using System.Net;

namespace MYAcademy_IdentityEmailProject.Controllers
{
    public class LoginController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMailService _mail;

        public LoginController(SignInManager<AppUser> signInManager,
                               UserManager<AppUser> userManager,
                               IMailService mail)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _mail = mail;
        }

        [HttpGet]
        public IActionResult SignIn() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignIn(UserLoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Username, model.Password, isPersistent: false, lockoutOnFailure: true);

            if (result.Succeeded)
                return RedirectToAction("Inbox", "Message");

            if (result.IsNotAllowed)
            {
                // Şifre doğruysa e-postası doğrulanmamıştır: doğrulama sayfasına götür
                var user = await _userManager.FindByNameAsync(model.Username);
                if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
                    return RedirectToAction("ConfirmEmail", "Register", new { email = user.Email });
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Çok fazla hatalı deneme yapıldı. Lütfen birkaç dakika sonra tekrar deneyin.");
                return View(model);
            }

            // Kullanıcı yok da olsa şifre yanlış da olsa aynı mesaj
            ModelState.AddModelError("", "Kullanıcı adı veya şifre hatalı.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOut()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("SignIn");
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null && user.EmailConfirmed)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var link = Url.Action("ResetPassword", "Login",
                    new { email = user.Email, token }, Request.Scheme);

                try
                {
                    await _mail.SendAsync(user.Email!, "Mailora şifre sıfırlama",
                        $"<p>Şifrenizi sıfırlamak için aşağıdaki bağlantıya tıklayın. Bağlantı 30 dakika geçerlidir.</p>" +
                        $"<p><a href=\"{WebUtility.HtmlEncode(link)}\">Şifremi sıfırla</a></p>" +
                        $"<p>Bu isteği siz yapmadıysanız bu e-postayı yok sayabilirsiniz.</p>");
                }
                catch { /* mail hatası kullanıcıya sızdırılmaz; ileride loglanabilir */ }
            }

            TempData["Info"] = "Bu adres kayıtlıysa şifre sıfırlama bağlantısı gönderildi. Gelen kutunuzu kontrol edin.";
            return RedirectToAction(nameof(ForgotPassword));
        }
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string? email, string? token)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
                return View("ResetPasswordInvalid");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return View("ResetPasswordInvalid");

            // Token'ı tüketmeden geçerli mi diye bakar
            var valid = await _userManager.VerifyUserTokenAsync(
                user,
                _userManager.Options.Tokens.PasswordResetTokenProvider,
                UserManager<AppUser>.ResetPasswordTokenPurpose,
                token);

            if (!valid)
                return View("ResetPasswordInvalid");

            return View(new ResetPasswordViewModel { Email = email, Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return View("ResetPasswordInvalid");

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

            if (result.Succeeded)
            {
                TempData["Info"] = "Şifreniz güncellendi. Yeni şifrenizle giriş yapabilirsiniz.";
                return RedirectToAction("SignIn");
            }

            // Token sorunu mu, yoksa şifre kuralı mı (çok kısa vb.)?
            if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken)))
                return View("ResetPasswordInvalid");

            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Description);
            return View(model);
        }
    }
}