using Cpp2IL.Core.Extensions;
using Nebula.Patches;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.Core.Internal;

namespace Nebula;


public class NebulaManager : MonoBehaviour
{
    static NebulaManager()
    {
        ClassInjector.RegisterTypeInIl2Cpp<NebulaManager>();
    }

    static public IEnumerator CaptureAndSave()
    {
        yield return new WaitForEndOfFrame();
        Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();

        File.WriteAllBytes(Patches.NebulaOption.CreateDirAndGetPictureFilePath(out string displayPath), tex.EncodeToPNG());
    }

    public GameObject? CommandInputField = null;

    public void Update()
    {
        //スクリーンショット
        if (!Components.TextInputField.ValidField && Input.GetKeyDown(KeyCode.P)) StartCoroutine(CaptureAndSave().WrapToIl2Cpp());

        /* ホスト専用コマンド */
        if (AmongUsClient.Instance && AmongUsClient.Instance.AmHost && AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started)
        {
            //ゲーム強制終了
            if (Input.GetKey(Module.NebulaInputManager.metaControlInput.keyCode) && Input.GetKey(Module.NebulaInputManager.noGameInput.keyCode))
            {
                Game.GameData.data.IsCanceled = true;
            }
        }

        //デバッグモード専用
        if (!Patches.NebulaOption.configGameControl.Value) return;

        if (AmongUsClient.Instance)
        {
            // Spawn dummys
            if (Input.GetKeyDown(KeyCode.F1))
            {
                var playerControl = UnityEngine.Object.Instantiate(AmongUsClient.Instance.PlayerPrefab);

                var i = playerControl.PlayerId = (byte)GameData.Instance.GetAvailableId();

                GameData.Instance.AddPlayer(playerControl);

                //playerControl.transform.position = PlayerControl.LocalPlayer.transform.position;
                playerControl.GetComponent<DummyBehaviour>().enabled = true;
                playerControl.isDummy = true;
                playerControl.SetName(Patches.RandomNamePatch.GetRandomName());
                playerControl.SetColor(NebulaPlugin.rnd.Next(15));

                AmongUsClient.Instance.Spawn(playerControl, -2, InnerNet.SpawnFlags.None);

                GameData.Instance.RpcSetTasks(playerControl.PlayerId, new byte[0]);
            }

            // Suiside
            if (Input.GetKeyDown(KeyCode.F9))
            {
                Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, PlayerControl.LocalPlayer, Game.PlayerData.PlayerStatus.Suicide, false, false);
            }

            // Kill nearest player
            if (Input.GetKeyDown(KeyCode.F10))
            {
                PlayerControl target = Patches.PlayerControlPatch.SetMyTarget();
                if (target == null) return;

                Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, target, Game.PlayerData.PlayerStatus.Dead, false, false);
            }
        }

        if (CommandInputField == null && Input.GetKeyDown(KeyCode.F2))
        {
            var obj = new GameObject("MetaCmd");
            CommandInputField = obj;

            if (HudManager.InstanceExists) obj.transform.SetParent(HudManager.Instance.transform);
            obj.transform.localPosition = new Vector3(0f, -1.8f, -300f);
            obj.transform.localScale = new Vector3(1f, 1f, 1f);
            var component = obj.AddComponent<Components.TextInputField>();
            component.SetTextProperty(new Vector2(7, 0.5f), 1.8f, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Normal);
            component.GetFocus();

            IEnumerator DestroyField(string errorTxt)
            {
                component.SetText("");
                component.HintText = errorTxt;

                float t = 2f;
                while (t > 0f)
                {
                    t -= Time.deltaTime;
                    yield return null;
                }
                GameObject.Destroy(obj);
                CommandInputField = null;
            }

            void SetField(int index, bool val)
            {
                if (val)
                    NebulaOption.configGameControlArgument.Value |= 1 << index;
                else
                    NebulaOption.configGameControlArgument.Value &= ~(1 << index);
            }

            component.DecisionAction = (cmd) =>
            {
                if (cmd.Length == 0)
                {
                    GameObject.Destroy(obj);
                    CommandInputField = null;
                }
                var strings = cmd.Split(" ");

                switch (strings[0])
                {
                    case "metarule":
                        if (strings.Length == 1)
                        {
                            component.StartCoroutine(DestroyField("metarule require 2 or 3 args.").WrapToIl2Cpp());
                            break;
                        }
                        bool? val = null;
                        if (strings.Length >= 3) val = (strings[2] == "true");
                        int index = -1;
                        if (strings[1] == "skipPerkAligning")
                            index = 0;
                        if (strings[1] == "withoutMoreCosmic")
                            index = 1;
                        if (strings[1] == "withoutGlobalMoreCosmic")
                            index = 2;
                        if (strings[1] == "withoutLocalMoreCosmic")
                            index = 3;

                        if (index == -1) component.StartCoroutine(DestroyField(strings[1] + " is not existed.").WrapToIl2Cpp());

                        if (val.HasValue) SetField(index, val.Value);
                        else val = NebulaOption.GetGameControlArgument(index);

                        component.StartCoroutine(DestroyField(strings[1] + ": " + val.Value).WrapToIl2Cpp());

                        break;
                }
            };
        }
    }

}