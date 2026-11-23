namespace MYAcademy_IdentityEmailProject.Models
{
    public class MessageDetailViewModel
    {
        public int MessageId { get; set; }
        public string Subject { get; set; }
        public string BodyHtml { get; set; } //temizlenmiş içerikmiş 
        public string SenderEmail { get; set; }
        public string ReciverEmail { get; set; }
        public DateTime SendDate { get; set; }
        public string? CategoryName { get; set; }
        public bool IsRead { get; set; }
        public bool IsReceived { get; set; } //giriş yapan kullanıcı alıcı mı? demekmiş.
                                             //aynı sayfanın hem gelen hem gönderilen için doğru “geri” butonunu ve menüyü seçmesini sağlıyor.
    }
}
