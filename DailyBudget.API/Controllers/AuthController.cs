using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DailyBudget.API.Controllers;

// Modelos de dados para os pedidos de autenticação (DTOs = Data Transfer Objects)
public record LoginDto(string Email, string Senha);
public record RegisterDto(string Nome, string Email, string Senha);

/// <summary>
/// Controller responsável pela autenticação e registo de utilizadores.
/// Endpoints: POST /api/auth/login e POST /api/auth/register
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly string _connStr;
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config)
    {
        _connStr = config.GetConnectionString("DailyBudget")!;
        _config = config;
    }

    /// <summary>
    /// Autentica um utilizador e devolve um token JWT.
    /// </summary>
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto dto)
    {
        // Validação dos campos obrigatórios
        if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Senha))
            return BadRequest(new { Mensagem = "Email e senha são obrigatórios." });

        // Gera o hash SHA-256 da senha para comparar com o que está guardado na BD
        string senhaHash = GerarHash(dto.Senha);

        using var conexao = new SqlConnection(_connStr);
        conexao.Open();

        // Consulta a BD para verificar se as credenciais correspondem a um utilizador
        string sql = "SELECT Id, Nome, Email FROM Utilizadores WHERE Email = @Email AND Senha = @Senha";
        using var cmd = new SqlCommand(sql, conexao);
        cmd.Parameters.AddWithValue("@Email", dto.Email);
        cmd.Parameters.AddWithValue("@Senha", senhaHash);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            int id = reader.GetInt32(0);
            string nome = reader.GetString(1);
            string email = reader.GetString(2);

            // Login bem-sucedido — gera um token JWT válido por 24 horas
            string token = GerarToken(id, email, nome);

            return Ok(new { Id = id, Nome = nome, Email = email, Token = token });
        }

        // Credenciais incorretas
        return Unauthorized(new { Mensagem = "Email ou palavra-passe incorretos." });
    }

    /// <summary>
    /// Regista um novo utilizador e devolve um token JWT.
    /// </summary>
    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterDto dto)
    {
        // Validação dos campos obrigatórios
        if (string.IsNullOrEmpty(dto.Nome) || string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Senha))
            return BadRequest(new { Mensagem = "Todos os campos são obrigatórios." });

        if (dto.Senha.Length < 6)
            return BadRequest(new { Mensagem = "A palavra-passe deve ter pelo menos 6 caracteres." });

        string senhaHash = GerarHash(dto.Senha);

        using var conexao = new SqlConnection(_connStr);
        conexao.Open();

        // Verifica se o email já está registado na base de dados
        using (var cmdCheck = new SqlCommand("SELECT COUNT(*) FROM Utilizadores WHERE Email = @Email", conexao))
        {
            cmdCheck.Parameters.AddWithValue("@Email", dto.Email);
            if ((int)cmdCheck.ExecuteScalar() > 0)
                return Conflict(new { Mensagem = "Este email já está registado." });
        }

        // Verifica se o nome de utilizador já existe
        using (var cmdNome = new SqlCommand("SELECT COUNT(*) FROM Utilizadores WHERE Nome = @Nome", conexao))
        {
            cmdNome.Parameters.AddWithValue("@Nome", dto.Nome);
            if ((int)cmdNome.ExecuteScalar() > 0)
                return Conflict(new { Mensagem = "Este nome de utilizador já está a ser usado." });
        }

        // Insere o novo utilizador e obtém o ID gerado automaticamente
        string sqlInsert = "INSERT INTO Utilizadores (Nome, Email, Senha) OUTPUT INSERTED.Id VALUES (@Nome, @Email, @Senha)";
        using var cmdInsert = new SqlCommand(sqlInsert, conexao);
        cmdInsert.Parameters.AddWithValue("@Nome", dto.Nome);
        cmdInsert.Parameters.AddWithValue("@Email", dto.Email);
        cmdInsert.Parameters.AddWithValue("@Senha", senhaHash);
        int id = (int)cmdInsert.ExecuteScalar();

        // Gera o token JWT para o novo utilizador
        string token = GerarToken(id, dto.Email, dto.Nome);

        return Created("", new { Id = id, dto.Nome, dto.Email, Token = token });
    }

    /// <summary>
    /// Faz login ou registo automático para utilizadores que entram pelo Google.
    /// </summary>
    [HttpPost("google-login")]
    public IActionResult GoogleLogin([FromBody] GoogleLoginDto dto)
    {
        if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Nome))
            return BadRequest(new { Mensagem = "Dados do Google incompletos." });

        using var conexao = new SqlConnection(_connStr);
        conexao.Open();

        // 1. Tenta encontrar o utilizador pelo email
        string sqlSelect = "SELECT Id, Nome, Email FROM Utilizadores WHERE Email = @Email";
        using (var cmdSelect = new SqlCommand(sqlSelect, conexao))
        {
            cmdSelect.Parameters.AddWithValue("@Email", dto.Email);
            using var reader = cmdSelect.ExecuteReader();
            if (reader.Read())
            {
                int id = reader.GetInt32(0);
                string nome = reader.GetString(1);
                string email = reader.GetString(2);
                string token = GerarToken(id, email, nome);
                return Ok(new { Id = id, Nome = nome, Email = email, Token = token });
            }
        }

        // 2. Se não existir, cria um novo utilizador (registo automático)
        // Usamos uma senha aleatória para utilizadores Google
        string senhaAleatoria = Guid.NewGuid().ToString("N");
        string sqlInsert = "INSERT INTO Utilizadores (Nome, Email, Senha) OUTPUT INSERTED.Id VALUES (@Nome, @Email, @Senha)";
        using (var cmdInsert = new SqlCommand(sqlInsert, conexao))
        {
            cmdInsert.Parameters.AddWithValue("@Nome", dto.Nome);
            cmdInsert.Parameters.AddWithValue("@Email", dto.Email);
            cmdInsert.Parameters.AddWithValue("@Senha", GerarHash(senhaAleatoria));
            int id = (int)cmdInsert.ExecuteScalar();
            string token = GerarToken(id, dto.Email, dto.Nome);
            return Ok(new { Id = id, Nome = dto.Nome, Email = dto.Email, Token = token });
        }
    }

    public record GoogleLoginDto(string Email, string Nome);

    /// <summary>
    /// Gera um token JWT com as informações do utilizador.
    /// O token é assinado com uma chave secreta e expira em 24 horas.
    /// </summary>
    private string GerarToken(int userId, string email, string nome)
    {
        // A chave simétrica é usada para assinar o token (garante que não foi alterado)
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Claims são informações guardadas dentro do token (visíveis mas protegidas)
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, nome)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(24),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Gera o hash SHA-256 de uma string (mesmo algoritmo usado no WPF).
    /// </summary>
    private static string GerarHash(string senha)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(senha));
        return Convert.ToHexString(bytes).ToLower();
    }
}
