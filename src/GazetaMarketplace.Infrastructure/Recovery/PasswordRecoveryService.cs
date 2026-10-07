using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Configuration;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GazetaMarketplace.Infrastructure.Recovery;

/// <inheritdoc cref="IPasswordRecovery"/>
public sealed class PasswordRecoveryService(
    AppDbContext db,
    UserManager<AppUser> users,
    RecoveryTokenProvider tokens,
    IAuditLog audit,
    PasswordRecoveryQueue queue,
    IOptions<SiteOptions> site,
    AccountOriginLockout lockout,
    TimeProvider time,
    ILogger<PasswordRecoveryService> log) : IPasswordRecovery
{
    /// <summary>RC-11: pedidos por hora para o mesmo e-mail.</summary>
    public const int MaxRequestsPerEmailPerHour = 3;

    /// <summary>Pedidos por hora vindos do mesmo IP; maior que o limite por e-mail porque um escritório tem várias pessoas.</summary>
    public const int MaxRequestsPerIpPerHour = 10;

    /// <summary>RC-11: o plano gratuito do SendGrid permite 100 e-mails por dia; o aviso sai ao chegar a 80.</summary>
    public const int DailyWarningThreshold = 80;

    /// <summary>Quanto tempo uma tentativa fica no banco.</summary>
    public static readonly TimeSpan Retention = TimeSpan.FromHours(24);

    private static readonly TimeSpan Window = TimeSpan.FromHours(1);

    public async Task RequestAsync(string email, string source, string fallbackBaseUrl, string traceId, CancellationToken cancellationToken)
    {
        string normalized = email.Trim().ToLowerInvariant();
        DateTime now = time.GetUtcNow().UtcDateTime;

        // Conta existente ou não, o caminho até aqui é o mesmo: grava o pedido e conta (RC-13, decisão C)
        db.PasswordRecoveryAttempts.Add(new PasswordRecoveryAttempt { Email = normalized, Ip = source, RequestedAt = now });
        await db.SaveChangesAsync(cancellationToken);

        DateTime hourAgo = now - Window;
        DateTime dayAgo = now - Retention;
        int byEmail = await db.PasswordRecoveryAttempts.CountAsync(a => a.Email == normalized && a.RequestedAt > hourAgo, cancellationToken);
        int byIp = await db.PasswordRecoveryAttempts.CountAsync(a => a.Ip == source && a.RequestedAt > hourAgo, cancellationToken);
        int today = await db.PasswordRecoveryAttempts.CountAsync(a => a.RequestedAt > dayAgo, cancellationToken);

        if (today >= DailyWarningThreshold)
        {
            log.LogWarning(
                "Pedidos de redefinição de senha nas últimas 24 horas: {Total}. O plano gratuito do SendGrid permite 100 e-mails por dia",
                today);
        }

        // O pedido que acabou de entrar já conta: o 4º no mesmo e-mail (ou o 11º no mesmo IP) é o primeiro barrado
        if (byEmail > MaxRequestsPerEmailPerHour)
        {
            log.LogWarning("Pedido de redefinição acima do limite de {Limit} por hora para {Email}: nenhum e-mail enviado", MaxRequestsPerEmailPerHour, normalized);
            return;
        }

        if (byIp > MaxRequestsPerIpPerHour)
        {
            log.LogWarning("Pedido de redefinição acima do limite de {Limit} por hora vindo de {Source}: nenhum e-mail enviado", MaxRequestsPerIpPerHour, source);
            return;
        }

        string baseUrl = string.IsNullOrWhiteSpace(site.Value.BaseUrl) ? fallbackBaseUrl : site.Value.BaseUrl;
        if (!queue.TryEnqueue(new RecoveryJob(normalized, baseUrl, traceId)))
        {
            log.LogWarning("Fila de envio de redefinição cheia: pedido descartado (traceId {TraceId})", traceId);
        }
    }

    public async Task<RecoveryLinkState> CheckLinkAsync(int userId, string code, CancellationToken cancellationToken)
    {
        (AppUser user, string token) = await FindAsync(userId, code);
        return user is null ? RecoveryLinkState.Invalid : tokens.Inspect(UserManager<AppUser>.ResetPasswordTokenPurpose, token, user);
    }

    public async Task<PasswordResetResult> ResetAsync(int userId, string code, string newPassword, CancellationToken cancellationToken)
    {
        (AppUser user, string token) = await FindAsync(userId, code);
        if (user is null)
        {
            return PasswordResetResult.BadLink(RecoveryLinkState.Invalid);
        }

        RecoveryLinkState state = tokens.Inspect(UserManager<AppUser>.ResetPasswordTokenPurpose, token, user);
        if (state != RecoveryLinkState.Valid)
        {
            return PasswordResetResult.BadLink(state);
        }

        // Atômico: o Identity valida o token e a política de senha antes de gravar, então senha fraca não gasta o link
        IdentityResult change = await users.ResetPasswordAsync(user, token, newPassword);
        if (!change.Succeeded)
        {
            IdentityError[] errors = [.. change.Errors];
            return errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken))
                ? PasswordResetResult.BadLink(RecoveryLinkState.Used)
                : PasswordResetResult.WeakPassword([.. errors.Select(e => e.Description)]);
        }

        // RC-12: quem foi bloqueado por tentativas volta a entrar; e a senha agora é escolha da própria pessoa
        lockout.ClearAccount(user.Id);
        if (user.MustChangePassword)
        {
            user.MustChangePassword = false;
            await users.UpdateAsync(user);
        }

        // Sem senha, token ou link na auditoria
        await audit.RecordAsync(new AuditRecord("user.recover_password", "User", user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)), cancellationToken);
        log.LogInformation("Usuário {UserId} redefiniu a senha pelo link enviado por e-mail", user.Id);
        return PasswordResetResult.Success;
    }

    /// <summary>Apaga as tentativas com mais de 24 horas (job diário). Devolve quantas apagou.</summary>
    public static Task<int> PurgeAsync(AppDbContext context, TimeProvider clock, CancellationToken cancellationToken)
    {
        DateTime limit = clock.GetUtcNow().UtcDateTime - Retention;
        return context.PasswordRecoveryAttempts.Where(a => a.RequestedAt < limit).ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<(AppUser User, string Token)> FindAsync(int userId, string code)
    {
        string token = RecoveryCode.Decode(code);
        AppUser user = token is null ? null : await users.FindByIdAsync(userId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return user is { IsActive: true } ? (user, token) : (null, null);
    }
}
