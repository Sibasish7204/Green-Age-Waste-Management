using System.Text.Json;
using System.Text.Json.Serialization;

namespace GreenAge.Infrastructure;

internal sealed class JsonCollection<T, TKey>(string filePath, Func<T, TKey> keySelector) where TKey : notnull
{
    private static readonly JsonSerializerOptions Options = CreateOptions();
    private readonly SemaphoreSlim gate = new(1, 1);
    private List<T>? values;

    public async Task<IReadOnlyList<T>> GetAllAsync()
    {
        await gate.WaitAsync();
        try
        {
            await LoadAsync();
            return Clone(values!);
        }
        finally { gate.Release(); }
    }

    public async Task AddAsync(T value)
    {
        await gate.WaitAsync();
        try
        {
            await LoadAsync();
            values!.Add(value);
            await SaveAsync();
        }
        finally { gate.Release(); }
    }

    public async Task UpdateAsync(T value)
    {
        await gate.WaitAsync();
        try
        {
            await LoadAsync();
            var index = values!.FindIndex(existing => EqualityComparer<TKey>.Default.Equals(keySelector(existing), keySelector(value)));
            if (index < 0) throw new InvalidOperationException("Record not found.");
            values[index] = value;
            await SaveAsync();
        }
        finally { gate.Release(); }
    }

    public async Task<TResult> MutateAsync<TResult>(Func<List<T>, TResult> mutation)
    {
        await gate.WaitAsync();
        try
        {
            await LoadAsync();
            var result = mutation(values!);
            await SaveAsync();
            return result;
        }
        finally { gate.Release(); }
    }

    public Task SeedIfEmptyAsync(IEnumerable<T> seed) => MutateAsync(values =>
    {
        if (values.Count == 0) values.AddRange(seed);
        return true;
    });

    private async Task LoadAsync()
    {
        if (values is not null) return;
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        if (!File.Exists(filePath))
        {
            values = [];
            return;
        }
        await using var stream = File.OpenRead(filePath);
        values = await JsonSerializer.DeserializeAsync<List<T>>(stream, Options) ?? [];
    }

    private async Task SaveAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, values, Options);
    }

    private static List<T> Clone(IEnumerable<T> source) => JsonSerializer.Deserialize<List<T>>(JsonSerializer.Serialize(source, Options), Options) ?? [];

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}