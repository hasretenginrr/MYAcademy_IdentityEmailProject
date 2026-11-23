using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MYAcademy_IdentityEmailProject.Entities;
using MYAcademy_IdentityEmailProject.Models;

namespace MYAcademy_IdentityEmailProject.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IWebHostEnvironment _env;


        public ProfileController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _env = env;

        }

        [HttpGet]
        public IActionResult ChangePassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("SignIn", "Login");

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                {
                    var msg = e.Code == nameof(IdentityErrorDescriber.PasswordMismatch)
                        ? "Mevcut şifre hatalı."
                        : e.Description;
                    ModelState.AddModelError("", msg);
                }
                return View(model);
            }

            // Şifre değişince SecurityStamp yenilenir; oturumu bozmadan çerezi tazele
            await _signInManager.RefreshSignInAsync(user);

            TempData["Flash"] = "Şifreniz başarıyla değiştirildi.";
            return RedirectToAction(nameof(ChangePassword));
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("SignIn", "Login");

            return View(ToModel(user));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("SignIn", "Login");

            if (!ModelState.IsValid)
            {
                // BindNever alanlar formdan gelmediği için boş; gösterim için yeniden doldur
                model.UserName = user.UserName!;
                model.Email = user.Email!;
                model.ImageUrl = user.ImageUrl;
                return View(model);
            }

            user.Name = model.Name.Trim();
            user.Surname = model.Surname.Trim();
            user.Description = model.Description?.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                model.UserName = user.UserName!;
                model.Email = user.Email!;
                model.ImageUrl = user.ImageUrl;
                return View(model);
            }

            TempData["Flash"] = "Profiliniz güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        private static ProfileViewModel ToModel(AppUser user) => new()
        {
            Name = user.Name,
            Surname = user.Surname,
            Description = user.Description,
            UserName = user.UserName!,
            Email = user.Email!,
            ImageUrl = user.ImageUrl
        };

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(3_000_000)]
        public async Task<IActionResult> UploadPhoto(IFormFile? photo)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("SignIn", "Login");

            if (photo == null || photo.Length == 0)
                return PhotoError("Lütfen bir resim seçin.");

            if (photo.Length > 2 * 1024 * 1024)
                return PhotoError("Resim en fazla 2 MB olabilir.");

            var ext = await DetectImageExtensionAsync(photo);
            if (ext == null)
                return PhotoError("Yalnızca JPG, PNG veya WebP yükleyebilirsiniz.");

            var folder = Path.Combine(_env.WebRootPath, "uploads", "avatars");
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            await using (var fs = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
                await photo.CopyToAsync(fs);

            DeleteAvatarFile(user.ImageUrl);          // eskisi çöp olarak kalmasın
            user.ImageUrl = $"/uploads/avatars/{fileName}";
            await _userManager.UpdateAsync(user);

            TempData["Flash"] = "Profil fotoğrafınız güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePhoto()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("SignIn", "Login");

            DeleteAvatarFile(user.ImageUrl);
            user.ImageUrl = null;
            await _userManager.UpdateAsync(user);

            TempData["Flash"] = "Profil fotoğrafı kaldırıldı.";
            return RedirectToAction(nameof(Index));
        }

        private IActionResult PhotoError(string message)
        {
            TempData["Error"] = message;
            return RedirectToAction(nameof(Index));
        }

        // Dosyanın ilk baytlarına bakarak gerçek türünü belirler (uzantıya/ContentType'a güvenmez)
        private static async Task<string?> DetectImageExtensionAsync(IFormFile file)
        {
            var h = new byte[12];
            await using var s = file.OpenReadStream();
            var read = await s.ReadAsync(h, 0, h.Length);
            if (read < 12) return null;

            if (h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return ".jpg";                         // JPEG
            if (h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47) return ".png";        // PNG
            if (h[0] == 0x52 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x46 &&
                h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50) return ".webp";     // WebP
            return null;
        }

        private void DeleteAvatarFile(string? url)
        {
            if (string.IsNullOrEmpty(url) || !url.StartsWith("/uploads/avatars/")) return;

            // Sadece dosya adını al: veritabanındaki değer bozulsa bile klasör dışına çıkılamaz
            var path = Path.Combine(_env.WebRootPath, "uploads", "avatars", Path.GetFileName(url));
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
    }
}