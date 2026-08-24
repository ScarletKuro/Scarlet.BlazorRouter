using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Scarlet.BlazorRouter;

public sealed partial class BlazorRouter : IComponent, IHandleAfterRender, IDisposable
{
    private RenderHandle _renderHandle;
    private string _baseUri = string.Empty;
    private string _locationAbsolute = string.Empty;
    private bool _navigationInterceptionEnabled;
    private bool _updateScrollPositionForHash;
    private string? _updateScrollPositionForHashLastLocation;
    private CancellationTokenSource? _onNavigateCts;
    private Task _previousOnNavigateTask = Task.CompletedTask;
    private bool _onNavigateCalled;
    private ExplicitRouteTableKey _routeTableLastBuiltForKey;
    private ExplicitRouteTable? _routeTable;
    private INavigationInterception? _navigationInterception;
    private IScrollToLocationHash? _scrollToLocationHash;
    private IRoutingStateProvider? _routingStateProvider;
    private ILogger<BlazorRouter> _logger = NullLogger<BlazorRouter>.Instance;
    private HotReloadCacheInvalidationRegistration? _hotReloadRegistration;
    private string? _externalNavigationTarget;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = default!;

    [Inject]
    private ILoggerFactory LoggerFactory { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public IReadOnlyList<BlazorRouteDefinition>? Routes { get; set; }

    [Parameter]
    [EditorRequired]
    public RenderFragment<RouteData>? Found { get; set; }

    [Parameter]
    public Type? NotFoundPage { get; set; }

    [Parameter]
    public RenderFragment? Navigating { get; set; }

    [Parameter]
    public EventCallback<BlazorNavigationContext> OnNavigateAsync { get; set; }

    public void Attach(RenderHandle renderHandle)
    {
        _logger = LoggerFactory.CreateLogger<BlazorRouter>();
        _renderHandle = renderHandle;
        _baseUri = NavigationManager.BaseUri;
        _locationAbsolute = NavigationManager.Uri;
        _navigationInterception = ServiceProvider.GetService<INavigationInterception>();
        _scrollToLocationHash = ServiceProvider.GetService<IScrollToLocationHash>();
        _routingStateProvider = ServiceProvider.GetService<IRoutingStateProvider>();
        NavigationManager.LocationChanged += OnLocationChanged;
        _hotReloadRegistration = HotReloadCacheInvalidationRegistration.Create(ClearRouteCaches);
    }

    public async Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (Routes is null)
        {
            throw new InvalidOperationException($"The {nameof(BlazorRouter)} component requires a value for the parameter {nameof(Routes)}.");
        }

        if (Found is null)
        {
            throw new InvalidOperationException($"The {nameof(BlazorRouter)} component requires a value for the parameter {nameof(Found)}.");
        }

        ValidateNotFoundPage();
        RefreshRouteTable();

        if (!_onNavigateCalled)
        {
            _onNavigateCalled = true;
            await RunOnNavigateAsync(NavigationManager.ToBaseRelativePath(_locationAbsolute), isNavigationIntercepted: false);
        }
        else
        {
            Refresh(isNavigationIntercepted: false);
        }
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
        _onNavigateCts?.Cancel();
        _onNavigateCts?.Dispose();
        _hotReloadRegistration?.Dispose();
    }

    private void ValidateNotFoundPage()
    {
        if (NotFoundPage is not null && !typeof(IComponent).IsAssignableFrom(NotFoundPage))
        {
            throw new InvalidOperationException($"The type {NotFoundPage.FullName} does not implement {typeof(IComponent).FullName}.");
        }
    }

    private static ReadOnlySpan<char> TrimQueryOrHash(ReadOnlySpan<char> path)
    {
        var firstIndex = path.IndexOfAny('?', '#');
        return firstIndex < 0 ? path : path[..firstIndex];
    }

    private void RefreshRouteTable()
    {
        var routeKey = new ExplicitRouteTableKey(Routes!);
        if (!routeKey.Equals(_routeTableLastBuiltForKey))
        {
            _routeTable = ExplicitRouteTableCache.Instance.Create(routeKey, Routes!, ServiceProvider);
            _routeTableLastBuiltForKey = routeKey;
        }
    }

    private void ClearRouteCaches()
    {
        ExplicitRouteTableCache.Instance.Clear();
        _routeTableLastBuiltForKey = default;
    }

    internal void Refresh(bool isNavigationIntercepted)
    {
        if (_previousOnNavigateTask.Status != TaskStatus.RanToCompletion)
        {
            if (Navigating is not null)
            {
                _renderHandle.Render(Navigating);
            }

            return;
        }

        var relativePath = NavigationManager.ToBaseRelativePath(_locationAbsolute);
        var locationPath = $"/{TrimQueryOrHash(relativePath.AsSpan())}";

        RefreshRouteTable();

        if (TryGetRouteDataFromRoutingStateProvider(locationPath, out var prerenderedRouteData))
        {
            Log.NavigatingToComponent(_logger, prerenderedRouteData.PageType, locationPath, _baseUri);
            _renderHandle.Render(Found!(prerenderedRouteData));
            return;
        }

        if (_routeTable!.TryMatch(locationPath, out var routeData))
        {
            Log.NavigatingToComponent(_logger, routeData.PageType, locationPath, _baseUri);
            _renderHandle.Render(Found!(routeData));

            if (!string.Equals(relativePath, _updateScrollPositionForHashLastLocation, StringComparison.Ordinal))
            {
                _updateScrollPositionForHashLastLocation = relativePath;
                _updateScrollPositionForHash = true;
            }
        }
        else
        {
            if (!isNavigationIntercepted)
            {
                Log.DisplayingNotFound(_logger, locationPath, _baseUri);
                RenderNotFound();
            }
            else
            {
                Log.NavigatingToExternalUri(_logger, _locationAbsolute, locationPath, _baseUri);
                _externalNavigationTarget = _locationAbsolute;
                NavigationManager.NavigateTo(_locationAbsolute, forceLoad: true);
            }
        }
    }

    private bool TryGetRouteDataFromRoutingStateProvider(string locationPath, out RouteData routeData)
    {
        if (_routingStateProvider?.RouteData is { } endpointRouteData &&
            _routeTable!.TryProcessRouteData(endpointRouteData, out routeData))
        {
            return true;
        }

        routeData = default!;
        return false;
    }

    internal async ValueTask RunOnNavigateAsync(string path, bool isNavigationIntercepted)
    {
        _onNavigateCts?.Cancel();
        await _previousOnNavigateTask;

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _previousOnNavigateTask = completion.Task;

        _onNavigateCts = new CancellationTokenSource();
        var navigationContext = new BlazorNavigationContext(path, _onNavigateCts.Token);
        var cancellation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellationRegistration = navigationContext.CancellationToken.Register(
            static state => ((TaskCompletionSource)state!).SetResult(),
            cancellation);

        try
        {
            if (!OnNavigateAsync.HasDelegate)
            {
                completion.TrySetResult();
                Refresh(isNavigationIntercepted);
                return;
            }

            if (Navigating is not null)
            {
                _renderHandle.Render(Navigating);
            }

            var task = await Task.WhenAny(OnNavigateAsync.InvokeAsync(navigationContext), cancellation.Task);
            await task;

            completion.TrySetResult();
            Refresh(isNavigationIntercepted);
        }
        catch (OperationCanceledException) when (_onNavigateCts?.IsCancellationRequested == true)
        {
            completion.TrySetResult();
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
            _renderHandle.Render(_ => ExceptionDispatchInfo.Throw(exception));
        }
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        if (string.Equals(args.Location, _externalNavigationTarget, StringComparison.Ordinal))
        {
            _externalNavigationTarget = null;
            return;
        }

        _locationAbsolute = args.Location;
        if (_renderHandle.IsInitialized && Routes is not null)
        {
            _ = RunOnNavigateAsync(NavigationManager.ToBaseRelativePath(_locationAbsolute), args.IsNavigationIntercepted);
        }
    }

    private void RenderNotFound()
    {
        _renderHandle.Render(builder =>
        {
            if (NotFoundPage is not null)
            {
                builder.OpenComponent(0, NotFoundPage);
                builder.CloseComponent();
                return;
            }

            builder.AddContent(1, "Not found");
        });
    }

    async Task IHandleAfterRender.OnAfterRenderAsync()
    {
        if (!_navigationInterceptionEnabled && _navigationInterception is not null)
        {
            _navigationInterceptionEnabled = true;
            await _navigationInterception.EnableNavigationInterceptionAsync();
        }

        if (_updateScrollPositionForHash && _scrollToLocationHash is not null)
        {
            _updateScrollPositionForHash = false;
            await _scrollToLocationHash.RefreshScrollPositionForHash(_locationAbsolute);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Displaying NotFound because path '{Path}' with base URI '{BaseUri}' does not match any component route", EventName = "DisplayingNotFound")]
        internal static partial void DisplayingNotFound(ILogger logger, string path, string baseUri);

        [LoggerMessage(2, LogLevel.Debug, "Navigating to component {ComponentType} in response to path '{Path}' with base URI '{BaseUri}'", EventName = "NavigatingToComponent")]
        internal static partial void NavigatingToComponent(ILogger logger, Type componentType, string path, string baseUri);

        [LoggerMessage(3, LogLevel.Debug, "Navigating to non-component URI '{ExternalUri}' in response to path '{Path}' with base URI '{BaseUri}'", EventName = "NavigatingToExternalUri")]
        internal static partial void NavigatingToExternalUri(ILogger logger, string externalUri, string path, string baseUri);
    }
}
