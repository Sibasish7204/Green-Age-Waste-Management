using GreenAge.Core;
using Microsoft.EntityFrameworkCore;

namespace GreenAge.Infrastructure;

public sealed class GreenAgeDbContext(DbContextOptions<GreenAgeDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<WasteCategory> Categories => Set<WasteCategory>();
    public DbSet<PickupRequest> Requests => Set<PickupRequest>();
    public DbSet<PickupRequestItem> RequestItems => Set<PickupRequestItem>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SosAlert> SosAlerts => Set<SosAlert>();
    public DbSet<NonBioSale> NonBioSales => Set<NonBioSale>();
    public DbSet<NonBioSaleItem> NonBioSaleItems => Set<NonBioSaleItem>();
    public DbSet<HouseholdQrToken> HouseholdQrTokens => Set<HouseholdQrToken>();
    public DbSet<BioCollectionVisit> BioCollectionVisits => Set<BioCollectionVisit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(user => user.Email).IsUnique();
        modelBuilder.Entity<User>().Property(user => user.Role).HasConversion<string>();
        modelBuilder.Entity<User>().Property(user => user.Address).HasMaxLength(500);
        modelBuilder.Entity<User>().Property(user => user.Area).HasMaxLength(160);
        modelBuilder.Entity<WasteCategory>().Property(category => category.Group).HasConversion<string>();
        modelBuilder.Entity<PickupRequest>().Property(request => request.Status).HasConversion<string>();
        modelBuilder.Entity<PickupRequest>().HasIndex(request => new { request.Status, request.CreatedAt });
        modelBuilder.Entity<PickupRequestItem>().Property(item => item.RequestedQuantity).HasPrecision(10, 2);
        modelBuilder.Entity<PickupRequestItem>().Property(item => item.ActualQuantity).HasPrecision(10, 2);
        modelBuilder.Entity<PickupRequest>().HasMany(request => request.Items).WithOne().HasForeignKey(item => item.RequestId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<NonBioSale>().Property(sale => sale.Status).HasConversion<string>();
        modelBuilder.Entity<NonBioSale>().HasIndex(sale => new { sale.SellerId, sale.CreatedAt });
        modelBuilder.Entity<NonBioSale>().HasIndex(sale => sale.QrTokenHash);
        modelBuilder.Entity<NonBioSaleItem>().Property(item => item.Quantity).HasPrecision(10, 2);
        modelBuilder.Entity<NonBioSaleItem>().Property(item => item.Rate).HasPrecision(10, 2);
        modelBuilder.Entity<NonBioSale>().HasMany(sale => sale.Items).WithOne().HasForeignKey(item => item.SaleId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<HouseholdQrToken>().HasIndex(token => token.TokenHash).IsUnique();
        modelBuilder.Entity<HouseholdQrToken>().HasIndex(token => new { token.HouseholdUserId, token.ExpiresAt });
        modelBuilder.Entity<BioCollectionVisit>().HasIndex(visit => new { visit.WorkerUserId, visit.CreatedAt });
    }

    public static IReadOnlyList<WasteCategory> SeedCategories =>
    [
        //new() { Id = 1, Name = "Biodegradable Waste", Group = WasteGroup.Bio, Unit = "kg", IconKey = "biodegradable" },
        //new() { Id = 2, Name = "Egg Shell Waste", Group = WasteGroup.Bio, Unit = "kg", IconKey = "eggshell" },
        //new() { Id = 3, Name = "Animal and Fish Waste", Group = WasteGroup.Bio, Unit = "kg", IconKey = "animal-fish" },
        //new() { Id = 4, Name = "Hard Organics", Group = WasteGroup.Bio, Unit = "kg", IconKey = "hard-organics" },
        //new() { Id = 5, Name = "Fallen Leaves and Twigs", Group = WasteGroup.Bio, Unit = "kg", IconKey = "leaves" },
        //new() { Id = 6, Name = "Used Oil", Group = WasteGroup.Bio, Unit = "litre", IconKey = "used-oil" },
        new() { Id = 7, Name = "Plastic Waste", Group = WasteGroup.NonBio, Unit = "kg", IconKey = "plastic" },
        new() { Id = 8, Name = "Paper and Cardboard", Group = WasteGroup.NonBio, Unit = "kg", IconKey = "paper" },
        new() { Id = 9, Name = "Thermocol and Rubber", Group = WasteGroup.NonBio, Unit = "kg", IconKey = "thermocol-rubber" },
        new() { Id = 10, Name = "Textiles", Group = WasteGroup.NonBio, Unit = "kg", IconKey = "textiles" },
        new() { Id = 11, Name = "Metal and Glass", Group = WasteGroup.NonBio, Unit = "kg", IconKey = "metal-glass" },
        new() { Id = 12, Name = "E-Waste", Group = WasteGroup.NonBio, Unit = "piece", IconKey = "e-waste" },
        new() { Id = 13, Name = "Construction Waste", Group = WasteGroup.NonBio, Unit = "kg", IconKey = "construction" }
    ];
}

public sealed class EfUserRepository(GreenAgeDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id) => db.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Id == id);
    public Task<User?> GetByEmailAsync(string email) => db.Users.FirstOrDefaultAsync(user => user.Email == email);
    public async Task<IReadOnlyList<User>> GetAllAsync() => await db.Users.AsNoTracking().ToListAsync();
    public async Task AddAsync(User user) { db.Users.Add(user); await db.SaveChangesAsync(); }
    public async Task UpdateAsync(User user) { db.Users.Update(user); await db.SaveChangesAsync(); }
}

public sealed class EfCategoryRepository(GreenAgeDbContext db) : ICategoryRepository
{
    public async Task<IReadOnlyList<WasteCategory>> GetAllAsync() => await db.Categories.AsNoTracking().OrderBy(category => category.Id).ToListAsync();
    public Task<WasteCategory?> GetByIdAsync(int id) => db.Categories.AsNoTracking().FirstOrDefaultAsync(category => category.Id == id);
}

public sealed class EfPickupRequestRepository(GreenAgeDbContext db) : IPickupRequestRepository
{
    private IQueryable<PickupRequest> WithItems() => db.Requests.Include(request => request.Items);
    public Task<PickupRequest?> GetByIdAsync(Guid id) => WithItems().AsNoTracking().FirstOrDefaultAsync(request => request.Id == id);
    public async Task<IReadOnlyList<PickupRequest>> GetAllAsync() => await WithItems().AsNoTracking().ToListAsync();
    public async Task<IReadOnlyList<PickupRequest>> GetByDeliverAsync(Guid userId) => await WithItems().AsNoTracking().Where(request => request.DeliverUserId == userId).ToListAsync();
    public async Task<IReadOnlyList<PickupRequest>> GetByCollectorAsync(Guid collectorId) => await WithItems().AsNoTracking().Where(request => request.CollectorId == collectorId).ToListAsync();
    public async Task<IReadOnlyList<PickupRequest>> GetOpenAsync() => await WithItems().AsNoTracking().Where(request => request.Status == RequestStatus.Requested).ToListAsync();
    public async Task AddAsync(PickupRequest request) { db.Requests.Add(request); await db.SaveChangesAsync(); }
    public async Task UpdateAsync(PickupRequest request) { db.Requests.Update(request); await db.SaveChangesAsync(); }
    public async Task<bool> TryAcceptAsync(Guid requestId, Guid collectorId)
    {
        var acceptedAt = DateTime.UtcNow;
        return await db.Requests.Where(request => request.Id == requestId && request.Status == RequestStatus.Requested)
            .ExecuteUpdateAsync(update => update.SetProperty(request => request.Status, RequestStatus.Accepted)
                .SetProperty(request => request.CollectorId, (Guid?)collectorId)
                .SetProperty(request => request.AcceptedAt, (DateTime?)acceptedAt)) == 1;
    }
}

public sealed class EfNotificationRepository(GreenAgeDbContext db) : INotificationRepository
{
    public async Task AddAsync(Notification notification) { db.Notifications.Add(notification); await db.SaveChangesAsync(); }
    public async Task<IReadOnlyList<Notification>> GetByUserAsync(Guid userId) => await db.Notifications.AsNoTracking().Where(notification => notification.UserId == userId).OrderByDescending(notification => notification.CreatedAt).ToListAsync();
    public async Task MarkReadAsync(Guid id, Guid userId) => await db.Notifications.Where(notification => notification.Id == id && notification.UserId == userId).ExecuteUpdateAsync(update => update.SetProperty(notification => notification.IsRead, true));
}

public sealed class EfSosRepository(GreenAgeDbContext db) : ISosRepository
{
    public async Task AddAsync(SosAlert alert) { db.SosAlerts.Add(alert); await db.SaveChangesAsync(); }
}