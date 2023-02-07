namespace Nebula.Patches;

[HarmonyPatch]
public class RitualPatch
{
    static public void SetTasks(GameData gameData, GameData.PlayerInfo playerById, UnhollowerBaseLib.Il2CppStructArray<byte> taskTypeIds)
    {
        var ritualTasks = new Il2CppSystem.Collections.Generic.List<GameData.TaskInfo>(3);

        int missions = (int)CustomOptionHolder.NumOfMissionsOption.getFloat();
        GameData.TaskInfo t;
        for (int i = 0; i < missions; i++)
        {
            t = new GameData.TaskInfo(Byte.MaxValue, 0);
            t.Id = (uint)i;
            ritualTasks.Add(t);
        }

        //脱出テキストタスク
        t = new GameData.TaskInfo(Byte.MaxValue - 1, 0);
        t.Id = (uint)missions;
        ritualTasks.Add(t);

        playerById.Tasks = ritualTasks;
        playerById.Object.SetTasks(playerById.Tasks);

        gameData.SetDirtyBit(1U << (int)playerById.PlayerId);
    }

    static public void OnSpawn(PlayerControl player)
    {
        player.NetTransform.SnapTo(Game.GameData.data.RitualData.PlayerData[player.PlayerId].SpawnPos);
    }


    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.GetTaskById))]
    class GetTaskByIdPatch
    {
        public static bool Prefix(ShipStatus __instance, ref NormalPlayerTask __result, [HarmonyArgument(0)] byte idx)
        {
            if (idx == Byte.MaxValue)
            {
                var obj = new GameObject();
                obj.hideFlags |= HideFlags.HideInHierarchy;
                __result = obj.AddComponent<Tasks.RitualWiringTask>();
                return false;
            }
            else if (idx == Byte.MaxValue - 1)
            {
                var obj = new GameObject();
                obj.hideFlags |= HideFlags.HideInHierarchy;
                __result = obj.AddComponent<Tasks.RitualEscapeTextTask>();
                return false;
            }
            else if (idx == Byte.MaxValue - 2)
            {
                var obj = new GameObject();
                obj.hideFlags |= HideFlags.HideInHierarchy;
                __result = obj.AddComponent<Tasks.OpportunistTask>();
                return false;
            }
            else if (idx == Byte.MaxValue - 3)
            {
                var obj = new GameObject();
                obj.hideFlags |= HideFlags.HideInHierarchy;
                __result = obj.AddComponent<Tasks.SpectreFriedTask>();
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.SetHudActive))]
    class HideReportButtonPatch
    {
        public static void Postfix(HudManager __instance, [HarmonyArgument(0)] bool isActive)
        {
            if (!isActive) return;

            if (Game.GameData.data.GameMode == Module.CustomGameMode.Ritual)
            {
                __instance.ReportButton.ToggleVisible(false);
            }
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.CoShowIntro))]
    class HideReportButtonAtIntroPatch
    {
        private static System.Collections.IEnumerator GetEnumerator(HudManager __instance)
        {
            while (!ShipStatus.Instance)
            {
                yield return null;
            }
            __instance.IsIntroDisplayed = true;
            DestroyableSingleton<HudManager>.Instance.FullScreen.transform.localPosition = new Vector3(0f, 0f, -250f);
            yield return DestroyableSingleton<HudManager>.Instance.ShowEmblem(true);
            IntroCutscene introCutscene = UnityEngine.Object.Instantiate<IntroCutscene>(__instance.IntroPrefab, __instance.transform);
            yield return introCutscene.CoBegin();
            yield return ShipStatus.Instance.PrespawnStep();
            PlayerControl.LocalPlayer.SetKillTimer(CustomOptionHolder.InitialKillCoolDownOption.getFloat());
            (ShipStatus.Instance.Systems[SystemTypes.Sabotage].Cast<SabotageSystemType>()).ForceSabTime(10f);
            PlayerControl.LocalPlayer.AdjustLighting();
            yield return __instance.CoFadeFullScreen(Color.black, Color.clear, 0.2f, false);
            DestroyableSingleton<HudManager>.Instance.FullScreen.transform.localPosition = new Vector3(0f, 0f, -500f);
            __instance.IsIntroDisplayed = false;
            __instance.CrewmatesKilled.gameObject.SetActive(GameManager.Instance.ShowCrewmatesKilled());
            GameManager.Instance.StartGame();
            yield break;
        }

        public static void Postfix(HudManager __instance, ref Il2CppSystem.Collections.IEnumerator __result)
        {
            HideReportButtonPatch.Postfix(__instance, true);
            __result = GetEnumerator(__instance).WrapToIl2Cpp();
        }
    }
}