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

        public void LoadMetaText(string text,ref string jumpTo,ref bool skipping,Dictionary<string,string> masterVariables, Dictionary<string, string> variables)
        {
            if (text == "NOT INITIALIZE") { notInitialize = true; return; }
            if (text.StartsWith("JUMP:"))
            {
                string[] strings = text.Split(":");
                if (strings.Length != 3) return;
                var formula = new Module.Parser.FormulaAnalyzer(strings[1], masterVariables, variables);
                if (formula.GetResult().GetBool())
                {
                    skipping = true;
                    jumpTo = strings[2];
                }
            }
            if (text.StartsWith("SUBSTITUTE:"))
            {
                string[] strings = text.Split(":");
                if (strings.Length != 3) return;
                var formula = new Module.Parser.FormulaAnalyzer(strings[2], masterVariables, variables);
                variables[strings[1]] = formula.GetResult().GetInt().ToString();
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

                /* JUMP,TO構文 */
                bool skipping = false;
                string jumpTo = "";
                /* IF 構文 0:一度も条件を満たしていない 1:条件を満たしている 2:過去に条件を満たしている */
                List<byte> conditionList = new List<byte>();

                /* 変数テーブル */
                Dictionary<string, string> variables = new Dictionary<string, string>();
                Dictionary<string, string> masterVariables = new Dictionary<string, string>();

                var players = PlayerControl.AllPlayerControls.Count.ToString();

                var mapId = PlayerControl.GameOptions.MapId.ToString();

                masterVariables["Players"] = players;
                masterVariables["P"] = players;

                masterVariables["MapId"] = mapId;
                masterVariables["Map"] = mapId;
                masterVariables["M"] = mapId;

                while (reader.Peek() > -1)
                {
                    text=reader.ReadLine().Replace(" ","").Replace("\t","");

                    if (text.StartsWith("//")) continue;

                    //JUMPの処理
                    if (skipping)
                    {
                        if (text.StartsWith("#TO:"))
                            if (text.Substring(4) == jumpTo)
                                skipping = false;
                    }

                    //IF関連の処理
                    if (text.StartsWith("#IF:"))
                    {
                        //条件を考慮するべき時のみ計算する
                        if (conditionList.Count > 0 && (conditionList[conditionList.Count - 1]!=1))
                        {
                            conditionList.Add(2);
                        }
                        else
                        {
                            var formula = new Module.Parser.FormulaAnalyzer(text.Substring(4), masterVariables, variables);
                            conditionList.Add((byte)(formula.GetResult().GetBool() ? 1 : 0));
                        }
                    }
                    else if (text.StartsWith("#ELSEIF:"))
                    {
                        //条件を考慮するべき時は計算する
                        if (conditionList.Count != 0 && (conditionList[conditionList.Count - 1]==0))
                        {
                            var formula = new Module.Parser.FormulaAnalyzer(text.Substring(8), masterVariables, variables);
                            conditionList[conditionList.Count - 1] = (byte)(formula.GetResult().GetBool()?1:0);
                        }
                        else if (conditionList.Count > 0)
                            conditionList[conditionList.Count - 1] = 2;
                    }
                    else if (text == "#ELSE") {
                        //条件を反転させる
                        if (conditionList.Count > 0)
                            conditionList[conditionList.Count - 1] = (byte)((conditionList[conditionList.Count - 1]==0)?1:2);
                    }
                    else if (text == "#ENDIF")
                    {
                        //IF文をぬける
                        if (conditionList.Count > 0)
                            conditionList.RemoveAt(conditionList.Count - 1);
                    }

                    //読み飛ばさない場合
                    if (!skipping && (conditionList.Count == 0 || (conditionList.Count > 0 && conditionList[conditionList.Count - 1] == 1)))
                    {
                        if (text.StartsWith("#")) preset.LoadMetaText(text.Substring(1), ref jumpTo, ref skipping,masterVariables,variables);
                        else
                        {
                            strings = text.Split(":");
                            if (strings.Length != 2) continue;

                            int result;
                            if (int.TryParse(strings[1], out result)) {
                                preset.options[strings[0]] = result;
                            }
                            else
                            {
                                var formula = new Module.Parser.FormulaAnalyzer(strings[1],masterVariables,variables);
                                preset.options[strings[0]] = formula.GetResult().GetInt();
                            }
                        }
                    }
                }

                preset.Import();

                return true;
            }catch(Exception exp) {
                NebulaPlugin.Instance.Logger.Print(exp.ToString());
                return false;
            }
        }
    }
}
