namespace GastroGestion.Application.Abstractions.Notifications;

/// <summary>
/// Port for sending an email. The default implementation logs the message; a real SMTP/provider
/// adapter can be swapped in once credentials are configured, without touching callers.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}
