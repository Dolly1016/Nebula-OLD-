using InnerNet;
using Nebula.Module;
using static Nebula.Tasks.OpportunistTask;

namespace Nebula.Tasks
{
    public class SpectreFriedMinigame : NebulaMinigame
    {

        static SpectreFriedMinigame()
        {
            ClassInjector.RegisterTypeInIl2Cpp<SpectreFriedMinigame>();
        }

        public override void __Begin(PlayerTask playerTask)
        {
            MyTask = playerTask;
            MyNormTask = playerTask.GetComponent<NormalPlayerTask>();
            GameObject[] frieds = new GameObject[] { gameObject.transform.GetChild(2).gameObject, gameObject.transform.GetChild(3).gameObject, gameObject.transform.GetChild(4).gameObject };
            int eaten = 0;
            int eatenSubcounter = 0;

            foreach (var f in frieds)
            {
                for (int i = 2; i >= 0; i--)
                {
                    var t = f.transform.GetChild(i); ;
                    var button = t.gameObject.AddComponent<PassiveButton>();
                    button.OnMouseOut = new UnityEngine.Events.UnityEvent();
                    button.OnMouseOver = new UnityEngine.Events.UnityEvent();
                    button.OnClick.RemoveAllListeners();
                    int index = i;
                    GameObject? nextObj = i != 0 ? f.transform.GetChild(i - 1).gameObject : null;
                    button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>{
                        if (amClosing == CloseState.Closing) return;

                        SoundPlayer.PlaySound(AudioAsset.SpectreFried).pitch = 1.0f + (float)NebulaPlugin.rnd.NextDouble() * 0.3f;
                        eatenSubcounter++;

                        if (eatenSubcounter < 3) return;

                        eatenSubcounter = 0;
                        t.gameObject.SetActive(false);

                        if (nextObj != null)
                        {
                            nextObj.SetActive(true);
                        }
                        else
                        {
                            eaten++;
                            if (eaten == 3)
                            {
                                MyNormTask.NextStep();
                                Close();
                                var pair = Roles.Roles.Spectre.CustomConsoles.FirstOrDefault(pair => pair.Value.name == Console.gameObject.name);
                                RPCEventInvoker.SpectreReform(pair.Key);
                            }
                        }
                    }));
                }
            }
        }

    }


    public class SpectreFriedTask : NebulaPlayerTask { 

        static Minigame? NebulaMinigamePrefab = null;
    
        static SpectreFriedTask()
        {
            ClassInjector.RegisterTypeInIl2Cpp<SpectreFriedTask>();
        }

        public override void __AppendTaskText(Il2CppSystem.Text.StringBuilder sb)
        {
            bool flag = false;
            if (this.IsComplete)
            {
                sb.Append("<color=#00DD00FF>");
                flag = true;
            }
            else if (taskStep > 0)
            {
                sb.Append("<color=#FFFF00FF>"); 
                flag = true;
            }

            /*
            sb.Append(DestroyableSingleton<TranslationController>.Instance.GetString(StartAt));
            sb.Append(": ");
            */

            sb.Append(Language.Language.GetString("role.spectre.task.fried"));

            if (!this.IsComplete)
            {
                sb.Append(" (");
                sb.Append(this.taskStep);
                sb.Append("/");
                sb.Append(this.MaxStep);
                sb.Append(")");
            }

            if (flag)
            {
                sb.Append("</color>");
            }
            sb.AppendLine();
        }

        public override bool __NextStep()
        {
            return true;
        }


        public override void __Initialize()
        {
            
            taskStep = 0;
            
            MaxStep = (int)Roles.Roles.Spectre.numOfTheFriedRequireToWinOption.getFloat();

            int count;

            count = 0;
            Roles.Roles.Spectre.friedTaskSetting.ForAllValidLoc(GameOptionsManager.Instance.CurrentGameOptions.MapId, (data) => {
                count++;
            });
            if (count < MaxStep) MaxStep = count;

            LocationDirty = true;
            HasLocation = true;

            if (NebulaMinigamePrefab == null) {
                NebulaMinigamePrefab = AssetLoader.SpectreFriedMinigamePrefab.gameObject.AddComponent<SpectreFriedMinigame>();

            }
            MinigamePrefab = NebulaMinigamePrefab;
        }

        public override bool __ValidConsole(Console console) {
            if (PlayerControl.LocalPlayer.Data.IsDead) return false;
            return console.gameObject.name.StartsWith("NoS-SpectreFried"); 
        }

        public override bool __IsCompleted()
        {
            return taskStep >= MaxStep;
        }

        public override void __GetLocations(ref Il2CppSystem.Collections.Generic.List<Vector2> __result)
        {
            if (__IsCompleted()) return;

            __result = FindValidConsolesPositions();
        }
    }
}
