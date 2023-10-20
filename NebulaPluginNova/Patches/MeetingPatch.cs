using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using System.Collections;
using Nebula.Modules;
using UnityEngine.Rendering;
using Nebula.Behaviour;
using static MeetingHud;

namespace Nebula.Patches;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
class MeetingStartPatch
{
    static private ISpriteLoader LightColorSprite = SpriteLoader.FromResource("Nebula.Resources.ColorLight.png", 100f);
    static private ISpriteLoader DarkColorSprite = SpriteLoader.FromResource("Nebula.Resources.ColorDark.png", 100f);

    class MeetingPlayerContent
    {
        public TMPro.TextMeshPro NameText = null!, RoleText = null!;
        public PlayerModInfo Player = null!;
    }

    static private void Update(List<MeetingPlayerContent> meetingContent)
    {
        foreach(var content in meetingContent)
        {
            if (content.NameText) content.Player.UpdateNameText(content.NameText, true);
            if (content.RoleText) content.Player.UpdateRoleText(content.RoleText);
        }
    }

    static void Postfix(MeetingHud __instance)
    {
        NebulaManager.Instance.CloseAllUI();

        List<MeetingPlayerContent> allContents = new();

        __instance.transform.localPosition = new Vector3(0f, 0f, -25f);


        //色の明暗を表示
        foreach (var player in __instance.playerStates)
        {
            bool isLightColor = DynamicPalette.IsLightColor(Palette.PlayerColors[player.TargetPlayerId]);

            SpriteRenderer renderer = UnityHelper.CreateObject<SpriteRenderer>("Color", player.transform, new Vector3(1.2f, -0.18f, -1f));
            renderer.sprite = isLightColor ? LightColorSprite.GetSprite() : DarkColorSprite.GetSprite();

            player.ColorBlindName.gameObject.SetActive(false);

            var roleText = GameObject.Instantiate(player.NameText, player.transform);
            roleText.transform.localPosition = new Vector3(0.3384f, -0.13f, -0.02f);
            roleText.transform.localScale = new Vector3(0.57f,0.57f);
            roleText.rectTransform.sizeDelta += new Vector2(0.35f, 0f);

            allContents.Add(new() { Player = Helpers.GetPlayer(player.TargetPlayerId)!.GetModInfo()!, NameText = player.NameText, RoleText = roleText });

            player.CancelButton.GetComponent<SpriteRenderer>().material = __instance.Glass.material;
            player.ConfirmButton.GetComponent<SpriteRenderer>().material = __instance.Glass.material;
            player.CancelButton.transform.GetChild(0).GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.None;
            player.ConfirmButton.transform.GetChild(0).GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.None;

        }

        NebulaGameManager.Instance?.OnMeetingStart();


        Update(allContents);

        IEnumerator CoUpdate()
        {
            while (true)
            {
                Update(allContents);
                yield return null;
            }
        }
        __instance.StartCoroutine(CoUpdate().WrapToIl2Cpp());
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
class MeetingClosePatch
{ 
    public static void Postfix(MeetingHud __instance)
    {
        NebulaManager.Instance.CloseAllUI();
    }
}

[HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.SetMaskLayer))]
class VoteMaskPatch
{
    public static bool Prefix(PlayerVoteArea __instance)
    {
        return false;
    }
}

[HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.Start))]
class VoteAreaPatch
{
    public static void Postfix(PlayerVoteArea __instance)
    {
        MeetingHudExtension.Reset();

        try
        {
            var maskParent = UnityHelper.CreateObject<SortingGroup>("MaskedObjects", __instance.transform, new Vector3(0, 0, -0.1f));
            __instance.MaskArea.transform.SetParent(maskParent.transform);
            __instance.PlayerIcon.transform.SetParent(maskParent.transform);
            __instance.Overlay.maskInteraction = SpriteMaskInteraction.None;
            __instance.Overlay.material = __instance.Megaphone.material;

            var mask = __instance.MaskArea.gameObject.AddComponent<SpriteMask>();
            mask.sprite = __instance.MaskArea.sprite;
            mask.transform.localScale = __instance.MaskArea.size;
            __instance.MaskArea.enabled = false;

            __instance.Background.material = __instance.Megaphone.material;

            __instance.PlayerIcon.cosmetics.currentBodySprite.BodySprite.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

            __instance.PlayerIcon.cosmetics.hat.FrontLayer.gameObject.AddComponent<ZOrderedSortingGroup>();
            __instance.PlayerIcon.cosmetics.hat.BackLayer.gameObject.AddComponent<ZOrderedSortingGroup>();
            __instance.PlayerIcon.cosmetics.visor.Image.gameObject.AddComponent<ZOrderedSortingGroup>();
            __instance.PlayerIcon.cosmetics.skin.layer.gameObject.AddComponent<ZOrderedSortingGroup>();
            __instance.PlayerIcon.cosmetics.currentBodySprite.BodySprite.gameObject.AddComponent<ZOrderedSortingGroup>();
        }
        catch { }
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Confirm))]
class CastVotePatch
{
    public static bool Prefix(MeetingHud __instance, [HarmonyArgument(0)] byte suspectStateIdx)
    {
        if (PlayerControl.LocalPlayer.Data.IsDead) return false;

        foreach (var state in __instance.playerStates)
        {
            state.ClearButtons();
            state.voteComplete = true;
        }

        __instance.SkipVoteButton.ClearButtons();
        __instance.SkipVoteButton.voteComplete = true;
        __instance.SkipVoteButton.gameObject.SetActive(false);

        if (__instance.state != MeetingHud.VoteStates.NotVoted) return false;
        
        __instance.state = MeetingHud.VoteStates.Voted;
        
        //CmdCastVote(Mod)
        int vote = 1;
        NebulaGameManager.Instance?.GetModPlayerInfo(PlayerControl.LocalPlayer.PlayerId)?.RoleAction((r) => r.OnCastVoteLocal(suspectStateIdx, ref vote));
        __instance.ModCastVote(PlayerControl.LocalPlayer.PlayerId, suspectStateIdx, vote);
        return false;
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
static class CheckForEndVotingPatch
{
    public static void AddValue(this Dictionary<byte,int> self, byte target,int num)
    {
        if (self.TryGetValue(target, out var last))
            self[target] = last + num;
        else
            self[target] = num;
    }

    public static Dictionary<byte, int> ModCalculateVotes(MeetingHud __instance)
    {
        Dictionary<byte, int> dictionary = new();

        for (int i = 0; i < __instance.playerStates.Length; i++)
        {
            PlayerVoteArea playerVoteArea = __instance.playerStates[i];
            if (playerVoteArea.VotedFor != 252 && playerVoteArea.VotedFor != 255 && playerVoteArea.VotedFor != 254)
            {
                if (!MeetingHudExtension.WeightMap.TryGetValue((byte)i, out var vote)) vote = 1;
                dictionary.AddValue(playerVoteArea.VotedFor,vote);
            }
        }
        
        return dictionary;
    }

    public static KeyValuePair<byte, int> MaxPair(this Dictionary<byte, int> self, out bool tie)
    {
        tie = true;
        KeyValuePair<byte, int> result = new KeyValuePair<byte, int>(byte.MaxValue, int.MinValue);
        foreach (KeyValuePair<byte, int> keyValuePair in self)
        {
            if (keyValuePair.Value > result.Value)
            {
                result = keyValuePair;
                tie = false;
            }
            else if (keyValuePair.Value == result.Value)
            {
                tie = true;
            }
        }
        return result;
    }

    public static bool Prefix(MeetingHud __instance)
    {
        //投票が済んでない場合、なにもしない
        if (!__instance.playerStates.All((PlayerVoteArea ps) => ps.AmDead || ps.DidVote)) return false;

        {
            Dictionary<byte, int> dictionary = ModCalculateVotes(__instance);
            KeyValuePair<byte, int> max = dictionary.MaxPair(out bool tie);

            List<byte> extraVotes = new();

            if (tie)
            {
                foreach (var state in __instance.playerStates)
                {
                    if (!state.DidVote) continue;

                    var modInfo = NebulaGameManager.Instance?.GetModPlayerInfo(state.TargetPlayerId);
                    modInfo?.RoleAction(r=>r.OnTieVotes(ref extraVotes,state));
                }

                foreach (byte target in extraVotes) dictionary.AddValue(target, 1);

                //再計算する
                max = dictionary.MaxPair(out tie);
            }


            GameData.PlayerInfo exiled = GameData.Instance.AllPlayers.Find((Il2CppSystem.Predicate<GameData.PlayerInfo>)((GameData.PlayerInfo v) => !tie && v.PlayerId == max.Key));
            List<MeetingHud.VoterState> allStates = new();

            //記名投票分
            foreach (var state in __instance.playerStates)
            {
                if (!state.DidVote) continue;

                if (!MeetingHudExtension.WeightMap.TryGetValue((byte)state.TargetPlayerId, out var vote)) vote = 1;

                for (int i = 0; i < vote; i++)
                {
                    allStates.Add(new MeetingHud.VoterState
                    {
                        VoterId = state.TargetPlayerId,
                        VotedForId = state.VotedFor
                    });
                }
            }

            //追加投票分
            foreach(var votedFor in extraVotes)
            {
                allStates.Add(new MeetingHud.VoterState
                {
                    VoterId = byte.MaxValue,
                    VotedForId = votedFor
                });
            }

            __instance.RpcVotingComplete(allStates.ToArray(), exiled, tie);
        }

        return false;
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.PopulateResults))]
class PopulateResultPatch
{
    private static void ModBloopAVoteIcon(MeetingHud __instance,GameData.PlayerInfo? voterPlayer, int index, Transform parent,bool isExtra)
    {
        SpriteRenderer spriteRenderer = GameObject.Instantiate<SpriteRenderer>(__instance.PlayerVotePrefab);
        if (GameManager.Instance.LogicOptions.GetAnonymousVotes() || voterPlayer == null)
            PlayerMaterial.SetColors(Palette.DisabledGrey, spriteRenderer);
        else
            PlayerMaterial.SetColors(voterPlayer.DefaultOutfit.ColorId, spriteRenderer);
        
        spriteRenderer.transform.SetParent(parent);
        spriteRenderer.transform.localScale = Vector3.zero;
        __instance.StartCoroutine(Effects.Bloop((float)index * 0.3f + (isExtra ? 0.85f : 0f), spriteRenderer.transform, 1f, isExtra ? 0.5f : 0.7f));

        if (isExtra)
            __instance.StartCoroutine(Effects.Sequence(Effects.Wait((float)index * 0.3f + 0.85f), ManagedEffects.Action(() => parent.GetComponent<VoteSpreader>().AddVote(spriteRenderer)).WrapToIl2Cpp()));
        else
            parent.GetComponent<VoteSpreader>().AddVote(spriteRenderer);
    }


    public static bool Prefix(MeetingHud __instance, [HarmonyArgument(0)]Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<MeetingHud.VoterState> states)
    {
        NebulaGameManager.Instance?.AllRoleAction(r => r.OnEndVoting());

        __instance.TitleText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.MeetingVotingResults);

        foreach (var voteArea in __instance.playerStates)
        {
            voteArea.ClearForResults();
            MeetingHudExtension.LastVotedForMap[voteArea.TargetPlayerId]= voteArea.VotedFor;
        }

        int lastVoteFor = -1;
        int num = 0;
        Transform? voteFor = null;

        //OrderByは安定ソート
        foreach (var state in states.OrderBy(s => s.VotedForId)){
            if(state.VotedForId != lastVoteFor)
            {
                lastVoteFor = state.VotedForId;
                num = 0;
                if (state.SkippedVote)
                    voteFor = __instance.SkippedVoting.transform;
                else
                    voteFor = __instance.playerStates.FirstOrDefault((area) => area.TargetPlayerId == lastVoteFor)?.transform ?? null;
            }

            if (voteFor != null)
            {
                GameData.PlayerInfo? playerById = GameData.Instance.GetPlayerById(state.VoterId);

                ModBloopAVoteIcon(__instance, playerById, num, voteFor, state.VoterId == byte.MaxValue);
                num++;
            }
        }

        return false;
    }
}