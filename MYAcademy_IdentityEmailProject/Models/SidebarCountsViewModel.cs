using MYAcademy_IdentityEmailProject.Entities;

namespace MYAcademy_IdentityEmailProject.Models
{
    public class SidebarCountsViewModel
    {
        public int UnreadCount { get; set; }
        public int DraftCount { get; set; }
        public int StarredCount { get; set; }
        public int TrashCount { get; set; }

        public List<SidebarCategoryItem> Categories { get; set; } = new();
        public class SidebarCategoryItem
        {
            public string Name { get; set; } = "";
            public int UnreadCount { get; set; }
        }
    }
}
