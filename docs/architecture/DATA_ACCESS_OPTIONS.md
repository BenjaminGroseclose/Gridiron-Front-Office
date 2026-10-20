# Data Access Options & SQLite Transactions

> **Status:** **Decided: EF Core with the §2 unit-of-work rules** ([D-024](../DECISIONS.md#d-024-data-access-ef-core-with-the-gfo-unit-of-work)) · 2026-10-13
>
> **➡ For the day-to-day how-to, read [DATA_ACCESS_GUIDE.md](DATA_ACCESS_GUIDE.md).** This doc stays as the background: why transactions fail and which alternatives were considered.
>
> Owner feedback: *"In the past I attempted to implement transactions with SQLite and have not had much success."* and *"I'm interested in exploring options between EF Core with SQLite or other ORM frameworks."*
>
> This doc answers two separate questions:
> 1. **Why do SQLite transactions fail in .NET apps, and what rules prevent it?** This applies whatever library we choose.
> 2. **Which data access library fits GFO best?**

---

## 1. SQLite transactions are reliable. The usage patterns are what break.

SQLite's transaction engine is one of the most heavily tested pieces of software in existence: atomic commit, rollback, crash recovery. When transactions "don't work" in a .NET app, the cause is almost always one of the patterns below, not SQLite itself.

| # | Symptom | Usual cause | GFO rule that prevents it |
|---|---|---|---|
| 1 | `SQLite Error 5: 'database is locked'` on write or commit | Another connection holds a lock: an undisposed `DataReader`, a second `DbContext`, a background task. In SQLite's **default rollback-journal mode, readers block writers.** | **WAL mode** (readers never block the writer). **One writer at a time** (`ISaveWriter`). Always `using`/`await using` readers. |
| 2 | `SqliteConnection does not support nested transactions` | Service A begins a transaction, then calls service B, which begins another | **Only `SaveWriter` begins transactions.** `BeginTransaction` is a banned API everywhere else. If a nested unit is truly needed, use a **savepoint** (Microsoft.Data.Sqlite 6.0+). |
| 3 | `InvalidOperationException: Execute requires the command to have a transaction object…` | A command executed without its `Transaction` set while the connection has a pending transaction. Classic with **Dapper** when `transaction:` isn't passed. | Commands are created only through the unit of work, which always attaches the transaction. |
| 4 | "Half-saved" changes after an error | Work spanned several `SaveChanges` calls or connections, so some parts committed before the failure. Or an exception was caught and the code carried on to `Commit`. | **One command = one transaction = one connection.** Commit only at the end. Any exception rolls back everything. No catch-and-continue inside a unit. |
| 5 | Raw SQL and ORM changes aren't atomic together | Raw ADO used a *different* connection than the ORM context | Raw commands use the unit of work's connection and transaction (with EF: `Database.GetDbConnection()` + `GetDbTransaction()`). |
| 6 | `A second operation was started on this context instance…` or stale and odd data | A long-lived `DbContext` injected into Blazor components. In Blazor Hybrid, "scoped" means app lifetime. Concurrent async calls share it, and a failed `SaveChanges` leaves tracked garbage behind. | **Never inject a DbContext (or connection) into UI.** Use a short-lived context per command. |
| 7 | `database is locked` *immediately*, ignoring `busy_timeout` | Two **deferred** transactions both read, then both try to upgrade to write. SQLite can't resolve this, so it returns BUSY at once. | Write transactions start **IMMEDIATE** (the Microsoft.Data.Sqlite default; `deferred: true` is opt-in). The single writer makes it impossible anyway. |
| 8 | Statements run outside the transaction, or the app hangs (**sqlite-net-pcl**) | A commonly reported pitfall: calling the outer `SQLiteAsyncConnection` *inside* `RunInTransactionAsync` instead of the connection passed to the lambda | If sqlite-net is ever used, only use the provided connection. (Not recommended for GFO, see §4.) |
| 9 | `TransactionScope` surprises | Ambient transactions with async code (missing `TransactionScopeAsyncFlowOption.Enabled`) and provider enlistment limits | **No `TransactionScope`.** Explicit transactions only. |
| 10 | A copied save file is corrupt or missing recent changes | The `.db` file was copied while open in WAL mode. The newest commits live in the `-wal` file. | Export and snapshot with **`VACUUM INTO`**, never by copying a live file. |

> **Answered ([Q-28](../OPEN_QUESTIONS.md#q-28-what-went-wrong-with-sqlite-transactions-before)):** the owner believes the past implementation was wrong rather than SQLite. [DATA_ACCESS_GUIDE.md §2](DATA_ACCESS_GUIDE.md#2-what-probably-went-wrong-before-and-the-fix-for-each) shows the likely mistakes side by side with the fixes.

---

## 2. The GFO unit of work (library-agnostic)

Every mutation goes through one place. Feature code never sees a raw connection or transaction.

```csharp
public interface IUnitOfWork
{
    // Exposes the chosen data access API, bound to THIS connection + transaction:
    //   EF Core  -> LeagueDbContext created on the same connection, Database.UseTransaction(tx)
    //   linq2db  -> DataConnection wrapping the same connection + transaction
    //   Dapper   -> extension methods that always pass the transaction
    SqliteCommand CreateCommand(string sql);          // raw bulk paths; Transaction is pre-attached
    Task SavepointAsync(string name, Func<Task> work); // rare: nested unit that can roll back alone
    void Raise(IDomainEvent e);                        // published only after commit
}

public sealed class SaveWriter(ISaveConnectionFactory connections, IDomainEventDispatcher events,
                               ILogger<SaveWriter> log) : ISaveWriter
{
    private readonly SemaphoreSlim _gate = new(1, 1);   // single writer

    public async Task<T> ExecuteAsync<T>(string operation,
        Func<IUnitOfWork, CancellationToken, Task<T>> work, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        var started = Stopwatch.GetTimestamp();
        try
        {
            await using var connection = await connections.OpenWriteConnectionAsync(ct);
            await using var tx = connection.BeginTransaction();          // BEGIN IMMEDIATE
            var uow = new UnitOfWork(connection, tx);
            try
            {
                var result = await work(uow, ct);
                tx.Commit();                                             // all or nothing
                events.Dispatch(uow.RaisedEvents);                       // after commit only
                log.LogInformation("{Operation} committed in {ElapsedMs} ms",
                    operation, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                tx.Rollback();
                log.LogError(ex, "{Operation} rolled back", operation);
                throw;
            }
        }
        finally { _gate.Release(); }
    }
}
```

### The rules
1. **Only `SaveWriter` begins, commits or rolls back.** `BeginTransaction`, `UseTransaction` and `TransactionScope` are banned outside it (banned-API analyzer plus an architecture test).
2. **One command = one connection = one transaction.** Short-lived.
3. **Feature code receives `IUnitOfWork`**, never a raw connection, context or transaction.
4. **Any exception rolls back the whole command.** No partial commits, no catch-and-continue.
5. **Nested logical steps use savepoints** through `uow.SavepointAsync` (rarely needed).
6. **Long operations are many short commands** tracked by the step journal ([CALENDAR_AND_PHASES.md §6](CALENDAR_AND_PHASES.md#6-resumable-advance-the-step-journal)), never one giant transaction.
7. **Reads** use separate read-only connections (WAL allows this) and never happen directly in UI components.
8. **Backups use `VACUUM INTO`**, never a file copy.

---

## 3. Prove it in M0: transaction torture tests

These run on every build (`build.ps1`, later CI) from M0 onward, and they must pass before any feature work starts.

| Test | Assertion |
|---|---|
| Throw halfway through a command | Nothing from that command is persisted |
| **Kill the process mid-transaction** (a test spawns a helper process that starts a large write and is killed with `Process.Kill`) | On reopen: `PRAGMA integrity_check` passes, there are no partial rows, and the previous data is intact |
| A long read is open while a write commits | No lock errors (WAL) |
| ORM update + raw bulk insert in one command, then throw | Neither is persisted |
| Two commands queued at the same instant (the auto-pick vs. user pick race) | Exactly one succeeds. The other gets a domain error, not an exception. |
| Code calls `BeginTransaction` outside `SaveWriter` | Build or architecture test fails |
| Export during writes | The exported file opens and passes `integrity_check` |

---

## 4. Library options

| | **EF Core 10** | **linq2db** | **Dapper** | **sqlite-net-pcl** | **Raw Microsoft.Data.Sqlite** |
|---|---|---|---|---|---|
| License | MIT | MIT | Apache 2.0 | MIT | MIT |
| Style | Full ORM, change tracking | Type-safe LINQ → SQL, **no change tracking** | Micro-ORM: you write SQL, it maps results | Simple attribute-mapped ORM | ADO.NET by hand |
| Type-safe queries (compile errors on rename) | ✅ | ✅ | ❌ (SQL strings) | Partial (weak LINQ, no joins) | ❌ |
| Migrations | ✅ Built in (auto-generated; SQLite table rebuilds for complex changes) | ❌ Pair with SQL scripts / FluentMigrator | ❌ Pair with SQL scripts / DbUp | Auto "add column" only | ❌ SQL scripts |
| Transaction ergonomics | Implicit per `SaveChanges`. Explicit transactions need care (§1 #5, #6). | Transaction on the `DataConnection`. Everything on it participates automatically. | **Must pass `transaction:` to every call** (§1 #3) | `RunInTransaction` (§1 #8 pitfall) | Explicit and visible |
| Bulk inserts (~26k rows/week) | Adequate with batching; raw ADO for hot paths | ✅ Built-in `BulkCopy` | Loop in one transaction (fast enough) | `InsertAll` (OK) | ✅ Prepared statements (fastest) |
| Strongly typed IDs | Value converters | Mapping schema converters | Type handlers | Awkward | Manual |
| Hidden state / magic | Most (change tracker, lazy behaviors) | Little | None | Little | None |
| Community and docs | Largest | Medium. Docs are thinner. | Very large | Large (Xamarin/MAUI) | — |
| Fit for GFO | **Good**, with the §2 rules | **Very good** for read-heavy + bulk | Good if you like SQL; costly at ~40 tables | **Poor**: too limited for a relational schema with joins | Only for hot paths |

### Migrations without EF
If the choice isn't EF Core, the simplest robust approach for **single-file saves** is hand-written SQL scripts:

```
src/GridironFrontOffice.Infrastructure/Migrations/
  0001_initial.sql
  0002_add_player_grades.sql
  0003_backfill_scouting_sigma.sql
```
- Each script runs in its own transaction (SQLite DDL is transactional). `PRAGMA user_version` records the last applied script.
- On open: take a snapshot → apply the scripts newer than `user_version` → verify.
- Tools that do this for you: **DbUp** (MIT, script runner) or **FluentMigrator** (Apache 2.0, C# migrations). For one SQLite file per save, a ~60-line in-house runner is arguably simpler than either.

### Not recommended
- **Mixing two ORMs** (e.g., EF for writes and Dapper for reads): two mental models and two places for transaction bugs. For a solo project, pick one and use raw ADO only for hot paths.
- **NHibernate:** heavy for this use. **ServiceStack.OrmLite:** commercial licensing terms. **sqlite-net-pcl:** see above.

---

## 5. Recommendation and the spike that decides it

**The unit-of-work rules in §2 matter more than the library.** Most "SQLite transactions don't work" stories are rules 1–6 being broken.

| Rank | Option | Choose it if… |
|---|---|---|
| 1 | **EF Core + the §2 rules** (raw ADO for bulk) | You value built-in migrations and productivity for ~40 tables, and the spike shows the rules make transactions boring |
| 2 | **linq2db + SQL-script migrations** | Your past pain came from EF's hidden state, or you want explicit, change-tracker-free data access with type-safe queries and fast bulk inserts |
| 3 | **Dapper + SQL-script migrations** | You want to see every SQL statement and accept more hand-written SQL plus string queries that don't fail at compile time on renames |

> **Outcome:** the owner chose **rank 1, EF Core** ([D-024](../DECISIONS.md#d-024-data-access-ef-core-with-the-gfo-unit-of-work)). Spike S0a below is now an **EF Core proof**: implement the four operations in EF Core only, run the torture tests, and confirm the bulk timing. linq2db stays the documented fallback if bulk timings or ergonomics disappoint.

### Spike S0a (M0): prove EF Core + the rules with real code
Implement these four operations in **EF Core** behind the §2 unit of work (the original plan also compared linq2db):

| # | Operation | Exercises |
|---|---|---|
| a | **Sign a free agent:** set `Player.TeamID`, insert a `Contract` + 4 `ContractYears`, append to `Transactions`, check the cap → roll back if over | Multi-table atomicity, a domain rollback |
| b | **Persist game results:** a season in one command (~26k `PlayerGameStats`, 285 compressed `GamePlayLogs`), a week (~1.4k rows, 16 logs), plus standings | Bulk performance (target < 1 s), mixed bulk + ORM |
| c | **Roster read model:** players + current cap hit + injury status for one team | Query ergonomics, projection speed |
| d | **Migration:** add a column with a default and backfill it | The migration story on a populated save |

Run the §3 torture tests. **Exit:** all torture tests pass, a season's bulk persist takes < 1 s, and the migration on a populated save works. Note the results in [D-024](../DECISIONS.md#d-024-data-access-ef-core-with-the-gfo-unit-of-work).

> **Outcome (2026-10-15): passed.** Season persist 665 ms, week 27 ms, migration + backfill 45 ms, all torture checks green. EF Core stays, and linq2db isn't needed. *(The original wording said "a week: ~26k rows". That was a season. A week is about 1,440 rows.)* Full results: [M0_FOUNDATIONS.md WP2](../milestones/M0_FOUNDATIONS.md#wp2--spike-s0a-ef-core-proof-sm-throwaway-code--done-2026-10-15).
