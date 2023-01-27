using Hazel;

namespace Nebula.Patches;

public static class BeginHubHelper
{
    private static void SetCamera(HudManager __instance, GameObject button)
    {
        var aPos = button.GetComponent<AspectPosition>();
        if (!aPos) return;
        aPos.parentCam = __instance.UICamera;
    }

    public static void Postfix(HudManager __instance)
    {
        var transform = __instance.transform.Find("Buttons");
        Transform child;
        if (transform)
        {
            child = transform.Find("TopRight");
            if (child)
                foreach (var aPos in child.GetComponentsInChildren<AspectPosition>())
                    aPos.parentCam = __instance.UICamera;

            child = transform.Find("BottomRight");
            if (child)
                foreach (var aPos in child.GetComponentsInChildren<AspectPosition>())
                    aPos.parentCam = __instance.UICamera;

            SetCamera(__instance, __instance.KillButton.gameObject);
            SetCamera(__instance, __instance.SabotageButton.gameObject);
            SetCamera(__instance, __instance.AbilityButton.gameObject);
            SetCamera(__instance, __instance.ImpostorVentButton.gameObject);
            SetCamera(__instance, __instance.ReportButton.gameObject);
            SetCamera(__instance, __instance.MapButton.gameObject);
        }
    }
}

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
class BeginHudPatch
{
    public static void Postfix(HudManager __instance)
    {
        try
        {
            BeginHubHelper.Postfix(__instance);
        }
        catch { }
    }
}

[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.OnDestroy))]
class BeginHudFinallyPatch
{
    public static void Postfix(IntroCutscene __instance)
    {
        BeginHubHelper.Postfix(HudManager.Instance);
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.StartMeeting))]
class StartMeetingPatch
{
    public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] GameData.PlayerInfo meetingTarget)
    {
        EyesightPatch.ObserverTarget = 0;
        HudManager.Instance.PlayerCam.SetTargetWithLight(PlayerControl.LocalPlayer);
        Objects.PlayerList.Instance.Close();
        EyesightPatch.SuspendRemoteControl(EyesightPatch.ObserverTarget);
    }
}

[HarmonyPriority(100)]
[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
class EyesightPatch
{
    static private float Distance = 1f;
    static private float ObserverModeRate = 1f;
    static public bool ObserverMode = false;
    static public int ObserverTarget = 0;

    public static void SuspendRemoteControl(int lastTarget)
    {
        //遠隔操作を終了する
        if (lastTarget != ObserverTarget && PlayerControl.LocalPlayer.PlayerId != lastTarget && (Game.GameData.data?.myData.CanControlOtherPlayers ?? false))
        {
            var p = PlayerControl.AllPlayerControls[lastTarget];
            p.MyPhysics.SetNormalizedVelocity(Vector2.zero);
            p.Collider.enabled = true;
        }
    }


    public static void ChangeObserverTarget(bool increamentFlag)
    {
        int lastTarget = ObserverTarget;
        while (true)
        {
            if (increamentFlag) ObserverTarget++; else ObserverTarget--;

            if (ObserverTarget < 0) ObserverTarget = PlayerControl.AllPlayerControls.Count - 1;
            else if (ObserverTarget >= PlayerControl.AllPlayerControls.Count) ObserverTarget = 0;

            //一周分調べたら終了
            if (lastTarget == ObserverTarget) break;

            var p = PlayerControl.AllPlayerControls[ObserverTarget];
            if (!p) continue;
            if (p.Data.IsDead) continue;

            HudManager.Instance.PlayerCam.SetTargetWithLight(p);

            break;
        }

        Objects.PlayerList.Instance.SelectPlayer(PlayerControl.AllPlayerControls[ObserverTarget].PlayerId);

        SuspendRemoteControl(lastTarget);
    }

    public static void Postfix(HudManager __instance)
    {
        try
        {
            if (Game.GameData.data != null && Game.GameData.data.myData.CanControlOtherPlayers && Objects.PlayerList.Instance!=null && Objects.PlayerList.Instance.IsOpen && ObserverTarget!=0)
            {
                var p = PlayerControl.AllPlayerControls[ObserverTarget];
                p.MyPhysics.SetNormalizedVelocity(__instance.joystick.DeltaL);
                p.Collider.enabled = !Input.GetKey(Module.NebulaInputManager.metaControlInput.keyCode);
            }
        }
        catch { }

        if ((Game.GameData.data != null && (Game.GameData.data.myData.CanSeeEveryoneInfo)) || AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Joined)
        {
            if (Input.GetKeyDown(Module.NebulaInputManager.observerInput.keyCode)) ObserverMode = !ObserverMode;

            if (Input.GetKeyDown(Module.NebulaInputManager.observerShortcutInput.keyCode))
            {
                if (Game.GameData.data != null && Game.GameData.data.myData.CanSeeEveryoneInfo && Objects.PlayerList.Instance != null)
                {
                    if (!Objects.PlayerList.Instance.IsOpen)
                    {
                        Objects.PlayerList.Instance.Show();
                    }

                    if (ObserverMode)
                        __instance.PlayerCam.SetTargetWithLight(PlayerControl.LocalPlayer);

                    Objects.PlayerList.Instance.SelectPlayer(PlayerControl.LocalPlayer.PlayerId);
                }

                ObserverMode = !ObserverMode;

            }
        }
        if ((Game.GameData.data != null && (Game.GameData.data.myData.CanControlOtherPlayers || Game.GameData.data.myData.CanSeeEveryoneInfo)) || AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Joined)
        {
            if (Game.GameData.data != null && (Game.GameData.data.myData.CanSeeEveryoneInfo || Game.GameData.data.myData.CanControlOtherPlayers))
            {
                if (Input.GetKeyDown(Module.NebulaInputManager.changeEyesightLeftInput.keyCode))
                {
                    ChangeObserverTarget(false);
                    Objects.PlayerList.Instance?.Show();
                }
                if (Input.GetKeyDown(Module.NebulaInputManager.changeEyesightRightInput.keyCode))
                {
                    ChangeObserverTarget(true);
                    Objects.PlayerList.Instance?.Show();
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape) && Module.MetaDialog.dialogOrder.Count == 0)
            {
                __instance.PlayerCam.SetTargetWithLight(PlayerControl.LocalPlayer);
                Objects.PlayerList.Instance?.Close();
                SuspendRemoteControl(ObserverTarget);
                ObserverMode = false;
            }
            if (__instance.PlayerCam.Target != PlayerControl.LocalPlayer && __instance.PlayerCam.Target is PlayerControl && ((PlayerControl)__instance.PlayerCam.Target).Data.IsDead)
            {
                ChangeObserverTarget(true);
            }

            if (AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started)
            {
                try
                {
                    Objects.PlayerList.Instance?.ListUpPlayers((b) => !Helpers.playerById(b)?.Data.IsDead ?? false);
                }
                catch { }
            }
        }

        if ((AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started && AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Joined)
                || MeetingHud.Instance
                || ExileController.Instance
                || Minigame.Instance
                || (MapBehaviour.Instance && MapBehaviour.Instance.IsOpen)
                || (AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started && Game.GameData.data != null && !Game.GameData.data.myData.CanSeeEveryoneInfo)
                || Module.MetaDialog.dialogOrder.Count > 0)
        {
            ObserverMode = false;
        }


        if (ObserverMode)
        {
            float axis = Input.GetAxis("Mouse ScrollWheel");
            if (axis < 0f) Distance -= 0.25f;
            if (axis > 0f) Distance += 0.25f;
            Distance = Mathf.Clamp(Distance, 0.25f, 3.5f);
        }
        else
        {
            Distance = 1f;
        }

        float dis = ((ObserverMode ? 2.0f : 1.0f) - ObserverModeRate);
        if (Mathf.Abs(dis) > 0.01f)
            ObserverModeRate += ((ObserverMode ? 2.0f : 1.0f) - ObserverModeRate) * 0.3f;
        else
            ObserverModeRate = ObserverMode ? 2.0f : 1.0f;

        float r = 1f;
        if (Game.GameData.data != null && Game.GameData.data.myData != null) r = Game.GameData.data.myData.Vision.GetVisionRate();

        Camera.main.orthographicSize = 3.0f * Distance * ObserverModeRate * r;
        __instance.UICamera.orthographicSize = 3.0f;
        __instance.transform.localScale = Vector3.one;


        if (HudManager.InstanceExists)
        {
            Transform transform;

            transform = __instance.transform.Find("TaskDisplay");
            if (transform) transform.gameObject.SetActive(!ObserverMode && ShipStatus.Instance);
        }
    }
}