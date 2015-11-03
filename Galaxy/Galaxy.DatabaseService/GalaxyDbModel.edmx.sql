
-- --------------------------------------------------
-- Entity Designer DDL Script for SQL Server 2005, 2008, 2012 and Azure
-- --------------------------------------------------
-- Date Created: 11/03/2015 11:15:49
-- Generated from EDMX file: C:\Dev\Repo\PhiSquare\Galaxy\Galaxy.DatabaseService\GalaxyDbModel.edmx
-- --------------------------------------------------

SET QUOTED_IDENTIFIER OFF;
GO
USE [UatDb];
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[FK_Deal_FK]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Deal] DROP CONSTRAINT [FK_Deal_FK];
GO
IF OBJECT_ID(N'[dbo].[FK_Deal_UserProfil]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Deal] DROP CONSTRAINT [FK_Deal_UserProfil];
GO
IF OBJECT_ID(N'[dbo].[FK_HistoricalPrice_FK]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[HistoricalPrice] DROP CONSTRAINT [FK_HistoricalPrice_FK];
GO
IF OBJECT_ID(N'[dbo].[FK_ProductId_FK]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Instrument] DROP CONSTRAINT [FK_ProductId_FK];
GO
IF OBJECT_ID(N'[dbo].[FK_VolParam_FK]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[VolParam] DROP CONSTRAINT [FK_VolParam_FK];
GO

-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[Deal]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Deal];
GO
IF OBJECT_ID(N'[dbo].[HistoricalPrice]', 'U') IS NOT NULL
    DROP TABLE [dbo].[HistoricalPrice];
GO
IF OBJECT_ID(N'[dbo].[Instrument]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Instrument];
GO
IF OBJECT_ID(N'[dbo].[Product]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Product];
GO
IF OBJECT_ID(N'[dbo].[UserProfil]', 'U') IS NOT NULL
    DROP TABLE [dbo].[UserProfil];
GO
IF OBJECT_ID(N'[dbo].[VolParam]', 'U') IS NOT NULL
    DROP TABLE [dbo].[VolParam];
GO

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'HistoricalPrice'
CREATE TABLE [dbo].[HistoricalPrice] (
    [InstrumentId] varchar(50)  NOT NULL,
    [ClosePrice] float  NOT NULL,
    [AsOfDate] datetime  NOT NULL
);
GO

-- Creating table 'Instrument'
CREATE TABLE [dbo].[Instrument] (
    [Id] varchar(50)  NOT NULL,
    [ProductId] varchar(50)  NOT NULL,
    [FullName] varchar(50)  NOT NULL,
    [OptionType] varchar(50)  NULL,
    [Strike] int  NULL,
    [MaturityDate] datetime  NOT NULL,
    [TtCode] varchar(50)  NOT NULL,
    [RefForwardId] varchar(50)  NULL,
    [RefFutureId] varchar(50)  NULL
);
GO

-- Creating table 'Product'
CREATE TABLE [dbo].[Product] (
    [Id] varchar(50)  NOT NULL,
    [LotSize] int  NOT NULL,
    [Market] varchar(50)  NOT NULL,
    [StrikeBase] int  NOT NULL,
    [ProductType] varchar(50)  NOT NULL,
    [ExerciseType] varchar(50)  NULL
);
GO

-- Creating table 'VolParam'
CREATE TABLE [dbo].[VolParam] (
    [ProductId] varchar(50)  NOT NULL,
    [MaturityDate] datetime  NOT NULL,
    [A] float  NOT NULL,
    [B] float  NOT NULL,
    [Sigma] float  NOT NULL,
    [Rho] float  NOT NULL,
    [M] float  NOT NULL
);
GO

-- Creating table 'Deal'
CREATE TABLE [dbo].[Deal] (
    [DealId] int IDENTITY(1,1) NOT NULL,
    [TraderId] varchar(50)  NOT NULL,
    [Quantity] int  NOT NULL,
    [ExecPrice] float  NOT NULL,
    [BookId] varchar(50)  NOT NULL,
    [TradeDate] datetime  NOT NULL,
    [Status] varchar(50)  NULL,
    [InstrumentId] varchar(50)  NOT NULL,
    [ClearingFee] float  NULL,
    [TransactionFee] float  NULL,
    [Broker] varchar(50)  NULL,
    [Counterparty] varchar(50)  NULL,
    [Comment] varchar(50)  NULL,
    [ForwardLevel] float  NULL,
    [VolatilityLevel] float  NULL
);
GO

-- Creating table 'UserProfil'
CREATE TABLE [dbo].[UserProfil] (
    [UserId] varchar(50)  NOT NULL,
    [FirstName] varchar(50)  NOT NULL,
    [LastName] varchar(50)  NOT NULL,
    [Job] varchar(50)  NOT NULL,
    [Email] varchar(50)  NOT NULL,
    [DailyReport] bit  NOT NULL,
    [WeeklyReport] bit  NOT NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [InstrumentId], [AsOfDate] in table 'HistoricalPrice'
ALTER TABLE [dbo].[HistoricalPrice]
ADD CONSTRAINT [PK_HistoricalPrice]
    PRIMARY KEY CLUSTERED ([InstrumentId], [AsOfDate] ASC);
GO

-- Creating primary key on [Id] in table 'Instrument'
ALTER TABLE [dbo].[Instrument]
ADD CONSTRAINT [PK_Instrument]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'Product'
ALTER TABLE [dbo].[Product]
ADD CONSTRAINT [PK_Product]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [ProductId], [MaturityDate] in table 'VolParam'
ALTER TABLE [dbo].[VolParam]
ADD CONSTRAINT [PK_VolParam]
    PRIMARY KEY CLUSTERED ([ProductId], [MaturityDate] ASC);
GO

-- Creating primary key on [DealId] in table 'Deal'
ALTER TABLE [dbo].[Deal]
ADD CONSTRAINT [PK_Deal]
    PRIMARY KEY CLUSTERED ([DealId] ASC);
GO

-- Creating primary key on [UserId] in table 'UserProfil'
ALTER TABLE [dbo].[UserProfil]
ADD CONSTRAINT [PK_UserProfil]
    PRIMARY KEY CLUSTERED ([UserId] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on [InstrumentId] in table 'HistoricalPrice'
ALTER TABLE [dbo].[HistoricalPrice]
ADD CONSTRAINT [FK_HistoricalPrice_FK]
    FOREIGN KEY ([InstrumentId])
    REFERENCES [dbo].[Instrument]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [ProductId] in table 'Instrument'
ALTER TABLE [dbo].[Instrument]
ADD CONSTRAINT [FK_ProductId_FK]
    FOREIGN KEY ([ProductId])
    REFERENCES [dbo].[Product]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_ProductId_FK'
CREATE INDEX [IX_FK_ProductId_FK]
ON [dbo].[Instrument]
    ([ProductId]);
GO

-- Creating foreign key on [ProductId] in table 'VolParam'
ALTER TABLE [dbo].[VolParam]
ADD CONSTRAINT [FK_VolParam_FK]
    FOREIGN KEY ([ProductId])
    REFERENCES [dbo].[Product]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [InstrumentId] in table 'Deal'
ALTER TABLE [dbo].[Deal]
ADD CONSTRAINT [FK_Deal_FK]
    FOREIGN KEY ([InstrumentId])
    REFERENCES [dbo].[Instrument]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_Deal_FK'
CREATE INDEX [IX_FK_Deal_FK]
ON [dbo].[Deal]
    ([InstrumentId]);
GO

-- Creating foreign key on [TraderId] in table 'Deal'
ALTER TABLE [dbo].[Deal]
ADD CONSTRAINT [FK_Deal_UserProfil]
    FOREIGN KEY ([TraderId])
    REFERENCES [dbo].[UserProfil]
        ([UserId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_Deal_UserProfil'
CREATE INDEX [IX_FK_Deal_UserProfil]
ON [dbo].[Deal]
    ([TraderId]);
GO

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------