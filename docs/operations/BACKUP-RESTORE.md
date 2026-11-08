# Backup and restore runbook

Status: **backup job implemented; off-box copy and a rehearsed restore are still open ops tasks.**
Last verified against: branch `feature/backlog-completion` (2026-11-08).

This runbook covers the DentalClinic SQL Server database that runs as the
`sqlserver` service in `docker-compose.yml`. It is deliberately ops-first: the
application itself does not take backups.

## 1. What runs today

| Piece | Where | Behaviour |
|---|---|---|
| SQL Agent | `sqlserver` service (`MSSQL_AGENT_ENABLED=true`) | Hosts the backup job. |
| Job registration | `containers/sqlserver/01-backup-job.sql` | Deletes and recreates `DentalClinic Daily Full Backup`, so re-running is idempotent. |
| Init sidecar | `sql-backup-init` + `containers/sqlserver/wait-for-sql-and-install.sh` | Waits for SQL Server, then applies every `*.sql` in `/scripts`. Exits 0 and stays stopped. |
| Schedule | Job step | Daily at **02:00**, full backup with `CHECKSUM, COMPRESSION`. |
| Output | `/var/opt/mssql/backups/DentalClinic_<yyyyMMdd_HHmmss>.bak` | Bind-mounted to `containers/sqlserver-backups/` on the host. |
| Retention | Job step 2 | `xp_delete_file` removes `.bak` files older than **7 days**. |
| Volume | `sqlserver-data` | Holds the live database files; not a backup. |

Bring the job up on a fresh machine:

```powershell
docker compose up -d sqlserver sql-backup-init
docker compose logs sql-backup-init          # expect "SQL Server initialisation complete."
```

Confirm the job exists and has run:

```powershell
docker exec -it dentalclinic-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Password1" -C -Q "EXEC msdb.dbo.sp_help_job @job_name = N'DentalClinic Daily Full Backup';"
```

Force one run instead of waiting for 02:00:

```powershell
docker exec -it dentalclinic-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Password1" -C -Q "EXEC msdb.dbo.sp_start_job @job_name = N'DentalClinic Daily Full Backup';"
```

## 2. Restore procedure (rehearse this)

A restore is only proven when it has actually been performed. Use a scratch
database name for the rehearsal so the live database is never touched.

1. **Stop writers** (API and Web) so nothing reconnects mid-restore:
   ```powershell
   docker compose stop dentalclinic-api dentalclinic-web
   ```
2. **Pick the backup** from `containers/sqlserver-backups/` (newest first).
3. **Restore to a scratch database** inside the SQL container:
   ```sql
   RESTORE DATABASE [DentalClinicRestoreTest]
     FROM DISK = N'/var/opt/mssql/backups/DentalClinic_YYYYMMDD_HHMMSS.bak'
     WITH MOVE N'DentalClinic'     TO N'/var/opt/mssql/data/DentalClinicRestoreTest.mdf',
          MOVE N'DentalClinic_log' TO N'/var/opt/mssql/data/DentalClinicRestoreTest_log.ldf',
          REPLACE, RECOVERY;
   ```
   Run it through `sqlcmd`:
   ```powershell
   docker exec -it dentalclinic-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Password1" -C -Q "<the RESTORE statement>"
   ```
4. **Verify records and relationships** (`TEST-010`): compare counts against the
   live database before the restore and spot-check a patient with invoices,
   payments, appointments, treatment records, images metadata, lab cases, and
   import jobs:
   ```sql
   SELECT COUNT(*) FROM [DentalClinicRestoreTest].dbo.Patients;
   SELECT COUNT(*) FROM [DentalClinicRestoreTest].dbo.Invoices;
   SELECT COUNT(*) FROM [DentalClinicRestoreTest].dbo.Payments;
   SELECT COUNT(*) FROM [DentalClinicRestoreTest].dbo.PatientImages;
   SELECT COUNT(*) FROM [DentalClinicRestoreTest].dbo.LabCases;
   SELECT COUNT(*) FROM [DentalClinicRestoreTest].dbo.ImportJobs;
   ```
5. **Start the apps against the restored copy** by pointing
   `ConnectionStrings__DefaultConnection` at `DentalClinicRestoreTest` and
   running one smoke pass: sign in, open a patient, open an invoice, open the
   dashboard.
6. **Record the rehearsal** (date, backup file, operator, result) and then drop
   the scratch database:
   ```sql
   DROP DATABASE [DentalClinicRestoreTest];
   ```
7. **Restart production services**:
   ```powershell
   docker compose start dentalclinic-api dentalclinic-web
   ```

For a real disaster recovery, replace step 3 with `RESTORE ... WITH REPLACE`
against `DentalClinic` after stopping the API and Web containers.

## 3. What is still missing

1. **Off-box copy.** The `.bak` files live on the same host as the database.
   A host loss loses both. Copy `containers/sqlserver-backups/*.bak` to
   S3/Backblaze (or another host) on a schedule, and never commit credentials
   for that target. Until this exists, treat the current setup as
   *host-level* protection only.
2. **Staleness alerting.** A backup job can fail silently. Alert when the
   newest `.bak` is older than 26 hours (Prometheus textfile exporter, a cron
   check, or the monitoring stack already in compose).
3. **Rehearsal evidence.** Run §2 at least once per release and record the
   result; `PRD.md` `REL-001` / `TEST-010` and `MVP-ROADMAP.md` phase 9 both
   require a *tested* restore, not merely a scheduled backup.
4. **Image storage.** `dentalclinic-images` is a Docker volume. Back it up
   separately (or move it to object storage) if losing uploaded X-rays and
   photos is unacceptable. The database holds only the metadata.