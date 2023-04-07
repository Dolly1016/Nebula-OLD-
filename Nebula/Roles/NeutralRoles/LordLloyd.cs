using Nebula.Module;
using Nebula.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityEngine.GraphicsBuffer;

namespace Nebula.Roles.NeutralRoles;

public class LordLloyd : Role, Template.HasWinTrigger
{
    static public Color RoleColor = new Color(135f / 255f, 135f / 255f, 153f / 255f);

    public CustomOption CanWinWithFollowers;
    public CustomOption RequiredKillingToWin;
    public CustomOption AbetCoolDown;
    public CustomOption KillCoolDown;
    public CustomOption KillStreakCoolDown;

    static int killingDataId;

    ModAbilityButton LloydButton;
    SpriteLoader lloydButtonSprite = new("Nebula.Resources.LloydButton.png", 115f);

    public bool WinTrigger { get; set; } = false;
    public byte Winner { get; set; } = Byte.MaxValue;

    public override void LoadOptionData()
    {
        base.LoadOptionData();

        CanWinWithFollowers = CreateOption(Color.white, "canWinWithFollowers", false);
        RequiredKillingToWin = CreateOption(Color.white, "requiredVictimsToWin", 2, 0, 10, 1);

        KillCoolDown = CreateOption(Color.white, "killCoolDown", 15f, 5f, 30f, 2.5f);
        KillStreakCoolDown = CreateOption(Color.white, "killStreakCoolDown", 20f, 10f, 60f, 2.5f);
        AbetCoolDown = CreateOption(Color.white, "abetCoolDown", 10f,5f,30f,2.5f);
    }

    public override void GlobalInitialize(PlayerControl __instance)
    {
        WinTrigger = false;
    }

    public override int GetCustomRoleCount() => 1;


    class LloydKillAbilityAttribute : SimpleAbilityAttribute
    {
        private PlayerControl? target;
        private PlayerControl killer;
        private Action<ModAbilityButton> inactivateAction;
        public override void OnActivated(ModAbilityButton button)
        {
            base.OnActivated(button);
            target = null;
            button.SetLabelLocalized("button.label.assassinate").SetLabelType(ModAbilityButton.LabelType.Impostor);
            button.SetSprite(HudManager.Instance.KillButton.graphic.sprite);
        }
        public override void Update(ModAbilityButton button)
        {
            base.Update(button);
            target = Patches.PlayerControlPatch.SetMyTarget(GameManager.Instance.LogicOptions.GetKillDistance(),(p)=>true,killer);
        }
        public override bool IsEnabled() => base.IsEnabled() && !killer.Data.IsDead && target != null;
        

        public override void OnEndMeeting(ModAbilityButton button)
        {
            base.OnEndMeeting(button);
            inactivateAction.Invoke(button);
        }

        public void SetInactivateAction(Action<ModAbilityButton> action)
        {
            inactivateAction = action;
        }

        public LloydKillAbilityAttribute(PlayerControl killer,float killCoolDown,float killStreakCoolDown) : base(killStreakCoolDown,killCoolDown,
            new SimpleButtonEvent((button) => {
                if(button.MyAttribute is LloydKillAbilityAttribute attr)
                {
                    var r = Helpers.checkMuderAttemptAndKill(attr.killer!, attr.target!, Game.PlayerData.PlayerStatus.Assassinated, false, false);
                    if (r == Helpers.MurderAttemptResult.PerformKill && Constants.ShouldPlaySfx())
                    {
                        if (RPCEventInvoker.AddAndUpdateRoleData(PlayerControl.LocalPlayer.PlayerId, killingDataId, 1) >= (int)Roles.LordLloyd.RequiredKillingToWin.getFloat())
                            RPCEventInvoker.WinTrigger(Roles.LordLloyd);
                        SoundManager.Instance.PlaySound(PlayerControl.LocalPlayer.KillSfx, false, 0.8f);
                    }
                    attr.target = null;
                }                
            },Module.NebulaInputManager.modKillInput.keyCode,true)) {
            this.killer = killer;
        }
    }

    public override void ButtonInitialize(HudManager __instance)
    {
        base.ButtonInitialize(__instance);

        LloydButton = new ModAbilityButton(lloydButtonSprite.GetSprite(),Expansion.GridArrangeExpansion.GridArrangeParameter.AlternativeKillButtonContent)
            .SetLabelLocalized("button.label.lloyd");

        ModAbilityButton.IButtonAttribute abetAttribute = null;
        void inactivate(ModAbilityButton button)
        {
            button.MyAttribute = abetAttribute;
            button.UpperText.text = "";
            button.SetSprite(lloydButtonSprite.GetSprite());
            button.SetLabelLocalized("button.label.lloyd").SetLabelType(ModAbilityButton.LabelType.Standard);
        }

        abetAttribute = new InterpersonalAbilityAttribute(AbetCoolDown.getFloat(), AbetCoolDown.getFloat(),(p)=>true,Color.yellow,1f,
            new SimpleButtonEvent((button) => {
                PlayerControl target = Game.GameData.data!.myData.currentTarget!;
                RPCEventInvoker.AddExtraRole(target, Roles.LloydFollower, 0);
                LloydKillAbilityAttribute killAttribute = new LloydKillAbilityAttribute(target, KillCoolDown.getFloat(), KillStreakCoolDown.getFloat());
                killAttribute.SetInactivateAction(inactivate);
                button.UpperText.text = target.name;
                button.MyAttribute = killAttribute;
            }, Module.NebulaInputManager.abilityInput.keyCode));

        LloydButton.MyAttribute= abetAttribute;

        

        
    }

    public override void CleanUp()
    {
        base.CleanUp();

        LloydButton?.Destroy();
    }

    public LordLloyd()
        : base("LordLloyd", "lordLloyd", RoleColor, RoleCategory.Neutral, Side.LordLloyd, Side.LordLloyd,
             new HashSet<Side>() { Side.LordLloyd }, new HashSet<Side>() { Side.LordLloyd },
             new HashSet<Patches.EndCondition>(),
             true, VentPermission.CanUseLimittedVent, true, false, false)
    {
        VentColor = RoleColor;
        FixedRoleCount = true;

        killingDataId = Game.GameData.RegisterRoleDataId("lloyd.killing");
    }

}
