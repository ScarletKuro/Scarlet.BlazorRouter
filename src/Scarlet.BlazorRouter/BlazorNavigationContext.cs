namespace Scarlet.BlazorRouter;

public sealed class BlazorNavigationContext
{
    public BlazorNavigationContext(string path, CancellationToken cancellationToken)
    {
        Path = path;
        CancellationToken = cancellationToken;
    }

    public string Path { get; }

    public CancellationToken CancellationToken { get; }
}
