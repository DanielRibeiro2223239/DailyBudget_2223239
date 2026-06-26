using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DailyBudget.API.Controllers;

public record CriarOrcamentoDto(int IdCategoria, decimal LimiteMensal, int Mes, int Ano);

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrcamentosController : ControllerBase
{
    private readonly string _connStr;
    public OrcamentosController(IConfiguration config) { _connStr = config.GetConnectionString("DailyBudget")!; }
    private int ObterUserId() => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    // GET /api/orcamentos — Lista orçamentos do utilizador com progresso de gasto
    [HttpGet]
    public IActionResult ObterTodos()
    {
        int userId = ObterUserId();
        var lista = new List<object>();
        using var conexao = new SqlConnection(_connStr);
        conexao.Open();

        // Obtém orçamentos com o total já gasto na categoria nesse mês
        string sql = @"SELECT o.Id, o.IdCategoria, c.Nome, c.Emoji, o.LimiteMensal, o.Mes, o.Ano,
                        ISNULL((SELECT SUM(d.Valor) FROM Despesas d 
                                WHERE d.IdUtilizador = @Id AND d.IdCategoria = o.IdCategoria 
                                AND MONTH(d.Data) = o.Mes AND YEAR(d.Data) = o.Ano), 0) AS TotalGasto
                        FROM Orcamentos o
                        INNER JOIN Categorias c ON o.IdCategoria = c.Id
                        WHERE o.IdUtilizador = @Id ORDER BY o.Ano DESC, o.Mes DESC";
        using var cmd = new SqlCommand(sql, conexao);
        cmd.Parameters.AddWithValue("@Id", userId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            decimal limite = reader.GetDecimal(4);
            decimal gasto = reader.GetDecimal(7);
            lista.Add(new {
                Id = reader.GetInt32(0), IdCategoria = reader.GetInt32(1),
                Categoria = reader.GetString(2), Emoji = reader.IsDBNull(3) ? "" : reader.GetString(3),
                LimiteMensal = limite, Mes = reader.GetInt32(5), Ano = reader.GetInt32(6),
                TotalGasto = gasto, Percentagem = limite > 0 ? Math.Round(gasto / limite * 100, 1) : 0
            });
        }
        return Ok(lista);
    }

    // POST /api/orcamentos — Cria ou atualiza um orçamento
    [HttpPost]
    public IActionResult Criar([FromBody] CriarOrcamentoDto dto)
    {
        if (dto.LimiteMensal <= 0) return BadRequest(new { Mensagem = "Limite deve ser positivo." });
        int userId = ObterUserId();
        using var conexao = new SqlConnection(_connStr);
        conexao.Open();

        // Verifica se já existe orçamento para esta categoria/mês/ano
        using (var cmdCheck = new SqlCommand(
            "SELECT Id FROM Orcamentos WHERE IdUtilizador=@Id AND IdCategoria=@Cat AND Mes=@Mes AND Ano=@Ano", conexao))
        {
            cmdCheck.Parameters.AddWithValue("@Id", userId);
            cmdCheck.Parameters.AddWithValue("@Cat", dto.IdCategoria);
            cmdCheck.Parameters.AddWithValue("@Mes", dto.Mes);
            cmdCheck.Parameters.AddWithValue("@Ano", dto.Ano);
            var existente = cmdCheck.ExecuteScalar();
            if (existente != null)
            {
                // Atualiza o orçamento existente
                using var cmdUpd = new SqlCommand("UPDATE Orcamentos SET LimiteMensal=@Limite WHERE Id=@Id", conexao);
                cmdUpd.Parameters.AddWithValue("@Limite", dto.LimiteMensal);
                cmdUpd.Parameters.AddWithValue("@Id", (int)existente);
                cmdUpd.ExecuteNonQuery();
                return Ok(new { Mensagem = "Orçamento atualizado." });
            }
        }

        string sql = "INSERT INTO Orcamentos (IdUtilizador, IdCategoria, LimiteMensal, Mes, Ano) OUTPUT INSERTED.Id VALUES (@Id, @Cat, @Limite, @Mes, @Ano)";
        using var cmd = new SqlCommand(sql, conexao);
        cmd.Parameters.AddWithValue("@Id", userId);
        cmd.Parameters.AddWithValue("@Cat", dto.IdCategoria);
        cmd.Parameters.AddWithValue("@Limite", dto.LimiteMensal);
        cmd.Parameters.AddWithValue("@Mes", dto.Mes);
        cmd.Parameters.AddWithValue("@Ano", dto.Ano);
        int novoId = (int)cmd.ExecuteScalar();
        return Created("", new { Id = novoId, Mensagem = "Orçamento criado." });
    }

    // DELETE /api/orcamentos/{id}
    [HttpDelete("{id}")]
    public IActionResult Remover(int id)
    {
        int userId = ObterUserId();
        using var conexao = new SqlConnection(_connStr);
        conexao.Open();
        using var cmd = new SqlCommand("DELETE FROM Orcamentos WHERE Id=@Id AND IdUtilizador=@UserId", conexao);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@UserId", userId);
        return cmd.ExecuteNonQuery() > 0 ? Ok(new { Mensagem = "Removido." }) : NotFound();
    }
}
