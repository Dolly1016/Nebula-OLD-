using System.Reflection;
using UnhollowerBaseLib;
using Hazel;
using System.Text;
using Nebula.Patches;

namespace Nebula;

[HarmonyPatch]
public static class Helpers
{
    public static bool ShowButtons
    {
        get
        {
            return (!(MapBehaviour.Instance && MapBehaviour.Instance.IsOpen) || Patches.MapBehaviorPatch.minimapFlag) &&
                  !MeetingHud.Instance &&
                  !ExileController.Instance;
        }
    }

    public static TMPro.TextMeshPro CreateButtonUpperText(this ActionButton button)
    {
        TMPro.TextMeshPro text = GameObject.Instantiate(HudManager.Instance.KillButton.cooldownTimerText, button.transform);
        text.enableWordWrapping = false;
        text.transform.localScale = Vector3.one * 0.5f;
        text.transform.localPosition += new Vector3(-0.05f, 0.7f, 0);
        text.gameObject.SetActive(true);
        return text;
    }

    public static bool ProceedTimer(bool isImpostorKillButton)
    {
        if (isImpostorKillButton) return PlayerControl.LocalPlayer.IsKillTimerEnabled;
        
        if (PlayerControl.LocalPlayer.inVent) return false;
        if (MeetingHud.Instance) return false;

        //情報端末以外ではカウントが進む
        if (MapBehaviour.Instance && MapBehaviour.Instance.IsOpen)
            return !MapBehaviour.Instance.countOverlay.isActiveAndEnabled;

        
        if (Minigame.Instance)
        {
            if (Minigame.Instance.TryCast<SpawnInMinigame>()) return false;
            if (Minigame.Instance.MyNormTask) return true;
            if (Minigame.Instance.TryCast<DoorCardSwipeGame>()) return true;
            if (Minigame.Instance.TryCast<DoorBreakerGame>()) return true;
            if (Minigame.Instance.TryCast<MultistageMinigame>()) return true;
            if (Minigame.Instance.TryCast<AutoMultistageMinigame>()) return true;
        }

        return PlayerControl.LocalPlayer.CanMove;
    }

    public static Sprite loadSpriteFromResources(Texture2D texture, float pixelsPerUnit, Rect textureRect)
    {
        return Sprite.Create(texture, textureRect, new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }

    public static Sprite loadSpriteFromResources(Texture2D texture, float pixelsPerUnit, Rect textureRect, Vector2 pivot)
    {
        return Sprite.Create(texture, textureRect, pivot, pixelsPerUnit);
    }

    public static Sprite loadSpriteFromResources(string path, float pixelsPerUnit)
    {
        try
        {
            Texture2D texture = loadTextureFromResources(path);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }
        catch
        {
            System.Console.WriteLine("Error loading sprite from path: " + path);
        }
        return null;
    }

    public static Sprite? getSpriteFromAssets(string name)
    {
        foreach (var sprite in UnityEngine.Object.FindObjectsOfTypeIncludingAssets(Sprite.Il2CppType))
        {
            if (sprite.name != name) continue;

            return sprite.Cast<Sprite>();
        }
        return null;
    }
    public static Texture2D loadTextureFromResources(string path)
    {
        try
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, true);
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream stream = assembly.GetManifestResourceStream(path);
            var byteTexture = new byte[stream.Length];
            var read = stream.Read(byteTexture, 0, (int)stream.Length);
            LoadImage(texture, byteTexture, false);
            return texture;
        }
        catch
        {
            System.Console.WriteLine("Error loading texture from resources: " + path);
        }
        return null;
    }

    public static string loadTextFromResources(string path)
    {
        try
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream stream = assembly.GetManifestResourceStream(path);
            var byteArray = new byte[stream.Length];
            var read = stream.Read(byteArray, 0, (int)stream.Length);
            return Encoding.Unicode.GetString(byteArray);
        }
        catch
        {

        }
        return null;
    }

    public static Texture2D loadTextureFromDisk(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, true);
                byte[] byteTexture = File.ReadAllBytes(path);
                LoadImage(texture, byteTexture, false);
                return texture;
            }
        }
        catch
        {
            System.Console.WriteLine("Error loading texture from disk: " + path);
        }
        return null;
    }

    internal delegate bool d_LoadImage(IntPtr tex, IntPtr data, bool markNonReadable);
    internal static d_LoadImage iCall_LoadImage;
    private static bool LoadImage(Texture2D tex, byte[] data, bool markNonReadable)
    {
        if (iCall_LoadImage == null)
            iCall_LoadImage = IL2CPP.ResolveICall<d_LoadImage>("UnityEngine.ImageConversion::LoadImage");
        var il2cppArray = (Il2CppStructArray<byte>)data;
        return iCall_LoadImage.Invoke(tex.Pointer, il2cppArray.Pointer, markNonReadable);
    }

    public static PlayerControl playerById(byte id)
    {
        foreach (PlayerControl player in PlayerControl.AllPlayerControls.GetFastEnumerator())
            if (player.PlayerId == id)
                return player;
        return null;
    }

    public static Dictionary<byte, PlayerControl> allPlayersById()
    {
        Dictionary<byte, PlayerControl> res = new Dictionary<byte, PlayerControl>();
        foreach (PlayerControl player in PlayerControl.AllPlayerControls.GetFastEnumerator())
            res.Add(player.PlayerId, player);
        return res;
    }

    public static bool isCustomServer()
    {
        if (!DestroyableSingleton<ServerManager>.InstanceExists) return false;
        StringNames n = DestroyableSingleton<ServerManager>.Instance.CurrentRegion.TranslateName;
        return n != StringNames.ServerNA && n != StringNames.ServerEU && n != StringNames.ServerAS;
    }

    public static void clearAllTasks(this PlayerControl player)
    {
        if (player == null) return;
        for (int i = 0; i < player.myTasks.Count; i++)
        {
            PlayerTask playerTask = player.myTasks[i];
            playerTask.OnRemove();
            UnityEngine.Object.Destroy(playerTask.gameObject);
        }
        player.myTasks.Clear();

        if (player.Data != null && player.Data.Tasks != null)
            player.Data.Tasks.Clear();

    }

    public static void setSemiTransparent(this PoolablePlayer player, bool value)
    {
        float alpha = value ? 0.25f : 1f;
        foreach (SpriteRenderer r in player.gameObject.GetComponentsInChildren<SpriteRenderer>())
            r.color = new Color(r.color.r, r.color.g, r.color.b, alpha);
        player.cosmetics.nameText.color = new Color(player.cosmetics.nameText.color.r, player.cosmetics.nameText.color.g, player.cosmetics.nameText.color.b, alpha);
    }

    public static string GetString(this TranslationController t, StringNames key, params Il2CppSystem.Object[] parts)
    {
        return t.GetString(key, parts);
    }

    public static string csTop(Color c)
    {
        return string.Format("<color=#{0:X2}{1:X2}{2:X2}{3:X2}>", ToByte(c.r), ToByte(c.g), ToByte(c.b), ToByte(c.a));
    }

    public static string cs(Color c, string s)
    {
        return string.Format("<color=#{0:X2}{1:X2}{2:X2}{3:X2}>{4}</color>", ToByte(c.r), ToByte(c.g), ToByte(c.b), ToByte(c.a), s);
    }

    private static byte ToByte(float f)
    {
        f = Mathf.Clamp01(f);
        return (byte)(f * 255);
    }

    public static KeyValuePair<byte, int> MaxPair(this Dictionary<byte, int> self, out bool tie)
    {
        tie = true;
        KeyValuePair<byte, int> result = new KeyValuePair<byte, int>(byte.MaxValue, int.MinValue);
        foreach (KeyValuePair<byte, int> keyValuePair in self)
        {
            if (keyValuePair.Value > result.Value)
            {
                result = keyValuePair;
                tie = false;
            }
            else if (keyValuePair.Value == result.Value)
            {
                tie = true;
            }
        }
        return result;
    }

    public static void SetLook(this PlayerControl target, string? playerName, int colorId, string hatId, string visorId, string skinId, string petId)
    {
        target.MyPhysics.ResetAnimState();
        target.RawSetVisor(visorId, colorId);
        target.RawSetHat(hatId, colorId);
        target.RawSetSkin(skinId, colorId);
        target.RawSetColor(colorId);

        var p = Game.GameData.data?.GetPlayerData(target.PlayerId) ?? null;
        if (p != null && playerName != null) p.currentName = playerName;

        //死体のペットは変更しない(生き返ってしまうため)
        if (target.Data.IsDead) return;
        if (target.cosmetics.currentPet) UnityEngine.Object.Destroy(target.cosmetics.currentPet.gameObject);

        try
        {
            target.cosmetics.currentPet = UnityEngine.Object.Instantiate<PetBehaviour>(FastDestroyableSingleton<HatManager>.Instance.GetPetById(petId).viewData.viewData);
            target.cosmetics.currentPet.transform.position = target.transform.position;
            target.cosmetics.currentPet.Source = target;
            target.cosmetics.currentPet.Visible = target.Visible;
            target.SetPlayerMaterialColors(target.cosmetics.currentPet.rend);
        }
        catch
        {
            //ペットが存在しない場合は例外が発生する
        }
    }

    public static void SetLook(this PlayerControl target, Game.PlayerData.PlayerOutfitData outfit)
    {
        if (outfit == null)
        {
            return;
        }

        target.SetLook(outfit.Name, outfit.ColorId, outfit.HatId, outfit.VisorId, outfit.SkinId, outfit.PetId);
    }

    public static Game.PlayerData.PlayerOutfitData? AddOutfit(this PlayerControl target, string? playerName, int colorId, string hatId, string visorId, string skinId, string petId, int priority)
    {
        var outfit = new Game.PlayerData.PlayerOutfitData(priority, playerName, colorId, hatId, visorId, skinId, petId);
        target.GetModData().AddOutfit(outfit);
        return outfit;
    }

    public static Game.PlayerData.PlayerOutfitData? AddOutfit(this PlayerControl target, PlayerControl reference, int priority)
    {
        var rp = Game.GameData.data.GetPlayerData(reference.PlayerId);
        if (rp == null) return null;

        string name = rp.name;
        Game.PlayerData.PlayerOutfitData outfit = new Game.PlayerData.PlayerOutfitData(priority, rp.name, rp.Outfit);
        if (outfit == null)
        {
            return null;
        }
        target.GetModData().AddOutfit(outfit);
        return outfit;
    }

    public static void AddOutfit(this PlayerControl target, Game.PlayerData.PlayerOutfitData outfit)
    {
        target.GetModData().AddOutfit(outfit);
    }

    public static bool RemoveOutfit(this PlayerControl target, Game.PlayerData.PlayerOutfitData outfit)
    {
        var data = Game.GameData.data.GetPlayerData(target.PlayerId);
        if (data == null) return false;
        data.RemoveOutfit(outfit);
        return true;
    }

    public static Game.PlayerData? GetModData(byte player)
    {
        if (Game.GameData.data.playersArray.Count > player)
        {
            return Game.GameData.data.playersArray[player];
        }
        return null;
    }

    public static bool HasModData(byte player)
    {
        if (Game.GameData.data == null) return false;
        return GetModData(player) != null;
    }

    public static Game.PlayerData? GetModData(this PlayerControl player)
    {
        return GetModData(player.PlayerId);
    }

    public static Game.PlayerData? GetModData(this GameData.PlayerInfo player)
    {
        return GetModData(player.PlayerId);
    }

    public static Game.PlayerData? GetModData(this DeadBody player)
    {
        return GetModData(player.ParentId);
    }

    public static DeadBody[] AllDeadBodies()
    {
        //Componentで探すよりタグで探す方が相当はやい
        var bodies = GameObject.FindGameObjectsWithTag("DeadBody");
        DeadBody[] deadBodies = new DeadBody[bodies.Count];
        for (int i = 0; i < bodies.Count; i++) deadBodies[i] = bodies[i].GetComponent<DeadBody>();
        return deadBodies;
    }

    public static float Distance(this Vector3 vector, Vector3 opponent)
    {
        float x = vector.x - opponent.x;
        float y = vector.y - opponent.y;
        return Mathf.Sqrt(x * x + y * y);
    }

    public static void shareGameVersion()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.VersionHandshake, Hazel.SendOption.Reliable, -1);
        writer.Write(NebulaPlugin.Instance.PluginVersionData.Length);
        if (NebulaOption.configDontCareMismatchedNoS.Value)
        {
            for (int i = 0; i < NebulaPlugin.Instance.PluginVersionData.Length; i++) writer.Write((byte)0);
        }
        else
        {
            foreach (byte data in NebulaPlugin.Instance.PluginVersionData) writer.Write(data);
        }
        writer.WritePacked(AmongUsClient.Instance.ClientId);
        writer.Write(Assembly.GetExecutingAssembly().ManifestModule.ModuleVersionId.ToByteArray());
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.VersionHandshake(NebulaPlugin.Instance.PluginVersionData, Assembly.GetExecutingAssembly().ManifestModule.ModuleVersionId, AmongUsClient.Instance.ClientId);
    }

    public static Vector3 GetVector(float radius)
    {
        return GetVector((float)(NebulaPlugin.rnd.NextDouble() * Math.PI * 2f), radius);
    }

    public static Vector3 GetVector(float angle, float radius)
    {
        return new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
    }

    public enum MurderAttemptResult
    {
        PerformKill,
        SuppressKill,
        BlankKill
    }

    private static MurderAttemptResult checkMuderAttempt(PlayerControl killer, PlayerControl target, bool blockRewind = false)
    {
        MurderAttemptResult result = MurderAttemptResult.PerformKill;
        var targetData = target.GetModData();

        if (targetData.guardStatus.RPCGuard())
        {
            RPCEventInvoker.Guard(killer.PlayerId, target.PlayerId);
            return MurderAttemptResult.SuppressKill;
        }

        //GlobalMethod
        result = targetData.role.OnMurdered(killer.PlayerId, target.PlayerId);
        if (result != MurderAttemptResult.PerformKill)
        {
            return result;
        }

        return MurderAttemptResult.PerformKill;
    }

    public static MurderAttemptResult checkMurderAttemptAndAction(PlayerControl killer, PlayerControl target, Action successAction, Action failedAction, bool isMeetingStart = false)
    {
        MurderAttemptResult murder = checkMuderAttempt(killer, target, isMeetingStart);
        switch (murder)
        {
            case MurderAttemptResult.PerformKill:
                successAction();
                break;
            case MurderAttemptResult.SuppressKill:
                failedAction();
                break;
        }

        return murder;
    }

    public static MurderAttemptResult checkMuderAttemptAndKill(PlayerControl killer, PlayerControl target, Game.PlayerData.PlayerStatus status, bool isMeetingStart = false, bool showAnimation = true)
    {
        MurderAttemptResult murder = checkMuderAttempt(killer, target, isMeetingStart);
        switch (murder)
        {
            case MurderAttemptResult.PerformKill:
                RPCEventInvoker.UncheckedMurderPlayer(killer.PlayerId, target.PlayerId, status.Id, showAnimation);
                break;
            case MurderAttemptResult.SuppressKill:
                target.ShowFailedMurder();
                break;
        }

        return murder;
    }

    public static void PlayCustomFlash(Color color, float fadeIn, float fadeOut, float maxAlpha = 0.5f)
    {
        float duration = fadeIn + fadeOut;

        var flash = GameObject.Instantiate(HudManager.Instance.FullScreen, HudManager.Instance.transform);
        flash.color = color;
        flash.enabled = true;
        flash.gameObject.active = true;

        HudManager.Instance.StartCoroutine(Effects.Lerp(duration, new Action<float>((p) =>
        {
            if (p < (fadeIn / duration))
            {
                if (flash != null)
                    flash.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(maxAlpha * p / (fadeIn / duration)));
            }
            else
            {
                if (flash != null)
                    flash.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(maxAlpha * (1 - p) / (fadeOut / duration)));
            }
            if (p == 1f && flash != null)
            {
                flash.enabled = false;
                GameObject.Destroy(flash.gameObject);
            }
        })));
    }

    public static void PlayFlash(Color color)
    {
        PlayCustomFlash(color, 0.375f, 0.375f);
    }

    public static void PlayQuickFlash(Color color)
    {
        PlayCustomFlash(color, 0.1f, 0.4f);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="chance">0～10の間で指定</param>
    /// <returns></returns>
    static public int CalcProbabilityCount(int chance, int max)
    {
        if (max == 0) { return 0; }

        int count = 0;
        double rate = (double)chance / 10.0;
        for (int i = 0; i < max; i++)
        {
            if (NebulaPlugin.rnd.NextDouble() < rate) count++;
        }
        return count;
    }

    static public int[] GetRandomArray(int length)
    {
        int[] arr = new int[length];
        for (int i = 0; i < length; i++)
        {
            arr[i] = i;
        }

        System.Random random = new System.Random();
        arr = arr.OrderBy(x => random.Next()).ToArray();

        return arr;
    }

    static public Type[] GetRandomArray<Type>(ICollection<Type> collection)
    {
        Type[] arr = new Type[collection.Count];
        int index = 0;
        foreach (Type value in collection)
        {
            arr[index] = value;
            index++;
        }

        System.Random random = new System.Random();
        arr = arr.OrderBy(x => random.Next()).ToArray();

        return arr;
    }

    static public void RoleAction(Game.PlayerData? player, System.Action<Roles.Assignable> action)
    {
        if (player == null) return;

        action.Invoke(player.role);

        if (player.extraRole.Count > 0)
        {
            foreach (Roles.ExtraRole role in player.extraRole)
            {
                action.Invoke(role);
            }
        }

    }
    static public void RoleAction(byte playerId, System.Action<Roles.Assignable> action)
    {
        Game.PlayerData data;
        try
        {
            data = Game.GameData.data.AllPlayers[playerId];

            action.Invoke(data.role);
            if (data.ShouldBeGhostRole) action.Invoke(data.ghostRole);

            if (data.extraRole.Count > 0)
            {
                foreach (Roles.ExtraRole role in data.extraRole)
                {
                    action.Invoke(role);
                }
            }
        }
        catch (Exception e) { return; }
    }

    static public void RoleAction(PlayerControl player, System.Action<Roles.Assignable> action)
    {
        RoleAction(player.PlayerId, action);
    }

    static public Game.VentData GetVentData(this Vent vent)
    {
        if (vent == null) return null;
        if (Game.GameData.data == null) return null;
        return Game.GameData.data.GetVentData(vent.gameObject.name);
    }

    static public bool SabotageIsActive()
    {
        foreach (PlayerTask task in PlayerControl.LocalPlayer.myTasks.GetFastEnumerator())
            if (task.TaskType == TaskTypes.FixLights || task.TaskType == TaskTypes.RestoreOxy || task.TaskType == TaskTypes.ResetReactor || task.TaskType == TaskTypes.ResetSeismic || task.TaskType == TaskTypes.FixComms || task.TaskType == TaskTypes.StopCharles)
                return true;
        return false;
    }

    static public void RepairSabotage()
    {
        foreach (PlayerTask task in PlayerControl.LocalPlayer.myTasks)
        {
            if (task.TaskType == TaskTypes.FixLights)
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.FixLights, Hazel.SendOption.Reliable, -1);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCEvents.FixLights();
            }
            else if (task.TaskType == TaskTypes.RestoreOxy)
            {
                ShipStatus.Instance.RpcRepairSystem(SystemTypes.LifeSupp, 0 | 64);
                ShipStatus.Instance.RpcRepairSystem(SystemTypes.LifeSupp, 1 | 64);
            }
            else if (task.TaskType == TaskTypes.ResetReactor)
            {
                ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 16);
            }
            else if (task.TaskType == TaskTypes.ResetSeismic)
            {
                ShipStatus.Instance.RpcRepairSystem(SystemTypes.Laboratory, 16);
            }
            else if (task.TaskType == TaskTypes.FixComms)
            {
                ShipStatus.Instance.RpcRepairSystem(SystemTypes.Comms, 16 | 0);
                ShipStatus.Instance.RpcRepairSystem(SystemTypes.Comms, 16 | 1);
            }
            else if (task.TaskType == TaskTypes.StopCharles)
            {
                ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 0 | 16);
                ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 1 | 16);
            }
        }
    }

    static public Texture2D CreateReadabeTexture(Texture texture)
    {
        RenderTexture renderTexture = RenderTexture.GetTemporary(
                    texture.width,
                    texture.height,
                    0,
                    RenderTextureFormat.Default,
                    RenderTextureReadWrite.Linear);

        Graphics.Blit(texture, renderTexture);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;
        Texture2D readableTextur2D = new Texture2D(texture.width, texture.height);
        readableTextur2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        readableTextur2D.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);

        return readableTextur2D;
    }

    public static void destroyList<T>(Il2CppSystem.Collections.Generic.List<T> items) where T : UnityEngine.Object
    {
        if (items == null) return;
        foreach (T item in items)
        {
            if (item) UnityEngine.Object.Destroy(item);
        }
    }

    public static void destroyList<T>(List<T> items) where T : MonoBehaviour
    {
        if (items == null) return;
        foreach (T item in items)
        {
            if (item) UnityEngine.Object.Destroy(item.gameObject);
        }
    }

    public static void ShowDialog(string text)
    {
        HudManager.Instance.Dialogue.transform.localPosition = new Vector3(0, 0, -920);
        HudManager.Instance.ShowPopUp(Language.Language.GetString(text));
    }

    public static bool AnyShadowsBetween(Vector2 source, Vector2 dirNorm, float mag)
    {
        int num = Physics2D.RaycastNonAlloc(source, dirNorm, PhysicsHelpers.castHits, mag, Constants.ShadowMask);
        bool result = false;
        Collider2D c;
        for (int i = 0; i < num; i++)
        {
            c = PhysicsHelpers.castHits[i].collider;
            if (c.isTrigger) continue;
            if (LightSource.NoShadows.ContainsKey(c.gameObject))
                if (LightSource.NoShadows[c.gameObject].hitOverride == c) continue;
            if (LightSource.OneWayShadows.ContainsKey(c.gameObject))
                if (LightSource.OneWayShadows[c.gameObject].IsIgnored(PlayerControl.LocalPlayer.lightSource)) continue;

            result = true;
            break;
        }
        return result;
    }

    public static bool AnyNonTriggersBetween(Vector2 source, Vector2 dirNorm, float mag, int layerMask, out float distance)
    {
        int num = Physics2D.RaycastNonAlloc(source, dirNorm, PhysicsHelpers.castHits, mag, layerMask);
        bool result = false;
        distance = -1f;
        for (int i = 0; i < num; i++)
        {
            if (!PhysicsHelpers.castHits[i].collider.isTrigger)
            {
                result = true;

                float d = Helpers.Distance(source, PhysicsHelpers.castHits[i].point);
                if (d < distance || distance < -0.5f)
                {
                    distance = d;
                }
            }
        }
        return result;
    }

    public static IEnumerator CoPlayerAppear(this PlayerControl player)
    {
        for (int i = 0; i < 2; i++)
        {
            yield return null;
        }
        player.transform.FindChild("Sprite").gameObject.SetActive(true);
        player.NetTransform.enabled = true;
        player.MyPhysics.enabled = true;
        //player.StartCoroutine(player.MyPhysics.CoSpawnPlayer(LobbyBehaviour.Instance));
    }

    public static void SetTargetWithLight(this FollowerCamera camera, MonoBehaviour target)
    {
        camera.Target = target;
        PlayerControl.LocalPlayer.lightSource.transform.SetParent(target.transform, false);
        if (target != PlayerControl.LocalPlayer) PlayerControl.LocalPlayer.NetTransform.Halt();
    }

    public static void SetLocalTask(this GameData.PlayerInfo player, List<GameData.TaskInfo> taskList)
    {
        var tasks = new Il2CppSystem.Collections.Generic.List<GameData.TaskInfo>(taskList.Count);
        foreach (var t in taskList) tasks.Add(t);


        player.Tasks = tasks;
        player.Object.SetTasks(player.Tasks);

        GameData.Instance.SetDirtyBit(1U << (int)player.PlayerId);
    }

    public static List<GameData.TaskInfo> GetRandomTaskList(int newTasks, double longTaskChance)
    {
        int shortTasks = 0, longTasks = 0;
        int sum = 0;
        for (int i = 0; i < newTasks; i++)
        {
            if (NebulaPlugin.rnd.NextDouble() < longTaskChance)
                longTasks++;
            else
                shortTasks++;
        }

        var tasks = new Il2CppSystem.Collections.Generic.List<byte>();

        int num = 0;
        var usedTypes = new Il2CppSystem.Collections.Generic.HashSet<TaskTypes>();
        Il2CppSystem.Collections.Generic.List<NormalPlayerTask> unused;

        unused = new Il2CppSystem.Collections.Generic.List<NormalPlayerTask>();
        foreach (var t in ShipStatus.Instance.LongTasks)
            unused.Add(t);
        Extensions.Shuffle<NormalPlayerTask>(unused.Cast<Il2CppSystem.Collections.Generic.IList<NormalPlayerTask>>(), 0);
        ShipStatus.Instance.AddTasksFromList(ref num, longTasks, tasks, usedTypes, unused);

        unused = new Il2CppSystem.Collections.Generic.List<NormalPlayerTask>();
        foreach (var t in ShipStatus.Instance.NormalTasks)
        {
            if (t.TaskType == TaskTypes.PickUpTowels) continue;
            unused.Add(t);
        }
        Extensions.Shuffle<NormalPlayerTask>(unused.Cast<Il2CppSystem.Collections.Generic.IList<NormalPlayerTask>>(), 0);
        ShipStatus.Instance.AddTasksFromList(ref num, shortTasks, tasks, usedTypes, unused);

        var result = new List<GameData.TaskInfo>();
        uint n = 0;
        foreach (var t in tasks)
        {
            result.Add(new GameData.TaskInfo(t, n));
            n++;
        }
        return result;
    }

    public static AudioClip? FindSound(string sound)
    {
        foreach (var audio in UnityEngine.Object.FindObjectsOfTypeIncludingAssets(AudioClip.Il2CppType))
        {
            if (audio.name == sound) return audio.Cast<AudioClip>();
        }
        return null;
    }

    public static void SetPlayerDefaultOutfit(this PoolablePlayer poolable, PlayerControl player)
    {
        poolable.cosmetics.ResetCosmetics();
        poolable.cosmetics.SetColor(player.Data.DefaultOutfit.ColorId);
        poolable.cosmetics.SetBodyColor(player.Data.DefaultOutfit.ColorId);
        if (player.Data.DefaultOutfit.SkinId != null) poolable.cosmetics.SetSkin(player.Data.DefaultOutfit.SkinId, player.Data.DefaultOutfit.ColorId);
        if (player.Data.DefaultOutfit.HatId != null) poolable.cosmetics.SetHat(player.Data.DefaultOutfit.HatId, player.Data.DefaultOutfit.ColorId);
        if (player.Data.DefaultOutfit.VisorId != null) poolable.cosmetics.SetVisor(player.Data.DefaultOutfit.VisorId, player.Data.DefaultOutfit.ColorId);
        poolable.cosmetics.nameText.text = "";
    }


    public static Color Blend(this Color myColor, Color color, float rate)
    {
        if (rate > 1f) rate = 1f;
        float myRate = 1 - rate;
        return new Color(
            myColor.r * myRate + color.r * rate,
            myColor.g * myRate + color.g * rate,
            myColor.b * myRate + color.b * rate,
            myColor.a * myRate + color.a * rate);
    }

    public static T[] ToArray<T>(this IEnumerator<T> enumerator)
    {
        List<T> result = new List<T>();
        while (enumerator.MoveNext()) result.Add(enumerator.Current);
        return result.ToArray();
    }

    public static void Ping(Vector2 pos,bool smallenNearPing) => Ping(new Vector2[] { pos }, smallenNearPing);
    public static void Ping(Vector2[] pos, bool smallenNearPing)
    {
        if (!HudManager.InstanceExists) return;

        var prefab = GameManagerCreator.Instance.HideAndSeekManagerPrefab.PingPool.Prefab.CastFast<PingBehaviour>();

        PingBehaviour[] pings = new PingBehaviour[pos.Length];
        int i = 0;
        foreach (var p in pos)
        {
            var ping = GameObject.Instantiate(prefab);
            ping.target = p;
            ping.AmSeeker = smallenNearPing;
            ping.UpdatePosition();
            ping.gameObject.SetActive(true);
            ping.SetImageEnabled(true);
            pings[i++] = ping;
        }

        IEnumerator GetEnumarator()
        {
            yield return new WaitForSeconds(2f);

            foreach(var p in pings)GameObject.Destroy(p.gameObject);
        }

        HudManager.Instance.StartCoroutine(GetEnumarator().WrapToIl2Cpp());
    } 
}