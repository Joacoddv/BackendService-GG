using GastroGestion.Application.Abstractions.Notifications;
using GastroGestion.Application.Abstractions.Persistence;

namespace GastroGestion.Application.Clientes.Cumpleaneros;

/// <summary>Sends a birthday promo email to every cliente (with an email) whose birthday is in Mes.</summary>
public sealed record EnviarPromoCumpleanosCommand(int Mes);

public sealed record EnviarPromoResult(int Enviados, int SinEmail);

public sealed class EnviarPromoCumpleanosHandler
{
    private readonly GetCumpleanerosHandler _cumpleaneros;
    private readonly IEmailSender           _email;

    public EnviarPromoCumpleanosHandler(GetCumpleanerosHandler cumpleaneros, IEmailSender email)
    {
        _cumpleaneros = cumpleaneros;
        _email        = email;
    }

    public async Task<EnviarPromoResult> Handle(EnviarPromoCumpleanosCommand cmd, CancellationToken ct = default)
    {
        var cumpleaneros = await _cumpleaneros.Handle(new GetCumpleanerosQuery(cmd.Mes), ct);

        var enviados = 0;
        var sinEmail = 0;
        foreach (var c in cumpleaneros)
        {
            if (string.IsNullOrWhiteSpace(c.Email))
            {
                sinEmail++;
                continue;
            }

            await _email.SendAsync(
                c.Email,
                "¡Feliz cumpleaños de GastroGestión! 🎉",
                $"Hola {c.Nombre}, ¡feliz cumpleaños! Te esperamos con una promoción especial este mes.",
                ct);
            enviados++;
        }

        return new EnviarPromoResult(enviados, sinEmail);
    }
}
