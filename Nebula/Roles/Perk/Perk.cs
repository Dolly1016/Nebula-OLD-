using Il2CppInterop.Generator.Passes;
using Il2CppSystem.Data;
using Nebula.Module;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Nebula.Roles.Perk.PerkHolder;

namespace Nebula.Roles.Perk;

public enum PerkPropertyType
{
    None=0,
    Percentage,
    Meter,
    Second
}

public interface IReleasable
{
    public int Id { get; }
    public bool IsAvailable { get; }

    public void UpdateReleaseStatus(int newStatus);

    public void ReleaseForcely();
}

public class DisplayPerk
{
    protected float[] ImportantProperties { get; set; } = new float[0];
    public float IP(int index) => ImportantProperties[index];
    public float IP(int index, PerkPropertyType propertyType)
    {
        float v = ImportantProperties[index];
        switch (propertyType)
        {
            case PerkPropertyType.Percentage:
                return v / 100f;
            case PerkPropertyType.Meter:
                return v / 2f;
            case PerkPropertyType.Second:
                return v;
        }
        return v;
    }


    public string LocalizedName { get; private set; }

    public virtual string DisplayName => "Undefined";
    
    public virtual string DisplayFlavor => "Undefined";


    public virtual bool IsNormalVisual => true;
    public int VisualFrontSpriteId { get; private set; }
    public int VisualBackSpriteId { get; private set; }
    public Color VisualBackSpriteColor { get; private set; }


    public bool IsCrewmatePerk { get; private set; }


    public DisplayPerk(string localizedName, bool isCrewmate,int frontSpriteId, int backSpriteId, Color backSpriteColor) {
        LocalizedName = localizedName;
        VisualFrontSpriteId = frontSpriteId;
        VisualBackSpriteId = backSpriteId;
        VisualBackSpriteColor = backSpriteColor;
        IsCrewmatePerk = isCrewmate;
    }
}

public class RolePerk : DisplayPerk, IReleasable
{
    public int Id { get; private set; }
    public Role RelatedRole { get; private set; }


    public override string DisplayName => Language.Language.GetString("perks.role." + LocalizedName + ".name");
    public override string DisplayFlavor
    {
        get
        {
            var result = Language.Language.GetString("perks.role." + LocalizedName + ".flavor").Replace("$ABILITY$", Helpers.cs(Color.red, Language.Language.GetString("perks.role." + LocalizedName + ".ability"))).Replace("$DC$", "\"");
            for (int i = 0; i < ImportantProperties.Length; i++)
                result = result.Replace("$PROP" + i + "$", Helpers.cs(Color.yellow, ImportantProperties[i].ToString()));
            return result;
        }
    }

    private IntegerDataEntry StatusEntry;
    public bool IsAvailable => true;// StatusEntry.Value >= 1;

    public void UpdateReleaseStatus(int newStatus)
    {
        bool lastAvailableState = IsAvailable;
        if (lastAvailableState) return;

        StatusEntry.Value = newStatus;
        if (!lastAvailableState && IsAvailable)
        {
            //新しく解放された場合
        }
    }

    public void ReleaseForcely()
    {
        UpdateReleaseStatus(10000);
    }

    static private IntegerDataEntry?[] equipedRolePerkEntry = new IntegerDataEntry?[2];
    static private void LoadEquipedRolePerkEntry(int index)
    {
        if (equipedRolePerkEntry[index] == null) equipedRolePerkEntry[index] = new IntegerDataEntry("perks.role.equiped" + index, PerkSaver.PerkDataSaver, -1);
    }

    public RolePerk(int id, string localizedName, Role relatedRole, int frontSpriteId, int backSpriteId, Color backSpriteColor, float[]? importantProperty = null)
        : base(localizedName,false,frontSpriteId, backSpriteId, backSpriteColor)
    {
        ImportantProperties = importantProperty ?? new float[0];

        id -= 4096;

        Id = id;

        this.RelatedRole= relatedRole;

        Perks.AllRolePerks[Id] = this;

        StatusEntry = new IntegerDataEntry("perk." + localizedName + ".status", PerkSaver.PerkDataSaver, 0);
    }
}

public class Perk : DisplayPerk, IReleasable
{
    public override string DisplayName => Language.Language.GetString("perks." + LocalizedName + ".name");
    public override string DisplayFlavor
    {
        get
        {
            string result = Language.Language.GetString("perks." + LocalizedName + ".flavor");
            for (int i = 0; i < ImportantProperties.Length; i++)
                result = result.Replace("$PROP" + i + "$", Helpers.cs(Color.yellow, ImportantProperties[i].ToString()));
            return result;
        }
    }

    private IntegerDataEntry StatusEntry;
    /// <summary>
    /// 解放済みかどうか調べる
    /// </summary>
    public virtual bool IsAvailable => StatusEntry.Value >= 1;
    /// <summary>
    /// パークの解放状況を更新する
    /// 既に解放済みの場合、何もしない
    /// </summary>
    /// <param name="newStatus"></param>
    public virtual void UpdateReleaseStatus(int newStatus)
    {
        bool lastAvailableState = IsAvailable;
        if (lastAvailableState) return;

        StatusEntry.Value = newStatus;
        if(!lastAvailableState && IsAvailable)
        {
            //新しく解放された場合
        }
    }
    /// <summary>
    /// 強制的にパークを解放する
    /// </summary>
    public void ReleaseForcely()
    {
        UpdateReleaseStatus(ReleaseStatus);
    }

    /// <summary>
    /// 強制的にパークを解放できる状態値
    /// </summary>
    protected virtual int ReleaseStatus => 1;




    /// <summary>
    /// 初期化します。
    /// </summary>
    /// <param name="perkData"></param>
    /// <param name="playerId"></param>
    public virtual void Initialize(PerkHolder.PerkInstance perkData, byte playerId) { }

    /// <summary>
    /// 初期化します。
    /// </summary>
    /// <param name="perkData"></param>
    /// <param name="playerId"></param>
    public virtual void GlobalInitialize(PerkHolder.PerkInstance perkData, byte playerId) { }

    /// <summary>
    /// 脅威範囲を設定します。
    /// 自身のパークのみ呼び出されます。
    /// </summary>
    public virtual void EditLocalIntimidation(PerkInstance perkData,ref float additional, ref float ratio) { }

    /// <summary>
    /// 脅威範囲を設定します。
    /// </summary>
    public virtual void EditGlobalIntimidation(PerkInstance perkData, ref float additional, ref float ratio) { }

    public virtual bool UnconditionalIntimidationGlobal(PerkInstance perkData) => false;
    public virtual bool UnconditionalIntimidationLocal(PerkInstance perkData) => false;

    /// <summary>
    /// タスクの効果を調整します。
    /// 自身のパークのみ呼び出されます。
    /// </summary>
    public virtual void OnCompleteHnSTaskLocal(PerkInstance perkData, ref float additional,ref float ratio){ }

    /// <summary>
    /// タスクの効果を調整します。
    /// </summary>
    public virtual void OnCompleteHnSTaskGlobal(PerkInstance perkData, byte playerId,ref float additional, ref float ratio) { }

    /// <summary>
    /// キルクールを調整します。
    /// 自身のパークのみ呼び出されます。
    /// </summary>
    public virtual void SetKillCoolDown(PerkInstance perkData, bool isSuccess, ref float additional, ref float ratio) { }

    /// <summary>
    /// プレイヤーがキルされたときに呼び出されます。
    /// 自身のパークのみ呼び出されます。
    /// </summary>
    public virtual void OnKillPlayer(PerkInstance perkData, byte targetId) { }

    /// <summary>
    /// キル失敗時の減速ペナルティの効果を調整します。
    /// 自身のパークのみ呼び出されます。
    /// </summary>
    public virtual void SetFailedKillPenalty(PerkInstance perkData, ref float speedAdditional, ref float speedRatio, ref float timeAdditional, ref float timeRatio) { }

    /// <summary>
    /// キル可能範囲を調整します。
    /// 自身のパークのみ呼び出されます。
    /// </summary>
    public virtual void SetKillRange(PerkInstance perkData, ref float additional, ref float ratio) { }

    /// <summary>
    /// 復活に要する時間を調整します。
    /// 自身のパークのみ呼び出されます。
    /// </summary>
    public virtual void SetReviveCost(PerkInstance perkData, ref float additional, ref float ratio) { }

    /// <summary>
    /// ゲーム開始時の復活チャージを設定します。
    /// </summary>
    /// <param name="perkData"></param>
    /// <param name="charge"></param>
    public virtual void SetReviveCharge(PerkInstance perkData, ref int charge) { }

    /// <summary>
    /// 復活した際に呼び出されます。
    /// 自身のパークのみ呼び出されます。
    /// </summary>
    public virtual void OnRevived(PerkInstance perkData) { }

    /// <summary>
    /// 通知間隔を調整します。
    /// </summary>
    public virtual void EditPingInterval(byte playerId, ref float additional, ref float ratio) { }

    /// <summary>
    /// 通知されるか否か返します。
    /// 全プレイヤー間で共通した動きをする必要がある点に注意してください。
    /// </summary>
    public virtual bool CanPing(PerkInstance perkData, byte playerId) { return true; }

    /// <summary>
    /// 誰かが死亡した際に呼び出されます。
    /// </summary>
    /// <param name="playerId"></param>
    public virtual void OneAnyoneDied(PerkHolder.PerkInstance perkData, byte playerId) { }

    public virtual void OnTaskComplete(PerkHolder.PerkInstance perkData, PlayerTask? task) { }

    

    public virtual void MyUpdate(PerkHolder.PerkInstance perkData) { }
    public virtual void MyControlUpdate(PerkHolder.PerkInstance perkData) { }
    public virtual void GlobalUpdate(PerkHolder.PerkInstance perkData) { }

    public virtual void ButtonInitialize(PerkInstance perkData,Action<Objects.CustomButton> buttonRegister) { }

    public int Id { get; private set; }
    public Perk(int id,string localizedName,bool isCrewmatePerk,int frontSpriteId,int backSpriteId,Color backSpriteColor)
        :base(localizedName,isCrewmatePerk,frontSpriteId, backSpriteId,backSpriteColor)
    {
        id -= 4096;

        Id = id;
        
        Perks.AllPerks[Id] = this;

        StatusEntry = new IntegerDataEntry("perk." + localizedName + ".status", PerkSaver.PerkDataSaver, 0);
    }

    static public IEnumerator CoProceedDisplayTimer(PerkDisplay? perkDisplay,float duration)
    {
        if (perkDisplay == null) yield break;

        float t = duration;
        float max = duration;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            perkDisplay.SetCool(t / max);
            yield return null;
        }
        perkDisplay.SetCool(0);
    }

}

public class Perks
{
    public static Dictionary<int, RolePerk> AllRolePerks = new Dictionary<int, RolePerk>();
    public static Dictionary<int, Perk> AllPerks = new Dictionary<int, Perk>();

    public static CrewmatePerks.Intuition Intuition = new CrewmatePerks.Intuition(0);
    public static CrewmatePerks.Tenacity Tenacity = new CrewmatePerks.Tenacity(1);
    public static CrewmatePerks.Lifeline Lifeline = new CrewmatePerks.Lifeline(2);
    public static CrewmatePerks.Cooperativeness Cooperativeness = new CrewmatePerks.Cooperativeness(3);
    public static CrewmatePerks.Patience Patience = new CrewmatePerks.Patience(4);
    public static CrewmatePerks.Sprint Sprint = new CrewmatePerks.Sprint(5);
    public static CrewmatePerks.Tempest Tempest = new CrewmatePerks.Tempest(6);
    public static CrewmatePerks.Mayday Mayday = new CrewmatePerks.Mayday(7);
    public static CrewmatePerks.SheriffGlance SheriffGlance = new CrewmatePerks.SheriffGlance(8);
    public static CrewmatePerks.TheWounded TheWounded = new CrewmatePerks.TheWounded(9);
    public static CrewmatePerks.GeniusInStorage GeniusInStorage = new CrewmatePerks.GeniusInStorage(10);
    public static CrewmatePerks.TSPResolver TSPResolver = new CrewmatePerks.TSPResolver(11);

    public static ImpostorPerks.Agitation Agitation = new ImpostorPerks.Agitation(1024);
    public static ImpostorPerks.Creeper Creeper = new ImpostorPerks.Creeper(1025);
    public static ImpostorPerks.BruteSabotage BruteSabotage = new ImpostorPerks.BruteSabotage(1026);
    public static ImpostorPerks.Insatiable Insatiable = new ImpostorPerks.Insatiable(1027);
    public static ImpostorPerks.WillNotMissYou WillNotMissYou = new ImpostorPerks.WillNotMissYou(1028);
    public static ImpostorPerks.ItWasCrewmate ItWasCrewmate = new ImpostorPerks.ItWasCrewmate(1029);
    public static ImpostorPerks.Debilitating Debilitating = new ImpostorPerks.Debilitating(1030);
    public static ImpostorPerks.DitherInDarkness DitherInDarkness = new ImpostorPerks.DitherInDarkness(1031);
    public static ImpostorPerks.Amnesia Amnesia = new ImpostorPerks.Amnesia(1032);
    public static ImpostorPerks.MurderersFame MurderersFame = new ImpostorPerks.MurderersFame(1033);
    public static ImpostorPerks.LimitlessAlarm LimitlessAlarm = new ImpostorPerks.LimitlessAlarm(1034);
    public static ImpostorPerks.DerelictCorpse DerelictCorpse = new ImpostorPerks.DerelictCorpse(1035);

    public static RolePerk RoleCleaner = new RolePerk(0, "cleaner", Roles.HnSCleaner, 33, 0, new Color(0.3f, 0.25f, 0.7f));
    public static RolePerk RoleHadar = new RolePerk(1, "hadar", Roles.HnSHadar, 37, 4, new Color(0.7f, 0.5f, 0.2f));
    public static RolePerk RoleRaider = new RolePerk(2, "raider", Roles.HnSRaider, 38, 7, new Color(0.8f, 0.1f, 0.1f), new float[] { 150, 50 });
    public static RolePerk RoleReaper = new RolePerk(3, "reaper", Roles.HnSReaper, 36, 6, new Color(0.6f, 0.05f, 0.2f));

}

public static class PerkSaver
{
    static private DataSaver? perkDataSaver = null;
    static public DataSaver PerkDataSaver
    {
        get
        {
            if (perkDataSaver == null) perkDataSaver = new("Perk.dat");
            return perkDataSaver;
        }
    }

    static private IntegerDataEntry?[] equipedPerkEntry = new IntegerDataEntry?[12];
    static private string GetCommonIndex(int index,bool isAbility,bool isCrewmate,out int commonIndex)
    {
        if (isAbility)
            commonIndex = 2 + index + (isCrewmate ? 5 : 0);
        else
            commonIndex = isCrewmate ? 1 : 0;



        return (isCrewmate ? "hider" : "seeker") + "." + (isAbility ? "ability" : "role") + "." + index;
    }

    static private void LoadEquipedPerkEntry(string commonIndexStr,int commonIndex)
    {
        if (equipedPerkEntry[commonIndex] == null) equipedPerkEntry[commonIndex] = new IntegerDataEntry("perks.equiped." + commonIndexStr, PerkDataSaver, -1);
    }

    static private int GetEquipedPerkId(string commonIndexStr, int commonIndex)
    {
        LoadEquipedPerkEntry(commonIndexStr, commonIndex);
        return equipedPerkEntry[commonIndex]!.Value;
    }

    static private void SetEquipedPerkId(string commonIndexStr, int commonIndex, int id)
    {
        LoadEquipedPerkEntry(commonIndexStr, commonIndex);
        equipedPerkEntry[commonIndex].Value = id;
    }

    static public RolePerk? GetEquipedRolePerk(int index,bool isCrewmate)
    {
        string commonStr = GetCommonIndex(index,false,isCrewmate,out int commonIdx);
        RolePerk? perk = null;
        Perks.AllRolePerks.TryGetValue(GetEquipedPerkId(commonStr, commonIdx), out perk);
        return perk;
    }

    static public void SetEquipedRolePerk(int index,bool isCrewmate, RolePerk? rolePerk)
    {
        string commonStr = GetCommonIndex(index, false, isCrewmate, out int commonIdx);
        SetEquipedPerkId(commonStr, commonIdx, rolePerk?.Id ?? -1);
    }

    static public Perk? GetEquipedAbilityPerk(int index, bool isCrewmate)
    {
        string commonStr = GetCommonIndex(index, true, isCrewmate, out int commonIdx);
        Perk? perk = null;
        Perks.AllPerks.TryGetValue(GetEquipedPerkId(commonStr, commonIdx), out perk);
        return perk;
    }

    static public void UnequipAbilityPerk(Perk? perk)
    {
        if (perk == null) return;
        for (int c = 0; c < 2; c++)
        {
            for (int i = 0; i < 5; i++) {
                string commonStr = GetCommonIndex(i, true, c == 0, out int commonIdx);
                if (GetEquipedPerkId(commonStr, commonIdx) == perk?.Id) SetEquipedPerkId(commonStr, commonIdx, -1);
            }
        }
    }

    static public void SetEquipedAbilityPerk(int index, bool isCrewmate, Perk? perk)
    {
        string commonStr = GetCommonIndex(index, true, isCrewmate, out int commonIdx);
        SetEquipedPerkId(commonStr, commonIdx, perk?.Id ?? -1);
    }

}