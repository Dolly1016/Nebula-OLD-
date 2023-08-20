using AmongUs.GameOptions;
using Nebula.Game;
using Nebula.Modules;
using Nebula.Roles;

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
public class PlayerTaskState
{
    public int CurrentTasks { get; private set; } = 0;
    public int CurrentCompleted { get; private set; } = 0;
    public int TotalTasks { get; private set; } = 0;
    public int TotalCompleted { get; private set; } = 0;
    public int Quota { get; private set; } = 0;
    public bool IsCrewmateTask { get; private set; } = true;
    private PlayerControl player { get; init; }

    public PlayerTaskState(PlayerControl player)
    {
        this.player = player;

        GetTasks(out int shortTasks, out int longTasks, out int commonTasks);
        int sum = shortTasks + longTasks + commonTasks;
        Quota = TotalTasks = CurrentTasks = sum;
    }

    public string ToString(bool canSee)
    {
        return canSee ? (TotalCompleted + "/" + Quota) : "?/?";
    }
    public static void GetTasks(out int shortTasks,out int longTasks,out int commonTasks)
    {
        var option = GameOptionsManager.Instance.CurrentGameOptions;
        shortTasks = option.GetInt(Int32OptionNames.NumShortTasks);
        longTasks = option.GetInt(Int32OptionNames.NumLongTasks);
        commonTasks = option.GetInt(Int32OptionNames.NumCommonTasks);
    }

    public void OnCompleteTask()
    {
        RpcUpdateTaskState.Invoke(new Tuple<byte, TaskUpdateMessage>(player.PlayerId,TaskUpdateMessage.CompleteTask));
    }

    public void BecomeToOutsider()
    {
        RpcUpdateTaskState.Invoke(new Tuple<byte, TaskUpdateMessage>(player.PlayerId, TaskUpdateMessage.BecomeToOutsider));
    }

    public void BecomeToCrewmate()
    {
        RpcUpdateTaskState.Invoke(new Tuple<byte, TaskUpdateMessage>(player.PlayerId, TaskUpdateMessage.BecomeToCrewmate));
    }

    public void WaiveAndBecomeToCrewmate()
    {
        Quota = 0;
        IsCrewmateTask = true;
        RpcSyncTaskState.Invoke(this);
    }

    public void WaiveAllTasksAsOutsider()
    {
        IsCrewmateTask = false;
        ReplaceTasks(0);
    }

    //指定の個数だけタスクを免除します。
    public void ExemptTasks(int tasks)
    {
        CurrentTasks -= tasks;
        TotalTasks-= tasks;
        Quota -= tasks;
        RpcSyncTaskState.Invoke(this);
    }

    //いま保持しているタスクを新たなものに切り替えます。
    public void ReplaceTasks(int tasks)
    {
        TotalTasks -= CurrentTasks;
        Quota -= CurrentTasks;
        TotalCompleted -= CurrentCompleted;
        
        CurrentTasks = tasks;
        CurrentCompleted = 0;
        TotalTasks += tasks;
        Quota += tasks;
        RpcSyncTaskState.Invoke(this);
    }

    public void GainExtraTasks(int tasks,bool addQuota = false)
    {
        TotalTasks += tasks;
        CurrentTasks = tasks;
        CurrentCompleted = 0;
        if (addQuota) Quota += tasks;
        RpcSyncTaskState.Invoke(this);
    }


    private static RemoteProcess<PlayerTaskState> RpcSyncTaskState = new RemoteProcess<PlayerTaskState>(
        "SyncTaskState",
        (writer, message) =>
        {
            writer.Write(message.player.PlayerId);
            writer.Write(message.CurrentTasks);
            writer.Write(message.CurrentCompleted);
            writer.Write(message.TotalTasks);
            writer.Write(message.TotalCompleted);
            writer.Write(message.Quota);
            writer.Write(message.IsCrewmateTask);
        },
        (reader) =>
        {
            var task = NebulaGameManager.Instance?.GetModPlayerInfo(reader.ReadByte())?.Tasks;
            if (task != null)
            {
                task.CurrentTasks = reader.ReadInt32();
                task.CurrentCompleted = reader.ReadInt32();
                task.CurrentTasks = reader.ReadInt32();
                task.TotalTasks = reader.ReadInt32();
                task.TotalCompleted = reader.ReadInt32();
                task.Quota = reader.ReadInt32();
                task.IsCrewmateTask = reader.ReadBoolean();
            }
            return task;
        },
        (message, isCalledByMe) => NebulaGameManager.Instance?.OnTaskUpdated()
        );

    private enum TaskUpdateMessage
    {
        CompleteTask,
        BecomeToCrewmate,
        BecomeToOutsider,
        WaiveTasks
    }

    private static RemoteProcess<Tuple<byte, TaskUpdateMessage>> RpcUpdateTaskState = new RemoteProcess<Tuple<byte, TaskUpdateMessage>>(
        "UpdateTaskState",
        (writer, message) =>
        {
            writer.Write(message.Item1);
            writer.Write((int)message.Item2);
        },
        (reader) =>
        {
            return new Tuple<byte, TaskUpdateMessage>(reader.ReadByte(), (TaskUpdateMessage)reader.ReadInt32());
        },
        (message, isCalledByMe) => {
            var task = NebulaGameManager.Instance?.GetModPlayerInfo(message.Item1)?.Tasks;
            if (task != null)
            {
                switch (message.Item2)
                {
                    case TaskUpdateMessage.CompleteTask:
                        task.CurrentCompleted++;
                        task.TotalCompleted++;
                        break;
                    case TaskUpdateMessage.BecomeToCrewmate:
                        task.IsCrewmateTask = true;
                        break;
                    case TaskUpdateMessage.BecomeToOutsider:
                        task.IsCrewmateTask = false;
                        break;
                    case TaskUpdateMessage.WaiveTasks:
                        task.Quota = 0;
                        break;
                }
            }
            NebulaGameManager.Instance?.OnTaskUpdated();
        }
        );    
}

[NebulaRPCHolder]
public class PlayerModInfo
{
    public class OutfitCandidate
    {
        public string Tag { get; private set; }
        public int priority { get; private set; }
        public GameData.PlayerOutfit outfit { get; private set; }

        public OutfitCandidate(string Tag,int priority,GameData.PlayerOutfit outfit)
        {
            this.Tag = Tag;
            this.priority = priority;
            this.outfit = outfit;
        }
    }

    public PlayerControl MyPlayer { get; private set; }
    public byte PlayerId { get; private set; }
    public bool AmOwner { get; private set; }
    public RoleInstance? Role => myRole;
    private RoleInstance? myRole = null;
    private RoleInstance? myGhostRole = null;
    private List<OutfitCandidate> outfit = new List<OutfitCandidate>();
    private TMPro.TextMeshPro roleText;

    public PlayerTaskState Tasks { get; set; }
    
    public IEnumerable<AssignableInstance> AllAssigned()
    {
        if (Role != null) yield return Role;

        //TODO Modifierも全て返すようにする
    }

    public void RoleAction(Action<AssignableInstance> action)
    {
        foreach (var role in AllAssigned()) action(role);
    }

    public PlayerModInfo(PlayerControl myPlayer)
    {
        this.MyPlayer = myPlayer;
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
    public GameData.PlayerOutfit CurrentOutfit => outfit.Count>0 ? outfit[0].outfit : DefaultOutfit;

    public void UpdateNameText(TMPro.TextMeshPro nameText)
    {
        var text = CurrentOutfit.PlayerName;
        var color = Color.white;

        var viewerInfo = PlayerControl.LocalPlayer.GetModInfo();

        viewerInfo?.Role?.DecoratePlayerName(this, ref text, ref color);
        nameText.text = text;
        nameText.color = color;
    }

    static Color fakeTaskColor = new Color(0x86 / 255f, 0x86 / 255f, 0x86 / 255f);
    static Color crewTaskColor = new Color(0xFA / 255f, 0xD9 / 255f, 0x34 / 255f);
    public void UpdateRoleText(TMPro.TextMeshPro roleText) {
        
        string text = myRole?.DisplayRoleName ?? "Undefined";

        if (myRole.HasAnyTasks)
            text += (" (" + Tasks.ToString(NebulaGameManager.Instance.CanSeeAllInfo || !Helpers.InCommSab) + ")").Color(myRole.HasCrewmateTasks ? crewTaskColor : fakeTaskColor);
        
        roleText.text = text;

        roleText.gameObject.SetActive(NebulaGameManager.Instance.CanSeeAllInfo || AmOwner);
    }

    private void SetRole(AbstractRole role, int[]? arguments)
    {
        myRole?.Inactivate();

        if (role.RoleCategory == Roles.RoleCategory.ImpostorRole)
            DestroyableSingleton<RoleManager>.Instance.SetRole(MyPlayer, RoleTypes.Impostor);
        else
            DestroyableSingleton<RoleManager>.Instance.SetRole(MyPlayer, RoleTypes.Crewmate);
        
        myRole = role.CreateInstance(MyPlayer,arguments);
        myRole.OnActivated();

        NebulaGameManager.Instance?.CheckGameState();
    }

    public class SetRoleMessage
    {
        public byte playerId;
        public int roleId;
        public int[]? arguments;
    }

    public readonly static RemoteProcess<SetRoleMessage> RpcSetRole = new RemoteProcess<SetRoleMessage>(
        "SetRole",
        (writer, message) =>
        {
            writer.Write(message.playerId);
            writer.Write(message.roleId);
            writer.Write(message.arguments?.Length ?? 0);
            for (int i = 0; i < (message.arguments?.Length ?? 0); i++) writer.Write(message.arguments![i]);
        },
        (reader) =>
        {
            SetRoleMessage message = new SetRoleMessage();
            message.playerId = reader.ReadByte();
            message.roleId = reader.ReadInt32();
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
            NebulaGameManager.Instance!.RegisterPlayer(PlayerControl.AllPlayerControls.Find((Il2CppSystem.Predicate<PlayerControl>)(p => p.PlayerId == message.playerId)))!.SetRole(Roles.Roles.AllRoles[message.roleId], message.arguments);
        }
        );
    
    public void Update()
    {
        UpdateNameText(MyPlayer.cosmetics.nameText);
        UpdateRoleText(roleText);

        RoleAction((role) => {
            role.Update();
            if (MyPlayer.AmOwner) role.LocalUpdate();
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
