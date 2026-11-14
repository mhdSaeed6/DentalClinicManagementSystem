#!/bin/sh
# ============================================================================
# Off-box backup: one restic snapshot per run holding every data loss domain.
#
# A .bak file on the same host as the database is host-level protection, not a
# backup. This script copies three things to a restic repository that lives
# somewhere else:
#
#   /data/db      the nightly SQL Server .bak      (read-only bind mount)
#   /data/images  uploaded X-rays / before-after   (named volume)
#   /data/keys    Data Protection key ring         (named volume)
#
# The key ring is included on purpose: without it every session and every
# outstanding antiforgery token becomes invalid, exactly like losing the JWT
# secret. It belongs in the same encrypted scope as the database.
#
# Local retention stays 7 days (the SQL Agent job). This script keeps the long
# history: 30 daily, 8 weekly, 12 monthly.
#
# Required environment:
#   RESTIC_REPOSITORY   e.g. s3:https://s3.us-west-004.backblazeb2.com/bucket/dentalclinic
#   RESTIC_PASSWORD     encrypts the repository; losing it loses every snapshot
#   AWS_ACCESS_KEY_ID / AWS_SECRET_ACCESS_KEY   (or B2_* equivalents)
# ============================================================================

set -eu

: "${RESTIC_REPOSITORY:?RESTIC_REPOSITORY must be set (see .env.example)}"
: "${RESTIC_PASSWORD:?RESTIC_PASSWORD must be set (see .env.example)}"

DB_DIR="${DB_DIR:-/data/db}"
IMAGES_DIR="${IMAGES_DIR:-/data/images}"
KEYS_DIR="${KEYS_DIR:-/data/keys}"
TAG="${RESTIC_TAG:-dentalclinic}"
CHECK_SUBSET="${RESTIC_CHECK_SUBSET:-1%}"

export RESTIC_CACHE_DIR="${RESTIC_CACHE_DIR:-/tmp/restic-cache}"

log() {
    printf '%s offsite-backup: %s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$*"
}

fail() {
    log "FAILED: $*"
    exit 1
}

for dir in "$DB_DIR" "$IMAGES_DIR" "$KEYS_DIR"; do
    [ -d "$dir" ] || fail "$dir is not mounted; refusing to take a partial snapshot"
done

# A snapshot without the database dump is not a database backup. Images and the
# key ring are still worth keeping, so warn loudly instead of aborting.
if ls -1 "$DB_DIR"/*.bak >/dev/null 2>&1; then
    log "newest database backup: $(ls -1t "$DB_DIR"/*.bak | head -n 1)"
else
    log "WARNING: no .bak found in $DB_DIR; snapshotting images and keys only"
fi

# First run against an empty bucket: initialise the repository. A credentials
# problem fails here too, with restic's own message rather than a silent no-op.
if ! restic snapshots --tag "$TAG" >/dev/null 2>&1; then
    log "repository not initialised (or unreachable); running restic init"
    restic init || fail "restic init failed"
fi

log "snapshotting $DB_DIR $IMAGES_DIR $KEYS_DIR"
restic backup "$DB_DIR" "$IMAGES_DIR" "$KEYS_DIR" \
    --tag "$TAG" \
    --host "$(hostname)" \
    --exclude '*.tmp' \
    --verbose \
    || fail "restic backup failed"

log "applying retention (30 daily / 8 weekly / 12 monthly)"
restic forget \
    --tag "$TAG" \
    --keep-daily 30 \
    --keep-weekly 8 \
    --keep-monthly 12 \
    --prune \
    || fail "restic forget failed"

log "verifying repository (read-data-subset=${CHECK_SUBSET})"
restic check --read-data-subset="$CHECK_SUBSET" || fail "restic check failed"

log "done"
