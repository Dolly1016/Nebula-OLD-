using BepInEx.Unity.IL2CPP.Utils;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Nebula.Patches;
using Nebula.Roles.Perk;
using static Nebula.Module.DynamicColors;

namespace Nebula.Roles.HnSImpostorRoles;

public class HnSRaider : Role
{
    public override bool ShowInHelpWindow => false;

    private CustomObject? lastAxe = null;
    private CustomObject? thrownAxe = null;
    private CustomButton killButton = null;
    private CustomButton reloadButton = null;
    private SpriteRenderer? guide;

    private int leftAxes;

    private SpriteLoader reloadButtonSprite = new SpriteLoader("Nebula.Resources.AxeButton.png", 115f, "ui.button.raider.reload");

    public override void Initialize(PlayerControl __instance)
    {
        thrownAxe = null;
        lastAxe = null;
    }

    public override void ButtonInitialize(HudManager __instance)
    {
        if (killButton != null) killButton.Destroy();
        killButton = new CustomButton(
            () =>
            {
                Objects.SoundPlayer.PlaySound(Module.AudioAsset.RaiderThrow);

                RPCEventInvoker.RaiderThrow(lastAxe.GameObject.transform.position, lastAxe.GameObject.transform.eulerAngles.z);
                thrownAxe = lastAxe;
                lastAxe = null;

                Vector2 killPos = PlayerControl.LocalPlayer.GetTruePosition() * 0.6f + (Vector2)thrownAxe.GameObject.transform.position * 0.4f;
                //周囲のプレイヤーをキルする
                ImpostorRoles.Raider.BeatenAroundAxe(killPos, 0.9f, true);

                float additional = 0f, ratio = Perks.RoleRaider.IP(0,PerkPropertyType.Percentage);
                Perk.PerkHolder.PerkData.MyPerkData.PerkAction((p) => p.Perk.SetKillCoolDown(p, true, ref additional, ref ratio));
                killButton.Timer = (2f + additional) * ratio;

                float sa = 0f, sr = Perks.RoleRaider.IP(1, PerkPropertyType.Percentage);
                float ta = 0f, tr = 0.5f;
                Perk.PerkHolder.PerkData.MyPerkData.PerkAction((p) => p.Perk.SetFailedKillPenalty(p, ref sa, ref sr, ref ta, ref tr));
                sr = Mathf.Min(0, sr);
                tr = Mathf.Min(0, tr);
                RPCEventInvoker.EmitSpeedFactor(PlayerControl.LocalPlayer, new Game.SpeedFactor(0, (killButton.Timer + ta) * tr, (0.25f + sa) * sr, false));

                leftAxes--;
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () => {
                killButton.UsesText.text = leftAxes.ToString();
                return PlayerControl.LocalPlayer.CanMove && !reloadButton.isEffectActive && lastAxe != null && lastAxe.Renderer.color.g > 0.5f;
            },
            () => { killButton.Timer = killButton.MaxTimer; },
            __instance.KillButton.graphic.sprite,
            Expansion.GridArrangeExpansion.GridArrangeParameter.AlternativeKillButtonContent,
            __instance,
            Module.NebulaInputManager.modKillInput.keyCode,
             "button.label.throw"
        ).SetTimer(5f);
        killButton.MaxTimer = GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.KillCooldown);
        killButton.FireOnClicked = true;
        killButton.SetButtonCoolDownOption(true);
        killButton.SetUsesIcon(1);

        IEnumerator CoReload(MonoBehaviour coroutineHolder, Vent vent)
        {
            PlayerControl p=PlayerControl.LocalPlayer;
            p.Collider.enabled = false;
            p.moveable = false;
            p.NetTransform.enabled = false;
            p.MyPhysics.inputHandler.enabled = true;
            yield return NebulaEffects.CoWait(p.MyPhysics.WalkPlayerTo(vent.transform.position + vent.Offset, 0.01f, 1f).WrapToManaged(), () => {
                if (reloadButton.isEffectActive) reloadButton.Timer = reloadButton.EffectDuration;
            });
            RPCEventInvoker.EmitSpeedFactor(p, new Game.SpeedFactor(0, reloadButton.EffectDuration, 0f, false));
            p.Collider.enabled = true;
            p.moveable = true;
            p.NetTransform.enabled = true;
            p.MyPhysics.inputHandler.enabled = true;
        }

        if (reloadButton != null) reloadButton.Destroy();
        reloadButton = new CustomButton(
            () => {
                PlayerControl.LocalPlayer.StartCoroutine(CoReload(PlayerControl.LocalPlayer, HudManager.Instance.ImpostorVentButton.currentTarget));
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () => { return PlayerControl.LocalPlayer.CanMove && HudManager.Instance.ImpostorVentButton.currentTarget != null; },
            () =>
            {
                reloadButton.Timer = reloadButton.MaxTimer;
                reloadButton.isEffectActive = false;
            },
            reloadButtonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            true,
            3f,
            () =>
            {
                leftAxes = 3;
            },
            "button.label.reload"
        ).SetTimer(0f);
        reloadButton.MaxTimer = 0f;
        leftAxes = 3;
    }

    public override void CleanUp()
    {
        if (lastAxe != null) RPCEventInvoker.RaiderSettleAxe();
        if (killButton != null) killButton.Destroy();
        if (guide != null) GameObject.Destroy(guide);

        killButton = null;
        guide = null;
        thrownAxe = null;
        lastAxe = null;
    }

    private SpriteLoader axeGuideSprite = new SpriteLoader("Nebula.Resources.RaiderAxeGuide.png", 100f);

    private IEnumerator GetGuideEnumrator()
    {
        if (guide == null)
        {
            GameObject obj = new GameObject();
            obj.name = "RaiderGuide";
            obj.transform.SetParent(PlayerControl.LocalPlayer.transform);
            guide = obj.AddComponent<SpriteRenderer>();
            guide.sprite = axeGuideSprite.GetSprite();
        }
        else
        {
            guide.gameObject.SetActive(true);
        }

        float counter = 0f;
        while (counter < 1f)
        {
            counter += Time.deltaTime;

            float angle = Game.GameData.data.myData.getGlobalData().MouseAngle;
            guide.transform.eulerAngles = new Vector3(0f, 0f, angle * 180f / Mathf.PI);
            guide.transform.localPosition = new Vector3(
                Mathf.Cos(angle) * (counter * 0.6f + 1.8f),
                Mathf.Sin(angle) * (counter * 0.6f + 1.8f),
                -1f);
            guide.color = new Color(1f, 1f, 1f, 0.9f - (counter * 0.9f));

            yield return null;

            if (lastAxe == null) break;
        }

        if (lastAxe != null) HudManager.Instance.StartCoroutine(GetGuideEnumrator().WrapToIl2Cpp());
        else guide.gameObject.SetActive(false);

        yield break;
    }


    public override void MyPlayerControlUpdate()
    {
        if (lastAxe == null && !(killButton.Timer > 0) && leftAxes>0)
        {
            lastAxe = RPCEventInvoker.ObjectInstantiate(Objects.ObjectTypes.RaidAxe.Axe, PlayerControl.LocalPlayer.transform.position);
            HudManager.Instance.StartCoroutine(GetGuideEnumrator().WrapToIl2Cpp());
        }

        if (lastAxe != null)
        {
            if (lastAxe.Data[0] == (int)Objects.ObjectTypes.RaidAxe.AxeState.Static)
            {
                Vector2 axeVec = (Vector2)lastAxe.GameObject.transform.position - (Vector2)PlayerControl.LocalPlayer.GetTruePosition();
                if (PhysicsHelpers.AnyNonTriggersBetween(PlayerControl.LocalPlayer.GetTruePosition(), axeVec.normalized, axeVec.magnitude, Constants.ShipAndObjectsMask))
                {
                    lastAxe.Renderer.color = Color.red;
                }
                else
                {
                    lastAxe.Renderer.color = Color.white;
                }
            }
        }

        if (thrownAxe != null)
        {
            if (!MeetingHud.Instance)
            {
                if (thrownAxe.Data[0] == (int)Objects.ObjectTypes.RaidAxe.AxeState.Thrown)
                {
                    ImpostorRoles.Raider.BeatenAroundAxe(thrownAxe.GameObject.transform.position, 0.5f, true);
                }
            }
        }

        if (lastAxe != null) RPCEventInvoker.UpdatePlayerControl();
        
    }

    public HnSRaider()
            : base("HnSRaider", "raiderHnS", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                 ImpostorRoles.Impostor.impostorSideSet, ImpostorRoles.Impostor.impostorSideSet, ImpostorRoles.Impostor.impostorEndSet,
                 true, VentPermission.CanUseInUnusualWays, false, true, true)
    {
        IsHideRole = true;
        ValidGamemode = Module.CustomGameMode.StandardHnS;
        HideInExclusiveAssignmentOption = true;
        canInvokeSabotage = false;
        HideKillButtonEvenImpostor = true;
        canReport = false;
        leftAxes = 0;
    }
}

