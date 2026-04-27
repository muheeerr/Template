namespace Template.Tests;

public sealed class TemplateAssemblyTests
{
    [Fact]
    public void TemplateDbContext_type_is_loadable()
    {
        var t = typeof(Template.Modules.TemplateDbContext);
        Assert.NotNull(t);
    }
}
