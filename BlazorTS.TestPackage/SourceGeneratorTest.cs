using System.Reflection;
using Xunit;
using BlazorTS.SourceGenerator.Extensions;

namespace BlazorTS.TestPackage
{
    public partial class TestComponent { }
    public class SourceGeneratorTest
    {
        [Fact]
        public void Test_Generated_Functions_Exist_For_TestModule()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var testModuleType = assembly.GetType("BlazorTS.TestPackage.TestModule");
            Assert.NotNull(testModuleType);
            
            var methods = testModuleType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            var methodNames = methods.Select(m => m.Name).ToArray();
            
            Assert.Contains("hello", methodNames);
            Assert.Contains("add", methodNames);
            Assert.Contains("greet", methodNames);
            Assert.Contains("arrowFunction", methodNames);
            Assert.Contains("asyncArrowFunction", methodNames);
            Assert.Contains("functionExpression", methodNames);
            Assert.Contains("defaultFunction", methodNames);
        }

        [Fact]
        public void Test_Generated_Functions_Exist_For_TestComponent()
        {
            var testComponentType = typeof(TestComponent);
            var tsInteropType = testComponentType.GetNestedType("TSInterop");
            Assert.NotNull(tsInteropType);

            var methods = tsInteropType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            var methodNames = methods.Select(m => m.Name).ToArray();

            Assert.Contains("componentFunc", methodNames);
        }
    }
}