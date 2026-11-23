# Local data schema

WingetUI has no database or network API. Its only persistent application data is the local activity-history JSON file described in [configuration](configuration.md).

## Activity record

The file contains a JSON array. Each valid entry has these persisted fields:

| Field | Type | Meaning |
| --- | --- | --- |
| `Package` | object | Confirmed package snapshot |
| `Upgrade` | boolean | `true` for upgrade, `false` for install |
| `Created` | timestamp | Queue time with offset |
| `Status` | string | Current or terminal operation status |
| `Detail` | string | Human-readable result or recovery detail |
| `Started` | timestamp or null | Execution start time |
| `Finished` | timestamp or null | Terminal time |

The package snapshot contains `Name`, `Id`, `Source`, `Installed`, `Available`, and `CanUpgrade`. Progress phase, percentage, and indeterminate state are transient presentation fields and are not persisted.

## Status values

`Queued`, `Resolving`, and `Running` are unfinished states. `Succeeded`, `Reboot required`, `Failed`, `Cancelled`, `Unknown`, and `Skipped` are terminal states.

On startup, persisted unfinished states become `Unknown` with an interruption message. This is a recovery rule, not evidence that installation succeeded or failed.

## Retention and compatibility

All unfinished records and the newest 100 terminal records are written. Invalid null records and records without a package are ignored during loading.

The format is currently internal and unversioned. Compatible additions should remain optional. Any incompatible change must introduce an explicit format version, migration behavior, rollback limitation, regression coverage, and documentation before release.
