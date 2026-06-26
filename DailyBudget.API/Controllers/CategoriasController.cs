using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DailyBudget.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriasController : ControllerBase
{
    private readonly string _connStr;
    public CategoriasController(IConfiguration config) { _connStr = config.GetConnectionString("DailyBudget")!; }

    // GET /api/categorias — Lista todas as categorias
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ObterTodas()
    {
        var lista = new List<object>();
        using var conexao = new SqlConnection(_connStr);
        conexao.Open();
        using var cmd = new SqlCommand("SELECT Id, Nome, Emoji, Cor FROM Categorias ORDER BY Nome", conexao);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(new {
                Id = reader.GetInt32(0),
                Nome = reader.GetString(1),
                Emoji = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Cor = reader.GetString(3)
            });
        }
        return Ok(lista);
    }

    // POST /api/categorias — Adiciona uma nova categoria
    [HttpPost]
    public IActionResult Adicionar([FromBody] CriarCategoriaDto dto)
    {
        if (string.IsNullOrEmpty(dto.Nome)) return BadRequest(new { Mensagem = "Nome é obrigatório." });
        using var conexao = new SqlConnection(_connStr);
        conexao.Open();
        using var cmd = new SqlCommand("INSERT INTO Categorias (Nome, Emoji, Cor) OUTPUT INSERTED.Id VALUES (@Nome, @Emoji, @Cor)", conexao);
        cmd.Parameters.AddWithValue("@Nome", dto.Nome.Trim());
        cmd.Parameters.AddWithValue("@Emoji", dto.Emoji ?? "");
        cmd.Parameters.AddWithValue("@Cor", string.IsNullOrWhiteSpace(dto.Cor) ? "#4CAF50" : dto.Cor);
        int id = (int)cmd.ExecuteScalar();
        return Created("", new { Id = id, Mensagem = "Categoria criada." });
    }

    // DELETE /api/categorias/{id} — Remove uma categoria
    [HttpDelete("{id}")]
    public IActionResult Remover(int id)
    {
        using var conexao = new SqlConnection(_connStr);
        conexao.Open();
        using var cmd = new SqlCommand("DELETE FROM Categorias WHERE Id = @Id", conexao);
        cmd.Parameters.AddWithValue("@Id", id);
        return cmd.ExecuteNonQuery() > 0 ? Ok(new { Mensagem = "Removida." }) : NotFound();
    }
}

public record CriarCategoriaDto(string Nome, string? Emoji, string? Cor);
