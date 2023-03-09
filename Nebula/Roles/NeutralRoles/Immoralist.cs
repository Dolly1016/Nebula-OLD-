using Nebula.Patches;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Roles.NeutralRoles
{
    public class Immoralist : Role
    {
        private Module.CustomOption showDeathFlashOption;
        private Module.CustomOption canKnowWhereIsDeadBodiesOption;
        private Module.CustomOption canFixLightsAndComms;
        public override bool CanFixSabotage { get { return canFixLightsAndComms.getBool(); } }

        public override void LoadOptionData()
        {
            TopOption.AddCustomPrerequisite(() => Roles.Spectre.IsSpawnable() && Roles.Spectre.spawnImmoralistOption.getBool());
            showDeathFlashOption = CreateOption(Color.white, "showDeathFlash", true);
            canKnowWhereIsDeadBodiesOption = CreateOption(Color.white, "canKnowWhereIsDeadBodies", true);

            canFixLightsAndComms = CreateOption(Color.white, "canFixLightsAndComms", false);

        }

        public override bool IsSpawnable()
        {
            return Roles.Spectre.IsSpawnable() && Roles.Spectre.spawnImmoralistOption.getBool();
        }

        Dictionary<byte, Arrow> Arrows;

        SpriteLoader arrowSprite = new SpriteLoader("role.immoralist.arrow");

        public override void MyPlayerControlUpdate()
        {
            if (PlayerControl.LocalPlayer.Data.IsDead)
            {
                if (Arrows.Count > 0) ClearArrows();
                return;
            }



            if (!canKnowWhereIsDeadBodiesOption.getBool()) return;

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
        }

        public override void OnAnyoneMurdered(byte murderId, byte targetId)
        {
            if (targetId == PlayerControl.LocalPlayer.PlayerId) return;

            if(showDeathFlashOption.getBool())Helpers.PlayFlash(Color);
        }

        private CustomButton suicideButton;
        private SpriteLoader buttonSprite = new SpriteLoader("Nebula.Resources.SuicideButton.png", 115f);

        public override void ButtonInitialize(HudManager __instance)
        {
            if (suicideButton != null)
            {
                suicideButton.Destroy();
            }
            suicideButton = new CustomButton(
                () =>
                {
                    RPCEventInvoker.UncheckedMurderPlayer(PlayerControl.LocalPlayer.PlayerId,PlayerControl.LocalPlayer.PlayerId,Game.PlayerData.PlayerStatus.Suicide.Id,false);
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () =>
                {
                    return PlayerControl.LocalPlayer.CanMove;
                },
                () => { suicideButton.Timer = 0; },
                buttonSprite.GetSprite(),
                Expansion.GridArrangeExpansion.GridArrangeParameter.None,
                __instance,
                Module.NebulaInputManager.abilityInput.keyCode,
                "button.label.suicide"
            ).SetTimer(CustomOptionHolder.InitialAbilityCoolDownOption.getFloat());
            suicideButton.MaxTimer = 0f;
        }

        public override void CleanUp()
        {
            ClearArrows();

            if (suicideButton != null)
            {
                suicideButton.Destroy();
                suicideButton = null;
            }
        }

        public override void EditDisplayNameColor(byte playerId, ref Color displayColor)
        {
            if (PlayerControl.LocalPlayer.GetModData().role == Roles.Spectre)
                displayColor = Color;
        }

        public Immoralist()
        : base("Immoralist", "immoralist", Spectre.RoleColor, RoleCategory.Neutral, Side.Spectre, Side.Spectre,
         new HashSet<Side>() { Side.Spectre }, new HashSet<Side>() { Side.Spectre },
         new HashSet<Patches.EndCondition>() { EndCondition.SpectreWin },
         true, VentPermission.CanNotUse, false, false, false)
        {
            CreateOptionFollowingRelatedRole = true;
            Allocation = AllocationType.None;
        }
    }
}
