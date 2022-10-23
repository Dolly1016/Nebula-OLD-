﻿using System;
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
        static private Language? language=null;
        static private Dictionary<string, string> defaultLanguageSet=new Dictionary<string, string>();

        public Dictionary<string, string> languageSet;


        public static string GetString(string key)
        {
            if (language?.languageSet.ContainsKey(key)??false)
            {
                return language.languageSet[key];
            }
            return "*" + key;
        }

        public static bool CheckValidKey(string key)
        {
            return language?.languageSet.ContainsKey(key) ?? true;
        }

        public static void AddDefaultKey(string key,string format)
        {
            defaultLanguageSet.Add(key, format);
            if (!CheckValidKey(key)) language.languageSet.Add(key,format);
        }


        public Language()
        {
            languageSet = new Dictionary<string, string>(defaultLanguageSet);
        }


        public static string GetLanguage(uint language)
        {
            switch (language)
            {
                case 0:
                    return "English";
                case 1:
                    return "Latam";
                case 2:
                    return "Brazilian";
                case 3:
                    return "Portuguese";
                case 4:
                    return "Korean";
                case 5:
                    return "Russian";
                case 6:
                    return "Dutch";
                case 7:
                    return "Filipino";
                case 8:
                    return "French";
                case 9:
                    return "German";
                case 10:
                    return "Italian";
                case 11:
                    return "Japanese";
                case 12:
                    return "Spanish";
                case 13:
                    return "SChinese";
                case 14:
                    return "TChinese";
                case 15:
                    return "Irish";
            }
            return "English";
        }

        public static void LoadDefaultKey()
        {
            AddDefaultKey("option.empty", "");
        }
        public static void Load()
        {
            string lang = GetLanguage((uint)AmongUs.Data.DataManager.Settings.Language.CurrentLanguage);
            

            language = new Language();

            language.deserialize(GetDefaultColorStream());
            language.deserialize(GetDefaultLanguageStream());

            var builtinLang = GetBuiltinLanguageStream(lang);
            if (builtinLang != null) language.deserialize(builtinLang);

            language.deserialize(@"language\" + lang + "_Color.dat");
            language.deserialize(@"language\" + lang + ".dat");

            /*
            language.deserialize(GetDefaultColorStream());
            Dictionary<string, string> defaultColorSet = language.languageSet;


            Dictionary<string, string> ColorSet = defaultColorSet;
            
            if (language.deserialize(@"language\" + lang + "_Color.dat"))
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


            if (language.deserialize(@"language\" + lang + ".dat"))
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
            */

        }

        public static Stream GetDefaultLanguageStream()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("Nebula.Resources.Lang.dat");
        }

        public static Stream GetDefaultColorStream()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("Nebula.Resources.Color.dat");
        }

        public static Stream? GetBuiltinLanguageStream(string lang)
        {
            try
            {
                return Assembly.GetExecutingAssembly().GetManifestResourceStream("Nebula.Resources.Languages." + lang + ".dat");
            }
            catch
            {
                return null;
            }
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

                    var deserialized = JsonSerializer.Deserialize<Dictionary<string, string>>("{ " + data + " }", option);
                    foreach(var entry in deserialized)
                    {
                        languageSet[entry.Key]=entry.Value;
                    }

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
