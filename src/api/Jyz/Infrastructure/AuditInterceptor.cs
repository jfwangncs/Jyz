using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using Jyz.Entity;

namespace Jyz.Infrastructure
{
    public class AuditInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        private sealed record PendingEntry(
            EntityEntry Entry,
            string Action,
            bool IsAdded,
            string? ResourceId,   // pre-captured for Modified/Deleted; null for Added (read after save)
            string? OldValue,
            string? NewValue
        );

        private List<PendingEntry>? _pendingEntries;
        private int? _capturedUserId;
        private string? _capturedUserName;
        private string? _capturedIp;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is AppDbContext context)
                CaptureEntries(context);

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is AppDbContext context && _pendingEntries is { Count: > 0 })
            {
                var logs = _pendingEntries.Select(p => new AuditLog
                {
                    UserId = _capturedUserId,
                    UserName = _capturedUserName,
                    Action = p.Action,
                    Resource = p.Entry.Entity.GetType().Name,
                    // Added: read real DB-assigned ID now; Modified/Deleted: use pre-captured value
                    ResourceId = p.IsAdded
                        ? p.Entry.Properties.FirstOrDefault(x => x.Metadata.IsPrimaryKey())?.CurrentValue?.ToString()
                        : p.ResourceId,
                    OldValue = p.OldValue,
                    NewValue = p.NewValue,
                    IP = _capturedIp,
                }).ToList();

                _pendingEntries = null; // clear before next SaveChanges to avoid re-entry

                context.AuditLogs.AddRange(logs);
                await context.SaveChangesAsync(cancellationToken);
            }

            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        private void CaptureEntries(AppDbContext context)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var user = httpContext?.User;

            int? userId = null;
            if (int.TryParse(user?.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedId))
                userId = parsedId;

            _capturedUserId = userId;
            _capturedUserName = user?.FindFirstValue(ClaimTypes.Name);
            _capturedIp = httpContext?.Connection.RemoteIpAddress?.ToString();

            var options = new JsonSerializerOptions { WriteIndented = false };

            _pendingEntries = context.ChangeTracker.Entries()
                .Where(e => e.Entity is not AuditLog && e.Entity is not LoginLog &&
                            e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .Select(entry => new PendingEntry(
                    Entry: entry,
                    Action: entry.State.ToString(),
                    IsAdded: entry.State == EntityState.Added,
                    // Capture PK now for Modified/Deleted (after save, Deleted entries are detached)
                    ResourceId: entry.State != EntityState.Added
                        ? entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString()
                        : null,
                    OldValue: entry.State == EntityState.Added
                        ? null
                        : JsonSerializer.Serialize(
                            entry.Properties.Where(p => !p.Metadata.IsPrimaryKey())
                                            .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue),
                            options),
                    NewValue: entry.State == EntityState.Deleted
                        ? null
                        : JsonSerializer.Serialize(
                            entry.Properties.Where(p => !p.Metadata.IsPrimaryKey())
                                            .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue),
                            options)
                ))
                .ToList();
        }
    }
}
