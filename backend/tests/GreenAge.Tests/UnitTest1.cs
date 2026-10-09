using GreenAge.Core;
using GreenAge.Services;

namespace GreenAge.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task RegisterRejectsDuplicateEmail()
    {
        var users = new MemoryUserRepository();
        var service = new AuthService(users);
        await service.RegisterAsync("First User", "same@example.com", "DemoPass1!", null, UserRole.Deliver);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.RegisterAsync("Second User", "SAME@example.com", "DemoPass1!", null, UserRole.Collect));

        Assert.Equal(409, exception.StatusCode);
    }

    private sealed class MemoryUserRepository : IUserRepository
    {
        private readonly List<User> users = [];
        public Task<User?> GetByIdAsync(Guid id) => Task.FromResult(users.FirstOrDefault(user => user.Id == id));
        public Task<User?> GetByEmailAsync(string email) => Task.FromResult(users.FirstOrDefault(user => user.Email == email));
        public Task<IReadOnlyList<User>> GetAllAsync() => Task.FromResult<IReadOnlyList<User>>(users);
        public Task AddAsync(User user) { users.Add(user); return Task.CompletedTask; }
        public Task UpdateAsync(User user) { return Task.CompletedTask; }
    }
}