using System.Text.Json;
using System.Security.Cryptography;
using JobPortalApi.Models;
using JobPortalApi.Models.Enums;
using JobPortalApi.Services.Matching;
using JobPortalApi.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;

namespace JobPortalApi.Services.Infrastructure;

/// <summary>
/// Persistent, idempotent background processing. The outbox is the source of
/// truth; the periodic loops are safe to restart and can run on every instance.
/// </summary>
public sealed class BackgroundProcessingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BackgroundProcessingService> _logger;
    private readonly IDataProtector _passwordResetProtector;
    private readonly string _workerId = Guid.NewGuid().ToString("N");
    private DateTime _lastMaintenance = DateTime.MinValue;
    private DateTime _lastReminder = DateTime.MinValue;
    private DateTime _lastNewsletter = DateTime.MinValue;

    public BackgroundProcessingService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<BackgroundProcessingService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _passwordResetProtector = dataProtectionProvider.CreateProtector("JobPortalApi.PasswordReset.v1");
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.GetValue("BackgroundJobs:Enabled", true))
        {
            _logger.LogInformation("Background jobs đang tắt qua BackgroundJobs:Enabled.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessOutboxAsync(stoppingToken);
                var now = DateTime.UtcNow;
                if (now - _lastMaintenance >= TimeSpan.FromMinutes(1))
                {
                    await RunMaintenanceAsync(stoppingToken);
                    _lastMaintenance = now;
                }
                if (now - _lastReminder >= TimeSpan.FromMinutes(5))
                {
                    await QueueInterviewRemindersAsync(stoppingToken);
                    _lastReminder = now;
                }
                if (now - _lastNewsletter >= TimeSpan.FromHours(1))
                {
                    await QueueNewsletterAsync(stoppingToken);
                    _lastNewsletter = now;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background processing cycle thất bại.");
            }
        }
    }

    private async Task ProcessOutboxAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var email = scope.ServiceProvider.GetRequiredService<EmailNotificationService>();
        var matching = scope.ServiceProvider.GetRequiredService<MatchingService>();
        var now = DateTime.UtcNow;
        var leaseDuration = TimeSpan.FromMinutes(_configuration.GetValue("BackgroundJobs:OutboxLeaseMinutes", 2));
        var candidates = await db.OutboxMessages
            .Where(message => message.ProcessedAt == null && message.DeadLetteredAt == null &&
                              (message.NextAttemptAt == null || message.NextAttemptAt <= now) && message.Attempts < 10 &&
                              (message.LockExpiresAt == null || message.LockExpiresAt <= now))
            .OrderBy(message => message.OccurredAt)
            .Take(50)
            .Select(message => new { message.Id, message.Attempts })
            .ToListAsync(cancellationToken);

        foreach (var candidate in candidates)
        {
            var claimed = await db.OutboxMessages
                .Where(message => message.Id == candidate.Id && message.ProcessedAt == null &&
                                  message.DeadLetteredAt == null && message.Attempts == candidate.Attempts &&
                                  (message.LockExpiresAt == null || message.LockExpiresAt <= now))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(message => message.Attempts, message => message.Attempts + 1)
                    .SetProperty(message => message.LockedBy, _workerId)
                    .SetProperty(message => message.LockExpiresAt, now.Add(leaseDuration)), cancellationToken);
            if (claimed != 1) continue;

            var message = await db.OutboxMessages.AsNoTracking()
                .FirstAsync(item => item.Id == candidate.Id && item.LockedBy == _workerId, cancellationToken);
            try
            {
                await HandleMessageAsync(db, email, matching, message, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
                await db.OutboxMessages
                    .Where(item => item.Id == message.Id && item.LockedBy == _workerId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.ProcessedAt, DateTime.UtcNow)
                        .SetProperty(item => item.LastError, (string?)null)
                        .SetProperty(item => item.NextAttemptAt, (DateTime?)null)
                        .SetProperty(item => item.LockedBy, (string?)null)
                        .SetProperty(item => item.LockExpiresAt, (DateTime?)null), cancellationToken);
            }
            catch (Exception ex)
            {
                // Không để các notification/match thay đổi dở dang của message
                // lỗi trôi sang message kế tiếp trong cùng DbContext.
                db.ChangeTracker.Clear();
                var lastError = ex.Message[..Math.Min(ex.Message.Length, 2000)];
                var isFinalAttempt = message.Attempts >= 10;
                var retryAt = isFinalAttempt
                    ? (DateTime?)null
                    : DateTime.UtcNow.AddSeconds(Math.Min(3600, Math.Pow(2, message.Attempts) * 5));
                await db.OutboxMessages
                    .Where(item => item.Id == message.Id && item.LockedBy == _workerId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.LastError, lastError)
                        .SetProperty(item => item.DeadLetteredAt, isFinalAttempt ? DateTime.UtcNow : (DateTime?)null)
                        .SetProperty(item => item.NextAttemptAt, retryAt)
                        .SetProperty(item => item.LockedBy, (string?)null)
                        .SetProperty(item => item.LockExpiresAt, (DateTime?)null), cancellationToken);
                _logger.LogWarning(ex, "Outbox {MessageId} xử lý lần {Attempt} thất bại.", message.Id, message.Attempts);
            }
        }
    }

    private async Task HandleMessageAsync(
        ApplicationDbContext db,
        EmailNotificationService email,
        MatchingService matching,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(message.Payload);
        var root = document.RootElement;
        var sourceId = message.Id.ToString("D");
        switch (message.Type)
        {
            case "application.created.notification":
                await AddNotificationAsync(db, root.GetProperty("EmployerId").GetGuid(), $"Có ứng viên mới cho tin \"{root.GetProperty("JobTitle").GetString()}\".", "application_created", sourceId, cancellationToken);
                var applicationCandidateId = root.GetProperty("CandidateId").GetGuid();
                await AddNotificationAsync(db, applicationCandidateId, $"Đã nhận hồ sơ ứng tuyển cho tin \"{root.GetProperty("JobTitle").GetString()}\".", "application_confirmation", sourceId, cancellationToken);
                if (await ShouldSendTransactionalEmailAsync(db, applicationCandidateId, cancellationToken))
                    await email.SendApplicationConfirmationAsync(root.GetProperty("CandidateEmail").GetString() ?? string.Empty, root.GetProperty("JobTitle").GetString() ?? string.Empty, true, sourceId);
                break;
            case "application.status.changed":
                var status = ReadStatus(root.GetProperty("ToStatus"));
                var statusCandidateId = root.GetProperty("CandidateId").GetGuid();
                await AddNotificationAsync(db, statusCandidateId, $"Trạng thái hồ sơ cho tin \"{root.GetProperty("JobTitle").GetString()}\" đã chuyển thành {status}.", "application_status_changed", sourceId, cancellationToken);
                if (await ShouldSendTransactionalEmailAsync(db, statusCandidateId, cancellationToken))
                    await email.SendApplicationStatusChangedAsync(root.GetProperty("CandidateEmail").GetString() ?? string.Empty, root.GetProperty("JobTitle").GetString() ?? string.Empty, status, true, sourceId);
                break;
            case "interview.scheduled":
                var scheduledCandidateId = root.GetProperty("CandidateId").GetGuid();
                await AddNotificationAsync(db, scheduledCandidateId, $"Bạn có lịch phỏng vấn cho tin \"{root.GetProperty("JobTitle").GetString()}\".", "interview_scheduled", sourceId, cancellationToken);
                if (await ShouldSendTransactionalEmailAsync(db, scheduledCandidateId, cancellationToken))
                    await email.SendInterviewInvitationAsync(root.GetProperty("CandidateEmail").GetString() ?? string.Empty, root.GetProperty("JobTitle").GetString() ?? string.Empty, root.GetProperty("StartAt").GetDateTime(), root.GetProperty("MeetingUrl").GetString(), root.GetProperty("Location").GetString(), true, sourceId);
                break;
            case "interview.reminder":
                if (!await IsCurrentInterviewReminderAsync(db, root, cancellationToken))
                    break;
                var reminderCandidateId = root.GetProperty("CandidateId").GetGuid();
                await AddNotificationAsync(db, reminderCandidateId, $"Nhắc lịch phỏng vấn cho tin \"{root.GetProperty("JobTitle").GetString()}\".", "interview_reminder", sourceId, cancellationToken);
                if (await ShouldSendTransactionalEmailAsync(db, reminderCandidateId, cancellationToken))
                    await email.SendInterviewReminderAsync(root.GetProperty("CandidateEmail").GetString() ?? string.Empty, root.GetProperty("JobTitle").GetString() ?? string.Empty, root.GetProperty("StartAt").GetDateTime(), root.GetProperty("MeetingUrl").GetString(), true, sourceId);
                break;
            case "newsletter.send":
                var subscriptionEmail = root.GetProperty("Email").GetString() ?? string.Empty;
                var subscription = await db.NewsletterSubscriptions
                    .Where(item => item.Email == subscriptionEmail && item.IsActive)
                    .Select(item => new { item.UserId })
                    .FirstOrDefaultAsync(cancellationToken);
                if (subscription == null || !await ShouldSendMarketingEmailAsync(db, subscription.UserId, cancellationToken))
                    break;
                var titles = root.GetProperty("Titles").EnumerateArray()
                    .Select(item => item.GetString() ?? string.Empty)
                    .Where(title => !string.IsNullOrWhiteSpace(title))
                    .ToArray();
                await email.SendNewsletterAsync(root.GetProperty("Email").GetString() ?? string.Empty, titles, true, sourceId);
                break;
            case "interview.rescheduled":
                var rescheduledCandidateId = root.GetProperty("CandidateId").GetGuid();
                await AddNotificationAsync(db, rescheduledCandidateId,
                    $"Lịch phỏng vấn cho tin \"{root.GetProperty("JobTitle").GetString()}\" đã được thay đổi.",
                    "interview_rescheduled", sourceId, cancellationToken);
                if (await ShouldSendTransactionalEmailAsync(db, rescheduledCandidateId, cancellationToken))
                    await email.SendInterviewRescheduledAsync(
                        root.GetProperty("CandidateEmail").GetString() ?? string.Empty,
                        root.GetProperty("JobTitle").GetString() ?? string.Empty,
                        root.GetProperty("StartAt").GetDateTime(),
                        root.GetProperty("MeetingUrl").GetString(),
                        root.GetProperty("Location").GetString(),
                        true,
                        sourceId);
                break;
            case "interview.cancelled":
                var cancelledCandidateId = root.GetProperty("CandidateId").GetGuid();
                await AddNotificationAsync(db, cancelledCandidateId,
                    $"Lịch phỏng vấn cho tin \"{root.GetProperty("JobTitle").GetString()}\" đã bị hủy.",
                    "interview_cancelled", sourceId, cancellationToken);
                if (await ShouldSendTransactionalEmailAsync(db, cancelledCandidateId, cancellationToken))
                    await email.SendInterviewCancelledAsync(
                        root.GetProperty("CandidateEmail").GetString() ?? string.Empty,
                        root.GetProperty("JobTitle").GetString() ?? string.Empty,
                        true,
                        sourceId);
                break;
            case "password.reset.requested":
                var resetEmail = root.GetProperty("Email").GetString() ?? string.Empty;
                var resetToken = _passwordResetProtector.Unprotect(root.GetProperty("ProtectedToken").GetString() ?? string.Empty);
                var resetTokenHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(resetToken))).ToLowerInvariant();
                var isActiveReset = await db.PasswordResetTokens.AnyAsync(token =>
                    token.TokenHash == resetTokenHash && token.UsedAt == null && token.ExpiresAt > DateTime.UtcNow && token.User.Email == resetEmail,
                    cancellationToken);
                if (isActiveReset)
                    await email.SendPasswordResetAsync(resetEmail, resetToken, sourceId);
                break;
            case "matching.candidate.refresh":
                var candidateCursorCreatedAt = root.TryGetProperty("CursorCreatedAt", out var candidateCursorDateElement) &&
                    candidateCursorDateElement.TryGetDateTime(out var parsedCandidateCursorDate)
                    ? parsedCandidateCursorDate
                    : (DateTime?)null;
                var candidateCursorJobId = root.TryGetProperty("CursorJobId", out var candidateCursorJobElement) &&
                    candidateCursorJobElement.TryGetGuid(out var parsedCandidateCursorJobId)
                    ? parsedCandidateCursorJobId
                    : (Guid?)null;
                await matching.RefreshCandidateMatchesAsync(
                    root.GetProperty("CandidateId").GetGuid(),
                    200,
                    candidateCursorCreatedAt,
                    candidateCursorJobId,
                    cancellationToken);
                break;
            case "matching.job.refresh":
                var applicationCursor = root.TryGetProperty("CursorApplicationId", out var applicationCursorElement) &&
                    applicationCursorElement.TryGetGuid(out var parsedApplicationCursor)
                    ? parsedApplicationCursor
                    : (Guid?)null;
                await matching.RefreshJobCandidateMatchesAsync(
                    root.GetProperty("JobPostId").GetGuid(),
                    200,
                    applicationCursor,
                    cancellationToken);
                break;
        }
    }

    private static async Task<bool> IsCurrentInterviewReminderAsync(
        ApplicationDbContext db,
        JsonElement payload,
        CancellationToken cancellationToken)
    {
        if (!payload.TryGetProperty("InterviewId", out var interviewIdElement) ||
            !payload.TryGetProperty("StartAt", out var startAtElement) ||
            !interviewIdElement.TryGetGuid(out var interviewId) ||
            !startAtElement.TryGetDateTime(out var startAt))
            return false;

        return await db.Interviews.AnyAsync(interview =>
            interview.Id == interviewId && interview.Status == InterviewStatus.Scheduled && interview.StartAt == startAt,
            cancellationToken);
    }

    private static async Task<bool> ShouldSendTransactionalEmailAsync(
        ApplicationDbContext db,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var preferences = await db.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new { user.EmailNotifications, user.ApplicationUpdates })
            .SingleOrDefaultAsync(cancellationToken);
        return preferences is { EmailNotifications: true, ApplicationUpdates: true };
    }

    private static async Task<bool> ShouldSendMarketingEmailAsync(
        ApplicationDbContext db,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var preferences = await db.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new { user.EmailNotifications, user.JobAlerts, user.MarketingEmails })
            .SingleOrDefaultAsync(cancellationToken);
        return preferences is { EmailNotifications: true, JobAlerts: true, MarketingEmails: true };
    }

    private async Task RunMaintenanceAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;
        await db.JobPosts.Where(post => post.Status == JobPostStatus.Active && post.ExpiresAt != null && post.ExpiresAt <= now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(post => post.Status, JobPostStatus.Expired), cancellationToken);
        await db.PaymentOrders.Where(order => order.Status == PaymentOrderStatus.Pending && order.ExpiresAt <= now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(order => order.Status, PaymentOrderStatus.Expired), cancellationToken);
    }

    private async Task QueueInterviewRemindersAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;
        var windows = new[] { (Hours: 24, Name: "24h"), (Hours: 1, Name: "1h") };
        foreach (var window in windows)
        {
            // Khoảng chồng lên chu kỳ 5 phút để restart hoặc trễ tick không làm mất nhắc lịch.
            var from = now.AddHours(window.Hours).AddMinutes(-6);
            var to = now.AddHours(window.Hours).AddMinutes(2);
            var interviews = await db.Interviews.Include(item => item.Application).ThenInclude(application => application.JobPost)
                .Include(item => item.Application).ThenInclude(application => application.Candidate)
                .Where(item => item.Status == InterviewStatus.Scheduled && item.StartAt >= from && item.StartAt <= to)
                .ToListAsync(cancellationToken);
            foreach (var interview in interviews)
            {
                var key = $"interview:{interview.Id:D}:reminder:{window.Name}:{interview.StartAt.Ticks}";
                if (await db.OutboxMessages.AnyAsync(message => message.DeduplicationKey == key, cancellationToken)) continue;
                db.OutboxMessages.Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(), Type = "interview.reminder", Payload = JsonSerializer.Serialize(new
                    {
                        InterviewId = interview.Id,
                        CandidateId = interview.Application.CandidateId,
                        CandidateEmail = interview.Application.Candidate.Email,
                        JobTitle = interview.Application.JobPost.Title,
                        interview.StartAt,
                        interview.MeetingUrl
                    }), OccurredAt = now, NextAttemptAt = now, DeduplicationKey = key
                });
            }
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task QueueNewsletterAsync(CancellationToken cancellationToken)
    {
        var bangkok = ResolveBangkokTimeZone();
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, bangkok);
        if (localNow.DayOfWeek != DayOfWeek.Monday || localNow.Hour != 8) return;
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var since = DateTime.UtcNow.AddDays(-7);
        var titles = await db.JobPosts.Where(post => post.Status == JobPostStatus.Active && post.CreatedAt >= since)
            .OrderByDescending(post => post.CreatedAt).Select(post => post.Title).Take(20).ToListAsync(cancellationToken);
        if (titles.Count == 0) return;
        var subscriptionOffset = 0;
        while (true)
        {
            var subscriptions = await db.NewsletterSubscriptions
                .Where(item => item.IsActive)
                .OrderBy(item => item.Id)
                .Select(item => new { item.Id, item.Email })
                .Skip(subscriptionOffset)
                .Take(200)
                .ToListAsync(cancellationToken);
            if (subscriptions.Count == 0) break;

            foreach (var subscription in subscriptions)
            {
                var key = $"newsletter:{subscription.Id:D}:{localNow:yyyyMMdd}";
                if (await db.OutboxMessages.AnyAsync(message => message.DeduplicationKey == key, cancellationToken)) continue;
                db.OutboxMessages.Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(), Type = "newsletter.send", Payload = JsonSerializer.Serialize(new { subscription.Email, Titles = titles }),
                    OccurredAt = DateTime.UtcNow, NextAttemptAt = DateTime.UtcNow, DeduplicationKey = key
                });
            }
            await db.SaveChangesAsync(cancellationToken);
            subscriptionOffset += subscriptions.Count;
        }
    }

    private static TimeZoneInfo ResolveBangkokTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
        catch (InvalidTimeZoneException) { return TimeZoneInfo.Utc; }
    }

    private static async Task AddNotificationAsync(ApplicationDbContext db, Guid userId, string message, string type, string sourceId, CancellationToken cancellationToken)
    {
        if (!await db.Notifications.AnyAsync(item => item.SourceMessageId == sourceId && item.UserId == userId, cancellationToken))
            db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(), UserId = userId, Message = message, Type = type,
                SourceMessageId = sourceId, Read = false, CreatedAt = DateTime.UtcNow
            });
    }

    private static string ReadStatus(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
            return value.GetString() ?? string.Empty;

        // Read legacy numeric payloads as well, so messages written before
        // the enum serialization fix can still be retried successfully.
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var numeric) &&
            Enum.IsDefined(typeof(ApplyStatus), numeric))
            return ((ApplyStatus)numeric).ToString();

        return value.ToString();
    }
}
