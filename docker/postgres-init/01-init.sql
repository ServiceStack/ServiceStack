-- Create test user and database
CREATE USER test WITH PASSWORD 'p@55wOrd';
CREATE DATABASE test OWNER test;
GRANT ALL PRIVILEGES ON DATABASE test TO test;

-- Create northwind user and database
CREATE USER northwind WITH PASSWORD 'p@55wOrd';
CREATE DATABASE northwind OWNER northwind;
GRANT ALL PRIVILEGES ON DATABASE northwind TO northwind;
