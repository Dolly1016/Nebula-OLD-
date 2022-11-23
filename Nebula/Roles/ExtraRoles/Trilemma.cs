using Nebula.Patches;

namespace Nebula.Roles.ExtraRoles;
public class Trilemma : ExtraRole
{

    public override RelatedExtraRoleData[] RelatedExtraRoleDataInfo { get => new RelatedExtraRoleData[] { new RelatedExtraRoleData("Lovers Identifer", this, 1, 5) }; }

    public Game.PlayerData[] GetLoversData(Game.PlayerData player)
    {
        Game.PlayerData[] result = new Game.PlayerData[3];
        int index = 0;

        ulong myLoverId = player.GetExtraRoleData(this);
        PlayerControl target;
        foreach (Game.PlayerData data in Game.GameData.data.AllPlayers.Values)
        {
            if (!data.extraRole.Contains(this)) continue;
            if (data.GetExtraRoleData(this) == myLoverId)
            {
                result[index] = data;
                index++;
            }
        }

        return result;
    }


    private void ActionForLover(PlayerControl player, System.Action<PlayerControl> action)
    {
        ulong myLoverId = player.GetModData().GetExtraRoleData(this);
        PlayerControl target;
        foreach (Game.PlayerData data in Game.GameData.data.AllPlayers.Values)
        {
            if (!data.extraRole.Contains(this)) continue;
            if (data.GetExtraRoleData(this) == myLoverId)
            {
                target = Helpers.playerById(data.id);

                if (target != null)
                {
                    //指定の方法で自殺する
                    action.Invoke(target);
                }
            }
        }
    }

    private void ActionForMyLover(System.Action<PlayerControl> action)
    {
        ActionForLover(PlayerControl.LocalPlayer, (player) =>
        {
            //自身であれば特に何もしない
            if (player == PlayerControl.LocalPlayer) return;

            action.Invoke(player);
        });
    }

    public override void OnExiledPre(byte[] voters)
    {
        //自殺側はなにもしない
        if (Game.GameData.data.myData.getGlobalData().Status == Game.PlayerData.PlayerStatus.Suicide) return;

        ActionForMyLover((player) =>
        {
            if (!player.Data.IsDead) RPCEventInvoker.UncheckedExilePlayer(player.PlayerId, Game.PlayerData.PlayerStatus.Suicide.Id);
        }
        );
    }

    public override void OnMurdered(byte murderId)
    {
        //自殺側はなにもしない
        if (Game.GameData.data.myData.getGlobalData().Status == Game.PlayerData.PlayerStatus.Suicide) return;

        ActionForMyLover((player) =>
        {
            if (!player.Data.IsDead) RPCEventInvoker.UncheckedMurderPlayer(player.PlayerId, player.PlayerId, Game.PlayerData.PlayerStatus.Suicide.Id, false);
        }
        );
    }

    //上記で殺しきれない場合
    public override void OnDied()
    {
        //自殺側はなにもしない
        if (Game.GameData.data.myData.getGlobalData().Status == Game.PlayerData.PlayerStatus.Suicide) return;

        ActionForMyLover((player) =>
        {
            if (!player.Data.IsDead) RPCEventInvoker.CloseUpKill(player, player, Game.PlayerData.PlayerStatus.Suicide);
        }
        );
    }

    public override void EditDisplayName(byte playerId, ref string displayName, bool hideFlag)
    {
        bool showFlag = false;
        if (Game.GameData.data.myData.CanSeeEveryoneInfo) showFlag = true;
        else if (Game.GameData.data.myData.getGlobalData().extraRole.Contains(this))
        {
            ulong pairId = Game.GameData.data.myData.getGlobalData().GetExtraRoleData(this);
            if (Game.GameData.data.AllPlayers[playerId].GetExtraRoleData(this) == pairId) showFlag = true;
        }

        if (showFlag) EditDisplayNameForcely(playerId, ref displayName);
    }

    public override void EditDisplayNameForcely(byte playerId, ref string displayName)
    {
        try
        {
            displayName += Helpers.cs(
                    Lover.iconColor[Game.GameData.data.AllPlayers[playerId].GetExtraRoleData(this) - 1], "♠");
        }
        catch (Exception e)
        {
            displayName += Helpers.cs(
                    Lover.iconColor[0], "♠");
        }
    }

    public override void EditDescriptionString(ref string description)
    {
        string partner = "";
        ActionForMyLover((player) =>
        {
            partner = player.name;
        });
        partner = Helpers.cs(Color, partner);
        description += "\n" + Language.Language.GetString("role.lover.description").Replace("%NAME%", partner);
    }

    public override bool CheckAdditionalWin(PlayerControl player, EndCondition condition)
    {
        if (player.Data.IsDead) return false;

        return condition == EndCondition.TrilemmaWin;
    }

    public override bool HasCrewmateTask(byte playerId)
    {
        return false;
    }

    public Trilemma() : base("Trilemma", "trilemma", Lover.iconColor[0], 0)
    {
        Allocation = AllocationType.None;
        IsHideRole = true;
    }
}