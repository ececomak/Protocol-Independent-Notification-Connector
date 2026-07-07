using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("FrontendPolicy");

var notifications = new ConcurrentDictionary<string, NotificationEnvelope>();

app.MapGet("/", () => Results.Ok(new
{
    service = "Notification Backend",
    status = "running",
    endpoints = new[]
    {
        "GET /api/health",
        "GET /api/notifications",
        "POST /api/notifications",
        "GET /api/notifications/{id}",
        "DELETE /api/notifications"
    }
}));

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "healthy",
    time = DateTimeOffset.UtcNow
}));

app.MapGet("/api/notifications", () =>
{
    var result = notifications.Values
        .OrderByDescending(x => x.ReceivedAt)
        .ToList();

    return Results.Ok(result);
});

app.MapGet("/api/notifications/{id}", (string id) =>
{
    if (!notifications.TryGetValue(id, out var notification))
    {
        return Results.NotFound(new
        {
            message = "Notification not found."
        });
    }

    return Results.Ok(notification);
});

app.MapPost("/api/notifications", (NotificationRequest request) =>
{
    var validationError = ValidateRequest(request);

    if (validationError is not null)
    {
        return Results.BadRequest(new
        {
            message = validationError
        });
    }

    var deduplicationKey = string.IsNullOrWhiteSpace(request.DeduplicationKey)
        ? GenerateDeduplicationKey(request)
        : request.DeduplicationKey.Trim();

    var notification = new NotificationEnvelope(
        Id: Guid.NewGuid().ToString("N"),
        Source: request.Source.Trim(),
        Type: request.Type.Trim(),
        Title: request.Title.Trim(),
        Message: request.Message.Trim(),
        DeduplicationKey: deduplicationKey,
        CreatedAt: request.CreatedAt ?? DateTimeOffset.UtcNow,
        ReceivedAt: DateTimeOffset.UtcNow
    );

    var alreadyExists = notifications.Values
        .Any(x => x.DeduplicationKey == notification.DeduplicationKey);

    if (alreadyExists)
    {
        return Results.Conflict(new
        {
            message = "Duplicate notification ignored.",
            deduplicationKey = notification.DeduplicationKey
        });
    }

    notifications[notification.Id] = notification;

    return Results.Created($"/api/notifications/{notification.Id}", notification);
});

app.MapDelete("/api/notifications", () =>
{
    notifications.Clear();

    return Results.Ok(new
    {
        message = "All notifications were deleted."
    });
});

app.Run();

static string? ValidateRequest(NotificationRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Source))
    {
        return "Source is required.";
    }

    if (string.IsNullOrWhiteSpace(request.Type))
    {
        return "Type is required.";
    }

    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return "Title is required.";
    }

    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return "Message is required.";
    }

    return null;
}

static string GenerateDeduplicationKey(NotificationRequest request)
{
    var rawValue = $"{request.Source}|{request.Type}|{request.Title}|{request.Message}|{request.CreatedAt}";
    var bytes = Encoding.UTF8.GetBytes(rawValue);
    var hash = SHA256.HashData(bytes);

    return Convert.ToHexString(hash).ToLowerInvariant();
}

public record NotificationRequest(
    string Source,
    string Type,
    string Title,
    string Message,
    string? DeduplicationKey,
    DateTimeOffset? CreatedAt
);

public record NotificationEnvelope(
    string Id,
    string Source,
    string Type,
    string Title,
    string Message,
    string DeduplicationKey,
    DateTimeOffset CreatedAt,
    DateTimeOffset ReceivedAt
);