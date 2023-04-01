using Nebula.Expansion;
using Nebula.Map;
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
                    Console.name = "NoS-Used";
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

    public class SpectreRancorStatueMinigame : NebulaMinigame
    {
        static SpectreRancorStatueMinigame()
        {
            ClassInjector.RegisterTypeInIl2Cpp<SpectreRancorStatueMinigame>();
        }

        public override void __Begin(PlayerTask playerTask)
        {

            MyTask = playerTask;
            MyNormTask = playerTask.GetComponent<NormalPlayerTask>();

            var states = new GameObject[] { gameObject.transform.GetChild(0).gameObject, gameObject.transform.GetChild(1).gameObject };

            PassiveButton button;

            //像の破壊
            button = states[0].AddComponent<PassiveButton>();
            button.OnMouseOut = new UnityEngine.Events.UnityEvent();
            button.OnMouseOver = new UnityEngine.Events.UnityEvent();
            button.OnClick.RemoveAllListeners();
            button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                if (amClosing == CloseState.Closing) return;
                if (actionCoolDown > 0f) return;

                if (progress < 5)
                {
                    SoundPlayer.PlaySound(NebulaPlugin.rnd.Next(1) == 0 ? AudioAsset.SpectreStatueCrush0 : AudioAsset.SpectreStatueCrush1).pitch = (0.75f + (float)NebulaPlugin.rnd.NextDouble() * 0.1f);

                    states[0].GetComponent<SpriteRenderer>().sprite = Roles.Roles.Spectre.spectreStatueSprites[progress].GetSprite();
                    StartCoroutine(CoHit().WrapToIl2Cpp());
                }
                else
                {
                    SoundPlayer.PlaySound(AudioAsset.SpectreStatueBroken);

                    states[1].gameObject.SetActive(true);
                    states[0].gameObject.SetActive(false);

                    //アニメーションを表示する場合
                    StartCoroutine(CoAnimation().WrapToIl2Cpp());

                    /*
                    if (!MyTask.TryCast<SpectreRancorTask>())
                    {
                        //アニメーションを出さない場合
                        MyNormTask.NextStep();
                        Close();
                    }
                    else
                    {
                        //アニメーションを表示する場合
                        StartCoroutine(CoAnimation().WrapToIl2Cpp());
                    }
                    */
                }
                actionCoolDown = 0.5f;
                progress++;
            }));

            //狐のアニメーション
            IEnumerator CoAnimation()
            {
                var obj = new GameObject("Fox");
                obj.layer = LayerExpansion.GetUILayer();
                obj.transform.SetParent(transform);
                obj.transform.localPosition = new Vector3(0.1f, 0.4f, -20f);
                obj.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
                var renderer = obj.AddComponent<SpriteRenderer>();
                renderer.color = Color.white.AlphaMultiplied(0.92f);
                StartCoroutine(Roles.Roles.Spectre.CoAnimateFox(renderer, 4f).WrapToIl2Cpp());

                yield return Effects.Wait(3f).WrapToManaged();

                if (amClosing != CloseState.Closing)
                {
                    MyNormTask.NextStep();
                    Close();

                    if (!PlayerControl.LocalPlayer.Data.IsDead)
                    {
                        var pair = Roles.Roles.Spectre.CustomConsoles.FirstOrDefault(pair => pair.Value.name == Console.gameObject.name);
                        RPCEventInvoker.SpectreReform(pair.Key);
                    }
                }
            }

            IEnumerator CoHit()
            {
                float p = 0f;
                Vector2 Direction = Vector2.leftVector * 0.035f;
                Direction = Direction.Rotate((float)NebulaPlugin.rnd.NextDouble() * 20f - 10f);
                if (NebulaPlugin.rnd.Next(1) == 0) Direction *= -1f;
                while (p < 1f)
                {
                    p += Time.deltaTime * 3.5f;
                    var vec = Direction * Mathf.Sin(Mathf.PI * p * 2f);
                    states[0].transform.localPosition = new Vector3(vec.x, vec.y, -10f);
                    yield return null;
                }
                states[0].transform.localPosition = new Vector3(0, 0, -10f);
                
            }
        }

        public void Update()
        {
            if (actionCoolDown > 0f) actionCoolDown -= Time.deltaTime;
        }

        private float actionCoolDown = 0f;
        private int progress = 0;

    }


    public class SpectreRancorTask : NebulaPlayerTask
    {

        public static Minigame? LetterMinigamePrefab = null;
        public static Minigame? StatueMinigamePrefab = null;

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
            if (nextStep == 2) Events.Schedule.RegisterPreMeetingAction(() => deliveryPlayer.AddTask(byte.MaxValue - 4), 0);
            if (nextStep == 1) UpdateDeliveryConsole();
            

            UpdateMinigamePrefab();

            return true;
        }

        public static void SetUpMinigamePrefab()
        {
            if (LetterMinigamePrefab == null)
                LetterMinigamePrefab = AssetLoader.SpectreRancorMinigamePrefab.gameObject.AddComponent<SpectreRancorLetterMinigame>();
            if (StatueMinigamePrefab == null)
                StatueMinigamePrefab = AssetLoader.SpectreStatueMinigamePrefab.gameObject.AddComponent<SpectreRancorStatueMinigame>();            
        }

        public override void __Initialize()
        {
            taskStep = 0;

            int step = 3;

            int count;

            count = 0;
            Roles.Roles.Spectre.letterTaskSetting.ForAllValidLoc(GameOptionsManager.Instance.CurrentGameOptions.MapId, (data) => {
                count++;
            });
            if (count < step) step = count;

            count = 0;
            Roles.Roles.Spectre.statueTaskSetting.ForAllValidLoc(GameOptionsManager.Instance.CurrentGameOptions.MapId, (data) => {
                count++;
            });
            if (count / 2 < step) step = count / 2;


            MaxStep = step * 3;

            LocationDirty = true;
            HasLocation = true;

            SetUpMinigamePrefab();
            UpdateMinigamePrefab();
        }

        public override bool __ValidConsole(Console console)
        {
            if (PlayerControl.LocalPlayer.Data.IsDead) return false;
            switch (taskStep % 3) {
                case 0:
                    return console.gameObject.name.StartsWith("NoS-SpectreLetter");
                case 1:
                    return deliveryConsole ? (console.GetInstanceID() == deliveryConsole.GetInstanceID()) : false;
                case 2:
                    return console.gameObject.name.StartsWith("NoS-SpectreStatue");
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

        public override Minigame GetMinigamePrefab()
        {
            if (taskStep % 3 == 0)
                return LetterMinigamePrefab;
            else
                return StatueMinigamePrefab;
        }

        public void UpdateMinigamePrefab()
        {
            MinigamePrefab = GetMinigamePrefab();
        }
    }

    public class SpectreRancorAdditionalTask : NebulaPlayerTask
    {

        static SpectreRancorAdditionalTask()
        {
            ClassInjector.RegisterTypeInIl2Cpp<SpectreRancorAdditionalTask>();
        }

        public override void __AppendTaskText(Il2CppSystem.Text.StringBuilder sb)
        {
            bool flag = false;
            if (this.IsComplete)
            {
                sb.Append("<color=#00DD00FF>");
                flag = true;
            }
            else 
            {
                sb.Append("<color=#FF6060FF>");
                flag = true;
            }

            sb.Append(Language.Language.GetString("role.spectre.task.rancor.player"));
            sb.Append("</color>");

            sb.AppendLine();
            
        }

        public override bool __NextStep()
        {
            return true;
        }


        public override void __Initialize()
        {
            taskStep = 0;
            MaxStep = 1;

            LocationDirty = true;
            HasLocation = true;

            SpectreRancorTask.SetUpMinigamePrefab();
            MinigamePrefab = SpectreRancorTask.StatueMinigamePrefab;
        }

        public override bool __ValidConsole(Console console)
        {
            //if (PlayerControl.LocalPlayer.Data.IsDead) return false;
            return console.gameObject.name.StartsWith("NoS-SpectreStatue");
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
