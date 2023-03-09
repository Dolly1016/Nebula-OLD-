using Il2CppInterop.Generator.Passes;
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

public class Perk
{
    
    public string LocalizedName { get; private set; }
    protected float[] ImportantProperties { get; set; } = new float[0];
    protected float IP(int index) => ImportantProperties[index];
    protected float IP(int index, PerkPropertyType propertyType)
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
    public string DisplayName => Language.Language.GetString("perks." + LocalizedName + ".name");
    public string DisplayFlavor
    {
        get
        {
            string result = Language.Language.GetString("perks." + LocalizedName + ".flavor");
            for (int i = 0; i < ImportantProperties.Length; i++)
                result = result.Replace("$PROP" + i + "$", Helpers.cs(Color.yellow, ImportantProperties[i].ToString()));
            return result;
        }
    }

    

    public bool IsCrewmatePerk { get; private set; }
    
    public int VisualFrontSpriteId { get; private set; }
    public int VisualBackSpriteId { get; private set; }
    public Color VisualBackSpriteColor { get; private set; }

    static private DataSaver? perkDataSaver = null;
    static private DataSaver PerkDataSaver { get
        {
            if(perkDataSaver==null) perkDataSaver = new("Perk.dat");
            return perkDataSaver;
        } 
    }
    static private IntegerDataEntry?[] equipedPerkEntry = new IntegerDataEntry?[10];
    static private void LoadEquipedPerkEntry(int index) {
        if (equipedPerkEntry[index] == null) equipedPerkEntry[index] = new IntegerDataEntry("perks.equiped" + index, PerkDataSaver, -1);
    }

    static public Perk? GetEquipedPerk(int index)
    {
        if (index < 0 || index >= 10) return null;
        LoadEquipedPerkEntry(index);
        if (Perks.AllPerks.TryGetValue(equipedPerkEntry[index]!.Value, out var p)) return p;
        return null;
    }
    static public Perk? GetEquipedPerk(int index, bool isCrewmatePerk) => GetEquipedPerk(index + (isCrewmatePerk ? 0 : 5));

    static public void SetEquipedPerk(int index,Perk? perk)
    {
        LoadEquipedPerkEntry(index);
        equipedPerkEntry[index].Value = perk?.Id ?? -1;
    }
    static public void SetEquipedPerk(int index, bool isCrewmatePerk, Perk? perk) => SetEquipedPerk(index + (isCrewmatePerk ? 0 : 5), perk);

    static public bool IsSeekerPerkSlot(int index) => index >= 5;


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
    public virtual void EditGlobalIntimidation(byte playerId, ref float additional, ref float ratio) { }

    /// <summary>
    /// タスクの効果を調整します。
    /// 自身のパークのみ呼び出されます。
    /// </summary>
    public virtual void OnCompleteHnSTaskLocal(PerkInstance perkData, ref float additional,ref float ratio){ }

    /// <summary>
    /// タスクの効果を調整します。
    /// </summary>
    public virtual void OnCompleteHnSTaskGlobal(byte playerId, ref float additional, ref float ratio) { }

    /// <summary>
    /// キルクールを調整します。
    /// 自身のパークのみ呼び出されます。
    /// </summary>
    public virtual void SetKillCoolDown(PerkInstance perkData, bool isSuccess, ref float additional, ref float ratio) { }

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

    public virtual void OnTaskComplete(PerkHolder.PerkInstance perkData) { }

    public virtual void MyUpdate(PerkHolder.PerkInstance perkData) { }

    public int Id { get; private set; }
    public Perk(int id,string localizedName,bool isCrewmatePerk,int frontSpriteId,int backSpriteId,Color backSpriteColor)
    {
        id -= 4096;

        Id = id;
        
        Perks.AllPerks[Id] = this;

        LocalizedName = localizedName;

        StatusEntry = new IntegerDataEntry("perk." + localizedName + ".status", PerkDataSaver, 0);

        VisualFrontSpriteId = frontSpriteId;
        VisualBackSpriteId = backSpriteId;
        VisualBackSpriteColor = backSpriteColor;

        IsCrewmatePerk = isCrewmatePerk;
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
    public static Dictionary<int, Perk> AllPerks = new Dictionary<int, Perk>();

    public static CrewmatePerks.Intuition Intuition = new CrewmatePerks.Intuition(0);
    public static CrewmatePerks.Tenacity Tenacity = new CrewmatePerks.Tenacity(1);
    public static CrewmatePerks.Patience Patience = new CrewmatePerks.Patience(2);
    public static CrewmatePerks.SheriffGlance SheriffGlance = new CrewmatePerks.SheriffGlance(3);
    public static CrewmatePerks.GeniusInStorage GeniusInStorage = new CrewmatePerks.GeniusInStorage(4);
    public static CrewmatePerks.TSPResolver TSPResolver = new CrewmatePerks.TSPResolver(5);

    public static ImpostorPerks.Agitation Agitation = new ImpostorPerks.Agitation(1024);
    public static ImpostorPerks.Creeper Creeper = new ImpostorPerks.Creeper(1025);
    public static ImpostorPerks.Insatiable Insatiable = new ImpostorPerks.Insatiable(1026);
    public static ImpostorPerks.WillNotMissYou WillNotMissYou = new ImpostorPerks.WillNotMissYou(1027);
    public static ImpostorPerks.LimitlessAlarm LimitlessAlarm = new ImpostorPerks.LimitlessAlarm(1028);
    public static ImpostorPerks.DerelictCorpse DerelictCorpse = new ImpostorPerks.DerelictCorpse(1029);

}