namespace BlazorTS;

public class MethodExtractor
{
    // 新的主要提取方法，返回完整的TSFunction信息
    public static IEnumerable<TSFunction> Extract(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Enumerable.Empty<TSFunction>();

        using var analyzer = new TSAnalyzer();
        return analyzer.ExtractFunctions(code);
    }
    
    // 保持向后兼容的方法名称提取
    public static IEnumerable<string> ExtractNames(string code)
    {
        return Extract(code).Select(f => f.Name);
    }

    // 保持原有API兼容性 - 内部调用新方法
    [Obsolete("Use Extract(string code) instead")]
    private static IEnumerable<string> ExtractLegacy(string code)
    {
        return ExtractNames(code);
    }
}