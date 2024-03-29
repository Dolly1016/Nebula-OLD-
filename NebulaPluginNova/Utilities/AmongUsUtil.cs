﻿using AmongUs.GameOptions;
using Nebula.Behaviour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Utilities;

[NebulaRPCHolder]
public static class AmongUsUtil
{
    public static void SetCamTarget(MonoBehaviour? target = null) => HudManager.Instance.PlayerCam.Target = target ?? PlayerControl.LocalPlayer;
    public static UiElement CurrentUiElement => ControllerManager.Instance.CurrentUiState.CurrentSelection;
    public static bool InMeeting => MeetingHud.Instance == null && ExileController.Instance == null;
    public static byte CurrentMapId => GameOptionsManager.Instance.CurrentGameOptions.MapId;
    private static string[] mapName = new string[] { "skeld", "mira", "polus", "undefined", "airship" };
    public static string ToMapName(byte mapId) => mapName[mapId];
    public static string ToDisplayString(SystemTypes room, byte? mapId = null) => Language.Translate("location." + mapName[mapId ?? CurrentMapId] + "." + Enum.GetName(typeof(SystemTypes), room)!.HeadLower());
    public static float VanillaKillCoolDown => GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.KillCooldown);
    public static float VanillaKillDistance => GameManager.Instance.LogicOptions.GetKillDistance();
    public static bool InCommSab => PlayerTask.PlayerHasTaskOfType<IHudOverrideTask>(PlayerControl.LocalPlayer);
    public static PoolablePlayer PoolablePrefab => HudManager.Instance.IntroPrefab.PlayerPrefab;
    public static PoolablePlayer GetPlayerIcon(GameData.PlayerOutfit outfit, Transform parent,Vector3 position,Vector3 scale,bool flip = false)
    {
        var player = GameObject.Instantiate(PoolablePrefab);

        player.transform.SetParent(parent);

        player.name = outfit.PlayerName;
        player.SetFlipX(flip);
        player.transform.localPosition = position;
        player.transform.localScale = scale;
        player.UpdateFromPlayerOutfit(outfit, PlayerMaterial.MaskType.None, false, true);
        player.ToggleName(false);
        player.SetNameColor(Color.white);

        return player;
    }

    public static PoolablePlayer SetAlpha(this PoolablePlayer player, float alpha)
    {
        foreach (SpriteRenderer r in player.gameObject.GetComponentsInChildren<SpriteRenderer>())
            r.color = new Color(r.color.r, r.color.g, r.color.b, alpha);
        return player;
    }

    public static float GetAlpha(this PoolablePlayer player)=> player.cosmetics.currentBodySprite.BodySprite.color.a;

    public static PoolablePlayer GetPlayerIcon(GameData.PlayerOutfit outfit, Transform parent, Vector3 position, Vector2 scale, float nameScale,Vector3 namePos,bool flip = false)
    {
        var player = GetPlayerIcon(outfit, parent, position, scale, flip);

        player.ToggleName(true);
        player.SetNameScale(Vector3.one * nameScale);
        player.SetNamePosition(namePos);
        player.SetName(outfit.PlayerName);

        return player;
    }
    public static void PlayCustomFlash(Color color, float fadeIn, float fadeOut, float maxAlpha = 0.5f)
    {
        float duration = fadeIn + fadeOut;

        var flash = GameObject.Instantiate(HudManager.Instance.FullScreen, HudManager.Instance.transform);
        flash.color = color;
        flash.enabled = true;
        flash.gameObject.active = true;

        HudManager.Instance.StartCoroutine(Effects.Lerp(duration, new Action<float>((p) =>
        {
            if (p < (fadeIn / duration))
            {
                if (flash != null)
                    flash.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(maxAlpha * p / (fadeIn / duration)));
            }
            else
            {
                if (flash != null)
                    flash.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(maxAlpha * (1 - p) / (fadeOut / duration)));
            }
            if (p == 1f && flash != null)
            {
                flash.enabled = false;
                GameObject.Destroy(flash.gameObject);
            }
        })));
    }

    public static void PlayFlash(Color color)
    {
        PlayCustomFlash(color, 0.375f, 0.375f);
    }

    public static void PlayQuickFlash(Color color)
    {
        PlayCustomFlash(color, 0.1f, 0.4f);
    }

    class CleanBodyMessage
    {
        public TranslatableTag? RelatedTag = null;
        public byte SourceId =  byte.MaxValue;
        public byte TargetId;
    }

    static RemoteProcess<CleanBodyMessage> RpcCleanDeadBodyDef = new RemoteProcess<CleanBodyMessage>(
        "CleanDeadBody",
        (writer, message) => { 
            writer.Write(message.SourceId);
            writer.Write(message.TargetId);
            writer.Write(message.RelatedTag?.Id ?? -1);
        },
        (reader)=> {
            return new() { SourceId = reader.ReadByte(), TargetId = reader.ReadByte(), RelatedTag = TranslatableTag.ValueOf(reader.ReadInt32()) };
        },
        (message, _) =>
        {
            foreach (var d in Helpers.AllDeadBodies()) if (d.ParentId == message.TargetId) GameObject.Destroy(d.gameObject);

            if (message.SourceId != byte.MaxValue)
                NebulaGameManager.Instance?.GameStatistics.RecordEvent(new GameStatistics.Event(GameStatistics.EventVariation.CreanBody, message.SourceId, 1 << message.TargetId) { RelatedTag = message.RelatedTag });
        }
        );

    static public void RpcCleanDeadBody(byte bodyId,byte sourceId=byte.MaxValue,TranslatableTag? relatedTag = null)
    {
        RpcCleanDeadBodyDef.Invoke(new() { TargetId = bodyId, SourceId = sourceId, RelatedTag = relatedTag });
    }

    public static PlayerModInfo? GetHolder(this DeadBody body)
    {
        return NebulaGameManager.Instance?.AllPlayerInfo().FirstOrDefault((p) => p.HoldingDeadBody.HasValue && p.HoldingDeadBody.Value == body.ParentId);
    }

    public static SpriteRenderer GenerateCustomLight(Vector2 position,Sprite lightSprite)
    {
        var renderer = UnityHelper.CreateObject<SpriteRenderer>("Light", null, (Vector3)position + new Vector3(0, 0, -50f), LayerExpansion.GetDrawShadowsLayer());
        renderer.sprite = lightSprite;
        renderer.material.shader = NebulaAsset.MultiplyBackShader;

        return renderer;
    }

    private static SpriteLoader footprintSprite = SpriteLoader.FromResource("Nebula.Resources.Footprint.png", 100f);
    public static void GenerateFootprint(Vector2 pos,Color color, float duration,bool canSeeInShadow = false)
    {
        var renderer = UnityHelper.CreateObject<SpriteRenderer>("Footprint", null, new Vector3(pos.x, pos.y, pos.y / 1000f + 0.001f), canSeeInShadow ? LayerExpansion.GetObjectsLayer() : LayerExpansion.GetDefaultLayer());
        renderer.sprite = footprintSprite.GetSprite();
        renderer.color = color;
        renderer.transform.eulerAngles = new Vector3(0f, 0f, (float)System.Random.Shared.NextDouble() * 360f);
        
        IEnumerator CoShowFootprint()
        {
            yield return new WaitForSeconds(duration);
            while (renderer.color.a > 0f)
            {
                Color col = renderer.color;
                col.a = Mathf.Clamp01(col.a - Time.deltaTime * 0.8f);
                renderer.color = col;
                yield return null;
            }
            GameObject.Destroy(renderer.gameObject);
        }

        NebulaManager.Instance.StartCoroutine(CoShowFootprint().WrapToIl2Cpp());
    }

    public static PlayerControl SpawnDummy()
    {
        var playerControl = UnityEngine.Object.Instantiate(AmongUsClient.Instance.PlayerPrefab);
        var i = playerControl.PlayerId = (byte)GameData.Instance.GetAvailableId();

        playerControl.isDummy = true;

        GameData.Instance.AddPlayer(playerControl);

        playerControl.transform.position = PlayerControl.LocalPlayer.transform.position;
        playerControl.GetComponent<DummyBehaviour>().enabled = true;
        playerControl.isDummy = true;
        playerControl.SetName(AccountManager.Instance.GetRandomName());
        playerControl.SetColor(i);
        playerControl.GetComponent<UncertifiedPlayer>().Certify();

        AmongUsClient.Instance.Spawn(playerControl, -2, InnerNet.SpawnFlags.None);
        GameData.Instance.RpcSetTasks(playerControl.PlayerId, new byte[0]);

        return playerControl;
    }
}
