using System.Reflection;
using TypeScriptParser;
using TypeScriptParser.TreeSitter;

namespace BlazorTS;

public class TSFunction
{
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<TSParameter> Parameters { get; set; } = new List<TSParameter>();
    public string ReturnType { get; set; } = string.Empty;
    public bool IsAsync { get; set; }
    public string? Documentation { get; set; }
}

public class TSParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsOptional { get; set; }
    public string? DefaultValue { get; set; }
}

public sealed class TSAnalyzer : IDisposable
{
    private readonly TypeScriptParser.Parser parser = new();

    public List<TSFunction> ExtractFunctions(string code)
    {
        using var tree = parser.ParseString(code);
        return ExtractWithTreeSitter(tree, code);
    }

    private List<TSFunction> ExtractWithTreeSitter(TSTree tree, string code)
    {
        var functions = new List<TSFunction>();
        var rootNode = tree.root_node();
        TraverseNode(rootNode, code, functions);
        return functions;
    }

    private void TraverseNode(TSNode node, string code, List<TSFunction> functions)
    {
        // 只处理 export_statement 节点
        if (node.type() == "export_statement")
        {
            ProcessExportStatement(node, code, functions);
        }

        // 递归遍历子节点
        for (uint i = 0; i < node.child_count(); i++)
        {
            var childNode = node.child(i);
            if (!childNode.is_null())
            {
                TraverseNode(childNode, code, functions);
            }
        }
    }

    private void ProcessExportStatement(TSNode exportNode, string code, List<TSFunction> functions)
    {
        // 获取导出声明
        var declaration = exportNode.child_by_field_name("declaration");
        if (declaration.is_null())
        {
            // 尝试通过遍历子节点找到声明
            for (uint i = 0; i < exportNode.child_count(); i++)
            {
                var child = exportNode.child(i);
                if (!child.is_null() && child.type() != "export")
                {
                    declaration = child;
                    break;
                }
            }
        }

        if (!declaration.is_null())
        {
            if (declaration.type() == "function_declaration")
            {
                // 处理 export function
                var function = ParseFunctionFromNode(declaration, code);
                if (function != null)
                {
                    functions.Add(function);
                }
            }
            else if (declaration.type() == "lexical_declaration")
            {
                // 处理 export const/let/var = ... 形式
                ProcessLexicalDeclaration(declaration, code, functions);
            }
        }
    }

    private void ProcessLexicalDeclaration(TSNode lexicalNode, string code, List<TSFunction> functions)
    {
        // 遍历变量声明
        for (uint i = 0; i < lexicalNode.child_count(); i++)
        {
            var child = lexicalNode.child(i);
            if (!child.is_null() && child.type() == "variable_declarator")
            {
                var function = ParseVariableDeclarator(child, code);
                if (function != null)
                {
                    functions.Add(function);
                }
            }
        }
    }

    private TSFunction? ParseVariableDeclarator(TSNode declaratorNode, string code)
    {
        try
        {
            var nameNode = declaratorNode.child_by_field_name("name");
            var valueNode = declaratorNode.child_by_field_name("value");

            if (nameNode.is_null() || valueNode.is_null())
                return null;

            var functionName = nameNode.text(code);
            var valueType = valueNode.type();

            // 处理箭头函数: const func = (params) => { ... }
            if (valueType == "arrow_function")
            {
                return ParseArrowFunction(valueNode, code, functionName);
            }
            // 处理函数表达式: const func = function(params) { ... }
            else if (valueType == "function_expression")
            {
                var function = ParseFunctionFromNode(valueNode, code);
                if (function != null)
                {
                    function.Name = functionName; // 覆盖函数名
                }
                return function;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private TSFunction? ParseArrowFunction(TSNode arrowNode, string code, string functionName)
    {
        try
        {
            var function = new TSFunction { Name = functionName };

            // 检查是否为异步函数 - 对于箭头函数，async 关键字在父节点
            var parent = arrowNode.parent();
            if (!parent.is_null())
            {
                for (uint i = 0; i < parent.child_count(); i++)
                {
                    var child = parent.child(i);
                    if (child.type() == "async")
                    {
                        function.IsAsync = true;
                        break;
                    }
                }
            }

            // 获取参数
            var parametersNode = arrowNode.child_by_field_name("parameters");
            if (!parametersNode.is_null())
            {
                function.Parameters = ParseParameters(parametersNode, code);
            }
            else
            {
                // 单个参数，没有括号的情况：x => x * 2
                var parameterNode = arrowNode.child_by_field_name("parameter");
                if (!parameterNode.is_null())
                {
                    var parameter = new TSParameter
                    {
                        Name = parameterNode.text(code),
                        Type = "any",
                        IsOptional = false
                    };
                    function.Parameters = new List<TSParameter> { parameter };
                }
            }

            // 获取返回类型（如果有类型注解）
            var returnTypeNode = arrowNode.child_by_field_name("return_type");
            if (!returnTypeNode.is_null())
            {
                for (uint i = 0; i < returnTypeNode.child_count(); i++)
                {
                    var child = returnTypeNode.child(i);
                    if (child.type() != ":")
                    {
                        function.ReturnType = child.text(code).Trim();
                        break;
                    }
                }
            }
            else
            {
                function.ReturnType = "any";
            }

            return function;
        }
        catch
        {
            return null;
        }
    }

    private TSFunction? ParseFunctionFromNode(TSNode functionNode, string code)
    {
        try
        {
            var function = new TSFunction();
            
            // 获取函数名
            var nameNode = functionNode.child_by_field_name("name");
            if (!nameNode.is_null())
            {
                function.Name = nameNode.text(code);
            }

            // 检查是否为异步函数
            for (uint i = 0; i < functionNode.child_count(); i++)
            {
                var child = functionNode.child(i);
                if (child.type() == "async")
                {
                    function.IsAsync = true;
                    break;
                }
            }

            // 获取参数
            var parametersNode = functionNode.child_by_field_name("parameters");
            if (!parametersNode.is_null())
            {
                function.Parameters = ParseParameters(parametersNode, code);
            }

            // 获取返回类型
            var returnTypeNode = functionNode.child_by_field_name("return_type");
            if (!returnTypeNode.is_null())
            {
                // 跳过冒号，获取实际类型
                for (uint i = 0; i < returnTypeNode.child_count(); i++)
                {
                    var child = returnTypeNode.child(i);
                    if (child.type() != ":")
                    {
                        function.ReturnType = child.text(code).Trim();
                        break;
                    }
                }
            }
            else
            {
                function.ReturnType = "void";
            }

            return function;
        }
        catch
        {
            return null;
        }
    }

    private List<TSParameter> ParseParameters(TSNode paramsNode, string code)
    {
        var parameters = new List<TSParameter>();

        for (uint i = 0; i < paramsNode.child_count(); i++)
        {
            var child = paramsNode.child(i);
            
            // 跳过括号和逗号
            if (child.type() == "(" || child.type() == ")" || child.type() == ",")
                continue;

            if (child.type() == "required_parameter" || child.type() == "optional_parameter")
            {
                var parameter = new TSParameter();
                
                // 获取参数名
                var identifierNode = child.child_by_field_name("pattern");
                if (!identifierNode.is_null())
                {
                    parameter.Name = identifierNode.text(code);
                }

                // 检查是否为可选参数
                parameter.IsOptional = child.type() == "optional_parameter";

                // 获取参数类型
                var typeNode = child.child_by_field_name("type");
                if (!typeNode.is_null())
                {
                    // 跳过冒号，获取实际类型
                    for (uint j = 0; j < typeNode.child_count(); j++)
                    {
                        var typeChild = typeNode.child(j);
                        if (typeChild.type() != ":")
                        {
                            parameter.Type = typeChild.text(code).Trim();
                            break;
                        }
                    }
                }
                else
                {
                    parameter.Type = "any";
                }

                // 获取默认值
                var defaultValueNode = child.child_by_field_name("value");
                if (!defaultValueNode.is_null())
                {
                    parameter.DefaultValue = defaultValueNode.text(code);
                }

                parameters.Add(parameter);
            }
        }

        return parameters;
    }


    public void Dispose() => parser?.Dispose();
}