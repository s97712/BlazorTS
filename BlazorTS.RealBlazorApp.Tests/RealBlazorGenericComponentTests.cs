using System.Reflection;
using System.Xml.Linq;
using BlazorTS;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorTS.RealBlazorApp.Tests;

public class RealBlazorGenericComponentTests
{
    [Fact]
    public void GenericComponent_Generates_ComponentScriptsMember_And_NonGenericInterop()
    {
        var assembly = typeof(BlazorTS.RealBlazorApp.Components.Pages.GenericProbe<>).Assembly;
        var interopType = assembly.GetType("BlazorTS.RealBlazorApp.Components.Pages.GenericProbeTSInterop");

        Assert.NotNull(interopType);
        Assert.Equal("GenericProbeTSInterop", interopType!.Name);
        Assert.DoesNotContain("`", interopType.FullName);

        var xmlPath = Path.Combine(AppContext.BaseDirectory, "BlazorTS.RealBlazorApp.xml");
        var document = XDocument.Load(xmlPath);
        var memberNames = document.Descendants("member")
            .Select(member => member.Attribute("name")?.Value)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .ToArray();

        Assert.Contains("T:BlazorTS.RealBlazorApp.Components.Pages.GenericProbe", memberNames);
        Assert.Contains("P:BlazorTS.RealBlazorApp.Components.Pages.GenericProbe.Scripts", memberNames);
        Assert.Contains("M:BlazorTS.RealBlazorApp.Components.Pages.GenericProbeTSInterop.describeCount(System.Double)", memberNames);
        Assert.Contains("M:BlazorTS.RealBlazorApp.Components.Pages.GenericProbeTSInterop.describeUser(BlazorTS.RealBlazorApp.Components.Pages.User)", memberNames);
    }

    [Fact]
    public void GeneratedInterop_Generates_InterfaceModels_And_Resolves_InterfaceMethodTypes()
    {
        var assembly = typeof(BlazorTS.RealBlazorApp.Components.Pages.GenericProbe<>).Assembly;
        var addressType = assembly.GetType("BlazorTS.RealBlazorApp.Components.Pages.Address");
        var userType = assembly.GetType("BlazorTS.RealBlazorApp.Components.Pages.User");
        var interopType = assembly.GetType("BlazorTS.RealBlazorApp.Components.Pages.GenericProbeTSInterop");

        Assert.NotNull(addressType);
        Assert.NotNull(userType);
        Assert.NotNull(interopType);

        var city = addressType!.GetProperty("city");
        var name = userType!.GetProperty("name");
        var address = userType.GetProperty("address");
        var tags = userType.GetProperty("tags");

        Assert.Equal(typeof(string), city!.PropertyType);
        Assert.Equal(typeof(string), name!.PropertyType);
        Assert.Equal(addressType, address!.PropertyType);
        Assert.Equal(typeof(string[]), tags!.PropertyType);

        var method = interopType!.GetMethod("describeUser", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<string>), method!.ReturnType);
        Assert.Equal(userType, Assert.Single(method.GetParameters()).ParameterType);
    }

    [Fact]
    public void AddBlazorTSScripts_Registers_GenericComponentInterop_AsConcreteService()
    {
        var services = new ServiceCollection();
        services.AddScripts(BlazorTS.RealBlazorApp.Generated.Scripts.AddScripts);

        var descriptor = services.SingleOrDefault(service =>
            service.ServiceType.FullName == "BlazorTS.RealBlazorApp.Components.Pages.GenericProbeTSInterop");

        Assert.NotNull(descriptor);
        Assert.Equal(descriptor!.ServiceType, descriptor.ImplementationType);
        Assert.DoesNotContain("`", descriptor.ServiceType.FullName);
    }

    [Fact]
    public void GeneratedInterop_Uses_StringBasedResolver_Path_Without_GenericArity()
    {
        var resolver = new NSResolver(
        [
            (fullName, suffix) =>
            {
                var path = fullName.Replace("BlazorTS.", string.Empty).Replace('.', '/');
                return $"/js/{path}{suffix}.js";
            }
        ]);
        var path = resolver.ResolveNS("BlazorTS.RealBlazorApp.Components.Pages.GenericProbe", ".razor");

        Assert.Equal("/js/RealBlazorApp/Components/Pages/GenericProbe.razor.js", path);
        Assert.DoesNotContain("`", path);
    }

    [Fact]
    public void GeneratedInterop_Exposes_Typescript_Function_On_TopLevel_Service()
    {
        var interopType = typeof(BlazorTS.RealBlazorApp.Components.Pages.GenericProbe<>).Assembly
            .GetType("BlazorTS.RealBlazorApp.Components.Pages.GenericProbeTSInterop");

        Assert.NotNull(interopType);

        var method = interopType!.GetMethod("describeCount", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<string>), method!.ReturnType);

        var parameter = Assert.Single(method.GetParameters());
        Assert.Equal("count", parameter.Name);
        Assert.Equal(typeof(double), parameter.ParameterType);
    }
}
