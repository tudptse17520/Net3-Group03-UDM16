IF DB_ID('CaroDB') IS NULL
BEGIN
    CREATE DATABASE CaroDB;
END
GO

USE CaroDB;
GO

IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserId INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(50) NOT NULL UNIQUE,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

IF OBJECT_ID('dbo.Matches', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Matches
    (
        MatchId INT IDENTITY(1,1) PRIMARY KEY,
        PlayerXId INT NOT NULL,
        PlayerOId INT NOT NULL,
        WinnerId INT NULL,
        Result NVARCHAR(20) NOT NULL,
        StartTime DATETIME NOT NULL,
        EndTime DATETIME NOT NULL,

        CONSTRAINT FK_Matches_PlayerX
            FOREIGN KEY (PlayerXId)
            REFERENCES dbo.Users(UserId),

        CONSTRAINT FK_Matches_PlayerO
            FOREIGN KEY (PlayerOId)
            REFERENCES dbo.Users(UserId),

        CONSTRAINT FK_Matches_Winner
            FOREIGN KEY (WinnerId)
            REFERENCES dbo.Users(UserId)
    );
END
GO