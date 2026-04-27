namespace Template.Modules.Common.Abstractions;

[AttributeUsage(AttributeTargets.Interface)]
public sealed class FeatureRouteTemplateAttribute(string template) : Attribute
{
    public string Template { get; } = template.Trim().TrimStart('/');
}
