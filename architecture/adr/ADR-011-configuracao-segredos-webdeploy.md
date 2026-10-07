# ADR-011: Configuração, segredos e publicação por WebDeploy no IIS compartilhado

**Date**: 2026-09-30
**Status**: Accepted

> **Em resumo:** o site é publicado pelo Visual Studio com WebDeploy no IIS do SmarterASP.NET. Nenhum segredo fica no repositório: os valores de produção entram como variáveis de ambiente no `web.config` publicado, a partir de um arquivo de transformação que existe só na máquina de quem publica. Fotos, logs e chaves ficam em pastas fora da raiz do site, que o WebDeploy não toca, e o perfil de publicação não apaga arquivos extras no servidor.

## Context
- Decisão do Product Owner (2026-09-30): publicação por WebDeploy (MSDeploy) do Visual Studio 2026 para o IIS do SmarterASP.NET.
- NFR-14: nenhum segredo no código nem no repositório. `rules/security.md`: User Secrets em desenvolvimento; cofre ou variáveis de ambiente em produção.
- Na hospedagem compartilhada não há acesso ao Application Pool nem ao `applicationHost.config`; o caminho disponível é a seção `<aspNetCore><environmentVariables>` do `web.config`, que o WebDeploy publica a cada vez.
- O WebDeploy sincroniza a raiz do site e pode apagar arquivos que não estão no projeto.
- Hoje o `src/GazetaMarketplace.Web/appsettings.Development.json` está versionado no git; o `.gitignore` já ignora `*.pubxml`.

## Options Considered
| Option | Pros | Cons |
|--------|------|------|
| **A. Variáveis no `web.config` por transformação `web.Production.config` fora do git; variáveis por site no painel do SmarterASP quando disponível** | Nenhum segredo no git; funciona no plano compartilhado; ordem de prioridade padrão do ASP.NET Core | O arquivo de transformação precisa existir na máquina de quem publica; os valores ficam em texto no `web.config` do servidor (protegido pelo IIS) |
| B. `appsettings.Production.json` com segredos, publicado | Simples | Segredo no pacote de publicação e, facilmente, no git |
| C. Editar o `web.config` à mão no servidor depois de cada publicação | Nada local | O WebDeploy sobrescreve a cada publicação; erro humano certo |
| D. Cofre de segredos (Azure Key Vault) | Padrão de mercado | Conta e identidade na nuvem que o plano compartilhado não oferece |

## Decision
Adopt **Option A** because é a única que mantém segredos fora do repositório e sobrevive a cada publicação no plano compartilhado; se o painel do SmarterASP oferecer variáveis por site, elas substituem a transformação (preferencial).

## Consequences
**Positive**: repositório sem segredos (NFR-14); publicação repetível; dados persistentes protegidos contra a sincronização do WebDeploy.
**Negative**: quem publica precisa do `web.Production.config` local (guardado em lugar seguro fora do git); uma publicação sem ele gera um site sem configuração.
**Risks**: publicar sem a transformação e o site subir com valores vazios. Mitigação: validação das opções na inicialização (`ValidateOnStart`) — sem conexão, pasta de fotos ou chave do SendGrid em produção, o site **não sobe** e o erro aparece no log, em vez de funcionar pela metade.

## v2 Upgrade Trigger
Revisit this decision when **any** of the following becomes true:
- A hospedagem mudar para uma plataforma com cofre de segredos ou variáveis de ambiente gerenciadas (Azure App Service, contêineres).
- A publicação passar a ser feita por pipeline de CI/CD (GitHub Actions) → segredos do pipeline.
- Mais de uma pessoa publicar com frequência (o arquivo local vira gargalo).

## Implementation Notes
- **Arquivos de configuração:**
  - `appsettings.json` (no git): só valores não sensíveis e padrões.
  - `appsettings.Development.json`: **sair do git** (`git rm --cached`) e entrar no `.gitignore`; valores locais em User Secrets (`dotnet user-secrets`). Tarefa do `/build`.
  - `.gitignore`: acrescentar `appsettings.Development.json`, `web.Production.config`, `*.pubxml.user` (o `*.pubxml` já está).
- **Prioridade:** variáveis de ambiente valem mais que o `appsettings.json` (ordem padrão do `WebApplication.CreateBuilder`); nomes com `__` para seções (`ConnectionStrings__DefaultConnection`).
- **Transformação** (`src/GazetaMarketplace.Web/web.Production.config`, fora do git), aplicada pelo SDK na publicação com `EnvironmentName=Production`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration xmlns:xdt="http://schemas.microsoft.com/XML-Document-Transform">
  <location>
    <system.webServer>
      <aspNetCore>
        <environmentVariables xdt:Transform="InsertIfMissing">
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" xdt:Transform="InsertIfMissing" />
          <environmentVariable name="ConnectionStrings__DefaultConnection" value="(valor real)" xdt:Transform="InsertIfMissing" />
          <environmentVariable name="PhotoStorage__BasePath" value="h:\root\home\mpaulohs-001\www\gazeta-fotos" xdt:Transform="InsertIfMissing" />
          <!-- demais variáveis de ARCHITECTURE.md §9 -->
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
```

- **Perfil de publicação (`Properties/PublishProfiles/*.pubxml`, fora do git):**

```xml
<PropertyGroup>
  <EnvironmentName>Production</EnvironmentName>
  <SkipExtraFilesOnServer>true</SkipExtraFilesOnServer>
  <!-- self-contained só se o servidor não tiver o runtime do .NET 10 (AR-02) -->
  <!-- <SelfContained>true</SelfContained><RuntimeIdentifier>win-x64</RuntimeIdentifier> -->
</PropertyGroup>
<ItemGroup>
  <Content Update="appsettings.Development.json" CopyToPublishDirectory="Never" />
</ItemGroup>
<ItemGroup>
  <!-- protege qualquer pasta de dados que precise ficar dentro da raiz do site -->
  <MsDeploySkipRules Include="ProtegerDados">
    <ObjectName>dirPath</ObjectName>
    <AbsolutePath>App_Data</AbsolutePath>
  </MsDeploySkipRules>
</ItemGroup>
```

- **Pastas persistentes fora da raiz** (caminhos reais confirmados pelo SmarterASP em 2026-10-07, AR-01): `PhotoStorage__BasePath` = `h:\root\home\mpaulohs-001\www\gazeta-fotos`, `Logging__FileDirectory` = `h:\root\home\mpaulohs-001\www\gazeta-logs`, `DataProtection__KeysDirectory` = `h:\root\home\mpaulohs-001\www\gazeta-chaves`; as três irmãs do site, com leitura e escrita para o pool. As chaves do Data Protection usam o repositório de arquivos nessa pasta. A cifra por DPAPI (`DataProtection__ProtectWithDpapi`) é opcional e **começa desligada**: depende de o pool do provedor ter o perfil do usuário carregado, o que só se sabe testando; sem ela as chaves ficam sem criptografia adicional, protegidas pela permissão da pasta.
- **Validação na inicialização:** opções tipadas com `ValidateDataAnnotations().ValidateOnStart()` para conexão, pastas e SendGrid em produção.
- **Primeiro Administrador:** variáveis `Bootstrap__AdminEmail` e `Bootstrap__AdminPassword` só na primeira publicação; retirar do `web.Production.config` em seguida (ADR-003).
- **Banco:** script idempotente de migrations executado antes de publicar o site novo (ADR-004).
- **Ordem de uma publicação:** (1) gerar e aplicar o script do banco; (2) publicar pelo perfil; (3) conferir `/health/ready`.
