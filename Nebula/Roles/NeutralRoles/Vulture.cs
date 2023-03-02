namespace Nebula.Roles.NeutralRoles;

public class Vulture : Role, Template.HasWinTrigger
{
    /* 陣営色 */
    static public Color RoleColor = new Color(139f / 255f, 69f / 255f, 18f / 255f);

    /* オプション */
    private Module.CustomOption eatOption;
    private Module.CustomOption eatCoolDownOption;
    private Module.CustomOption ventCoolDownOption;
    private Module.CustomOption ventDurationOption;

    public override void LoadOptionData()
    {
        eatOption = CreateOption(Color.white, "eatenCountNeeded", 3f, 1f, 5f, 1f);
        eatCoolDownOption = CreateOption(Color.white, "eatCoolDown", 10f, 5f, 40f, 2.5f);
        eatCoolDownOption.suffix = "second";
        ventCoolDownOption = CreateOption(Color.white, "ventCoolDown", 20f, 5f, 60f, 2.5f);
        ventCoolDownOption.suffix = "second";
        ventDurationOption = CreateOption(Color.white, "ventDuration", 10f, 5f, 60f, 2.5f);
        ventDurationOption.suffix = "second";
    }


    public bool WinTrigger { get; set; } = false;
    public byte Winner { get; set; } = Byte.MaxValue;

    public override void OnUpdateRoleData(int dataId, int newData)
    {
        if (dataId != eatLeftId) return;
        if (newData <= 0) RPCEventInvoker.WinTrigger(this);
    }

    /* ボタン */
    static private CustomButton eatButton;
    public override void ButtonInitialize(HudManager __instance)
    {
        if (eatButton != null)
        {
            eatButton.Destroy();
        }
        eatButton = new CustomButton(
            () =>
            {
                byte targetId = deadBodyId;

                RPCEventInvoker.CleanDeadBody(targetId);

                RPCEventInvoker.AddAndUpdateRoleData(PlayerControl.LocalPlayer.PlayerId, eatLeftId, -1);
                eatButton.UsesText.text = (Game.GameData.data.myData.getGlobalData().GetRoleData(eatLeftId)).ToString();
                eatButton.Timer = eatButton.MaxTimer;
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () => { return deadBodyId != Byte.MaxValue && PlayerControl.LocalPlayer.CanMove; },
            () => { eatButton.Timer = eatButton.MaxTimer; },
            eatButtonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            "button.label.eat"
        ).SetTimer(CustomOptionHolder.InitialAbilityCoolDownOption.getFloat());
        eatButton.UsesText.text = (Game.GameData.data.myData.getGlobalData().GetRoleData(eatLeftId)).ToString();
        eatButton.SetUsesIcon(3);
        eatButton.MaxTimer = eatCoolDownOption.getFloat();
    }

    /* 矢印 */
    Dictionary<byte, Arrow> Arrows;

    public byte deadBodyId;

    public int eatLeftId;
    public override RelatedRoleData[] RelatedRoleDataInfo { get => new RelatedRoleData[] { new RelatedRoleData(eatLeftId, "Eat Left", 0, 20) }; }

    /* 画像 */
    private SpriteLoader eatButtonSprite = new SpriteLoader("Nebula.Resources.EatButton.png", 115f);

    public override HelpSprite[] helpSprite => new HelpSprite[]
    {
            new HelpSprite(eatButtonSprite,"role.vulture.help.eat",0.3f)
    };

    SpriteLoader arrowSprite = new SpriteLoader("role.vulture.arrow");

    public override void MyPlayerControlUpdate()
    {
        if (PlayerControl.LocalPlayer.Data.IsDead) return;

        /* 捕食対象の探索 */

        {
            DeadBody body = Patches.PlayerControlPatch.SetMyDeadTarget();
            if (body)
            {
                deadBodyId = body.ParentId;
            }
            else
            {
                deadBodyId = byte.MaxValue;
            }
            Patches.PlayerControlPatch.SetDeadBodyOutline(body, Color.yellow);
        }

        /* 矢印の更新 */

        if (PlayerControl.LocalPlayer.Data.IsDead)
        {
            if (Arrows.Count > 0) ClearArrows();
        }
        else
        {
            DeadBody[] deadBodys = Helpers.AllDeadBodies();
            //削除するキーをリストアップする
            var removeList = Arrows.Where(entry =>
            {
                foreach (DeadBody body in deadBodys)
                {
                    if (body.ParentId == entry.Key) return false;
                }
                return true;
            });
            foreach (var entry in removeList)
            {
                UnityEngine.Object.Destroy(entry.Value.arrow);
                Arrows.Remove(entry.Key);
            }
            //残った矢印を最新の状態へ更新する
            foreach (DeadBody body in deadBodys)
            {
                if (!Arrows.ContainsKey(body.ParentId))
                {
                    Arrows[body.ParentId] = new Arrow(Color.blue,true,arrowSprite.GetSprite());
                    Arrows[body.ParentId].arrow.SetActive(true);
                }
                Arrows[body.ParentId].Update(body.transform.position);
            }
        }
    }

    private void ClearArrows()
    {
        //矢印を消す
        foreach (Arrow arrow in Arrows.Values)
        {
            UnityEngine.Object.Destroy(arrow.arrow);
        }
        Arrows.Clear();
    }

    public override void OnDied()
    {
        ClearArrows();
    }

    public override void OnMeetingEnd()
    {
        ClearArrows();
    }

    public override void Initialize(PlayerControl __instance)
    {
        Arrows = new Dictionary<byte, Arrow>();
        WinTrigger = false;

        VentCoolDownMaxTimer = ventCoolDownOption.getFloat();
        VentDurationMaxTimer = ventDurationOption.getFloat();
    }

    public override void GlobalInitialize(PlayerControl __instance)
    {
        __instance.GetModData().SetRoleData(eatLeftId, (int)eatOption.getFloat());
    }

    public override void CleanUp()
    {
        ClearArrows();

        if (eatButton != null)
        {
            eatButton.Destroy();
            eatButton = null;
        }

        WinTrigger = false;
    }

    public Vulture()
        : base("Vulture", "vulture", RoleColor, RoleCategory.Neutral, Side.Vulture, Side.Vulture,
             new HashSet<Side>() { Side.Vulture }, new HashSet<Side>() { Side.Vulture },
             new HashSet<Patches.EndCondition>() { Patches.EndCondition.VultureWin },
             true, VentPermission.CanUseLimittedVent, true, true, true)
    {
        eatButton = null;

        eatLeftId = Game.GameData.RegisterRoleDataId("vulture.eatLeft");

        Patches.EndCondition.VultureWin.TriggerRole = this;
    }
}
