using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Hazel;
using BepInEx.Configuration;
using Nebula.Language;

namespace Nebula.Language
{
    public class Language
    {
        static private Language language;

        public Dictionary<string, string> languageSet;

        public ConfigEntry<string> entry;
        public string LangageConfig;

        public static string GetString(string key)
        {
            if (language.languageSet.ContainsKey(key))
            {
                return language.languageSet[key];
            }
            return "*" + key;
        }

        public Language()
        {
            languageSet = new Dictionary<string, string>();

            entry = NebulaPlugin.Instance.Config.Bind("Language","Language", "English");
        }

        public static void Load(string lang)
        {

            language = new Language();

            if (lang != null)
            {
                language.entry.Value = lang;
            }


            language.deserialize(GetDefaultColorStream());
            Dictionary<string, string> defaultColorSet = language.languageSet;


            Dictionary<string, string> ColorSet = defaultColorSet;
            if (language.deserialize(@"language\" + language.entry.Value + "_Color.dat"))
            {
                //翻訳セットに不足データがある場合デフォルト言語セットで補う
                foreach (KeyValuePair<string, string> pair in defaultColorSet)
                {
                    if (!language.languageSet.ContainsKey(pair.Key))
                    {
                        language.languageSet.Add(pair.Key, pair.Value);
                    }
                }
                ColorSet = language.languageSet;
            }


            language.deserialize(GetDefaultLanguageStream());
            Dictionary<string, string> defaultSet = language.languageSet;


            if (language.deserialize(@"language\" + language.entry.Value + ".dat"))
            {
                //翻訳セットに不足データがある場合デフォルト言語セットで補う
                foreach (KeyValuePair<string, string> pair in defaultSet)
                {
                    if (!language.languageSet.ContainsKey(pair.Key))
                    {
                        language.languageSet.Add(pair.Key, pair.Value);
                    }
                }
            }


            //色データを足しこむ
            foreach (KeyValuePair<string, string> pair in ColorSet)
            {
                language.languageSet.Add(pair.Key, pair.Value);
            }


        }

        public static void Load()
        {
            Load(null);
        }

        public static Stream GetDefaultLanguageStream()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("Nebula.Resources.Lang.dat");
        }

        public static Stream GetDefaultColorStream()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("Nebula.Resources.Color.dat");
        }

        public bool deserialize(string path)
        {
            try
            {
                using (StreamReader sr = new StreamReader(
                        path, Encoding.GetEncoding("utf-8")))
                {
                    return deserialize(sr);
                }
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool deserialize(Stream stream)
        {
            using (StreamReader sr = new StreamReader(
                    stream, Encoding.GetEncoding("utf-8")))
            {
                return deserialize(sr);
            }
        }

        public bool deserialize(StreamReader reader)
        {
            bool result = true;
            try
            {
                string data = "", line;


                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length < 3)
                    {
                        continue;
                    }
                    if (data.Equals(""))
                    {
                        data = line;
                    }
                    else
                    {
                        data += "," + line;
                    }
                }


                if (!data.Equals(""))
                {


                    JsonSerializerOptions option = new JsonSerializerOptions
                    {
                        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                        WriteIndented = true
                    };

                    languageSet = JsonSerializer.Deserialize<Dictionary<string, string>>("{ " + data + " }", option);

                    result = true;
                }
            }
            catch (Exception e)
            {
                result = false;
            }

            return result;
        }
    }
}
