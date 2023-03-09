namespace Nebula.Roles.ExtraRoles;

public class AvengerTarget : ExtraRole
{
    static public Color RoleColor = Palette.ImpostorRed;
    static public Color TargetColor = new Color(100f / 255f, 100f / 255f, 100f / 255f);

    public override RelatedExtraRoleData[] RelatedExtraRoleDataInfo { get => new RelatedExtraRoleData[] { new RelatedExtraRoleData("Target Lovers Identifer", this, 0, 6) }; }

    public override Assignable AssignableOnHelp { get => Roles.Avenger.canKnowExistenceOfAvengerOption.getSelection() != 0 ? this : null; }

    /* 矢印 */
    private Arrow Arrow;
    private float noticeInterval = 0f;
    private Vector2 noticePos = Vector2.zero;

    public override void Initialize(PlayerControl __instance)
    {
        base.Initialize(__instance);

        if (Roles.Avenger.showFlashForMurdererOption.getBool())
        {
            Helpers.PlayFlash(Roles.Avenger.Color);
        }
    }

    public override void MyPlayerControlUpdate()
    {
        if (!Roles.Avenger.murderCanKnowAvengerOption.getBool()) return;

        var myGData = Game.GameData.data.myData.getGlobalData();

        bool aliveFlag = false;
        foreach (var data in Game.GameData.data.AllPlayers.Values)
        {
            if (data.GetExtraRoleData(Roles.Lover) == myGData.GetExtraRoleData(Roles.AvengerTarget))
            {
                if (data.IsAlive)
                {
                    aliveFlag = true;

                    if (Helpers.playerById(data.id) != null)
                    {
                        if (Arrow == null)
                        {
                            Arrow = new Arrow(TargetColor,true,Roles.Avenger.arrowSprite.GetSprite());
                            Arrow.arrow.SetActive(true);
                            noticeInterval = 0f;
                        }
                        noticeInterval -= Time.deltaTime;

                        if (noticeInterval < 0f)
                        {
                            noticePos = Helpers.playerById(data.id).transform.position;
                            noticeInterval = Roles.Avenger.murderNoticeIntervalOption.getFloat();
                            Arrow.arrow.SetActive(true);
                        }

                        Arrow.Update(noticePos);
                    }
                    break;
                }
            }

        }
        if (!aliveFlag && Arrow != null)
        {
            UnityEngine.Object.Destroy(Arrow.arrow);
            Arrow = null;
        }
    }

    public override void CleanUp()
    {
        if (Arrow != null)
        {
            UnityEngine.Object.Destroy(Arrow.arrow);
            Arrow = null;
        }
    }

    public override void OnMeetingEnd()
    {
        base.OnMeetingEnd();
        if (Arrow != null) Arrow.arrow.SetActive(false);
        noticeInterval = Roles.Avenger.murderNoticeIntervalOption.getFloat();
    }

    public override void OnExiledPre(byte[] voters)
    {
        OnDied();
    }

    public PlayerControl? GetAvenger(byte playerId)
    {
        PlayerControl? avenger = null;
        var data = Helpers.allPlayersById()[playerId].GetModData();
        foreach (var player in Helpers.allPlayersById().Values)
        {
            if (player.Data.IsDead) continue;
            if (player.GetModData().role != Roles.Avenger) continue;
            if (player.GetModData().GetRoleData(Roles.Avenger.loversId) != (int)data.GetExtraRoleData(this)) continue;

            avenger = player;
            break;
        }
        return avenger;
    }
    public bool CheckAvengersMission(byte playerId)
    {
        PlayerControl? avenger = GetAvenger(playerId);

        if (avenger == null) return false;

        byte murder = byte.MaxValue;
        if (Game.GameData.data.deadPlayers.ContainsKey(playerId))
        {
            murder = Game.GameData.data.deadPlayers[playerId].MurderId;
        }

        if (murder == avenger.PlayerId)
        {
            //Avengerの目標を達成させる それぞれローカルで反映させる
            RPCEvents.UpdateRoleData(avenger.PlayerId, Roles.Avenger.avengerCheckerId, 1);
            return true;
        }
        return false;
    }

    public override void OnDied(byte playerId)
    {
        CheckAvengersMission(playerId);
    }

    public override void OnDied()
    {
        PlayerControl? avenger = GetAvenger(PlayerControl.LocalPlayer.PlayerId);

        if (avenger == null) return;

        byte murder = byte.MaxValue;
        if (Game.GameData.data.deadPlayers.ContainsKey(PlayerControl.LocalPlayer.PlayerId))
        {
            murder = Game.GameData.data.deadPlayers[PlayerControl.LocalPlayer.PlayerId].MurderId;
        }

        if (murder != avenger.PlayerId)
        {
            //標的を失ったAvengerを自殺させる
            if (MeetingHud.Instance || ExileController.Instance)
            {
                if (Game.GameData.data.myData.getGlobalData().Status == Game.PlayerData.PlayerStatus.Guessed ||
                       Game.GameData.data.myData.getGlobalData().Status == Game.PlayerData.PlayerStatus.Misguessed)
                    RPCEventInvoker.CloseUpKill(avenger, avenger, Game.PlayerData.PlayerStatus.Suicide);
                else
                    RPCEventInvoker.UncheckedExilePlayer(avenger.PlayerId, Game.PlayerData.PlayerStatus.Suicide.Id);
            }
            else
            {
                RPCEventInvoker.UncheckedMurderPlayer(avenger.PlayerId, avenger.PlayerId, Game.PlayerData.PlayerStatus.Suicide.Id, false);
            }
        }
    }

    public AvengerTarget() : base("AvengerTarget", "avengerTarget", RoleColor, 0)
    {
        IsHideRole = true;

        Arrow = null;
    }
}