using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MYAcademy_IdentityEmailProject.Context;
using MYAcademy_IdentityEmailProject.Entities;
using MYAcademy_IdentityEmailProject.Extensions;
using MYAcademy_IdentityEmailProject.Models;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;

namespace MYAcademy_IdentityEmailProject.Controllers
{
    [Authorize]
    public class MessageController : Controller
    {
        private readonly EmailContext _context;
        private readonly UserManager<AppUser> _userManager;

        public MessageController(EmailContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public async Task<IActionResult> Inbox()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("SignIn", "Login");

            var messages = await _context.Messages
                .Include(m => m.Category)
                .InboxOf(user.Email!)
                .OrderByDescending(m => m.SendDate)
                .ToListAsync();

            return View(messages);
        }
        public async Task<IActionResult> Starred()
        {
            var user= await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("SignIn", "Login");
            var values= await _context.Messages
                .StarredOf(user.Email!)
                .OrderByDescending(x => x.SendDate)
                .ToListAsync();
            return View(values);
        }
        private async Task FillCategories()
        {
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "CategoryName");
        }
        [HttpGet]
        public async Task<IActionResult> CreateMessage(int? draftId)
        {
            await FillCategories();

            if (draftId == null)
                return View(new CreateMessageViewModel());

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("SignIn", "Login");

            var draft = await _context.Messages
                .DraftsOf(user.Email!)
                .FirstOrDefaultAsync(m => m.MessageId == draftId);

            if (draft == null)
                return NotFound();

            return View(new CreateMessageViewModel
            {
                MessageId = draft.MessageId,
                ReceiverEmail = draft.ReceiverEmail,
                CategoryId = draft.CategoryId,
                Subject = draft.Subject,
                MessageDetail = draft.MessageDetail
            });
        }
        [ValidateAntiForgeryToken]
        [HttpPost]

        public async Task<IActionResult> CreateMessage(CreateMessageViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("SignIn", "Login");

            if (model.CategoryId != null && !await _context.Categories.AnyAsync(c => c.CategoryId == model.CategoryId))
                ModelState.AddModelError(nameof(model.CategoryId), "Geçerli bir kategori seçin.");

            if (!string.IsNullOrWhiteSpace(model.ReceiverEmail) &&
                await _userManager.FindByEmailAsync(model.ReceiverEmail.Trim()) == null)
                ModelState.AddModelError(nameof(model.ReceiverEmail), "Bu adrese sahip bir Mailora kullanıcısı bulunamadı.");

            if (!ModelState.IsValid)
            {
                await FillCategories();
                return View(model);
            }

            Message message;
            if (model.MessageId != null)
            {
                // Taslaktan gönderiliyor: yeni satır açma, mevcut taslak satırını güncelle
                message = await _context.Messages
                    .DraftsOf(user.Email!)
                    .FirstOrDefaultAsync(m => m.MessageId == model.MessageId);

                if (message == null)
                    return NotFound();
            }
            else
            {
                message = new Message { SenderEmail = user.Email! };
                _context.Messages.Add(message);
            }

            message.ReceiverEmail = model.ReceiverEmail.Trim();
            message.Subject = model.Subject.Trim();
            message.MessageDetail = new HtmlSanitizer().Sanitize(model.MessageDetail);
            message.CategoryId = model.CategoryId;
            message.SendDate = DateTime.Now;
            message.IsDraft = false;
            message.IsRead = false;

            await _context.SaveChangesAsync();
            return RedirectToAction("Sendbox");
        }



        public async Task<IActionResult> Sendbox()
        {
            var user = await _userManager.GetUserAsync(User);
           if (user==null)
                return RedirectToAction("SignIn", "Login");
            var values = await _context.Messages
                .SentOf(user.Email!)
                .OrderByDescending(x => x.SendDate)
                .ToListAsync();
            return View(values);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if(user==null)
                return RedirectToAction("SignIn", "Login");

            var message = await _context.Messages
    .Include(m => m.Category)
    .AccessibleBy(user.Email!)
    .FirstOrDefaultAsync(m => m.MessageId == id);

            if (message == null)
                return NotFound();

            var isReceived = message.ReceiverEmail == user.Email && !message.IsDraft;
            if (isReceived && !message.IsRead) { 

            message.IsRead = true;
            await _context.SaveChangesAsync();

            }

            var model = new MessageDetailViewModel
            {
                MessageId = message.MessageId,
                Subject = message.Subject,
                BodyHtml = new HtmlSanitizer().Sanitize(message.MessageDetail ?? ""),
                SenderEmail = message.SenderEmail,
                ReciverEmail = message.ReceiverEmail,
                SendDate = message.SendDate,
                CategoryName = message.Category?.CategoryName,
                IsRead = message.IsRead,
                IsReceived = isReceived
            };
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStar(int id, bool starred)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var message = await _context.Messages
                .AccessibleBy(user.Email!)
                .FirstOrDefaultAsync(m => m.MessageId == id);

            if (message == null)
                return NotFound();

            if (message.ReceiverEmail == user.Email && !message.IsDraft)
                message.IsStarredByReceiver = starred;

            if (message.SenderEmail == user.Email)
                message.IsStarredBySender = starred;

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> MarkAsRead(int[] ids) => SetReadState(ids, true);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> MarkAsUnread(int[] ids) => SetReadState(ids, false);

        private async Task<IActionResult> SetReadState(int[] ids, bool isRead)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            if (ids == null || ids.Length == 0)
                return BadRequest();

            var messages = await _context.Messages
                .InboxOf(user.Email!)
                .Where(m => ids.Contains(m.MessageId))
                .ToListAsync();

            foreach (var m in messages)
                m.IsRead = isRead;

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveToTrash(int[] ids)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            if (ids == null || ids.Length == 0)
                return BadRequest();

            var messages = await _context.Messages
                .AccessibleBy(user.Email!)
                .Where(m => ids.Contains(m.MessageId))
                .ToListAsync();

            foreach (var m in messages)
            {
                if (m.IsDraft)
                {
                    _context.Messages.Remove(m);   // taslağın karşı tarafı yok, doğrudan silinebilir
                    continue;
                }

                if (m.ReceiverEmail == user.Email)
                    m.IsTrashedByReceiver = true;

                if (m.SenderEmail == user.Email)
                    m.IsTrashedBySender = true;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
        public async Task<IActionResult> Trash()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("SignIn", "Login");

            var values = await _context.Messages
                .TrashOf(user.Email!)
                .OrderByDescending(x => x.SendDate)
                .ToListAsync();

            return View(values);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int[] ids)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            if (ids == null || ids.Length == 0)
                return BadRequest();

            var messages = await _context.Messages
                .TrashOf(user.Email!)
                .Where(m => ids.Contains(m.MessageId))
                .ToListAsync();

            foreach (var m in messages)
            {
                if (m.ReceiverEmail == user.Email)
                    m.IsTrashedByReceiver = false;

                if (m.SenderEmail == user.Email)
                    m.IsTrashedBySender = false;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePermanently(int[] ids)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            if (ids == null || ids.Length == 0)
                return BadRequest();

            var messages = await _context.Messages
                .TrashOf(user.Email!)
                .Where(m => ids.Contains(m.MessageId))
                .ToListAsync();

            foreach (var m in messages)
            {
                if (m.ReceiverEmail == user.Email)
                    m.IsDeletedByReceiver = true;

                if (m.SenderEmail == user.Email)
                    m.IsDeletedBySender = true;

                // İki taraf da sildiyse bu satırı artık kimse göremez: gerçekten kaldır
                if (m.IsDeletedBySender && m.IsDeletedByReceiver)
                    _context.Messages.Remove(m);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDraft(CreateMessageViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("SignIn", "Login");

            // Taslakta alanlar boş olabilir: [Required] hatalarını at, sadece gerçek kuralları elle uygula
            ModelState.Clear();

            var receiver = model.ReceiverEmail?.Trim() ?? "";
            var subject = model.Subject?.Trim() ?? "";

            if (receiver != "" && !new EmailAddressAttribute().IsValid(receiver))
                ModelState.AddModelError(nameof(model.ReceiverEmail), "Geçerli bir e-posta adresi girin.");
            if (receiver.Length > 256)
                ModelState.AddModelError(nameof(model.ReceiverEmail), "Alıcı adresi çok uzun.");
            if (subject.Length > 200)
                ModelState.AddModelError(nameof(model.Subject), "Konu en fazla 200 karakter olabilir.");
            if (model.CategoryId != null && !await _context.Categories.AnyAsync(c => c.CategoryId == model.CategoryId))
                ModelState.AddModelError(nameof(model.CategoryId), "Geçerli bir kategori seçin.");

            if (!ModelState.IsValid)
            {
                await FillCategories();
                return View("CreateMessage", model);
            }

            Message draft;
            if (model.MessageId != null)
            {
                draft = await _context.Messages
                    .DraftsOf(user.Email!)
                    .FirstOrDefaultAsync(m => m.MessageId == model.MessageId);

                if (draft == null)
                    return NotFound();
            }
            else
            {
                draft = new Message { SenderEmail = user.Email!, IsDraft = true };
                _context.Messages.Add(draft);
            }

            draft.ReceiverEmail = receiver;
            draft.Subject = subject;
            draft.MessageDetail = new HtmlSanitizer().Sanitize(model.MessageDetail ?? "");
            draft.CategoryId = model.CategoryId;
            draft.SendDate = DateTime.Now;     // son kaydedilme zamanı
            draft.IsRead = false;

            await _context.SaveChangesAsync();

            TempData["Flash"] = "Taslak kaydedildi.";
            return RedirectToAction("CreateMessage", new { draftId = draft.MessageId });
        }
        public async Task<IActionResult> Drafts()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("SignIn", "Login");

            var values = await _context.Messages
                .DraftsOf(user.Email!)
                .OrderByDescending(x => x.SendDate)
                .ToListAsync();

            return View(values);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDrafts(int[] ids)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            if (ids == null || ids.Length == 0)
                return BadRequest();

            var drafts = await _context.Messages
                .DraftsOf(user.Email!)
                .Where(m => ids.Contains(m.MessageId))
                .ToListAsync();

            _context.Messages.RemoveRange(drafts);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // Orijinal mesajı yalnızca erişimi olan kullanıcıya getirir (taslak hariç)
        private async Task<Message?> FindOriginalAsync(int id, string email)
        {
            return await _context.Messages
                .Include(m => m.Category)
                .AccessibleBy(email)
                .FirstOrDefaultAsync(m => m.MessageId == id && !m.IsDraft);
        }

        public async Task<IActionResult> Reply(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("SignIn", "Login");

            var m = await FindOriginalAsync(id, user.Email!);
            if (m == null) return NotFound();

            // Ben alıcıysam gönderene, ben göndericiysem alıcıya yanıt veririm
            var to = m.ReceiverEmail == user.Email ? m.SenderEmail : m.ReceiverEmail;

            var model = new CreateMessageViewModel
            {
                ReceiverEmail = to,
                CategoryId = m.CategoryId,
                Subject = Prefix("Re: ", m.Subject),
                MessageDetail = BuildQuote(m, forward: false)
            };

            await FillCategories();
            return View("CreateMessage", model);
        }

        public async Task<IActionResult> Forward(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("SignIn", "Login");

            var m = await FindOriginalAsync(id, user.Email!);
            if (m == null) return NotFound();

            var model = new CreateMessageViewModel
            {
                CategoryId = m.CategoryId,
                Subject = Prefix("Fwd: ", m.Subject),
                MessageDetail = BuildQuote(m, forward: true)
            };

            await FillCategories();
            return View("CreateMessage", model);
        }
        // "Re: Re: Re:" yığılmasın
        private static string Prefix(string prefix, string subject)
            => subject.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? subject : prefix + subject;

        private static string BuildQuote(Message m, bool forward)
        {
            var body = new HtmlSanitizer().Sanitize(m.MessageDetail ?? "");
            var from = WebUtility.HtmlEncode(m.SenderEmail);
            var to = WebUtility.HtmlEncode(m.ReceiverEmail);
            var subj = WebUtility.HtmlEncode(m.Subject);
            var date = m.SendDate.ToString("d MMMM yyyy HH:mm", new CultureInfo("tr-TR"));

            var header = forward
                ? $"---------- İletilen mesaj ----------<br>Kimden: {from}<br>Tarih: {date}<br>Konu: {subj}<br>Kime: {to}"
                : $"{date} tarihinde {from} şunu yazdı:";

            return $"<p></p><p></p><blockquote>{header}<br><br>{body}</blockquote>";
        }

    }
}
