-- Example SQL Server init script

DECLARE @DatabaseName NVARCHAR(128) = N'{DatabaseName}';
DECLARE @AdminLogin NVARCHAR(128) = N'{AdminLogin}';
DECLARE @AppLogin NVARCHAR(128) = N'{AppLogin}';
DECLARE @AdminPassword NVARCHAR(128) = '{AdminPassword}';
DECLARE @AppPassword NVARCHAR(128) = '{AppPassword}';

IF NOT EXISTS (SELECT * FROM [sys].[databases] WHERE name = @DatabaseName)
BEGIN
  EXEC('CREATE DATABASE ' + @DatabaseName);
END;
GO

USE Master;

-- Create logins with the provided passwords
EXEC('CREATE LOGIN ' + @AdminLogin + ' WITH PASSWORD = ''' + @AdminPassword + '''');
EXEC('CREATE LOGIN ' + @AppLogin + ' WITH PASSWORD = ''' + @AppPassword + '''');

USE ' + @DatabaseName;
-- Create users for the logins
EXEC('CREATE USER ' + @AdminLogin + ' FROM LOGIN ' + @AdminLogin);
EXEC('EXEC sp_addrolemember ''db_owner'', ''' + @AdminLogin + '''');
EXEC('CREATE USER ' + @AppLogin + ' FROM LOGIN ' + @AppLogin);
EXEC('EXEC sp_addrolemember ''db_datareader'', ''' + @AppLogin + '''');
EXEC('EXEC sp_addrolemember ''db_datawriter'', ''' + @AppLogin + '''');