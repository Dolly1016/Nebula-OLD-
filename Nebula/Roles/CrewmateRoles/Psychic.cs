using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using Hazel;
using Nebula.Objects;

namespace Nebula.Roles.CrewmateRoles
{
    public class Psychic : Role
    {
        static public Color Color = new Color(96f / 255f, 206f / 255f, 137f / 255f);

        private CustomButton killButton;

        private Module.CustomOption killCooldownOption;

        private float deathMessageInterval;

        private string[] PsychicMessage = new string[] { "elapsedTime", "killerColor", "killerRole", "myRole","losing" };

        public override void MyPlayerControlUpdate()
        {
            deathMessageInterval -= Time.deltaTime;
            if (deathMessageInterval > 0) return;
            deathMessageInterval = 7f;

            foreach (Game.DeadPlayerData deadPlayerData in Game.GameData.data.deadPlayers.Values)
            {
                if (!deadPlayerData.existDeadBody) continue;

                float distance=deadPlayerData.deathLocation.Distance(PlayerControl.LocalPlayer.transform.position);
                if (distance > 5) continue;

                string m_time = "", m_color = "", m_role = "",i_role="";
                
                m_time =((int)(deadPlayerData.Elapsed / 5f) * 5).ToString();
                i_role = Language.Language.GetString("role." + deadPlayerData.Data.role.localizeName + ".name");
                
                if (deadPlayerData.MurderId != Byte.MaxValue)
                {
                    m_color = Module.CustomColors.lighterColors.Contains(Helpers.GetModData(deadPlayerData.MurderId).Outfit.ColorId)?
                        Language.Language.GetString("role.psychic.color.light"): Language.Language.GetString("role.psychic.color.dark");
                    m_role = Language.Language.GetString("role." + Helpers.GetModData(deadPlayerData.MurderId).role.localizeName + ".name");

                }

                string transratedMessage = Language.Language.GetString("role.psychic.message."+PsychicMessage[NebulaPlugin.rnd.Next(PsychicMessage.Length)]);
                transratedMessage = transratedMessage.Replace("%TIME%",m_time).Replace("%COLOR%",m_color).Replace("%ROLE%",m_role).Replace("%MYROLE%", i_role);

                CustomMessage message=CustomMessage.Create(deadPlayerData.deathLocation,true, transratedMessage, (5-distance),1f,1f,1f,new Color32(255,255,255,150));
                message.textSwapGain = (int)(distance * 6);
                message.textSwapDuration = 0.05f+(5-distance)*0.08f;
                message.textSizeVelocity = new Vector3(0.1f, 0.1f);
                message.velocity = new Vector3(0, 0.1f, 0);
            }
        }

        public Psychic()
            : base("Psychic", "psychic", Color, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                 Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Crewmate.crewmateEndSet,
                 false, false, false, false, false)
        {
            deathMessageInterval = 5f;
        }
    }
}
