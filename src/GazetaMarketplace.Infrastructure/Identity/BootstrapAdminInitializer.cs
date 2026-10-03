using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Configuration;
using GazetaMarketplace.Core.Team;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GazetaMarketplace.Infrastructure.Identity;

/// <summary>Configuração do primeiro Administrador inválida: o site não sobe, em vez de ficar sem acesso ao painel (ADR-011).</summary>
public sealed class InvalidBootstrapException(string message) : InvalidOperationException(message);

/// <summary>
/// Cria o primeiro Administrador na partida (S17, ADR-003): só se <c>Bootstrap__AdminEmail</c> e <c>Bootstrap__AdminPassword</c>
/// existirem e nenhuma conta tiver o papel Administrador (ativa ou não: quem reativa é outro Administrador, não esta rotina).
/// A conta nasce com troca de senha obrigatória. A senha nunca vai para o log.
/// </summary>
public sealed class BootstrapAdminInitializer(
    IServiceScopeFactory scopes,
    IOptions<BootstrapOptions> options,
    ILogger<BootstrapAdminInitializer> log) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellation)
    {
        string email = options.Value.AdminEmail?.Trim();
        string adminPassword = options.Value.AdminPassword;
        bool hasEmail = !string.IsNullOrWhiteSpace(email);
        bool hasPassword = !string.IsNullOrEmpty(adminPassword);

        // Sem variáveis, nada a fazer e nem o banco é consultado
        if (!hasEmail && !hasPassword)
        {
            return;
        }

        using IServiceScope scope = scopes.CreateScope();
        UserManager<AppUser> users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        bool alreadyExists;
        try
        {
            alreadyExists = (await users.GetUsersInRoleAsync(RoleNames.Administrator)).Count > 0;
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            // Banco fora do ar na partida: o site sobe e o /health/ready avisa; a próxima partida tenta de novo
            log.LogError(error, "Não foi possível verificar se já existe um Administrador; o Administrador inicial não foi criado");
            return;
        }

        if (alreadyExists)
        {
            // RC-19: variável esquecida no servidor
            log.LogWarning(
                "Já existe um Administrador, mas {Variables} ainda existe(m) no ambiente do servidor. Remova-a(s): não têm mais efeito e deixam uma senha guardada em texto",
                string.Join(" e ", new[] { hasEmail ? "Bootstrap__AdminEmail" : null, hasPassword ? "Bootstrap__AdminPassword" : null }.Where(v => v is not null)));
            return;
        }

        await CreateAsync(users, email, adminPassword, hasEmail && hasPassword);
    }

    public Task StopAsync(CancellationToken cancellation) => Task.CompletedTask;

    private async Task CreateAsync(UserManager<AppUser> users, string email, string adminPassword, bool complete)
    {
        if (!complete)
        {
            throw new InvalidBootstrapException("Bootstrap__AdminEmail e Bootstrap__AdminPassword precisam existir juntas para criar o primeiro Administrador.");
        }

        if (!new EmailAddressAttribute().IsValid(email))
        {
            throw new InvalidBootstrapException("Bootstrap__AdminEmail não é um e-mail válido.");
        }

        AppUser administrator = new()
        {
            UserName = email,
            Email = email,
            FullName = "Administrador",
            IsActive = true,
            MustChangePassword = true
        };

        // Valida a política de senha antes de tocar no banco; as mensagens não repetem a senha
        foreach (IPasswordValidator<AppUser> validator in users.PasswordValidators)
        {
            IdentityResult validation = await validator.ValidateAsync(users, administrator, adminPassword);
            if (!validation.Succeeded)
            {
                throw new InvalidBootstrapException(
                    "Bootstrap__AdminPassword não cumpre a política de senha: " + string.Join("; ", validation.Errors.Select(e => e.Description)));
            }
        }

        try
        {
            IdentityResult created = await users.CreateAsync(administrator, adminPassword);
            if (!created.Succeeded)
            {
                // Outra instância pode ter criado a conta no mesmo instante: o e-mail repetido é o caso esperado
                log.LogError("O Administrador inicial não foi criado: {Errors}", string.Join(", ", created.Errors.Select(e => e.Code)));
                return;
            }

            await users.AddToRoleAsync(administrator, RoleNames.Administrator);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            log.LogError(error, "Não foi possível criar o Administrador inicial");
            return;
        }

        log.LogInformation(
            "Administrador inicial criado: usuário {UserId} ({Email}), com troca de senha obrigatória no primeiro acesso. Remova Bootstrap__AdminEmail e Bootstrap__AdminPassword do servidor",
            administrator.Id, email);
    }
}
