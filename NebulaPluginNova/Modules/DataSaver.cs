﻿using Innersloth.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Modules;

public abstract class DataEntry<T> where T : notnull
{
    private T value;
    string name;
    DataSaver saver;

    public T Value
    {
        get { return value; }
        set
        {
            this.value = value;
            saver.SetValue(name, Serialize(value));
        }
    }

    public void SetValueWithoutSave(T value)
    {
        this.value = value;
        saver.SetValue(name, Serialize(value), true);
    }

    public DataEntry(string name, DataSaver saver, T defaultValue)
    {
        this.name = name;
        this.saver = saver;
        value = Parse(saver.GetValue(name, Serialize(defaultValue)));
    }

    public abstract T Parse(string str);
    protected virtual string Serialize(T value) => value.ToString()!;
}

public class StringDataEntry : DataEntry<string>
{
    public override string Parse(string str) { return str; }
    public StringDataEntry(string name, DataSaver saver, string defaultValue) : base(name, saver, defaultValue) { }
}

public class FloatDataEntry : DataEntry<float>
{
    public override float Parse(string str) { return float.Parse(str); }
    public FloatDataEntry(string name, DataSaver saver, float defaultValue) : base(name, saver, defaultValue) { }
}

public class ByteDataEntry : DataEntry<byte>
{
    public override byte Parse(string str) { return byte.Parse(str); }
    public ByteDataEntry(string name, DataSaver saver, byte defaultValue) : base(name, saver, defaultValue) { }
}

public class IntegerDataEntry : DataEntry<int>
{
    public override int Parse(string str) { return int.Parse(str); }
    public IntegerDataEntry(string name, DataSaver saver, int defaultValue) : base(name, saver, defaultValue) { }
}

public class BooleanDataEntry : DataEntry<bool>
{
    public override bool Parse(string str) { return bool.Parse(str); }
    public BooleanDataEntry(string name, DataSaver saver, bool defaultValue) : base(name, saver, defaultValue) { }
}

public class IntegerTupleAryDataEntry : DataEntry<(int,int)[]>
{
    public override (int,int)[] Parse(string str) {
        if (str == "Empty") return new (int, int)[0];

        var strings = str.Split('|');
        (int, int)[] result = new (int, int)[strings.Length];
        for(int i = 0; i < result.Length; i++)
        {
            var tuple = strings[i].Split(',');
            result[i] = (int.Parse(tuple[0]), int.Parse(tuple[1]));
        }
        return result;
    }

    protected override string Serialize((int, int)[] value)
    {
        if (value.Length == 0) return "Empty";
        StringBuilder builder = new();
        foreach(var tuple in value)
        {
            if(builder.Length>0)builder.Append('|');
            builder.Append(tuple.Item1 + ',' + tuple.Item2);
        }
        return builder.ToString();
    }

    public IntegerTupleAryDataEntry(string name, DataSaver saver, (int,int)[] defaultValue) : base(name, saver, defaultValue) { }
}

public class StringTupleAryDataEntry : DataEntry<(string, string)[]>
{
    public override (string, string)[] Parse(string str)
    {
        if (str == "Empty") return new (string, string)[0];

        var strings = str.Split('|');
        (string, string)[] result = new (string, string)[strings.Length];
        for (int i = 0; i < result.Length; i++)
        {
            var tuple = strings[i].Split(',');
            result[i] = (tuple[0], tuple[1]);
        }
        return result;
    }

    protected override string Serialize((string, string)[] value)
    {
        if (value.Length == 0) return "Empty";
        StringBuilder builder = new();
        foreach (var tuple in value)
        {
            if (builder.Length > 0) builder.Append('|');

            builder.Append(tuple.Item1 + ',' + tuple.Item2);
        }
        return builder.ToString();
    }

    public StringTupleAryDataEntry(string name, DataSaver saver, (string, string)[] defaultValue) : base(name, saver, defaultValue) { }
}

public class StringArrayDataEntry : DataEntry<string[]>
{
    public override string[] Parse(string str)
    {
        if (str == "Empty") return new string[0];

        return str.Split('|');
    }

    protected override string Serialize(string[] value)
    {
        if (value.Length == 0) return "Empty";
        StringBuilder builder = new();
        foreach (var elem in value)
        {
            if (builder.Length > 0) builder.Append('|');

            builder.Append(elem);
        }
        return builder.ToString();
    }

    public StringArrayDataEntry(string name, DataSaver saver, string[] defaultValue) : base(name, saver, defaultValue) { }
}

public class DataSaver
{
    private Dictionary<string, string> contents = new();
    string filename;

    public string GetValue(string name, object defaultValue)
    {
        if (contents.TryGetValue(name, out string? value))
        {
            return value!;
        }
        var res = contents[name] = defaultValue.ToString()!;
        return res;
    }

    public void SetValue(string name, object value, bool skipSave = false)
    {
        contents[name] = value.ToString()!;
        if (!skipSave) Save();
    }

    public DataSaver(string filename)
    {
        this.filename = "NebulaOnTheShip\\" + filename + ".dat";
        Load();
    }

    public void Load()
    {
        string dataPathTo = FileIO.GetDataPathTo(new string[] { filename });

        if (!FileIO.Exists(dataPathTo)) return;
        
        string[] vals = (FileIO.ReadAllText(dataPathTo)).Split("\n");
        foreach (string val in vals)
        {
            string[] str = val.Split(":", 2);
            if (str.Length != 2) continue;
            contents[str[0]] = str[1];
        }
    }

    public void Save()
    {
        string strContents = "";
        foreach (var entry in contents)
        {
            strContents += entry.Key + ":" + entry.Value + "\n";
        }
        FileIO.WriteAllText(FileIO.GetDataPathTo(new string[] { filename }), strContents);
    }
}