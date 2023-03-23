using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Utilities;

public class JsonStringBuilder
{
    private StringBuilder builder = new();
    private int indent = 0;
    public void Append(string text) { builder.Append(text); }
    public void AppendLine()
    {
        builder.AppendLine();
        for (int i = 0; i < indent; i++) builder.Append("    ");
    }

    public void IncreaseIndent() { indent++; }
    public void DecreaseIndent() { if (indent > 0) indent--; }
    public override String ToString() { return builder.ToString(); }
}

public interface JsonContent
{
    void Generate(JsonStringBuilder builder);
}

public class JsonStringContent : JsonContent
{
    private string str;
    public void Generate(JsonStringBuilder builder) { 
        builder.Append("\"");
        builder.Append(str);
        builder.Append("\"");
    }

    public JsonStringContent(string content)
    {
        str= content;
    }
}

public class JsonBooleanContent : JsonContent
{
    private bool value;
    public void Generate(JsonStringBuilder builder)
    {
        builder.Append(value ? "true" : "false");
    }

    public JsonBooleanContent(bool content)
    {
        value = content;
    }
}

public class JsonObjectContent : JsonContent
{
    private List<Tuple<string, JsonContent>> contents = new();

    public int Count => contents.Count;
    public void AddContent(string label,JsonContent content) { contents.Add(new(label, content)); }
    public void Generate(JsonStringBuilder builder)
    {
        builder.Append("{");
        builder.IncreaseIndent();
        int i = 0;
        foreach(var c in contents)
        {
            if (i != 0) builder.Append(",");
            builder.AppendLine();
            builder.Append("\"");
            builder.Append(c.Item1);
            builder.Append("\"");
            builder.Append(":");
            c.Item2.Generate(builder);
            i++;
        }
        builder.DecreaseIndent();
        builder.AppendLine();
        builder.Append("}");

    }
}

public class JsonArrayContent : JsonContent
{
    private List<JsonContent> contents = new();

    public int Length => contents.Count;
    public void AddContent(JsonContent content) { contents.Add(content); }
    public void Generate(JsonStringBuilder builder)
    {
        builder.Append("[");
        builder.IncreaseIndent();
        int i = 0;
        foreach (var c in contents)
        {
            if (i != 0) builder.Append(",");
            builder.AppendLine();
            c.Generate(builder);
            i++;
        }
        builder.DecreaseIndent();
        builder.AppendLine();
        builder.Append("]");
    }
}

public class JsonGenerator
{
    public JsonObjectContent RootContent { get; private set; } = new JsonObjectContent();

    public JsonGenerator() { }

    public string Generate()
    {
        JsonStringBuilder builder = new JsonStringBuilder();
        RootContent.Generate(builder);
        return builder.ToString();
    }
}
