using System;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace GazetaMarketplace.Infrastructure.Recovery;

/// <summary>
/// Monta e envia o e-mail de redefinição: só para conta que existe e está ativa (US-007, "conta desativada não recebe").
/// O link e o token nunca vão para o log.
/// </summary>
public sealed class PasswordRecoveryMailer(
    UserManager<AppUser> users,
    IEmailSender sender,
    ILogger<PasswordRecoveryMailer> log)
{
    public async Task SendAsync(RecoveryJob job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);

        AppUser user = await users.FindByEmailAsync(job.Email);
        if (user is null || !user.IsActive)
        {
            log.LogInformation("Pedido de redefinição para {Recipient} sem conta ativa: nenhum e-mail enviado", job.Email);
            return;
        }

        if (job.Kind == RecoveryJobKind.LockoutNotice)
        {
            await sender.SendAsync(ComposeLockoutNotice(user, job.BaseUrl.TrimEnd('/') + "/painel/esqueci-minha-senha"), cancellationToken);
            return;
        }

        string token = await users.GeneratePasswordResetTokenAsync(user);
        string link = job.BaseUrl.TrimEnd('/') + RecoveryCode.LinkPath
            + "?id=" + user.Id.ToString(CultureInfo.InvariantCulture)
            + "&code=" + RecoveryCode.Encode(token);

        await sender.SendAsync(Compose(user, link), cancellationToken);
    }

    // SC-03: sem link com credencial; só o endereço da página "Esqueci minha senha", que qualquer pessoa já vê
    private static EmailMessage ComposeLockoutNotice(AppUser user, string forgotPasswordUrl)
    {
        string name = string.IsNullOrWhiteSpace(user.FullName) ? "" : " " + user.FullName.Trim();
        string text = $"""
            Olá{name},

            Houve várias tentativas seguidas de entrar na sua conta da área da equipe do GazetaMarketplace com a senha errada, e a entrada foi bloqueada por alguns minutos para a rede de onde elas vieram.

            Se foi você, espere e tente de novo com a senha certa. Se não foi, escolha uma nova senha agora, por este endereço:

            {forgotPasswordUrl}
            """;

        string safeName = WebUtility.HtmlEncode(name);
        string safeUrl = WebUtility.HtmlEncode(forgotPasswordUrl);
        string html = $"""
            <p>Olá{safeName},</p>
            <p>Houve várias tentativas seguidas de entrar na sua conta da área da equipe do GazetaMarketplace com a senha errada, e a entrada foi bloqueada por alguns minutos para a rede de onde elas vieram.</p>
            <p>Se foi você, espere e tente de novo com a senha certa. Se não foi, <a href="{safeUrl}">escolha uma nova senha agora</a>.</p>
            """;

        return new EmailMessage(user.Email, PasswordRecoveryMessages.LockoutNoticeSubject, text, html);
    }

    private static EmailMessage Compose(AppUser user, string link)
    {
        string name = string.IsNullOrWhiteSpace(user.FullName) ? "" : " " + user.FullName.Trim();
        string text = $"""
            Olá{name},

            Recebemos um pedido para redefinir a senha da sua conta na área da equipe do GazetaMarketplace.

            Para escolher uma nova senha, abra o link abaixo. Ele vale por 1 hora e só pode ser usado uma vez:

            {link}

            Se você não solicitou a redefinição, ignore este e-mail: sua senha continua a mesma.
            """;

        string safeName = WebUtility.HtmlEncode(name);
        string safeLink = WebUtility.HtmlEncode(link);
        string html = $"""
            <p>Olá{safeName},</p>
            <p>Recebemos um pedido para redefinir a senha da sua conta na área da equipe do GazetaMarketplace.</p>
            <p>Para escolher uma nova senha, use o botão ou o link abaixo. Ele vale por <strong>1 hora</strong> e só pode ser usado uma vez.</p>
            <p><a href="{safeLink}">Redefinir minha senha</a></p>
            <p>Se o botão não abrir, copie e cole este endereço no navegador:<br>{safeLink}</p>
            <p>Se você não solicitou a redefinição, ignore este e-mail: sua senha continua a mesma.</p>
            """;

        return new EmailMessage(user.Email, PasswordRecoveryMessages.EmailSubject, text, html);
    }
}
