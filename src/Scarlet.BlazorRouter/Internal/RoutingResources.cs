using System.Globalization;

// The vendored routing sources under Routing/ resolve their exception messages through
// Microsoft.AspNetCore.Components.Routing.Resources. Upstream generates that class from
// src/Components/Components/src/Routing/Resources.resx with an Arcade MSBuild task that is not
// available outside the dotnet/aspnetcore build, so it is hand-written here instead.
//
// The message text is copied verbatim from that resx (tag v10.0.6) plus the four entries the
// !COMPONENTS code paths reference, so exceptions read exactly like the built-in Blazor router's.
// Keep in sync when tools/sync-aspnetcore-routing.ps1 moves to a new tag.
namespace Microsoft.AspNetCore.Components.Routing;

internal static class Resources
{
    internal const string ArgumentMustBeGreaterThanOrEqualTo = "Value must be greater than or equal to {0}.";

    internal const string AttributeRoute_DifferentLinkGenerationEntries_SameName = "Two or more routes named '{0}' have different templates.";

    internal const string ConstraintMustBeStringOrConstraint = "The constraint entry '{0}' - '{1}' must have a string value or be of a type which implements '{2}'.";

    internal const string DefaultInlineConstraintResolver_AmbiguousCtors = "The constructor to use for activating the constraint type '{0}' is ambiguous. Multiple constructors were found with the following number of parameters: {1}.";

    internal const string DefaultInlineConstraintResolver_CouldNotFindCtor = "Could not find a constructor for constraint type '{0}' with the following number of parameters: {1}.";

    internal const string DefaultInlineConstraintResolver_TypeNotConstraint = "The constraint type '{0}' which is mapped to constraint key '{1}' must implement the '{2}' interface.";

    internal const string MapGroup_RepeatedDictionaryEntry = "MapGroup cannot build a pattern for '{0}' because the 'RoutePattern.{1}' dictionary key '{2}' has multiple values.";

    internal const string RangeConstraint_MinShouldBeLessThanOrEqualToMax = "The value for argument '{0}' should be less than or equal to the value for the argument '{1}'.";

    internal const string RegexRouteContraint_NotConfigured = "A route parameter uses the regex constraint, which isn't registered. To enable it add the property 'BlazorRoutingEnableRegexConstraint' to your project file inside a `PropertyGroup`.";

    internal const string RouteConstraintBuilder_CouldNotResolveConstraint = "The constraint entry '{0}' - '{1}' on the route '{2}' could not be resolved by the constraint resolver of type '{3}'.";

    internal const string RouteConstraintBuilder_ValidationMustBeStringOrCustomConstraint = "The constraint entry '{0}' - '{1}' on the route '{2}' must have a string value or be of a type which implements '{3}'.";

    internal const string RoutePattern_ConstraintReferenceNotFound = "The constraint reference '{0}' could not be resolved to a type. Register the constraint type with '{1}.{2}'.";

    internal const string RoutePattern_InvalidConstraintReference = "Invalid constraint '{0}'. A constraint must be of type 'string' or '{1}'.";

    internal const string RoutePattern_InvalidParameterConstraintReference = "Invalid constraint '{0}' for parameter '{1}'. A constraint must be of type 'string', '{2}', or '{3}'.";

    internal const string RoutePattern_InvalidStringConstraintReference = "Invalid constraint type '{0}' registered as '{1}'. A constraint  type must either implement '{2}', or inherit from '{3}'.";

    internal const string RoutePatternBuilder_CollectionCannotBeEmpty = "The collection cannot be empty.";

    internal const string RouteValueDictionary_DuplicateKey = "An element with the key '{0}' already exists in the {1}.";

    internal const string RouteValueDictionary_DuplicatePropertyName = "The type '{0}' defines properties '{1}' and '{2}' which differ only by casing. This is not supported by {3} which uses case-insensitive comparisons.";

    internal const string TemplateRoute_CannotHaveCatchAllInMultiSegment = "A path segment that contains more than one section, such as a literal section or a parameter, cannot contain a catch-all parameter.";

    internal const string TemplateRoute_CannotHaveConsecutiveParameters = "A path segment cannot contain two consecutive parameters. They must be separated by a '/' or by a literal string.";

    internal const string TemplateRoute_CannotHaveConsecutiveSeparators = "The route template separator character '/' cannot appear consecutively. It must be separated by either a parameter or a literal value.";

    internal const string TemplateRoute_CannotHaveDefaultValueSpecifiedInlineAndExplicitly = "The route parameter '{0}' has both an inline default value and an explicit default value specified. A route parameter cannot contain an inline default value when a default value is specified explicitly. Consider removing one of them.";

    internal const string TemplateRoute_CatchAllCannotBeOptional = "A catch-all parameter cannot be marked optional.";

    internal const string TemplateRoute_CatchAllMustBeLast = "A catch-all parameter can only appear as the last segment of the route template.";

    internal const string TemplateRoute_Exception = "An error occurred while creating the route with name '{0}' and template '{1}'.";

    internal const string TemplateRoute_InvalidLiteral = "The literal section '{0}' is invalid. Literal sections cannot contain the '?' character.";

    internal const string TemplateRoute_InvalidParameterName = "The route parameter name '{0}' is invalid. Route parameter names must be non-empty and cannot contain these characters: '{{', '}}', '/'. The '?' character marks a parameter as optional, and can occur only at the end of the parameter. The '*' character marks a parameter as catch-all, and can occur only at the start of the parameter.";

    internal const string TemplateRoute_InvalidRouteTemplate = "The route template cannot start with a '~' character unless followed by a '/'.";

    internal const string TemplateRoute_MismatchedParameter = "There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character.";

    internal const string TemplateRoute_OptionalCannotHaveDefaultValue = "An optional parameter cannot have default value.";

    internal const string TemplateRoute_OptionalParameterCanbBePrecededByPeriod = "In the segment '{0}', the optional parameter '{1}' is preceded by an invalid segment '{2}'. Only a period (.) can precede an optional parameter.";

    internal const string TemplateRoute_OptionalParameterHasTobeTheLast = "An optional parameter must be at the end of the segment. In the segment '{0}', optional parameter '{1}' is followed by '{2}'.";

    internal const string TemplateRoute_RepeatedParameter = "The route parameter name '{0}' appears more than one time in the route template.";

    internal const string TemplateRoute_UnescapedBrace = "In a route parameter, '{' and '}' must be escaped with '{{' and '}}'.";

    internal static string FormatArgumentMustBeGreaterThanOrEqualTo(object? p0) =>
        Format(ArgumentMustBeGreaterThanOrEqualTo, p0);

    internal static string FormatAttributeRoute_DifferentLinkGenerationEntries_SameName(object? p0) =>
        Format(AttributeRoute_DifferentLinkGenerationEntries_SameName, p0);

    internal static string FormatConstraintMustBeStringOrConstraint(object? p0, object? p1, object? p2) =>
        Format(ConstraintMustBeStringOrConstraint, p0, p1, p2);

    internal static string FormatDefaultInlineConstraintResolver_AmbiguousCtors(object? p0, object? p1) =>
        Format(DefaultInlineConstraintResolver_AmbiguousCtors, p0, p1);

    internal static string FormatDefaultInlineConstraintResolver_CouldNotFindCtor(object? p0, object? p1) =>
        Format(DefaultInlineConstraintResolver_CouldNotFindCtor, p0, p1);

    internal static string FormatDefaultInlineConstraintResolver_TypeNotConstraint(object? p0, object? p1, object? p2) =>
        Format(DefaultInlineConstraintResolver_TypeNotConstraint, p0, p1, p2);

    internal static string FormatMapGroup_RepeatedDictionaryEntry(object? p0, object? p1, object? p2) =>
        Format(MapGroup_RepeatedDictionaryEntry, p0, p1, p2);

    internal static string FormatRangeConstraint_MinShouldBeLessThanOrEqualToMax(object? p0, object? p1) =>
        Format(RangeConstraint_MinShouldBeLessThanOrEqualToMax, p0, p1);

    internal static string FormatRouteConstraintBuilder_CouldNotResolveConstraint(object? p0, object? p1, object? p2, object? p3) =>
        Format(RouteConstraintBuilder_CouldNotResolveConstraint, p0, p1, p2, p3);

    internal static string FormatRouteConstraintBuilder_ValidationMustBeStringOrCustomConstraint(object? p0, object? p1, object? p2, object? p3) =>
        Format(RouteConstraintBuilder_ValidationMustBeStringOrCustomConstraint, p0, p1, p2, p3);

    internal static string FormatRoutePattern_ConstraintReferenceNotFound(object? p0, object? p1, object? p2) =>
        Format(RoutePattern_ConstraintReferenceNotFound, p0, p1, p2);

    internal static string FormatRoutePattern_InvalidConstraintReference(object? p0, object? p1) =>
        Format(RoutePattern_InvalidConstraintReference, p0, p1);

    internal static string FormatRoutePattern_InvalidParameterConstraintReference(object? p0, object? p1, object? p2, object? p3) =>
        Format(RoutePattern_InvalidParameterConstraintReference, p0, p1, p2, p3);

    internal static string FormatRoutePattern_InvalidStringConstraintReference(object? p0, object? p1, object? p2, object? p3) =>
        Format(RoutePattern_InvalidStringConstraintReference, p0, p1, p2, p3);

    internal static string FormatRouteValueDictionary_DuplicateKey(object? p0, object? p1) =>
        Format(RouteValueDictionary_DuplicateKey, p0, p1);

    internal static string FormatRouteValueDictionary_DuplicatePropertyName(object? p0, object? p1, object? p2, object? p3) =>
        Format(RouteValueDictionary_DuplicatePropertyName, p0, p1, p2, p3);

    internal static string FormatTemplateRoute_CannotHaveDefaultValueSpecifiedInlineAndExplicitly(object? p0) =>
        Format(TemplateRoute_CannotHaveDefaultValueSpecifiedInlineAndExplicitly, p0);

    internal static string FormatTemplateRoute_Exception(object? p0, object? p1) =>
        Format(TemplateRoute_Exception, p0, p1);

    internal static string FormatTemplateRoute_InvalidLiteral(object? p0) =>
        Format(TemplateRoute_InvalidLiteral, p0);

    internal static string FormatTemplateRoute_InvalidParameterName(object? p0) =>
        Format(TemplateRoute_InvalidParameterName, p0);

    internal static string FormatTemplateRoute_OptionalParameterCanbBePrecededByPeriod(object? p0, object? p1, object? p2) =>
        Format(TemplateRoute_OptionalParameterCanbBePrecededByPeriod, p0, p1, p2);

    internal static string FormatTemplateRoute_OptionalParameterHasTobeTheLast(object? p0, object? p1, object? p2) =>
        Format(TemplateRoute_OptionalParameterHasTobeTheLast, p0, p1, p2);

    internal static string FormatTemplateRoute_RepeatedParameter(object? p0) =>
        Format(TemplateRoute_RepeatedParameter, p0);

    private static string Format(string format, params object?[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, format, arguments);
}
