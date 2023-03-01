using AmongUs.Data;
using Innersloth.IO;
using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Module
{
    public abstract class DataEntry<T>
    {
        private T value;
        string name;
        DataSaver saver;

        public T Value { get { return value; } set {
                this.value = value;
                saver.SetValue(name, value);
            } }   

        public void SetValueWithoutSave(T value) { 
            this.value = value;
            saver.SetValue(name, value, true);
        }

        public DataEntry(string name, DataSaver saver, T defaultValue)
        {
            this.name = name;
            this.saver = saver;
            value = Parse(saver.GetValue(name, defaultValue).ToString());
        }

        public abstract T Parse(string str);
    }

    public class StringDataEntry : DataEntry<string>
    {
        public override string Parse(string str) { return str; }
        public StringDataEntry(string name, DataSaver saver,string defaultValue):base(name, saver, defaultValue) { }
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

    public class DataSaver 
    {
        private Dictionary<string, string> contents;
        string filename;

        public string GetValue(string name, object defaultValue)
        {
            if(contents.TryGetValue(name,out string value))
            {
                return value;
            }
            var res = contents[name] = defaultValue.ToString();
            return res;
        }

        public void SetValue(string name, object value,bool skipSave = false)
        {
            contents[name] = value.ToString();
            if(!skipSave)Save();
        }

        public DataSaver(string filename)
        {
            this.filename = "Nebula\\"+filename;
            Load();
        }

        public void Load()
        {
            string dataPathTo = FileIO.GetDataPathTo(new string[] { filename });

            if (!FileIO.Exists(dataPathTo))
            {
                contents=new();
                return;
            }
            contents = new();
            string[] vals = (FileIO.ReadAllText(dataPathTo)).Split("\n");
            foreach(string val in vals)
            {
                string[] str = val.Split(":", 2);
                if (str.Length != 2) continue;
                contents[str[0]] = str[1];
            }
        }

        public void Save()
        {
            Formatting formatting = Application.isEditor ? Formatting.Indented : Formatting.None;

            string strContents = "";
            foreach(var entry in contents){
                strContents += entry.Key + ":" + entry.Value + "\n";
            }
            FileIO.WriteAllText(FileIO.GetDataPathTo(new string[] { filename }), strContents);
        }
    }
}
