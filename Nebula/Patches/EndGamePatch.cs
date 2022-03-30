using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Nebula.Patches
{
    public delegate void EndAction(FinalPlayerData finalPlayerData);

    public class EndCondition
    {
        public static EndCondition CrewmateWinByVote = new EndCondition(GameOverReason.HumansByVote, Palette.CrewmateBlue, "crewmate", 16,Module.CustomGameMode.Standard);
        public static EndCondition CrewmateWinByTask = new EndCondition(GameOverReason.HumansByTask, Palette.CrewmateBlue, "crewmate", 16, Module.CustomGameMode.Standard);
        public static EndCondition CrewmateWinDisconnect = new EndCondition(GameOverReason.HumansDisconnect, Palette.CrewmateBlue, "crewmate", 16, Module.CustomGameMode.Standard);
        public static EndCondition ImpostorWinByKill = new EndCondition(GameOverReason.ImpostorByKill, Palette.ImpostorRed, "impostor", 16, Module.CustomGameMode.Standard);
        public static EndCondition ImpostorWinBySabotage = new EndCondition(GameOverReason.ImpostorBySabotage, Palette.ImpostorRed, "impostor", 16, Module.CustomGameMode.Standard);
        public static EndCondition ImpostorWinByVote = new EndCondition(GameOverReason.ImpostorByVote, Palette.ImpostorRed, "impostor", 16, Module.CustomGameMode.Standard);
        public static EndCondition ImpostorWinDisconnect = new EndCondition(GameOverReason.ImpostorDisconnect, Palette.ImpostorRed, "impostor",16, Module.CustomGameMode.Standard);
        public static EndCondition JesterWin = new EndCondition(16, Roles.NeutralRoles.Jester.RoleColor, "jester", 1, Module.CustomGameMode.Standard);
        public static EndCondition JackalWin = new EndCondition(17, Roles.NeutralRoles.Jackal.RoleColor, "jackal", 2, Module.CustomGameMode.Standard);
        public static EndCondition ArsonistWin = new EndCondition(18, Roles.NeutralRoles.Arsonist.RoleColor, "arsonist", 1, Module.CustomGameMode.Standard, false, (fpData) => { PlayerControl.AllPlayerControls.ForEach((Action<PlayerControl>)((p) => { if (!p.Data.IsDead && Roles.Roles.Arsonist.Winner != p.PlayerId) { p.MurderPlayer(p);  fpData.players[p.PlayerId].status = Game.PlayerData.PlayerStatus.Burned; } })); });
        public static EndCondition EmpiricWin = new EndCondition(19, Roles.NeutralRoles.Empiric.RoleColor, "empiric", 1, Module.CustomGameMode.Standard);
        public static EndCondition VultureWin = new EndCondition(20, Roles.NeutralRoles.Vulture.RoleColor, "vulture", 1, Module.CustomGameMode.Standard);
        public static EndCondition AvengerWin = new EndCondition(21, Roles.NeutralRoles.Avenger.RoleColor, "avenger", 0, Module.CustomGameMode.Standard);
        public static EndCondition TrilemmaWin = new EndCondition(24, new Color(209f / 255f, 63f / 255f, 138f / 255f), "trilemma",0, Module.CustomGameMode.Standard);

        public static EndCondition InvestigatorRightGuess = new EndCondition(32, Palette.CrewmateBlue, "rightGuess", 0, Module.CustomGameMode.Investigators,true);
        public static EndCondition InvestigatorWrongGuess = new EndCondition(33, Palette.ImpostorRed, "wrongGuess", 0, Module.CustomGameMode.Investigators);
        public static EndCondition InvestigatorDeathBySabotage = new EndCondition(34, Palette.ImpostorRed, "ghost", 0, Module.CustomGameMode.Investigators);

        public static EndCondition NobodyWin = new EndCondition(48, new Color(72f / 255f, 78f / 255f, 84f / 255f), "nobody", 0, Module.CustomGameMode.All);
        public static EndCondition NobodySkeldWin = new EndCondition(49, new Color(72f / 255f, 78f / 255f, 84f / 255f), "nobody.skeld", 32, Module.CustomGameMode.All);
        public static EndCondition NobodyMiraWin = new EndCondition(50, new Color(72f / 255f, 78f / 255f, 84f / 255f), "nobody.mira", 32, Module.CustomGameMode.All);
        public static EndCondition NobodyPolusWin = new EndCondition(51, new Color(72f / 255f, 78f / 255f, 84f / 255f), "nobody.polus", 32, Module.CustomGameMode.All);
        public static EndCondition NobodyAirshipWin = new EndCondition(52, new Color(72f / 255f, 78f / 255f, 84f / 255f), "nobody.airship", 32, Module.CustomGameMode.All);

        public static EndCondition NoGame = new EndCondition(64, new Color(72f / 255f, 78f / 255f, 84f / 255f), "noGame", 0, Module.CustomGameMode.All);
        public static EndCondition HostDisconnected = new EndCondition(65, new Color(72f / 255f, 78f / 255f, 84f / 255f), "hostDisconnected", 0, Module.CustomGameMode.Investigators);

        public static EndCondition MinigamePlayersWin = new EndCondition(128, Palette.CrewmateBlue, "players", 0, Module.CustomGameMode.Minigame, true);
        public static EndCondition MinigameEscapeesWin = new EndCondition(129, Palette.CrewmateBlue, "escapees", 0, Module.CustomGameMode.Minigame, true);
        public static EndCondition MinigameHunterWin = new EndCondition(130, Palette.ImpostorRed, "hunter", 0, Module.CustomGameMode.Minigame);

        public static EndCondition ShowDownWin = new EndCondition(256, new Color(255f / 255f, 213f / 255f, 0f / 255f), "showDown", 0, Module.CustomGameMode.Parlour);

        




        public static HashSet<EndCondition> AllEnds = new HashSet<EndCondition>() {
            CrewmateWinByVote ,CrewmateWinByTask,CrewmateWinDisconnect,
            ImpostorWinByKill,ImpostorWinBySabotage,ImpostorWinByVote,ImpostorWinDisconnect,
            JesterWin,JackalWin,ArsonistWin,EmpiricWin,VultureWin,
            TrilemmaWin,AvengerWin,
            NoGame,NobodyWin,NobodySkeldWin,NobodyMiraWin,NobodyPolusWin,NobodyAirshipWin,
            InvestigatorRightGuess,InvestigatorWrongGuess,HostDisconnected,
            MinigamePlayersWin,MinigameEscapeesWin,MinigameHunterWin,
            ShowDownWin
        };
    
        public static EndCondition GetEndCondition(GameOverReason gameOverReason)
        {
            foreach(EndCondition condition in AllEnds)
            {
                if (condition.Id == gameOverReason)
                {
                    return condition;
                }
            }
            return null;
        }

        public GameOverReason Id { get; }
        public Color Color { get; }
        public String Identifier { get; }
        public EndAction EndAction { get; }
        public byte Priority { get; }
        public Roles.Template.HasWinTrigger TriggerRole { get; set; }
        public bool IsPeaceful;

        public Module.CustomGameMode GameMode { get; set; }
        public EndCondition(GameOverReason Id,Color Color,String EndText, byte Priority,Module.CustomGameMode GameMode,bool IsPeaceful=false, EndAction EndAction = null)
        {
            this.Id = Id;
            this.Color = Color;
            this.Identifier = EndText;
            this.EndAction = EndAction == null ? (FinalPlayerData data) => { } : EndAction;
            this.Priority = Priority;
            this.GameMode = GameMode;
            this.TriggerRole = null;
            this.IsPeaceful = IsPeaceful;
        }

        public EndCondition(int Id, Color Color, String EndText, byte Priority, Module.CustomGameMode GameMode, bool IsPeaceful = false, EndAction EndAction = null):
            this((GameOverReason)Id,Color,EndText,Priority,GameMode,IsPeaceful,EndAction)
        {
        }

    }

    public class FinalPlayerData
    {
        public class FinalPlayer
        {
            public string name { get; private set; }
            public Roles.Role role { get; private set; }
            public int totalTasks { get; private set; }
            public int completedTasks { get; private set; }
            public Game.PlayerData.PlayerStatus status { get; set; }

            public FinalPlayer(string name, Roles.Role role, Game.PlayerData.PlayerStatus status,int totalTasks, int completedTasks)
            {
                this.name = name;
                this.role = role;
                this.totalTasks = totalTasks;
                this.completedTasks = completedTasks;
                this.status = status;
            }
        }

        public List<FinalPlayer> players { get; private set; }
        public FinalPlayerData()
        {
            players = new List<FinalPlayer>();

            string name;
            foreach (Game.PlayerData player in Game.GameData.data.players.Values)
            {
                //名前に表示を追加する
                name = "";
                Helpers.RoleAction(player.id, (role) => { role.EditDisplayNameForcely(player.id, ref name); });
                if (name.Equals(""))
                    name = player.name;
                else
                    name = player.name + " " + name;
                players.Add(new FinalPlayer(name,
                    player.role, player.Status, player.Tasks.Quota, player.Tasks.Completed));
            }

            players.Sort((p1, p2) => p1.status.Id - p2.status.Id);
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
    public class OnGameEndPatch
    {
        public static EndCondition EndCondition;
        public static FinalPlayerData FinalData;
        public static void Prefix(AmongUsClient __instance, [HarmonyArgument(0)] ref EndGameResult endGameResult)
        {
            FinalData = new FinalPlayerData();
            EndCondition = EndCondition.GetEndCondition(endGameResult.GameOverReason);
            if ((int)endGameResult.GameOverReason >= 10) endGameResult.GameOverReason = EndCondition.IsPeaceful ? GameOverReason.HumansByTask : GameOverReason.ImpostorByKill;
        }

        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ref EndGameResult endGameResult)
        {
            EndCondition.EndAction.Invoke(FinalData);
            //勝利者を消去する
            TempData.winners.Clear();

            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (Game.GameData.data.players[player.PlayerId].role.CheckWin(player, EndCondition))
                {
                    TempData.winners.Add(new WinningPlayerData(player.Data));
                }
            }

            //追加勝利
            bool addedFlag = false;
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (TempData.winners.FindAll((Il2CppSystem.Predicate<WinningPlayerData>)((data) => { return data.PlayerName == player.name; })).Count > 0) continue;

                addedFlag = false;
                Helpers.RoleAction(player, (role) =>
                {
                    if ((!addedFlag) && role.CheckAdditionalWin(player, EndCondition))
                    {
                        TempData.winners.Add(new WinningPlayerData(player.Data));
                        addedFlag = true;
                    }
                });
            }

            //変更したオプションを元に戻す
            PlayerControl.GameOptions.VotingTime = Game.GameData.data.GameRule.vanillaVotingTime;

            // Reset Settings
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ResetVaribles, Hazel.SendOption.Reliable, -1);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCEvents.ResetVaribles();
        }
    }

    [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
    public class EndGameManagerSetUpPatch
    {
        static HashSet<string> AdditionalTextSet = new HashSet<string>();

        public static void AddEndText(string text) { AdditionalTextSet.Add(text); }

        public static void Postfix(EndGameManager __instance)
        {
            //勝利トリガをもどす
            Roles.Roles.ResetWinTrigger();

            //元の勝利チームを削除する
            foreach (PoolablePlayer pb in __instance.transform.GetComponentsInChildren<PoolablePlayer>())
            {
                UnityEngine.Object.Destroy(pb.gameObject);
            }

            //勝利メンバーを載せる
            int num = Mathf.CeilToInt(7.5f);
            List<WinningPlayerData> list = TempData.winners.ToArray().ToList().OrderBy(delegate (WinningPlayerData b)
            {
                if (!b.IsYou)
                {
                    return 0;
                }
                return -1;
            }).ToList<WinningPlayerData>();

            for (int i = 0; i < list.Count; i++)
            {
                WinningPlayerData winningPlayerData2 = list[i];
                int num2 = (i % 2 == 0) ? -1 : 1;
                int num3 = (i + 1) / 2;
                float num4 = (float)num3 / (float)num;
                float num5 = Mathf.Lerp(1f, 0.75f, num4);
                float num6 = (float)((i == 0) ? -8 : -1);
                PoolablePlayer poolablePlayer = UnityEngine.Object.Instantiate<PoolablePlayer>(__instance.PlayerPrefab, __instance.transform);
                poolablePlayer.transform.localPosition = new Vector3(1f * (float)num2 * (float)num3 * num5, FloatRange.SpreadToEdges(-1.125f, 0f, num3, num), num6 + (float)num3 * 0.01f) * 0.9f;
                float num7 = Mathf.Lerp(1f, 0.65f, num4) * 0.9f;
                Vector3 vector = new Vector3(num7, num7, 1f);
                poolablePlayer.transform.localScale = vector;
                poolablePlayer.UpdateFromPlayerOutfit(winningPlayerData2, winningPlayerData2.IsDead);
                if (winningPlayerData2.IsDead)
                {
                    poolablePlayer.Body.sprite = __instance.GhostSprite;
                    poolablePlayer.SetDeadFlipX(i % 2 == 0);
                }
                else
                {
                    poolablePlayer.SetFlipX(i % 2 == 0);
                }

                poolablePlayer.NameText.color = Color.white;
                poolablePlayer.NameText.transform.localScale = new Vector3(1f / vector.x, 1f / vector.y, 1f / vector.z);
                poolablePlayer.NameText.transform.localPosition = new Vector3(poolablePlayer.NameText.transform.localPosition.x, poolablePlayer.NameText.transform.localPosition.y, -15f);
                poolablePlayer.NameText.text = winningPlayerData2.PlayerName;
            }

            // テキストを追加する
            GameObject bonusText = UnityEngine.Object.Instantiate(__instance.WinText.gameObject);
            bonusText.transform.position = new Vector3(__instance.WinText.transform.position.x, __instance.WinText.transform.position.y - 0.5f, __instance.WinText.transform.position.z);
            bonusText.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            TMPro.TMP_Text textRenderer = bonusText.GetComponent<TMPro.TMP_Text>();
            textRenderer.text = Language.Language.GetString("game.endText."+OnGameEndPatch.EndCondition.Identifier);
            foreach(string text in AdditionalTextSet)
            {
                textRenderer.text += text;
            }
            textRenderer.color = OnGameEndPatch.EndCondition.Color;
            AdditionalTextSet.Clear();

            __instance.BackgroundBar.material.SetColor("_Color", OnGameEndPatch.EndCondition.Color);

            var position = Camera.main.ViewportToWorldPoint(new Vector3(0f, 1f, Camera.main.nearClipPlane));
            GameObject roleSummary = UnityEngine.Object.Instantiate(__instance.WinText.gameObject);
            roleSummary.transform.position = new Vector3(__instance.Navigation.ExitButton.transform.position.x + 0.1f, position.y - 0.1f, -14f);
            roleSummary.transform.localScale = new Vector3(1f, 1f, 1f);

            //結果表示
            var roleSummaryText = new StringBuilder();
            roleSummaryText.AppendLine("Roles breakdown:");
            
            foreach (FinalPlayerData.FinalPlayer player in OnGameEndPatch.FinalData.players)
            {
                var roles = string.Join(" ", Helpers.cs(player.role.Color, Language.Language.GetString("role." + player.role.LocalizeName + ".name")));

                var status = string.Join(" ", Language.Language.GetString("status." + player.status.Status));

                string tasks;
                if (player.role.HasFakeTask)
                    tasks = player.totalTasks > 0 ? $"<color=#868686FF>({player.completedTasks}/{player.totalTasks})</color>" : "";
                else
                    tasks = player.totalTasks > 0 ? $"<color=#FAD934FF>({player.completedTasks}/{player.totalTasks})</color>" : "";

                roleSummaryText.AppendLine($"{player.name} - {roles} {status}{tasks}");
            }

            TMPro.TMP_Text roleSummaryTextMesh = roleSummary.GetComponent<TMPro.TMP_Text>();
            roleSummaryTextMesh.alignment = TMPro.TextAlignmentOptions.TopLeft;
            roleSummaryTextMesh.color = Color.white;
            roleSummaryTextMesh.fontSizeMin = 1.5f;
            roleSummaryTextMesh.fontSizeMax = 1.5f;
            roleSummaryTextMesh.fontSize = 1.5f;

            var roleSummaryTextMeshRectTransform = roleSummaryTextMesh.GetComponent<RectTransform>();
            roleSummaryTextMeshRectTransform.anchoredPosition = new Vector2(position.x + 3.5f, position.y - 0.1f);
            roleSummaryTextMesh.text = roleSummaryText.ToString();
        }
    }


    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CheckEndCriteria))]
    class CheckEndCriteriaPatch
    {
        public static bool Prefix(ShipStatus __instance)
        {
            if (ExileController.Instance != null)
            {
                if(SpawnInMinigame.Instance==null)
                    return false;
            }

            if (!GameData.Instance) return false;
            if (DestroyableSingleton<TutorialManager>.InstanceExists) // InstanceExists | Don't check Custom Criteria when in Tutorial
                return true;
            var statistics = new PlayerStatistics(__instance);

            Patches.EndCondition endCondition = null, temp;
            byte priority=Byte.MaxValue;

            foreach(Roles.Side side in Roles.Side.AllSides)
            {   

                temp= side.endCriteriaChecker(statistics, __instance);
                if (temp != null && priority>=temp.Priority)
                {
                    if ((temp.GameMode & Game.GameData.data.GameMode) == 0) continue;

                    endCondition = temp;
                    priority = temp.Priority;
                }
            }

            if (endCondition != null)
            {
                //勝利乗っ取り
                foreach (Roles.Side side in Roles.Side.AllSides)
                {
                    temp = side.endTakeoverChecker(endCondition, statistics, __instance);
                    if(temp!=null) endCondition = temp;
                }

                __instance.enabled = false;
                ShipStatus.RpcEndGame(endCondition.Id, false);
                return false;
            }

            return false;
        }
    }

    public class PlayerStatistics
    {
        private Dictionary<Roles.Side, int> alivePlayers;
        public int TotalAlive { get; private set; }

        //
        public int GetAlivePlayers(Roles.Side side)
        {
            if (alivePlayers.ContainsKey(side)){
                return alivePlayers[side];
            }
            return 0;
        }

        public PlayerStatistics(ShipStatus __instance)
        {
            alivePlayers = new Dictionary<Roles.Side, int>();
            TotalAlive = 0;

            Roles.Side side;

            foreach (GameData.PlayerInfo playerInfo in GameData.Instance.AllPlayers)
            {
                try
                {
                    if (playerInfo.Disconnected)
                    {
                        continue;
                    }
                    if (playerInfo.IsDead)
                    {
                        continue;
                    }
                    TotalAlive++;

                    side = Game.GameData.data.players[playerInfo.PlayerId].role.side;

                    if (!alivePlayers.ContainsKey(side))
                    {
                        alivePlayers.Add(side, 1);
                    }
                    else
                    {
                        alivePlayers[side] = alivePlayers[side] + 1;
                    }
                }catch(NullReferenceException exp)
                {
                    continue;
                }
            }
        }
    }
}
