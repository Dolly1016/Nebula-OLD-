using AmongUs.Data.Player;
using AmongUs.GameOptions;
using Nebula.Game;
using Nebula.Modules;
using Nebula.Roles;
using TMPro;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Nebula.Player;

[NebulaPreLoad]
public static class PlayerState
{
    public static TranslatableTag Alive = new("state.alive");
    public static TranslatableTag Dead = new("state.dead");
    public static TranslatableTag Exiled = new("state.exiled");
    public static TranslatableTag Misfired = new("state.misfired");
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
    
    public byte? HoldingDeadBody { get; private set; } = null;
    private DeadBody? deadBodyCache { get; set; } = null;

    public RoleInstance Role => myRole;
    private RoleInstance myRole;
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

        var viewerInfo = PlayerControl.LocalPlayer.GetModInfo();

        viewerInfo?.Role?.DecoratePlayerName(ref text, ref color);

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

        if (myRole.HasAnyTasks)
            text += (" (" + Tasks.ToString(NebulaGameManager.Instance.CanSeeAllInfo || !AmongUsUtil.InCommSab) + ")").Color(myRole.HasCrewmateTasks ? crewTaskColor : fakeTaskColor);
        
        roleText.text = text;

        roleText.gameObject.SetActive(NebulaGameManager.Instance.CanSeeAllInfo || AmOwner);
    }

    private void SetRole(AbstractRole role, int[]? arguments)
    {
        myRole?.Inactivate();

        if (role.RoleCategory == Roles.RoleCategory.ImpostorRole)
            DestroyableSingleton<RoleManager>.Instance.SetRole(MyControl, RoleTypes.Impostor);
        else
            DestroyableSingleton<RoleManager>.Instance.SetRole(MyControl, RoleTypes.Crewmate);
        
        myRole = role.CreateInstance(this, arguments);
        myRole.OnActivated();
    }

    private void SetModifier(AbstractModifier role, int[]? arguments)
    {
        var modifier = role.CreateInstance(this, arguments);
        myModifiers.Add(modifier);
        modifier.OnActivated();
    }

    public void RpcSetRole(AbstractRole role, int[]? arguments) => RpcSetAssignable.Invoke(new SetAssignableMessage() { playerId = PlayerId, assignableId = role.Id, arguments = arguments, isRole = true });
    public void RpcSetModifier(AbstractModifier modifier, int[]? arguments) => RpcSetAssignable.Invoke(new SetAssignableMessage() { playerId = PlayerId, assignableId = modifier.Id, arguments = arguments, isRole = false });


    public class SetAssignableMessage
    {
        public byte playerId;
        public int assignableId;
        public int[]? arguments;
        public bool isRole;
    }

    public readonly static RemoteProcess<SetAssignableMessage> RpcSetAssignable = new RemoteProcess<SetAssignableMessage>(
        "SetAssignable",
        (writer, message) =>
        {
            writer.Write(message.playerId);
            writer.Write(message.assignableId);
            writer.Write(message.isRole);
            writer.Write(message.arguments?.Length ?? 0);
            for (int i = 0; i < (message.arguments?.Length ?? 0); i++) writer.Write(message.arguments![i]);
        },
        (reader) =>
        {
            SetAssignableMessage message = new SetAssignableMessage();
            message.playerId = reader.ReadByte();
            message.assignableId = reader.ReadInt32();
            message.isRole = reader.ReadBoolean();
            int length = reader.ReadInt32();
            if (length > 0)
            {
                message.arguments = new int[length];
                for (int i = 0; i < length; i++) message.arguments[i] = reader.ReadInt32();
            }
            return message;
        },
        (message, isCalledByMe) =>
        {
            var player = NebulaGameManager.Instance!.RegisterPlayer(PlayerControl.AllPlayerControls.Find((Il2CppSystem.Predicate<PlayerControl>)(p => p.PlayerId == message.playerId)));
            if(message.isRole)
                player.SetRole(Roles.Roles.AllRoles[message.assignableId], message.arguments);
            else
                player.SetModifier(Roles.Roles.AllModifiers[message.assignableId], message.arguments);
        }
        );

    public readonly static RemoteProcess<Tuple<byte,OutfitCandidate>> RpcAddOutfit = new RemoteProcess<Tuple<byte, OutfitCandidate>>(
        "AddOutfit",
        (writer, message) =>
        {
            writer.Write(message.Item1);
            writer.Write(message.Item2.outfit.PlayerName);
            writer.Write(message.Item2.outfit.HatId);
            writer.Write(message.Item2.outfit.SkinId);
            writer.Write(message.Item2.outfit.VisorId);
            writer.Write(message.Item2.outfit.PetId);
            writer.Write(message.Item2.outfit.ColorId);
            writer.Write(message.Item2.Tag);
            writer.Write(message.Item2.Priority);
            writer.Write(message.Item2.SelfAware);
        },
        (reader) =>
        {
            byte playerId = reader.ReadByte();
            GameData.PlayerOutfit outfit = new();
            outfit.PlayerName = reader.ReadString();
            outfit.HatId = reader.ReadString();
            outfit.SkinId = reader.ReadString();
            outfit.VisorId = reader.ReadString();
            outfit.PetId = reader.ReadString();
            outfit.ColorId = reader.ReadInt32();
            return new(playerId, new(reader.ReadString(), reader.ReadInt32(), reader.ReadBoolean() ,outfit));
        },
        (message, isCalledByMe) =>
        {
            NebulaGameManager.Instance.GetModPlayerInfo(message.Item1).AddOutfit(message.Item2);
        }
        );

    public readonly static RemoteProcess<Tuple<byte, string>> RpcRemoveOutfit = new RemoteProcess<Tuple<byte, string>>(
       "RemoveOutfit",
       (writer, message) =>
       {
           writer.Write(message.Item1);
           writer.Write(message.Item2);
       },
       (reader) =>
       {
           return new(reader.ReadByte(), reader.ReadString());
       },
       (message, isCalledByMe) =>
       {
           NebulaGameManager.Instance.GetModPlayerInfo(message.Item1).RemoveOutfit(message.Item2);
       }
       );

    private void UpdateHoldingDeadBody()
    {
        if (!HoldingDeadBody.HasValue) return;

        //同じ死体を持つプレイヤーがいる
        if (NebulaGameManager.Instance.AllPlayerInfo().Any(p => p.PlayerId < PlayerId && p.HoldingDeadBody.HasValue && p.HoldingDeadBody.Value == HoldingDeadBody.Value))
        {
            deadBodyCache = null;
            if (AmOwner) ReleaseDeadBody();
            return;
        }


        if (!deadBodyCache || deadBodyCache.ParentId != HoldingDeadBody.Value)
        {
            deadBodyCache = Helpers.AllDeadBodies().FirstOrDefault((d) => d.ParentId == HoldingDeadBody.Value);
            if (!deadBodyCache)
            {
                deadBodyCache = null;
                if (AmOwner) ReleaseDeadBody();
                return;
            }
        }

        if (MyControl.inVent)
        {
            deadBodyCache.transform.localPosition = new Vector3(10000, 10000);
        }
        else
        {
            var targetPosition = MyControl.transform.position + new Vector3(-0.1f, -0.1f);

            if (MyControl.transform.position.Distance(deadBodyCache.transform.position) < 1.8f)
                deadBodyCache.transform.position += (targetPosition - deadBodyCache.transform.position) * 0.15f;
            else
                deadBodyCache.transform.position = targetPosition;


            Vector3 playerPos = MyControl.GetTruePosition();
            Vector3 deadBodyPos = deadBodyCache.TruePosition;
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

                deadBodyCache.transform.localPosition = playerPos + diff.normalized * d;
            }
            else
            {
                deadBodyCache.transform.localPosition = deadBodyCache.transform.position;
            }
        }
    }

    public void ReleaseDeadBody() {
        RpcHoldDeadBody.Invoke(new(PlayerId, byte.MaxValue, deadBodyCache?.transform.localPosition ?? new Vector2(10000, 10000)));
    }

    public void HoldDeadBody(DeadBody deadBody) {
        RpcHoldDeadBody.Invoke(new(PlayerId, deadBody.ParentId, deadBody.transform.position));
    }

    readonly static RemoteProcess<Tuple<byte, byte,Vector2>> RpcHoldDeadBody = new RemoteProcess<Tuple<byte, byte, Vector2>>(
      "HoldDeadBody",
      (writer, message) =>
      {
          writer.Write(message.Item1);
          writer.Write(message.Item2);
          writer.Write(message.Item3.x);
          writer.Write(message.Item3.y);
      },
      (reader) =>
      {
          return new(reader.ReadByte(), reader.ReadByte(), new(reader.ReadSingle(), reader.ReadSingle()));
      },
      (message, isCalledByMe) =>
      {
          var info = NebulaGameManager.Instance.GetModPlayerInfo(message.Item1);
          
          if(message.Item2 == byte.MaxValue)
          {
              info.HoldingDeadBody = null;
          }
          else
          {
              info.HoldingDeadBody = message.Item2;

              var deadBody = Helpers.AllDeadBodies().FirstOrDefault(d => d.ParentId == message.Item2);
              info.deadBodyCache = deadBody;
              if (deadBody && message.Item3.magnitude < 10000) deadBody.transform.localPosition = new Vector3(message.Item3.x, message.Item3.y, message.Item3.y / 1000f);
          }
      }
      );

    public void Update()
    {
        UpdateNameText(MyControl.cosmetics.nameText, true);
        UpdateRoleText(roleText);
        UpdateHoldingDeadBody();

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

        RoleAction((role) =>role.OnGameStart());
    }
    
}
