#!/bin/bash
set -e

# Start SQL Server in background
/opt/mssql/bin/sqlservr &
MSSQL_PID=$!

# Wait for SQL Server to be ready
echo "[entrypoint] Waiting for SQL Server to start..."
for i in $(seq 1 30); do
    if /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" -C -Q "SELECT 1" > /dev/null 2>&1; then
        echo "[entrypoint] SQL Server is ready."
        break
    fi
    echo "[entrypoint] Attempt $i/30 - not ready yet, waiting 2s..."
    sleep 2
done

# Run init scripts in order
INIT_DIR="/docker-entrypoint-initdb.d"
if [ -d "$INIT_DIR" ]; then
    for f in $(ls "$INIT_DIR"/*.sql 2>/dev/null | sort); do
        echo "[entrypoint] Running $f ..."
        /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" -C -i "$f"
        echo "[entrypoint] Done: $f"
    done
fi

# Wait for SQL Server process to exit
wait $MSSQL_PID
