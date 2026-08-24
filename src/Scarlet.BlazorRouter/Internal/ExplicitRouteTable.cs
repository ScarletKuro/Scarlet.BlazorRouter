using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Logging.Abstractions;

namespace Scarlet.BlazorRouter;

internal sealed class ExplicitRouteTable
{
    private static readonly IReadOnlyDictionary<string, object?> EmptyRouteValues = new Dictionary<string, object?>();

    private readonly IReadOnlyList<ExplicitRouteEntry> _entries;

    public ExplicitRouteTable(IReadOnlyList<ExplicitRouteEntry> entries)
    {
        _entries = entries;
    }

    public bool TryMatch(string path, out Microsoft.AspNetCore.Components.RouteData routeData)
    {
        for (var index = 0; index < _entries.Count; index++)
        {
            if (_entries[index].TryMatch(path, out routeData))
            {
                return true;
            }
        }

        routeData = default!;
        return false;
    }

    public bool TryProcessRouteData(Microsoft.AspNetCore.Components.RouteData routeData, out Microsoft.AspNetCore.Components.RouteData processedRouteData)
    {
        for (var index = 0; index < _entries.Count; index++)
        {
            if (_entries[index].Matches(routeData.PageType, routeData.Template))
            {
                processedRouteData = _entries[index].ProcessRouteData(routeData.RouteValues);
                return true;
            }
        }

        processedRouteData = default!;
        return false;
    }

    internal sealed class ExplicitRouteEntry
    {
        private static readonly HttpContext ConstraintHttpContext = new DefaultHttpContext();

        private readonly TemplateMatcher _matcher;
        private readonly IDictionary<string, IRouteConstraint> _constraints;
        private readonly IReadOnlyList<string> _unusedRouteParameterNames;

        public ExplicitRouteEntry(
            BlazorRouteDefinition route,
            RouteTemplate template,
            decimal precedence,
            IDictionary<string, IRouteConstraint> constraints,
            IReadOnlyList<string> unusedRouteParameterNames)
        {
            Route = route;
            Template = template;
            Precedence = precedence;
            _constraints = constraints;
            _unusedRouteParameterNames = unusedRouteParameterNames;
            _matcher = new TemplateMatcher(template, new RouteValueDictionary());
        }

        public BlazorRouteDefinition Route { get; }

        public RouteTemplate Template { get; }

        public decimal Precedence { get; }

        public bool Matches(Type pageType, string? template) =>
            pageType == Route.PageType &&
            string.Equals(template, Route.Template, StringComparison.OrdinalIgnoreCase);

        public bool TryMatch(string path, out Microsoft.AspNetCore.Components.RouteData routeData)
        {
            var routeValues = new RouteValueDictionary();
            if (!_matcher.TryMatch(path, routeValues))
            {
                routeData = default!;
                return false;
            }

            if (_constraints.Count > 0 &&
                !RouteConstraintMatcher.Match(
                    _constraints,
                    routeValues,
                    ConstraintHttpContext,
                    route: NullRouter.Instance,
                    RouteDirection.IncomingRequest,
                    NullLogger.Instance))
            {
                routeData = default!;
                return false;
            }

            ProcessParameters(routeValues);
            routeData = CreateRouteData(routeValues);
            return true;
        }

        public Microsoft.AspNetCore.Components.RouteData ProcessRouteData(IReadOnlyDictionary<string, object?> routeValues)
        {
            var processedValues = routeValues.Count == 0
                ? new RouteValueDictionary()
                : new RouteValueDictionary(routeValues);

            ProcessParameters(processedValues);
            return CreateRouteData(processedValues);
        }

        private void ProcessParameters(RouteValueDictionary routeValues)
        {
            for (var index = 0; index < _unusedRouteParameterNames.Count; index++)
            {
                routeValues[_unusedRouteParameterNames[index]] = null;
            }

            foreach (var key in routeValues.Keys.ToArray())
            {
                if (routeValues[key] is string value)
                {
                    routeValues[key] = value.Replace("%2F", "/", StringComparison.OrdinalIgnoreCase);
                }
            }

            foreach (var parameter in Template.Parameters)
            {
                if (!routeValues.TryGetValue(parameter.Name!, out var parameterValue))
                {
                    routeValues[parameter.Name!] = null;
                    continue;
                }

                if (!parameter.IsCatchAll)
                {
                    foreach (var constraint in parameter.InlineConstraints)
                    {
                        switch (constraint.Constraint)
                        {
                            case "bool":
                                routeValues[parameter.Name!] = bool.Parse((string)parameterValue!);
                                break;
                            case "datetime":
                                routeValues[parameter.Name!] = DateTime.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                                break;
                            case "decimal":
                                routeValues[parameter.Name!] = decimal.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                                break;
                            case "double":
                                routeValues[parameter.Name!] = double.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                                break;
                            case "float":
                                routeValues[parameter.Name!] = float.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                                break;
                            case "guid":
                                routeValues[parameter.Name!] = Guid.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                                break;
                            case "int":
                                routeValues[parameter.Name!] = int.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                                break;
                            case "long":
                                routeValues[parameter.Name!] = long.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                                break;
                        }
                    }
                }
            }
        }

        private Microsoft.AspNetCore.Components.RouteData CreateRouteData(RouteValueDictionary routeValues)
        {
            return new Microsoft.AspNetCore.Components.RouteData(
                Route.PageType,
                routeValues.Count == 0 ? EmptyRouteValues : routeValues)
            {
                Template = Route.Template,
            };
        }
    }

#pragma warning disable ASP5001
    private sealed class NullRouter : IRouter
    {
        public static NullRouter Instance { get; } = new();

        public VirtualPathData? GetVirtualPath(VirtualPathContext context) => null;

        public Task RouteAsync(RouteContext context) => Task.CompletedTask;
    }
#pragma warning restore ASP5001
}
