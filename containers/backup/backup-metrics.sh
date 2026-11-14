#!/bin/sh
# ============================================================================
# Exports the state of the SQL Server full-backup folder as Prometheus metrics.
#
# The SQL Agent job (containers/sqlserver/01-backup-job.sql) writes
# DentalClinic_<timestamp>.bak into a bind-mounted host folder. Nothing else
# knows whether that job actually ran, so a silently failing job produced no
# signal at all. This sidecar turns the folder contents into scraped metrics;
# containers/prometheus/alerts.yml turns them into alerts.
#
# Output goes through the node-exporter textfile collector. The file is written
# to a temporary name and moved into place, so a scrape never reads a
# half-written file.
# ============================================================================

set -eu

BACKUP_DIR="${BACKUP_DIR:-/var/opt/mssql/backups}"
TEXTFILE_PATH="${TEXTFILE_PATH:-/var/lib/node_exporter/textfile}"
INTERVAL_SECONDS="${INTERVAL_SECONDS:-60}"
FILE="${TEXTFILE_PATH}/dentalclinic_backup.prom"

mkdir -p "$TEXTFILE_PATH"

write_metrics() {
    # Newest .bak first; the glob stays literal (and ls fails quietly) when the
    # folder is empty, which is the "backup missing" case we want to report.
    newest=$(ls -1t "$BACKUP_DIR"/*.bak 2>/dev/null | head -n 1 || true)

    if [ -z "${newest:-}" ]; then
        present=0
        last=0
        size=0
        count=0
    else
        present=1
        last=$(date -r "$newest" +%s 2>/dev/null || stat -c %Y "$newest")
        size=$(wc -c < "$newest" | tr -d ' ')
        count=$(ls -1 "$BACKUP_DIR"/*.bak 2>/dev/null | wc -l | tr -d ' ')
    fi

    tmp="${FILE}.tmp"
    {
        printf '# HELP dentalclinic_backup_present 1 when at least one .bak file exists.\n'
        printf '# TYPE dentalclinic_backup_present gauge\n'
        printf 'dentalclinic_backup_present %s\n' "$present"

        printf '# HELP dentalclinic_backup_last_success_timestamp_seconds Unix time of the newest .bak file (0 when none exists).\n'
        printf '# TYPE dentalclinic_backup_last_success_timestamp_seconds gauge\n'
        printf 'dentalclinic_backup_last_success_timestamp_seconds %s\n' "$last"

        printf '# HELP dentalclinic_backup_size_bytes Size of the newest .bak file in bytes.\n'
        printf '# TYPE dentalclinic_backup_size_bytes gauge\n'
        printf 'dentalclinic_backup_size_bytes %s\n' "$size"

        printf '# HELP dentalclinic_backup_file_count Number of .bak files currently in the folder.\n'
        printf '# TYPE dentalclinic_backup_file_count gauge\n'
        printf 'dentalclinic_backup_file_count %s\n' "$count"
    } > "$tmp"

    # 0644: the exporter container runs as an unprivileged user and only reads.
    chmod 0644 "$tmp"
    mv "$tmp" "$FILE"
}

echo "backup-metrics: watching ${BACKUP_DIR} every ${INTERVAL_SECONDS}s -> ${FILE}"
while :; do
    write_metrics || echo "backup-metrics: metric generation failed" >&2
    sleep "$INTERVAL_SECONDS"
done
