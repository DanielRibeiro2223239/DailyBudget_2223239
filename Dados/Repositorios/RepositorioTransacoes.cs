using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Data.SqlClient;
using DailyBudgetWPF.Modelos;

namespace DailyBudgetWPF.Dados.Repositorios
{
    public static class RepositorioTransacoes
    {
        // ──────────────────────────────────────────────
        // Helpers internos
        // ──────────────────────────────────────────────
        private static decimal ExecScalar(string sql, int userId)
        {
            using var conn = ConexaoBD.ObterConexao();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", userId);
            var res = cmd.ExecuteScalar();
            return res != null && res != DBNull.Value ? Convert.ToDecimal(res) : 0;
        }

        // ──────────────────────────────────────────────
        // Totais
        // ──────────────────────────────────────────────
        public static decimal ObterTotalReceitas(int userId)
            => ExecScalar("SELECT ISNULL(SUM(Valor),0) FROM Receitas WHERE IdUtilizador = @Id AND Data <= CAST(GETDATE() AS DATE)", userId);

        public static decimal ObterTotalDespesas(int userId)
            => ExecScalar("SELECT ISNULL(SUM(Valor),0) FROM Despesas WHERE IdUtilizador = @Id AND Data <= CAST(GETDATE() AS DATE)", userId);

        // ──────────────────────────────────────────────
        // Transações recentes (dashboard)
        // ──────────────────────────────────────────────
        public static List<Transacao> ObterRecentes(int userId)
        {
            var lista = new List<Transacao>();
            using var conn = ConexaoBD.ObterConexao();
            conn.Open();
            string sql = @"
                SELECT TOP 20 Id, Descricao, Valor, Data, Tipo, Categoria FROM (
                    SELECT Id, Descricao, Valor, Data, 'Receita' AS Tipo, 'Saldo' AS Categoria
                    FROM Receitas WHERE IdUtilizador = @Id AND Data <= CAST(GETDATE() AS DATE)
                    UNION ALL
                    SELECT d.Id, d.Descricao, d.Valor, d.Data, 'Despesa' AS Tipo,
                           ISNULL(c.Nome, 'Sem Categoria') AS Categoria
                    FROM Despesas d LEFT JOIN Categorias c ON d.IdCategoria = c.Id
                    WHERE d.IdUtilizador = @Id AND d.Data <= CAST(GETDATE() AS DATE)
                ) AS T ORDER BY Data DESC";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", userId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                lista.Add(new Transacao
                {
                    Id = r.GetInt32(0),
                    Descricao = r.GetString(1),
                    Valor = r.GetDecimal(2),
                    Data = r.GetDateTime(3),
                    Tipo = r.GetString(4),
                    Categoria = r.GetString(5)
                });
            }
            return lista;
        }

        // ──────────────────────────────────────────────
        // Receitas
        // ──────────────────────────────────────────────
        public static List<Transacao> ObterReceitas(int userId)
            => ObterReceitasFiltradas(userId, null, null);

        public static List<Transacao> ObterReceitasFiltradas(int userId, DateTime? de, DateTime? ate)
        {
            var lista = new List<Transacao>();
            using var conn = ConexaoBD.ObterConexao();
            conn.Open();
            string sql = @"SELECT Id, Descricao, Valor, Data
                           FROM Receitas WHERE IdUtilizador = @Id
                           AND (@De IS NULL OR Data >= @De)
                           AND (@Ate IS NULL OR Data <= @Ate)
                           ORDER BY Data DESC";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", userId);
            cmd.Parameters.AddWithValue("@De", (object?)de ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Ate", (object?)ate ?? DBNull.Value);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                lista.Add(new Transacao
                {
                    Id = r.GetInt32(0),
                    Descricao = r.GetString(1),
                    Valor = r.GetDecimal(2),
                    Data = r.GetDateTime(3),
                    Tipo = "Receita",
                    Categoria = "Saldo"
                });
            }
            return lista;
        }

        public static void AdicionarReceita(int userId, string desc, decimal valor, DateTime data)
        {
            using var conn = ConexaoBD.ObterConexao();
            conn.Open();
            using var cmd = new SqlCommand(
                "INSERT INTO Receitas (IdUtilizador, Descricao, Valor, Data) VALUES (@Id, @Desc, @Val, @Data)", conn);
            cmd.Parameters.AddWithValue("@Id", userId);
            cmd.Parameters.AddWithValue("@Desc", desc);
            cmd.Parameters.AddWithValue("@Val", valor);
            cmd.Parameters.AddWithValue("@Data", data);
            cmd.ExecuteNonQuery();
        }

        public static void EditarReceita(int id, string desc, decimal valor, DateTime data)
        {
            using var conn = ConexaoBD.ObterConexao();
            conn.Open();
            using var cmd = new SqlCommand(
                "UPDATE Receitas SET Descricao=@Desc, Valor=@Val, Data=@Data WHERE Id=@Id", conn);
            cmd.Parameters.AddWithValue("@Desc", desc);
            cmd.Parameters.AddWithValue("@Val", valor);
            cmd.Parameters.AddWithValue("@Data", data);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
        }

        public static void RemoverReceita(int id)
        {
            using var conn = ConexaoBD.ObterConexao();
            conn.Open();
            using var cmd = new SqlCommand("DELETE FROM Receitas WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
        }

        // ──────────────────────────────────────────────
        // Despesas  (LEFT JOIN → inclui despesas sem categoria)
        // ──────────────────────────────────────────────
        public static List<Transacao> ObterDespesas(int userId)
            => ObterDespesasFiltradas(userId, null, null);

        public static List<Transacao> ObterDespesasFiltradas(int userId, DateTime? de, DateTime? ate)
        {
            var lista = new List<Transacao>();
            using var conn = ConexaoBD.ObterConexao();
            conn.Open();
            string sql = @"SELECT d.Id, d.Descricao, d.Valor, d.Data, ISNULL(c.Nome, 'Sem Categoria') AS Categoria
                           FROM Despesas d LEFT JOIN Categorias c ON d.IdCategoria = c.Id
                           WHERE d.IdUtilizador = @Id
                           AND (@De IS NULL OR d.Data >= @De)
                           AND (@Ate IS NULL OR d.Data <= @Ate)
                           ORDER BY d.Data DESC";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", userId);
            cmd.Parameters.AddWithValue("@De", (object?)de ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Ate", (object?)ate ?? DBNull.Value);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                lista.Add(new Transacao
                {
                    Id = r.GetInt32(0),
                    Descricao = r.GetString(1),
                    Valor = r.GetDecimal(2),
                    Data = r.GetDateTime(3),
                    Tipo = "Despesa",
                    Categoria = r.GetString(4)
                });
            }
            return lista;
        }

        public static void AdicionarDespesa(int userId, string desc, decimal valor, DateTime data, int catId)
        {
            using var conn = ConexaoBD.ObterConexao();
            conn.Open();
            using var cmd = new SqlCommand(
                "INSERT INTO Despesas (IdUtilizador, Descricao, Valor, Data, IdCategoria) VALUES (@Id, @Desc, @Val, @Data, @CatId)", conn);
            cmd.Parameters.AddWithValue("@Id", userId);
            cmd.Parameters.AddWithValue("@Desc", desc);
            cmd.Parameters.AddWithValue("@Val", valor);
            cmd.Parameters.AddWithValue("@Data", data);
            cmd.Parameters.AddWithValue("@CatId", catId);
            cmd.ExecuteNonQuery();
        }

        public static void EditarDespesa(int id, string desc, decimal valor, DateTime data, int catId)
        {
            using var conn = ConexaoBD.ObterConexao();
            conn.Open();
            using var cmd = new SqlCommand(
                "UPDATE Despesas SET Descricao=@Desc, Valor=@Val, Data=@Data, IdCategoria=@CatId WHERE Id=@Id", conn);
            cmd.Parameters.AddWithValue("@Desc", desc);
            cmd.Parameters.AddWithValue("@Val", valor);
            cmd.Parameters.AddWithValue("@Data", data);
            cmd.Parameters.AddWithValue("@CatId", catId);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
        }

        public static void RemoverDespesa(int id)
        {
            using var conn = ConexaoBD.ObterConexao();
            conn.Open();
            using var cmd = new SqlCommand("DELETE FROM Despesas WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
        }

        // ──────────────────────────────────────────────
        // Relatórios
        // ──────────────────────────────────────────────
        public static List<(string Periodo, decimal Receitas, decimal Despesas)> ObterRelatorioConsolidado(int userId, string tipo)
        {
            var dados = new List<(string, decimal, decimal)>();
            using var conn = ConexaoBD.ObterConexao();
            conn.Open();
            string formato = tipo switch { "Mensal" => "MM/yyyy", "Semanal" => "yyyy-WW", _ => "yyyy" };
            string sql = $@"
                SELECT Periodo, SUM(Receita) AS T_Rec, SUM(Despesa) AS T_Des
                FROM (
                    SELECT FORMAT(Data, '{formato}') AS Periodo, Valor AS Receita, 0 AS Despesa
                    FROM Receitas WHERE IdUtilizador = @Id
                    UNION ALL
                    SELECT FORMAT(Data, '{formato}') AS Periodo, 0 AS Receita, Valor AS Despesa
                    FROM Despesas WHERE IdUtilizador = @Id
                ) t
                GROUP BY Periodo
                ORDER BY Periodo";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", userId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                dados.Add((r.GetString(0), r.GetDecimal(1), r.GetDecimal(2)));
            }
            return dados;
        }

        public static List<(string Categoria, decimal Total)> ObterGastosPorCategoria(int userId)
        {
            var dados = new List<(string, decimal)>();
            using var conn = ConexaoBD.ObterConexao();
            conn.Open();
            using var cmd = new SqlCommand(
                @"SELECT ISNULL(c.Nome, 'Sem Categoria'), SUM(d.Valor)
                  FROM Despesas d LEFT JOIN Categorias c ON d.IdCategoria = c.Id
                  WHERE d.IdUtilizador = @Id
                  GROUP BY c.Nome", conn);
            cmd.Parameters.AddWithValue("@Id", userId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                dados.Add((r.GetString(0), r.GetDecimal(1)));
            }
            return dados;
        }
    }
}
