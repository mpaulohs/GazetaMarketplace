using System;

namespace GazetaMarketplace.Core.Entities;

/// <summary>
/// Um pedido de "Esqueci minha senha" (RC-11), exista ou não a conta: o limite por e-mail e por IP e o total diário
/// saem daqui. Fica no banco, e não na memória, porque o IIS recicla o processo e zeraria a contagem.
/// Entradas com mais de 24 horas são apagadas todo dia.
/// </summary>
public sealed class PasswordRecoveryAttempt
{
    public int Id { get; set; }

    /// <summary>E-mail digitado, em minúsculas.</summary>
    public string Email { get; set; }

    public string Ip { get; set; }

    public DateTime RequestedAt { get; set; }
}
