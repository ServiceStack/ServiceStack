#!/bin/bash
set -e

# Start SQL Server in background
/opt/mssql/bin/sqlservr &
sqlserver_pid=$!
trap 'kill "$sqlserver_pid" 2>/dev/null || true' EXIT
trap 'exit 143' TERM
trap 'exit 130' INT

# Wait for SQL Server to be ready
echo "[entrypoint] Waiting for SQL Server to start..."
ready=false
for i in $(seq 1 30); do
    if /opt/mssql-tools18/bin/sqlcmd -b -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" -C -Q "SELECT 1" > /dev/null 2>&1; then
        echo "[entrypoint] SQL Server is ready."
        ready=true
        break
    fi
    echo "[entrypoint] Attempt $i/30 - not ready yet, waiting 2s..."
    sleep 2
done

if [ "$ready" != true ]; then
    echo "[entrypoint] SQL Server did not become ready." >&2
    exit 1
fi

# Run init scripts in order
INIT_DIR="/docker-entrypoint-initdb.d"
if [ -d "$INIT_DIR" ]; then
    for f in "$INIT_DIR"/*.sql; do
        [ -f "$f" ] || continue
        echo "[entrypoint] Running $f ..."
        /opt/mssql-tools18/bin/sqlcmd -b -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" -C -i "$f"
        echo "[entrypoint] Done: $f"
    done
fi

# Wait for SQL Server process to exit
wait "$sqlserver_pid"
