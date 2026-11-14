# Backup and restore runbook

Status: **backup, off-box copy, staleness alerting and a scripted restore
rehearsal are implemented.** Point-in-time recovery (log backups) is not.
Last verified against: branch `main` @ `eba7f28` + Phase D2 (2026-11-12).

This runbook covers the DentalClinic SQL Server database that runs as the
`sqlserver` service, plus the two Docker volumes that hold state the database
does not: clinical images and the Data Protection key ring. Backups are an ops
concern by design — the application process is the wrong failure domain for
them.

## 0. Recovery objectives and scope

| Field | Value |
|---|---|
| **RPO** (data you can lose) | **24 hours.** One full backup per night; there are no transaction-log backups, so a failure at 23:00 loses up to a day of work. |
| **RTO** (time to be back) | **~1 hour** for a ~1 GB database: provision host, pull the snapshot, restore, start the stack. Rehearsed elapsed time is in `restore-rehearsal-log.md`. |
| Covered by a backup | `DentalClinic` database, `dentalclinic-images` volume (X-rays, before/after photos), `dentalclinic-keys` volume (Data Protection ring). |
| **Not** covered | `containers/sqlserver-backups/` itself (host-local, deliberately), Seq logs, Grafana dashboards, `.env` secrets, the container images (rebuildable from git). |
| Verified by | `scripts/backup/restore-rehearsal.ps1` + `docs/operations/restore-rehearsal-log.md` |

## 1. What runs today

| Piece | Where | Behaviour |
|---|---|---|
| SQL Agent | `sqlserver` service (`MSSQL_AGENT_ENABLED=true`) | Hosts the nightly backup job. |
| Job registration | `containers/sqlserver/01-backup-job.sql` | Deletes and recreates `DentalClinic Daily Full Backup`, so re-running is idempotent. |
| Init sidecar | `sql-backup-init` + `containers/sqlserver/wait-for-sql-and-install.sh` | Waits for SQL Server, then applies every `*.sql` in `/scripts`. Exits 0 and stays stopped. |
| Schedule | Job step | Daily at **02:00**, full backup with `CHECKSUM, COMPRESSION`. |
| Output | `/var/opt/mssql/backups/DentalClinic_<yyyyMMdd_HHmmss>.bak` | Bind-mounted to `containers/sqlserver-backups/` on the host. |
| Local retention | Job step 2 | `xp_delete_file` removes `.bak` files older than **7 days**. |
| Off-box copy | `backup-offsite` service (`restic`) | Runs at **03:30**, after the SQL job, and once on container start. Snapshots the database dump, `dentalclinic-images` and `dentalclinic-keys` into an S3-compatible restic repository. Retention **30 daily / 8 weekly / 12 monthly**, then `restic check` with a 1% read-data subset. |
| Backup metrics | `backup-metrics` service + `node-exporter` | Publishes `dentalclinic_backup_last_success_timestamp_seconds`, `dentalclinic_backup_size_bytes`, `dentalclinic_backup_file_count`, `dentalclinic_backup_present` through the node-exporter textfile collector. |
| Alerting | `containers/prometheus/alerts.yml` | `DentalClinicBackupMissing`, `DentalClinicBackupStale` (> 26 h), `DentalClinicBackupMetricsDown`. Fires in the Prometheus UI (`/alerts`) and in Grafana. No Alertmanager, so there is no page — the ops UIs are loopback-only. |

Bring the job up on a fresh machine:

```powershell
docker compose -f docker-compose.prod.yml up -d sqlserver sql-backup-init
docker compose -f docker-compose.prod.yml logs sql-backup-init   # expect "SQL Server initialisation complete."
```

> The initialiser waits for **SQL Server Agent** before applying the job scripts.
> `sp_add_jobserver` raises Msg 14258 while the agent is still starting, and a
> job registered without a job server is *created but never runs* — the nightly
> backup silently never happens. `01-backup-job.sql` therefore retries the
> association and asserts `msdb.dbo.sysjobservers` has a row, failing the
> initialiser (exit 1) instead of leaving a dead job behind. If
> `docker compose logs sql-backup-init` shows a non-zero exit, do not assume the
> backup exists — check with `sp_help_job` before trusting it.

> `containers/sqlserver-backups/` and `containers/sqlserver-restore/` are
> git-ignored. Never commit a `.bak`: it contains real patient data.

## 2. Confirm backups are actually healthy

Three independent things can fail silently, so check all three.

**a) The job produced a file.** Force a run instead of waiting for 02:00:

```powershell
docker exec -it dentalclinic-sql /opt/mssql-tools18/bin/sqlcmd `
  -S localhost -U sa -P "$env:SA_PASSWORD" -C `
  -Q "EXEC msdb.dbo.sp_start_job @job_name = N'DentalClinic Daily Full Backup';"
```

**b) The metric sees it.** Within a minute of the file appearing:

```powershell
docker exec dentalclinic-sql /bin/sh -c "ls -1t /var/opt/mssql/backups/*.bak | head -n 1"
curl http://localhost:9100/metrics | Select-String dentalclinic_backup
```

Freshness must be less than 26 hours, `dentalclinic_backup_present` must be `1`.
If the metric is stale, look at `docker compose logs backup-metrics`.

**c) The offsite snapshot exists and verifies.**

```powershell
docker compose -f docker-compose.prod.yml run --rm backup-offsite restic snapshots
docker compose -f docker-compose.prod.yml run --rm backup-offsite restic check
docker compose -f docker-compose.prod.yml logs backup-offsite
```

A `restic check` failure means the repository, not the database, is in trouble —
re-run immediately, then look at the bucket's object integrity.

## 3. Rehearse a restore (do this every release)

The drill is scripted, restores into a scratch database and never touches the
live one, so the API and Web containers can keep running:

```powershell
# prove the off-host copy (recommended: this is the one that matters)
pwsh ./scripts/backup/restore-rehearsal.ps1 -Source offsite

# prove last night's local .bak only
pwsh ./scripts/backup/restore-rehearsal.ps1 -Source local
```

The script resolves the backup, restores it as `DentalClinicRestoreTest`, runs
`DBCC CHECKDB`, compares row counts against the live database with rules that
account for the backup being up to 24 hours old, spot-checks a patient across
invoices/payments/images, drops the scratch database and appends a row to
[`restore-rehearsal-log.md`](restore-rehearsal-log.md).

Useful switches: `-SkipCheckDb` (fast drill on a large database),
`-KeepScratchDatabase` (inspect the result), `-BackupFile <path>` (rehearse one
specific file), `-Source offsite` (pull the newest snapshot to
`containers/sqlserver-restore/` first).

**A drill that fails is a production incident in slow motion.** Do not wait for
the next night's backup: fix it while you still have a working database.

## 4. Appendix — manual restore (when the script cannot run)

Use a scratch database name so the live database is never touched.

1. **Stop writers** (API and Web) so nothing reconnects mid-restore:
   ```powershell
   docker compose -f docker-compose.prod.yml stop dentalclinic-api dentalclinic-web
   ```
2. **Pick the backup** from `containers/sqlserver-backups/` (newest first), or
   pull the newest snapshot:
   ```powershell
   docker compose -f docker-compose.prod.yml run --rm --no-deps `
     -v "${PWD}/containers/sqlserver-restore:/restore" `
     --entrypoint /bin/sh backup-offsite `
     -c "restic restore latest --target /restore --include /data/db"
   ```
3. **Restore** (adjust the `FROM DISK` path to where the file is visible inside
   the SQL container: `/var/opt/mssql/backups/...` or
   `/var/opt/mssql/restore/data/db/...`):
   ```sql
   RESTORE DATABASE [DentalClinicRestoreTest]
     FROM DISK = N'/var/opt/mssql/backups/DentalClinic_YYYYMMDD_HHMMSS.bak'
     WITH MOVE N'DentalClinic'     TO N'/var/opt/mssql/data/DentalClinicRestoreTest.mdf',
          MOVE N'DentalClinic_log' TO N'/var/opt/mssql/data/DentalClinicRestoreTest_log.ldf',
          REPLACE, RECOVERY;
   ```
   Get the logical names from `RESTORE FILELISTONLY FROM DISK = N'...'` if the
   database was ever renamed.
4. **Verify** integrity and rows:
   ```sql
   DBCC CHECKDB ([DentalClinicRestoreTest]) WITH NO_INFOMSGS;
   SELECT COUNT(*) FROM [DentalClinicRestoreTest].dbo.Patients;
   SELECT COUNT(*) FROM [DentalClinicRestoreTest].dbo.Invoices;
   SELECT COUNT(*) FROM [DentalClinicRestoreTest].dbo.Payments;
   SELECT COUNT(*) FROM [DentalClinicRestoreTest].dbo.PatientImages;
   ```
5. **Record it** in `restore-rehearsal-log.md`, then drop the scratch database:
   ```sql
   ALTER DATABASE [DentalClinicRestoreTest] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
   DROP DATABASE [DentalClinicRestoreTest];
   ```
6. **Restart production services**:
   ```powershell
   docker compose -f docker-compose.prod.yml start dentalclinic-api dentalclinic-web
   ```

## 5. Real disaster recovery

Host lost: provision a new host, restore `.env` from the password manager, pull
the repo, then:

```powershell
docker compose -f docker-compose.prod.yml up -d sqlserver
docker compose -f docker-compose.prod.yml up -d sql-backup-init
# restore the newest offsite snapshot for database AND volumes:
docker compose -f docker-compose.prod.yml run --rm --no-deps `
  -v "${PWD}/containers/sqlserver-restore:/restore" `
  --entrypoint /bin/sh backup-offsite `
  -c "restic restore latest --target /restore"
```

Then `RESTORE ... WITH REPLACE` the database (stopping API/Web first), copy
`/restore/data/images/*` back into the `dentalclinic-images` volume and
`/restore/data/keys/*` into `dentalclinic-keys` (for example with a one-shot
`alpine` container mounting both), and finally `docker compose up -d`.

The key ring matters: restoring the database without it works, but every user is
logged out and outstanding antiforgery tokens are invalid. Restoring it keeps
sessions alive.

## 6. What is still missing

1. **Point-in-time recovery.** No transaction-log backups, so RPO is 24 hours.
   If the clinic needs a 15-minute RPO, add log backups + a restore-to-time
   procedure (this also complicates the bucket layout).
2. **`.bak` files are unencrypted on the host.** They are protected by file
   permissions only; the offsite restic repository *is* encrypted. Treat host
   access as data access.
3. **No paging.** Alerts appear in Prometheus/Grafana but nobody is notified
   out of band; add Alertmanager with a receiver if backups must page someone.
4. **Offsite restore is rehearsed, not automated.** The drill proves a snapshot
   restores; nothing verifies it on a schedule. Consider a monthly CI job once a
   second environment exists.
