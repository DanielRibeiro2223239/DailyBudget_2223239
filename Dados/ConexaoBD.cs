using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System;

namespace DailyBudgetWPF.Dados
{
    public static class ConexaoBD
    {
        private const string SERVIDOR = @"localhost\SQLEXPRESS";
        private const string BASE_DADOS = "DailyBudget_2223239";

        public static string StringConexao => $"Server={SERVIDOR};Database={BASE_DADOS};Integrated Security=True;TrustServerCertificate=True;";
        public static SqlConnection ObterConexao() => new(StringConexao);

        public static void InicializarBancoDeDados()
        {
            string stringMaster = $"Server={SERVIDOR};Database=master;Integrated Security=True;TrustServerCertificate=True;";
            try 
            {
                using (var conexaoMaster = new SqlConnection(stringMaster))
                {
                    conexaoMaster.Open();
                    string sqlCreateDb = $@"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '{BASE_DADOS}') CREATE DATABASE {BASE_DADOS};";
                    using (var cmd = new SqlCommand(sqlCreateDb, conexaoMaster)) cmd.ExecuteNonQuery();
                }

                using (var conexao = ObterConexao())
                {
                    conexao.Open();
                    string sqlSchema = @"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Utilizadores')
                        CREATE TABLE Utilizadores (Id INT IDENTITY(1,1) PRIMARY KEY, Nome NVARCHAR(100) NOT NULL, Username NVARCHAR(50) NOT NULL UNIQUE, Email NVARCHAR(150) NOT NULL UNIQUE, Senha NVARCHAR(255) NOT NULL, DataCriacao DATETIME DEFAULT GETDATE());

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Utilizadores') AND name = 'Username')
                        BEGIN
                            ALTER TABLE Utilizadores ADD Username NVARCHAR(50) NULL;
                            EXEC('UPDATE Utilizadores SET Username = REPLACE(Email, ''@'', ''_'') WHERE Username IS NULL');
                            ALTER TABLE Utilizadores ALTER COLUMN Username NVARCHAR(50) NOT NULL;
                            ALTER TABLE Utilizadores ADD CONSTRAINT UQ_Utilizadores_Username UNIQUE (Username);
                        END

                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Categorias')
                        BEGIN
                            CREATE TABLE Categorias (Id INT IDENTITY(1,1) PRIMARY KEY, IdUtilizador INT, Nome NVARCHAR(100) NOT NULL, Emoji NVARCHAR(10), Cor NVARCHAR(7) DEFAULT '#27AE60', FOREIGN KEY (IdUtilizador) REFERENCES Utilizadores(Id));
                        END
                        ELSE IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Categorias') AND name = 'IdUtilizador')
                        BEGIN
                            ALTER TABLE Categorias ADD IdUtilizador INT NULL;
                            -- Migrar categorias existentes para o primeiro utilizador (utilizador demo)
                            EXEC('UPDATE Categorias SET IdUtilizador = (SELECT TOP 1 Id FROM Utilizadores ORDER BY Id) WHERE IdUtilizador IS NULL');
                        END

                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Receitas')
                        CREATE TABLE Receitas (Id INT IDENTITY(1,1) PRIMARY KEY, IdUtilizador INT NOT NULL, Descricao NVARCHAR(200) NOT NULL, Valor DECIMAL(10,2) NOT NULL, Data DATE DEFAULT GETDATE(), FOREIGN KEY (IdUtilizador) REFERENCES Utilizadores(Id));

                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Despesas')
                        CREATE TABLE Despesas (Id INT IDENTITY(1,1) PRIMARY KEY, IdUtilizador INT NOT NULL, IdCategoria INT, IdProduto INT, IdEstabelecimento INT, Descricao NVARCHAR(200) NOT NULL, Valor DECIMAL(10,2) NOT NULL, Data DATE DEFAULT GETDATE(), FOREIGN KEY (IdUtilizador) REFERENCES Utilizadores(Id), FOREIGN KEY (IdCategoria) REFERENCES Categorias(Id));

                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ListaDesejos')
                        CREATE TABLE ListaDesejos (Id INT IDENTITY(1,1) PRIMARY KEY, IdUtilizador INT NOT NULL, Item NVARCHAR(200) NOT NULL, ValorEstimado DECIMAL(10,2), Prioridade INT DEFAULT 1, Adquirido BIT DEFAULT 0, FOREIGN KEY (IdUtilizador) REFERENCES Utilizadores(Id));

                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Estabelecimentos')
                        CREATE TABLE Estabelecimentos (Id INT IDENTITY(1,1) PRIMARY KEY, Nome NVARCHAR(200) NOT NULL, Morada NVARCHAR(255), VezesVisitado INT DEFAULT 1);

                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Produtos')
                        CREATE TABLE Produtos (Id INT IDENTITY(1,1) PRIMARY KEY, Nome NVARCHAR(200) NOT NULL, IdCategoria INT, VezesUsado INT DEFAULT 1, UltimoValor DECIMAL(10,2), FOREIGN KEY (IdCategoria) REFERENCES Categorias(Id));

                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrcamentoCategorias')
                        CREATE TABLE OrcamentoCategorias (Id INT IDENTITY(1,1) PRIMARY KEY, IdUtilizador INT NOT NULL, IdCategoria INT NOT NULL, LimiteMensal DECIMAL(10,2) NOT NULL, FOREIGN KEY (IdUtilizador) REFERENCES Utilizadores(Id), FOREIGN KEY (IdCategoria) REFERENCES Categorias(Id), CONSTRAINT UQ_Orc UNIQUE (IdUtilizador, IdCategoria));
                    ";
                    using (var cmd = new SqlCommand(sqlSchema, conexao)) cmd.ExecuteNonQuery();
                    PopularDadosDemo(conexao);
                }
            }
            catch (Exception ex) { DailyBudgetWPF.Vistas.CaixaMensagem.Mostrar("Erro BD: " + ex.Message, "Erro Base de Dados", DailyBudgetWPF.Vistas.TipoMensagem.Erro); }
        }

        private static void PopularDadosDemo(SqlConnection conn)
        {
            // Verificar se já existem utilizadores
            using (var cmdCount = new SqlCommand("SELECT COUNT(*) FROM Utilizadores", conn))
            {
                int count = (int)cmdCount.ExecuteScalar();
                if (count > 0) return; // Já tem dados, não faz nada
            }

            // Criar utilizador de demonstração
            string senhaHash = GerarHash("123456");
            int userId = 0;
            const string sqlUser = "INSERT INTO Utilizadores (Nome, Username, Email, Senha) OUTPUT INSERTED.Id VALUES (@Nome, @Username, @Email, @Senha)";
            using (var cmdUser = new SqlCommand(sqlUser, conn))
            {
                cmdUser.Parameters.AddWithValue("@Nome", "Daniel Ribeiro");
                cmdUser.Parameters.AddWithValue("@Username", "daniel");
                cmdUser.Parameters.AddWithValue("@Email", "daniel@dailybudget.pt");
                cmdUser.Parameters.AddWithValue("@Senha", senhaHash);
                userId = (int)cmdUser.ExecuteScalar();
            }

            // Inserir categorias predefinidas para este utilizador
            var categoriasPredefinidas = new[]
            {
                ("Alimentação", "🍔", "#E74C3C"),
                ("Transporte", "🚗", "#3498DB"),
                ("Lazer", "🎮", "#F1C40F"),
                ("Saúde", "💊", "#1ABC9C"),
                ("Habitação", "🏠", "#9B59B6"),
                ("Salário", "💰", "#27AE60")
            };
            string sqlCat = "INSERT INTO Categorias (IdUtilizador, Nome, Emoji, Cor) OUTPUT INSERTED.Id, INSERTED.Nome VALUES (@Uid, @Nome, @Emoji, @Cor)";
            // Mapear IDs das categorias criadas
            var catIds = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var cat in categoriasPredefinidas)
            {
                using var cmdCat = new SqlCommand(sqlCat, conn);
                cmdCat.Parameters.AddWithValue("@Uid", userId);
                cmdCat.Parameters.AddWithValue("@Nome", cat.Item1);
                cmdCat.Parameters.AddWithValue("@Emoji", cat.Item2);
                cmdCat.Parameters.AddWithValue("@Cor", cat.Item3);
                using var rdr = cmdCat.ExecuteReader();
                if (rdr.Read()) catIds[rdr.GetString(1)] = rdr.GetInt32(0);
            }

            // Inserir Receitas
            string sqlRec = "INSERT INTO Receitas (IdUtilizador, Descricao, Valor, Data) VALUES (@Uid, @Desc, @Val, @Dt)";
            var receitas = new[]
            {
                ("Salário Mensal", 1500.00m, new DateTime(2026, 3, 25)),
                ("Salário Mensal", 1500.00m, new DateTime(2026, 4, 25)),
                ("Salário Mensal", 1500.00m, new DateTime(2026, 5, 25)),
                ("Projeto Freelance", 250.00m, new DateTime(2026, 5, 10))
            };
            foreach (var r in receitas)
            {
                using var cmd = new SqlCommand(sqlRec, conn);
                cmd.Parameters.AddWithValue("@Uid", userId);
                cmd.Parameters.AddWithValue("@Desc", r.Item1);
                cmd.Parameters.AddWithValue("@Val", r.Item2);
                cmd.Parameters.AddWithValue("@Dt", r.Item3);
                cmd.ExecuteNonQuery();
            }

            // Inserir Despesas
            string sqlDesp = "INSERT INTO Despesas (IdUtilizador, IdCategoria, Descricao, Valor, Data) VALUES (@Uid, @Cid, @Desc, @Val, @Dt)";
            var despesas = new[]
            {
                ("Renda Casa", 550.00m, new DateTime(2026, 3, 1), "Habitação"),
                ("Renda Casa", 550.00m, new DateTime(2026, 4, 1), "Habitação"),
                ("Renda Casa", 550.00m, new DateTime(2026, 5, 1), "Habitação"),
                
                ("Compras Supermercado", 85.50m, new DateTime(2026, 3, 5), "Alimentação"),
                ("Jantar de Amigos", 24.00m, new DateTime(2026, 3, 12), "Alimentação"),
                ("Compras Supermercado", 92.30m, new DateTime(2026, 4, 5), "Alimentação"),
                ("Almoço de Trabalho", 15.50m, new DateTime(2026, 4, 18), "Alimentação"),
                ("Compras Supermercado", 105.20m, new DateTime(2026, 5, 5), "Alimentação"),
                ("Sushi Takeaway", 35.00m, new DateTime(2026, 5, 15), "Alimentação"),

                ("Passe Metro", 40.00m, new DateTime(2026, 3, 2), "Transporte"),
                ("Combustível BP", 60.00m, new DateTime(2026, 3, 15), "Transporte"),
                ("Passe Metro", 40.00m, new DateTime(2026, 4, 2), "Transporte"),
                ("Combustível BP", 55.00m, new DateTime(2026, 4, 20), "Transporte"),
                ("Passe Metro", 40.00m, new DateTime(2026, 5, 2), "Transporte"),
                ("Combustível BP", 65.00m, new DateTime(2026, 5, 18), "Transporte"),

                ("Subscrição Netflix", 15.99m, new DateTime(2026, 3, 10), "Lazer"),
                ("Bilhete Cinema", 7.50m, new DateTime(2026, 3, 20), "Lazer"),
                ("Subscrição Netflix", 15.99m, new DateTime(2026, 4, 10), "Lazer"),
                ("Jogo Steam", 29.99m, new DateTime(2026, 4, 22), "Lazer"),
                ("Subscrição Netflix", 15.99m, new DateTime(2026, 5, 10), "Lazer"),

                ("Farmácia Wells", 12.40m, new DateTime(2026, 4, 15), "Saúde"),
                ("Consulta Dentista", 50.00m, new DateTime(2026, 5, 12), "Saúde")
            };

            foreach (var d in despesas)
            {
                if (catIds.TryGetValue(d.Item4, out int cid))
                {
                    using var cmd = new SqlCommand(sqlDesp, conn);
                    cmd.Parameters.AddWithValue("@Uid", userId);
                    cmd.Parameters.AddWithValue("@Cid", cid);
                    cmd.Parameters.AddWithValue("@Desc", d.Item1);
                    cmd.Parameters.AddWithValue("@Val", d.Item2);
                    cmd.Parameters.AddWithValue("@Dt", d.Item3);
                    cmd.ExecuteNonQuery();
                }
            }

            // Inserir Wishlist Items
            string sqlWish = "INSERT INTO ListaDesejos (IdUtilizador, Item, ValorEstimado, Prioridade, Adquirido) VALUES (@Uid, @Item, @Val, @Prio, @Adq)";
            var desejos = new[]
            {
                ("PlayStation 5", 549.00m, 2, false),
                ("Smart TV 4K", 399.00m, 1, false),
                ("Teclado Mecânico", 89.90m, 3, true)
            };
            foreach (var w in desejos)
            {
                using var cmd = new SqlCommand(sqlWish, conn);
                cmd.Parameters.AddWithValue("@Uid", userId);
                cmd.Parameters.AddWithValue("@Item", w.Item1);
                cmd.Parameters.AddWithValue("@Val", w.Item2);
                cmd.Parameters.AddWithValue("@Prio", w.Item3);
                cmd.Parameters.AddWithValue("@Adq", w.Item4 ? 1 : 0);
                cmd.ExecuteNonQuery();
            }

            // Inserir Budgets
            string sqlBud = "INSERT INTO OrcamentoCategorias (IdUtilizador, IdCategoria, LimiteMensal) VALUES (@Uid, @Cid, @Lim)";
            var orcamentos = new[]
            {
                ("Habitação", 600.00m),
                ("Alimentação", 200.00m),
                ("Transporte", 120.00m),
                ("Lazer", 80.00m)
            };
            foreach (var o in orcamentos)
            {
                if (catIds.TryGetValue(o.Item1, out int cid))
                {
                    using var cmd = new SqlCommand(sqlBud, conn);
                    cmd.Parameters.AddWithValue("@Uid", userId);
                    cmd.Parameters.AddWithValue("@Cid", cid);
                    cmd.Parameters.AddWithValue("@Lim", o.Item2);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static string GerarHash(string senha)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(senha));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}
