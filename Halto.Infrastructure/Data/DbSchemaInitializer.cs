using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halto.Infrastructure.Data;

/// <summary>
/// Creates all tables using raw idempotent SQL.
/// Tables are created WITHOUT foreign keys first (order irrelevant),
/// FKs added after all tables exist — every step independently idempotent.
/// </summary>
public static class DbSchemaInitializer
{
    public static async Task EnsureSchemaAsync(HaltoDbContext db, ILogger logger)
    {
        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();

        try
        {
            foreach (var sql in Scripts)
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                await cmd.ExecuteNonQueryAsync();
            }
            logger.LogInformation("Schema script executed successfully.");
        }
        finally
        {
            await conn.CloseAsync();
        }
    }

    private static readonly string[] Scripts =
    [
        // ── 1. Tables (no FKs) ───────────────────────────────

        """
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Organizations')
            CREATE TABLE [Organizations] (
                [Id]           UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                [Name]         NVARCHAR(200)    NOT NULL,
                [BusinessType] INT              NOT NULL,
                [IsActive]     BIT              NOT NULL DEFAULT 1,
                [CreatedAt]    DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
                [UpdatedAt]    DATETIME2        NULL,
                CONSTRAINT [PK_Organizations] PRIMARY KEY ([Id])
            )
        """,

        """
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Users')
            CREATE TABLE [Users] (
                [Id]             UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                [Email]          NVARCHAR(256)    NOT NULL,
                [PasswordHash]   NVARCHAR(MAX)    NOT NULL,
                [FullName]       NVARCHAR(200)    NOT NULL,
                [Phone]          NVARCHAR(20)     NULL,
                [Role]           INT              NOT NULL,
                [IsActive]       BIT              NOT NULL DEFAULT 1,
                [CreatedAt]      DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
                [UpdatedAt]      DATETIME2        NULL,
                [OrganizationId] UNIQUEIDENTIFIER NULL,
                CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
            )
        """,

        """
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MemberCategories')
            CREATE TABLE [MemberCategories] (
                [Id]              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                [Name]            NVARCHAR(200)    NOT NULL,
                [Description]     NVARCHAR(500)    NULL,
                [MonthlyRent]     DECIMAL(18,2)    NOT NULL DEFAULT 0,
                [AdmissionFee]    DECIMAL(18,2)    NOT NULL DEFAULT 0,
                [DepositAmount]   DECIMAL(18,2)    NOT NULL DEFAULT 0,
                [IsActive]        BIT              NOT NULL DEFAULT 1,
                [CreatedAt]       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
                [UpdatedAt]       DATETIME2        NULL,
                [OrganizationId]  UNIQUEIDENTIFIER NOT NULL,
                CONSTRAINT [PK_MemberCategories] PRIMARY KEY ([Id])
            )
        """,

        """
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Members')
            CREATE TABLE [Members] (
                [Id]              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                [FullName]        NVARCHAR(200)    NOT NULL,
                [Email]           NVARCHAR(256)    NULL,
                [Phone]           NVARCHAR(20)     NULL,
                [Designation]     NVARCHAR(100)    NULL,
                [IdDocumentType]  NVARCHAR(50)     NULL,
                [IdDocumentUrl]   NVARCHAR(500)    NULL,
                [IsActive]        BIT              NOT NULL DEFAULT 1,
                [JoinedAt]        DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
                [DiscontinuedAt]  DATETIME2        NULL,
                [DiscontinuedReason] NVARCHAR(500) NULL,
                [CreatedAt]       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
                [UpdatedAt]       DATETIME2        NULL,
                [ExtraFieldsJson] NVARCHAR(MAX)    NULL,
                [OrganizationId]  UNIQUEIDENTIFIER NOT NULL,
                [CategoryId]      UNIQUEIDENTIFIER NULL,
                [UserId]          UNIQUEIDENTIFIER NULL,
                CONSTRAINT [PK_Members] PRIMARY KEY ([Id])
            )
        """,

        """
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Dues')
            CREATE TABLE [Dues] (
                [Id]             UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                [Year]           INT              NOT NULL,
                [Month]          INT              NOT NULL,
                [Amount]         DECIMAL(18,2)    NOT NULL,
                [Status]         INT              NOT NULL DEFAULT 1,
                [Notes]          NVARCHAR(500)    NULL,
                [CreatedAt]      DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
                [UpdatedAt]      DATETIME2        NULL,
                [OrganizationId] UNIQUEIDENTIFIER NOT NULL,
                [MemberId]       UNIQUEIDENTIFIER NOT NULL,
                CONSTRAINT [PK_Dues] PRIMARY KEY ([Id])
            )
        """,

        """
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Payments')
            CREATE TABLE [Payments] (
                [Id]             UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                [AmountPaid]     DECIMAL(18,2)    NOT NULL,
                [PaidOn]         DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
                [Method]         INT              NOT NULL DEFAULT 1,
                [Notes]          NVARCHAR(500)    NULL,
                [CreatedAt]      DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
                [OrganizationId] UNIQUEIDENTIFIER NOT NULL,
                [MemberId]       UNIQUEIDENTIFIER NOT NULL,
                [DueId]          UNIQUEIDENTIFIER NOT NULL,
                [MarkedByUserId] UNIQUEIDENTIFIER NOT NULL,
                CONSTRAINT [PK_Payments] PRIMARY KEY ([Id])
            )
        """,

        // ── 2. Foreign Keys ───────────────────────────────────

        "IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Users_Organizations') ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations]([Id]) ON DELETE NO ACTION",
        "IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MemberCategories_Organizations') ALTER TABLE [MemberCategories] ADD CONSTRAINT [FK_MemberCategories_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations]([Id]) ON DELETE NO ACTION",
        "IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Members_Organizations') ALTER TABLE [Members] ADD CONSTRAINT [FK_Members_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations]([Id]) ON DELETE NO ACTION",
        "IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Members_MemberCategories') ALTER TABLE [Members] ADD CONSTRAINT [FK_Members_MemberCategories] FOREIGN KEY ([CategoryId]) REFERENCES [MemberCategories]([Id]) ON DELETE NO ACTION",
        "IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Members_Users') ALTER TABLE [Members] ADD CONSTRAINT [FK_Members_Users] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE NO ACTION",
        "IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Dues_Organizations') ALTER TABLE [Dues] ADD CONSTRAINT [FK_Dues_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations]([Id]) ON DELETE NO ACTION",
        "IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Dues_Members') ALTER TABLE [Dues] ADD CONSTRAINT [FK_Dues_Members] FOREIGN KEY ([MemberId]) REFERENCES [Members]([Id]) ON DELETE NO ACTION",
        "IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Payments_Organizations') ALTER TABLE [Payments] ADD CONSTRAINT [FK_Payments_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations]([Id]) ON DELETE NO ACTION",
        "IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Payments_Members') ALTER TABLE [Payments] ADD CONSTRAINT [FK_Payments_Members] FOREIGN KEY ([MemberId]) REFERENCES [Members]([Id]) ON DELETE NO ACTION",
        "IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Payments_Dues') ALTER TABLE [Payments] ADD CONSTRAINT [FK_Payments_Dues] FOREIGN KEY ([DueId]) REFERENCES [Dues]([Id]) ON DELETE NO ACTION",
        "IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Payments_Users') ALTER TABLE [Payments] ADD CONSTRAINT [FK_Payments_Users] FOREIGN KEY ([MarkedByUserId]) REFERENCES [Users]([Id]) ON DELETE NO ACTION",
        "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Members') AND name = 'Designation') ALTER TABLE [Members] ADD [Designation] NVARCHAR(100) NULL",
        "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Members') AND name = 'IdDocumentType') ALTER TABLE [Members] ADD [IdDocumentType] NVARCHAR(50) NULL",
        "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Members') AND name = 'IdDocumentUrl') ALTER TABLE [Members] ADD [IdDocumentUrl] NVARCHAR(500) NULL",
        "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Members') AND name = 'DiscontinuedAt') ALTER TABLE [Members] ADD [DiscontinuedAt] DATETIME2 NULL",
        "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Members') AND name = 'DiscontinuedReason') ALTER TABLE [Members] ADD [DiscontinuedReason] NVARCHAR(500) NULL",
        "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Members') AND name = 'CategoryId') ALTER TABLE [Members] ADD [CategoryId] UNIQUEIDENTIFIER NULL",
        // ── 3. Indexes ────────────────────────────────────────

        "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Organizations_Name') CREATE INDEX [IX_Organizations_Name] ON [Organizations]([Name])",
        "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Email') CREATE UNIQUE INDEX [IX_Users_Email] ON [Users]([Email])",
        "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_OrganizationId') CREATE INDEX [IX_Users_OrganizationId] ON [Users]([OrganizationId])",
        "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MemberCategories_OrganizationId') CREATE INDEX [IX_MemberCategories_OrganizationId] ON [MemberCategories]([OrganizationId])",
        "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Members_OrganizationId') CREATE INDEX [IX_Members_OrganizationId] ON [Members]([OrganizationId])",
        "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Members_CategoryId') CREATE INDEX [IX_Members_CategoryId] ON [Members]([CategoryId])",
        "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Members_UserId') CREATE UNIQUE INDEX [IX_Members_UserId] ON [Members]([UserId]) WHERE [UserId] IS NOT NULL",

        // ── 3b. Alter existing Members table if it already exists (add new columns) ──
        "IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Members') AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Members') AND name = 'Designation') ALTER TABLE [Members] ADD [Designation] NVARCHAR(100) NULL",
        "IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Members') AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Members') AND name = 'IdDocumentType') ALTER TABLE [Members] ADD [IdDocumentType] NVARCHAR(50) NULL",
        "IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Members') AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Members') AND name = 'IdDocumentUrl') ALTER TABLE [Members] ADD [IdDocumentUrl] NVARCHAR(500) NULL",
        "IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Members') AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Members') AND name = 'DiscontinuedAt') ALTER TABLE [Members] ADD [DiscontinuedAt] DATETIME2 NULL",
        "IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Members') AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Members') AND name = 'DiscontinuedReason') ALTER TABLE [Members] ADD [DiscontinuedReason] NVARCHAR(500) NULL",
        "IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Members') AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Members') AND name = 'CategoryId') ALTER TABLE [Members] ADD [CategoryId] UNIQUEIDENTIFIER NULL",
        "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Dues_OrganizationId') CREATE INDEX [IX_Dues_OrganizationId] ON [Dues]([OrganizationId])",
        "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Dues_MemberId_Year_Month') CREATE UNIQUE INDEX [IX_Dues_MemberId_Year_Month] ON [Dues]([MemberId],[Year],[Month])",
        "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payments_OrganizationId') CREATE INDEX [IX_Payments_OrganizationId] ON [Payments]([OrganizationId])",
        "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payments_MemberId') CREATE INDEX [IX_Payments_MemberId] ON [Payments]([MemberId])",
        "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payments_DueId') CREATE INDEX [IX_Payments_DueId] ON [Payments]([DueId])",
        "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payments_MarkedByUserId') CREATE INDEX [IX_Payments_MarkedByUserId] ON [Payments]([MarkedByUserId])",
        
        // ── 4. EF Migration History ───────────────────────────

        """
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = '__EFMigrationsHistory')
            CREATE TABLE [__EFMigrationsHistory] (
                [MigrationId]    NVARCHAR(150) NOT NULL,
                [ProductVersion] NVARCHAR(32)  NOT NULL,
                CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
            )
        """,

        """
        IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20240101000000_InitialCreate')
            INSERT INTO [__EFMigrationsHistory]([MigrationId],[ProductVersion])
            VALUES('20240101000000_InitialCreate','9.0.0')
        """
    ];
}
