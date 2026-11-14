# Restore rehearsal log

Every row is one executed **restore**, produced by
`scripts/backup/restore-rehearsal.ps1`. A scheduled backup that has never been
restored is an assumption, not a recovery plan — `REL-001` / `TEST-010` in
`docs/product/PRD.md` and phase 9 of `docs/product/MVP-ROADMAP.md` require this
evidence.

What `PASS` means:

- the backup was readable and restored into `DentalClinicRestoreTest`;
- `DBCC CHECKDB` reported no errors (unless the drill ran with `-SkipCheckDb`);
- every table that only ever grows restored to between 1 row and its live count
  (the backup is up to 24 hours old, so an exact match is the *wrong* check);
- a real patient still has their invoices, payments and image metadata.

Cadence: **once per release, and at least quarterly.** `-Source offsite` is the
drill that matters — it proves the off-host copy, not the local `.bak`.

| Run (UTC) | Source | Backup file | Snapshot | Operator | Elapsed | Result |
|---|---|---|---|---|---|---|
| 2026-11-12 11:17 | local | `DentalClinic_20261112_101258.bak` | - | USER | 00:00:05 | PASS |
