-- =============================================================
-- OASIS SURVIVAL - DATABASE CREATION & SCHEMA SCRIPT
-- Compatible with Microsoft SQL Server 2019 / 2022 Express
-- =============================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'OasisShooterDB')
BEGIN
    CREATE DATABASE [OasisShooterDB];
END
GO

USE [OasisShooterDB];
GO

-- 1. PLAYERS TABLE
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Players]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Players] (
        [PlayerID] INT IDENTITY(1,1) PRIMARY KEY,
        [Username] NVARCHAR(50) NOT NULL UNIQUE,
        [PasswordHash] NVARCHAR(255) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastLogin] DATETIME2 NULL
    );
    CREATE INDEX IX_Players_Username ON [dbo].[Players]([Username]);
END
GO

-- 2. MATCH SESSIONS TABLE
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MatchSessions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MatchSessions] (
        [SessionID] INT IDENTITY(1,1) PRIMARY KEY,
        [PlayerID] INT NOT NULL,
        [Score] INT NOT NULL DEFAULT 0,
        [HighestWave] INT NOT NULL DEFAULT 1,
        [TotalKills] INT NOT NULL DEFAULT 0,
        [Headshots] INT NOT NULL DEFAULT 0,
        [DurationSeconds] INT NOT NULL DEFAULT 0,
        [IsVictory] BIT NOT NULL DEFAULT 0,
        [PlayedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_MatchSessions_Players FOREIGN KEY ([PlayerID]) REFERENCES [dbo].[Players]([PlayerID]) ON DELETE CASCADE
    );
    CREATE INDEX IX_MatchSessions_PlayerID ON [dbo].[MatchSessions]([PlayerID]);
    CREATE INDEX IX_MatchSessions_Score ON [dbo].[MatchSessions]([Score] DESC);
END
GO

-- 3. GLOBAL LEADERBOARD VIEW
IF OBJECT_ID(N'[dbo].[v_Leaderboard]', N'V') IS NOT NULL
    DROP VIEW [dbo].[v_Leaderboard];
GO

CREATE VIEW [dbo].[v_Leaderboard] AS
SELECT TOP 10
    p.PlayerID,
    p.Username,
    ISNULL(MAX(m.Score), 0) AS BestScore,
    ISNULL(MAX(m.HighestWave), 1) AS MaxWaveReached,
    ISNULL(SUM(m.TotalKills), 0) AS LifetimeKills,
    ISNULL(SUM(m.Headshots), 0) AS LifetimeHeadshots,
    COUNT(m.SessionID) AS MatchesPlayed
FROM [dbo].[Players] p
INNER JOIN [dbo].[MatchSessions] m ON p.PlayerID = m.PlayerID
GROUP BY p.PlayerID, p.Username
ORDER BY BestScore DESC, MaxWaveReached DESC, LifetimeKills DESC;
GO
