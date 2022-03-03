using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Roles.MetaRoles
{
    public class MetaRole : ExtraRole
    {
        static public Color Color = new Color(255 / 255f, 255 / 255f, 255 / 255f);

        private TMPro.TextMeshPro log;
        private Vector3 pos;

        public override void Assignment(Patches.AssignMap assignMap)
        {
            if (Game.GameData.data.GameMode != Module.CustomGameMode.FreePlay) return;

            foreach (var player in Game.GameData.data.players.Keys)
            {
                assignMap.Assign(player, this.id, 0);
            }
        }

        public override void Initialize(PlayerControl __instance)
        {
            log = UnityEngine.Object.Instantiate(HudManager.Instance.TaskText, HudManager.Instance.transform);
            log.maxVisibleLines = 28;
            log.fontSize = log.fontSizeMin = log.fontSizeMax = 2.5f;
            log.outlineWidth += 0.04f;
            log.autoSizeTextContainer = false;
            log.enableWordWrapping = false;
            log.alignment = TMPro.TextAlignmentOptions.TopRight;
            log.rectTransform.pivot = new Vector2(1.0f,0f);
            log.transform.position = Vector3.zero;
            log.transform.localPosition = log.transform.localPosition + new Vector3(4.5f, 0.8f, 0.0f);
            log.transform.localScale = Vector3.one;
            log.color = Palette.White;
            log.enabled = true;

            pos = new Vector3(0f,0f);
        }

        public override void MyUpdate()
        {
            if (log)
            {
                log.text = "";

                log.text += "Distance:" + String.Format("{0:f2}", pos.Distance(PlayerControl.LocalPlayer.transform.position));
            }

            if(Input.GetKeyDown(KeyCode.Keypad1)|| Input.GetKeyDown(KeyCode.Alpha1))
            {
                pos = PlayerControl.LocalPlayer.transform.position;
            }
        }

        public override void CleanUp()
        {
            if (log)
            {
                UnityEngine.Object.Destroy(log);
            }
        }

        public override void EditDisplayNameForcely(byte playerId, ref string displayName)
        {
            displayName += Helpers.cs(
                    Color, "⌘");
        }

        public override void EditDisplayName(byte playerId, ref string displayName, bool hideFlag)
        {
            EditDisplayNameForcely(playerId,ref displayName);
        }

        public MetaRole() : base("MetaRole", "metaRole", Color, 1)
        {
            IsHideRole = true;
            ValidGamemode = Module.CustomGameMode.FreePlay;

            log = null;
        }
    }
}
