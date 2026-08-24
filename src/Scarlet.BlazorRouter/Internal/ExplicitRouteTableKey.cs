namespace Scarlet.BlazorRouter;

internal readonly struct ExplicitRouteTableKey : IEquatable<ExplicitRouteTableKey>
{
    private readonly RouteKeyEntry[] _entries;

    public ExplicitRouteTableKey(IReadOnlyList<BlazorRouteDefinition> routes)
    {
        _entries = new RouteKeyEntry[routes.Count];
        for (var index = 0; index < routes.Count; index++)
        {
            var route = routes[index];
            _entries[index] = new RouteKeyEntry(route.Template, route.PageType);
        }
    }

    public bool Equals(ExplicitRouteTableKey other)
    {
        if (_entries is null || other._entries is null)
        {
            return _entries is null && other._entries is null;
        }

        if (_entries.Length != other._entries.Length)
        {
            return false;
        }

        for (var index = 0; index < _entries.Length; index++)
        {
            if (!_entries[index].Equals(other._entries[index]))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is ExplicitRouteTableKey other && Equals(other);

    public override int GetHashCode()
    {
        if (_entries is null)
        {
            return 0;
        }

        var hash = new HashCode();
        for (var index = 0; index < _entries.Length; index++)
        {
            hash.Add(_entries[index]);
        }

        return hash.ToHashCode();
    }

    private readonly record struct RouteKeyEntry(string Template, Type PageType)
    {
        public bool Equals(RouteKeyEntry other) =>
            PageType == other.PageType &&
            string.Equals(Template, other.Template, StringComparison.OrdinalIgnoreCase);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(PageType);
            hash.Add(Template, StringComparer.OrdinalIgnoreCase);
            return hash.ToHashCode();
        }
    }
}
