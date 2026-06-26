using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using DailyBudgetWPF.Modelos;

namespace DailyBudgetWPF.Dados.Repositorios
{
    public static class RepositorioOrcamentos
    {
        /// <summary>Obtém todos os orçamentos do utilizador com o gasto atual do mês.</summary>
        public static List<OrcamentoCategoria> ObterOrcamentos(int userId)
        {
            var lista = new List<OrcamentoCategoria>();
            try
            {
                using var conn = ConexaoBD.ObterConexao();
                conn.Open();
                string sql = @"
                    SELECT o.Id, o.IdCategoria, c.Nome, c.Emoji, o.LimiteMensal,
                           ISNULL((SELECT SUM(d.Valor) FROM Despesas d
                                   WHERE d.IdCategoria = o.IdCategoria
                                     AND d.IdUtilizador = @Id
                                     AND MONTH(d.Data) = MONTH(GETDATE())
                                     AND YEAR(d.Data) = YEAR(GETDATE())
                                     AND d.Data <= CAST(GETDATE() AS DATE)), 0) AS GastoMes
                    FROM OrcamentoCategorias o
                    INNER JOIN Categorias c ON o.IdCategoria = c.Id
                    WHERE o.IdUtilizador = @Id
                    ORDER BY c.Nome";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", userId);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    lista.Add(new OrcamentoCategoria
                    {
                        Id = r.GetInt32(0),
                        IdCategoria = r.GetInt32(1),
                        NomeCategoria = r.GetString(2),
                        EmojiCategoria = r.IsDBNull(3) ? "📁" : r.GetString(3),
                        LimiteMensal = Convert.ToDecimal(r.GetValue(4)),
                        GastoMesAtual = Convert.ToDecimal(r.GetValue(5))
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao obter orçamentos: {ex.Message}");
            }
            return lista;
        }

        /// <summary>Define (insere ou atualiza) o limite mensal de uma categoria.</summary>
        public static void DefinirOrcamento(int userId, int catId, decimal limite)
        {
            using var conn = ConexaoBD.ObterConexao();
            conn.Open();
            string sql = @"
                MERGE OrcamentoCategorias AS target
                USING (SELECT @UserId AS IdUtilizador, @CatId AS IdCategoria) AS source
                ON (target.IdUtilizador = source.IdUtilizador AND target.IdCategoria = source.IdCategoria)
                WHEN MATCHED THEN UPDATE SET LimiteMensal = @Limite
                WHEN NOT MATCHED THEN INSERT (IdUtilizador, IdCategoria, LimiteMensal) VALUES (@UserId, @CatId, @Limite);";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@CatId", catId);
            cmd.Parameters.AddWithValue("@Limite", limite);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Remove o orçamento de uma categoria.</summary>
        public static void RemoverOrcamento(int id)
        {
            using var conn = ConexaoBD.ObterConexao();
            conn.Open();
            using var cmd = new SqlCommand("DELETE FROM OrcamentoCategorias WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Retorna apenas os orçamentos ultrapassados ou em risco (&gt;80%).</summary>
        public static List<OrcamentoCategoria> ObterAlertas(int userId)
        {
            try
            {
                var todos = ObterOrcamentos(userId);
                return todos.FindAll(o => o.Percentagem >= 80);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao obter alertas: {ex.Message}");
                return new List<OrcamentoCategoria>();
            }
        }
    }
}
