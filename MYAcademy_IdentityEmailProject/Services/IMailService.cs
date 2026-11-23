namespace MYAcademy_IdentityEmailProject.Services
{
    public interface IMailService
    {
        Task SendAsync(string to, string subject, string htmlBody);

    }
}
