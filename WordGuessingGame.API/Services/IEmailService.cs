namespace WordGuessingGame.API.Services;

public interface IEmailService
{
    Task SendConfirmationEmailAsync(string toEmail, string username, string token);
    Task SendPasswordResetEmailAsync(string toEmail, string username, string token);
    Task SendWelcomeEmailAsync(string toEmail, string username);
}
