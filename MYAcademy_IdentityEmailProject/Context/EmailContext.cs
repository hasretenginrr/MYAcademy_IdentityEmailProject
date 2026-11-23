using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MYAcademy_IdentityEmailProject.Entities;

namespace MYAcademy_IdentityEmailProject.Context
{
    public class EmailContext: IdentityDbContext<AppUser>
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=HASRET\\SQLEXPRESS; initial Catalog=IdentityEmailDb; integrated Security=true; TrustServerCertificate=True",
            o => o.UseCompatibilityLevel(110));
        }

        public DbSet<Message> Messages { get; set; }
        public DbSet<Category> Categories { get; set; }

    }
}
