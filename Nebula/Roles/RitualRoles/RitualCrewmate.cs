using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using Nebula.Patches;
using BepInEx.IL2CPP.Utils.Collections;
using UnityEngine;

namespace Nebula.Roles.RitualRoles
{
    public class RitualCrewmate : RitualRole
    {
        public RitualCrewmate()
                : base("Crewmate", "ritualCrewmate", Palette.CrewmateBlue, RoleCategory.Crewmate, Side.RitualCrewmate, Side.RitualCrewmate,
                     new HashSet<Side>(new Side[] { Side.RitualCrewmate }), new HashSet<Side>(new Side[] { Side.RitualCrewmate }), new HashSet<EndCondition>(),
                     false, VentPermission.CanNotUse, false, false, false)
        {
            IsHideRole = true;
            ValidGamemode = Module.CustomGameMode.Ritual;
        }

        Sprite circleSprite;
        private Sprite GetCircleSprite()
        {
            if (circleSprite) return circleSprite;
            circleSprite = Helpers.loadSpriteFromResources("Nebula.Resources.SearchCircle.png",100f);
            return circleSprite;
        }

        private IEnumerator GetAppearEnumerator(SpriteRenderer r)
        {
            float p = 0;
            while (true)
            {
                if (p > 1f) break;
                r.color=new Color(1f,1f,1f,p);
                p += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator GetDisappearEnumerator()
        {
            GameObject obj = new GameObject();
            obj.transform.localScale = new Vector3(0.1f, 0.1f, 1f);
            obj.transform.position = PlayerControl.LocalPlayer.transform.position;
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = GetCircleSprite();
            renderer.color = new Color(0,0,0,0);

            int state = 0;
            float a = 0f;
            while (true)
            {
                if (state == 0)
                {
                    a += Time.deltaTime * 4.2f;
                    if (a > 1f)
                    {
                        a = 1f;
                        state = 1;
                    }
                }
                else if(state == 1){
                    a -= Time.deltaTime * 2.2f;
                    if (a < 0f)
                    {
                        break;
                    }
                }

                obj.transform.localScale += new Vector3(Time.deltaTime * 1.4f, Time.deltaTime * 1.4f, 0);
                renderer.color = new Color(1,1,1,a);
                yield return null;
            }
            GameObject.Destroy(obj);
        }

        Objects.CustomButton searchButton;
        private Sprite buttonSprite = null;
        public Sprite getButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.SearchButton.png", 115f);
            return buttonSprite;
        }

        public void RevealMission(Vector2 pos)
        {
            bool flag = false;
            float d= CustomOptionHolder.RitualSearchableDistanceOption.getFloat();
            foreach (var task in PlayerControl.LocalPlayer.myTasks)
            {
                var t = task.GetComponent<Tasks.RitualWiringTask>();
                if (!t) continue;

                flag = false;
                for (int i = 0; i < t.NextLocations.Count; i++)
                {
                    if (t.IsComplete) continue;
                    if ((t.ExistingConsoles & (1<<i)) != 0) continue;
                    //一定距離より遠ければ顕現しない
                    if ((pos - t.NextLocations[i]).magnitude > d) continue;

                    flag = true;

                    Console c = Map.MapEditor.CreateConsoleG(t.ValidRooms[i], "Crack", Tasks.RitualWiringTask.GetTaskSprite(), t.NextLocations[i]);
                    c.TaskTypes = new TaskTypes[] { (TaskTypes)10000 };
                    c.ConsoleId = 0;
                    var renderer = c.GetComponent<SpriteRenderer>();
                    renderer.color = new Color(1f,1f,1f,0f);
                    HudManager.Instance.StartCoroutine(GetAppearEnumerator(renderer).WrapToIl2Cpp());
                    t.ExistingConsoles|= (1 << i);
                }
                if (flag) t.SearchNextLocation();
            }
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            if (searchButton != null)
            {
                searchButton.Destroy();
            }
            searchButton = new Objects.CustomButton(
                () => {
                    var pos = PlayerControl.LocalPlayer.transform.position;
                    __instance.StartCoroutine(GetDisappearEnumerator().WrapToIl2Cpp());
                    var enumerator=Effects.Sequence(new UnhollowerBaseLib.Il2CppReferenceArray<Il2CppSystem.Collections.IEnumerator>(
                        new Il2CppSystem.Collections.IEnumerator[] {
                            Effects.Wait(0.8f),
                            Effects.Action((Il2CppSystem.Action)(()=>{
                                RevealMission(pos);
                            }))
                        }));
                    __instance.StartCoroutine(enumerator);
                    searchButton.Timer = searchButton.MaxTimer;
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return PlayerControl.LocalPlayer.CanMove; },
                () => { searchButton.Timer = searchButton.MaxTimer; },
                getButtonSprite(),
                new Vector3(-0.9f, 0, 0),
                __instance,
                Module.NebulaInputManager.abilityInput.keyCode,
                false,
                "button.label.ritual.search"
            );
            searchButton.MaxTimer = searchButton.Timer = CustomOptionHolder.RitualSearchCoolDownOption.getFloat();
        }

        public override void CleanUp()
        {
            if (searchButton != null)
            {
                searchButton.Destroy();
                searchButton = null;
            }
        }
    }
}
