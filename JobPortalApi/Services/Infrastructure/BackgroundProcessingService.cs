using System.Text.Json;
using JobPortalApi.Models;
using JobPortalApi.Models.Enums;
using JobPortalApi.Services.Matching;
using JobPortalApi.Services.Notifications;
using Microsoft.EntityFrameworkCore;

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
    private DateTime _lastMaintenance = DateTime.MinValue;
    private DateTime _lastReminder = DateTime.MinValue;
    private DateTime _lastNewsletter = DateTime.MinValue;
    private DateTime _lastMatching = DateTime.MinValue;

    public BackgroundProcessingService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<BackgroundProcessingService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
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
                if (now - _lastMatching >= TimeSpan.FromHours(1))
                {
                    await RecalculateMatchingAsync(stoppingToken);
                    _lastMatching = now;
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
        var now = DateTime.UtcNow;
        var candidates = await db.OutboxMessages
            .Where(message => message.ProcessedAt == null && message.DeadLetteredAt == null &&
                              (message.NextAttemptAt == null || message.NextAttemptAt <= now) && message.Attempts < 10)
            .OrderBy(message => message.OccurredAt)
            .Take(50)
            .Select(message => new { message.Id, message.Attempts })
            .ToListAsync(cancellationToken);

        foreach (var candidate in candidates)
        {
            var claimed = await db.OutboxMessages
                .Where(message => message.Id == candidate.Id && message.ProcessedAt == null &&
                                  message.DeadLetteredAt == null && message.Attempts == candidate.Attempts)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(message => message.Attempts, message => message.Attempts + 1), cancellationToken);
            if (claimed != 1) continue;

            var message = await db.OutboxMessages.FirstAsync(item => item.Id == candidate.Id, cancellationToken);
            try
            {
                await HandleMessageAsync(db, email, message, cancellationToken);
                message.ProcessedAt = DateTime.UtcNow;
                message.LastError = null;
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                message.LastError = ex.Message[..Math.Min(ex.Message.Length, 2000)];
                if (message.Attempts >= 10)
                    message.DeadLetteredAt = DateTime.UtcNow;
                else
                    message.NextAttemptAt = DateTime.UtcNow.AddSeconds(Math.Min(3600, Math.Pow(2, message.Attempts) * 5));
                await db.SaveChangesAsync(cancellationToken);
                _logger.LogWarning(ex, "Outbox {MessageId} xử lý lần {Attempt} thất bại.", message.Id, message.Attempts);
            }
        }
    }

    private static async Task HandleMessageAsync(
        ApplicationDbContext db,
        EmailNotificationService email,
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
                await AddNotificationAsync(db, root.GetProperty("CandidateId").GetGuid(), $"Đã nhận hồ sơ ứng tuyển cho tin \"{root.GetProperty("JobTitle").GetString()}\".", "application_confirmation", sourceId, cancellationToken);
                await email.SendApplicationConfirmationAsync(root.GetProperty("CandidateEmail").GetString() ?? string.Empty, root.GetProperty("JobTitle").GetString() ?? string.Empty, true);
                break;
            case "application.status.changed":
                await AddNotificationAsync(db, root.GetProperty("CandidateId").GetGuid(), $"Trạng thái hồ sơ cho tin \"{root.GetProperty("JobTitle").GetString()}\" đã chuyển thành {root.GetProperty("ToStatus").GetString()}.", "application_status_changed", sourceId, cancellationToken);
                await email.SendApplicationStatusChangedAsync(root.GetProperty("CandidateEmail").GetString() ?? string.Empty, root.GetProperty("JobTitle").GetString() ?? string.Empty, root.GetProperty("ToStatus").GetString() ?? string.Empty, true);
                break;
            case "interview.scheduled":
                await AddNotificationAsync(db, root.GetProperty("CandidateId").GetGuid(), $"Bạn có lịch phỏng vấn cho tin \"{root.GetProperty("JobTitle").GetString()}\".", "interview_scheduled", sourceId, cancellationToken);
                await email.SendInterviewInvitationAsync(root.GetProperty("CandidateEmail").GetString() ?? string.Empty, root.GetProperty("JobTitle").GetString() ?? string.Empty, root.GetProperty("StartAt").GetDateTime(), root.GetProperty("MeetingUrl").GetString(), root.GetProperty("Location").GetString(), true);
                break;
            case "interview.reminder":
                await AddNotificationAsync(db, root.GetProperty("CandidateId").GetGuid(), $"Nhắc lịch phỏng vấn cho tin \"{root.GetProperty("JobTitle").GetString()}\".", "interview_reminder", sourceId, cancellationToken);
                await email.SendInterviewReminderAsync(root.GetProperty("CandidateEmail").GetString() ?? string.Empty, root.GetProperty("JobTitle").GetString() ?? string.Empty, root.GetProperty("StartAt").GetDateTime(), root.GetProperty("MeetingUrl").GetString(), true);
                break;
            case "newsletter.send":
                var titles = root.GetProperty("Titles").EnumerateArray()
                    .Select(item => item.GetString() ?? string.Empty)
                    .Where(title => !string.IsNullOrWhiteSpace(title))
                    .ToArray();
                await email.SendNewsletterAsync(root.GetProperty("Email").GetString() ?? string.Empty, titles, true);
                break;
            case "interview.rescheduled":
            case "interview.cancelled":
                break;
        }
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
            var from = now.AddHours(window.Hours).AddMinutes(-2);
            var to = now.AddHours(window.Hours).AddMinutes(2);
            var interviews = await db.Interviews.Include(item => item.Application).ThenInclude(application => application.JobPost)
                .Include(item => item.Application).ThenInclude(application => application.Candidate)
                .Where(item => item.Status == InterviewStatus.Scheduled && item.StartAt >= from && item.StartAt <= to)
                .ToListAsync(cancellationToken);
            foreach (var interview in interviews)
            {
                var key = $"interview:{interview.Id:D}:reminder:{window.Name}";
                if (await db.OutboxMessages.AnyAsync(message => message.DeduplicationKey == key, cancellationToken)) continue;
                db.OutboxMessages.Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(), Type = "interview.reminder", Payload = JsonSerializer.Serialize(new
                    {
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
        var subscriptions = await db.NewsletterSubscriptions.Where(item => item.IsActive).ToListAsync(cancellationToken);
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
    }

    private static TimeZoneInfo ResolveBangkokTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
        catch (InvalidTimeZoneException) { return TimeZoneInfo.Utc; }
    }

    private async Task RecalculateMatchingAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var matching = scope.ServiceProvider.GetRequiredService<MatchingService>();
        var candidates = await db.candidateProfiles.Select(profile => profile.UserId).Take(1000).ToListAsync(cancellationToken);
        foreach (var candidateId in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await matching.GetRecommendedJobsAsync(candidateId, new DTOs.Matching.MatchQueryDto { Page = 1, PageSize = 100 });
        }
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
}
