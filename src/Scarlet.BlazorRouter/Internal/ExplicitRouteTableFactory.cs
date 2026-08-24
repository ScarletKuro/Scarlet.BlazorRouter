using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Scarlet.BlazorRouter;

/// <summary>
/// Builds the tree router that backs <see cref="ExplicitRouteTable"/>.
/// </summary>
/// <remarks>
/// This mirrors <c>Microsoft.AspNetCore.Components.RouteTableFactory</c>, except that the templates come from the
/// explicitly supplied <see cref="BlazorRouteDefinition"/> list rather than from a <c>[Route]</c> attribute scan.
/// </remarks>
internal static class ExplicitRouteTableFactory
{
    public static ExplicitRouteTable Create(IReadOnlyList<BlazorRouteDefinition> routes, IServiceProvider serviceProvider)
    {
        var routeOptions = Options.Create(new RouteOptions());
        if (!OperatingSystem.IsBrowser() || RegexConstraintSupport.IsEnabled)
        {
            routeOptions.Value.SetParameterPolicy("regex", typeof(RegexInlineRouteConstraint));
        }

        var builder = new TreeRouteBuilder(
            serviceProvider.GetRequiredService<ILoggerFactory>(),
            new DefaultInlineConstraintResolver(routeOptions, serviceProvider));

        // A page can be reached through several templates, and Blazor passes null for any parameter the matched
        // template does not supply. So parse everything first to learn the full parameter set per page type, then map.
        var parsedRoutes = new List<(BlazorRouteDefinition Route, RoutePattern Pattern, HashSet<string> ParameterNames)>(routes.Count);
        var allParameterNamesByPageType = new Dictionary<Type, HashSet<string>>();

        foreach (var route in routes)
        {
            ValidateRoute(route);

            var pattern = RoutePatternParser.Parse(route.Template);
            var parameterNames = GetParameterNames(pattern);
            parsedRoutes.Add((route, pattern, parameterNames));

            if (!allParameterNamesByPageType.TryGetValue(route.PageType, out var allParameterNames))
            {
                allParameterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                allParameterNamesByPageType[route.PageType] = allParameterNames;
            }

            allParameterNames.UnionWith(parameterNames);
        }

        var entriesByRoute = new Dictionary<ExplicitRouteTable.EntryKey, InboundRouteEntry>();

        foreach (var (route, pattern, parameterNames) in parsedRoutes)
        {
            // route.PageType carries the DynamicallyAccessedMembers annotation MapInbound requires; passing it
            // directly (rather than via a Dictionary key) is what keeps the trim analyzer satisfied.
            var entry = builder.MapInbound(
                route.PageType,
                pattern,
                GetUnusedParameterNames(allParameterNamesByPageType[route.PageType], parameterNames));

            entriesByRoute[new ExplicitRouteTable.EntryKey(route.PageType, route.Template)] = entry;
        }

        DetectAmbiguousRoutes(builder);

        return new ExplicitRouteTable(builder.Build(), entriesByRoute);
    }

    private static void ValidateRoute(BlazorRouteDefinition route)
    {
        if (string.IsNullOrWhiteSpace(route.Template))
        {
            throw new InvalidOperationException("Route templates must be non-empty.");
        }

        if (!typeof(IComponent).IsAssignableFrom(route.PageType))
        {
            throw new InvalidOperationException($"The type {route.PageType.FullName} does not implement {typeof(IComponent).FullName}.");
        }
    }

    private static HashSet<string> GetParameterNames(RoutePattern pattern)
    {
        var parameterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in pattern.Parameters)
        {
            parameterNames.Add(parameter.Name);
        }

        return parameterNames;
    }

    private static List<string>? GetUnusedParameterNames(HashSet<string> allRouteParameterNames, HashSet<string> routeParameterNames)
    {
        List<string>? unusedParameters = null;
        foreach (var name in allRouteParameterNames)
        {
            if (!routeParameterNames.Contains(name))
            {
                unusedParameters ??= [];
                unusedParameters.Add(name);
            }
        }

        return unusedParameters;
    }

    private static void DetectAmbiguousRoutes(TreeRouteBuilder builder)
    {
        var seen = new HashSet<InboundRouteEntry>(InboundRouteEntryAmbiguityEqualityComparer.Instance);
        seen.EnsureCapacity(builder.InboundEntries.Count);

        for (var index = 0; index < builder.InboundEntries.Count; index++)
        {
            var current = builder.InboundEntries[index];
            if (seen.Add(current))
            {
                continue;
            }

            seen.TryGetValue(current, out var existing);
            throw new InvalidOperationException(
                $"""
                The following routes are ambiguous:
                '{existing!.RoutePattern.RawText!.Trim('/')}' in '{existing.Handler.FullName}'
                '{current.RoutePattern.RawText!.Trim('/')}' in '{current.Handler.FullName}'
                """);
        }
    }

    /// <summary>
    /// Two routes are ambiguous when they have the same precedence and their literal segments match case-insensitively,
    /// which means neither could ever be selected deterministically over the other.
    /// </summary>
    private sealed class InboundRouteEntryAmbiguityEqualityComparer : IEqualityComparer<InboundRouteEntry>
    {
        public static InboundRouteEntryAmbiguityEqualityComparer Instance { get; } = new();

        public bool Equals(InboundRouteEntry? x, InboundRouteEntry? y)
        {
            if (x is null)
            {
                return y is null;
            }

            if (y is null || x.Precedence != y.Precedence)
            {
                return false;
            }

            for (var segmentIndex = 0; segmentIndex < x.RoutePattern.PathSegments.Count; segmentIndex++)
            {
                var leftSegment = x.RoutePattern.PathSegments[segmentIndex];
                var rightSegment = y.RoutePattern.PathSegments[segmentIndex];
                if (leftSegment.Parts.Count != rightSegment.Parts.Count)
                {
                    return false;
                }

                for (var partIndex = 0; partIndex < leftSegment.Parts.Count; partIndex++)
                {
                    if (leftSegment.Parts[partIndex] is RoutePatternLiteralPart leftLiteral &&
                        rightSegment.Parts[partIndex] is RoutePatternLiteralPart rightLiteral &&
                        !string.Equals(leftLiteral.Content, rightLiteral.Content, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Only ever called by HashSet with non-null entries.")]
        public int GetHashCode(InboundRouteEntry obj)
        {
            var hash = new HashCode();
            hash.Add(obj.Precedence);

            for (var segmentIndex = 0; segmentIndex < obj.RoutePattern.PathSegments.Count; segmentIndex++)
            {
                var segment = obj.RoutePattern.PathSegments[segmentIndex];
                for (var partIndex = 0; partIndex < segment.Parts.Count; partIndex++)
                {
                    if (segment.Parts[partIndex] is RoutePatternLiteralPart literal)
                    {
                        hash.Add(literal.Content, StringComparer.OrdinalIgnoreCase);
                    }
                }
            }

            return hash.ToHashCode();
        }
    }
}
