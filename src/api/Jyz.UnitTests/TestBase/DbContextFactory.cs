using JfYu.Data.Context;
using Jyz.Entity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Jyz.UnitTests.TestBase
{
    public static class DbContextFactory
    {
        /// <summary>
        /// Creates an isolated SQLite in-memory AppDbContext + ReadonlyDBContext pair for unit tests.
        /// SQLite (unlike EF InMemory) supports relational features such as filtered <c>Include()</c>,
        /// transactions and FK constraints — keeping unit tests close to production behavior.
        /// The shared <see cref="SqliteConnection"/> is owned by the returned <see cref="AppDbContext"/>
        /// and is closed automatically when the context is disposed.
        /// </summary>
        /// <param name="dbName">Unused, retained for backward compatibility.</param>
        /// <param name="interceptors">Optional EF interceptors (e.g. <c>AuditInterceptor</c>) to register.</param>
        public static (AppDbContext context, ReadonlyDBContext<AppDbContext> readonlyContext) CreateInMemory(
            string? dbName = null,
            IEnumerable<IInterceptor>? interceptors = null)
        {
            // ":memory:" creates a fresh per-connection database; keep the connection open
            // for the lifetime of the context so the schema and data persist across queries.
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection, b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
                .Options;

            var context = new AppDbContext(options, interceptors ?? []);
            context.Database.EnsureCreated();

            // AppDbContext.OnModelCreating seeds a default admin User/Role via HasData() so it also
            // applies under EnsureCreated() (no migrations involved). Tests assume an empty schema
            // and manage their own fixture rows/ids, so strip that seed data back out here.
            context.Database.ExecuteSqlRaw("DELETE FROM RoleUser; DELETE FROM Users; DELETE FROM Roles;");

            // Tie the connection lifetime to the context so callers don't need to dispose it explicitly.
            context.Database.GetDbConnection().StateChange += (_, e) =>
            {
                if (e.CurrentState == System.Data.ConnectionState.Closed)
                    connection.Dispose();
            };

            var readonlyContext = new ReadonlyDBContext<AppDbContext>(context);
            return (context, readonlyContext);
        }
    }
}
