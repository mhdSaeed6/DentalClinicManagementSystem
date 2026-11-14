#!/bin/sh
# ============================================================================
# Scheduler for the off-box backup.
#
# Deliberately a shell loop, not cron: the restic image is Alpine-based and a
# crontab would add a dependency whose logging goes to a syslog that does not
# exist in the container. The loop:
#
#   * runs one backup immediately on start, so a fresh deploy is protected
#     before the first 03:30 arrives;
#   * then fires once per day at SCHEDULE_HOUR:SCHEDULE_MINUTE (local time, from
#     the container's TZ — 03:30 by default, after the 02:00 SQL Agent job);
#   * retries a failed run up to RETRIES times, 5 minutes apart, because a
#     silently skipped nightly copy is the failure mode this whole slice exists
#     to prevent.
# ============================================================================

set -eu

BACKUP_SCRIPT="${BACKUP_SCRIPT:-/scripts/offsite-backup.sh}"
SCHEDULE_HOUR="${SCHEDULE_HOUR:-3}"
SCHEDULE_MINUTE="${SCHEDULE_MINUTE:-30}"
RETRIES="${RETRIES:-3}"
RETRY_DELAY_SECONDS="${RETRY_DELAY_SECONDS:-300}"

# Strip leading zeros so printf does not treat values as octal.
normalize() {
    value=$(printf '%s' "$1" | sed 's/^0*//')
    [ -n "$value" ] || value=0
    printf '%s' "$value"
}

target_hour=$(normalize "$SCHEDULE_HOUR")
target_minute=$(normalize "$SCHEDULE_MINUTE")
target="$(printf '%02d:%02d' "$target_hour" "$target_minute")"

run_backup() {
    attempt=1
    while :; do
        # Invoked through `sh` rather than executed directly: bind-mounted
        # scripts on Windows hosts do not always carry the executable bit.
        if sh "$BACKUP_SCRIPT"; then
            return 0
        fi
        if [ "$attempt" -ge "$RETRIES" ]; then
            echo "scheduler: backup failed after ${attempt} attempt(s); will try again tomorrow" >&2
            return 1
        fi
        echo "scheduler: backup attempt ${attempt} failed; retrying in ${RETRY_DELAY_SECONDS}s" >&2
        attempt=$((attempt + 1))
        sleep "$RETRY_DELAY_SECONDS"
    done
}

echo "scheduler: running an initial backup now, then daily at ${target} (container local time)"
run_backup || true

last_run_day=""
while :; do
    today=$(date +%Y-%m-%d)
    if [ "$(date +%H:%M)" = "$target" ] && [ "$today" != "$last_run_day" ]; then
        last_run_day="$today"
        echo "scheduler: daily run for ${today}"
        run_backup || true
    fi
    sleep 60
done
