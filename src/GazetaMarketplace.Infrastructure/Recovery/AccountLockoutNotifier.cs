using System;
using System.Collections.Generic;
using GazetaMarketplace.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GazetaMarketplace.Infrastructure.Recovery;

/// <summary>
/// Avisa por e-mail o dono de uma conta que está sendo bloqueada por tentativas repetidas (SC-03). O envio vai para a mesma fila da
/// redefinição de senha, fora do caminho da resposta, e sai no máximo uma vez por hora por conta: quem erra a senha de propósito
/// não consegue usar o aviso para encher a caixa de entrada nem a cota de e-mails do SendGrid.
/// </summary>
public sealed class AccountLockoutNotifier(
    PasswordRecoveryQueue queue,
    IOptions<SiteOptions> site,
    TimeProvider time,
    ILogger<AccountLockoutNotifier> log)
{
    public static readonly TimeSpan MinimumInterval = TimeSpan.FromHours(1);

    private readonly object _lock = new();
    private readonly Dictionary<string, DateTimeOffset> _last = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Põe o aviso na fila, se não houve outro para a mesma conta na última hora.</summary>
    public void Notify(string email, string fallbackBaseUrl, string traceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        DateTimeOffset now = time.GetUtcNow();
        lock (_lock)
        {
            if (_last.Count > 1024)
            {
                foreach (string key in new List<string>(_last.Keys))
                {
                    if (now - _last[key] > MinimumInterval)
                    {
                        _last.Remove(key);
                    }
                }
            }

            if (_last.TryGetValue(email, out DateTimeOffset previous) && now - previous < MinimumInterval)
            {
                return;
            }

            _last[email] = now;
        }

        string baseUrl = string.IsNullOrWhiteSpace(site.Value.BaseUrl) ? fallbackBaseUrl : site.Value.BaseUrl;
        if (!queue.TryEnqueue(new RecoveryJob(email.Trim().ToLowerInvariant(), baseUrl, traceId, RecoveryJobKind.LockoutNotice)))
        {
            log.LogWarning("Fila de envio cheia: aviso de bloqueio de conta descartado (traceId {TraceId})", traceId);
        }
    }
}
