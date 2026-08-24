using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Tree;

namespace Scarlet.BlazorRouter;

/// <summary>
/// Matches paths against the explicitly supplied route list.
/// </summary>
/// <remarks>
/// This mirrors <c>Microsoft.AspNetCore.Components.Routing.RouteTable</c> so that matching, precedence and parameter
/// conversion behave exactly like the built-in Blazor router.
/// </remarks>
internal sealed class ExplicitRouteTable
{
    private static readonly IReadOnlyDictionary<string, object?> EmptyRouteValues = new Dictionary<string, object?>();

    private readonly TreeRouter _router;
    private readonly Dictionary<EntryKey, InboundRouteEntry> _entriesByRoute;

    public ExplicitRouteTable(TreeRouter router, Dictionary<EntryKey, InboundRouteEntry> entriesByRoute)
    {
        _router = router;
        _entriesByRoute = entriesByRoute;
    }

    public bool TryMatch(string path, out RouteData routeData)
    {
        var routeContext = new RouteContext(path);
        _router.Route(routeContext);

        if (routeContext.Entry is null)
        {
            routeData = default!;
            return false;
        }

        ProcessParameters(routeContext.Entry, routeContext.RouteValues);
        routeData = CreateRouteData(routeContext.Entry, routeContext.RouteValues);
        return true;
    }

    /// <summary>
    /// Re-processes route values produced by server-side endpoint routing during prerendering, so that a route matched
    /// outside the component still gets the same parameter conversions an interactive match would apply.
    /// </summary>
    public bool TryProcessRouteData(RouteData endpointRouteData, out RouteData processedRouteData)
    {
        if (endpointRouteData.Template is null ||
            !_entriesByRoute.TryGetValue(new EntryKey(endpointRouteData.PageType, endpointRouteData.Template), out var entry))
        {
            processedRouteData = default!;
            return false;
        }

        var routeValues = endpointRouteData.RouteValues.Count == 0
            ? new RouteValueDictionary()
            : new RouteValueDictionary(endpointRouteData.RouteValues);

        ProcessParameters(entry, routeValues);
        processedRouteData = CreateRouteData(entry, routeValues);
        return true;
    }

    private static RouteData CreateRouteData(InboundRouteEntry entry, RouteValueDictionary routeValues) =>
        new(entry.Handler, routeValues.Count == 0 ? EmptyRouteValues : routeValues)
        {
            Template = entry.RoutePattern.RawText,
        };

    private static void ProcessParameters(InboundRouteEntry entry, RouteValueDictionary routeValues)
    {
        // Add null values for route parameters this page declares on its other templates but not on this one.
        if (entry.UnusedRouteParameterNames is not null)
        {
            foreach (var parameter in entry.UnusedRouteParameterNames)
            {
                routeValues[parameter] = null;
            }
        }

        foreach (var routeValue in routeValues)
        {
            if (routeValue.Value is string value)
            {
                // At this point the values have already been URL decoded, but we might not have decoded '/' characters,
                // as that can cause issues when routing the request (you wouldn't be able to accept parameters that
                // contained '/'). To be consistent with existing Blazor quirks that used Uri.UnescapeDataString, we
                // replace %2F with /. We don't call Uri.UnescapeDataString here as that would decode other characters
                // we don't want to decode, for example any value that was "double" encoded within the original URL.
                routeValues[routeValue.Key] = value.Replace("%2F", "/", StringComparison.OrdinalIgnoreCase);
            }
        }

        foreach (var parameter in entry.RoutePattern.Parameters)
        {
            // Add null values for optional route parameters that weren't provided.
            if (!routeValues.TryGetValue(parameter.Name, out var parameterValue))
            {
                routeValues.Add(parameter.Name, null);
            }
            else if (parameter.ParameterPolicies.Count > 0 && !parameter.IsCatchAll)
            {
                // If the parameter has some well-known set of route constraints, convert the value to the target type.
                for (var index = 0; index < parameter.ParameterPolicies.Count; index++)
                {
                    switch (parameter.ParameterPolicies[index].Content)
                    {
                        case "bool":
                            routeValues[parameter.Name] = bool.Parse((string)parameterValue!);
                            break;
                        case "datetime":
                            routeValues[parameter.Name] = DateTime.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                            break;
                        case "decimal":
                            routeValues[parameter.Name] = decimal.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                            break;
                        case "double":
                            routeValues[parameter.Name] = double.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                            break;
                        case "float":
                            routeValues[parameter.Name] = float.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                            break;
                        case "guid":
                            routeValues[parameter.Name] = Guid.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                            break;
                        case "int":
                            routeValues[parameter.Name] = int.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                            break;
                        case "long":
                            routeValues[parameter.Name] = long.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                            break;
                        default:
                            continue;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Identifies a single (page type, template) pair. Templates are compared case-insensitively to match how the
    /// server-side endpoint reports them during prerendering.
    /// </summary>
    internal readonly record struct EntryKey(Type PageType, string Template)
    {
        public bool Equals(EntryKey other) =>
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
