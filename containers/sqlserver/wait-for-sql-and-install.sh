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

# The job registration below calls sp_add_jobserver, which fails with
# "Cannot perform this operation while SQLServerAgent is starting" (Msg 14258)
# when the agent is still coming up. That failure left the job registered but
# with no job server, i.e. a nightly backup that never runs. Wait for the agent
# first; if it is disabled we still apply the scripts and let the SQL raise.
echo "Waiting for SQL Server Agent ..."
agent_attempt=0
agent_max_attempts=24
while :; do
    agent_running=$(sqlcmd_helper -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.dm_server_services WHERE servicename LIKE N'SQL Server Agent%' AND status = 4;" 2>/dev/null | tr -d '[:space:]')
    if [ "${agent_running:-0}" = "1" ]; then
        echo "SQL Server Agent is running."
        break
    fi
    agent_attempt=$((agent_attempt + 1))
    if [ "$agent_attempt" -ge "$agent_max_attempts" ]; then
        echo "WARNING: SQL Server Agent is not running after ${agent_max_attempts} attempts; applying scripts anyway." >&2
        break
    fi
    sleep 5
done

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
