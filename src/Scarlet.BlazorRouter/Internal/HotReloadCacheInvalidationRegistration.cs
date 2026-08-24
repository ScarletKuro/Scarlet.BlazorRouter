using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Scarlet.BlazorRouter;

internal sealed class HotReloadCacheInvalidationRegistration : IDisposable
{
    private readonly object? _instance;
    private readonly EventInfo? _eventInfo;
    private readonly Delegate? _delegate;

    private HotReloadCacheInvalidationRegistration(object? instance, EventInfo? eventInfo, Delegate? @delegate)
    {
        _instance = instance;
        _eventInfo = eventInfo;
        _delegate = @delegate;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Hot reload integration is optional and degrades to a no-op when the internal ASP.NET Core hot reload manager type is unavailable after trimming.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Hot reload integration is optional and degrades to a no-op when the internal ASP.NET Core hot reload manager members are unavailable after trimming.")]
    public static HotReloadCacheInvalidationRegistration Create(Action callback)
    {
        var hotReloadManagerType = typeof(NavigationManager).Assembly.GetType(
            "Microsoft.AspNetCore.Components.HotReload.HotReloadManager",
            throwOnError: false);

        if (hotReloadManagerType is null)
        {
            return new HotReloadCacheInvalidationRegistration(null, null, null);
        }

        var isSupportedProperty = hotReloadManagerType.GetProperty("IsSupported", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (isSupportedProperty?.GetValue(null) is not bool isSupported || !isSupported)
        {
            return new HotReloadCacheInvalidationRegistration(null, null, null);
        }

        var defaultProperty = hotReloadManagerType.GetProperty("Default", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        var instance = defaultProperty?.GetValue(null);
        var eventInfo = hotReloadManagerType.GetEvent("OnDeltaApplied", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (instance is null || eventInfo?.EventHandlerType is null)
        {
            return new HotReloadCacheInvalidationRegistration(null, null, null);
        }

        var @delegate = Delegate.CreateDelegate(eventInfo.EventHandlerType, callback.Target, callback.Method);
        eventInfo.AddEventHandler(instance, @delegate);
        return new HotReloadCacheInvalidationRegistration(instance, eventInfo, @delegate);
    }

    public void Dispose()
    {
        if (_instance is not null && _eventInfo is not null && _delegate is not null)
        {
            _eventInfo.RemoveEventHandler(_instance, _delegate);
        }
    }
}
