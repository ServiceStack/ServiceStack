-- Create test login and database
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'test')
    CREATE LOGIN [test] WITH PASSWORD = 'p@55wOrd', CHECK_POLICY = OFF;
ELSE
BEGIN
    ALTER LOGIN [test] ENABLE;
    ALTER LOGIN [test] WITH PASSWORD = 'p@55wOrd', CHECK_POLICY = OFF;
END;

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'test')
    CREATE DATABASE [test];

ALTER SERVER ROLE sysadmin ADD MEMBER [test];

-- Create northwind login and database
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'northwind')
    CREATE LOGIN [northwind] WITH PASSWORD = 'p@55wOrd', CHECK_POLICY = OFF;
ELSE
BEGIN
    ALTER LOGIN [northwind] ENABLE;
    ALTER LOGIN [northwind] WITH PASSWORD = 'p@55wOrd', CHECK_POLICY = OFF;
END;

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'northwind')
    CREATE DATABASE [northwind];

ALTER SERVER ROLE sysadmin ADD MEMBER [northwind];
