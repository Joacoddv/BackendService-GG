using GastroGestion.Application.Abstractions.Notifications;
using Microsoft.Extensions.Logging;

namespace GastroGestion.Infrastructure.Notifications;

/// <summary>
/// Development email sender that logs the message instead of sending it. Replace with a real
/// SMTP/provider adapter (same interface) once credentials are configured.
/// </summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger) => _logger = logger;

    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation("EMAIL (dev/no-op) → To: {To} | Subject: {Subject} | Body: {Body}",
            to, subject, body);
        return Task.CompletedTask;
    }
}
