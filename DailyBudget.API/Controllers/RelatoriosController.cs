using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DailyBudget.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RelatoriosController : ControllerBase
{
    private readonly string _connStr;
    public RelatoriosController(IConfiguration config) { _connStr = config.GetConnectionString("DailyBudget")!; }
    private int ObterUserId() => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    // GET /api/relatorios/consolidado?periodo=Mensal
    [HttpGet("consolidado")]
    public IActionResult Consolidado([FromQuery] string periodo = "Mensal")
    {
        int userId = ObterUserId();
        string formatoSql = periodo switch { "Semanal" => "DATEPART(week, Data)", "Anual" => "YEAR(Data)", _ => "FORMAT(Data, 'MM/yyyy')" };
        var resultado = new List<object>();
        using var conexao = new SqlConnection(_connStr);
        conexao.Open();
        string sql = $@"SELECT {formatoSql} as Periodo, 
            SUM(CASE WHEN Tipo='Receita' THEN Valor ELSE 0 END) as TotalReceitas, 
            SUM(CASE WHEN Tipo='Despesa' THEN Valor ELSE 0 END) as TotalDespesas 
            FROM (SELECT Data, Valor, 'Receita' as Tipo FROM Receitas WHERE IdUtilizador=@Id 
            UNION ALL SELECT Data, Valor, 'Despesa' as Tipo FROM Despesas WHERE IdUtilizador=@Id) as T 
            GROUP BY {formatoSql} ORDER BY MIN(Data) ASC";
        using var cmd = new SqlCommand(sql, conexao);
        cmd.Parameters.AddWithValue("@Id", userId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string display = periodo switch { "Semanal" => "Semana " + reader.GetValue(0).ToString(), _ => reader.GetValue(0).ToString()! };
            resultado.Add(new { Periodo = display, Receitas = reader.GetDecimal(1), Despesas = reader.GetDecimal(2) });
        }
        return Ok(resultado);
    }

    // GET /api/relatorios/por-categoria
    [HttpGet("por-categoria")]
    public IActionResult PorCategoria()
    {
        int userId = ObterUserId();
        var resultado = new List<object>();
        using var conexao = new SqlConnection(_connStr);
        conexao.Open();
        string sql = @"SELECT ISNULL(c.Nome, 'Sem Categoria'), SUM(d.Valor), c.Emoji, c.Cor
                        FROM Despesas d LEFT JOIN Categorias c ON d.IdCategoria = c.Id
                        WHERE d.IdUtilizador = @Id GROUP BY c.Nome, c.Emoji, c.Cor ORDER BY SUM(d.Valor) DESC";
        using var cmd = new SqlCommand(sql, conexao);
        cmd.Parameters.AddWithValue("@Id", userId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            resultado.Add(new { Categoria = reader.GetString(0), Total = reader.GetDecimal(1),
                Emoji = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Cor = reader.IsDBNull(3) ? "#999" : reader.GetString(3) });
        }
        return Ok(resultado);
    }

    // GET /api/relatorios/por-produto — Gastos agrupados por produto
    [HttpGet("por-produto")]
    public IActionResult PorProduto()
    {
        int userId = ObterUserId();
        var resultado = new List<object>();
        using var conexao = new SqlConnection(_connStr);
        conexao.Open();
        string sql = @"SELECT ISNULL(p.Nome, d.Descricao) AS Produto, SUM(d.Valor) AS Total, COUNT(*) AS Vezes
                        FROM Despesas d LEFT JOIN Produtos p ON d.IdProduto = p.Id
                        WHERE d.IdUtilizador = @Id GROUP BY ISNULL(p.Nome, d.Descricao)
                        ORDER BY SUM(d.Valor) DESC";
        using var cmd = new SqlCommand(sql, conexao);
        cmd.Parameters.AddWithValue("@Id", userId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            resultado.Add(new { Produto = reader.GetString(0), Total = reader.GetDecimal(1), Vezes = reader.GetInt32(2) });
        }
        return Ok(resultado);
    }

    // GET /api/relatorios/resumo — Resumo geral (saldo, totais, estado)
    [HttpGet("resumo")]
    public IActionResult Resumo()
    {
        int userId = ObterUserId();
        using var conexao = new SqlConnection(_connStr);
        conexao.Open();
        decimal totalReceitas = 0, totalDespesas = 0;
        using (var cmd = new SqlCommand("SELECT ISNULL(SUM(Valor),0) FROM Receitas WHERE IdUtilizador=@Id", conexao))
        { cmd.Parameters.AddWithValue("@Id", userId); totalReceitas = (decimal)cmd.ExecuteScalar(); }
        using (var cmd = new SqlCommand("SELECT ISNULL(SUM(Valor),0) FROM Despesas WHERE IdUtilizador=@Id", conexao))
        { cmd.Parameters.AddWithValue("@Id", userId); totalDespesas = (decimal)cmd.ExecuteScalar(); }
        decimal saldo = totalReceitas - totalDespesas;
        string estado = totalDespesas > totalReceitas ? "Em Risco" : totalDespesas > totalReceitas * 0.8m ? "Cuidado" : "Saudável";
        return Ok(new { TotalReceitas = totalReceitas, TotalDespesas = totalDespesas, Saldo = saldo, Estado = estado });
    }
}
