using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DailyBudget.API.Controllers;

public record CriarDesejoDto(
    [Required(ErrorMessage = "O nome do item é obrigatório.")]
    [MaxLength(200)]
    string Item,
    [Range(0.00, double.MaxValue, ErrorMessage = "O valor estimado não pode ser negativo.")]
    decimal ValorEstimado,
    [Range(1, 3, ErrorMessage = "A prioridade deve ser entre 1 (Baixa) e 3 (Alta).")]
    int Prioridade
);

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DesejosController : ControllerBase
{
    private readonly string _connStr;
    public DesejosController(IConfiguration config)
        => _connStr = config.GetConnectionString("DailyBudget")!;

    private int ObterUserId()
        => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    // GET: api/desejos
    [HttpGet]
    public IActionResult ObterTodos()
    {
        int userId = ObterUserId();
        var lista = new List<object>();
        using var conexao = new SqlConnection(_connStr);
        conexao.Open();
        string sql = @"SELECT Id, Item, ValorEstimado, Prioridade, Adquirido 
                       FROM ListaDesejos 
                       WHERE IdUtilizador = @UserId 
                       ORDER BY Adquirido ASC, Prioridade DESC, Item ASC";
        using var cmd = new SqlCommand(sql, conexao);
        cmd.Parameters.AddWithValue("@UserId", userId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(new
            {
                Id = reader.GetInt32(0),
                Item = reader.GetString(1),
                ValorEstimado = reader.GetDecimal(2),
                Prioridade = reader.GetInt32(3),
                Adquirido = reader.GetBoolean(4)
            });
        }
        return Ok(lista);
    }

    // POST: api/desejos
    [HttpPost]
    public IActionResult Adicionar([FromBody] CriarDesejoDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        int userId = ObterUserId();
        using var conexao = new SqlConnection(_connStr);
        conexao.Open();
        string sql = @"INSERT INTO ListaDesejos (IdUtilizador, Item, ValorEstimado, Prioridade, Adquirido) 
                       OUTPUT INSERTED.Id 
                       VALUES (@UserId, @Item, @Val, @Prio, 0)";
        using var cmd = new SqlCommand(sql, conexao);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@Item", dto.Item);
        cmd.Parameters.AddWithValue("@Val", dto.ValorEstimado);
        cmd.Parameters.AddWithValue("@Prio", dto.Prioridade);
        int novoId = (int)cmd.ExecuteScalar();
        return Created("", new { Id = novoId, Mensagem = "Item adicionado à lista de desejos." });
    }

    // PUT: api/desejos/adquirido/{id}
    [HttpPut("adquirido/{id}")]
    public IActionResult MarcarAdquirido(int id)
    {
        int userId = ObterUserId();
        using var conexao = new SqlConnection(_connStr);
        conexao.Open();
        using var cmd = new SqlCommand(
            "UPDATE ListaDesejos SET Adquirido = 1 WHERE Id = @Id AND IdUtilizador = @UserId", conexao);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@UserId", userId);
        return cmd.ExecuteNonQuery() > 0 ? Ok(new { Mensagem = "Marcado como adquirido." }) : NotFound();
    }

    // DELETE: api/desejos/{id}
    [HttpDelete("{id}")]
    public IActionResult Remover(int id)
    {
        int userId = ObterUserId();
        using var conexao = new SqlConnection(_connStr);
        conexao.Open();
        using var cmd = new SqlCommand(
            "DELETE FROM ListaDesejos WHERE Id = @Id AND IdUtilizador = @UserId", conexao);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@UserId", userId);
        return cmd.ExecuteNonQuery() > 0 ? Ok(new { Mensagem = "Item removido." }) : NotFound();
    }
}
