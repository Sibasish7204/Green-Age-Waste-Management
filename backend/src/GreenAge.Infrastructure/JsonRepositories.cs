using GreenAge.Core;

namespace GreenAge.Infrastructure;

public sealed class JsonUserRepository(string dataDirectory) : IUserRepository
{
    private readonly JsonCollection<User, Guid> store = new(Path.Combine(dataDirectory, "users.json"), user => user.Id);
    public async Task<User?> GetByIdAsync(Guid id) => (await store.GetAllAsync()).FirstOrDefault(user => user.Id == id);
    public async Task<User?> GetByEmailAsync(string email) => (await store.GetAllAsync()).FirstOrDefault(user => user.Email == email);
    public Task<IReadOnlyList<User>> GetAllAsync() => store.GetAllAsync();
    public Task AddAsync(User user) => store.AddAsync(user);
    public Task UpdateAsync(User user) => store.UpdateAsync(user);
}

public sealed class JsonCategoryRepository(string dataDirectory) : ICategoryRepository
{
    private readonly JsonCollection<WasteCategory, int> store = new(Path.Combine(dataDirectory, "categories.json"), category => category.Id);
    public Task InitializeAsync() => store.SeedIfEmptyAsync(Seed);
    public Task<IReadOnlyList<WasteCategory>> GetAllAsync() => store.GetAllAsync();
    public async Task<WasteCategory?> GetByIdAsync(int id) => (await store.GetAllAsync()).FirstOrDefault(category => category.Id == id);

    private static readonly WasteCategory[] Seed =
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

public sealed class JsonPickupRequestRepository(string dataDirectory) : IPickupRequestRepository
{
    private readonly JsonCollection<PickupRequest, Guid> store = new(Path.Combine(dataDirectory, "requests.json"), request => request.Id);
    public async Task<PickupRequest?> GetByIdAsync(Guid id) => (await store.GetAllAsync()).FirstOrDefault(request => request.Id == id);
    public Task<IReadOnlyList<PickupRequest>> GetAllAsync() => store.GetAllAsync();
    public async Task<IReadOnlyList<PickupRequest>> GetByDeliverAsync(Guid userId) => (await store.GetAllAsync()).Where(request => request.DeliverUserId == userId).ToList();
    public async Task<IReadOnlyList<PickupRequest>> GetByCollectorAsync(Guid collectorId) => (await store.GetAllAsync()).Where(request => request.CollectorId == collectorId).ToList();
    public async Task<IReadOnlyList<PickupRequest>> GetOpenAsync() => (await store.GetAllAsync()).Where(request => request.Status == RequestStatus.Requested).ToList();
    public Task AddAsync(PickupRequest request) => store.AddAsync(request);
    public Task UpdateAsync(PickupRequest request) => store.UpdateAsync(request);
    public Task<bool> TryAcceptAsync(Guid requestId, Guid collectorId) => store.MutateAsync(requests =>
    {
        var request = requests.FirstOrDefault(candidate => candidate.Id == requestId);
        if (request is null || request.Status != RequestStatus.Requested) return false;
        request.Status = RequestStatus.Accepted;
        request.CollectorId = collectorId;
        request.AcceptedAt = DateTime.UtcNow;
        return true;
    });
}

public sealed class JsonNotificationRepository(string dataDirectory) : INotificationRepository
{
    private readonly JsonCollection<Notification, Guid> store = new(Path.Combine(dataDirectory, "notifications.json"), notification => notification.Id);
    public Task AddAsync(Notification notification) => store.AddAsync(notification);
    public async Task<IReadOnlyList<Notification>> GetByUserAsync(Guid userId) => (await store.GetAllAsync()).Where(notification => notification.UserId == userId).OrderByDescending(notification => notification.CreatedAt).ToList();
    public Task MarkReadAsync(Guid id, Guid userId) => store.MutateAsync(notifications =>
    {
        var notification = notifications.FirstOrDefault(candidate => candidate.Id == id && candidate.UserId == userId);
        if (notification is not null) notification.IsRead = true;
        return true;
    });
}

public sealed class JsonSosRepository(string dataDirectory) : ISosRepository
{
    private readonly JsonCollection<SosAlert, Guid> store = new(Path.Combine(dataDirectory, "sos.json"), alert => alert.Id);
    public Task AddAsync(SosAlert alert) => store.AddAsync(alert);
}