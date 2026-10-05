using System.Collections.Generic;

namespace GazetaMarketplace.Core.Ads;

/// <summary>
/// Uma linha "rótulo: valor" das características do anúncio, já em texto pronto para mostrar. <see cref="Key"/> é a chave estável do campo ("jobAreaIds"):
/// quem precisa achar um campo específico procura por ela, nunca pelo rótulo, que é texto de tela e pode mudar. <see cref="Items"/> traz os itens de um campo de
/// múltipla escolha, um a um (<see cref="Value"/> é só a junção deles com vírgula); nos demais campos é nulo.
/// </summary>
public sealed record AdSpec(string Label, string Value, string Key = null, IReadOnlyList<string> Items = null);
