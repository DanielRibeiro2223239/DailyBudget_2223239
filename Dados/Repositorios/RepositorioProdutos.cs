using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace DailyBudgetWPF.Dados.Repositorios
{
    /// <summary>
    /// Repositório para gestão de produtos.
    /// </summary>
    public static class RepositorioProdutos
    {
        public static List<SugestaoProduto> ObterSugestoes(string query)
        {
            var lista = new List<SugestaoProduto>();
            if (string.IsNullOrEmpty(query) || query.Length < 2) return lista;

            try
            {
                using var conexao = ConexaoBD.ObterConexao();
                conexao.Open();
                string sql = @"SELECT TOP 5 p.Nome, p.UltimoValor, c.Nome AS Categoria
                                FROM Produtos p 
                                LEFT JOIN Categorias c ON p.IdCategoria = c.Id
                                WHERE p.Nome LIKE @Query + '%'
                                ORDER BY p.VezesUsado DESC";

                using var cmd = new SqlCommand(sql, conexao);
                cmd.Parameters.AddWithValue("@Query", query);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    lista.Add(new SugestaoProduto
                    {
                        Nome = reader.GetString(0),
                        UltimoValor = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1),
                        Categoria = reader.IsDBNull(2) ? "" : reader.GetString(2)
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao obter sugestões de produtos: {ex.Message}");
            }
            return lista;
        }

        public static int? RegistarOuAtualizar(string nome, decimal valor, int? categoriaId)
        {
            if (string.IsNullOrEmpty(nome)) return null;

            try
            {
                using var conexao = ConexaoBD.ObterConexao();
                conexao.Open();

                using var cmdCheck = new SqlCommand("SELECT Id FROM Produtos WHERE Nome = @Nome", conexao);
                cmdCheck.Parameters.AddWithValue("@Nome", nome);
                var resultado = cmdCheck.ExecuteScalar();

                if (resultado != null)
                {
                    int prodId = Convert.ToInt32(resultado);
                    using var cmdUpd = new SqlCommand(
                        "UPDATE Produtos SET VezesUsado = VezesUsado + 1, UltimoValor = @Val WHERE Id = @Id", conexao);
                    cmdUpd.Parameters.AddWithValue("@Val", valor);
                    cmdUpd.Parameters.AddWithValue("@Id", prodId);
                    cmdUpd.ExecuteNonQuery();
                    return prodId;
                }
                else
                {
                    using var cmdIns = new SqlCommand(
                        "INSERT INTO Produtos (Nome, IdCategoria, VezesUsado, UltimoValor) OUTPUT INSERTED.Id VALUES (@Nome, @Cat, 1, @Val)", conexao);
                    cmdIns.Parameters.AddWithValue("@Nome", nome);
                    cmdIns.Parameters.AddWithValue("@Cat", (object?)categoriaId ?? DBNull.Value);
                    cmdIns.Parameters.AddWithValue("@Val", valor);
                    return (int)cmdIns.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao registar/atualizar produto: {ex.Message}");
                return null;
            }
        }

        public static List<(string Nome, int VezesUsado, decimal TotalGasto)> ObterTopProdutos()
        {
            var lista = new List<(string, int, decimal)>();
            try
            {
                using var conexao = ConexaoBD.ObterConexao();
                conexao.Open();
                string sql = @"SELECT TOP 10 p.Nome, p.VezesUsado, 
                                ISNULL((SELECT SUM(d.Valor) FROM Despesas d WHERE d.IdProduto = p.Id), 0) AS TotalGasto
                                FROM Produtos p ORDER BY p.VezesUsado DESC";
                using var cmd = new SqlCommand(sql, conexao);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    lista.Add((reader.GetString(0), reader.GetInt32(1), reader.GetDecimal(2)));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao obter top produtos: {ex.Message}");
            }
            return lista;
        }
    }

    public class SugestaoProduto
    {
        public string Nome { get; set; } = "";
        public decimal UltimoValor { get; set; }
        public string Categoria { get; set; } = "";
        public string TextoExibicao => $"{Nome} — {UltimoValor:N2}€ ({Categoria})";
    }
}
