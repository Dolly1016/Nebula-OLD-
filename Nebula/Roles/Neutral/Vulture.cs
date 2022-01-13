﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using Hazel;
using Nebula.Objects;

namespace Nebula.Roles.Neutral
{
    public class Vulture : Role
    {
        /* 陣営色 */
        static public Color Color = new Color(139f/255f, 69f / 255f, 18f / 255f);

        /* オプション */
        private Module.CustomOption eatOption;
        public override void LoadOptionData()
        {
            eatOption = CreateOption(Color.white, "eatenCountNeeded", 3f, 1f, 5f, 1f);
        }

        public bool WinTrigger = false;

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

                    MessageWriter eatWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.CleanDeadBody, Hazel.SendOption.Reliable, -1);
                    eatWriter.Write(PlayerControl.LocalPlayer.PlayerId);
                    eatWriter.Write(targetId);
                    eatWriter.Write(byte.MaxValue);
                    AmongUsClient.Instance.FinishRpcImmediately(eatWriter);
                    RPCEvents.CleanDeadBody(targetId);
                    eatLeft--;
                    if (eatLeft==0)
                    {
                        WinTrigger = true;
                    }

                    eatButton.Timer = eatButton.MaxTimer;
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return deadBodyId != Byte.MaxValue && PlayerControl.LocalPlayer.CanMove; },
                () => { eatButton.Timer = eatButton.MaxTimer; },
                getEatButtonSprite(),
                new Vector3(-1.8f, -0.06f, 0),
                __instance,
                KeyCode.F
            );
            eatButton.MaxTimer = 5;
        }

        /* 矢印 */
        Dictionary<byte, Arrow> Arrows;

        public byte deadBodyId;
        public int eatLeft;

        /* 画像 */
        private Sprite eatButtonSprite = null;
        public Sprite getEatButtonSprite()
        {
            if (eatButtonSprite) return eatButtonSprite;
            eatButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.EatButton.png", 115f);
            return eatButtonSprite;
        }
        public override void MyPlayerControlUpdate()
        {
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
                if (!Arrows.ContainsKey(body.ParentId)) {
                    Arrows[body.ParentId] = new Arrow(Color.blue);
                    Arrows[body.ParentId].arrow.SetActive(true);
                }
                Arrows[body.ParentId].Update(body.transform.position);
            }

        }

        public override void OnMeetingEnd()
        {
            //矢印を消す
            foreach (Arrow arrow in Arrows.Values)
            {
                UnityEngine.Object.Destroy(arrow.arrow);
            }
            Arrows.Clear();
        }

        public override void Initialize(PlayerControl __instance)
        {
            Arrows = new Dictionary<byte, Arrow>();
            eatLeft = (int)eatOption.getFloat();
            WinTrigger = false;
        }

        public override void CleanUp()
        {
            if (eatButton != null)
            {
                eatButton.Destroy();
                eatButton = null;
            }

            WinTrigger = false;
        }

        public Vulture()
            : base("Vulture", "vulture", Color, RoleCategory.Neutral, Side.Vulture, Side.Vulture,
                 new HashSet<Side>() { Side.Vulture }, new HashSet<Side>() { Side.Vulture },
                 new HashSet<Patches.EndCondition>() { Patches.EndCondition.VultureWin },
                 true, true, true, true, true)
        {
            eatButton = null;
        }
    }
}