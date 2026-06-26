using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using DailyBudgetWPF.Modelos;
using DailyBudgetWPF.Dados;

namespace DailyBudgetWPF.Dados.Repositorios
{
    public static class RepositorioCategorias
    {
        /// <summary>Obtém as categorias do utilizador atual (com fallback para categorias globais legacy).</summary>
        public static List<Categoria> ObterTodas()
        {
            var lista = new List<Categoria>();
            int userId = Sessao.UtilizadorAtual?.Id ?? 0;
            try
            {
                using var conn = ConexaoBD.ObterConexao();
                conn.Open();
                
                void CarregarCategorias()
                {
                    lista.Clear();
                    using var cmd = new SqlCommand(
                        "SELECT Id, Nome, Emoji, Cor FROM Categorias WHERE IdUtilizador = @UserId ORDER BY Nome", conn);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        lista.Add(new Categoria
                        {
                            Id = r.GetInt32(0),
                            Nome = r.GetString(1),
                            Emoji = r.IsDBNull(2) ? "📁" : r.GetString(2),
                            CorHex = r.IsDBNull(3) ? "#27AE60" : r.GetString(3)
                        });
                    }
                }

                CarregarCategorias();

                // Auto-create default categories if the user has none
                if (lista.Count == 0 && userId > 0)
                {
                    CriarCategoriasPredefinidas(userId);
                    CarregarCategorias(); // Reload after creation
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao obter categorias: {ex.Message}");
            }
            return lista;
        }

        public static void Adicionar(string nome, string emoji, string cor)
        {
            int userId = Sessao.UtilizadorAtual?.Id ?? 0;
            if (userId == 0) return;
            try
            {
                using var conn = ConexaoBD.ObterConexao();
                conn.Open();
                using var cmd = new SqlCommand(
                    "INSERT INTO Categorias (IdUtilizador, Nome, Emoji, Cor) VALUES (@UserId, @Nome, @Emoji, @Cor)", conn);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Nome", nome);
                cmd.Parameters.AddWithValue("@Emoji", emoji);
                cmd.Parameters.AddWithValue("@Cor", cor);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao adicionar categoria: {ex.Message}");
                throw;
            }
        }

        /// <summary>Cria as categorias predefinidas para um novo utilizador.</summary>
        public static void CriarCategoriasPredefinidas(int userId)
        {
            var categorias = new[]
            {
                ("Alimentação", "🍔", "#E74C3C"),
                ("Transporte", "🚗", "#3498DB"),
                ("Lazer", "🎮", "#F1C40F"),
                ("Saúde", "💊", "#1ABC9C"),
                ("Habitação", "🏠", "#9B59B6"),
                ("Salário", "💰", "#27AE60")
            };
            try
            {
                using var conn = ConexaoBD.ObterConexao();
                conn.Open();
                foreach (var cat in categorias)
                {
                    using var cmd = new SqlCommand(
                        "INSERT INTO Categorias (IdUtilizador, Nome, Emoji, Cor) VALUES (@UserId, @Nome, @Emoji, @Cor)", conn);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@Nome", cat.Item1);
                    cmd.Parameters.AddWithValue("@Emoji", cat.Item2);
                    cmd.Parameters.AddWithValue("@Cor", cat.Item3);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao criar categorias predefinidas: {ex.Message}");
            }
        }

        public static void Remover(int id)
        {
            using var conn = ConexaoBD.ObterConexao();
            conn.Open();
            // Tenta remover; se houver FK violation, lança exceção amigável
            try
            {
                using var cmd = new SqlCommand("DELETE FROM Categorias WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
            }
            catch
            {
                throw new Exception("Não é possível remover esta categoria porque tem despesas ou orçamentos associados.");
            }
        }
    }
}
