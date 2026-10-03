using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GazetaMarketplace.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsPostable = table.Column<bool>(type: "bit", nullable: false),
                    FieldGroup = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.CheckConstraint("CK_Categories_NotOwnParent", "[ParentId] IS NULL OR [ParentId] <> [Id]");
                    table.ForeignKey(
                        name: "FK_Categories_Categories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DisplayOrder", "FieldGroup", "IsPostable", "IsSystem", "Name", "ParentId", "Slug", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, null, false, true, "Imóveis", null, "imoveis", null, null },
                    { 2, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, null, false, true, "Automóveis, Peças e Acessórios", null, "automoveis-pecas-e-acessorios", null, null },
                    { 4, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, null, false, true, "Celulares e Telefonia", null, "celulares-e-telefonia", null, null },
                    { 5, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, null, false, true, "Casa, Decoração e Utensílios", null, "casa-decoracao-e-utensilios", null, null },
                    { 6, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 5, null, false, true, "Esportes e Fitness", null, "esportes-e-fitness", null, null },
                    { 7, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 6, null, false, true, "Serviços", null, "servicos-grupo", null, null },
                    { 8, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 7, null, false, true, "Moda e beleza", null, "moda-e-beleza", null, null },
                    { 9, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 8, null, false, true, "Artigos infantis", null, "artigos-infantis", null, null },
                    { 10, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 9, null, false, true, "Animais de estimação", null, "animais-de-estimacao", null, null },
                    { 11, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 10, null, false, true, "Música e hobbies", null, "musica-e-hobbies", null, null },
                    { 12, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 11, null, false, true, "Agro e indústria", null, "agro-e-industria", null, null },
                    { 13, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 12, null, false, true, "Vagas de emprego", null, "vagas-de-emprego-grupo", null, null },
                    { 14, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 13, null, false, true, "Comércio", null, "comercio", null, null },
                    { 15, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 14, null, false, true, "Câmeras e Drones", null, "cameras-e-drones", null, null },
                    { 16, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 15, null, false, true, "Games", null, "games", null, null },
                    { 17, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 16, null, false, true, "TVs e video", null, "tvs-e-video", null, null },
                    { 18, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 17, null, false, true, "Áudio", null, "audio", null, null },
                    { 19, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 18, null, false, true, "Informática", null, "informatica", null, null },
                    { 20, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 19, null, false, true, "Eletro", null, "eletro", null, null },
                    { 21, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 20, null, false, true, "Móveis", null, "moveis", null, null },
                    { 22, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 21, null, false, true, "Materiais de Construção", null, "materiais-de-construcao", null, null },
                    { 23, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 22, null, false, true, "Escritório e Home Office", null, "escritorio-e-home-office", null, null },
                    { 3, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 6, null, false, true, "Autopeças", 2, "autopecas", null, null },
                    { 26, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, null, true, true, "Apartamentos", 1, "apartamentos", null, null },
                    { 27, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, null, true, true, "Casas", 1, "casas", null, null },
                    { 28, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, null, true, true, "Aluguel de quartos", 1, "aluguel-de-quartos", null, null },
                    { 29, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, null, true, true, "Temporada", 1, "temporada", null, null },
                    { 30, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 5, null, true, true, "Terrenos, sítios e fazendas", 1, "terrenos-sitios-e-fazendas", null, null },
                    { 31, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 6, null, true, true, "Comércio e indústria", 1, "comercio-e-industria", null, null },
                    { 33, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, null, true, true, "Carros, vans e utilitários", 2, "cars", null, null },
                    { 34, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, null, true, true, "Caminhões", 2, "caminhoes", null, null },
                    { 35, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, null, true, true, "Ônibus", 2, "onibus", null, null },
                    { 36, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, null, true, true, "Motos", 2, "motos", null, null },
                    { 37, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 5, null, true, true, "Barcos e aeronaves", 2, "barcos-e-aeronaves", null, null },
                    { 43, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, null, true, true, "Celulares e Smartphones", 4, "celulares-e-smartphones", null, null },
                    { 44, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, null, true, true, "Acessórios de Celular", 4, "acessorios-de-celular", null, null },
                    { 45, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, null, true, true, "Peças de Celular", 4, "pecas-de-celular", null, null },
                    { 46, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, null, true, true, "Smartwatches", 4, "smartwatches", null, null },
                    { 47, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 5, null, true, true, "Acessórios Para Smartwatch", 4, "acessorios-para-smartwatch", null, null },
                    { 48, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 6, null, true, true, "Telefonia Fixa e Sem Fio", 4, "telefonia-fixa-e-sem-fio", null, null },
                    { 49, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, null, true, true, "Tecidos de Cama, Mesa e Banho", 5, "tecidos-de-cama-mesa-e-banho", null, null },
                    { 50, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, null, true, true, "Decorações Para Casa", 5, "decoracoes-para-casa", null, null },
                    { 51, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, null, true, true, "Casa Inteligente", 5, "casa-inteligente", null, null },
                    { 52, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, null, true, true, "Utensílios Para Cozinha", 5, "utensilios-para-cozinha", null, null },
                    { 53, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 5, null, true, true, "Utensílios Para Banheiro e Limpeza", 5, "utensilios-para-banheiro-e-limpeza", null, null },
                    { 54, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 6, null, true, true, "Iluminação", 5, "iluminacao", null, null },
                    { 55, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 7, null, true, true, "Segurança Residencial", 5, "seguranca-residencial", null, null },
                    { 56, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 8, null, true, true, "Jardinagem e Plantas", 5, "jardinagem-e-plantas", null, null },
                    { 57, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 9, null, true, true, "Área Externa", 5, "area-externa", null, null },
                    { 58, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, null, true, true, "Ciclismo", 6, "ciclismo", null, null },
                    { 59, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, null, true, true, "Academia e Exercícios", 6, "academia-e-exercicios", null, null },
                    { 60, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, null, true, true, "Acampamento", 6, "acampamento", null, null },
                    { 61, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, null, true, true, "Esportes Sobre Rodas", 6, "esportes-sobre-rodas", null, null },
                    { 62, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 5, null, true, true, "Esportes de Quadra e Ao Ar Livre", 6, "esportes-de-quadra-e-ao-ar-livre", null, null },
                    { 63, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 6, null, true, true, "Esportes Aquáticos", 6, "esportes-aquaticos", null, null },
                    { 64, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 7, null, true, true, "Roupas Esportivas", 6, "roupas-esportivas", null, null },
                    { 65, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 8, null, true, true, "Calçados Esportivos", 6, "calcados-esportivos", null, null },
                    { 66, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, null, true, true, "Serviços", 7, "servicos", null, null },
                    { 67, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, null, true, true, "Beleza e Cuidados Pessoais", 8, "beleza-e-cuidados-pessoais", null, null },
                    { 68, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, null, true, true, "Roupas", 8, "roupas", null, null },
                    { 69, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, null, true, true, "Calçados", 8, "calcados", null, null },
                    { 70, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, null, true, true, "Bolsas, malas e mochilas", 8, "bolsas-malas-e-mochilas", null, null },
                    { 71, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 5, null, true, true, "Acessórios", 8, "acessorios", null, null },
                    { 72, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, null, true, true, "Roupas Infantis", 9, "roupas-infantis", null, null },
                    { 73, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, null, true, true, "Brinquedos e Jogos", 9, "brinquedos-e-jogos", null, null },
                    { 74, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, null, true, true, "Maternidade e Cuidados com o Bebê", 9, "maternidade-e-cuidados-com-o-bebe", null, null },
                    { 75, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, null, true, true, "Calçados Infantis", 9, "calcados-infantis", null, null },
                    { 76, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 5, null, true, true, "Roupas para Bebês", 9, "roupas-para-bebes", null, null },
                    { 77, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 6, null, true, true, "Calçados Para Bebês", 9, "calcados-para-bebes", null, null },
                    { 78, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 7, null, true, true, "Móveis Infantis", 9, "moveis-infantis", null, null },
                    { 81, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, null, true, true, "Acessórios para pets", 10, "acessorios-para-pets", null, null },
                    { 84, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, null, true, true, "Instrumentos musicais", 11, "instrumentos-musicais", null, null },
                    { 85, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, null, true, true, "CDs, DVDs etc", 11, "cds-dvds-etc", null, null },
                    { 86, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, null, true, true, "Livros e revistas", 11, "livros-e-revistas", null, null },
                    { 87, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, null, true, true, "Antiguidades", 11, "antiguidades", null, null },
                    { 88, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 5, null, true, true, "Hobbies e coleções", 11, "hobbies-e-colecoes", null, null },
                    { 89, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, null, true, true, "Tratores e máquinas agrícolas", 12, "tratores-e-maquinas-agricolas", null, null },
                    { 90, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, null, true, true, "Peças para tratores e máquinas", 12, "pecas-para-tratores-e-maquinas", null, null },
                    { 92, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, null, true, true, "Máquinas pesadas para construção", 12, "maquinas-pesadas-para-construcao", null, null },
                    { 93, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 5, null, true, true, "Máquinas para produção industrial", 12, "maquinas-para-producao-industrial", null, null },
                    { 94, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 6, null, true, true, "Outros itens para agro e indústria", 12, "outros-itens-para-agro-e-industria", null, null },
                    { 95, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 7, null, true, true, "Produção Rural", 12, "producao-rural", null, null },
                    { 96, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, null, true, true, "Vagas de emprego", 13, "vagas-de-emprego", null, null },
                    { 97, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, null, true, true, "Trailers e carrinhos comerciais", 14, "trailers-e-carrinhos-comerciais", null, null },
                    { 98, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, null, true, true, "Equipamentos Para Comércio", 14, "equipamentos-para-comercio", null, null },
                    { 99, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, null, true, true, "Gastronomia e Hotelaria", 14, "gastronomia-e-hotelaria", null, null },
                    { 100, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, null, true, true, "Equipamentos Médicos e Hospitalares", 14, "equipamentos-medicos-e-hospitalares", null, null },
                    { 101, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 5, null, true, true, "Uniformes de Trabalho e EPIs", 14, "uniformes-de-trabalho-e-epis", null, null },
                    { 102, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, null, true, true, "Câmeras e Filmadoras", 15, "cameras-e-filmadoras", null, null },
                    { 103, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, null, true, true, "Acessórios para Câmeras e Filmadoras", 15, "acessorios-para-cameras-e-filmadoras", null, null },
                    { 104, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, null, true, true, "Drones", 15, "drones", null, null },
                    { 105, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, null, true, true, "Consoles de Vídeo Game", 16, "consoles-de-video-game", null, null },
                    { 106, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, null, true, true, "Jogos de Vídeo Game", 16, "jogos-de-video-game", null, null },
                    { 107, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, null, true, true, "Peças e Acessórios de Vídeo Game", 16, "pecas-e-acessorios-de-video-game", null, null },
                    { 108, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, null, true, true, "TVs", 17, "tvs", null, null },
                    { 109, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, null, true, true, "Peças e Acessórios para TV", 17, "pecas-e-acessorios-para-tv", null, null },
                    { 110, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, null, true, true, "Projetores e Telas de Projeção", 17, "projetores-e-telas-de-projecao", null, null },
                    { 111, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, null, true, true, "DVD, Blu-Ray e Vídeo Cassete", 17, "dvd-blu-ray-e-video-cassete", null, null },
                    { 112, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 5, null, true, true, "Dispositivos de Streaming", 17, "dispositivos-de-streaming", null, null },
                    { 113, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, null, true, true, "Fones de Ouvido", 18, "fones-de-ouvido", null, null },
                    { 114, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, null, true, true, "Aparelhos de Som", 18, "aparelhos-de-som", null, null },
                    { 115, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, null, true, true, "Microfones e Gravadores", 18, "microfones-e-gravadores", null, null },
                    { 116, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, null, true, true, "Equipamentos e Acessórios de Som", 18, "equipamentos-e-acessorios-de-som", null, null },
                    { 117, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, null, true, true, "Computadores e Desktops", 19, "computadores-e-desktops", null, null },
                    { 118, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, null, true, true, "Notebooks", 19, "notebooks", null, null },
                    { 119, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, null, true, true, "Monitores", 19, "monitores", null, null },
                    { 120, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, null, true, true, "Periféricos e Acessórios de Computador", 19, "perifericos-e-acessorios-de-computador", null, null },
                    { 121, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 5, null, true, true, "Peças de Hardware", 19, "pecas-de-hardware", null, null },
                    { 122, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 6, null, true, true, "Armazenamento", 19, "armazenamento", null, null },
                    { 123, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 7, null, true, true, "Memória RAM", 19, "memoria-ram", null, null },
                    { 124, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 8, null, true, true, "Processadores", 19, "processadores", null, null },
                    { 125, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 9, null, true, true, "Placas de Vídeo", 19, "placas-de-video", null, null },
                    { 126, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 10, null, true, true, "Conectividade e Dispositivos de Rede", 19, "conectividade-e-dispositivos-de-rede", null, null },
                    { 127, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 11, null, true, true, "Tablets e E-Readers", 19, "tablets-e-e-readers", null, null },
                    { 128, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, null, true, true, "Ar-condicionados", 20, "ar-condicionados", null, null },
                    { 129, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, null, true, true, "Ventiladores e Climatizadores", 20, "ventiladores-e-climatizadores", null, null },
                    { 130, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, null, true, true, "Geladeiras e Freezers", 20, "geladeiras-e-freezers", null, null },
                    { 131, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, null, true, true, "Fogões e Fornos", 20, "fogoes-e-fornos", null, null },
                    { 132, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 5, null, true, true, "Máquinas de Lavar e Secadoras", 20, "maquinas-de-lavar-e-secadoras", null, null },
                    { 133, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 6, null, true, true, "Eletroportáteis Para Cozinha e Limpeza", 20, "eletroportateis-para-cozinha-e-limpeza", null, null },
                    { 134, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 7, null, true, true, "Eletroportáteis Para Cuidados Pessoais", 20, "eletroportateis-para-cuidados-pessoais", null, null },
                    { 135, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, null, true, true, "Camas e Colchões", 21, "camas-e-colchoes", null, null },
                    { 136, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, null, true, true, "Sofás e Poltronas", 21, "sofas-e-poltronas", null, null },
                    { 137, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, null, true, true, "Bancos e Cadeiras", 21, "bancos-e-cadeiras", null, null },
                    { 138, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, null, true, true, "Mesas", 21, "mesas", null, null },
                    { 139, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 5, null, true, true, "Escrivaninhas e Penteadeiras", 21, "escrivaninhas-e-penteadeiras", null, null },
                    { 140, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 6, null, true, true, "Racks e Painéis", 21, "racks-e-paineis", null, null },
                    { 141, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 7, null, true, true, "Armários e Guarda-Roupas", 21, "armarios-e-guarda-roupas", null, null },
                    { 142, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 8, null, true, true, "Móveis Para Organização", 21, "moveis-para-organizacao", null, null },
                    { 143, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, null, true, true, "Fundação e Estrutura", 22, "fundacao-e-estrutura", null, null },
                    { 144, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, null, true, true, "Alvenaria", 22, "alvenaria", null, null },
                    { 145, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, null, true, true, "Pisos e Revestimentos", 22, "pisos-e-revestimentos", null, null },
                    { 146, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, null, true, true, "Portas e Janelas", 22, "portas-e-janelas", null, null },
                    { 147, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 5, null, true, true, "Cubas e Pias", 22, "cubas-e-pias", null, null },
                    { 148, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 6, null, true, true, "Torneiras, Duchas e Vasos", 22, "torneiras-duchas-e-vasos", null, null },
                    { 149, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 7, null, true, true, "Instalações Elétricas e Hidráulicas", 22, "instalacoes-eletricas-e-hidraulicas", null, null },
                    { 150, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 8, null, true, true, "Ferramentas de Construção", 22, "ferramentas-de-construcao", null, null },
                    { 151, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 9, null, true, true, "Ferramentas de Pintura", 22, "ferramentas-de-pintura", null, null },
                    { 152, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, null, true, true, "Itens Para Escritório", 23, "itens-para-escritorio", null, null },
                    { 153, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, null, true, true, "Cadeiras de Escritório e Gamer", 23, "cadeiras-de-escritorio-e-gamer", null, null },
                    { 154, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, null, true, true, "Móveis de Escritório", 23, "moveis-de-escritorio", null, null },
                    { 155, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, null, true, true, "Papelaria", 23, "papelaria", null, null },
                    { 38, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, null, true, true, "Peças para carros, vans e utilitários", 3, "pecas-carros-vans-e-utilitarios", null, null },
                    { 39, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, null, true, true, "Peças para caminhões", 3, "pecas-caminhoes", null, null },
                    { 40, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, null, true, true, "Peças para motos", 3, "pecas-motos", null, null },
                    { 41, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 5, null, true, true, "Peças para barcos e aeronaves", 3, "pecas-barcos-e-aeronaves", null, null },
                    { 42, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, null, true, true, "Peças para ônibus", 3, "pecas-onibus", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentId_DisplayOrder",
                table: "Categories",
                columns: new[] { "ParentId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "UQ_Categories_Name_Root",
                table: "Categories",
                column: "Name",
                unique: true,
                filter: "[ParentId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_Categories_ParentId_Name",
                table: "Categories",
                columns: new[] { "ParentId", "Name" },
                unique: true,
                filter: "[ParentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
