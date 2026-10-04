using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;

namespace GazetaMarketplace.Core.Ads;

/// <summary>
/// O que a pessoa preencheu no formulário do anúncio, ainda em texto. Nada aqui diz a situação, o autor ou a data de publicação: esses valores
/// nunca vêm do formulário (RC-14), só do servidor.
/// </summary>
/// <param name="Title">Título.</param>
/// <param name="Description">Descrição (ou "Informações adicionais").</param>
/// <param name="CategoryId">Categoria postável, ou nulo (rascunho só com o título).</param>
/// <param name="Price">Preço em notação brasileira, como digitado.</param>
/// <param name="Cep">CEP, com ou sem hífen.</param>
/// <param name="City">Cidade escolhida pelo navegador; vazia quando o navegador não a preencheu (o servidor consulta o CEP).</param>
/// <param name="Uf">Estado, com o mesmo critério da cidade.</param>
/// <param name="LocationCep">O CEP para o qual o navegador obteve <paramref name="City"/> e <paramref name="Uf"/>. Se for diferente do <paramref name="Cep"/> (o CEP mudou depois, sem JavaScript), cidade e estado enviados são descartados e o servidor consulta o CEP.</param>
/// <param name="LocationManual">Cidade e estado escolhidos à mão porque o serviço de CEP falhou (selo de conferência).</param>
/// <param name="Fields">Campos do grupo, pela chave do campo (<c>km</c>, <c>brandId</c>…); vários valores só em campos de várias opções.</param>
public sealed record AdDraftInput(
    string Title,
    string Description,
    int? CategoryId,
    string Price,
    string Cep,
    string City,
    string Uf,
    string LocationCep,
    bool LocationManual,
    IReadOnlyDictionary<string, string[]> Fields);

/// <summary>Como ficou o endereço depois de gravar.</summary>
public enum LocationOutcome
{
    /// <summary>Sem CEP.</summary>
    None = 0,

    /// <summary>Cidade e estado vieram do navegador (ou da escolha manual) e o servidor os conferiu.</summary>
    Informed = 1,

    /// <summary>O navegador não trouxe cidade e estado e o servidor os obteve do CEP.</summary>
    Resolved = 2,

    /// <summary>O CEP não existe: o rascunho foi salvo sem cidade e estado, pendentes para o envio.</summary>
    CepNotFound = 3,

    /// <summary>O serviço de CEP está fora do ar: o rascunho foi salvo sem cidade e o formulário abre no modo manual.</summary>
    CepUnavailable = 4
}

/// <param name="Id">O anúncio.</param>
/// <param name="RowVersion">A versão gravada, para o próximo salvamento detectar edição concorrente.</param>
/// <param name="Location">Como ficou o endereço.</param>
public sealed record AdDraftResult(int Id, byte[] RowVersion, LocationOutcome Location);

/// <summary>
/// Cria e salva o rascunho do anúncio (US-008). Salva em uma só gravação, junto com a auditoria. Só o título é obrigatório; o resto do que foi
/// preenchido é conferido, mas pode ficar incompleto.
/// </summary>
public interface IAdDraftService
{
    /// <summary>Cria o rascunho do usuário atual. Sem título, nada é criado.</summary>
    /// <exception cref="ValidationException">Algum campo recusado; as chaves são os nomes do formulário (<c>Title</c>, <c>Price</c>, <c>Fields[km]</c>…).</exception>
    Task<AdDraftResult> CreateAsync(AdDraftInput input, CancellationToken cancellationToken);

    /// <summary>
    /// Salva o anúncio mantendo a situação (um Rejeitado continua Rejeitado; o Administrador que edita não muda a situação, US-008-S13).
    /// </summary>
    /// <exception cref="NotFoundException">O anúncio não existe.</exception>
    /// <exception cref="ForbiddenException">Sem acesso (S10) ou situação que não aceita edição (S12).</exception>
    /// <exception cref="ConflictException">Outra pessoa salvou o anúncio depois que o formulário foi aberto.</exception>
    /// <exception cref="ValidationException">Algum campo recusado.</exception>
    Task<AdDraftResult> UpdateAsync(int id, byte[] rowVersion, AdDraftInput input, CancellationToken cancellationToken);
}
