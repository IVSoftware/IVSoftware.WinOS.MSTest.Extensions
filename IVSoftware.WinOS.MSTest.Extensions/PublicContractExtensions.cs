using IVSoftware.Portable.Common.Exceptions;
using System.Reflection;
using System.Xml.Linq;

namespace IVSoftware.WinOS.MSTest.Extensions
{
    /// <summary>
    /// Controls how type references are emitted in the public manifest.
    /// </summary>
    /// <remarks>
    /// AssemblyOnly: only types defined in the target assembly are emitted.
    /// AllowExternalReferences: structure is scoped to assembly, but member
    /// signatures may reference external types.
    /// NormalizeExternalReferences: external types are normalized to a
    /// stable canonical form for cross-runtime consistency.
    /// </remarks>
    public enum ManifestTypePolicy
    {
        /// <summary>
        /// Used to detect breaking changes in this NuGet library.
        /// </summary>
        AssemblyOnly,

        /// <summary>
        /// Used to detect breaking changes in this NuGet library, including IVS dependencies.
        /// </summary>
        IVSoftwareAssembliesOnly,

        AllowExternalReferences,

        NormalizeExternalReferences,
    }

    /// <summary>
    /// Public-contract helpers for manifest baselining and breaking-change analysis.
    /// </summary>
    public static class PublicContractExtensions
    {
        public static string ReadManifestResourceFile<T>(
            this string resourcePath,
            ThrowOrAdvise @throw = ThrowOrAdvise.ThrowSoft)
        {
            var asm = typeof(T).Assembly;

            var names = asm.GetManifestResourceNames();
            if (names.FirstOrDefault(_ => _.Equals(resourcePath, StringComparison.OrdinalIgnoreCase)) is null)
            {
                var msg = $"Missing resource:{Environment.NewLine}" +
                    $"  Requested : {resourcePath}{Environment.NewLine}" +
                    $"  Assembly  : {asm.GetName().Name}{Environment.NewLine}" +
                    $"  Resources :{Environment.NewLine}    " +
                    string.Join(
                        Environment.NewLine + "    ",
                        names.OrderBy(_ => _, StringComparer.Ordinal));

                switch (@throw)
                {
                    default:
                    case ThrowOrAdvise.ThrowHard:
                        nameof(ReadManifestResourceFile)
                            .ThrowHard<FileNotFoundException>(msg);
                        break;
                    case ThrowOrAdvise.ThrowSoft:
                        nameof(ReadManifestResourceFile)
                            .ThrowSoft<FileNotFoundException>(msg);
                        break;
                    case ThrowOrAdvise.Advisory:
                        nameof(ReadManifestResourceFile)
                            .Advisory(msg);
                        break;
                }
                // Reachable if Throw pattern is handled or advisory.
                return string.Join(Environment.NewLine, msg);
            }

            using var stream = asm.GetManifestResourceStream(resourcePath);
            using var reader = new StreamReader(stream!);

            return reader.ReadToEnd();
        }

        /// <summary>
        /// Emits a deterministic XML contract for all public types in the assembly.
        /// </summary>
        /// <remarks>
        /// Aggregates Type-level manifests into a single, ordered contract surface.
        /// Only exported (public) types are included.
        /// </remarks>
        public static XElement ToPublicContract(
            this Assembly assembly,
            ManifestTypePolicy policy = ManifestTypePolicy.AssemblyOnly)
        {
            return new XElement("assembly",
                new XAttribute("name", assembly.GetName().Name!),
                new XAttribute("version", assembly.GetName().Version!.ToString()),
                assembly
                    .GetExportedTypes()
                    .OrderBy(GetTypeIdentity)
                    .Select(t => t.ToPublicManifest(assembly, policy)));
        }

        /// <summary>
        /// Emits a deterministic XML manifest of the public contract surface.
        /// </summary>
        public static XElement ToPublicManifest(
            this Type type,
            Assembly asm,
            ManifestTypePolicy policy = ManifestTypePolicy.AllowExternalReferences)
        {
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

            var root = new XElement("type",
                new XAttribute("name", GetTypeIdentity(type)));

            root.Add(new XElement("interfaces",
                type.GetInterfaces()
                    .Where(i => IsInAssembly(i, asm))
                    .Distinct()
                    .OrderBy(i => GetTypeName(i, asm, policy))
                    .Select(i => new XElement("interface",
                        new XAttribute("name", GetTypeName(i, asm, policy))))));

            root.Add(new XElement("constructors",
                type.GetConstructors(flags)
                    .OrderBy(c => c.GetParameters().Length)
                    .ThenBy(c => string.Join(",",
                        c.GetParameters()
                            .Select(p => GetTypeName(p.ParameterType, asm, policy))))
                    .Select(c => new XElement("ctor",
                        new XElement("parameters",
                            c.GetParameters().Select(p =>
                                new XElement("param",
                                    new XAttribute("name", p.Name ?? string.Empty),
                                    new XAttribute("type", GetTypeName(p.ParameterType, asm, policy)))))))));

            root.Add(new XElement("properties",
                type.GetProperties(flags)
                    .OrderBy(p => p.Name)
                    .ThenBy(p => GetTypeName(p.PropertyType, asm, policy))
                    .ThenBy(p => string.Join(",",
                        p.GetIndexParameters()
                            .Select(ip => GetTypeName(ip.ParameterType, asm, policy))))
                    .ThenBy(p => p.CanRead)
                    .ThenBy(p => p.CanWrite)
                    .Select(p => new XElement("property",
                        new XAttribute("name", p.Name),
                        new XAttribute("type", GetTypeName(p.PropertyType, asm, policy)),
                        new XAttribute("canRead", p.CanRead),
                        new XAttribute("canWrite", p.CanWrite)))));

            root.Add(new XElement("events",
                type.GetEvents(flags)
                    .OrderBy(e => e.Name)
                    .ThenBy(e => GetTypeName(e.EventHandlerType!, asm, policy))
                    .Select(e => new XElement("event",
                        new XAttribute("name", e.Name),
                        new XAttribute("type", GetTypeName(e.EventHandlerType!, asm, policy))))));

            root.Add(new XElement("fields",
                type.GetFields(flags)
                    .OrderBy(f => f.Name)
                    .ThenBy(f => GetTypeName(f.FieldType, asm, policy))
                    .Select(f => new XElement("field",
                        new XAttribute("name", f.Name),
                        new XAttribute("type", GetTypeName(f.FieldType, asm, policy))))));

            root.Add(new XElement("methods",
                type.GetMethods(flags)
                    .Where(m => !m.IsSpecialName)
                    .DistinctBy(GetMethodSignature)
                    .OrderBy(m => m.Name)
                    .ThenBy(m => m.GetParameters().Length)
                    .ThenBy(GetMethodSignature)
                    .Select(m => new XElement("method",
                        new XAttribute("name", m.Name),
                        new XAttribute("returns", GetTypeName(m.ReturnType, asm, policy)),
                        new XElement("parameters",
                            m.GetParameters().Select(p =>
                                new XElement("param",
                                    new XAttribute("name", p.Name ?? string.Empty),
                                    new XAttribute("type", GetTypeName(p.ParameterType, asm, policy)))))))));

            root.Add(new XElement("nestedTypes",
                type.GetNestedTypes(BindingFlags.Public)
                    .Where(t => IsInAssembly(t, asm))
                    .OrderBy(GetTypeIdentity)
                    .Select(t => t.ToPublicManifest(asm, policy))));

            return root;
        }

        /// <summary>
        /// Returns true if revision preserves all baseline public signatures.
        /// </summary>
        public static bool IsContractValid(this string baselineXml, string revisionXml, ManifestTypePolicy policy)
        {
            var baseline = XElement.Parse(baselineXml);
            var revision = XElement.Parse(revisionXml);

            var baselineSet = ExtractSignatures(baseline, policy);
            var revisionSet = ExtractSignatures(revision, policy);

            foreach (var sig in baselineSet)
            {
                if (!revisionSet.Contains(sig))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns breaking public API changes grouped into an XML hierarchy.
        /// </summary>
        public static XElement GetBreakingChanges(
            this string baselineXml,
            string revisionXml,
            ManifestTypePolicy policy)
        {
            return BuildBreakingChangesXml();

            IReadOnlyList<string> GetBreakingChangeSignatures()
            {
                var baseline = XElement.Parse(baselineXml);
                var revision = XElement.Parse(revisionXml);

                var baselineSet = ExtractSignatures(baseline, policy);
                var revisionSet = ExtractSignatures(revision, policy);

                return baselineSet
                    .Where(sig => !revisionSet.Contains(sig))
                    .OrderBy(sig => sig)
                    .ToArray();
            }

            XElement BuildBreakingChangesXml()
            {
                var signatures = GetBreakingChangeSignatures()
                    .OrderBy(sig => sig)
                    .ToArray();

                var parsed = signatures
                    .Select(sig =>
                    {
                        var marker = sig[0];
                        var payload = sig[2..];
                        string ownerType;

                        if (marker == 'T')
                        {
                            ownerType = payload;
                        }
                        else if (marker == 'C')
                        {
                            var paren = payload.IndexOf('(');
                            ownerType = paren >= 0 ? payload[..paren] : payload;
                        }
                        else
                        {
                            ownerType = payload.Split('|')[0];
                        }

                        var (ns, typeName) = SplitTypeName(ownerType);

                        return new
                        {
                            Signature = sig,
                            Namespace = ns,
                            TypeName = typeName,
                            Member = MakeMemberElement(sig),
                        };
                    })
                    .GroupBy(x => x.Namespace)
                    .OrderBy(g => g.Key)
                    .Select(namespaceGroup =>
                        new XElement("namespace",
                            new XAttribute("name", namespaceGroup.Key),
                            namespaceGroup
                                .GroupBy(x => x.TypeName)
                                .OrderBy(g => g.Key)
                                .Select(typeGroup =>
                                    new XElement("type",
                                        new XAttribute("name", typeGroup.Key),
                                        typeGroup
                                            .OrderBy(x => x.Signature)
                                            .Select(x => x.Member)))));

                return new XElement("breakingChanges",
                    new XAttribute("policy", policy),
                    parsed);
            }
        }

        private static HashSet<string> ExtractSignatures(
            XElement root,
            ManifestTypePolicy policy)
        {
            var set = new HashSet<string>();

            foreach (var type in EnumerateTypes(root))
            {
                var typeName = type.Attribute("name")?.Value;
                if (typeName == null)
                {
                    continue;
                }

                set.Add($"T:{typeName}");

                foreach (var i in type.Element("interfaces")?.Elements("interface") ?? [])
                {
                    var name = i.Attribute("name")?.Value;
                    if (name != null)
                    {
                        set.Add($"I:{typeName}|{NormalizeSignatureTypeName(name, policy)}");
                    }
                }

                foreach (var c in type.Element("constructors")?.Elements("ctor") ?? [])
                {
                    var paramTypes = c.Element("parameters")?
                        .Elements("param")
                        .Select(p => NormalizeSignatureTypeName(
                            p.Attribute("type")?.Value ?? string.Empty,
                            policy));

                    set.Add($"C:{typeName}({string.Join(",", paramTypes ?? [])})");
                }

                foreach (var p in type.Element("properties")?.Elements("property") ?? [])
                {
                    var name = p.Attribute("name")?.Value;
                    var memberType = p.Attribute("type")?.Value;
                    var canRead = p.Attribute("canRead")?.Value;
                    var canWrite = p.Attribute("canWrite")?.Value;

                    if (name != null && memberType != null)
                    {
                        set.Add($"P:{typeName}|{name}|{NormalizeSignatureTypeName(memberType, policy)}|{canRead}|{canWrite}");
                    }
                }

                foreach (var e in type.Element("events")?.Elements("event") ?? [])
                {
                    var name = e.Attribute("name")?.Value;
                    var memberType = e.Attribute("type")?.Value;

                    if (name != null && memberType != null)
                    {
                        set.Add($"E:{typeName}|{name}|{NormalizeSignatureTypeName(memberType, policy)}");
                    }
                }

                foreach (var f in type.Element("fields")?.Elements("field") ?? [])
                {
                    var name = f.Attribute("name")?.Value;
                    var memberType = f.Attribute("type")?.Value;

                    if (name != null && memberType != null)
                    {
                        set.Add($"F:{typeName}|{name}|{NormalizeSignatureTypeName(memberType, policy)}");
                    }
                }

                foreach (var m in type.Element("methods")?.Elements("method") ?? [])
                {
                    var name = m.Attribute("name")?.Value;
                    var returns = m.Attribute("returns")?.Value;

                    if (name == null || returns == null)
                    {
                        continue;
                    }

                    var paramTypes = m.Element("parameters")?
                        .Elements("param")
                        .Select(p => NormalizeSignatureTypeName(
                            p.Attribute("type")?.Value ?? string.Empty,
                            policy));

                    var paramList = paramTypes != null
                        ? string.Join(",", paramTypes)
                        : string.Empty;

                    set.Add($"M:{typeName}|{name}({paramList})->{NormalizeSignatureTypeName(returns, policy)}");
                }
            }

            return set;
        }

        private static IEnumerable<XElement> EnumerateTypes(XElement element)
        {
            if (element.Name == "type")
            {
                yield return element;
            }

            foreach (var nested in element.Element("nestedTypes")?.Elements("type") ?? [])
            {
                foreach (var nestedType in EnumerateTypes(nested))
                {
                    yield return nestedType;
                }
            }

            if (element.Name == "assembly")
            {
                foreach (var child in element.Elements("type"))
                {
                    foreach (var nestedType in EnumerateTypes(child))
                    {
                        yield return nestedType;
                    }
                }
            }
        }

        private static bool IsInAssembly(Type type, Assembly asm)
        {
            if (type.IsGenericParameter)
            {
                return true;
            }

            if (type.HasElementType)
            {
                return type.GetElementType() is { } elementType && IsInAssembly(elementType, asm);
            }

            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition().Assembly == asm)
                {
                    return true;
                }

                return type.GetGenericArguments().All(arg => IsInAssembly(arg, asm));
            }

            return type.Assembly == asm;
        }

        private static bool IsIVS(Type type)
        {
            if (type.IsGenericParameter)
            {
                return true;
            }

            if (type.HasElementType)
            {
                return type.GetElementType() is { } elementType && IsIVS(elementType);
            }

            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition().FullName?.StartsWith("IVSoftware.", StringComparison.Ordinal) == true)
                {
                    return true;
                }

                return type.GetGenericArguments().All(IsIVS);
            }

            return type.FullName?.StartsWith("IVSoftware.", StringComparison.Ordinal) == true;
        }

        private static string GetTypeIdentity(Type type)
        {
            if (type.IsGenericParameter)
            {
                return type.Name;
            }

            if (type.IsArray)
            {
                return $"{GetTypeIdentity(type.GetElementType()!)}[]";
            }

            if (type.IsGenericType)
            {
                var def = type.GetGenericTypeDefinition();
                var name = def.FullName ?? def.Name;
                var tick = name.IndexOf('`');
                if (tick >= 0)
                {
                    name = name[..tick];
                }

                var args = string.Join(",", type.GetGenericArguments().Select(GetTypeIdentity));
                return $"{name}<{args}>";
            }

            return type.FullName ?? type.Name;
        }

        private static string GetTypeName(Type type, Assembly asm, ManifestTypePolicy policy)
        {
            var identity = GetTypeIdentity(type);

            return policy switch
            {
                ManifestTypePolicy.AssemblyOnly => IsInAssembly(type, asm) ? identity : "[external]",
                ManifestTypePolicy.IVSoftwareAssembliesOnly => (IsInAssembly(type, asm) || IsIVS(type)) ? identity : "[external]",
                ManifestTypePolicy.NormalizeExternalReferences => IsInAssembly(type, asm) ? identity : type.Name,
                _ => identity,
            };
        }

        private static string GetMethodSignature(MethodInfo method)
        {
            var parameters = string.Join(",",
                method.GetParameters().Select(p => GetTypeIdentity(p.ParameterType)));
            var returns = GetTypeIdentity(method.ReturnType);
            return $"{method.Name}({parameters})->{returns}";
        }

        private static string NormalizeSignatureTypeName(string typeName, ManifestTypePolicy policy)
        {
            if (string.IsNullOrWhiteSpace(typeName) || typeName == "[external]")
            {
                return "[external]";
            }

            return policy switch
            {
                ManifestTypePolicy.AssemblyOnly => IsInternalTypeName(typeName) ? typeName : "[external]",
                ManifestTypePolicy.IVSoftwareAssembliesOnly => typeName.StartsWith("IVSoftware.", StringComparison.Ordinal) || IsGenericParameterName(typeName)
                    ? typeName
                    : "[external]",
                ManifestTypePolicy.NormalizeExternalReferences => typeName,
                _ => typeName,
            };
        }

        private static bool IsGenericParameterName(string typeName) =>
            !string.IsNullOrWhiteSpace(typeName)
            && !typeName.Contains('.')
            && !typeName.Contains('<')
            && !typeName.Contains('[')
            && !typeName.Contains(',');

        private static bool IsInternalTypeName(string typeName) =>
            typeName.StartsWith("IVSoftware.", StringComparison.Ordinal)
            || IsGenericParameterName(typeName);

        private static (string Namespace, string TypeName) SplitTypeName(string fullTypeName)
        {
            var genericDepth = 0;
            for (int i = fullTypeName.Length - 1; i >= 0; i--)
            {
                switch (fullTypeName[i])
                {
                    case '>':
                        genericDepth++;
                        break;
                    case '<':
                        genericDepth--;
                        break;
                    case '.':
                        if (genericDepth == 0)
                        {
                            return (fullTypeName[..i], fullTypeName[(i + 1)..]);
                        }
                        break;
                }
            }

            return (string.Empty, fullTypeName);
        }

        private static XElement MakeMemberElement(string signature)
        {
            var marker = signature[0];
            var payload = signature[2..];

            return marker switch
            {
                'T' => new XElement("typeRemoved"),
                'I' => MakeInterfaceElement(payload),
                'C' => MakeConstructorElement(payload),
                'P' => MakePropertyElement(payload),
                'E' => MakeEventElement(payload),
                'F' => MakeFieldElement(payload),
                'M' => MakeMethodElement(payload),
                _ => new XElement("member", new XAttribute("signature", signature)),
            };
        }

        private static XElement MakeInterfaceElement(string payload)
        {
            var parts = payload.Split('|');
            return new XElement("interface",
                new XAttribute("name", parts.ElementAtOrDefault(1) ?? string.Empty),
                new XAttribute("signature", $"I:{payload}"));
        }

        private static XElement MakeConstructorElement(string payload) =>
            new XElement("constructor",
                new XAttribute("signature", $"C:{payload}"));

        private static XElement MakePropertyElement(string payload)
        {
            var parts = payload.Split('|');
            return new XElement("property",
                new XAttribute("name", parts.ElementAtOrDefault(1) ?? string.Empty),
                new XAttribute("type", parts.ElementAtOrDefault(2) ?? string.Empty),
                new XAttribute("canRead", parts.ElementAtOrDefault(3) ?? string.Empty),
                new XAttribute("canWrite", parts.ElementAtOrDefault(4) ?? string.Empty),
                new XAttribute("signature", $"P:{payload}"));
        }

        private static XElement MakeEventElement(string payload)
        {
            var parts = payload.Split('|');
            return new XElement("event",
                new XAttribute("name", parts.ElementAtOrDefault(1) ?? string.Empty),
                new XAttribute("type", parts.ElementAtOrDefault(2) ?? string.Empty),
                new XAttribute("signature", $"E:{payload}"));
        }

        private static XElement MakeFieldElement(string payload)
        {
            var parts = payload.Split('|');
            return new XElement("field",
                new XAttribute("name", parts.ElementAtOrDefault(1) ?? string.Empty),
                new XAttribute("type", parts.ElementAtOrDefault(2) ?? string.Empty),
                new XAttribute("signature", $"F:{payload}"));
        }

        private static XElement MakeMethodElement(string payload)
        {
            var parts = payload.Split('|');
            return new XElement("method",
                new XAttribute("name", parts.ElementAtOrDefault(1)?.Split('(')[0] ?? string.Empty),
                new XAttribute("signature", $"M:{payload}"));
        }
    }
}
