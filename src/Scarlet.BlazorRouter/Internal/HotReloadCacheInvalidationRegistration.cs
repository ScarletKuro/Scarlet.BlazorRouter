using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Scarlet.BlazorRouter;

internal sealed class HotReloadCacheInvalidationRegistration : IDisposable
{
    private const string HotReloadManagerTypeName = "Microsoft.AspNetCore.Components.HotReload.HotReloadManager, Microsoft.AspNetCore.Components";

    private readonly object? _instance;
    private readonly Action? _delegate;

    private HotReloadCacheInvalidationRegistration(object? instance, Action? @delegate)
    {
        _instance = instance;
        _delegate = @delegate;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Hot reload integration is optional and degrades to a no-op when the internal ASP.NET Core hot reload manager type is unavailable after trimming.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Hot reload integration reflects only the internal singleton field that UnsafeAccessorType cannot currently express for inaccessible field returns.")]
    public static HotReloadCacheInvalidationRegistration Create(Action callback)
    {
        var hotReloadManagerType = typeof(NavigationManager).Assembly.GetType(
            "Microsoft.AspNetCore.Components.HotReload.HotReloadManager",
            throwOnError: false);

        if (hotReloadManagerType is null)
        {
            return new HotReloadCacheInvalidationRegistration(null, null);
        }

        var defaultField = hotReloadManagerType.GetField("Default", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (defaultField?.GetValue(null) is not { } instance)
        {
            return new HotReloadCacheInvalidationRegistration(null, null);
        }

        if (!HotReloadManagerAccessors.GetMetadataUpdateSupported(instance))
        {
            return new HotReloadCacheInvalidationRegistration(null, null);
        }

        HotReloadManagerAccessors.AddOnDeltaApplied(instance, callback);
        return new HotReloadCacheInvalidationRegistration(instance, callback);
    }

    public void Dispose()
    {
        if (_instance is not null && _delegate is not null)
        {
            HotReloadManagerAccessors.RemoveOnDeltaApplied(_instance, _delegate);
        }
    }

    private static class HotReloadManagerAccessors
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_MetadataUpdateSupported")]
        internal static extern bool GetMetadataUpdateSupported([UnsafeAccessorType(HotReloadManagerTypeName)] object manager);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "add_OnDeltaApplied")]
        internal static extern void AddOnDeltaApplied([UnsafeAccessorType(HotReloadManagerTypeName)] object manager, Action callback);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "remove_OnDeltaApplied")]
        internal static extern void RemoveOnDeltaApplied([UnsafeAccessorType(HotReloadManagerTypeName)] object manager, Action callback);
    }
}
