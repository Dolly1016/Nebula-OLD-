using Iced.Intel;
using LibCpp2IL;
using Nebula.Expansion;
using Nebula.Objects;

namespace Nebula.Roles.Perk;

[NebulaRPCHolder]
public class PerkHolder : ExtraRole
{
    public struct SharePerkMessage
    {
        public byte playerId;
        public int[] perks;
        public int choicedSeekerRole;
    }

    public static RemoteProcess<SharePerkMessage> SharePerks = new RemoteProcess<SharePerkMessage>(
        "SharePerks",
            (writer, message) =>
            {
                writer.Write(message.playerId);
                writer.Write(message.choicedSeekerRole);
                writer.Write(message.perks.Length);
                for (int i = 0; i < message.perks.Length; i++) writer.Write(message.perks[i]);
            },
            (reader) =>
            {
                SharePerkMessage message = new SharePerkMessage();
                
                message.playerId = reader.ReadByte();

                message.choicedSeekerRole = reader.ReadInt32();

                int length = Mathf.Min(reader.ReadInt32(), (int)CustomOptionHolder.ValidPerksOption.getFloat());
                message.perks = new int[length];
                
                for(int i = 0; i < length; i++) message.perks[i]=reader.ReadInt32();
                return message;
            },
            (message, isCalledByMe) =>
            {
                if (message.perks.Length > (int)CustomOptionHolder.ValidPerksOption.getFloat()) message.perks = message.perks.SubArray(0, (int)CustomOptionHolder.ValidPerksOption.getFloat());

                PerkData.AllPerkData[message.playerId] = new PerkData(message.playerId,message.perks);

                if (Helpers.playerById(message.playerId).Data.Role.IsImpostor)
                {
                    //希望の役職にかえておく
                    Game.GameData.data.GetPlayerData(message.playerId).role = Role.GetRoleById((byte)message.choicedSeekerRole);
                }
            }
            );

    public struct PerkDataMessage<T>
    {
        public T value;
        public int dataIndex;
        public int perkIndex;
        public byte playerId;
    }

    public static RemoteProcess<PerkDataMessage<float>> ShareFloatPerkData = new RemoteProcess<PerkDataMessage<float>>(
        "SharePerkDataFloat",
            (writer, message) =>
            {
                writer.Write(message.playerId);
                writer.Write(message.perkIndex);
                writer.Write(message.dataIndex);
                writer.Write(message.value);
            },
            (reader) =>
            {
                PerkDataMessage<float> message = new PerkDataMessage<float>();
                message.playerId = reader.ReadByte();
                message.perkIndex = reader.ReadInt32();
                message.dataIndex = reader.ReadInt32();
                message.value = reader.ReadSingle();
                return message;
            },
            (message, isCalledByMe) =>
            {
                PerkData.AllPerkData[(byte)message.playerId].MyPerks[message.perkIndex].DataAry[message.dataIndex] = message.value;
            }
            );

    public static RemoteProcess<PerkDataMessage<int>> ShareIntegerPerkData = new RemoteProcess<PerkDataMessage<int>>(
        "SharePerkDataInteger",
            (writer, message) =>
            {
                writer.Write(message.playerId);
                writer.Write(message.perkIndex);
                writer.Write(message.dataIndex);
                writer.Write(message.value);
            },
            (reader) =>
            {
                PerkDataMessage<int> message = new PerkDataMessage<int>();
                message.playerId = reader.ReadByte();
                message.perkIndex = reader.ReadInt32();
                message.dataIndex = reader.ReadInt32();
                message.value = reader.ReadInt32();
                return message;
            },
            (message, isCalledByMe) =>
            {
                PerkData.AllPerkData[(byte)message.playerId].MyPerks[message.perkIndex].IntegerAry[message.dataIndex] = message.value;
            }
            );

    public class PerkInstance
    {
        public Perk Perk;
        public float[]? DataAry;
        public int[]? IntegerAry;
        public PerkDisplay? Display;
        public byte PlayerId { get; private set; }
        public int Index { get; private set; }

        public PerkInstance(byte playerId,int dataIndex,Perk perk,PerkDisplay? display=null)
        {
            this.Perk = perk;
            PlayerId = playerId;
            Index = dataIndex;
            DataAry = null;
            IntegerAry = null;
            this.Display = display;
        }

    }
    public class PerkData {
        static public Dictionary<byte, PerkData> AllPerkData = new();
        static private PerkData? myPerkData = null;
        static public PerkData MyPerkData
        {
            get
            {
                if (myPerkData == null) myPerkData = AllPerkData[PlayerControl.LocalPlayer.PlayerId];
                return myPerkData;
            }
        }

        public PerkInstance?[] MyPerks { get; private set; }
        public Perk? GetPerk(int index) => MyPerks[index]?.Perk ?? null;

        public byte PlayerId { get; private set; }

        public PerkData(byte playerId, int[] perks)
        {
            PlayerId = playerId;
            MyPerks = new PerkInstance[perks.Length];
            for (int i = 0; i < perks.Length; i++) {
                Perk? p = null;
                Perks.AllPerks.TryGetValue(perks[i], out p);
                MyPerks[i] = p != null ? new PerkInstance(playerId, i, p) : null;

                if (MyPerks[i] != null)
                {
                    MyPerks[i].Perk.GlobalInitialize(MyPerks[i], playerId);
                    if (playerId==PlayerControl.LocalPlayer.PlayerId)MyPerks[i].Perk.Initialize(MyPerks[i],playerId);
                }
            }
        }
        public void PerkAction(Action<PerkInstance> action)
        {
            for (int i = 0; i < MyPerks.Length; i++) if (MyPerks[i] != null) action.Invoke(MyPerks[i]!);
        }

        static public void GeneralPerkAction(Action<PerkInstance, byte> action)
        {
            foreach (var data in AllPerkData) data.Value.PerkAction((p) => action.Invoke(p, data.Value.PlayerId));
        }

        public static void InitializePerkData()
        {
            myPerkData = null;
            AllPerkData.Clear();
        }
    }

    PerkDisplay[] MyPerkDisplay;
    GameObject PerkDisplayHolder;
    List<Objects.CustomButton> CustomButtonList=new List<CustomButton>();
    


    public override void Assignment(Patches.AssignMap assignMap)
    {
        if ((Game.GameData.data.GameMode & Module.CustomGameMode.AllHnS) == 0) return;

        foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
        {
            assignMap.AssignExtraRole(p.PlayerId, id, 0);
        }
    }

    public override void Initialize(PlayerControl __instance)
    {
        MyPerkDisplay = new PerkDisplay[PerkData.MyPerkData.MyPerks.Length];
        PerkDisplayHolder = new GameObject("PerkDisplayHolder");
        GridArrangeExpansion.AddGridArrangeContent(PerkDisplayHolder,GridArrangeExpansion.GridArrangeParameter.LeftSideContent);

        for(int i = 0; i < MyPerkDisplay.Length; i++)
        {
            MyPerkDisplay[i] = new GameObject("PerkDisplay").AddComponent<PerkDisplay>();
            MyPerkDisplay[i].transform.SetParent(PerkDisplayHolder.transform);
            MyPerkDisplay[i].transform.localPosition = new Vector3(-0.5f + 0.32f * (float)i, i % 2 == 0 ? -0.18f : -0.45f, -1f);
            MyPerkDisplay[i].transform.localScale = Vector3.one * 0.3f;
            MyPerkDisplay[i].SetPerk(PerkData.MyPerkData.GetPerk(i));

            if(PerkData.MyPerkData.MyPerks[i]!=null) PerkData.MyPerkData.MyPerks[i].Display = MyPerkDisplay[i];
        }
    }

    public override void OnTaskComplete(PlayerTask? task)
    {
        PerkData.MyPerkData.PerkAction((p)=>p.Perk.OnTaskComplete(p,task));
    }

    public override void OnAnyoneDied(byte playerId)
    {
        PerkData.MyPerkData.PerkAction((p) => p.Perk.OneAnyoneDied(p, playerId));
    }

    public override void OnAnyoneMurdered(byte murderId, byte targetId)
    {
        if (PlayerControl.LocalPlayer.PlayerId == murderId)
        {
            PerkData.MyPerkData.PerkAction((p) => p.Perk.OnKillPlayer(p, targetId));
        }
    }

    public override void onRevived(byte playerId)
    {
        if (playerId == PlayerControl.LocalPlayer.PlayerId) PerkData.MyPerkData.PerkAction((p) => p.Perk.OnRevived(p));
    }

    public override void MyUpdate()
    {
        PerkData.MyPerkData.PerkAction((p) => p.Perk.MyUpdate(p));
        PerkData.GeneralPerkAction((p, id) => p.Perk.GlobalUpdate(p));
    }

    public override void MyPlayerControlUpdate()
    {
        PerkData.MyPerkData.PerkAction((p) => p.Perk.MyControlUpdate(p));
    }

    public override void CleanUp()
    {
        foreach(var button in CustomButtonList)
        {
            button.Destroy();
        }
        CustomButtonList.Clear();
    }

    public override void ButtonInitialize(HudManager __instance)
    {
        PerkData.MyPerkData.PerkAction((p) => p.Perk.ButtonInitialize(p, (b)=>RegisterButton(b)));
    }

    private void RegisterButton(Objects.CustomButton button)
    {
        button.SetHotKey(KeyCode.Alpha1 + CustomButtonList.Count);
        CustomButtonList.Add(button);
    }

    public PerkHolder() : base("PerkHolder", "perkHolder", Palette.White, 0)
    {
        ValidGamemode = Module.CustomGameMode.AllHnS;
        IsHideRole = true;
    }
}
