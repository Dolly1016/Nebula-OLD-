using Nebula.Expansion;
using Nebula.Module;

namespace Nebula.Tasks
{
    public class SpectreRancorLetterMinigame : NebulaMinigame
    {

        static SpectreRancorLetterMinigame()
        {
            ClassInjector.RegisterTypeInIl2Cpp<SpectreRancorLetterMinigame>();
        }

        public override void __Begin(PlayerTask playerTask)
        {
            MyTask = playerTask;
            MyNormTask = playerTask.GetComponent<NormalPlayerTask>();

            var states = new GameObject[] { gameObject.transform.GetChild(0).gameObject, gameObject.transform.GetChild(1).gameObject };

            PassiveButton button;

            //手紙の開封
            button = states[0].transform.GetChild(0).gameObject.AddComponent<PassiveButton>();
            button.OnMouseOut = new UnityEngine.Events.UnityEvent();
            button.OnMouseOver = new UnityEngine.Events.UnityEvent();
            button.OnClick.RemoveAllListeners();
            button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                if (amClosing == CloseState.Closing) return;

                states[1].gameObject.SetActive(true);
                states[0].gameObject.SetActive(false);
            }));

            //文書のとりだし
            IEnumerator getEnumerator()
            {
                var paperTransform = states[1].transform.GetChild(0);
                var letterTransform = states[1].transform.GetChild(1);
                Vector2 vec = new Vector2(0.7f,1.58f);
                float p = 0f;

                while (p < 1f)
                {
                    float num = 1f - Mathf.Pow(1f - p, 3.2f);
                    paperTransform.localPosition = vec * num * 0.65f;
                    letterTransform.localPosition = vec * -num * 0.7f;
                    p += Time.deltaTime * 1.5f;
                    if (p > 1f) p = 1f;
                    yield return null;
                }

                if (amClosing != CloseState.Closing)
                {
                    MyNormTask.NextStep();
                    Close();
                }
            }

            bool openFlag = false;
            button = states[1].transform.GetChild(0).gameObject.AddComponent<PassiveButton>();
            button.OnMouseOut = new UnityEngine.Events.UnityEvent();
            button.OnMouseOver = new UnityEngine.Events.UnityEvent();
            button.OnClick.RemoveAllListeners();
            button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                if (amClosing == CloseState.Closing) return;
                if (openFlag) return;

                StartCoroutine(getEnumerator().WrapToIl2Cpp());
                openFlag = true;
            }));
        }

    }


    public class SpectreRancorTask : NebulaPlayerTask
    {

        static Minigame? NebulaMinigamePrefab = null;

        Console? deliveryConsole = null;
        PlayerControl? deliveryPlayer = null;

        static SpectreRancorTask()
        {
            ClassInjector.RegisterTypeInIl2Cpp<SpectreRancorTask>();
        }

        public void UpdateDeliveryConsole()
        {
            var candidates = PlayerControl.AllPlayerControls.GetFastEnumerator().Where(p => p.PlayerId != PlayerControl.LocalPlayer.PlayerId && !p.Data.IsDead).ToArray();
            if (candidates.Length == 0) return;
            deliveryPlayer = candidates[NebulaPlugin.rnd.Next(candidates.Length)];
            deliveryConsole = ConsoleExpansion.ConsolizePlayer<AutoTaskConsole>(deliveryPlayer,"DeliveryConsole");
        }

        public void OnMeetingEnd()
        {
            if (taskStep % 3 != 1) return;
            if (!deliveryPlayer || !deliveryPlayer.Data.IsDead) return;
            UpdateDeliveryConsole();
            
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

            sb.Append(Language.Language.GetString("role.spectre.task.rancor"));

            if (!this.IsComplete)
            {
                sb.Append(" (");
                sb.Append(this.taskStep / 3);
                sb.Append("/");
                sb.Append(this.MaxStep / 3);
                sb.Append(")");
            }

            if (flag)
            {
                sb.Append("</color>");
            }
            sb.AppendLine();

            if (!this.IsComplete)
            {
                string phaseDetail = "";
                switch (taskStep % 3)
                {
                    case 0:
                        phaseDetail = Language.Language.GetString("role.spectre.task.rancor.phase.letter");
                        break;
                    case 1:
                        phaseDetail = Language.Language.GetString("role.spectre.task.rancor.phase.delivery").Replace("%PLAYER%", deliveryPlayer ? deliveryPlayer.name : "[ERROR]");
                        break;
                    case 2:
                        phaseDetail = Language.Language.GetString("role.spectre.task.rancor.phase.statue");
                        break;
                }
                sb.Append("    " + Language.Language.GetString("role.spectre.task.rancor.phase").Replace("%PHASE%", ((taskStep % 3) + 1).ToString()).Replace("%DETAIL%", phaseDetail));
                sb.AppendLine();
            }
        }

        public override bool __NextStep()
        {
            int nextStep = (taskStep + 1) % 3;
            if (nextStep == 1)
                UpdateDeliveryConsole();
            
            return true;
        }

        
        public override void __Initialize()
        {
            taskStep = 0;
            MaxStep = 3 * 2;

            if (Roles.Roles.Spectre.FriedConsoles.Count < MaxStep) MaxStep = Roles.Roles.Spectre.FriedConsoles.Count;

            LocationDirty = true;
            HasLocation = true;

            if (NebulaMinigamePrefab == null)
            {
                NebulaMinigamePrefab = AssetLoader.SpectreRancorMinigamePrefab.gameObject.AddComponent<SpectreRancorLetterMinigame>();

            }
            MinigamePrefab = NebulaMinigamePrefab;
        }

        public override bool __ValidConsole(Console console)
        {
            if (PlayerControl.LocalPlayer.Data.IsDead) return false;
            switch (taskStep % 3) {
                case 0:
                    return console.gameObject.name.StartsWith("NoS-SpectreFried");
                case 1:
                    return deliveryConsole ? (console.GetInstanceID() == deliveryConsole.GetInstanceID()) : false;
                case 2:
                    return console.gameObject.name.StartsWith("NoS-SpectreFried");
            }
            return false;
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
