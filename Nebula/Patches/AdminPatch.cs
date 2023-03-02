using Nebula.Map;
using Sentry;

namespace Nebula.Patches;

[Harmony]
public class AdminPatch
{
    static float adminTimer = 0f;
    static public TMPro.TextMeshPro OutOfTime;
    static public TMPro.TextMeshPro TimeRemaining;
    static bool clearedIcons = false;

    public enum AdminMode
    {
        Default,
        ImpostorsAndDeadBodies,
        PlayerColors
    }

    //時間制限が適用されるアドミンであるかどうか
    public static bool isStandardAdmin = false;
    //コミュ制限が適用されるアドミンであるかどうか
    public static bool isAffectedByCommAdmin = false;
    //アイコンの色表示設定
    public static AdminMode adminMode = AdminMode.Default;
    //背景色を変更するべきかどうか
    public static bool shouldChangeColor = true;

    public static Int32 divMask = Int32.MaxValue;

    public static void ResetData()
    {
        adminTimer = 0f;
        if (TimeRemaining != null)
        {
            UnityEngine.Object.Destroy(TimeRemaining);
            TimeRemaining = null;
        }

        if (OutOfTime != null)
        {
            UnityEngine.Object.Destroy(OutOfTime);
            OutOfTime = null;
        }
    }

    static void UseAdminTime()
    {
        if (CustomOptionHolder.DevicesOption.getBool() && CustomOptionHolder.AdminLimitOption.getBool() && !PlayerControl.LocalPlayer.Data.IsDead)
        {
            RPCEventInvoker.UpdateAdminRestrictTimer(adminTimer);
        }
        adminTimer = 0f;
    }

    [HarmonyPatch(typeof(MapConsole), nameof(MapConsole.CanUse))]
    public static class MapConsoleCanUsePatch
    {
        public static bool Prefix(ref float __result, MapConsole __instance, [HarmonyArgument(0)] GameData.PlayerInfo pc, [HarmonyArgument(1)] out bool canUse, [HarmonyArgument(2)] out bool couldUse)
        {
            // temp fix for the admin bug on airship
            if (GameOptionsManager.Instance.CurrentGameOptions.MapId == 4)
                __instance.useIcon = ImageNames.PolusAdminButton;

            canUse = couldUse = false;
            return true;
        }
    }

    [HarmonyPatch(typeof(MapConsole), nameof(MapConsole.Use))]
    public static class MapConsoleUsePatch
    {
        public static void Prefix(MapConsole __instance)
        {
            var mapData = MapData.GetCurrentMapData();
            divMask = Int32.MaxValue & ~1;

            if (CustomOptionHolder.DevicesOption.getBool() && CustomOptionHolder.LimitedAdmin.getBool())
            {
                int adminId;
                if (!mapData.AdminNameMap.TryGetValue(__instance.name, out adminId)) adminId = 0;

                if (mapData.LimitedAdmin.TryGetValue(adminId, out var option))
                    divMask &= option.getSelection();
            }
        }

        public static void Postfix(MapConsole __instance)
        {
            isStandardAdmin = true;
            isAffectedByCommAdmin = true;
            adminMode = AdminMode.Default;
            shouldChangeColor = true;
        }
    }

    static Dictionary<CounterArea, int> impostorsMap = new Dictionary<CounterArea, int>();
    static Dictionary<CounterArea, int> deadBodiesMap = new Dictionary<CounterArea, int>();

    public static Int32 divMaskByConsole = Int32.MaxValue;
    public static Int32 divMaskFinally = Int32.MaxValue;

    [HarmonyPatch(typeof(MapCountOverlay), nameof(MapCountOverlay.OnEnable))]
    public static class MapCountOverlayOnEnablePatch
    {
        static bool Prefix(MapCountOverlay __instance)
        {
            adminTimer = 0f;
            impostorsMap.Clear();
            deadBodiesMap.Clear();

            divMaskByConsole = divMask;
            divMask = Int32.MaxValue;

            if (CustomOptionHolder.mapOptions.getBool() && CustomOptionHolder.useClassicAdmin.getBool())
                divMaskByConsole &= ~Map.MapData.GetCurrentMapData().ClassicAdminMask;
            

            divMaskFinally = divMaskByConsole;
            MapBehaviourExpansion.EnmaskMap(divMaskFinally);

            if (Roles.RoleSystem.ImpAdminSystem.IsJailerCountOverlay(__instance))
            {
                __instance.timer = 1f;
                return false;
            }

            return true;
        }
    }


    [HarmonyPatch(typeof(MapCountOverlay), nameof(MapCountOverlay.OnDisable))]
    public static class MapCountOverlayOnDisablePatch
    {
        static void Prefix(MapCountOverlay __instance)
        {
            UseAdminTime();
        }
    }

    [HarmonyPatch(typeof(MapCountOverlay), nameof(MapCountOverlay.Update))]
    public static class MapCountOverlayUpdatePatch
    {
        static ContactFilter2D filter = new ContactFilter2D()
        {
            useTriggers = true,
            layerMask = LayerMask.GetMask(new string[] { "Players" }),
            useLayerMask = true
        };

        static void updateImpostors(CounterArea counterArea, int impostors, int deadBodies)
        {
            foreach (var icon in counterArea.myIcons.GetFastEnumerator())
            {
                if (impostors > 0)
                {
                    PlayerMaterial.SetColors(Palette.ImpostorRed, icon.GetComponent<SpriteRenderer>());
                    impostors--;
                }
                else if (deadBodies > 0)
                {
                    PlayerMaterial.SetColors(Palette.DisabledGrey, icon.GetComponent<SpriteRenderer>());
                    deadBodies--;
                }
                else
                {
                    PlayerMaterial.SetColors(new Color(224f / 255f, 255f / 255f, 0f / 255f), icon.GetComponent<SpriteRenderer>());
                }
            }
        }

        static void update(MapCountOverlay __instance)
        {
            __instance.timer += Time.deltaTime;
            if (__instance.timer < 0.1f) return;


            __instance.timer = 0f;
            if (isAffectedByCommAdmin && !__instance.isSab && PlayerTask.PlayerHasTaskOfType<IHudOverrideTask>(PlayerControl.LocalPlayer))
            {
                __instance.isSab = true;
                if (shouldChangeColor) __instance.BackgroundColor.SetColor(Palette.DisabledGrey);
                __instance.SabotageText.gameObject.SetActive(shouldChangeColor);
            }
            else if (!isAffectedByCommAdmin || (__instance.isSab && !PlayerTask.PlayerHasTaskOfType<IHudOverrideTask>(PlayerControl.LocalPlayer)))
            {
                __instance.isSab = false;
                if (shouldChangeColor) __instance.BackgroundColor.SetColor(Color.green);
                __instance.SabotageText.gameObject.SetActive(false);
            }

            //重複防止
            HashSet<byte> detectedPlayers = new HashSet<byte>();
            Map.MapData? currentMapData = (divMaskFinally != Int32.MaxValue) ? Map.MapData.GetCurrentMapData() : null;

            for (int i = 0; i < __instance.CountAreas.Length; i++)
            {
                CounterArea counterArea = __instance.CountAreas[i];
                int impostors = 0;
                int deadBodies = 0;

                if (__instance.isSab)
                {
                    counterArea.UpdateCount(0);
                    continue;
                }

                PlainShipRoom plainShipRoom;
                try
                {
                    plainShipRoom = ShipStatus.Instance.FastRooms[counterArea.RoomType];
                }
                catch
                {
                    counterArea.UpdateCount(0);
                    continue;
                }

                if (plainShipRoom != null && plainShipRoom.roomArea)
                {
                    if (!MeetingHud.Instance)
                    {

                        if (divMaskFinally != Int32.MaxValue)
                            if (currentMapData!.AdminSystemTypeMap.TryGetValue(plainShipRoom.RoomId, out int index) && (divMaskFinally & (1 << index)) == 0) continue;


                        //通常時のアドミン

                        int num = plainShipRoom.roomArea.OverlapCollider(filter, __instance.buffer);
                        int num2 = num;
                        for (int j = 0; j < num; j++)
                        {
                            Collider2D collider2D = __instance.buffer[j];
                            if (!(collider2D.tag == "DeadBody"))
                            {
                                PlayerControl component = collider2D.GetComponent<PlayerControl>();
                                if (!component || component.Data == null || component.Data.Disconnected || component.Data.IsDead || detectedPlayers.Contains(component.PlayerId))
                                {
                                    num2--;
                                }
                                else
                                {
                                    if (adminMode == AdminMode.ImpostorsAndDeadBodies && (component.Data.Role.IsImpostor || component.GetModData().role.DeceiveImpostorInNameDisplay))
                                        impostors++;
                                    detectedPlayers.Add(component.PlayerId);
                                }
                            }
                            else
                            {
                                DeadBody component = collider2D.GetComponent<DeadBody>();
                                if (detectedPlayers.Contains(component.ParentId))
                                {
                                    num2--;
                                }
                                else
                                {
                                    if (adminMode == AdminMode.ImpostorsAndDeadBodies)
                                        deadBodies++;
                                    else if (!__instance.includeDeadBodies)
                                        num2--;

                                    detectedPlayers.Add(component.ParentId);
                                }
                            }
                        }

                        counterArea.UpdateCount(num2);
                    }
                    else
                    {
                        //会議中のアドミン

                        int num = 0;
                        foreach (var data in Game.GameData.data.AllPlayers)
                        {
                            if (!data.Value.IsAlive) continue;
                            if (data.Value.preMeetingPosition == null || detectedPlayers.Contains(data.Value.id)) continue;

                            if (!plainShipRoom.roomArea.OverlapPoint(data.Value.preMeetingPosition.Value)) continue;

                            num++;
                            if (data.Value.role.category == Roles.RoleCategory.Impostor || data.Value.role.DeceiveImpostorInNameDisplay) impostors++;
                        }

                        counterArea.UpdateCount(num);
                        counterArea.UpdateCount(num);
                    }

                    int lastImpostors = 0;
                    int lastDeadBodies = 0;
                    if (adminMode != AdminMode.PlayerColors && (!impostorsMap.TryGetValue(counterArea, out lastImpostors) || lastImpostors != impostors || !deadBodiesMap.TryGetValue(counterArea, out lastDeadBodies) || lastDeadBodies != deadBodies))
                    {
                        impostorsMap[counterArea] = impostors;
                        deadBodiesMap[counterArea] = deadBodies;
                        //インポスター人数変更
                        updateImpostors(counterArea, impostors, deadBodies);
                    }
                }
            }
        }

        static bool Prefix(MapCountOverlay __instance)
        {
            if (CustomOptionHolder.DevicesOption.getBool() && CustomOptionHolder.AdminLimitOption.getBool())
            {
                if (isStandardAdmin)
                {
                    adminTimer += Time.deltaTime;
                    if (adminTimer > 0.1f)
                        UseAdminTime();



                    if (OutOfTime == null)
                    {
                        OutOfTime = UnityEngine.Object.Instantiate(__instance.SabotageText, __instance.SabotageText.transform.parent);
                        OutOfTime.text = Language.Language.GetString("game.device.restrictOutOfTime");
                    }

                    if (TimeRemaining == null)
                    {
                        TimeRemaining = UnityEngine.Object.Instantiate(HudManager.Instance.TaskPanel.taskText, __instance.transform);
                        TimeRemaining.alignment = TMPro.TextAlignmentOptions.BottomRight;
                        TimeRemaining.transform.position = Vector3.zero;
                        TimeRemaining.transform.localPosition = new Vector3(3.25f, 5.25f);
                        TimeRemaining.transform.localScale *= 2f;
                        TimeRemaining.color = Palette.White;
                    }

                    if (Game.GameData.data.UtilityTimer.AdminTimer <= 0f)
                    {
                        __instance.BackgroundColor.SetColor(Palette.DisabledGrey);
                        OutOfTime.gameObject.SetActive(true);
                        TimeRemaining.gameObject.SetActive(false);
                        if (clearedIcons == false)
                        {
                            foreach (CounterArea ca in __instance.CountAreas) ca.UpdateCount(0);
                            clearedIcons = true;
                        }
                        return false;
                    }

                    clearedIcons = false;
                    OutOfTime.gameObject.SetActive(false);
                    string timeString = TimeSpan.FromSeconds(Game.GameData.data.UtilityTimer.AdminTimer).ToString(@"mm\:ss\.f");
                    TimeRemaining.text = Language.Language.GetString("game.device.timeRemaining").Replace("%TIMER%", timeString);
                    TimeRemaining.gameObject.SetActive(true);
                }
                else
                {
                    if (TimeRemaining != null)
                        TimeRemaining.gameObject.SetActive(false);
                    if (OutOfTime != null)
                        OutOfTime.gameObject.SetActive(false);
                }
            }

            update(__instance);
            return false;
        }
    }
}