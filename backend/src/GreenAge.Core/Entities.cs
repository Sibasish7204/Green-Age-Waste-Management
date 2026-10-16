namespace GreenAge.Core;

public enum UserRole { Deliver = 1, Collect = 2, Household = 3, MunicipalWorker = 4 }
//public enum WasteGroup { Bio = 1, NonBio = 2 }
public enum WasteGroup { NonBio = 2 }
public enum RequestStatus { Requested = 1, Accepted = 2, Verified = 3, Completed = 4, Cancelled = 5 }
public enum NonBioSaleStatus { AwaitingHandover = 1, Sold = 2 }

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string? Phone { get; set; }
    public UserRole Role { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Address { get; set; }
    public string? Area { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class WasteCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public WasteGroup Group { get; set; }
    public string Unit { get; set; } = "kg";
    public string IconKey { get; set; } = "";
}

public sealed class PickupRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeliverUserId { get; set; }
    public Guid? CollectorId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Address { get; set; } = "";
    public RequestStatus Status { get; set; } = RequestStatus.Requested;
    public string? Notes { get; set; }
    public string? QrToken { get; set; }
    public DateTime? QrTokenExpiresAt { get; set; }
    public string? CancelReason { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<PickupRequestItem> Items { get; set; } = [];
}

public sealed class PickupRequestItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequestId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public string Unit { get; set; } = "kg";
    public decimal RequestedQuantity { get; set; }
    public decimal? ActualQuantity { get; set; }
}

public sealed class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public Guid? RequestId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class SosAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class DomainException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class NonBioSale
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SellerId { get; set; }
    public Guid? CollectorId { get; set; }
    public string StoreId { get; set; } = "";
    public string StoreName { get; set; } = "";
    public decimal Total { get; set; }
    public NonBioSaleStatus Status { get; set; } = NonBioSaleStatus.AwaitingHandover;
    public string? QrTokenHash { get; set; }
    public DateTime? QrTokenExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? VerifiedAt { get; set; }
    public List<NonBioSaleItem> Items { get; set; } = [];
}

public sealed class NonBioSaleItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SaleId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryKey { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public string Unit { get; set; } = "kg";
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
}

public sealed class NonBioOffer
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Area { get; set; } = "";
    public double DistanceKm { get; set; }
    public List<NonBioOfferRate> Rates { get; set; } = [];
}

public sealed class NonBioOfferRate
{
    public int CategoryId { get; set; }
    public decimal Rate { get; set; }
}

public sealed class HouseholdQrToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HouseholdUserId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
}

public sealed class BioCollectionVisit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HouseholdUserId { get; set; }
    public Guid WorkerUserId { get; set; }
    public string HouseholdName { get; set; } = "";
    public string Address { get; set; } = "";
    public string Area { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}