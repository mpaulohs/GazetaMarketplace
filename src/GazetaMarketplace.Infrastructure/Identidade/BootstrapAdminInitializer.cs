using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Configuracao;
using GazetaMarketplace.Core.Equipe;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GazetaMarketplace.Infrastructure.Identidade;

/// <summary>Configuração do primeiro Administrador inválida: o site não sobe, em vez de ficar sem acesso ao painel (ADR-011).</summary>
public sealed class BootstrapInvalidoException(string mensagem) : InvalidOperationException(mensagem);

/// <summary>
/// Cria o primeiro Administrador na partida (S17, ADR-003): só se <c>Bootstrap__AdminEmail</c> e <c>Bootstrap__AdminPassword</c>
/// existirem e nenhuma conta tiver o papel Administrador (ativa ou não: quem reativa é outro Administrador, não esta rotina).
/// A conta nasce com troca de senha obrigatória. A senha nunca vai para o log.
/// </summary>
public sealed class BootstrapAdminInitializer(
    IServiceScopeFactory escopos,
    IOptions<BootstrapOptions> opcoes,
    ILogger<BootstrapAdminInitializer> log) : IHostedService
{
    public async Task StartAsync(CancellationToken cancelamento)
    {
        string email = opcoes.Value.AdminEmail?.Trim();
        string senha = opcoes.Value.AdminPassword;
        bool temEmail = !string.IsNullOrWhiteSpace(email);
        bool temSenha = !string.IsNullOrEmpty(senha);

        // Sem variáveis, nada a fazer e nem o banco é consultado
        if (!temEmail && !temSenha)
        {
            return;
        }

        using IServiceScope escopo = escopos.CreateScope();
        UserManager<UsuarioIdentity> usuarios = escopo.ServiceProvider.GetRequiredService<UserManager<UsuarioIdentity>>();

        bool jaExiste;
        try
        {
            jaExiste = (await usuarios.GetUsersInRoleAsync(Papeis.Administrador)).Count > 0;
        }
        catch (Exception erro) when (erro is not OperationCanceledException)
        {
            // Banco fora do ar na partida: o site sobe e o /health/ready avisa; a próxima partida tenta de novo
            log.LogError(erro, "Não foi possível verificar se já existe um Administrador; o Administrador inicial não foi criado");
            return;
        }

        if (jaExiste)
        {
            // RC-19: variável esquecida no servidor
            log.LogWarning(
                "Já existe um Administrador, mas {Variaveis} ainda existe(m) no ambiente do servidor. Remova-a(s): não têm mais efeito e deixam uma senha guardada em texto",
                string.Join(" e ", new[] { temEmail ? "Bootstrap__AdminEmail" : null, temSenha ? "Bootstrap__AdminPassword" : null }.Where(v => v is not null)));
            return;
        }

        await CriarAsync(usuarios, email, senha, temEmail && temSenha);
    }

    public Task StopAsync(CancellationToken cancelamento) => Task.CompletedTask;

    private async Task CriarAsync(UserManager<UsuarioIdentity> usuarios, string email, string senha, bool completas)
    {
        if (!completas)
        {
            throw new BootstrapInvalidoException("Bootstrap__AdminEmail e Bootstrap__AdminPassword precisam existir juntas para criar o primeiro Administrador.");
        }

        if (!new EmailAddressAttribute().IsValid(email))
        {
            throw new BootstrapInvalidoException("Bootstrap__AdminEmail não é um e-mail válido.");
        }

        UsuarioIdentity administrador = new()
        {
            UserName = email,
            Email = email,
            FullName = "Administrador",
            IsActive = true,
            MustChangePassword = true
        };

        // Valida a política de senha antes de tocar no banco; as mensagens não repetem a senha
        foreach (IPasswordValidator<UsuarioIdentity> validador in usuarios.PasswordValidators)
        {
            IdentityResult validacao = await validador.ValidateAsync(usuarios, administrador, senha);
            if (!validacao.Succeeded)
            {
                throw new BootstrapInvalidoException(
                    "Bootstrap__AdminPassword não cumpre a política de senha: " + string.Join("; ", validacao.Errors.Select(e => e.Description)));
            }
        }

        try
        {
            IdentityResult criado = await usuarios.CreateAsync(administrador, senha);
            if (!criado.Succeeded)
            {
                // Outra instância pode ter criado a conta no mesmo instante: o e-mail repetido é o caso esperado
                log.LogError("O Administrador inicial não foi criado: {Erros}", string.Join(", ", criado.Errors.Select(e => e.Code)));
                return;
            }

            await usuarios.AddToRoleAsync(administrador, Papeis.Administrador);
        }
        catch (Exception erro) when (erro is not OperationCanceledException)
        {
            log.LogError(erro, "Não foi possível criar o Administrador inicial");
            return;
        }

        log.LogInformation(
            "Administrador inicial criado: usuário {UsuarioId} ({Email}), com troca de senha obrigatória no primeiro acesso. Remova Bootstrap__AdminEmail e Bootstrap__AdminPassword do servidor",
            administrador.Id, email);
    }
}
