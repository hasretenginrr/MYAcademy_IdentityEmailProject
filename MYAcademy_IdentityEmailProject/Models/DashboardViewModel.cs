namespace MYAcademy_IdentityEmailProject.Models
{
    public class DashboardViewModel
    {
        public string FullName { get; set; } = "";

        public int InboxCount { get; set; }
        public int UnreadCount { get; set; }
        public int SentCount { get; set; }
        public int DraftCount { get; set; }
        public int StarredCount { get; set; }

        // Yeni: detaylı istatistikler
        public int TodayReceived { get; set; }
        public int TodaySent { get; set; }
        public int WeekReceived { get; set; }       // son 7 gün
        public int PrevWeekReceived { get; set; }   // ondan önceki 7 gün
        public int WeekSent { get; set; }
        public int PrevWeekSent { get; set; }

        public List<RecentMessageItem> Recent { get; set; } = new();
        public List<CategoryShare> Categories { get; set; } = new();
        public List<DailyActivity> Activity { get; set; } = new();
        public List<SenderShare> TopSenders { get; set; } = new();
    }

    public class RecentMessageItem
    {
        public int MessageId { get; set; }
        public string Sender { get; set; } = "";
        public string Subject { get; set; } = "";
        public DateTime SendDate { get; set; }
        public bool IsRead { get; set; }
        public string? Category { get; set; }
    }

    public class CategoryShare
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }

    public class DailyActivity
    {
        public DateTime Date { get; set; }
        public int Received { get; set; }
        public int Sent { get; set; }
    }

    public class SenderShare
    {
        public string Email { get; set; } = "";
        public int Count { get; set; }
        public DateTime LastDate { get; set; }
    }
}