using Il2CppSystem.CodeDom;
using Nebula.Modules;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using static Il2CppSystem.Linq.Expressions.Interpreter.CastInstruction.CastInstructionNoT;
using static Il2CppSystem.Linq.Expressions.Interpreter.NullableMethodCallInstruction;

namespace Nebula.Utilities;

[AttributeUsage(AttributeTargets.Field)]
public class JsonSerializableField : Attribute
{
    public bool SkipSerializeIfIsNull { get; private init; } = false;

    public JsonSerializableField(bool skipSerializeIfIsNull = false)
    {
        SkipSerializeIfIsNull = skipSerializeIfIsNull;
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class JSFieldAmbiguous : JsonSerializableField
{
}

public class JsonRawStructure
{
    public List<string> ContentsAsList = null!;
    public Dictionary<string, JsonRawStructure> StructiveContents = null!;
    public string? Prefix;
    public string? AsRaw;

    public bool MergeWith(JsonRawStructure structure)
    {
        if (structure.ContentsAsList != null)
        {
            ContentsAsList ??= new();
            foreach (var c in structure.ContentsAsList!) ContentsAsList.Add(c);
        }

        if (structure.StructiveContents != null)
        {
            StructiveContents ??= new();
            foreach (var entry in structure.StructiveContents)
            {
                if (!StructiveContents.ContainsKey(entry.Key)) StructiveContents.Add(entry.Key, entry.Value);
                else StructiveContents[entry.Key].MergeWith(entry.Value);
            }
        }

        return true;
    }
}

public static class JsonStructure
{
    public static T? Deserialize<T>(Stream json) => (T?)Deserialize(json, typeof(T));
    public static T? Deserialize<T>(string json) => (T?)Deserialize(json, typeof(T));

    private static object? DeserializePrimitive(string json, Type type)
    {
        if (type.IsGenericType) type = type.GetGenericArguments()[0];

        var trimmed = json.Trim('"');

        if (type.Equals(typeof(int)))
            return int.Parse(trimmed);
        if (type.Equals(typeof(byte)))
            return byte.Parse(trimmed);
        if (type.Equals(typeof(string)))
            return trimmed;
        if (type.Equals(typeof(float)))
            return float.Parse(trimmed);
        if (type.Equals(typeof(double)))
            return double.Parse(trimmed);
        if (type.Equals(typeof(bool)))
            return bool.Parse(trimmed);
        return null;
    }

    private static object? DeserializeCollection(string json,Type type)
    {
        var addMethod = type.GetMethod("Add");
        var constructor = type.GetConstructor(new Type[0]);

        if (addMethod == null || constructor == null) throw new Exception("Collection can not be substituted to Non-Collection field.");
        var containedType = addMethod.GetParameters()[0].ParameterType;

        object? instance = constructor.Invoke(new object[0]);

        json = json.Substring(1).TrimStart();

        while (true)
        {
            SplitObject(json, out var current, out string? follower);
            if (current == null) break;

            addMethod.Invoke(instance, new object?[] { Deserialize(current, containedType) });

            if (follower == null) break;
            json = follower;
        }

        return instance;
    }

    public static object? Deserialize(Stream json,Type type)
    {
        using var reader = new StreamReader(json,System.Text.Encoding.UTF8);
        var result = Deserialize(reader.ReadToEnd(),type);
        reader.Close();
        json.Close();
        return result;
    }

    public static object? Deserialize(string json,Type type)
    {
        return DeserializeTrimmed(json.Replace("\n", "").Trim(), type);
    }

    private static object? DeserializeTrimmed(string json,Type type) {
        if (json.Equals("null")) return null;

        if (json.StartsWith('<'))
            return Deserialize(json);

        if (json.StartsWith('['))
            return DeserializeCollection(json, type);

        if (!json.StartsWith('{'))
            return DeserializePrimitive(json, type);

        json = json.Substring(1).TrimStart();

        Dictionary<string, string> textMap = new();
        Dictionary<string, string> ignoreCaseMap = new();
        while (true) {
            Split(json, out var current, out string? follower);
            if (current == null) break;

            textMap.Add(current.Item1,current.Item2);
            ignoreCaseMap.Add(current.Item1.ToLower(), current.Item1);

            if(follower == null) break;
            json = follower;
        }

        object? instance = type.GetConstructor(new Type[0])?.Invoke(new object[0]);
        if (instance == null) throw new Exception("Constructor is not found.");

        if (type.IsAssignableTo(typeof(IDictionary)))
        {
            //Dictionaryのデシリアライズ

            var entryType = type.GetGenericArguments();
            if(entryType[0] != typeof(string)) throw new Exception("Deserializable dictionary must have string key.");

            var addMethod = type.GetMethod("Add");
            foreach (var entry in textMap) addMethod!.Invoke(instance, new object?[] { entry.Key, DeserializeTrimmed(entry.Value, entryType[1]) });
        }
        else
        {
            //その他のデータのデシリアライズ

            foreach (var f in type.GetFields())
            {
                if (!f.IsDefined(typeof(JsonSerializableField))) continue;

                var name = f.Name;

                //あいまい一致
                if (f.IsDefined(typeof(JSFieldAmbiguous)) && ignoreCaseMap.TryGetValue(f.Name.ToLower(), out var tableName)) name = tableName;

                if (!textMap.TryGetValue(name, out var rawValue)) continue;

                try
                {
                    f.SetValue(instance, DeserializeTrimmed(rawValue, f.FieldType));
                }catch(Exception e)
                {
                    throw new Exception($"Substitute exception ({rawValue} => {f.Name} as {f.FieldType.FullName})", e);
                }
            }
        }

        return instance;
    }

    private static object? Deserialize(string json)
    {
        if (!json.StartsWith('<')) return null;

        string[] strings = json.Split('>', 2);
        strings[0] = strings[0].Substring(1);
        Type? type = null;
        type = Assembly.GetAssembly(typeof(JsonStructure))?.GetType(strings[0]);
        if (type != null)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(strings[0]);
                if (type != null) break;
            }
        }

        if (type == null) return null;

        return DeserializeTrimmed(json, type);
    }

    public static JsonRawStructure? DeserializeRaw(string json)
    {
        json = json.Trim();

        if (json.Equals("null")) return null;

        if (json.StartsWith('<'))
        {
            var top = json.Substring(0, json.IndexOf('>') + 1);
            var follower = DeserializeRaw(json.Substring(top.Length));
            if (follower != null) follower.Prefix = top;
            return follower;
        }

        if (json.StartsWith('['))
        {
            json = json.Substring(1).TrimStart();

            JsonRawStructure structure = new JsonRawStructure();
            structure.ContentsAsList = new();
            while (true)
            {
                SplitObject(json, out var current, out string? follower);
                if (current == null) break;

                structure.ContentsAsList.Add(current);

                if (follower == null) break;
                json = follower;
            }

            return structure;
        }

        if (!json.StartsWith('{'))
            return new JsonRawStructure() { AsRaw = json };

        json = json.Substring(1).TrimStart();

        Dictionary<string, string> textMap = new();
        while (true)
        {
            Split(json, out var current, out string? follower);
            if (current == null) break;

            textMap.Add(current.Item1, current.Item2);

            if (follower == null) break;
            json = follower;
        }

        {
            JsonRawStructure structure = new JsonRawStructure();
            structure.StructiveContents = new();
            foreach (var entry in textMap) structure.StructiveContents.Add(entry.Key, DeserializeRaw(entry.Value)!);
            return structure;
        }
    }

    private static string SerializeDictionary(IDictionary obj)
    {
        var valType = obj.GetType().GenericTypeArguments[1];

        string result = "{";
        bool isFirst = true;
        foreach (var key in obj.Keys)
        {
            if (!isFirst) result += ",";
            result += "\n\t";
            result += "\"" + (string)key + "\" : ";
            var val = obj[(string)key];
            if (val != null && !valType.Equals(val.GetType())) result += "<" + obj.GetType()!.Name + ">";
            result += (val?.Serialize()! ?? "null").Replace("\n", "\n\t");

            isFirst = false;
        }
        result += "\n}";

        return result;
    }
    

    private static string SerializeEnumerable(IEnumerable obj)
    {
        string result = "[";
        bool isFirst = true;
        foreach (var val in obj)
        {
            if (!isFirst) result += ",";
            result += "\n\t";
            result += val.Serialize().Replace("\n", "\n\t");
            isFirst = false;
        }
        result += "\n]";
        return result;
    }

    public static string Serialize(this object obj)
    {
        if (obj is JsonRawStructure structure)
        {
            if(structure.ContentsAsList != null)
            {
                string json = "";
                foreach(var item in structure.ContentsAsList) { if (json.Length > 0) json += ","; json += "\n" + item; }
                return $"[{json}\n]";
            }
            if (structure.AsRaw != null) return structure.AsRaw;
            if (structure.StructiveContents != null) return SerializeDictionary(structure.StructiveContents);
            return "null";
        }

        if (obj is int or byte or float or double or bool)
            return obj.ToString() ?? "null";
        if (obj is string)
            return "\"" + obj + "\"";
        if (obj is IDictionary dic && dic.GetType().GenericTypeArguments[0] == typeof(string))
            return SerializeDictionary(dic);
        if (obj is IList and not Array)
            return SerializeEnumerable((IEnumerable)obj);
    

        string result = "{";
        bool isFirst = true;
        foreach (var f in obj.GetType().GetFields())
        {
            if (!f.IsDefined(typeof(JsonSerializableField))) continue;

            var val = f.GetValue(obj);
            if (val == null && f.GetCustomAttribute<JsonSerializableField>()!.SkipSerializeIfIsNull) continue;

            if (!isFirst) result += ",";
            result += "\n\t";
            result += "\"" + f.Name + "\" : ";

            if (val != null && !f.FieldType.Equals(val.GetType()) && !val.GetType().IsPrimitive) result += "<" + val.GetType()!.Name + ">";
            result += (val?.Serialize()! ?? "null").Replace("\n", "\n\t");

            isFirst = false;
        }
        result += "\n}";

        return result;
    }

    public static void Split(string json,out Tuple<string,string>? current,out string? follower)
    {
        current = null;
        follower = null;
        
        if (!json.StartsWith("\"")) return;
        int index = 1;
        while (index < json.Length && json[index] != '"') index++;
        string label = json.Substring(1, index - 1);

        json = json.Substring(index + 1).Trim();
        if (json[0] != ':') return;

        SplitObject(json.Substring(1).TrimStart(), out var currentVal,out follower);
        if (currentVal != null) current = new(label, currentVal);
    }

    private static void SplitObject(string json,out string? current,out string? follower)
    {
        current = null;
        follower = null;
        int index = 0;

        bool inStr = false;
        int nested = 0;
        while (index < json.Length)
        {
            if (json[index] is '"' && (index == 0 || json[index - 1] is not '\\')) inStr = !inStr;
            else if (!inStr && json[index] is '{' or '[') nested++;
            else if (!inStr && json[index] is '}' or ']') nested--;

            if (nested == -1 || (nested == 0 && !inStr && json[index] is ',')) break;

            index++;
        }

        if (index > 0) current = json.Substring(0, index).Trim().Replace("\\\"","\"");
        if (index + 1 < json.Length) follower = json.Substring(index + 1).TrimStart();
    }

    static public string ShapeJsonString(string json)
    {
        string result = "";

        int read = 0, advanced = 0;
        int scopes = 0;

        string getCurrentString(bool requireAdvance = true)
        {
            string str = json.Substring(read, advanced).Trim();
            if (requireAdvance) advance();
            return str;
        }

        void appendNewLine()
        {
            result += "\n";
            for (int i = 0; i < scopes; i++) result += "\t";
        }

        bool readIgnored(bool requireAdvance = true)
        {
            while (true)
            {
                if (read + advanced >= json.Length) return true;
                if (!(json[read + advanced] is ' ' or '\n' or '\t' or '\r' or '\b')) break;
                advanced++;
            }
            if (requireAdvance) advance();
            return false;
        }

        bool readString()
        {
            if (json[read + advanced] != '\"') return false;
            advanced++;
            while (json[read + advanced] is not '\"' || json[read + advanced - 1] is '\\') advanced++;

            advanced++;
            return true;
        }

        bool readPrimitive()
        {
            char first = json[read + advanced];
            if (first is ',' or '{' or '}' or '[' or ']' or ':' or '\"') return false;

            while (!(json[read + advanced] is ',' or '}' or ']')) advanced++;
            return true;
        }

        void advance()
        {
            Debug.Log("Read:" + json.Substring(read,advanced));
            read += advanced;
            advanced = 0;
        }

        bool readOpener()
        {
            if (json[read + advanced] is '{' or '[')
            {
                advanced++;
                scopes++;
                return true;
            }
            return false;
        }

        bool readCloser()
        {
            if (json[read + advanced] is '}' or ']')
            {
                advanced++;
                scopes--;
                return true;
            }
            return false;
        }

        bool readColon()
        {
            if (json[read + advanced] is ':')
            {
                advanced++;
                return true;
            }
            return false;
        }

        bool readComma()
        {
            if (json[read + advanced] is ',')
            {
                advanced++;
                return true;
            }
            return false;
        }

        while (read + advanced < json.Length)
        {
            readIgnored();
            if (readOpener())
            {
                result += getCurrentString();
                appendNewLine();
                if (readIgnored()) break;
            }

            if (readCloser())
            {
                appendNewLine();
                result += getCurrentString();
                if (readIgnored()) break;
            }

            if (readColon())
            {
                advance();
                result += " : ";
                advance();
                if (readIgnored()) break;
            }

            if (readComma())
            {
                advance();
                result += ",";
                appendNewLine();
                if (readIgnored()) break;
            }

            if (readString())
            {
                result += getCurrentString();
                if (readIgnored()) break;
            }

            if (readPrimitive())
            {
                result += getCurrentString();
                if (readIgnored()) break;
            }
        }

        return result;
    }
}
