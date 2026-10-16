namespace GreenAge.Core;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<IReadOnlyList<User>> GetAllAsync();
    Task AddAsync(User user);
    Task UpdateAsync(User user);
}

public interface ICategoryRepository
{
    Task<IReadOnlyList<WasteCategory>> GetAllAsync();
    Task<WasteCategory?> GetByIdAsync(int id);
}

public interface IPickupRequestRepository
{
    Task<PickupRequest?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<PickupRequest>> GetAllAsync();
    Task<IReadOnlyList<PickupRequest>> GetByDeliverAsync(Guid userId);
    Task<IReadOnlyList<PickupRequest>> GetByCollectorAsync(Guid collectorId);
    Task<IReadOnlyList<PickupRequest>> GetOpenAsync();
    Task AddAsync(PickupRequest request);
    Task<bool> TryAcceptAsync(Guid requestId, Guid collectorId);
    Task UpdateAsync(PickupRequest request);
}

public interface INotificationRepository
{
    Task AddAsync(Notification notification);
    Task<IReadOnlyList<Notification>> GetByUserAsync(Guid userId);
    Task MarkReadAsync(Guid id, Guid userId);
}

public interface ISosRepository
{
    Task AddAsync(SosAlert alert);
}

public interface INonBioSaleRepository
{
    Task<NonBioSale?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<NonBioSale>> GetBySellerAsync(Guid sellerId);
    Task AddAsync(NonBioSale sale);
    Task UpdateAsync(NonBioSale sale);
    Task<bool> TryCompleteAsync(Guid id, Guid collectorId, string tokenHash, DateTime now);
}

public interface INonBioOfferRepository
{
    Task<IReadOnlyList<NonBioOffer>> GetAllAsync();
    Task<NonBioOffer?> GetByIdAsync(string id);
}

public interface IBioCollectionRepository
{
    Task AddQrTokenAsync(HouseholdQrToken token);
    Task<HouseholdQrToken?> GetQrTokenAsync(string tokenHash);
    Task<bool> TryRecordVisitAsync(string tokenHash, BioCollectionVisit visit, DateTime now);
    Task<IReadOnlyList<BioCollectionVisit>> GetVisitsByWorkerAsync(Guid workerId);
    Task<BioCollectionVisit?> GetVisitByWorkerAsync(Guid visitId, Guid workerId);
}