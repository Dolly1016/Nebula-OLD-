using AmongUs.Data.Player;
using AmongUs.GameOptions;
using Nebula.Game;
using Nebula.Modules;
using Nebula.Roles;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using static Il2CppSystem.Globalization.CultureInfo;
using static UnityEngine.GraphicsBuffer;

namespace Nebula.Player;

[NebulaPreLoad(typeof(TranslatableTag))]
public static class PlayerState
{
    public static TranslatableTag Alive = new("state.alive");
    public static TranslatableTag Dead = new("state.dead");
    public static TranslatableTag Exiled = new("state.exiled");
    public static TranslatableTag Misfired = new("state.misfired");
    public static TranslatableTag Sniped = new("state.sniped");
    public static TranslatableTag Beaten = new("state.beaten");
    public static TranslatableTag Guessed = new("state.guessed");
    public static TranslatableTag Misguessed = new("state.misguessed");
    public static TranslatableTag Embroiled = new("state.embroiled");
    public static TranslatableTag Suicide = new("state.suicide");
    public static TranslatableTag Trapped = new("state.trapped");
}

public class TimeLimitedModulator
{
    public float Timer { get; private set; }
    public bool CanPassMeeting { get; private set; }
    public int Priority { get; private set; }
    public int DuplicateTag { get; private set; }

    public void Update()
    {
        if (Timer < 9999f) Timer -= Time.deltaTime;
    }

    public void OnMeetingStart()
    {
        if (!CanPassMeeting) Timer = -1f;
    }

    public bool IsBroken => Timer < 0f;

    public TimeLimitedModulator(float timer, bool canPassMeeting, int priority, int? duplicateTag)
    {
        this.Timer = timer;
        this.CanPassMeeting = canPassMeeting;
        this.Priority = priority;
        this.DuplicateTag = duplicateTag ?? 0;
    }
}

public class SpeedModulator : TimeLimitedModulator
{
    public float Num { get; private set; }
    public bool IsMultiplier { get; private set; }
   
    public void Calc(ref float speed)
    {
        if (IsMultiplier)
            speed *= Num;
        else
            speed += Num;
    }

    
    public SpeedModulator(float? num, bool isMultiplier, float timer, bool canPassMeeting,int priority, int duplicateTag = 0) :base(timer,canPassMeeting, priority, duplicateTag)
    {
        this.Num = num ?? 10000f;
        this.IsMultiplier = isMultiplier;
    }
}

public class AttributeModulator : TimeLimitedModulator
{
    public enum PlayerAttribute
    {
        Invisibility,
        MaxId
    }

    public PlayerAttribute Attribute;

    public AttributeModulator(PlayerAttribute attribute, float timer, bool canPassMeeting, int priority, int duplicateTag = 0) : base(timer, canPassMeeting, priority, duplicateTag)
    {
        Attribute = attribute;
    }
}


[NebulaRPCHolder]
public class PlayerModInfo
{
    public class OutfitCandidate
    {
        public string Tag { get; private set; }
        public int Priority { get; private set; }
        public bool SelfAware { get; private set; }
        public GameData.PlayerOutfit outfit { get; private set; }

        public OutfitCandidate(string tag,int priority,bool selfAware,GameData.PlayerOutfit outfit)
        {
            this.Tag = tag;
            this.Priority = priority;
            this.SelfAware = selfAware;
            this.outfit = outfit;
        }
    }

    public PlayerControl MyControl { get; private set; }
    public byte PlayerId { get; private set; }
    public bool AmOwner { get; private set; }
    public bool IsDisconnected { get; set; } = false;
    public bool IsDead => IsDisconnected || MyControl.Data.IsDead;
    public float MouseAngle { get; private set; }
    private bool requiredUpdateMouseAngle { get; set; }
    public void RequireUpdateMouseAngle()=> requiredUpdateMouseAngle= true;
    
    public byte? HoldingDeadBody { get; private set; } = null;
    private DeadBody? deadBodyCache { get; set; } = null;
    private List<SpeedModulator> speedModulators = new();
    private List<AttributeModulator> attributeModulators = new();

    public RoleInstance Role => myRole;
    private RoleInstance myRole = null!;
    private List<ModifierInstance> myModifiers = new();

    private List<OutfitCandidate> outfits = new List<OutfitCandidate>();
    private TMPro.TextMeshPro roleText;

    public PlayerTaskState Tasks { get; set; }

    public bool HasCrewmateTasks
    {
        get
        {
            bool hasCrewmateTasks = Role.HasCrewmateTasks;
            ModifierAction((modifier) => { hasCrewmateTasks &= !modifier.InvalidateCrewmateTask; });
            return hasCrewmateTasks;
        }
    }

    //各種収集データ
    public PlayerModInfo? MyKiller = null;
    public float? DeathTimeStamp = null;

    public IEnumerable<AssignableInstance> AllAssigned()
    {
        if (Role != null) yield return Role;

        foreach (var m in myModifiers) yield return m;
    }

    public void RoleAction(Action<AssignableInstance> action)
    {
        foreach (var role in AllAssigned()) action(role);
    }

    public void ModifierAction(Action<ModifierInstance> action)
    {
        foreach (var role in myModifiers) action(role);
    }

    public IEnumerable<ModifierInstance> AllModifiers => myModifiers;
    public bool TryGetModifier<Modifier>(out Modifier modifier) where Modifier : ModifierInstance {
        foreach(var m in AllModifiers)
        {
            if(m is Modifier result)
            {
                modifier = result;
                return true;
            }
        }
        modifier = null!;
        return false;
    }

    public PlayerModInfo(PlayerControl myPlayer)
    {
        this.MyControl = myPlayer;
        this.Tasks = new PlayerTaskState(myPlayer);
        PlayerId = myPlayer.PlayerId;
        AmOwner = myPlayer.AmOwner;
        DefaultOutfit = myPlayer.Data.DefaultOutfit;
        roleText = GameObject.Instantiate(myPlayer.cosmetics.nameText, myPlayer.cosmetics.nameText.transform);
        roleText.transform.localPosition = new Vector3(0,0.185f,-0.01f);
        roleText.fontSize = 1.7f;
        roleText.text = "Unassigned";
    }

    public string DefaultName => DefaultOutfit.PlayerName;
    public GameData.PlayerOutfit DefaultOutfit { get; private set; }
    public GameData.PlayerOutfit CurrentOutfit => outfits.Count>0 ? outfits[0].outfit : DefaultOutfit;

    private void UpdateOutfit()
    {
        GameData.PlayerOutfit newOutfit = DefaultOutfit;
        if (outfits.Count > 0)
        {
            outfits.Sort((o1, o2) => o2.Priority - o1.Priority);
            newOutfit = outfits[outfits.Count - 1].outfit;
        }

        MyControl.RawSetColor(newOutfit.ColorId);
        MyControl.RawSetName(newOutfit.PlayerName);
        MyControl.RawSetHat(newOutfit.HatId, newOutfit.ColorId);
        MyControl.RawSetSkin(newOutfit.SkinId, newOutfit.ColorId);
        MyControl.RawSetVisor(newOutfit.VisorId, newOutfit.ColorId);
        MyControl.RawSetPet(newOutfit.PetId, newOutfit.ColorId);
        MyControl.RawSetColor(newOutfit.ColorId);
        MyControl.MyPhysics.ResetAnimState();
        MyControl.cosmetics.StopAllAnimations();
    }

    public void AddOutfit(OutfitCandidate outfit)
    {
        if (!outfit.SelfAware && MyControl.AmOwner) return;
        outfits.Add(outfit);
        UpdateOutfit();
    }

    public void RemoveOutfit(string tag)
    {
        outfits.RemoveAll(o => o.Tag.Equals(tag));
        UpdateOutfit();
    }

    public GameData.PlayerOutfit GetOutfit(int maxPriority)
    {
        foreach(var outfit in outfits) if (outfit.Priority <= maxPriority) return outfit.outfit;
        return DefaultOutfit;
    }

    public void UpdateNameText(TMPro.TextMeshPro nameText,bool showDefaultName = false)
    {
        var text = CurrentOutfit.PlayerName;
        var color = Color.white;

        RoleAction(r => r.DecoratePlayerName(ref text, ref color));
        PlayerControl.LocalPlayer.GetModInfo()?.RoleAction(r=>r.DecorateOtherPlayerName(this,ref text,ref color));

        if (showDefaultName && !CurrentOutfit.PlayerName.Equals(DefaultName))
            text += (" (" + DefaultName + ")").Color(Color.gray);
        

        nameText.text = text;
        nameText.color = color;

        
    }

    static Color fakeTaskColor = new Color(0x86 / 255f, 0x86 / 255f, 0x86 / 255f);
    static Color crewTaskColor = new Color(0xFA / 255f, 0xD9 / 255f, 0x34 / 255f);
    public void UpdateRoleText(TMPro.TextMeshPro roleText) {
        
        string text = myRole?.DisplayRoleName ?? "Undefined";

        ModifierAction(m => m.DecorateRoleName(ref text));

        if (myRole?.HasAnyTasks ?? true)
            text += (" (" + Tasks.ToString((NebulaGameManager.Instance?.CanSeeAllInfo ?? false) || !AmongUsUtil.InCommSab) + ")").Color((myRole?.HasCrewmateTasks ?? false) ? crewTaskColor : fakeTaskColor);
        
        roleText.text = text;

        roleText.gameObject.SetActive((NebulaGameManager.Instance?.CanSeeAllInfo ?? false) || AmOwner);
    }

    private void SetRole(AbstractRole role, int[] arguments)
    {
        myRole?.Inactivate();

        if (role.RoleCategory == Roles.RoleCategory.ImpostorRole)
            DestroyableSingleton<RoleManager>.Instance.SetRole(MyControl, RoleTypes.Impostor);
        else
            DestroyableSingleton<RoleManager>.Instance.SetRole(MyControl, RoleTypes.Crewmate);
        
        myRole = role.CreateInstance(this, arguments);
        myRole.OnActivated();
    }

    private void SetModifier(AbstractModifier role, int[] arguments)
    {
        var modifier = role.CreateInstance(this, arguments);
        myModifiers.Add(modifier);
        modifier.OnActivated();
    }

    public void OnSetAttribute(AttributeModulator.PlayerAttribute attribute) { }
    public void OnUnsetAttribute(AttributeModulator.PlayerAttribute attribute) { }

    public bool HasAttribute(AttributeModulator.PlayerAttribute attribute) => attributeModulators.Any(m => m.Attribute == attribute);

    public NebulaRPCInvoker RpcInvokerSetRole(AbstractRole role, int[]? arguments) => RpcSetAssignable.GetInvoker((PlayerId, role.Id, arguments ?? Array.Empty<int>(), true));
    public NebulaRPCInvoker RpcInvokerSetModifier(AbstractModifier modifier, int[]? arguments) => RpcSetAssignable.GetInvoker((PlayerId, modifier.Id, arguments ?? Array.Empty<int>(), false));
    public NebulaRPCInvoker RpcInvokerUnsetModifier(AbstractModifier modifier) => RpcRemoveModifier.GetInvoker(new(PlayerId,modifier.Id));
    public void UnsetModifierLocal(Predicate<ModifierInstance> predicate)
    {
        myModifiers.RemoveAll(m =>
        {
            if (predicate.Invoke(m))
            {
                m.Inactivate();
                return true;
            }
            return false;
        });
        if (NebulaGameManager.Instance?.GameState != NebulaGameStates.NotStarted) HudManager.Instance.UpdateHudContent();
    }

    public readonly static RemoteProcess<(byte playerId,int assignableId, int[] arguments,bool isRole)> RpcSetAssignable = new(
        "SetAssignable",
        (message, isCalledByMe) =>
        {
            var player = NebulaGameManager.Instance!.RegisterPlayer(PlayerControl.AllPlayerControls.Find((Il2CppSystem.Predicate<PlayerControl>)(p => p.PlayerId == message.playerId)));
            if(message.isRole)
                player.SetRole(Roles.Roles.AllRoles[message.assignableId], message.arguments);
            else
                player.SetModifier(Roles.Roles.AllModifiers[message.assignableId], message.arguments);

            if (NebulaGameManager.Instance.GameState != NebulaGameStates.NotStarted) HudManager.Instance.UpdateHudContent();
            
        }
        );

    private readonly static RemoteProcess<(byte playerId, int modifierId)> RpcRemoveModifier = new(
        "RemoveModifier", (message, _) => NebulaGameManager.Instance?.GetModPlayerInfo(message.playerId)?.UnsetModifierLocal((m) => m.Role.Id == message.modifierId)
        );

    public readonly static RemoteProcess<(byte playerId,OutfitCandidate outfit)> RpcAddOutfit = new(
        "AddOutfit", (message, _) => NebulaGameManager.Instance?.GetModPlayerInfo(message.playerId)?.AddOutfit(message.outfit)
        );

    public readonly static RemoteProcess<(byte playerId, string tag)> RpcRemoveOutfit = new(
       "RemoveOutfit", (message, _) => NebulaGameManager.Instance!.GetModPlayerInfo(message.playerId)?.RemoveOutfit(message.tag)
       );

    public readonly static RemoteProcess<(byte playerId, float angle)> RpcUpdateAngle = new(
       "UpdateAngle", (message, _) => NebulaGameManager.Instance!.GetModPlayerInfo(message.playerId)!.MouseAngle = message.angle
       );

    public readonly static RemoteProcess<(byte playerId, SpeedModulator modulator)> RpcSpeedModulator = new(
       "AddSpeedModulator", (message, _) =>
       {
           var modulators = NebulaGameManager.Instance!.GetModPlayerInfo(message.playerId)!.speedModulators;
           if (message.modulator.DuplicateTag != 0 && modulators.Any(m => m.DuplicateTag == message.modulator.DuplicateTag)) return;
           modulators.Add(message.modulator);
           modulators.Sort((m1, m2) => m2.Priority - m1.Priority);
           
       }
       );

    public readonly static RemoteProcess<(byte playerId, AttributeModulator modulator)> RpcAttrModulator = new(
       "AddAttributeModulator", (message, _) =>
       {
           var playerInfo = NebulaGameManager.Instance!.GetModPlayerInfo(message.playerId);
           if (playerInfo == null) return;

           var modulators = playerInfo!.attributeModulators;
           if (message.modulator.DuplicateTag != 0 && modulators.Any(m => m.DuplicateTag == message.modulator.DuplicateTag)) return;

           //新たな属性が付与されたとき
           if (!modulators.Any(m => m.Attribute == message.modulator.Attribute)) playerInfo!.OnSetAttribute(message.modulator.Attribute);
           
           modulators.Add(message.modulator);
           modulators.Sort((m1, m2) => m2.Priority - m1.Priority);
       }
       );

    private void UpdateHoldingDeadBody()
    {
        if (!HoldingDeadBody.HasValue) return;

        //同じ死体を持つプレイヤーがいる
        if (NebulaGameManager.Instance?.AllPlayerInfo().Any(p => p.PlayerId < PlayerId && p.HoldingDeadBody.HasValue && p.HoldingDeadBody.Value == HoldingDeadBody.Value) ?? false)
        {
            deadBodyCache = null;
            if (AmOwner) ReleaseDeadBody();
            return;
        }


        if (!deadBodyCache || deadBodyCache!.ParentId != HoldingDeadBody.Value)
        {
            deadBodyCache = Helpers.AllDeadBodies().FirstOrDefault((d) => d.ParentId == HoldingDeadBody.Value);
            if (!deadBodyCache)
            {
                deadBodyCache = null;
                if (AmOwner) ReleaseDeadBody();
                return;
            }
        }

        //ベント中の死体
        deadBodyCache!.Reported = MyControl.inVent;
        foreach (var r in deadBodyCache!.bodyRenderers) r.enabled = !MyControl.inVent;

        var targetPosition = MyControl.transform.position + new Vector3(-0.1f, -0.1f);

        if (MyControl.transform.position.Distance(deadBodyCache!.transform.position) < 1.8f)
            deadBodyCache!.transform.position += (targetPosition - deadBodyCache!.transform.position) * 0.15f;
        else
            deadBodyCache!.transform.position = targetPosition;


        Vector3 playerPos = MyControl.GetTruePosition();
        Vector3 deadBodyPos = deadBodyCache!.TruePosition;
        Vector3 diff = (deadBodyPos - playerPos);
        float d = diff.magnitude;
        if (PhysicsHelpers.AnythingBetween(playerPos, deadBodyPos, Constants.ShipAndAllObjectsMask, false))
        {
            foreach (var ray in PhysicsHelpers.castHits)
            {
                float temp = ((Vector3)ray.point - playerPos).magnitude;
                if (d > temp) d = temp;
            }

            d -= 0.15f;
            if (d < 0f) d = 0f;

            deadBodyCache!.transform.localPosition = playerPos + diff.normalized * d;
        }
        else
        {
            deadBodyCache!.transform.localPosition = deadBodyCache!.transform.position;
        }

    }

    public void ReleaseDeadBody() {
        RpcHoldDeadBody.Invoke(new(PlayerId, byte.MaxValue, deadBodyCache?.transform.localPosition ?? new Vector2(10000, 10000)));
    }

    public void HoldDeadBody(DeadBody deadBody) {
        RpcHoldDeadBody.Invoke(new(PlayerId, deadBody.ParentId, deadBody.transform.position));
    }

    readonly static RemoteProcess<(byte holderId, byte bodyId, Vector2 pos)> RpcHoldDeadBody = new(
      "HoldDeadBody",
      (message, _) =>
      {
          var info = NebulaGameManager.Instance?.GetModPlayerInfo(message.holderId);
          if (info == null) return;

          if(message.bodyId == byte.MaxValue)
              info.HoldingDeadBody = null;
          else
          {
              info.HoldingDeadBody = message.bodyId;
              var deadBody = Helpers.AllDeadBodies().FirstOrDefault(d => d.ParentId == message.bodyId);
              info.deadBodyCache = deadBody;
              if (deadBody && message.pos.magnitude < 10000) deadBody!.transform.localPosition = new Vector3(message.Item3.x, message.Item3.y, message.Item3.y / 1000f);
          }
      }
      );

    private void UpdateMouseAngle()
    {
        if (!requiredUpdateMouseAngle) return;

        Vector2 vec = (Vector2)Input.mousePosition - new Vector2(Screen.width / 2, Screen.height / 2);
        float currentAngle = Mathf.Atan2(vec.y,vec.x);

        if (Mathf.Repeat(currentAngle - MouseAngle, Mathf.PI * 2f) > 0.02f) RpcUpdateAngle.Invoke((PlayerId, currentAngle));

        requiredUpdateMouseAngle = false;
    }

    private void UpdateSpeedModulators()
    {
        foreach(var m in speedModulators) m.Update();
        speedModulators.RemoveAll(m => m.IsBroken);
        MyControl.MyPhysics.Speed = CalcSpeed();
    }

    private void UpdateAttributeModulators()
    {
        ulong maskBefore = 0, maskAfter = 0;
        foreach (var m in attributeModulators)
        {
            m.Update();
            maskBefore |= 1ul << (int)m.Attribute;
        }
        attributeModulators.RemoveAll(m => m.IsBroken);
        foreach (var m in attributeModulators) maskAfter |= 1ul << (int)m.Attribute;

        ulong mask = maskBefore ^ maskAfter;
        for(int i = 0; i < (int)AttributeModulator.PlayerAttribute.MaxId; i++)
        {
            if ((mask & (1ul << i)) != 0) OnUnsetAttribute((AttributeModulator.PlayerAttribute)i);
        }
    }

    private void UpdateVisibility()
    {
        bool isInvisible = HasAttribute(AttributeModulator.PlayerAttribute.Invisibility) && !IsDead;
        MyControl.cosmetics.nameText.gameObject.SetActive((!isInvisible) || AmOwner || (NebulaGameManager.Instance?.CanSeeAllInfo ?? false));

        if (IsDead) return;

        float alpha = MyControl.cosmetics.currentBodySprite.BodySprite.color.a;
        if (isInvisible)
            alpha -= 0.85f * Time.deltaTime;
        else
            alpha += 0.85f * Time.deltaTime;

        float min = 0f, max = 1f;
        if (AmOwner || (NebulaGameManager.Instance?.CanSeeAllInfo ?? false)) min = 0.25f;
        alpha = Mathf.Clamp(alpha, min, max);


        var color = new Color(1f, 1f, 1f, alpha);
        

        if (MyControl.cosmetics.currentBodySprite.BodySprite != null) MyControl.cosmetics.currentBodySprite.BodySprite.color = color;

        if (MyControl.cosmetics.skin.layer != null) MyControl.cosmetics.skin.layer.color = color;

        if (MyControl.cosmetics.hat)
        {
            if (MyControl.cosmetics.hat.FrontLayer != null) MyControl.cosmetics.hat.FrontLayer.color = color;
            if (MyControl.cosmetics.hat.BackLayer != null) MyControl.cosmetics.hat.BackLayer.color = color;
        }

        if (MyControl.cosmetics.currentPet)
        {
            if (MyControl.cosmetics.currentPet.rend != null) MyControl.cosmetics.currentPet.rend.color = color;

            if (MyControl.cosmetics.currentPet.shadowRend != null) MyControl.cosmetics.currentPet.shadowRend.color = color;
        }

        if (MyControl.cosmetics.visor != null) MyControl.cosmetics.visor.Image.color = color;
    }

    public void Update()
    {
        UpdateNameText(MyControl.cosmetics.nameText, NebulaGameManager.Instance?.CanSeeAllInfo ?? false);
        UpdateRoleText(roleText);
        UpdateHoldingDeadBody();
        UpdateMouseAngle();
        UpdateSpeedModulators();
        UpdateAttributeModulators();
        UpdateVisibility();

        RoleAction((role) => {
            role.Update();
            if (MyControl.AmOwner) role.LocalUpdate();
        });
    }

    public void OnGameStart()
    {
        if (AmOwner)
        {
            if (!Role.HasAnyTasks)
                Tasks.WaiveAllTasksAsOutsider();
            else if (!Role.HasCrewmateTasks)
                Tasks.BecomeToOutsider();
        }

        UpdateOutfit();

        RoleAction((role) =>role.OnGameStart());
    }

    public void OnMeetingStart()
    {
        foreach (var m in speedModulators) m.OnMeetingStart();
    }

    public void CalcSpeed(ref float speed)
    {
        foreach (var m in speedModulators) m.Calc(ref speed);
    }

    public float CalcSpeed()
    {
        float speed = 2.5f;
        CalcSpeed(ref speed);
        return speed;
    }
}
