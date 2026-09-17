using System.Reflection;

using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Infrastructure.Identity;

namespace Landscape.Tsi.Tests.Architecture;

public sealed class IdentityModuleBoundaryTests
{
    [Fact]
    public void Application_DoesNotReferenceInfrastructure()
    {
        var references = typeof(IIdentityAccessService).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references, assembly =>
            assembly.Name == typeof(IdentityDbContext).Assembly.GetName().Name);
    }

    [Fact]
    public void WebControllers_DoNotDependOnIdentityPersistence()
    {
        var controllerTypes = typeof(Program).Assembly.GetTypes()
            .Where(type => type.Name.EndsWith("Controller", StringComparison.Ordinal));

        foreach (var controllerType in controllerTypes)
        {
            Assert.DoesNotContain(
                controllerType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .SelectMany(constructor => constructor.GetParameters()),
                parameter => parameter.ParameterType == typeof(IdentityDbContext));

            Assert.DoesNotContain(
                controllerType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                field => field.FieldType == typeof(IdentityDbContext));
        }
    }
}