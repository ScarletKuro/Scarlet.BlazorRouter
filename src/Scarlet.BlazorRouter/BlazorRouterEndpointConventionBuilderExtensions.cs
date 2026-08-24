using System.Collections;
using System.Runtime.CompilerServices;

namespace Scarlet.BlazorRouter;

public static class BlazorRouterEndpointConventionBuilderExtensions
{
    private const string ComponentApplicationBuilderTypeName = "Microsoft.AspNetCore.Components.Discovery.ComponentApplicationBuilder, Microsoft.AspNetCore.Components.Endpoints";
    private const string ComponentApplicationBuilderActionsTypeName = "System.Collections.Generic.List`1[[System.Action`1[[Microsoft.AspNetCore.Components.Discovery.ComponentApplicationBuilder, Microsoft.AspNetCore.Components.Endpoints]]]]";
    private const string PageCollectionBuilderTypeName = "Microsoft.AspNetCore.Components.Discovery.PageCollectionBuilder, Microsoft.AspNetCore.Components.Endpoints";
    private const string PageComponentBuilderTypeName = "Microsoft.AspNetCore.Components.Discovery.PageComponentBuilder, Microsoft.AspNetCore.Components.Endpoints";
    private const string PageComponentBuilderReadOnlyListTypeName = "System.Collections.Generic.IReadOnlyList`1[[Microsoft.AspNetCore.Components.Discovery.PageComponentBuilder, Microsoft.AspNetCore.Components.Endpoints]]";

    public static TBuilder AddBlazorRouterRoutes<TBuilder>(
        this TBuilder builder,
        IReadOnlyList<BlazorRouteDefinition> routes)
        where TBuilder : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(routes);

        var actions = (IList)EndpointConventionBuilderAccessors.GetComponentApplicationBuilderActions(builder);
        actions.Add((Action<object>)(applicationBuilder => ApplyRoutesToApplicationBuilder(applicationBuilder, routes)));
        return builder;
    }

    private static void ApplyRoutesToApplicationBuilder(object applicationBuilder, IReadOnlyList<BlazorRouteDefinition> routes)
    {
        var pagesBuilder = ComponentApplicationBuilderAccessors.GetPages(applicationBuilder);

        var explicitPagesByAssembly = routes
            .GroupBy(route => route.PageType.Assembly.GetName().Name!, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .GroupBy(route => route.PageType)
                    .Select(pageGroup =>
                    {
                        var pageBuilder = PageComponentBuilderAccessors.Create();
                        PageComponentBuilderAccessors.SetAssemblyName(pageBuilder, group.Key);
                        PageComponentBuilderAccessors.SetRouteTemplates(pageBuilder, pageGroup.Select(route => route.Template).ToArray());
                        PageComponentBuilderAccessors.SetPageType(pageBuilder, pageGroup.Key);
                        return pageBuilder;
                    })
                    .ToArray(),
                StringComparer.Ordinal);

        foreach (var entry in explicitPagesByAssembly)
        {
            PageCollectionBuilderAccessors.RemoveFromAssembly(pagesBuilder, entry.Key);
            PageCollectionBuilderAccessors.AddFromLibraryInfo(
                pagesBuilder,
                entry.Key,
                CreatePageComponentBuilderArray(entry.Value));
        }
    }

    private static Array CreatePageComponentBuilderArray(object[] pageBuilders)
    {
        var array = Array.CreateInstance(pageBuilders[0].GetType(), pageBuilders.Length);
        for (var index = 0; index < pageBuilders.Length; index++)
        {
            array.SetValue(pageBuilders[index], index);
        }

        return array;
    }

    private static class EndpointConventionBuilderAccessors
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_ComponentApplicationBuilderActions")]
        [return: UnsafeAccessorType(ComponentApplicationBuilderActionsTypeName)]
        internal static extern object GetComponentApplicationBuilderActions([UnsafeAccessorType("Microsoft.AspNetCore.Builder.RazorComponentsEndpointConventionBuilder, Microsoft.AspNetCore.Components.Endpoints")] object builder);
    }

    private static class ComponentApplicationBuilderAccessors
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Pages")]
        [return: UnsafeAccessorType(PageCollectionBuilderTypeName)]
        internal static extern object GetPages([UnsafeAccessorType(ComponentApplicationBuilderTypeName)] object applicationBuilder);
    }

    private static class PageCollectionBuilderAccessors
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "RemoveFromAssembly")]
        internal static extern void RemoveFromAssembly([UnsafeAccessorType(PageCollectionBuilderTypeName)] object pageCollectionBuilder, string assemblyName);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "AddFromLibraryInfo")]
        internal static extern void AddFromLibraryInfo(
            [UnsafeAccessorType(PageCollectionBuilderTypeName)] object pageCollectionBuilder,
            string assemblyName,
            [UnsafeAccessorType(PageComponentBuilderReadOnlyListTypeName)] object pages);
    }

    private static class PageComponentBuilderAccessors
    {
        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        [return: UnsafeAccessorType(PageComponentBuilderTypeName)]
        internal static extern object Create();

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_AssemblyName")]
        internal static extern void SetAssemblyName([UnsafeAccessorType(PageComponentBuilderTypeName)] object pageComponentBuilder, string assemblyName);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_PageType")]
        internal static extern void SetPageType([UnsafeAccessorType(PageComponentBuilderTypeName)] object pageComponentBuilder, Type pageType);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_RouteTemplates")]
        internal static extern void SetRouteTemplates([UnsafeAccessorType(PageComponentBuilderTypeName)] object pageComponentBuilder, IReadOnlyList<string> routeTemplates);
    }

}
