using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Components;

namespace Scarlet.BlazorRouter;

internal static class ExplicitRouteTableFactory
{
    public static ExplicitRouteTable Create(IReadOnlyList<BlazorRouteDefinition> routes, IServiceProvider serviceProvider)
    {
        var resolver = CreateConstraintResolver(serviceProvider);
        var unusedRouteParameterNames = BuildUnusedRouteParameterMap(routes);
        var entries = new List<ExplicitRouteTable.ExplicitRouteEntry>(routes.Count);

        foreach (var route in routes)
        {
            ValidateRoute(route);

            var template = TemplateParser.Parse(route.Template);
            var constraints = BuildConstraints(template, resolver);
            var precedence = RoutePrecedence.ComputeInbound(template);
            entries.Add(new ExplicitRouteTable.ExplicitRouteEntry(
                route,
                template,
                precedence,
                constraints,
                unusedRouteParameterNames[(route.PageType, route.Template)]));
        }

        DetectAmbiguousRoutes(entries);
        entries.Sort(ExplicitRouteEntryComparer.Instance);

        return new ExplicitRouteTable(entries);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The router only registers the built-in regex inline constraint mapping and does not enumerate or add arbitrary user-supplied constraint types.")]
    private static DefaultInlineConstraintResolver CreateConstraintResolver(IServiceProvider serviceProvider)
    {
        var routeOptions = new RouteOptions();
        if (!OperatingSystem.IsBrowser())
        {
            routeOptions.ConstraintMap["regex"] = typeof(RegexInlineRouteConstraint);
        }

        var options = Options.Create(routeOptions);

        var resolverType = typeof(DefaultInlineConstraintResolver);
        var twoArgumentConstructor = resolverType.GetConstructor([typeof(IOptions<RouteOptions>), typeof(IServiceProvider)]);
        if (twoArgumentConstructor is not null)
        {
            return (DefaultInlineConstraintResolver)twoArgumentConstructor.Invoke([options, serviceProvider]);
        }

        var oneArgumentConstructor = resolverType.GetConstructor([typeof(IOptions<RouteOptions>)]);
        if (oneArgumentConstructor is not null)
        {
            return (DefaultInlineConstraintResolver)oneArgumentConstructor.Invoke([options]);
        }

        throw new InvalidOperationException($"Unable to create {resolverType.FullName}.");
    }

    private static Dictionary<(Type PageType, string Template), IReadOnlyList<string>> BuildUnusedRouteParameterMap(
        IReadOnlyList<BlazorRouteDefinition> routes)
    {
        var allParametersByPage = new Dictionary<Type, HashSet<string>>();
        var templateParameters = new List<(BlazorRouteDefinition Route, HashSet<string> Parameters)>(routes.Count);

        foreach (var route in routes)
        {
            ValidateRoute(route);

            var template = TemplateParser.Parse(route.Template);
            var parameterNames = template.Parameters
                .Select(parameter => parameter.Name!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            templateParameters.Add((route, parameterNames));

            if (!allParametersByPage.TryGetValue(route.PageType, out var names))
            {
                names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                allParametersByPage[route.PageType] = names;
            }

            foreach (var parameterName in parameterNames)
            {
                names.Add(parameterName);
            }
        }

        var result = new Dictionary<(Type PageType, string Template), IReadOnlyList<string>>();
        foreach (var item in templateParameters)
        {
            var unusedParameters = allParametersByPage[item.Route.PageType]
                .Where(name => !item.Parameters.Contains(name))
                .ToArray();

            result[(item.Route.PageType, item.Route.Template)] = unusedParameters;
        }

        return result;
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

    private static Dictionary<string, IRouteConstraint> BuildConstraints(
        RouteTemplate template,
        IInlineConstraintResolver resolver)
    {
        var result = new Dictionary<string, IRouteConstraint>(StringComparer.OrdinalIgnoreCase);

        foreach (var parameter in template.Parameters)
        {
            var constraints = new List<IRouteConstraint>();
            foreach (var inlineConstraint in parameter.InlineConstraints)
            {
                var resolved = resolver.ResolveConstraint(inlineConstraint.Constraint);
                if (resolved is null)
                {
                    throw new InvalidOperationException(
                        $"Unable to resolve the constraint '{inlineConstraint.Constraint}' in route '{template.TemplateText}'.");
                }

                constraints.Add(resolved);
            }

            if (constraints.Count == 1)
            {
                result[parameter.Name!] = constraints[0];
            }
            else if (constraints.Count > 1)
            {
                result[parameter.Name!] = new CompositeRouteConstraint(constraints);
            }
        }

        return result;
    }

    private static void DetectAmbiguousRoutes(IReadOnlyList<ExplicitRouteTable.ExplicitRouteEntry> entries)
    {
        var seen = new HashSet<ExplicitRouteTable.ExplicitRouteEntry>(ExplicitRouteAmbiguityComparer.Instance);
        foreach (var current in entries)
        {
            if (seen.Add(current))
            {
                continue;
            }

            var existing = seen.First(entry => ExplicitRouteAmbiguityComparer.Instance.Equals(entry, current));
            throw new InvalidOperationException(
                $"""
                The following routes are ambiguous:
                '{existing.Template.TemplateText!.Trim('/')}' in '{existing.Route.PageType.FullName}'
                '{current.Template.TemplateText!.Trim('/')}' in '{current.Route.PageType.FullName}'
                """);
        }
    }

    private sealed class ExplicitRouteEntryComparer : IComparer<ExplicitRouteTable.ExplicitRouteEntry>
    {
        public static ExplicitRouteEntryComparer Instance { get; } = new();

        public int Compare(ExplicitRouteTable.ExplicitRouteEntry? x, ExplicitRouteTable.ExplicitRouteEntry? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            var result = y.Precedence.CompareTo(x.Precedence);
            return result != 0
                ? result
                : string.Compare(x.Template.TemplateText, y.Template.TemplateText, StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class ExplicitRouteAmbiguityComparer : IEqualityComparer<ExplicitRouteTable.ExplicitRouteEntry>
    {
        public static ExplicitRouteAmbiguityComparer Instance { get; } = new();

        public bool Equals(ExplicitRouteTable.ExplicitRouteEntry? x, ExplicitRouteTable.ExplicitRouteEntry? y)
        {
            if (x is null)
            {
                return y is null;
            }

            if (y is null || x.Precedence != y.Precedence || x.Template.Segments.Count != y.Template.Segments.Count)
            {
                return false;
            }

            for (var segmentIndex = 0; segmentIndex < x.Template.Segments.Count; segmentIndex++)
            {
                var leftSegment = x.Template.Segments[segmentIndex];
                var rightSegment = y.Template.Segments[segmentIndex];
                if (leftSegment.Parts.Count != rightSegment.Parts.Count)
                {
                    return false;
                }

                for (var partIndex = 0; partIndex < leftSegment.Parts.Count; partIndex++)
                {
                    var leftPart = leftSegment.Parts[partIndex];
                    var rightPart = rightSegment.Parts[partIndex];

                    if (leftPart.IsLiteral != rightPart.IsLiteral)
                    {
                        return false;
                    }

                    if (leftPart.IsLiteral &&
                        !string.Equals(leftPart.Text, rightPart.Text, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public int GetHashCode(ExplicitRouteTable.ExplicitRouteEntry obj)
        {
            var hash = new HashCode();
            hash.Add(obj.Precedence);

            for (var segmentIndex = 0; segmentIndex < obj.Template.Segments.Count; segmentIndex++)
            {
                var segment = obj.Template.Segments[segmentIndex];
                for (var partIndex = 0; partIndex < segment.Parts.Count; partIndex++)
                {
                    var part = segment.Parts[partIndex];
                    if (part.IsLiteral)
                    {
                        hash.Add(part.Text, StringComparer.OrdinalIgnoreCase);
                    }
                }
            }

            return hash.ToHashCode();
        }
    }
}
