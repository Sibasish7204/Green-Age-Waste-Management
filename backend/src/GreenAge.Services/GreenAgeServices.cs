using System.Security.Cryptography;
using GreenAge.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace GreenAge.Services;

public sealed class AuthService(IUserRepository users)
{
    private readonly PasswordHasher<User> passwordHasher = new();

    public async Task<User> RegisterAsync(string name, string email, string password, string? phone, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || password.Length < 8 || !Enum.IsDefined(role))
            throw new DomainException("Name, valid email, password (minimum 8 characters), and role are required.", 400);
        email = email.Trim().ToLowerInvariant();
        if (await users.GetByEmailAsync(email) is not null) throw new DomainException("Email is already registered.", 409);
        var user = new User { Name = name.Trim(), Email = email, Phone = phone?.Trim(), Role = role };
        user.PasswordHash = passwordHasher.HashPassword(user, password);
        await users.AddAsync(user);
        return user;
    }

    public async Task<User> LoginAsync(string email, string password)
    {
        var user = await users.GetByEmailAsync(email.Trim().ToLowerInvariant());
        if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) == PasswordVerificationResult.Failed)
            throw new DomainException("Invalid email or password.", 401);
        return user;
    }

    public async Task<User> UpdateProfileAsync(Guid id, string name, string? phone, string? photoUrl)
    {
        var user = await users.GetByIdAsync(id) ?? throw new DomainException("User not found.", 404);
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name is required.", 400);
        user.Name = name.Trim();
        user.Phone = phone?.Trim();
        user.PhotoUrl = photoUrl;
        await users.UpdateAsync(user);
        return user;
    }

    public async Task SeedDemoUsersAsync()
    {
        foreach (var (email, role, name) in new[]
        {
            ("deliver@demo.com", UserRole.Deliver, "Demo Deliverer"),
            ("collector@demo.com", UserRole.Collect, "Demo Collector")
        })
        {
            if (await users.GetByEmailAsync(email) is not null) continue;
            var user = new User { Email = email, Name = name, Role = role };
            user.PasswordHash = passwordHasher.HashPassword(user, "Demo@123");
            await users.AddAsync(user);
        }
    }
}

public sealed class RequestService(
    IUserRepository users,
    ICategoryRepository categories,
    IPickupRequestRepository requests,
    INotificationRepository notifications,
    ISosRepository sos,
    ILogger<RequestService> logger)
{
    public async Task<PickupRequest> CreateAsync(Guid deliverId, double latitude, double longitude, string address, string? notes, IReadOnlyCollection<(int CategoryId, decimal Quantity)> items)
    {
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180 || string.IsNullOrWhiteSpace(address))
            throw new DomainException("A valid location and address are required.", 400);
        if (items.Count == 0) throw new DomainException("Select at least one waste category.", 400);
        var requestItems = new List<PickupRequestItem>();
        foreach (var item in items)
        {
            ValidateQuantity(item.Quantity);
            var category = await categories.GetByIdAsync(item.CategoryId) ?? throw new DomainException($"Category {item.CategoryId} was not found.", 400);
            requestItems.Add(new PickupRequestItem { CategoryId = category.Id, CategoryName = category.Name, Unit = category.Unit, RequestedQuantity = item.Quantity });
        }
        var request = new PickupRequest
        {
            DeliverUserId = deliverId, Latitude = latitude, Longitude = longitude,
            Address = address.Trim(), Notes = notes, Items = requestItems
        };
        foreach (var item in request.Items) item.RequestId = request.Id;
        await requests.AddAsync(request);
        return request;
    }

    public async Task<IReadOnlyList<(PickupRequest Request, double? DistanceKm)>> GetOpenAsync(double? latitude, double? longitude, double radiusKm)
    {
        if (radiusKm <= 0 || radiusKm > 200) throw new DomainException("Radius must be between 0 and 200 km.", 400);
        var open = await requests.GetOpenAsync();
        if (latitude is null || longitude is null) return open.OrderByDescending(request => request.CreatedAt).Select(request => (request, (double?)null)).ToList();
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180) throw new DomainException("Coordinates are invalid.", 400);
        return open.Select(request => (Request: request, Distance: Distance(latitude.Value, longitude.Value, request.Latitude, request.Longitude)))
            .Where(entry => entry.Distance <= radiusKm).OrderBy(entry => entry.Distance)
            .Select(entry => (entry.Request, (double?)Math.Round(entry.Distance, 1))).ToList();
    }

    public async Task<IReadOnlyList<PickupRequest>> GetMineAsync(Guid userId, UserRole role) => role == UserRole.Deliver
        ? await requests.GetByDeliverAsync(userId)
        : await requests.GetByCollectorAsync(userId);

    public async Task<PickupRequest> GetVisibleAsync(Guid id, Guid userId, UserRole role)
    {
        var request = await GetRequestAsync(id);
        var allowed = request.DeliverUserId == userId || request.CollectorId == userId || (role == UserRole.Collect && request.Status == RequestStatus.Requested);
        if (!allowed) throw new DomainException("You cannot view this request.", 403);
        return request;
    }

    public async Task<PickupRequest> AcceptAsync(Guid id, Guid collectorId)
    {
        var active = (await requests.GetByCollectorAsync(collectorId)).Count(request => request.Status is RequestStatus.Accepted or RequestStatus.Verified);
        if (active >= 3) throw new DomainException("Collectors may hold at most 3 active requests.", 409);
        if (!await requests.TryAcceptAsync(id, collectorId)) throw new DomainException("Request already accepted by another collector.", 409);
        var request = await GetRequestAsync(id);
        var collector = await users.GetByIdAsync(collectorId) ?? throw new DomainException("Collector not found.", 404);
        await NotifyAsync(request.DeliverUserId, "Request accepted", $"Your request was accepted by {collector.Name}.", id);
        return request;
    }

    public async Task<(PickupRequest Request, string Token, DateTime ExpiresAt)> CreateQrAsync(Guid id, Guid deliverId, int ttlMinutes)
    {
        var request = await GetRequestAsync(id);
        if (request.DeliverUserId != deliverId) throw new DomainException("Only the deliverer can generate this QR code.", 403);
        if (request.Status != RequestStatus.Accepted) throw new DomainException("QR codes are available after a request is accepted.", 409);
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var expiresAt = DateTime.UtcNow.AddMinutes(ttlMinutes);
        request.QrToken = token;
        request.QrTokenExpiresAt = expiresAt;
        await requests.UpdateAsync(request);
        return (request, token, expiresAt);
    }

    public async Task<PickupRequest> VerifyQrAsync(Guid id, Guid collectorId, string token)
    {
        var request = await GetRequestAsync(id);
        if (request.CollectorId != collectorId) throw new DomainException("You are not assigned to this request.", 403);
        if (request.Status != RequestStatus.Accepted || request.QrToken is null || request.QrTokenExpiresAt <= DateTime.UtcNow || !CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(request.QrToken), System.Text.Encoding.UTF8.GetBytes(token)))
            throw new DomainException("QR token is invalid or expired.", 400);
        request.Status = RequestStatus.Verified;
        request.VerifiedAt = DateTime.UtcNow;
        request.QrToken = null;
        request.QrTokenExpiresAt = null;
        await requests.UpdateAsync(request);
        await NotifyAsync(request.DeliverUserId, "Handover verified", "The collector verified your QR code.", id);
        return request;
    }

    public async Task<PickupRequest> CompleteAsync(Guid id, Guid collectorId, IReadOnlyCollection<(int CategoryId, decimal Quantity)> actualItems)
    {
        var request = await GetRequestAsync(id);
        if (request.CollectorId != collectorId) throw new DomainException("You are not assigned to this request.", 403);
        if (request.Status != RequestStatus.Verified) throw new DomainException("The request must be verified before completion.", 409);
        if (actualItems.Count != request.Items.Count || actualItems.Select(item => item.CategoryId).Distinct().Count() != actualItems.Count)
            throw new DomainException("Provide an actual quantity for every requested category and no others.", 400);
        foreach (var input in actualItems)
        {
            ValidateQuantity(input.Quantity);
            var item = request.Items.FirstOrDefault(candidate => candidate.CategoryId == input.CategoryId) ?? throw new DomainException("Unexpected category in completion.", 400);
            item.ActualQuantity = input.Quantity;
        }
        request.Status = RequestStatus.Completed;
        request.CompletedAt = DateTime.UtcNow;
        request.QrToken = null;
        request.QrTokenExpiresAt = null;
        await requests.UpdateAsync(request);
        var differs = request.Items.Any(item => item.ActualQuantity != item.RequestedQuantity);
        await NotifyAsync(request.DeliverUserId, "Request completed", differs ? "Your pickup is complete; quantity differs from the request." : "Your pickup is complete.", id);
        return request;
    }

    public async Task<PickupRequest> CancelAsync(Guid id, Guid userId, string? reason)
    {
        var request = await GetRequestAsync(id);
        if (request.Status is not (RequestStatus.Requested or RequestStatus.Accepted)) throw new DomainException("Only requested or accepted jobs can be cancelled.", 409);
        if (request.DeliverUserId == userId)
        {
            request.Status = RequestStatus.Cancelled;
            request.CancelledByUserId = userId;
            request.CancelReason = reason;
            if (request.CollectorId is Guid collectorId) await NotifyAsync(collectorId, "Request cancelled", "The deliverer cancelled this pickup.", id);
        }
        else if (request.CollectorId == userId)
        {
            request.Status = RequestStatus.Requested;
            request.CollectorId = null;
            request.AcceptedAt = null;
            request.CancelledByUserId = userId;
            request.CancelReason = reason;
            await NotifyAsync(request.DeliverUserId, "Collector unavailable", "The collector released your request; it is open again.", id);
        }
        else throw new DomainException("You cannot cancel this request.", 403);
        request.QrToken = null;
        request.QrTokenExpiresAt = null;
        await requests.UpdateAsync(request);
        return request;
    }

    public async Task<SosAlert> CreateSosAsync(Guid userId, double latitude, double longitude, string? message)
    {
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180) throw new DomainException("Coordinates are invalid.", 400);
        var alert = new SosAlert { UserId = userId, Latitude = latitude, Longitude = longitude, Message = message };
        await sos.AddAsync(alert);
        logger.LogWarning("SOS alert {AlertId} raised by user {UserId}", alert.Id, userId);
        var active = (await requests.GetAllAsync()).Where(request => request.Status is RequestStatus.Accepted or RequestStatus.Verified)
            .Where(request => request.DeliverUserId == userId || request.CollectorId == userId)
            .OrderByDescending(request => request.CreatedAt).FirstOrDefault();
        if (active is not null)
        {
            var otherId = active.DeliverUserId == userId ? active.CollectorId : active.DeliverUserId;
            if (otherId is Guid target) await NotifyAsync(target, "Emergency alert", "Your pickup partner sent an SOS alert.", active.Id);
        }
        return alert;
    }

    private async Task<PickupRequest> GetRequestAsync(Guid id) => await requests.GetByIdAsync(id) ?? throw new DomainException("Request not found.", 404);

    private Task NotifyAsync(Guid userId, string title, string message, Guid? requestId) => notifications.AddAsync(new Notification
    {
        UserId = userId, Title = title, Message = message, RequestId = requestId
    });

    private static void ValidateQuantity(decimal quantity)
    {
        if (quantity <= 0 || quantity > 10000 || decimal.Round(quantity, 2) != quantity)
            throw new DomainException("Quantity must be greater than 0, at most 10000, and have no more than 2 decimals.", 400);
    }

    private static double Distance(double lat1, double lng1, double lat2, double lng2)
    {
        const double radius = 6371;
        var dLat = Degrees(lat2 - lat1);
        var dLng = Degrees(lng2 - lng1);
        var a = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Cos(Degrees(lat1)) * Math.Cos(Degrees(lat2)) * Math.Pow(Math.Sin(dLng / 2), 2);
        return radius * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double Degrees(double value) => value * Math.PI / 180;
}