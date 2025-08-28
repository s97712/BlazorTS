using System.Reflection;
using Xunit;

namespace BlazorTS.TestPackage
{
    public partial class TestFunctions { }
    public class SourceGeneratorTests
    {
        [Fact]
        public void Test_Generated_Functions_Exist()
        {
            // 使用反射测试确保正确生成函数 - 引用实际的生成类
            var testFunctionsType = typeof(TestFunctions);
            
            // 获取TSInterop嵌套类
            var tsInteropType = testFunctionsType.GetNestedType("TSInterop");
            Assert.NotNull(tsInteropType);
            
            // 验证主要函数已生成
            var methods = tsInteropType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            var methodNames = methods.Select(m => m.Name).ToArray();
            
            Assert.Contains("hello", methodNames);
            Assert.Contains("add", methodNames);
            Assert.Contains("greet", methodNames);
        }
    }
}