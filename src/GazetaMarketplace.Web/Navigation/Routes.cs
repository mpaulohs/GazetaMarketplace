namespace GazetaMarketplace.Web.Navigation;

/// <summary>Caminhos do site público usados pelo layout. Cada tarefa que cria a tela confirma o caminho aqui.</summary>
public static class PublicRoutes
{
    public const string Home = "/";

    /// <summary>Endereço da página de uma categoria: <c>/categoria/{slug}</c> (o slug é único na árvore, então um segmento basta).</summary>
    public static string Category(string slug) => "/categoria/" + slug;

    /// <summary>Endereço da página do anúncio: <c>/anuncio/{id}/{slug}</c>.</summary>
    public static string Ad(int id, string title) => GazetaMarketplace.Core.Ads.AdRoutes.Detail(id, title);

    public const string Search = "/busca";

    public const string Favorites = "/favoritos";
}

/// <summary>Caminhos do painel da equipe. Cada tarefa da Fase 1 em diante confirma o caminho aqui.</summary>
public static class PanelRoutes
{
    public const string Ads = "/painel/anuncios";

    public const string Categories = "/painel/categorias";

    public const string Users = "/painel/usuarios";

    public const string Settings = "/painel/configuracoes";

    /// <summary>Página inicial do Administrador. Provisória até a tarefa 4.1.</summary>
    public const string ReviewQueue = "/painel/anuncios/fila";

    public const string SignIn = "/painel/entrar";

    public const string SignOut = "/painel/sair";

    public const string AccessDenied = "/painel/acesso-negado";

    public const string SetPassword = "/painel/definir-senha";

    public const string ForgotPassword = "/painel/esqueci-minha-senha";

    /// <summary>Destino do link enviado por e-mail; leva <c>?id=</c> e <c>&amp;code=</c>.</summary>
    public const string ResetPassword = "/painel/redefinir-senha";
}
