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
    
}