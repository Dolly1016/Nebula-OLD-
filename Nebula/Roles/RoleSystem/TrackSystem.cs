using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Roles.RoleSystem
{
    static public class TrackSystem
    {
        static public Objects.CustomButton ButtonInitialize(HudManager __instance, Dictionary<byte,Objects.Arrow> arrows,Sprite buttonSprite, float duration, float coolDown)
        {
            Objects.CustomButton result;
            result = new Objects.CustomButton(
                () => { 

                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return PlayerControl.LocalPlayer.CanMove; },
                () => { },
                buttonSprite,
                new Vector3(-1.8f, -0.06f, 0),
                __instance,
                KeyCode.F,
                true,
                duration,
                () => {
                    foreach (var arrow in arrows.Values) UnityEngine.Object.Destroy(arrow.arrow);
                    arrows.Clear();
                }
            );
            result.MaxTimer = result.Timer = coolDown;
            return result;
        }

        static public void MyControlUpdate(bool showFlag, Dictionary<byte, Objects.Arrow> arrows)
        {
            if (!showFlag)
            {
                if (arrows.Count > 0)
                {
                    foreach (var arrow in arrows.Values)
                    {
                        UnityEngine.Object.Destroy(arrow.arrow);
                    }
                    arrows.Clear();
                }
            }
            else
            {
                HashSet<byte> removeKeys = new HashSet<byte>();
                DeadBody[] deadBodies = Helpers.AllDeadBodies();

                //情報の更新および、存在しない死体への矢印を消去
                bool existFlag = false;
                foreach (var entry in arrows)
                {
                    existFlag = false;
                    foreach (var body in deadBodies)
                    {
                        if (body.ParentId == entry.Key)
                        {
                            existFlag = true; break;
                        }
                    }
                    if (!existFlag) removeKeys.Add(entry.Key);
                }
                foreach (var id in removeKeys) arrows.Remove(id);

                //あらたな死体を追加
                foreach (var body in deadBodies)
                {
                    if (!arrows.ContainsKey(body.ParentId))
                    {
                        arrows.Add(body.ParentId, new Objects.Arrow(Color.blue));
                    }
                }

                foreach (var body in deadBodies)
                {
                    arrows[body.ParentId].Update(body.transform.position);
                }
            }
        }
    }
}
