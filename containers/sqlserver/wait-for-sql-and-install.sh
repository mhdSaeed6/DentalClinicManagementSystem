#!/bin/sh
# ============================================================================
# One-shot initializer: waits for SQL Server to become reachable, then applies
# every *.sql script mounted in /scripts (sorted, so 01-backup-job.sql runs
# first). Registered as the entrypoint of the sql-backup-init sidecar in
# docker-compose.yml. Exits 0 on success so the container stops cleanly.
# ============================================================================

host="${SQLSERVER_HOST:-sqlserver}"
user="${SQLSERVER_USER:-sa}"
password="${SQLSERVER_PASSWORD:-YourStrong@Password1}"

# The 2022 server image ships sqlcmd under mssql-tools18; fall back to the
# legacy path when running against an older image tag.
sqlcmd_helper() {
    if [ -x /opt/mssql-tools18/bin/sqlcmd ]; then
        /opt/mssql-tools18/bin/sqlcmd -S "$host" -U "$user" -P "$password" -C "$@"
    else
        /opt/mssql-tools/bin/sqlcmd -S "$host" -U "$user" -P "$password" "$@"
    fi
}

echo "Waiting for SQL Server at ${host} ..."
attempt=0
max_attempts=60
until sqlcmd_helper -Q "SELECT 1" -b > /dev/null 2>&1
do
    attempt=$((attempt + 1))
    if [ "$attempt" -ge "$max_attempts" ]; then
        echo "ERROR: SQL Server was not reachable after ${max_attempts} attempts." >&2
        exit 1
    fi
    sleep 5
done
echo "SQL Server is reachable."

found_scripts=0
for script in /scripts/*.sql; do
    if [ -e "$script" ]; then
        found_scripts=1
        echo "Applying ${script} ..."
        if ! sqlcmd_helper -i "$script" -b; then
            echo "ERROR: ${script} failed." >&2
            exit 1
        fi
    fi
done

if [ "$found_scripts" -eq 0 ]; then
    echo "No .sql scripts found under /scripts; nothing to do."
fi

echo "SQL Server initialisation complete."
