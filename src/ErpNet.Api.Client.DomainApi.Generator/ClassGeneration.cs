using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ErpNet.Api.Client.DomainApi.Generator
{
    /// <summary>
    /// Generates types for entities and complex objects in ERP.net Domain API
    /// </summary>
    public static class ClassGeneration
    {
        /// <summary>
        /// Generates the c# types in one .cs file.
        /// </summary>
        /// <param name="fileName">The .cs file</param>
        /// <param name="metaDataStream">The stream from ODATA $metadata endpoint</param>
        public static void GenerateFile(string fileName, Stream metaDataStream)
        {
            var doc = XDocument.Load(metaDataStream, LoadOptions.SetBaseUri);

            var schema = (XElement)doc.Descendants(XName.Get("Schema", "http://docs.oasis-open.org/odata/ns/edm")).FirstOrDefault();
            string mainNamespace = schema.Attribute("Namespace").Value;
            Dictionary<string, TypeNode> allTypes = new Dictionary<string, TypeNode>();

            var boundMethods = schema.Elements(schema.Name.Namespace + "Action")
                .Union(schema.Elements(schema.Name.Namespace + "Function"))
                .Where(n => n.Attribute("IsBound")?.Value == "true")
                .GroupBy(n => ((XElement)n.FirstNode).Attribute("Type").Value)
                .ToDictionary(g => g.Key.Split('.').Last());


            void AppendClass(XElement typeElement, string? entitySet)
            {
                var edmType = typeElement.Attribute("Name").Value;
                var fullName = edmType.Replace("_", ".");
                var baseTypeName = typeElement.Attribute("BaseType")?.Value.Substring(mainNamespace.Length + 1).Replace("_", ".");

                TypeNode type = new TypeNode(fullName);
                type.BaseType = baseTypeName;

                if (!string.IsNullOrEmpty(entitySet))
                {

                    type.Kind = NodeKind.EntityType;
                    var entityNameNode = typeElement
                        .Descendants(schema.Name.Namespace + "Annotation")
                        .FirstOrDefault(n => n.Attribute("Term").Value == mainNamespace + ".EntityName");
                    string args = $"EntitySet = \"{entitySet}\"";
                    string? tableName = null;
                    if (entityNameNode != null)
                    {
                        tableName = entityNameNode.Attribute("String").Value;
                        args += $", TableName = \"{tableName}\"";
                    }
                    type.Attributes.Add($"[Entity({args})]");


                    if (string.IsNullOrEmpty(type.BaseType))
                    {
                        type.BaseType = nameof(EntityResource);
                        type.Members.Add($"public const string EntitySetName = \"{entitySet}\";");
                        type.Members.Add($"public const string EntityTableName = \"{tableName}\";");
                    }
                    else
                    {
                        type.Members.Add($"public new const string EntitySetName = \"{entitySet}\";");
                        type.Members.Add($"public new const string EntityTableName = \"{tableName}\";");
                    }

                  
                }
                else
                {
                    type.Kind = NodeKind.ComplexType;
                    if (string.IsNullOrEmpty(type.BaseType))
                        type.BaseType = nameof(ComplexTypeResource);
                }
                allTypes.Add(type.FullName, type);

                //add primitive properties
                foreach (XElement prop in typeElement.Elements(schema.Name.Namespace + "Property"))
                {
                    var propertyName = prop.Attribute("Name").Value;
                    if (propertyName.StartsWith("CustomProperty_") || propertyName.StartsWith("CalculatedAttribute_"))
                        continue;

                    if (type.Kind == NodeKind.EntityType && propertyName == "Id")
                        continue;

                    string propertyType = GetTypeFromEdmType(allTypes, mainNamespace, prop.Attribute("Type").Value);
                    //var nullableAttr = prop.Attribute("Nullable");
                    //if ((nullableAttr == null || nullableAttr.Value.ToLower() != "false"))
                    propertyType += "?";

                    type.Members.Add("[ODataProperty]");
                    type.Members.Add($"public {propertyType} {propertyName} {{ get => {nameof(ApiResource.GetPropertyValue)}<{propertyType}>(\"{propertyName}\");"
                        + $" set => {nameof(ApiResource.SetPropertyValue)}<{propertyType}>(\"{propertyName}\", value); }}");
                }
                //add navigation properties
                foreach (XElement prop in typeElement.Elements(schema.Name.Namespace + "NavigationProperty"))
                {
                    var propertyName = prop.Attribute("Name").Value;
                    string propertyType = prop.Attribute("Type").Value.Replace("_", ".");

                    if (propertyType.StartsWith("Collection"))
                    {
                        propertyType = propertyType.Replace("Collection(", "").Replace(")", "");

                        if (propertyType.StartsWith(mainNamespace + "."))
                            propertyType = propertyType.Substring(mainNamespace.Length + 1);

                        type.Members.Add("[ODataProperty]");
                        type.Members.Add($"public IEnumerable<{propertyType}>? {propertyName} {{ get => GetCollection<{propertyType}>(\"{propertyName}\"); set => SetCollection<{propertyType}>(\"{propertyName}\", value); }}");
                    }
                    else
                    {
                        bool isOwner = prop.Element(schema.Name.Namespace + "OnDelete") != null;
                        if (isOwner)
                        {
                            type.Members.Add("[Owner]");
                        }

                        if (propertyType.StartsWith(mainNamespace + "."))
                            propertyType = propertyType.Substring(mainNamespace.Length + 1);

                        //type.Members.Add($"public EntityIdentifier? {propertyName}Id {{ get => GetODataId(\"{propertyName}\");"
                        //                            + $" set => SetODataId(\"{propertyName}\", value); }}");
                        type.Members.Add("[ODataProperty]");
                        type.Members.Add($"public {propertyType}? {propertyName} {{ get => {nameof(ApiResource.GetPropertyValue)}<{propertyType}>(\"{propertyName}\");"
                                                    + $" set => {nameof(ApiResource.SetPropertyValue)}<{propertyType}>(\"{propertyName}\", value); }}");
                    }
                }
                //add methods
                if (boundMethods.TryGetValue(edmType, out var group))
                    foreach (var a in group)
                    {
                        var methodName = a.Attribute("Name").Value;
                        List<Parameter> parameters = new List<Parameter>();
                        foreach (var p in a.Elements(schema.Name.Namespace + "Parameter").Skip(1))
                        {
                            var paramType = GetTypeFromEdmType(allTypes, mainNamespace, p.Attribute("Type").Value);
                            var paramName = p.Attribute("Name").Value;
                            string? defaultValue = null;
                            var op = p.Elements().FirstOrDefault(e => e.Name.LocalName == "Annotation" && e.Attribute("Term")?.Value?.EndsWith("OptionalParameter") == true);
                            if (op != null)
                            {
                                if (op.HasElements && op.Descendants(schema.Name.Namespace + "PropertyValue").FirstOrDefault() is XElement pv)
                                {
                                    if (pv.Attribute("String") is XAttribute str)
                                    {
                                        switch (paramType.ToLower())
                                        {
                                            case "boolean":
                                            case "int32":
                                            case "int16":
                                                defaultValue = str.Value.ToLower();
                                                break;
                                            case "byte":
                                                break;
                                            case "string":
                                                defaultValue = $"\"{str.Value}\"";
                                                break;
                                            default:
                                                // probably enum?
                                                defaultValue = paramType + "." + str.Value;
                                                break;
                                        }
                                        
                                    }
                                }
                                else
                                {
                                    paramType += "?";
                                    defaultValue = "null"; 
                                }
                            }
                            else if (p.Attribute("Nullable")?.Value == "true")
                                paramType += "?";
                            parameters.Add(new Parameter(paramName, paramType, defaultValue));
                        }

                        var edmReturnType = a.Element(schema.Name.Namespace + "ReturnType")?.Attribute("Type")?.Value;
                        var parametersDeclaration = $"{nameof(DomainApiService)} service";
                        var parametersUsage = "";
                        if (parameters.Any())
                        {
                            parametersDeclaration += ", " +
                            string.Join(
                            ", ",
                            parameters.Select(p => p.ToString()));

                            parametersUsage += ", " + string.Join(", ", parameters.Select(p => $"new Param<{p.Type}>(\"{p.Name}\", {p.Name})"));
                        }
                        var isAction = a.Name.LocalName == "Action";

                        string body;
                        if (isAction)
                            body = $"await this.InvokeActionAsync(service, \"{methodName}\"{parametersUsage})";
                        else
                            body = $"await this.InvokeFunctionAsync(service, \"{methodName}\"{parametersUsage})";

                        var returnType = "Task";
                        if (edmReturnType != null)
                        {
                            returnType = GetTypeFromEdmType(allTypes, mainNamespace, edmReturnType);
                            body = $"return ({returnType}?)({body})";
                            returnType = $"Task<{returnType}?>";
                        }

                        type.Members.Add($"public async {returnType} {methodName}Async({parametersDeclaration}) {{ {body}; }}");
                    }

                //end class
            }

            var entitySets = schema.Descendants(schema.Name.Namespace + "EntitySet").ToDictionary(
                x => x.Attribute("EntityType").Value.Substring(mainNamespace.Length + 1),
                x => x.Attribute("Name").Value
                );


            // First add enums
            foreach (XElement enumType in schema.Elements(schema.Name.Namespace + "EnumType"))
            {
                var fullName = enumType.Attribute("Name").Value.Replace("_", ".");
                TypeNode type = new TypeNode(fullName);
                type.Kind = NodeKind.Enum;
                allTypes.Add(type.FullName, type);

                var members = enumType.Elements(schema.Name.Namespace + "Member")
                    .Select(prop => $"{prop.Attribute("Name").Value} = {prop.Attribute("Value").Value}")
                    .ToList();

                for (int i = 0; i < members.Count; i++)
                {
                    string comma = ",";
                    if (i == members.Count - 1)
                        comma = "";
                    type.Members.Add(members[i] + comma);
                }

            }

            foreach (XElement complexType in schema.Elements(schema.Name.Namespace + "ComplexType"))
            {
                AppendClass(complexType, null);
            }

            foreach (XElement entityType in schema.Elements(schema.Name.Namespace + "EntityType"))
            {
                AppendClass(entityType, entitySets[entityType.Attribute("Name").Value]);
            }


            // Build tree.
            Dictionary<string, TypeNode> nodes = new Dictionary<string, TypeNode>();
            var root = new TypeNode("") { Kind = NodeKind.Namespace };
            nodes.Add("", root);
            foreach (var type in allTypes.Values.OrderBy(t => t.FullName))
            {
                // create namespaces
                TypeNode? parentNode = null;
                if (!nodes.TryGetValue(type.Parent, out parentNode))
                {
                    Stack<TypeNode> parentsStack = new Stack<TypeNode>();
                    parentNode = new TypeNode(type.Parent) { Kind = NodeKind.Namespace };
                    parentsStack.Push(parentNode);
                    while (parentsStack.Any())
                    {
                        var parent = parentsStack.Pop();
                        nodes.Add(parent.FullName, parent);
                        if (!nodes.TryGetValue(parent.Parent, out var pParent))
                        {
                            pParent = new TypeNode(parent.Parent) { Kind = NodeKind.Namespace };
                            parentsStack.Push(pParent);
                        }
                        pParent.Types.Add(parent);
                    }
                }

                parentNode.Types.Add(type);
                nodes.Add(type.FullName, type);
            }



            FileContents fileContents = new FileContents();

            fileContents.AppendLine("using System;");
            fileContents.AppendLine("using System.Collections.Generic;");
            fileContents.AppendLine("using System.Threading.Tasks;");
            fileContents.AppendLine("#pragma warning disable CS1591");

            fileContents.AppendLine($"namespace {typeof(ApiResource).Namespace}");
            fileContents.BeginBlock();

            foreach (var ns in root.Types)
                WriteContents(ns);
            //end namespace
            fileContents.EndBlock();
            File.WriteAllText(fileName, fileContents.ToString());

            void WriteContents(TypeNode type)
            {
                foreach (var attr in type.Attributes)
                    fileContents.AppendLine(attr);

                switch (type.Kind)
                {
                    case NodeKind.Namespace:
                        fileContents.AppendLine($"namespace {type.Name}");
                        break;
                    case NodeKind.Enum:
                        fileContents.AppendLine($"public enum {type.Name}");
                        break;
                    case NodeKind.ComplexType:
                    case NodeKind.EntityType:
                        if (type.BaseType != null)
                            fileContents.AppendLine($"public partial class {type.Name}: {type.BaseType}");
                        else
                            fileContents.AppendLine($"public partial class {type.Name}: {nameof(ApiResource)}");
                        break;
                }

                fileContents.BeginBlock();

                // Add ctor
                if (type.Kind == NodeKind.EntityType || type.Kind == NodeKind.ComplexType)
                {
                    fileContents.AppendLine($"public {type.Name}(IDictionary<string, object?>? rawData = null): base(rawData) {{}}");
                }
                foreach (var subType in type.Types)
                    WriteContents(subType);

                foreach (var member in type.Members)
                    fileContents.AppendLine(member);

                fileContents.EndBlock();
            }






        }


        static string GetTypeFromEdmType(Dictionary<string, TypeNode> allTypes, string mainNamespace, string edmType)
        {
            string propertyType = edmType.Replace("_", ".");

            if (propertyType.StartsWith("Edm."))
                propertyType = propertyType.Substring(4);

            var m = System.Text.RegularExpressions.Regex.Match(propertyType, @"Collection\(([^\)]+)\)");
            if (m.Success)
            {
                var elementType = GetTypeFromEdmType(allTypes, mainNamespace, m.Groups[1].Value);
                return $"IEnumerable<{elementType}>"; 
            }

            bool isComplexType = false;
            if (propertyType.StartsWith(mainNamespace + "."))
            {
                propertyType = propertyType.Substring(mainNamespace.Length + 1);
                isComplexType = true;
                if (allTypes.TryGetValue(propertyType, out var t))
                    isComplexType = t.Kind == NodeKind.ComplexType;

            }
            switch (propertyType)
            {
                case "Time": propertyType = "TimeSpan"; break;
                case "TimeOfDay": propertyType = "TimeSpan"; break;
                case "DateTimeOffset": propertyType = "DateTime"; break;
                case "Binary": propertyType = "Byte[]"; break;
            }
            return propertyType;
        }

        class FileContents
        {
            StringBuilder sb = new StringBuilder();
            int indent;

            public FileContents AppendLine(string str)
            {
                sb.Append(new string(' ', indent * 4)).AppendLine(str);
                return this;
            }

            public FileContents Append(string str)
            {
                sb.Append(str);
                return this;
            }

            public FileContents BeginBlock()
            {
                AppendLine("{");
                indent++;
                return this;
            }
            public FileContents EndBlock()
            {
                indent--;
                AppendLine("}");
                return this;
            }

            public override string ToString()
            {
                return sb.ToString();
            }
        }

        enum NodeKind
        {
            Namespace,
            ComplexType,
            EntityType,
            Enum
        }

        class Parameter
        {
            public Parameter(string name, string type, string? defaultValue)
            {
                Name = name;
                Type = type;
                DefaultValue = defaultValue;
            }
            public string Name { get; }
            public string Type { get; }
            public string? DefaultValue { get; }

            public override string ToString()
            {
                if (DefaultValue == null)
                    return $"{Type} {Name}";
                return $"{Type} {Name} = {DefaultValue}";

            }
        }

        class TypeNode
        {
            public TypeNode(string fullName)
            {
                FullName = fullName;
                int k = FullName.LastIndexOf('.');
                Name = FullName.Substring(k + 1);
                if (k > -1)
                    Parent = FullName.Substring(0, k);
                else
                    Parent = "";
            }

            public override string ToString()
            {
                return $"{Kind} {FullName}";
            }

            public string? BaseType;
            public NodeKind Kind;

            public List<string> Attributes { get; } = new List<string>();
            public string FullName { get; }
            public string Name { get; }

            public string Parent { get; }
            public List<string> Members { get; } = new List<string>();

            public List<TypeNode> Types { get; } = new List<TypeNode>();
        }
    }
}
