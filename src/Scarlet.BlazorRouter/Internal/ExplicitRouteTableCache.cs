using System.Collections.Concurrent;

namespace Scarlet.BlazorRouter;

internal sealed class ExplicitRouteTableCache
{
    public static ExplicitRouteTableCache Instance { get; } = new();

    private readonly ConcurrentDictionary<ExplicitRouteTableKey, ExplicitRouteTable> _cache = new();

    public ExplicitRouteTable Create(
        ExplicitRouteTableKey key,
        IReadOnlyList<BlazorRouteDefinition> routes,
        IServiceProvider serviceProvider)
    {
        if (_cache.TryGetValue(key, out var table))
        {
            return table;
        }

        table = ExplicitRouteTableFactory.Create(routes, serviceProvider);
        _cache.TryAdd(key, table);
        return table;
    }

    public void Clear() => _cache.Clear();
}
