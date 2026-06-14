namespace ECommerceFinalProject.Services;

public interface IEmailService
{
    Task GuiMaXacThucAsync(string email, string maXacThuc, string hoTen);
}
