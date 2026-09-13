namespace TenantCore.Infrastructure.Caching;

/// <summary>
/// Holds the current snapshot of a background-refreshed dataset with no expiration of its own.
/// <see cref="Current"/> is null only until the first successful refresh; after that it always
/// holds the last fully-built snapshot — a refresh replaces it with one atomic reference swap,
/// so readers never see a partial, empty, or "expired" value.
/// </summary>
public sealed class RefreshableCache<T>
{
    private volatile IReadOnlyList<T>? _items;

    public IReadOnlyList<T>? Current => _items;

    public void Set(IReadOnlyList<T> items) => _items = items;
}
