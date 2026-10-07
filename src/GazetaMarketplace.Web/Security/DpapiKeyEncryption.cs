using System;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.Logging;

namespace GazetaMarketplace.Web.Security;

/// <summary>
/// Cifra as chaves do Data Protection com o DPAPI do Windows, no escopo do usuário do pool de aplicação (RR-2). Só existe no Windows: pedir em outro sistema
/// falha na hora, com uma mensagem que diz o que fazer, em vez de deixar o site gravar as chaves sem cifra achando que estão protegidas.
/// </summary>
public static class DpapiKeyEncryption
{
    public const string NotWindowsMessage =
        "DataProtection:ProtectWithDpapi está ligado, mas o DPAPI só existe no Windows. Desligue a opção neste ambiente (Linux, contêiner, desenvolvimento).";

    /// <summary>Cria o cifrador; lança <see cref="PlatformNotSupportedException"/> fora do Windows.</summary>
    public static IXmlEncryptor Create(ILoggerFactory loggers)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(NotWindowsMessage);
        }

        return new DpapiXmlEncryptor(protectToLocalMachine: false, loggers);
    }
}
