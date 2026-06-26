using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using DailyBudgetWPF.Modelos;

namespace DailyBudgetWPF.Dados.Repositorios
{
    public static class RepositorioDesejos
    {
        public static List<ItemDesejado> ObterItens(int userId)
        {
            var lista = new List<ItemDesejado>();
            try
            {
                using var conn = ConexaoBD.ObterConexao();
                conn.Open();
                string sql = @"SELECT Id, Item, ValorEstimado, Prioridade, Adquirido 
                               FROM ListaDesejos 
                               WHERE IdUtilizador = @Id 
                               ORDER BY Adquirido ASC, Prioridade DESC, Item ASC";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", userId);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    lista.Add(new ItemDesejado
                    {
                        Id = r.GetInt32(0),
                        Item = r.GetString(1),
                        ValorEstimado = r.IsDBNull(2) ? 0 : r.GetDecimal(2),
                        Prioridade = r.GetInt32(3),
                        Adquirido = r.GetBoolean(4)
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao obter wishlist: {ex.Message}");
            }
            return lista;
        }

        public static bool AdicionarItem(int userId, string item, decimal valor, int prioridade)
        {
            try
            {
                using var conn = ConexaoBD.ObterConexao();
                conn.Open();
                using var cmd = new SqlCommand(
                    "INSERT INTO ListaDesejos (IdUtilizador, Item, ValorEstimado, Prioridade) VALUES (@Id, @Item, @Val, @Prio)", conn);
                cmd.Parameters.AddWithValue("@Id", userId);
                cmd.Parameters.AddWithValue("@Item", item);
                cmd.Parameters.AddWithValue("@Val", valor);
                cmd.Parameters.AddWithValue("@Prio", prioridade);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao adicionar à wishlist: {ex.Message}");
                return false;
            }
        }

        public static void MarcarComoAdquirido(int id)
        {
            ExecutarComando("UPDATE ListaDesejos SET Adquirido = 1 WHERE Id = @Id", id);
        }

        public static void RemoverItem(int id)
        {
            ExecutarComando("DELETE FROM ListaDesejos WHERE Id = @Id", id);
        }

        private static void ExecutarComando(string sql, int id)
        {
            try
            {
                using var conn = ConexaoBD.ObterConexao();
                conn.Open();
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao executar comando na wishlist: {ex.Message}");
            }
        }
    }
}
