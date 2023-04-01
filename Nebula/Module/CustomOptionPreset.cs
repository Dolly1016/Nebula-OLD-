namespace Nebula.Module;


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
    static public StringOption? SaveButton = null;

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

        foreach (var option in CustomOption.AllOptions)
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

    private void LoadVanillaOption(string option, int value)
    {
        var data = GameOptionsManager.Instance.CurrentGameOptions;
        switch (option)
        {
            case "vanilla.map":
                data.SetByte(ByteOptionNames.MapId, (byte)value);
                break;
            case "vanilla.impostors":
                data.SetInt(Int32OptionNames.NumImpostors, value);
                break;
            case "vanilla.confirmImpostor":
                data.SetBool(BoolOptionNames.ConfirmImpostor ,(value == 1));
                break;
            case "vanilla.emergencyMeeting":
                data.SetInt(Int32OptionNames.NumEmergencyMeetings, value);
                break;
            case "vanilla.anonymousVotes":
                data.SetBool(BoolOptionNames.AnonymousVotes , (value == 1));
                break;
            case "vanilla.emergencyCooldown":
                data.SetInt(Int32OptionNames.EmergencyCooldown, value);
                break;
            case "vanilla.discussionTime":
                data.SetInt(Int32OptionNames.DiscussionTime, value);
                break;
            case "vanilla.votingTime":
                data.SetInt(Int32OptionNames.VotingTime, value);
                break;
            case "vanilla.playerSpeed":
                data.SetFloat(FloatOptionNames.PlayerSpeedMod, value / 4f);
                break;
            case "vanilla.crewmateVision":
                data.SetFloat(FloatOptionNames.CrewLightMod, value / 4f);
                break;
            case "vanilla.impostorVision":
                data.SetFloat(FloatOptionNames.ImpostorLightMod , value / 4f);
                break;
            case "vanilla.killCooldown":
                data.SetFloat(FloatOptionNames.KillCooldown , value / 2f);
                break;
            case "vanilla.killDistance":
                data.SetInt(Int32OptionNames.KillDistance, value);
                break;
            case "vanilla.visualTasks":
                data.SetBool(BoolOptionNames.VisualTasks, (value == 1));
                break;
            case "vanilla.taskBarUpdates":
                data.SetInt(Int32OptionNames.TaskBarMode, value);
                break;
            case "vanilla.commonTasks":
                data.SetInt(Int32OptionNames.NumCommonTasks, value);
                break;
            case "vanilla.shortTasks":
                data.SetInt(Int32OptionNames.NumShortTasks, value);
                break;
            case "vanilla.longTasks":
                data.SetInt(Int32OptionNames.NumLongTasks, value);
                break;
        }
    }

    private void OutputVanillaOptions(StreamWriter writer)
    {
        var data = GameOptionsManager.Instance.CurrentGameOptions;

        writer.WriteLine("vanilla.map:" + data.MapId);
        writer.WriteLine("vanilla.impostors:" + data.NumImpostors);
        writer.WriteLine("vanilla.confirmImpostor:" + (data.GetBool(BoolOptionNames.ConfirmImpostor) ? 1 : 0));
        writer.WriteLine("vanilla.emergencyMeeting:" + data.GetInt(Int32OptionNames.NumEmergencyMeetings));
        writer.WriteLine("vanilla.anonymousVotes:" + (data.GetBool(BoolOptionNames.AnonymousVotes) ? 1 : 0));
        writer.WriteLine("vanilla.emergencyCooldown:" + data.GetInt(Int32OptionNames.EmergencyCooldown));
        writer.WriteLine("vanilla.discussionTime:" + data.GetInt(Int32OptionNames.DiscussionTime));
        writer.WriteLine("vanilla.votingTime:" + data.GetInt(Int32OptionNames.VotingTime));
        writer.WriteLine("vanilla.playerSpeed:" + (int)(data.GetFloat(FloatOptionNames.PlayerSpeedMod) * 4f));
        writer.WriteLine("vanilla.crewmateVision:" + (int)(data.GetFloat(FloatOptionNames.CrewLightMod) * 4f));
        writer.WriteLine("vanilla.impostorVision:" + (int)(data.GetFloat(FloatOptionNames.ImpostorLightMod) * 4f));
        writer.WriteLine("vanilla.killCooldown:" + (int)(data.GetFloat(FloatOptionNames.KillCooldown) * 2f));
        writer.WriteLine("vanilla.killDistance:" + data.GetInt(Int32OptionNames.KillDistance));
        writer.WriteLine("vanilla.visualTasks:" + (data.GetBool(BoolOptionNames.VisualTasks) ? 1 : 0));
        writer.WriteLine("vanilla.taskBarUpdates:" + (int)data.GetInt(Int32OptionNames.TaskBarMode));
        writer.WriteLine("vanilla.commonTasks:" + data.GetInt(Int32OptionNames.NumCommonTasks));
        writer.WriteLine("vanilla.shortTasks:" + data.GetInt(Int32OptionNames.NumShortTasks));
        writer.WriteLine("vanilla.longTasks:" + data.GetInt(Int32OptionNames.NumLongTasks));
    }

    private void Import()
    {
        if (notInitialize)
        {
            //デフォルトに戻す
            foreach (var option in CustomOption.AllOptions)
            {
                if (option.isProtected) continue;

                option.selection = option.defaultSelection;
            }
            GameOptionsManager.Instance.CurrentGameOptions.SetRecommendations(PlayerControl.AllPlayerControls.Count, true);
        }

        //読み込み
        foreach (var entry in options)
        {
            if (IsVanillaOption(entry.Key))
                LoadVanillaOption(entry.Key, entry.Value);
            else
                CustomOption.loadOptionWithoutSync(entry.Key, entry.Value);
        }

        CustomOption.ShareAllOptions.Invoke(CustomOption.AllOptions);
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
        foreach (var file in info.GetFiles())
        {
            name = file.Name;
            if (!name.EndsWith(".options")) continue;

            Presets.Add(new CustomOptionPresetInfo(name.Substring(0, name.Length - 8)));
        }
    }

    public void LoadMetaText(string text, ref string jumpTo, ref bool skipping, Dictionary<string, string> masterVariables, Dictionary<string, string> variables)
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

            var mapId = GameOptionsManager.Instance.CurrentGameOptions.MapId.ToString();

            masterVariables["Players"] = players;
            masterVariables["P"] = players;

            masterVariables["MapId"] = mapId;
            masterVariables["Map"] = mapId;
            masterVariables["M"] = mapId;

            while (reader.Peek() > -1)
            {
                text = reader.ReadLine().Replace(" ", "").Replace("\t", "");

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
                    if (conditionList.Count > 0 && (conditionList[conditionList.Count - 1] != 1))
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
                    if (conditionList.Count != 0 && (conditionList[conditionList.Count - 1] == 0))
                    {
                        var formula = new Module.Parser.FormulaAnalyzer(text.Substring(8), masterVariables, variables);
                        conditionList[conditionList.Count - 1] = (byte)(formula.GetResult().GetBool() ? 1 : 0);
                    }
                    else if (conditionList.Count > 0)
                        conditionList[conditionList.Count - 1] = 2;
                }
                else if (text == "#ELSE")
                {
                    //条件を反転させる
                    if (conditionList.Count > 0)
                        conditionList[conditionList.Count - 1] = (byte)((conditionList[conditionList.Count - 1] == 0) ? 1 : 2);
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
                    if (text.StartsWith("#")) preset.LoadMetaText(text.Substring(1), ref jumpTo, ref skipping, masterVariables, variables);
                    else
                    {
                        strings = text.Split(":");
                        if (strings.Length != 2) continue;

                        int result;
                        if (int.TryParse(strings[1], out result))
                        {
                            preset.options[strings[0]] = result;
                        }
                        else
                        {
                            var formula = new Module.Parser.FormulaAnalyzer(strings[1], masterVariables, variables);
                            preset.options[strings[0]] = formula.GetResult().GetInt();
                        }
                    }
                }
            }

            preset.Import();

            return true;
        }
        catch (Exception exp)
        {
            NebulaPlugin.Instance.Logger.Print(exp.ToString());
            return false;
        }
    }
}