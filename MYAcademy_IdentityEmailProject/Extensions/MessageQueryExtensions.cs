using MYAcademy_IdentityEmailProject.Entities;

namespace MYAcademy_IdentityEmailProject.Extensions
{
    public static class MessageQueryExtensions
    {
        public static IQueryable<Message> InboxOf(this IQueryable<Message> q, string email) =>
            q.Where(m => m.ReceiverEmail == email && !m.IsDraft
                      && !m.IsTrashedByReceiver && !m.IsDeletedByReceiver);

        public static IQueryable<Message> SentOf(this IQueryable<Message> q, string email) =>
            q.Where(m => m.SenderEmail == email && !m.IsDraft
                      && !m.IsTrashedBySender && !m.IsDeletedBySender);

        public static IQueryable<Message> DraftsOf(this IQueryable<Message> q, string email) =>
            q.Where(m => m.SenderEmail == email && m.IsDraft && !m.IsDeletedBySender);

        public static IQueryable<Message> StarredOf(this IQueryable<Message> q, string email) =>
            q.Where(m => !m.IsDraft && (
                (m.ReceiverEmail == email && m.IsStarredByReceiver && !m.IsTrashedByReceiver && !m.IsDeletedByReceiver) ||
                (m.SenderEmail == email && m.IsStarredBySender && !m.IsTrashedBySender && !m.IsDeletedBySender)));

        public static IQueryable<Message> TrashOf(this IQueryable<Message> q, string email) =>
            q.Where(m => !m.IsDraft && (
                (m.ReceiverEmail == email && m.IsTrashedByReceiver && !m.IsDeletedByReceiver) ||
                (m.SenderEmail == email && m.IsTrashedBySender && !m.IsDeletedBySender)));

        // Kullanıcının mesajı AÇABİLECEĞİ tüm durumlar (detay sayfası için)
        public static IQueryable<Message> AccessibleBy(this IQueryable<Message> q, string email) =>
            q.Where(m => (m.ReceiverEmail == email && !m.IsDraft && !m.IsDeletedByReceiver)
                      || (m.SenderEmail == email && !m.IsDeletedBySender));
    }
}