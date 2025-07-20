using Microsoft.JSInterop;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services;

public interface IDatabasePersistenceService
{
    Task SaveDatabaseAsync(byte[] databaseData);
    Task<byte[]?> LoadDatabaseAsync();
    Task ClearDatabaseAsync();
    Task<bool> DatabaseExistsAsync();
}

public class DatabasePersistenceService : IDatabasePersistenceService
{
    private readonly IJSRuntime _jsRuntime;

    public DatabasePersistenceService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task SaveDatabaseAsync(byte[] databaseData)
    {
        await _jsRuntime.InvokeVoidAsync("DatabaseStorage.saveDatabase", databaseData);
    }

    public async Task<byte[]?> LoadDatabaseAsync()
    {
        var result = await _jsRuntime.InvokeAsync<byte[]?>("DatabaseStorage.loadDatabase");
        return result;
    }

    public async Task ClearDatabaseAsync()
    {
        await _jsRuntime.InvokeVoidAsync("DatabaseStorage.clearDatabase");
    }

    public async Task<bool> DatabaseExistsAsync()
    {
        return await _jsRuntime.InvokeAsync<bool>("DatabaseStorage.databaseExists");
    }
}