using TypeScriptParser;
using TypeScriptParser.TreeSitter;

namespace BlazorTS;

public sealed class TSFunction
{
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<TSParameter> Parameters { get; set; } = [];
    public string ReturnType { get; set; } = string.Empty;
    public bool IsAsync { get; set; }
}

public sealed class TSParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsOptional { get; set; }
    public string? DefaultValue { get; set; }
}

public sealed class TSInterface
{
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<TSProperty> Properties { get; set; } = [];
}

public sealed class TSProperty
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsOptional { get; set; }
}

public sealed class TSModule
{
    public IReadOnlyList<TSFunction> Functions { get; set; } = [];
    public IReadOnlyList<TSInterface> Interfaces { get; set; } = [];
}

public sealed class TSAnalyzer : IDisposable
{
    private readonly TypeScriptParser.Parser parser = new();

    public TSModule Analyze(string code)
    {
        using var tree = parser.ParseString(code);
        return ExtractWithTreeSitter(tree, code);
    }

    private TSModule ExtractWithTreeSitter(TSTree tree, string code)
    {
        var functions = new List<TSFunction>();
        var interfaces = new List<TSInterface>();
        var rootNode = tree.root_node();
        TraverseNode(rootNode, code, functions, interfaces);

        return new TSModule
        {
            Functions = functions,
            Interfaces = interfaces
        };
    }

    private void TraverseNode(TSNode node, string code, List<TSFunction> functions, List<TSInterface> interfaces)
    {
        var nodeType = node.type();
        if (nodeType == "export_statement")
        {
            ProcessExportStatement(node, code, functions);
        }
        else if (nodeType == "interface_declaration")
        {
            var tsInterface = ParseInterface(node, code);
            if (tsInterface != null)
            {
                interfaces.Add(tsInterface);
            }
        }

        for (uint i = 0; i < node.child_count(); i++)
        {
            var childNode = node.child(i);
            if (!childNode.is_null())
            {
                TraverseNode(childNode, code, functions, interfaces);
            }
        }
    }

    private static TSInterface? ParseInterface(TSNode interfaceNode, string code)
    {
        var nameNode = interfaceNode.child_by_field_name("name");
        if (nameNode.is_null())
        {
            return null;
        }

        var bodyNode = interfaceNode.child_by_field_name("body");
        if (bodyNode.is_null())
        {
            for (uint i = 0; i < interfaceNode.child_count(); i++)
            {
                var child = interfaceNode.child(i);
                if (child.type() == "object_type")
                {
                    bodyNode = child;
                    break;
                }
            }
        }

        var properties = new List<TSProperty>();
        for (uint i = 0; i < bodyNode.child_count(); i++)
        {
            var child = bodyNode.child(i);
            if (child.type() != "property_signature")
            {
                continue;
            }

            var propertyNameNode = child.child_by_field_name("name");
            var typeNode = child.child_by_field_name("type");
            if (propertyNameNode.is_null() || typeNode.is_null())
            {
                continue;
            }

            var isOptional = false;
            for (uint j = 0; j < child.child_count(); j++)
            {
                if (child.child(j).type() == "?")
                {
                    isOptional = true;
                    break;
                }
            }

            properties.Add(new TSProperty
            {
                Name = propertyNameNode.text(code),
                Type = ExtractType(typeNode, code),
                IsOptional = isOptional
            });
        }

        return new TSInterface
        {
            Name = nameNode.text(code),
            Properties = properties
        };
    }

    private static string ExtractType(TSNode typeNode, string code)
    {
        for (uint i = 0; i < typeNode.child_count(); i++)
        {
            var child = typeNode.child(i);
            if (child.type() != ":")
            {
                return child.text(code).Trim();
            }
        }

        var rawText = typeNode.text(code).Trim();
        return rawText.TrimStart(':').Trim();
    }

    private static bool HasAsyncModifier(TSNode node)
    {
        for (uint i = 0; i < node.child_count(); i++)
        {
            var child = node.child(i);
            if (child.type() == "async")
            {
                return true;
            }
        }

        return false;
    }

    private void ProcessExportStatement(TSNode exportNode, string code, List<TSFunction> functions)
    {
        var declaration = exportNode.child_by_field_name("declaration");
        if (declaration.is_null())
        {
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

        if (declaration.is_null())
        {
            return;
        }

        var declType = declaration.type();
        if (declType == "function_declaration")
        {
            var function = ParseFunctionFromNode(declaration, code);
            if (function != null)
            {
                functions.Add(function);
            }
        }
        else if (declType == "lexical_declaration")
        {
            ProcessLexicalDeclaration(declaration, code, functions);
        }
    }

    private void ProcessLexicalDeclaration(TSNode lexicalNode, string code, List<TSFunction> functions)
    {
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
        var nameNode = declaratorNode.child_by_field_name("name");
        var valueNode = declaratorNode.child_by_field_name("value");
        if (nameNode.is_null() || valueNode.is_null())
        {
            return null;
        }

        var functionName = nameNode.text(code);
        var valueType = valueNode.type();

        if (valueType == "arrow_function")
        {
            return ParseArrowFunction(valueNode, code, functionName);
        }

        if (valueType == "function_expression")
        {
            var function = ParseFunctionFromNode(valueNode, code);
            if (function != null)
            {
                function.Name = functionName;
            }
            return function;
        }

        return null;
    }

    private TSFunction? ParseArrowFunction(TSNode arrowNode, string code, string functionName)
    {
        var parent = arrowNode.parent();
        var isAsync = !parent.is_null() && HasAsyncModifier(parent);

        var function = new TSFunction
        {
            Name = functionName,
            IsAsync = isAsync
        };

        var parametersNode = arrowNode.child_by_field_name("parameters");
        if (!parametersNode.is_null())
        {
            function.Parameters = ParseParameters(parametersNode, code);
        }
        else
        {
            var parameterNode = arrowNode.child_by_field_name("parameter");
            if (!parameterNode.is_null())
            {
                var parameter = new TSParameter
                {
                    Name = parameterNode.text(code),
                    Type = "any",
                    IsOptional = false
                };
                function.Parameters = [parameter];
            }
        }

        var returnTypeNode = arrowNode.child_by_field_name("return_type");
        function.ReturnType = !returnTypeNode.is_null()
            ? ExtractType(returnTypeNode, code)
            : "any";

        return function;
    }

    private TSFunction? ParseFunctionFromNode(TSNode functionNode, string code)
    {
        var function = new TSFunction
        {
            IsAsync = HasAsyncModifier(functionNode)
        };

        var nameNode = functionNode.child_by_field_name("name");
        if (!nameNode.is_null())
        {
            function.Name = nameNode.text(code);
        }

        var parametersNode = functionNode.child_by_field_name("parameters");
        if (!parametersNode.is_null())
        {
            function.Parameters = ParseParameters(parametersNode, code);
        }

        var returnTypeNode = functionNode.child_by_field_name("return_type");
        function.ReturnType = !returnTypeNode.is_null()
            ? ExtractType(returnTypeNode, code)
            : "void";

        return function;
    }

    private List<TSParameter> ParseParameters(TSNode paramsNode, string code)
    {
        var parameters = new List<TSParameter>();

        for (uint i = 0; i < paramsNode.child_count(); i++)
        {
            var child = paramsNode.child(i);
            var childType = child.type();
            if (childType is "(" or ")" or ",")
            {
                continue;
            }

            if (childType is "required_parameter" or "optional_parameter")
            {
                var parameter = new TSParameter();

                var identifierNode = child.child_by_field_name("pattern");
                if (!identifierNode.is_null())
                {
                    parameter.Name = identifierNode.text(code);
                }

                parameter.IsOptional = childType == "optional_parameter";

                var typeNode = child.child_by_field_name("type");
                parameter.Type = !typeNode.is_null()
                    ? ExtractType(typeNode, code)
                    : "any";

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

    public void Dispose()
    {
        parser.Dispose();
    }
}
