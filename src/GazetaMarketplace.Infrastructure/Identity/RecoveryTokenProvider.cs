using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Team;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace GazetaMarketplace.Infrastructure.Identity;

/// <summary>
/// Token do link de redefinição de senha (US-007). Tem o mesmo desenho do token padrão do Identity (protegido pelo Data Protection,
/// amarrado à pessoa, ao propósito e ao carimbo de segurança), com duas diferenças que o padrão não tem:
/// o relógio é o <see cref="TimeProvider"/> do site, e o resultado diz se o link <em>expirou</em> ou <em>já foi usado</em>
/// (a SPEC mostra mensagens diferentes). Vale pelo tempo de <see cref="DataProtectionTokenProviderOptions.TokenLifespan"/> (1 hora)
/// e só uma vez: trocar a senha muda o carimbo de segurança, e o mesmo link deixa de valer.
/// </summary>
public sealed class RecoveryTokenProvider(
    IDataProtectionProvider dataProtection,
    TimeProvider time,
    IOptions<DataProtectionTokenProviderOptions> options) : IUserTwoFactorTokenProvider<AppUser>
{
    public const string Name = "GazetaRecovery";

    private readonly IDataProtector _protector = dataProtection.CreateProtector("Gazeta.PasswordRecovery.v1");

    public Task<string> GenerateAsync(string purpose, UserManager<AppUser> manager, AppUser user)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(user);

        using MemoryStream stream = new();
        using (BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(time.GetUtcNow().UtcTicks);
            writer.Write(user.Id);
            writer.Write(purpose ?? string.Empty);
            writer.Write(user.SecurityStamp ?? string.Empty);
        }

        return Task.FromResult(Convert.ToBase64String(_protector.Protect(stream.ToArray())));
    }

    public Task<bool> ValidateAsync(string purpose, string token, UserManager<AppUser> manager, AppUser user) =>
        Task.FromResult(Inspect(purpose, token, user) == RecoveryLinkState.Valid);

    public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<AppUser> manager, AppUser user) => Task.FromResult(false);

    /// <summary>Diz em que situação o link está: vale, expirou (passou de 1 hora), já foi usado (o carimbo mudou) ou não é válido.</summary>
    public RecoveryLinkState Inspect(string purpose, string token, AppUser user)
    {
        if (user is null || string.IsNullOrEmpty(token))
        {
            return RecoveryLinkState.Invalid;
        }

        long issuedTicks;
        int userId;
        string tokenPurpose;
        string stamp;
        try
        {
            using MemoryStream stream = new(_protector.Unprotect(Convert.FromBase64String(token)));
            using BinaryReader reader = new(stream, Encoding.UTF8);
            issuedTicks = reader.ReadInt64();
            userId = reader.ReadInt32();
            tokenPurpose = reader.ReadString();
            stamp = reader.ReadString();
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException or EndOfStreamException or ArgumentException)
        {
            return RecoveryLinkState.Invalid;
        }

        if (userId != user.Id || !string.Equals(tokenPurpose, purpose ?? string.Empty, StringComparison.Ordinal))
        {
            return RecoveryLinkState.Invalid;
        }

        DateTimeOffset expiresAt = new DateTimeOffset(issuedTicks, TimeSpan.Zero) + options.Value.TokenLifespan;
        if (expiresAt < time.GetUtcNow())
        {
            return RecoveryLinkState.Expired;
        }

        return string.Equals(stamp, user.SecurityStamp ?? string.Empty, StringComparison.Ordinal)
            ? RecoveryLinkState.Valid
            : RecoveryLinkState.Used;
    }
}
