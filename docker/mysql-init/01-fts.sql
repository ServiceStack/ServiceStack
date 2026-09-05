-- Ensure test user and database exist with correct password
CREATE DATABASE IF NOT EXISTS test;
CREATE USER IF NOT EXISTS 'test'@'%' IDENTIFIED BY 'p@55wOrd';
GRANT ALL PRIVILEGES ON test.* TO 'test'@'%';

-- Create northwind user and database
CREATE DATABASE IF NOT EXISTS northwind;
CREATE USER IF NOT EXISTS 'northwind'@'%' IDENTIFIED BY 'p@55wOrd';
GRANT ALL PRIVILEGES ON northwind.* TO 'northwind'@'%';

FLUSH PRIVILEGES;

-- FTS health check table
USE test;
CREATE TABLE IF NOT EXISTS fts_health (
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    content TEXT NOT NULL,
    FULLTEXT KEY ix_fts_health_content (content)
) ENGINE=InnoDB;
