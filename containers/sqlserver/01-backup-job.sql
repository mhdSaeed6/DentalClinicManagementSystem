-- ============================================================================
-- DentalClinic daily full backup job (AUDIT.md item 8, §6.x ops hardening).
--
-- Applied automatically by the sql-backup-init one-shot sidecar in
-- docker-compose.yml; safe to re-run: the job is deleted and recreated, and
-- the schedule is re-registered with the job each time.
--
-- Output: /var/opt/mssql/backups/DentalClinic_<yyyyMMdd_HHmmss>.bak
--         (bind-mounted to ./containers/sqlserver-backups on the host)
-- Retention: 7 days, enforced by xp_delete_file.
-- ============================================================================

USE [msdb];
GO

DECLARE @jobName nvarchar(128) = N'DentalClinic Daily Full Backup';

-- If a previous version of the job exists, remove it first so the script is
-- idempotent and always ends up with one clean definition.
IF EXISTS (SELECT 1 FROM msdb.dbo.sysjobs WHERE name = @jobName)
BEGIN
    EXECUTE msdb.dbo.sp_delete_job
        @job_name = @jobName,
        @delete_unused_schedule = 1;
END
GO

BEGIN
    DECLARE @jobId binary(16);

    EXECUTE msdb.dbo.sp_add_job
        @job_name = N'DentalClinic Daily Full Backup',
        @description = N'Full backup of the DentalClinic database at 02:00 with CHECKSUM and COMPRESSION.',
        @enabled = 1,
        @job_id = @jobId OUTPUT;

    -- 1. Take the backup with a timestamped file name.
    EXECUTE msdb.dbo.sp_add_jobstep
        @job_id = @jobId,
        @step_id = 1,
        @step_name = N'Full backup',
        @database_name = N'msdb',
        @subsystem = N'TSQL',
        @command = N'DECLARE @stamp nvarchar(15) = FORMAT(GETDATE(), ''yyyyMMdd_HHmmss'');
                     DECLARE @path nvarchar(260) = N''/var/opt/mssql/backups/DentalClinic_'' + @stamp + N''.bak'';
                     BACKUP DATABASE [DentalClinic]
                       TO DISK = @path
                       WITH CHECKSUM, COMPRESSION, STATS = 25;',
        @retry_attempts = 1;

    -- 2. Delete backups older than the 7-day retention window.
    EXECUTE msdb.dbo.sp_add_jobstep
        @job_id = @jobId,
        @step_id = 2,
        @step_name = N'Apply retention (7 days)',
        @database_name = N'msdb',
        @subsystem = N'TSQL',
        @command = N'DECLARE @cutoff datetime = DATEADD(day, -7, GETDATE());
                     EXECUTE master.dbo.xp_delete_file
                       0,                       -- file type: 0 = backup files (bak)
                       N''/var/opt/mssql/backups'',
                       N''bak'',
                       @cutoff;';

    EXECUTE msdb.dbo.sp_add_jobschedule
        @job_id = @jobId,
        @name = N'Daily at 02:00',
        @enabled = 1,
        @freq_type = 4,                 -- daily
        @freq_interval = 1,
        @freq_subday_type = 1,
        @active_start_date = 20260101,
        @active_start_time = 020000;    -- 02:00 nightly

    EXECUTE msdb.dbo.sp_add_jobserver
        @job_id = @jobId,
        @server_name = N'(local)';
END
GO
