using System.Net.Http.Headers;
using System.Text.Json;

namespace WordGuessingGame.API.Services;

public class EmailService : IEmailService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<EmailService> _logger;
    private readonly string _apiKey;
    private readonly string _fromEmail;
    private readonly string _appUrl;

    public EmailService(IHttpClientFactory httpFactory, IConfiguration config, ILogger<EmailService> logger)
    {
        _httpFactory = httpFactory;
        _logger    = logger;
        _apiKey    = config["Resend:ApiKey"]    ?? "";
        _fromEmail = config["Resend:FromEmail"] ?? "noreply@raadhetwoord.be";
        _appUrl    = config["App:BaseUrl"]      ?? "https://raadhetwoord.be";
    }

    public async Task SendConfirmationEmailAsync(string toEmail, string username, string token)
    {
        var confirmUrl = $"{_appUrl}/verificeren?token={Uri.EscapeDataString(token)}";
        var html = await LoadTemplate("Email/confirm-account.html");
        html = html.Replace("{{USERNAME}}", username)
                   .Replace("{{CONFIRM_URL}}", confirmUrl)
                   .Replace("{{UNSUBSCRIBE_URL}}", $"{_appUrl}/uitschrijven");

        await SendAsync(toEmail, "Bevestig je account", html);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string username, string token)
    {
        var resetUrl = $"{_appUrl}/wachtwoord-reset?token={Uri.EscapeDataString(token)}";
        var html = await LoadTemplate("Email/password-reset.html");
        html = html.Replace("{{USERNAME}}", username)
                   .Replace("{{RESET_URL}}", resetUrl)
                   .Replace("{{UNSUBSCRIBE_URL}}", $"{_appUrl}/uitschrijven");

        await SendAsync(toEmail, "Wachtwoord resetten", html);
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string username)
    {
        var html = await LoadTemplate("Email/welcome.html");
        html = html.Replace("{{USERNAME}}", username)
                   .Replace("{{APP_URL}}", _appUrl)
                   .Replace("{{UNSUBSCRIBE_URL}}", $"{_appUrl}/uitschrijven");

        await SendAsync(toEmail, "Welkom bij het spel!", html);
    }

    private static async Task<string> LoadTemplate(string path) =>
        await File.ReadAllTextAsync(path);

    private async Task SendAsync(string toEmail, string subject, string html)
    {
        var client = _httpFactory.CreateClient();

        var payload = new
        {
            from    = $"Raad het woord <{_fromEmail}>",
            to      = new[] { toEmail },
            subject,
            html
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
        {
            Content = JsonContent.Create(payload),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _apiKey) }
        };

        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Resend {Status}: {Body}", (int)response.StatusCode, body);
        }
    }
}
