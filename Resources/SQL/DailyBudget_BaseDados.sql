
USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'DailyBudget_2223239')
BEGIN
    CREATE DATABASE DailyBudget_2223239;
END
GO

USE DailyBudget_2223239;
GO

-- 1. TABELA DE UTILIZADORES
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Utilizadores')
BEGIN
    CREATE TABLE Utilizadores (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nome VARCHAR(100) NOT NULL,
        Email VARCHAR(150) NOT NULL UNIQUE,
        Senha VARCHAR(255) NOT NULL,
        DataCriacao DATETIME DEFAULT GETDATE()
    );
END
GO

-- 2. TABELA DE CATEGORIAS
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Categorias')
BEGIN
    CREATE TABLE Categorias (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nome VARCHAR(100) NOT NULL,
        Emoji VARCHAR(10),
        Cor VARCHAR(7) DEFAULT '#27AE60'
    );

    -- Categorias Iniciais
    INSERT INTO Categorias (Nome, Emoji, Cor) VALUES 
    ('Alimentação', '🍔', '#E74C3C'),
    ('Transporte', '🚗', '#3498DB'),
    ('Lazer', '🎮', '#F1C40F'),
    ('Saúde', '💊', '#1ABC9C'),
    ('Habitação', '🏠', '#9B59B6'),
    ('Salário', '💰', '#27AE60');
END
GO

-- 3. TABELA DE RECEITAS
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Receitas')
BEGIN
    CREATE TABLE Receitas (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        IdUtilizador INT NOT NULL,
        Descricao VARCHAR(200) NOT NULL,
        Valor DECIMAL(10,2) NOT NULL,
        Data DATE DEFAULT GETDATE(),
        CONSTRAINT FK_Receitas_Utilizador FOREIGN KEY (IdUtilizador) REFERENCES Utilizadores(Id)
    );
END
GO

-- 4. TABELA DE DESPESAS
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Despesas')
BEGIN
    CREATE TABLE Despesas (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        IdUtilizador INT NOT NULL,
        IdCategoria INT,
        IdProduto INT,
        Descricao VARCHAR(200) NOT NULL,
        Valor DECIMAL(10,2) NOT NULL,
        Data DATE DEFAULT GETDATE(),
        CONSTRAINT FK_Despesas_Utilizador FOREIGN KEY (IdUtilizador) REFERENCES Utilizadores(Id),
        CONSTRAINT FK_Despesas_Categoria FOREIGN KEY (IdCategoria) REFERENCES Categorias(Id)
    );
END
GO

-- 5. TABELA LISTA DE DESEJOS (WISHLIST)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ListaDesejos')
BEGIN
    CREATE TABLE ListaDesejos (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        IdUtilizador INT NOT NULL,
        Item VARCHAR(200) NOT NULL,
        ValorEstimado DECIMAL(10,2),
        Prioridade INT DEFAULT 1,
        Adquirido BIT DEFAULT 0,
        CONSTRAINT FK_Wishlist_Utilizador FOREIGN KEY (IdUtilizador) REFERENCES Utilizadores(Id)
    );
END
GO

-- 6. TABELA DE PRODUTOS (SISTEMA INTELIGENTE DE AUTO-COMPLETAR)
-- O sistema aprende automaticamente os produtos/locais onde o utilizador gasta dinheiro.
-- Quando uma despesa é registada, o produto é guardado e sugerido na próxima vez.
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Produtos')
BEGIN
    CREATE TABLE Produtos (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nome NVARCHAR(200) NOT NULL,
        IdCategoria INT,
        VezesUsado INT DEFAULT 1,
        UltimoValor DECIMAL(10,2),
        CONSTRAINT FK_Produtos_Categoria FOREIGN KEY (IdCategoria) REFERENCES Categorias(Id)
    );
END
GO

-- 7. TABELA DE ORÇAMENTOS MENSAIS
-- Permite ao utilizador definir limites de gasto por categoria para cada mês.
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Orcamentos')
BEGIN
    CREATE TABLE Orcamentos (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        IdUtilizador INT NOT NULL,
        IdCategoria INT NOT NULL,
        LimiteMensal DECIMAL(10,2) NOT NULL,
        Mes INT NOT NULL,
        Ano INT NOT NULL,
        CONSTRAINT FK_Orcamentos_Utilizador FOREIGN KEY (IdUtilizador) REFERENCES Utilizadores(Id),
        CONSTRAINT FK_Orcamentos_Categoria FOREIGN KEY (IdCategoria) REFERENCES Categorias(Id)
    );
END
GO

-- MIGRAÇÃO: Adicionar coluna IdProduto à tabela Despesas (se a tabela já existia antes)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Despesas') AND name = 'IdProduto')
BEGIN
    ALTER TABLE Despesas ADD IdProduto INT NULL;
END
GO

-- STORED PROCEDURE DE LOGIN
CREATE OR ALTER PROCEDURE sp_Login
    @Email VARCHAR(150),
    @Senha VARCHAR(255)
AS
BEGIN
    SELECT Id, Nome, Email FROM Utilizadores 
    WHERE Email = @Email AND Senha = @Senha;
END
GO


