using System.Data;
using JobPortalApi.Models;
using JobPortalApi.Models.Enums;
using JobPortalApi.Services.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace JobPortalApi.Services.Payments;

/// <summary>
/// Thao tác trên credit ledger theo kiểu append-only. Khi được gọi trong
/// transaction của business action, method chỉ thêm entry; caller sẽ SaveChanges
/// và commit cùng business data để không mất credit khi action thất bại.
/// </summary>
public class CreditLedgerService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;

    public CreditLedgerService(ApplicationDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    public async Task<bool> TryConsumeAsync(
        Guid userId,
        CreditType creditType,
        int quantity,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        var key = NormalizeKey(idempotencyKey);
        return await ExecuteInTransactionAsync(IsolationLevel.Serializable, async () =>
        {
            var alreadyConsumed = await _context.CreditLedgers
                .AnyAsync(entry => entry.IdempotencyKey == key ||
                    (entry.IdempotencyKey != null && entry.IdempotencyKey.StartsWith(key + ":")), cancellationToken);
            if (alreadyConsumed) return true;

            var now = DateTime.UtcNow;
            var entries = await _context.CreditLedgers
                .AsNoTracking()
                .Where(entry => entry.UserId == userId &&
                    entry.CreditType == creditType &&
                    (!entry.ExpiresAt.HasValue || entry.ExpiresAt > now))
                .ToListAsync(cancellationToken);

            var lots = entries
                .GroupBy(entry => entry.ExpiresAt)
                .Select(group => new
                {
                    ExpiresAt = group.Key,
                    Available = group.Sum(entry => (long)entry.Quantity)
                })
                .Where(lot => lot.Available > 0)
                .OrderBy(lot => lot.ExpiresAt ?? DateTime.MaxValue)
                .ToList();

            var remaining = quantity;
            var allocations = new List<(DateTime? ExpiresAt, int Quantity)>();
            foreach (var lot in lots)
            {
                if (remaining == 0) break;
                var allocated = (int)Math.Min(remaining, lot.Available);
                allocations.Add((lot.ExpiresAt, allocated));
                remaining -= allocated;
            }

            if (remaining > 0) return false;

            for (var index = 0; index < allocations.Count; index++)
            {
                var allocation = allocations[index];
                _context.CreditLedgers.Add(new CreditLedger
                {
                    UserId = userId,
                    CreditType = creditType,
                    EntryType = CreditLedgerEntryType.Debit,
                    Quantity = -allocation.Quantity,
                    ExpiresAt = allocation.ExpiresAt,
                    IdempotencyKey = allocations.Count == 1 ? key : $"{key}:{index}",
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }, cancellationToken);
    }

    public async Task<bool> TryRefundAsync(
        Guid userId,
        CreditType creditType,
        int quantity,
        string idempotencyKey,
        DateTime? expiresAt = null,
        CancellationToken cancellationToken = default)
    {
        return await TryRefundAsync(
            userId,
            creditType,
            quantity,
            idempotencyKey,
            idempotencyKey,
            expiresAt,
            actorId: null,
            reason: null,
            cancellationToken: cancellationToken);
    }

    public async Task<bool> TryRefundAsync(
        Guid userId,
        CreditType creditType,
        int quantity,
        string idempotencyKey,
        string sourceIdempotencyKey,
        DateTime? expiresAt = null,
        Guid? actorId = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        var key = NormalizeKey(idempotencyKey);
        var sourceKey = NormalizeKey(sourceIdempotencyKey);
        return await ExecuteInTransactionAsync(IsolationLevel.Serializable, async () =>
        {
            var existingRefund = await _context.CreditLedgers
                .AsNoTracking()
                .Where(entry => entry.EntryType == CreditLedgerEntryType.Refund &&
                    entry.IdempotencyKey != null &&
                    (entry.IdempotencyKey == key || entry.IdempotencyKey.StartsWith(key + ":")))
                .ToListAsync(cancellationToken);
            if (existingRefund.Count > 0)
            {
                return existingRefund.All(entry =>
                        entry.UserId == userId &&
                        entry.CreditType == creditType &&
                        entry.SourceIdempotencyKey != null &&
                        (entry.SourceIdempotencyKey == sourceKey || entry.SourceIdempotencyKey.StartsWith(sourceKey + ":"))) &&
                    existingRefund.Sum(entry => entry.Quantity) == quantity;
            }

            var debitEntries = await _context.CreditLedgers
                .AsNoTracking()
                .Where(entry => entry.UserId == userId &&
                    entry.CreditType == creditType &&
                    entry.EntryType == CreditLedgerEntryType.Debit &&
                    entry.IdempotencyKey != null &&
                    (entry.IdempotencyKey == sourceKey || entry.IdempotencyKey.StartsWith(sourceKey + ":")))
                .OrderBy(entry => entry.CreatedAt)
                .ToListAsync(cancellationToken);
            var debitedQuantity = -debitEntries.Sum(entry => entry.Quantity);
            if (debitEntries.Count == 0 || debitedQuantity != quantity)
                return false;

            var sourceKeys = debitEntries
                .Select(entry => entry.IdempotencyKey!)
                .ToList();
            var alreadyRefunded = await _context.CreditLedgers
                .AsNoTracking()
                .AnyAsync(entry => entry.EntryType == CreditLedgerEntryType.Refund &&
                    entry.SourceIdempotencyKey != null &&
                    sourceKeys.Contains(entry.SourceIdempotencyKey), cancellationToken);
            if (alreadyRefunded)
                return false;

            var currentBalance = await _context.CreditLedgers
                .Where(entry => entry.UserId == userId && entry.CreditType == creditType)
                .SumAsync(entry => (long?)entry.Quantity, cancellationToken) ?? 0;
            if (currentBalance + debitedQuantity < 0)
                return false;

            for (var index = 0; index < debitEntries.Count; index++)
            {
                var debit = debitEntries[index];
                _context.CreditLedgers.Add(new CreditLedger
                {
                    UserId = userId,
                    CreditType = creditType,
                    EntryType = CreditLedgerEntryType.Refund,
                    Quantity = -debit.Quantity,
                    ExpiresAt = debit.ExpiresAt ?? expiresAt,
                    IdempotencyKey = debitEntries.Count == 1 ? key : $"{key}:{index}",
                    SourceIdempotencyKey = debit.IdempotencyKey,
                    ActorId = actorId,
                    Reason = NormalizeReason(reason),
                });
            }

            _auditLog.Add(
                "credit.refunded",
                "CreditLedger",
                key,
                null,
                new { userId, creditType, quantity, sourceIdempotencyKey = sourceKey, reason });
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }, cancellationToken);
    }

    public async Task<bool> TryAdjustAsync(
        Guid userId,
        CreditType creditType,
        int quantity,
        string idempotencyKey,
        Guid actorId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (quantity == 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (actorId == Guid.Empty) throw new ArgumentException("Actor không hợp lệ.", nameof(actorId));
        var key = NormalizeKey(idempotencyKey);
        var normalizedReason = NormalizeReason(reason)
            ?? throw new ArgumentException("Reason là bắt buộc.", nameof(reason));

        return await ExecuteInTransactionAsync(IsolationLevel.Serializable, async () =>
        {
            var existing = await _context.CreditLedgers
                .AsNoTracking()
                .FirstOrDefaultAsync(entry => entry.IdempotencyKey == key, cancellationToken);
            if (existing != null)
            {
                return existing.EntryType == CreditLedgerEntryType.Adjustment &&
                    existing.UserId == userId &&
                    existing.CreditType == creditType &&
                    existing.Quantity == quantity &&
                    existing.ActorId == actorId &&
                    string.Equals(existing.Reason, normalizedReason, StringComparison.Ordinal);
            }

            var currentBalance = await _context.CreditLedgers
                .Where(entry => entry.UserId == userId && entry.CreditType == creditType)
                .SumAsync(entry => (long?)entry.Quantity, cancellationToken) ?? 0;
            if (currentBalance + quantity < 0)
                return false;

            var entry = new CreditLedger
            {
                UserId = userId,
                CreditType = creditType,
                EntryType = CreditLedgerEntryType.Adjustment,
                Quantity = quantity,
                IdempotencyKey = key,
                ActorId = actorId,
                Reason = normalizedReason,
            };
            _context.CreditLedgers.Add(entry);
            _auditLog.Add(
                "credit.adjusted",
                "CreditLedger",
                key,
                null,
                new { userId, creditType, quantity, actorId, reason = normalizedReason });
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }, cancellationToken);
    }

    private async Task<T> ExecuteInTransactionAsync<T>(
        IsolationLevel isolationLevel,
        Func<Task<T>> operation,
        CancellationToken cancellationToken)
    {
        if (_context.Database.CurrentTransaction != null)
            return await operation();

        var executionStrategy = _context.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
            var result = await operation();
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    private static string NormalizeKey(string idempotencyKey)
    {
        var key = idempotencyKey.Trim();
        if (key.Length == 0 || key.Length > 200)
            throw new ArgumentException("Idempotency key không hợp lệ.", nameof(idempotencyKey));
        return key;
    }

    private static string? NormalizeReason(string? reason)
    {
        var normalized = reason?.Trim();
        if (string.IsNullOrEmpty(normalized)) return null;
        if (normalized.Length > 500)
            throw new ArgumentException("Reason không được vượt quá 500 ký tự.", nameof(reason));
        return normalized;
    }
}
