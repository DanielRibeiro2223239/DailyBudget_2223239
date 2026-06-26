using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DailyBudget.API.Controllers;

// ── DTOs com DataAnnotations ──────────────────────────────────────────────
public record CriarReceitaDto(
    [Required(ErrorMessage = "A descrição é obrigatória.")]
    [MaxLength(200)]
    string Descricao,
    [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser positivo.")]
    decimal Valor,
    DateTime? Data
);

public record CriarDespesaDto(
    [Required(ErrorMessage = "A descrição é obrigatória.")]
    [MaxLength(200)]
    string Descricao,
    [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser positivo.")]
    decimal Valor,
    string? Produto,
    string? Categoria,
    string? Estabelecimento,
    DateTime? Data
);

public record EditarTransacaoDto(
    [Required] string Descricao,
    [Range(0.01, double.MaxValue)] decimal Valor,
    string? Categoria,
    DateTime? Data
);

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransacoesController : ControllerBase
{
    private readonly string _connStr;
    public TransacoesController(IConfiguration config)
        => _connStr = config.GetConnectionString("DailyBudget")!;

    private int ObterUserId()
        => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    // ── GET /api/transacoes ────────────────────────────────────────────────
    [HttpGet]
    public IActionResult ObterTodas()
    {
        int userId = ObterUserId();
        var lista = new List<object>();
        using var conexao = new SqlConnection(_connStr);
        conexao.Open();
        string sql = @"
            SELECT TOP 50 * FROM (
                SELECT Id, Descricao, Valor, Data, 'Receita' AS Tipo, NULL AS Categoria, NULL AS Produto
                FROM Receitas WHERE IdUtilizador = @Id
                UNION ALL
                SELECT d.Id, d.Descricao, d.Valor, d.Data, 'Despesa' AS Tipo,
                       ISNULL(c.Nome, 'Sem Categoria') AS Categoria,
                       p.Nome AS Produto
                FROM Despesas d
                LEFT JOIN Categorias c ON d.IdCategoria = c.Id
                LEFT JOIN Produtos p ON d.IdProduto = p.Id
                WHERE d.IdUtilizador = @Id
            ) AS T ORDER BY Data DESC";
        using var cmd = new SqlCommand(sql, conexao);
        cmd.Parameters.AddWithValue("@Id", userId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(new
            {
                Id = reader.GetInt32(0),
                Descricao = reader.GetString(1),
                Valor = reader.GetDecimal(2),
                Data = reader.GetDateTime(3),
                Tipo = reader.GetString(4),
                Categoria = reader.IsDBNull(5) ? null : reader.GetString(5),
                Produto = reader.IsDBNull(6) ? null : reader.GetString(6)
            });
        }
        return Ok(lista);
    }

    // ── POST /api/transacoes/receita ───────────────────────────────────────
    [HttpPost("receita")]
    public IActionResult AdicionarReceita([FromBody] CriarReceitaDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        int userId = ObterUserId();
        using var conexao = new SqlConnection(_connStr);
        conexao.Open();
        string sql = "INSERT INTO Receitas (IdUtilizador, Descricao, Valor, Data) OUTPUT INSERTED.Id VALUES (@Id, @Desc, @Val, @Data)";
        using var cmd = new SqlCommand(sql, conexao);
        cmd.Parameters.AddWithValue("@Id", userId);
        cmd.Parameters.AddWithValue("@Desc", dto.Descricao);
        cmd.Parameters.AddWithValue("@Val", dto.Valor);
        cmd.Parameters.AddWithValue("@Data", dto.Data ?? DateTime.Now);
        int novoId = (int)cmd.ExecuteScalar();
        return Created("", new { Id = novoId, Mensagem = "Receita adicionada." });
    }

    // ── POST /api/transacoes/despesa ───────────────────────────────────────
    [HttpPost("despesa")]
    public IActionResult AdicionarDespesa([FromBody] CriarDespesaDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        int userId = ObterUserId();
        using var conexao = new SqlConnection(_connStr);
        conexao.Open();

        int? catId = null;
        if (!string.IsNullOrEmpty(dto.Categoria))
        {
            using var cmdCat = new SqlCommand("SELECT Id FROM Categorias WHERE Nome = @Nome", conexao);
            cmdCat.Parameters.AddWithValue("@Nome", dto.Categoria);
            var resCat = cmdCat.ExecuteScalar();
            if (resCat != null) catId = Convert.ToInt32(resCat);
        }

        int? prodId = null;
        if (!string.IsNullOrEmpty(dto.Produto))
        {
            using var cmdProd = new SqlCommand("SELECT Id FROM Produtos WHERE Nome = @Nome", conexao);
            cmdProd.Parameters.AddWithValue("@Nome", dto.Produto);
            var resProd = cmdProd.ExecuteScalar();
            if (resProd != null)
            {
                prodId = Convert.ToInt32(resProd);
                using var cmdUpd = new SqlCommand(
                    "UPDATE Produtos SET VezesUsado = VezesUsado + 1, UltimoValor = @Val WHERE Id = @Id", conexao);
                cmdUpd.Parameters.AddWithValue("@Val", dto.Valor);
                cmdUpd.Parameters.AddWithValue("@Id", prodId);
                cmdUpd.ExecuteNonQuery();
            }
            else
            {
                using var cmdIns = new SqlCommand(
                    "INSERT INTO Produtos (Nome, IdCategoria, VezesUsado, UltimoValor) OUTPUT INSERTED.Id VALUES (@Nome, @Cat, 1, @Val)", conexao);
                cmdIns.Parameters.AddWithValue("@Nome", dto.Produto);
                cmdIns.Parameters.AddWithValue("@Cat", (object?)catId ?? DBNull.Value);
                cmdIns.Parameters.AddWithValue("@Val", dto.Valor);
                prodId = (int)cmdIns.ExecuteScalar();
            }
        }

        int? estabId = null;
        if (!string.IsNullOrEmpty(dto.Estabelecimento))
        {
            using var cmdEst = new SqlCommand("SELECT Id FROM Estabelecimentos WHERE Nome = @Nome", conexao);
            cmdEst.Parameters.AddWithValue("@Nome", dto.Estabelecimento);
            var resEst = cmdEst.ExecuteScalar();
            if (resEst != null)
            {
                estabId = Convert.ToInt32(resEst);
            }
            else
            {
                using var cmdIns = new SqlCommand(
                    "INSERT INTO Estabelecimentos (Nome) OUTPUT INSERTED.Id VALUES (@Nome)", conexao);
                cmdIns.Parameters.AddWithValue("@Nome", dto.Estabelecimento);
                estabId = (int)cmdIns.ExecuteScalar();
            }
        }

        string sql = @"INSERT INTO Despesas (IdUtilizador, IdCategoria, IdProduto, IdEstabelecimento, Descricao, Valor, Data)
                       OUTPUT INSERTED.Id VALUES (@UserId, @Cat, @Prod, @Estab, @Desc, @Val, @Data)";
        using var cmd = new SqlCommand(sql, conexao);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@Cat", (object?)catId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Prod", (object?)prodId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Estab", (object?)estabId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Desc", dto.Descricao);
        cmd.Parameters.AddWithValue("@Val", dto.Valor);
        cmd.Parameters.AddWithValue("@Data", dto.Data ?? DateTime.Now);
        int novoId = (int)cmd.ExecuteScalar();
        return Created("", new { Id = novoId, Mensagem = "Despesa adicionada." });
    }

    // ── PUT /api/transacoes/{tipo}/{id} ────────────────────────────────────
    [HttpPut("{tipo}/{id}")]
    public IActionResult Editar(string tipo, int id, [FromBody] EditarTransacaoDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        int userId = ObterUserId();

        // Validar tipo para evitar SQL injection
        string tipoNorm = tipo.ToLower();
        if (tipoNorm != "receita" && tipoNorm != "despesa")
            return BadRequest(new { Mensagem = "Tipo inválido. Use 'receita' ou 'despesa'." });

        using var conexao = new SqlConnection(_connStr);
        conexao.Open();

        if (tipoNorm == "receita")
        {
            using var cmd = new SqlCommand(
                "UPDATE Receitas SET Descricao=@Desc, Valor=@Val, Data=@Data WHERE Id=@Id AND IdUtilizador=@UserId", conexao);
            cmd.Parameters.AddWithValue("@Desc", dto.Descricao);
            cmd.Parameters.AddWithValue("@Val", dto.Valor);
            cmd.Parameters.AddWithValue("@Data", dto.Data ?? DateTime.Now);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@UserId", userId);
            return cmd.ExecuteNonQuery() > 0 ? Ok(new { Mensagem = "Atualizado." }) : NotFound();
        }
        else
        {
            int? catId = null;
            if (!string.IsNullOrEmpty(dto.Categoria))
            {
                using var cmdCat = new SqlCommand("SELECT Id FROM Categorias WHERE Nome = @Nome", conexao);
                cmdCat.Parameters.AddWithValue("@Nome", dto.Categoria);
                var r = cmdCat.ExecuteScalar();
                if (r != null) catId = Convert.ToInt32(r);
            }
            using var cmd = new SqlCommand(
                "UPDATE Despesas SET Descricao=@Desc, Valor=@Val, IdCategoria=@Cat, Data=@Data WHERE Id=@Id AND IdUtilizador=@UserId", conexao);
            cmd.Parameters.AddWithValue("@Desc", dto.Descricao);
            cmd.Parameters.AddWithValue("@Val", dto.Valor);
            cmd.Parameters.AddWithValue("@Cat", (object?)catId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Data", dto.Data ?? DateTime.Now);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@UserId", userId);
            return cmd.ExecuteNonQuery() > 0 ? Ok(new { Mensagem = "Atualizado." }) : NotFound();
        }
    }

    // ── DELETE /api/transacoes/{tipo}/{id} ─────────────────────────────────
    [HttpDelete("{tipo}/{id}")]
    public IActionResult Remover(string tipo, int id)
    {
        // Validar tipo explicitamente para evitar SQL injection
        string tipoNorm = tipo.ToLower();
        if (tipoNorm != "receita" && tipoNorm != "despesa")
            return BadRequest(new { Mensagem = "Tipo inválido. Use 'receita' ou 'despesa'." });

        int userId = ObterUserId();
        string tabela = tipoNorm == "receita" ? "Receitas" : "Despesas";

        using var conexao = new SqlConnection(_connStr);
        conexao.Open();
        using var cmd = new SqlCommand($"DELETE FROM {tabela} WHERE Id = @Id AND IdUtilizador = @UserId", conexao);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@UserId", userId);
        return cmd.ExecuteNonQuery() > 0 ? Ok(new { Mensagem = "Removido." }) : NotFound();
    }
}
