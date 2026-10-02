namespace GazetaMarketplace.Web.Navegacao;

/// <summary>Caminhos do site público usados pelo layout. Cada tarefa que cria a tela confirma o caminho aqui.</summary>
public static class RotasPublicas
{
    public const string Busca = "/busca";

    public const string Favoritos = "/favoritos";
}

/// <summary>Caminhos do painel da equipe. Cada tarefa da Fase 1 em diante confirma o caminho aqui.</summary>
public static class RotasDoPainel
{
    public const string Anuncios = "/painel/anuncios";

    public const string Categorias = "/painel/categorias";

    public const string Usuarios = "/painel/usuarios";

    public const string Configuracoes = "/painel/configuracoes";

    /// <summary>Provisório até a tarefa 1.1 (entrar e sair).</summary>
    public const string Entrar = "/painel/entrar";

    /// <summary>Provisório até a tarefa 1.1 (entrar e sair).</summary>
    public const string Sair = "/painel/sair";
}
