-- Memory-optimized filegroup for test database (runs in a separate batch after CREATE DATABASE)
USE [test];
IF NOT EXISTS (SELECT * FROM sys.filegroups WHERE type = 'FX')
BEGIN
    ALTER DATABASE [test] ADD FILEGROUP [test_mod] CONTAINS MEMORY_OPTIMIZED_DATA;
    ALTER DATABASE [test] ADD FILE (name='test_mod_file', filename='/var/opt/mssql/data/test_mod_file') TO FILEGROUP [test_mod];
END;
ALTER DATABASE [test] SET MEMORY_OPTIMIZED_ELEVATE_TO_SNAPSHOT = ON;
