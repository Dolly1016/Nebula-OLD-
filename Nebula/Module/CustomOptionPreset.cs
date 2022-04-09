using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Nebula.Module
{

    public class CustomOptionPreset
    {
        public class CustomOptionPresetInfo
        {
            public string Name;
            public StringOption? Option;

            public CustomOptionPresetInfo(string name)
            {
                Name = name;
                Option = null;
            }
        }

        static public List<CustomOptionPresetInfo> Presets = new List<CustomOptionPresetInfo>();
        static public StringOption? SaveButton=null;

        Dictionary<string, int> options;
        bool notInitialize;

        public CustomOptionPreset()
        {
            options = new Dictionary<string, int>();
            notInitialize = false;
        }

        static public CustomOptionPreset Export()
        {
            var data = new CustomOptionPreset();

            foreach(var option in CustomOption.options)
            {
                if (option.isProtected) continue;

                data.options[option.identifierName] = option.selection;
            }

            return data;
        }

        private bool IsVanillaOption(string option)
        {
            return option.StartsWith("vanilla.");
        }

        private void LoadVanillaOption(string option,int value)
        {
            GameOptionsData data = PlayerControl.GameOptions;
            switch (option)
            {
                case "vanilla.map":
                    data.MapId = (byte)value;
                    break;
                case "vanilla.impostors":
                    data.NumImpostors = value;
                    break;
                case "vanilla.confirmImpostor":
                    data.ConfirmImpostor = (value==1);
                    break;
                case "vanilla.emergencyMeeting":
                    data.NumEmergencyMeetings = value;
                    break;
                case "vanilla.anonymousVotes":
                    data.AnonymousVotes = (value==1);
                    break;
                case "vanilla.emergencyCooldown":
                    data.EmergencyCooldown=value;
                    break;
                case "vanilla.discussionTime":
                    data.DiscussionTime = value;
                    break;
                case "vanilla.votingTime":
                    data.VotingTime = value;
                    break;
                case "vanilla.playerSpeed":
                    data.PlayerSpeedMod = value / 4f;
                    break;
                case "vanilla.crewmateVision":
                    data.CrewLightMod = value / 4f;
                    break;
                case "vanilla.impostorVision":
                    data.ImpostorLightMod = value / 4f;
                    break;
                case "vanilla.killCooldown":
                    data.KillCooldown = value / 2f;
                    break;
                case "vanilla.killDistance":
                    data.KillDistance = value;
                    break;
                case "vanilla.visualTasks":
                    data.VisualTasks = (value == 1);
                    break;
                case "vanilla.taskBarUpdates":
                    data.TaskBarMode = (TaskBarMode)value;
                    break;
                case "vanilla.commonTasks":
                    data.NumCommonTasks = value;
                    break;
                case "vanilla.shortTasks":
                    data.NumShortTasks = value;
                    break;
                case "vanilla.longTasks":
                    data.NumLongTasks = value;
                    break;
            }
        }

        private void OutputVanillaOptions(StreamWriter writer)
        {
            GameOptionsData data = PlayerControl.GameOptions;

            writer.WriteLine("vanilla.map:" + data.MapId);
            writer.WriteLine("vanilla.impostors:" + data.NumImpostors);
            writer.WriteLine("vanilla.confirmImpostor:" + (data.ConfirmImpostor ? 1 : 0));
            writer.WriteLine("vanilla.emergencyMeeting:" + data.NumEmergencyMeetings);
            writer.WriteLine("vanilla.anonymousVotes:" + (data.AnonymousVotes ? 1 : 0));
            writer.WriteLine("vanilla.emergencyCooldown:" + data.EmergencyCooldown);
            writer.WriteLine("vanilla.discussionTime:" + data.DiscussionTime);
            writer.WriteLine("vanilla.votingTime:" + data.VotingTime);
            writer.WriteLine("vanilla.playerSpeed:" + (int)(data.PlayerSpeedMod * 4f));
            writer.WriteLine("vanilla.crewmateVision:" + (int)(data.CrewLightMod * 4f));
            writer.WriteLine("vanilla.impostorVision:" + (int)(data.ImpostorLightMod*4f));
            writer.WriteLine("vanilla.killCooldown:" + (int)(data.KillCooldown*2f));
            writer.WriteLine("vanilla.killDistance:" + data.KillDistance);
            writer.WriteLine("vanilla.visualTasks:" + (data.VisualTasks ? 1 : 0));
            writer.WriteLine("vanilla.taskBarUpdates:" + (int)data.TaskBarMode);
            writer.WriteLine("vanilla.commonTasks:" + data.NumCommonTasks);
            writer.WriteLine("vanilla.shortTasks:" + data.NumShortTasks);
            writer.WriteLine("vanilla.longTasks:" + data.NumLongTasks);
        }

        private void Import()
        {
            if (notInitialize)
            {
                //デフォルトに戻す
                foreach (var option in CustomOption.options)
                {
                    if (option.isProtected) continue;

                    option.updateSelection(option.defaultSelection);

                    PlayerControl.GameOptions.SetRecommendations(PlayerControl.AllPlayerControls.Count, GameModes.LocalGame);
                }
            }
            
            //読み込み
            foreach (var entry in options)
            {
                if (IsVanillaOption(entry.Key))
                    LoadVanillaOption(entry.Key,entry.Value);
                else
                    CustomOption.loadOption(entry.Key, entry.Value);
            }
        }

        public void Output()
        {
            System.IO.Directory.CreateDirectory("Presets");

            StreamWriter writer = new StreamWriter("Presets/Output.options", false);

            OutputVanillaOptions(writer);

            foreach (var entry in options)
            {
                writer.WriteLine(entry.Key + ":" + entry.Value);
            }

            writer.Close();
        }

        public static void LoadPresets()
        {
            Presets.Clear();

            System.IO.Directory.CreateDirectory("Presets");

            var info = new DirectoryInfo("Presets");
            string name;
            foreach(var file in info.GetFiles())
            {
                name = file.Name;
                if (!name.EndsWith(".options")) continue;

                Presets.Add(new CustomOptionPresetInfo(name.Substring(0,name.Length-8)));
            }
        }

        public void LoadMetaText(string text,ref string jumpTo,ref bool skipping)
        {
            if (text == "NOT INITIALIZE") { notInitialize = true; return; }
            if (text.StartsWith("JUMP:"))
            {
                string[] strings = text.Split(":");
                if (strings.Length != 3) return;
                var formula = new Module.Parser.FormulaAnalyzer(strings[1]);
                if (formula.GetResult().GetBool())
                {
                    skipping = true;
                    jumpTo = strings[2];
                }
            }
        }

        public static bool LoadAndInput(string path)
        {
            try
            {
                StreamReader reader = new StreamReader(path);

                string text;
                string[] strings;

                CustomOptionPreset preset = new CustomOptionPreset();

                bool skipping = false;
                string jumpTo = "";

                while (reader.Peek() > -1)
                {
                    text=reader.ReadLine().Replace(" ","");

                    if (text.StartsWith("//")) continue;

                    if (skipping)
                    {
                        if (text.StartsWith("#TO:"))
                            if (text.Substring(4) == jumpTo)
                                skipping = false;
                    }
                    else
                    {
                        if (text.StartsWith("#")) preset.LoadMetaText(text.Substring(1), ref jumpTo, ref skipping);
                        strings = text.Split(":");
                        if (strings.Length != 2) continue;

                        preset.options[strings[0]] = int.Parse(strings[1]);
                    }
                }

                preset.Import();

                return true;
            }catch(Exception exp) {
                return false;
            }
        }
    }
}
