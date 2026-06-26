using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DailyBudget.API.Controllers;

/// <summary>
/// Controller para gestão de produtos com auto-completar inteligente.
/// O sistema aprende com o histórico do utilizador para sugerir produtos.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProdutosController : ControllerBase
{
    private readonly string _connStr;
    public ProdutosController(IConfiguration config) { _connStr = config.GetConnectionString("DailyBudget")!; }

    // GET /api/produtos — Lista todos os produtos ordenados por frequência de uso
    [HttpGet]
    public IActionResult ObterTodos()
    {
        var lista = new List<object>();
        using var conexao = new SqlConnection(_connStr);
        conexao.Open();
        string sql = @"SELECT p.Id, p.Nome, p.UltimoValor, p.VezesUsado, c.Nome AS Categoria, c.Emoji
                        FROM Produtos p LEFT JOIN Categorias c ON p.IdCategoria = c.Id
                        ORDER BY p.VezesUsado DESC";
        using var cmd = new SqlCommand(sql, conexao);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(new {
                Id = reader.GetInt32(0), Nome = reader.GetString(1),
                UltimoValor = reader.IsDBNull(2) ? (decimal?)null : reader.GetDecimal(2),
                VezesUsado = reader.GetInt32(3),
                Categoria = reader.IsDBNull(4) ? null : reader.GetString(4),
                Emoji = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }
        return Ok(lista);
    }

    /// <summary>
    /// Auto-completar de produtos — procura produtos que começam com o texto digitado.
    /// Os resultados são ordenados por frequência de uso (mais usados primeiro).
    /// GET /api/produtos/sugestoes?q=caf
    /// </summary>
    [HttpGet("sugestoes")]
    public IActionResult Sugestoes([FromQuery] string q)
    {
        if (string.IsNullOrEmpty(q) || q.Length < 2)
            return Ok(Array.Empty<object>());

        var sugestoes = new List<object>();
        using var conexao = new SqlConnection(_connStr);
        conexao.Open();

        // Procura produtos cujo nome começa com o texto digitado
        // Ordenados por VezesUsado DESC para que os mais frequentes apareçam primeiro
        string sql = @"SELECT TOP 5 p.Nome, p.UltimoValor, c.Nome AS Categoria, c.Emoji
                        FROM Produtos p LEFT JOIN Categorias c ON p.IdCategoria = c.Id
                        WHERE p.Nome LIKE @Query + '%'
                        ORDER BY p.VezesUsado DESC";
        using var cmd = new SqlCommand(sql, conexao);
        cmd.Parameters.AddWithValue("@Query", q);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            sugestoes.Add(new {
                Produto = reader.GetString(0),
                UltimoValor = reader.IsDBNull(1) ? (decimal?)null : reader.GetDecimal(1),
                Categoria = reader.IsDBNull(2) ? null : reader.GetString(2),
                Emoji = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
        }
        return Ok(sugestoes);
    }

    // GET /api/produtos/top — Top 10 produtos mais usados (para gráficos)
    [HttpGet("top")]
    public IActionResult Top10()
    {
        var lista = new List<object>();
        using var conexao = new SqlConnection(_connStr);
        conexao.Open();
        string sql = @"SELECT TOP 10 p.Nome, p.VezesUsado, p.UltimoValor, c.Nome AS Categoria
                        FROM Produtos p LEFT JOIN Categorias c ON p.IdCategoria = c.Id
                        ORDER BY p.VezesUsado DESC";
        using var cmd = new SqlCommand(sql, conexao);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(new {
                Nome = reader.GetString(0), VezesUsado = reader.GetInt32(1),
                UltimoValor = reader.IsDBNull(2) ? 0m : reader.GetDecimal(2),
                Categoria = reader.IsDBNull(3) ? "Sem Categoria" : reader.GetString(3)
            });
        }
        return Ok(lista);
    }
}
