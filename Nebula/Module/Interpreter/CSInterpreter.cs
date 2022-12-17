using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Nebula.Module.Interpreter;
using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace Nebula.Module;

public static class CSInterpreterDownloader
{
    private static string[] required = new string[]
    {
        "Microsoft.CodeAnalysis.CSharp",
        "Microsoft.CodeAnalysis.CSharp.Scripting"
    };
    public static async Task DownloadRequiredDll()
    {
        foreach(var r in required) await DllInstaller.DownloadDll(r);
    }

    public static void Download()
    {
        DownloadRequiredDll().Wait();
    }
}

public class CSInterpreter
{
    public class CSInterpreterSpace
    {
        public enum GameState
        {
            InLobby,
            InGame
        }

        public enum SabotageType
        {
            Reactor,
            LifeSupport,
            BlackOut,
            Communication
        }

        Color Color(float r, float g, float b) => new Color(r, g, b);
        Color Color(float r, float g, float b, float a) => new Color(r, g, b, a);

        CSPlayer Player(string name) => new CSPlayer(name);
        bool InvokeSabotage(string sabotageType) { return true; }
        bool InvokeSabotage(SabotageType sabotageType) { return true; }
        bool EndGame(string endReason) { return true; }
        bool StartGame(bool startImmediately = false) { return true; }
        bool RegisterCustomGameLogic(IEnumerator logic) { return true; }
        int Players { get => PlayerControl.AllPlayerControls.Count; }

    }

    TextBoxTMP textArea;
    Script<object> script;
    static CSInterpreterSpace? interpreterSpace = null;
    static ScriptOptions? option;

    static public void Initialize()
    {
        interpreterSpace = null;
    }

    public CSInterpreter()
    {
        textArea = GameObject.Instantiate(HudManager.Instance.Chat.TextArea);
        if(option==null)
            option = Microsoft.CodeAnalysis.Scripting.ScriptOptions.Default.AddImports(new string[] { "System.Collections.Generic", "System.Collections" });
        script = CSharpScript.Create("", option, typeof(CSInterpreterSpace));
    }

    public void Run(string code)
    {
        if (interpreterSpace == null) interpreterSpace = new CSInterpreterSpace();
        script = script.ContinueWith(code);
        script.RunAsync(interpreterSpace);
    }
}
