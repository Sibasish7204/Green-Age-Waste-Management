using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GreenAge.Core;
using GreenAge.Infrastructure;
using GreenAge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace GreenAge.Api.Controllers;

public sealed record UserDto(Guid Id, string Name, string Email, string? Phone, UserRole Role, string? PhotoUrl, string? Address, string? Area);
public sealed record AuthResponse(string Token, UserDto User);
public sealed record RequestItemDto(int CategoryId, string CategoryName, string Unit, decimal RequestedQuantity, decimal? ActualQuantity);
public sealed record PartyDto(Guid Id, string Name, string? Phone);
public sealed record RequestDto(Guid Id, RequestStatus Status, double Latitude, double Longitude, string Address, string? Notes,
    PartyDto Deliver, PartyDto? Collector, IReadOnlyList<RequestItemDto> Items, double? DistanceKm, DateTime CreatedAt,
    DateTime? AcceptedAt, DateTime? VerifiedAt, DateTime? CompletedAt, string? CancelReason);

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService auth, IConfiguration configuration) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterBody body)
    {
        if (!Enum.TryParse<UserRole>(body.Role, true, out var role) || !Enum.IsDefined(role)) throw new DomainException("Role must be Deliver, Collect, Household, or MunicipalWorker.", 400);
        var user = await auth.RegisterAsync(body.Name, body.Email, body.Password, body.Phone, role, body.Address, body.Area);
        return StatusCode(201, CreateResponse(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginBody body) => Ok(CreateResponse(await auth.LoginAsync(body.Email, body.Password)));

    private AuthResponse CreateResponse(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(configuration["Jwt:Issuer"], configuration["Jwt:Audience"],
            [new Claim("sub", user.Id.ToString()), new Claim("email", user.Email), new Claim("role", user.Role.ToString()), new Claim("name", user.Name)],
            expires: DateTime.UtcNow.AddMinutes(configuration.GetValue("Jwt:ExpiryMinutes", 480)), signingCredentials: credentials);
        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), ToDto(user));
    }

    internal static UserDto ToDto(User user) => new(user.Id, user.Name, user.Email, user.Phone, user.Role, user.PhotoUrl, user.Address, user.Area);
}

public sealed record RegisterBody([Required, MinLength(1)] string Name, [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password, string? Phone, [Required] string Role, string? Address, string? Area);
public sealed record LoginBody([Required, EmailAddress] string Email, [Required] string Password);

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(ICategoryRepository categories) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<WasteCategory>> Get() => categories.GetAllAsync();
}

[ApiController]
[Authorize]
[Route("api/me")]
public sealed class MeController(AuthService auth, IUserRepository users) : ControllerBase
{
    [HttpGet]
    public async Task<UserDto> Get() => AuthController.ToDto(await users.GetByIdAsync(UserId()) ?? throw new DomainException("User not found.", 404));

    [HttpPut]
    public async Task<UserDto> Update(ProfileBody body) => AuthController.ToDto(await auth.UpdateProfileAsync(UserId(), body.Name, body.Phone, body.PhotoUrl));

    private Guid UserId() => Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public sealed record ProfileBody([Required, MinLength(1)] string Name, string? Phone, string? PhotoUrl);

[ApiController]
[Authorize]
[Route("api/requests")]
public sealed class RequestsController(RequestService service, IUserRepository users, IConfiguration configuration) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Deliver")]
    public async Task<ActionResult<RequestDto>> Create(CreateRequestBody body)
    {
        var request = await service.CreateAsync(UserId(), body.Latitude, body.Longitude, body.Address, body.Notes,
            body.Items.Select(item => (item.CategoryId, item.Quantity)).ToArray());
        return StatusCode(201, await MapAsync(request));
    }

    [HttpGet("mine")]
    public async Task<IReadOnlyList<RequestDto>> Mine()
    {
        var role = UserRoleValue();
        var mine = await service.GetMineAsync(UserId(), role);
        return await Task.WhenAll(mine.OrderByDescending(item => item.CreatedAt).Select(request => MapAsync(request)));
    }

    [HttpGet("open")]
    [Authorize(Roles = "Collect")]
    public async Task<IReadOnlyList<RequestDto>> Open([FromQuery] double? lat, [FromQuery] double? lng, [FromQuery] double radiusKm = 10)
    {
        var open = await service.GetOpenAsync(lat, lng, radiusKm);
        return await Task.WhenAll(open.Select(entry => MapAsync(entry.Request, entry.DistanceKm)));
    }

    [HttpGet("{id:guid}")]
    public async Task<RequestDto> Get(Guid id) => await MapAsync(await service.GetVisibleAsync(id, UserId(), UserRoleValue()));

    [HttpGet("{id:guid}/qr")]
    [Authorize(Roles = "Deliver")]
    public async Task<object> Qr(Guid id)
    {
        var qr = await service.CreateQrAsync(id, UserId(), configuration.GetValue("Qr:TokenTtlMinutes", 10));
        return new { requestId = id, token = qr.Token, expiresAt = qr.ExpiresAt, payload = $"greenage://verify?requestId={id}&token={qr.Token}" };
    }

    [HttpPost("{id:guid}/accept")]
    [Authorize(Roles = "Collect")]
    public async Task<RequestDto> Accept(Guid id) => await MapAsync(await service.AcceptAsync(id, UserId()));

    [HttpPost("{id:guid}/verify-qr")]
    [Authorize(Roles = "Collect")]
    public async Task<RequestDto> Verify(Guid id, VerifyQrBody body) => await MapAsync(await service.VerifyQrAsync(id, UserId(), body.Token));

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "Collect")]
    public async Task<RequestDto> Complete(Guid id, CompleteRequestBody body) => await MapAsync(await service.CompleteAsync(id, UserId(), body.Items.Select(item => (item.CategoryId, item.ActualQuantity)).ToArray()));

    [HttpPost("{id:guid}/cancel")]
    public async Task<RequestDto> Cancel(Guid id, CancelRequestBody body) => await MapAsync(await service.CancelAsync(id, UserId(), body.Reason));

    private async Task<RequestDto> MapAsync(PickupRequest request, double? distance = null)
    {
        var deliver = await users.GetByIdAsync(request.DeliverUserId) ?? throw new DomainException("User not found.", 404);
        var collector = request.CollectorId is Guid collectorId ? await users.GetByIdAsync(collectorId) : null;
        var showContacts = request.Status is RequestStatus.Accepted or RequestStatus.Verified or RequestStatus.Completed;
        return new RequestDto(request.Id, request.Status, request.Latitude, request.Longitude, request.Address, request.Notes,
            new PartyDto(deliver.Id, deliver.Name, showContacts ? deliver.Phone : null),
            collector is null ? null : new PartyDto(collector.Id, collector.Name, showContacts ? collector.Phone : null),
            request.Items.Select(item => new RequestItemDto(item.CategoryId, item.CategoryName, item.Unit, item.RequestedQuantity, item.ActualQuantity)).ToArray(),
            distance, request.CreatedAt, request.AcceptedAt, request.VerifiedAt, request.CompletedAt, request.CancelReason);
    }

    private Guid UserId() => Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private UserRole UserRoleValue() => Enum.Parse<UserRole>(User.FindFirstValue("role")!);
}

public sealed record CreateRequestBody([Range(-90, 90)] double Latitude, [Range(-180, 180)] double Longitude,
    [Required, MinLength(1)] string Address, string? Notes, [MinLength(1)] IReadOnlyList<CreateRequestItemBody> Items);
public sealed record CreateRequestItemBody([Range(1, int.MaxValue)] int CategoryId, [Range(typeof(decimal), "0.01", "10000")] decimal Quantity);
public sealed record VerifyQrBody([Required] string Token);
public sealed record CompleteRequestBody([MinLength(1)] IReadOnlyList<CompleteRequestItemBody> Items);
public sealed record CompleteRequestItemBody([Range(1, int.MaxValue)] int CategoryId, [Range(typeof(decimal), "0.01", "10000")] decimal ActualQuantity);
public sealed record CancelRequestBody(string? Reason);

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(INotificationRepository notifications) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<Notification>> Get() => await notifications.GetByUserAsync(UserId());
    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> Read(Guid id) { await notifications.MarkReadAsync(id, UserId()); return NoContent(); }
    private Guid UserId() => Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

[ApiController]
[Authorize]
[Route("api/sos")]
public sealed class SosController(RequestService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(SosBody body)
    {
        var alert = await service.CreateSosAsync(Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!), body.Latitude, body.Longitude, body.Message);
        return StatusCode(201, new { alert.Id, alert.CreatedAt });
    }
}

public sealed record SosBody([Range(-90, 90)] double Latitude, [Range(-180, 180)] double Longitude, string? Message);