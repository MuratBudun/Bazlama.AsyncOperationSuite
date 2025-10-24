-- Create database schema for Bazlama.AsyncOperationSuite MSSQL Storage
-- This script is safe to run multiple times - it only creates tables and indexes if they don't exist
-- Bu script birden çok kez çalıştırılabilir - sadece mevcut olmayan tabloları ve indexleri oluşturur
-- Compatible with SQL Server 2012+ - Tüm SQL Server sürümleriyle uyumlu

PRINT 'Starting Bazlama AsyncOperationSuite database schema setup...';
PRINT 'Bazlama AsyncOperationSuite veritabanı şeması kurulumu başlıyor...';

-- AsyncOperations table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AsyncOperations' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[AsyncOperations](
        [_id] [nvarchar](255) NOT NULL PRIMARY KEY,
        [CreatedAt] [datetime2](7) NOT NULL,
        [OwnerId] [nvarchar](255) NULL,
        [Name] [nvarchar](255) NULL,
        [Status] [int] NOT NULL DEFAULT(0),
        [Description] [nvarchar](max) NULL,
        [StartedAt] [datetime2](7) NULL,
        [CompletedAt] [datetime2](7) NULL,
        [FailedAt] [datetime2](7) NULL,
        [CanceledAt] [datetime2](7) NULL,
        [ErrorMessage] [nvarchar](max) NULL,
        [InnerErrorMessage] [nvarchar](max) NULL,
        [ErrorStackTrace] [nvarchar](max) NULL,
        [ExecutionTimeMs] [int] NOT NULL DEFAULT(0)
    );
    PRINT 'AsyncOperations table created successfully.';
END
ELSE
BEGIN
    PRINT 'AsyncOperations table already exists.';
END

-- AsyncOperationPayloads table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AsyncOperationPayloads' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[AsyncOperationPayloads](
        [_id] [nvarchar](255) NOT NULL PRIMARY KEY,
        [CreatedAt] [datetime2](7) NOT NULL,
        [OwnerId] [nvarchar](255) NULL,
        [OperationId] [nvarchar](255) NULL,
        [PayloadType] [nvarchar](255) NULL,
        [Name] [nvarchar](255) NULL,
        [Description] [nvarchar](max) NULL,
        [PayloadData] [nvarchar](max) NULL
    );
    PRINT 'AsyncOperationPayloads table created successfully.';
END
ELSE
BEGIN
    PRINT 'AsyncOperationPayloads table already exists.';
END

-- Add foreign key constraint if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_AsyncOperationPayloads_OperationId')
BEGIN
    ALTER TABLE [dbo].[AsyncOperationPayloads]
    ADD CONSTRAINT FK_AsyncOperationPayloads_OperationId 
    FOREIGN KEY ([OperationId]) REFERENCES [AsyncOperations]([_id]) ON DELETE NO ACTION;
    PRINT 'Foreign key FK_AsyncOperationPayloads_OperationId created.';
END

-- AsyncOperationProgress table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AsyncOperationProgress' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[AsyncOperationProgress](
        [_id] [nvarchar](255) NOT NULL PRIMARY KEY,
        [CreatedAt] [datetime2](7) NOT NULL,
        [OwnerId] [nvarchar](255) NULL,
        [OperationId] [nvarchar](255) NULL,
        [Status] [int] NOT NULL DEFAULT(0),
        [Progress] [int] NOT NULL DEFAULT(0),
        [ProgressMessage] [nvarchar](max) NULL
    );
    PRINT 'AsyncOperationProgress table created successfully.';
END
ELSE
BEGIN
    PRINT 'AsyncOperationProgress table already exists.';
END

-- Add foreign key constraint if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_AsyncOperationProgress_OperationId')
BEGIN
    ALTER TABLE [dbo].[AsyncOperationProgress]
    ADD CONSTRAINT FK_AsyncOperationProgress_OperationId 
    FOREIGN KEY ([OperationId]) REFERENCES [AsyncOperations]([_id]) ON DELETE NO ACTION;
    PRINT 'Foreign key FK_AsyncOperationProgress_OperationId created.';
END

-- AsyncOperationResults table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AsyncOperationResults' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[AsyncOperationResults](
        [_id] [nvarchar](255) NOT NULL PRIMARY KEY,
        [CreatedAt] [datetime2](7) NOT NULL,
        [OwnerId] [nvarchar](255) NULL,
        [OperationId] [nvarchar](255) NULL,
        [Result] [nvarchar](max) NULL,
        [ResultMessage] [nvarchar](max) NULL
    );
    PRINT 'AsyncOperationResults table created successfully.';
END
ELSE
BEGIN
    PRINT 'AsyncOperationResults table already exists.';
END

-- Add foreign key constraint if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_AsyncOperationResults_OperationId')
BEGIN
    ALTER TABLE [dbo].[AsyncOperationResults]
    ADD CONSTRAINT FK_AsyncOperationResults_OperationId 
    FOREIGN KEY ([OperationId]) REFERENCES [AsyncOperations]([_id]) ON DELETE NO ACTION;
    PRINT 'Foreign key FK_AsyncOperationResults_OperationId created.';
END

-- Create indexes for better performance - compatible with all SQL Server versions
-- Performans için indexler oluştur - tüm SQL Server sürümleriyle uyumlu

-- AsyncOperations table indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AsyncOperations_Status' AND object_id = OBJECT_ID('AsyncOperations'))
BEGIN
    CREATE INDEX IX_AsyncOperations_Status ON [AsyncOperations]([Status]);
    PRINT 'Index IX_AsyncOperations_Status created successfully.';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AsyncOperations_OwnerId' AND object_id = OBJECT_ID('AsyncOperations'))
BEGIN
    CREATE INDEX IX_AsyncOperations_OwnerId ON [AsyncOperations]([OwnerId]);
    PRINT 'Index IX_AsyncOperations_OwnerId created successfully.';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AsyncOperations_CreatedAt' AND object_id = OBJECT_ID('AsyncOperations'))
BEGIN
    CREATE INDEX IX_AsyncOperations_CreatedAt ON [AsyncOperations]([CreatedAt]);
    PRINT 'Index IX_AsyncOperations_CreatedAt created successfully.';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AsyncOperations_Status_OwnerId' AND object_id = OBJECT_ID('AsyncOperations'))
BEGIN
    CREATE INDEX IX_AsyncOperations_Status_OwnerId ON [AsyncOperations]([Status], [OwnerId]);
    PRINT 'Index IX_AsyncOperations_Status_OwnerId created successfully.';
END

-- Foreign key indexes for better JOIN performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AsyncOperationPayloads_OperationId' AND object_id = OBJECT_ID('AsyncOperationPayloads'))
BEGIN
    CREATE INDEX IX_AsyncOperationPayloads_OperationId ON [AsyncOperationPayloads]([OperationId]);
    PRINT 'Index IX_AsyncOperationPayloads_OperationId created successfully.';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AsyncOperationProgress_OperationId' AND object_id = OBJECT_ID('AsyncOperationProgress'))
BEGIN
    CREATE INDEX IX_AsyncOperationProgress_OperationId ON [AsyncOperationProgress]([OperationId]);
    PRINT 'Index IX_AsyncOperationProgress_OperationId created successfully.';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AsyncOperationResults_OperationId' AND object_id = OBJECT_ID('AsyncOperationResults'))
BEGIN
    CREATE INDEX IX_AsyncOperationResults_OperationId ON [AsyncOperationResults]([OperationId]);
    PRINT 'Index IX_AsyncOperationResults_OperationId created successfully.';
END

PRINT 'All indexes verified/created successfully.';
PRINT 'Database schema setup completed successfully!';
PRINT 'Veritabanı şeması başarıyla kuruldu!';

-- Note: Foreign keys are set to NO ACTION to avoid cascade path conflicts
-- For cleanup operations, use application-level cascade delete logic
-- Cascading delete can be handled in the application code when deleting operations

PRINT 'NOTE: Foreign keys use NO ACTION constraint to avoid SQL Server cascade conflicts.';
PRINT 'NOT: Foreign key''ler SQL Server cascade çakışmalarını önlemek için NO ACTION kullanır.';
PRINT 'Application should handle related record cleanup when deleting operations.';
PRINT 'Uygulama operasyon silerken ilgili kayıtların temizliğini yapmalıdır.';

-- Show summary
SELECT 
    'Tables Created/Verified' as Category,
    COUNT(*) as Count
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN ('AsyncOperations', 'AsyncOperationPayloads', 'AsyncOperationProgress', 'AsyncOperationResults')
AND TABLE_SCHEMA = 'dbo'

UNION ALL

SELECT 
    'Indexes Created/Verified' as Category,
    COUNT(*) as Count
FROM sys.indexes i
INNER JOIN sys.objects o ON i.object_id = o.object_id
WHERE o.name IN ('AsyncOperations', 'AsyncOperationPayloads', 'AsyncOperationProgress', 'AsyncOperationResults')
AND i.name LIKE 'IX_%';