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

    /// <summary>Página inicial do Administrador. Provisória até a tarefa 4.1.</summary>
    public const string Fila = "/painel/anuncios/fila";

    public const string Entrar = "/painel/entrar";

    public const string Sair = "/painel/sair";

    public const string AcessoNegado = "/painel/acesso-negado";

    /// <summary>Criada na tarefa 1.4; até lá o link do formulário de entrada leva a 404.</summary>
    public const string EsqueciSenha = "/painel/esqueci-minha-senha";
}
