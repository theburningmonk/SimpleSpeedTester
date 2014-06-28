using Microsoft.CSharp;

using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace System.Text.Json
{
    public static class JsonWriter
    {
        static Dictionary<string, MethodTarget> GeneratedType;
        static CSharpCodeProvider provider;
        private static int NumberOfExecute = 0;

        private static List<Assembly> compiled;

        public class CacheWriter<T>
        {
            internal CacheWriter(Action<TextWriter, T> action)
            {
                this.Writer = action;
            }

            public Action<TextWriter, T> Writer { get; internal set; }
            public string Write(T Graph)
            {
                StringBuilder stb = new StringBuilder();
                using (var stm = new StringWriter(stb))
                {
                    Writer(stm, Graph);
                }
                return stb.ToString();
            }
        }

        private class MethodTarget
        {
            public string Name { get; set; }
            public BuildOptions Options { get; set; }

            //Action<TextWriter, object> method;
            MethodInfo method;
            public void Generate(TextWriter wr, object graph)
            {
                //if (method == null)
                //    method = (Action<TextWriter, object>)Delegate.CreateDelegate(typeof(Action<TextWriter, object>), Options.CompiledType.GetMethod(Name));
                if (method == null)
                    method = Options.CompiledType.GetMethod(Name);
                //method(wr, graph);
                method.Invoke(null, new[] { wr, graph });
            }
        }

        private class BuildOptions
        {

            public BuildOptions(string Namespace, string Name, int Number)
            {
                this.Namespace = Namespace;
                this.Number = Number;
                this.ClassName = Name + Number;
                this.FullName = Namespace + "." + ClassName;
            }

            public int Number { get; set; }
            public string Namespace { get; set; }
            public string ClassName { get; set; }
            public string FullName { get; set; }
            public Type CompiledType { get; set; }
        }

        static JsonWriter()
        {
            provider = new CSharpCodeProvider();
            GeneratedType = new Dictionary<string, MethodTarget>();
            compiled = new List<Assembly>();
        }

        private static MethodTarget FindType(Type t)
        {
            if (GeneratedType.ContainsKey(t.FullName))
                return GeneratedType[t.FullName];

            var globalCodes = new StringBuilder();

            globalCodes.AppendLine("using Microsoft.CSharp;");
            globalCodes.AppendLine("using System;");
            globalCodes.AppendLine("using System.Collections;");
            globalCodes.AppendLine("using System.Collections.Generic;");
            globalCodes.AppendLine("using System.IO;");
            globalCodes.AppendLine("using System.Text;");
            globalCodes.AppendLine("using System.Text.Json;");

            BuildOptions options = new BuildOptions("System.Text.Json.CacheType", "Cache", NumberOfExecute++);

            globalCodes.AppendLine("");
            globalCodes.AppendFormat("namespace {0} {{ ", options.Namespace);
            globalCodes.AppendLine("");
            globalCodes.AppendFormat("\tpublic static class {0} {{ ", options.ClassName);
            globalCodes.AppendLine("");

            var gen = GenerateSource(globalCodes, t, options);

            globalCodes.AppendLine("");
            globalCodes.AppendLine("}");
            globalCodes.AppendLine("}");

            var parms = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = false,
            };

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    string location = assembly.Location;
                    if (!String.IsNullOrEmpty(location))
                    {
                        parms.ReferencedAssemblies.Add(location);
                    }
                }
                catch (NotSupportedException)
                {
                }
            }

            foreach (Assembly assembly in compiled)
            {
                try
                {
                    string location = assembly.Location;
                    if (!String.IsNullOrEmpty(location))
                    {
                        parms.ReferencedAssemblies.Add(location);
                    }
                }
                catch (NotSupportedException)
                {
                }
            }

            var result = provider.CompileAssemblyFromSource(parms, globalCodes.ToString());
            options.CompiledType = result.CompiledAssembly.GetType(options.FullName);

            compiled.Add(result.CompiledAssembly);

            return gen;
        }

        public static CacheWriter<T> GetWriter<T>()
        {
            var CurrentMethod = FindType(typeof(T));
            var Writer = (Action<TextWriter, T>)Delegate.CreateDelegate(typeof(Action<TextWriter, T>), CurrentMethod.Options.CompiledType.GetMethod(CurrentMethod.Name));
            return new CacheWriter<T>(Writer);
        }

        public static string WriteJson<T>(T Graph)
        {
            var CurrentMethod = FindType(typeof(T));

            StringBuilder stb = new StringBuilder();
            using (var stm = new StringWriter(stb))
            {
                CurrentMethod.Generate(stm, Graph);
            }
            return stb.ToString();
        }
        public static void WriteJson<T>(TextWriter wr, T Graph)
        {
            var CurrentMethod = FindType(typeof(T));
            CurrentMethod.Generate(wr, Graph);
        }

        static string GetFriendlyName(Type type)
        {
            return provider.GetTypeOutput(new CodeTypeReference(type));
        }
        private static MethodTarget GenerateSource(StringBuilder nstrBuilder, Type t, BuildOptions options)
        {

            if (typeof(IEnumerable).IsAssignableFrom(t))
            {
                var subType = default(Type);
                if (t.IsArray)
                {
                    subType = t.GetElementType();
                }
                else
                {
                    var t_gen = t.GetInterface("IEnumerable`1", true);
                    if (t_gen != null)
                        subType = t_gen.GetGenericArguments()[0];
                    else
                        throw new ArgumentException("not generic collectiondose not supported yet!");
                }

                var genFuncName = GenerateFuncForArrayType(nstrBuilder, subType, options);
                return genFuncName;
            }
            else
            {
                switch (Type.GetTypeCode(t))
                {
                    case TypeCode.Byte:
                    case TypeCode.Decimal:
                    case TypeCode.Double:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.SByte:
                    case TypeCode.Single:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                    case TypeCode.String:
                        throw new ArgumentException("driect type of number and string dose not supported yet!");
                    case TypeCode.Object:
                        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            throw new ArgumentException("driect type of number and string dose not supported yet!");
                        }
                        else
                        {
                            var genFuncName = GenerateFuncForType(nstrBuilder, t, options);
                            return genFuncName;
                        }
                    default:
                        throw new ArgumentException("this driect type dose not supported yet!");
                }
            }
        }

        private static MethodTarget GenerateFuncForArrayType(StringBuilder nstrBuilder, Type t, BuildOptions options)
        {
            if (GeneratedType.ContainsKey("Arr:" + t.FullName))
                return GeneratedType["Arr:" + t.FullName];

            var funcName = "Gen_Arr_Func_" + t.ToString()
                                          .Replace(".", "_")
                                          .Replace("`", "_")
                                          .Replace("[", "_")
                                          .Replace("]", "_") + "_1";
            GeneratedType["Arr:" + t.FullName] = new MethodTarget() { Name = funcName, Options = options };

            var globalCodes = new StringBuilder();

            var format = "wr.Write(item)";
            switch (Type.GetTypeCode(t))
            {
                case TypeCode.String:
                    format = "System.Text.Json.JsonWriterTools.EncodeJsString(wr, item);";
                    break;
                case TypeCode.Object:
                    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        format = "if (item.HasValue) { wr.Write(item.Value); } else { wr.Write(\"null\"); }";
                    }
                    else
                    {
                        var genFuncName = GenerateFuncForType(nstrBuilder, t, options);
                        format = string.Format("if (item != null) {{ {0}.{1}(wr, item); }} else {{ wr.Write(\"null\"); }}", genFuncName.Options.FullName, genFuncName.Name);
                    }
                    break;
            }


            var csName = GetFriendlyName(t);
            globalCodes.AppendFormat("public static void {0} (TextWriter wr, IEnumerable<{1}> graph) {{", funcName, csName);
            globalCodes.Append("wr.Write(\"[\");");

            globalCodes.Append("var isNext = false;");
            globalCodes.Append("foreach (var item in graph) { ");
            globalCodes.Append("if (isNext) { wr.Write(\",\"); }");
            globalCodes.Append(" else { isNext = true; }");

            globalCodes.Append(format);

            globalCodes.Append("} wr.Write(\"]\"); }");
            nstrBuilder.AppendLine(globalCodes.ToString());

            return GeneratedType["Arr:" + t.FullName];
        }

        private static MethodTarget GenerateFuncForType(StringBuilder nstrBuilder, Type t, BuildOptions options)
        {
            if (GeneratedType.ContainsKey(t.FullName))
                return GeneratedType[t.FullName];

            var funcName = "Gen_Func_" + t.ToString()
                                          .Replace(".", "_")
                                          .Replace("`", "_")
                                          .Replace("[", "_")
                                          .Replace("]", "_") + "_1";

            GeneratedType[t.FullName] = new MethodTarget() { Name = funcName, Options = options }; ;

            var globalCodes = new StringBuilder();

            var csName = GetFriendlyName(t);
            globalCodes.AppendFormat("public static void {0} (TextWriter wr, {1} graph) {{", funcName, csName);
            globalCodes.Append("wr.Write(\"{\");");

            var isCont = false;
            var props = t.GetProperties();
            foreach (var item in props)
            {
                //if (isCont)
                //    globalCodes.Append("wr.Write(\",\");");

                switch (Type.GetTypeCode(item.PropertyType))
                {
                    case TypeCode.Byte:
                    case TypeCode.Decimal:
                    case TypeCode.Double:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.SByte:
                    case TypeCode.Single:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        globalCodes.AppendFormat("wr.Write(\"{1}'{0}':{{0}}\", graph.{0});", item.Name, isCont ? "," : "");
                        break;
                    case TypeCode.String:
                        globalCodes.AppendFormat("wr.Write(\"{1}'{0}':\"); System.Text.Json.JsonWriterTools.EncodeJsString(wr, graph.{0});", item.Name, isCont ? "," : "");
                        break;
                    case TypeCode.Object:
                        if (item.PropertyType.IsGenericType && item.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            globalCodes.AppendFormat("if (graph.{0}.HasValue) wr.Write(\"{1}'{0}':{{0}}\", graph.{0}.Value);", item.Name, isCont ? "," : "");
                        }
                        else
                        {
                            var genFuncName = GenerateSource(nstrBuilder, item.PropertyType, options);
                            globalCodes.AppendFormat("if (graph.{0} != null) wr.Write(\"{1}'{0}':\"); {2}.{3}(wr, graph.{0});", item.Name, isCont ? "," : "", genFuncName.Options.FullName, genFuncName.Name);
                        }
                        break;
                }

                isCont = true;
            }
            globalCodes.Append("wr.Write(\"}\"); }");
            nstrBuilder.AppendLine(globalCodes.ToString());

            return GeneratedType[t.FullName];
        }

    }

    #region Tools

    public static class JsonWriterTools
    {
        /// <summary>
        /// Encodes a string to be represented as a string literal. The format
        /// is essentially a JSON string.
        /// 
        /// The string returned includes outer quotes 
        /// Example Output: "Hello \"Rick\"!\r\nRock on"
        /// </summary>
        /// <param name="s"></param>
        /// <remarks>http://weblog.west-wind.com/posts/2007/Jul/14/Embedding-JavaScript-Strings-from-an-ASPNET-Page</remarks>
        /// <returns></returns>
        public static void EncodeJsString(TextWriter wr, string s)
        {
            wr.Write("\"");
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\"':
                        wr.Write("\\\"");
                        break;
                    case '\\':
                        wr.Write("\\\\");
                        break;
                    case '\b':
                        wr.Write("\\b");
                        break;
                    case '\f':
                        wr.Write("\\f");
                        break;
                    case '\n':
                        wr.Write("\\n");
                        break;
                    case '\r':
                        wr.Write("\\r");
                        break;
                    case '\t':
                        wr.Write("\\t");
                        break;
                    default:
                        int i = (int)c;
                        if (i < 32 || i > 127)
                        {
                            wr.Write("\\u{0:X04}", i);
                        }
                        else
                        {
                            wr.Write(c);
                        }
                        break;
                }
            }
            wr.Write("\"");
        }

    }

    #endregion
}