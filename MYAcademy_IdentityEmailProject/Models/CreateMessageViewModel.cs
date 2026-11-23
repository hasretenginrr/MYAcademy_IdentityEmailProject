using System.ComponentModel.DataAnnotations;

namespace MYAcademy_IdentityEmailProject.Models
{
    public class CreateMessageViewModel
    {
        [Required(ErrorMessage = "Alıcı e-posta adresini girin.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        [StringLength(256)]
        public string ReceiverEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lütfen bir kategori seçin.")]
        public int? CategoryId { get; set; }

        [Required(ErrorMessage = "Konu alanı boş bırakılamaz.")]
        [StringLength(200, ErrorMessage = "Konu en fazla 200 karakter olabilir.")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mesaj içeriği boş olamaz.")]
        public string MessageDetail { get; set; } = string.Empty;
        public int? MessageId { get; set; }   // düzenlenen taslağın numarası (yeni mesajda boş)
    }
}
