using Nebula.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;

namespace Nebula.Module
{
    
    public class MetaDialog : MetaScreen
    {

        static public List<MetaDialog> dialogOrder = new List<MetaDialog>();
        static public MetaDialog? activeDialogue { get => dialogOrder.Count == 0 ? null : dialogOrder[dialogOrder.Count - 1]; }
        
        static public void Update()
        {
            for (int i = 0; i < dialogOrder.Count; i++)
            {
                if (dialogOrder[i].updateFunc != null) dialogOrder[i].updateFunc(dialogOrder[i]);
                dialogOrder[i].dialog.BackButton.gameObject.SetActive(i == dialogOrder.Count - 1);

                if (dialogOrder[i].dialog.gameObject.activeSelf) continue;

                EraseDialog(dialogOrder[i]);

                break;
            }
        }

        static public void Initialize()
        {
            foreach (var dialog in dialogOrder.AsEnumerable().Reverse())
            {
                if (dialog.dialog)
                {
                    dialog.dialog.Hide();
                    GameObject.Destroy(dialog.dialog.gameObject);
                }
            }
            dialogOrder.Clear();
        }

        static public void EraseDialogAll()
        {
            Initialize();
        }

        /// <summary>
        /// 最前面から指定の数だけダイアログを閉じます。
        /// </summary>
        /// <param name="num"></param>
        static public void EraseDialog(int num)
        {
            for (int i = 0; i <num; i++)
            {
                dialogOrder[dialogOrder.Count-1-i].dialog.Hide();
                GameObject.Destroy(dialogOrder[dialogOrder.Count - 1 - i].dialog.gameObject);
            }
            dialogOrder.RemoveRange(dialogOrder.Count - num, num);
        }

        static public void EraseDialog(MetaDialog dialog)
        {
            if (!dialogOrder.Contains(dialog)) return;
            int index = dialogOrder.IndexOf(dialog);

            for (int i = dialogOrder.Count-1; i >=index; i--)
            {
                dialogOrder[i].dialog.Hide();
                GameObject.Destroy(dialogOrder[i].dialog.gameObject);
            }
            dialogOrder.RemoveRange(index,dialogOrder.Count-index);
        }

        public override void Close()
        {
            EraseDialog(this);
        }

        

        

        static public MSDesigner OpenDialog(Vector2 size, string title)
        {
            DialogueBox dialogue = GameObject.Instantiate(HudManager.Instance.Dialogue);
            dialogue.name = "Dialogue" + dialogOrder.Count;
            dialogue.transform.SetParent(activeDialogue?.dialog.transform ?? HudManager.Instance.transform);
            SpriteRenderer renderer = dialogue.gameObject.transform.GetChild(0).GetComponent<SpriteRenderer>();
            SpriteRenderer closeButton = dialogue.gameObject.transform.GetChild(1).GetComponent<SpriteRenderer>();
            GameObject fullScreen = renderer.transform.GetChild(0).gameObject;
            fullScreen.GetComponent<PassiveButton>().OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            fullScreen.GetComponent<SpriteRenderer>().color = new Color(0f, 0f, 0f, 0.35f);
            renderer.gameObject.AddComponent<BoxCollider2D>().size = size;
            renderer.color = new Color(1f, 1f, 1f, 0.8f);
            renderer.size = size;

            closeButton.transform.localPosition = new Vector3(-size.x / 2f - 0.3f, size.y / 2f - 0.3f, -10f);
            dialogue.transform.localScale = new Vector3(1, 1, 1);
            dialogue.transform.localPosition = new Vector3(0f, 0f, -50f);
            if (dialogue.transform.parent == HudManager.Instance.transform) dialogue.transform.localPosition += new Vector3(0, 0, -50f);
            var metaDialog = new MetaDialog(dialogue);
            dialogOrder.Add(metaDialog);

            dialogue.target.rectTransform.sizeDelta = size * 1.66f - new Vector2(0.7f, 0.7f);
            dialogue.Show(title);

            return new MSDesigner(metaDialog, size, title.Length > 0 ? dialogue.target.GetPreferredHeight() + 0.1f : 0.2f);
        }

        static public MSDesigner OpenPlayerDialog(Vector2 size,PlayerControl player)
        {
            var dialog = OpenDialog(size, player.name);

            var poolable = GameObject.Instantiate(Patches.IntroCutsceneOnDestroyPatch.PlayerPrefab);
            poolable.transform.SetParent(dialog.screen.screen.transform);
            poolable.gameObject.layer = LayerExpansion.GetUILayer();

            poolable.SetPlayerDefaultOutfit(player);
            poolable.cosmetics.SetMaskType(PlayerMaterial.MaskType.None);

            poolable.transform.localScale = new Vector3(0.2f, 0.2f, 1f);
            poolable.transform.localPosition = new Vector3(-size.x / 2f + 0.35f, size.y / 2f - 0.37f, 0f);


            ((MetaDialog)dialog.screen).dialog.target.transform.localPosition += new Vector3(0.4f, 0f, 0f);

            return dialog;
        }

        static public MSDesigner OpenRolesDialog(Predicate<Roles.Role> roleCondition,int page,int rolesPerPage,Action<Roles.Role> onClick)
        {
            var designer = Module.MetaDialog.OpenDialog(new Vector2(10.5f, 5.4f), "Roles");
            var designers = designer.Split(6, 0.14f);

            int skip = page * rolesPerPage;
            int index = 0;
            bool hasNext = false;
            foreach (var role in Roles.Roles.AllRoles)
            {
                if (!roleCondition(role)) continue;

                if (skip > 0)
                {
                    skip--;
                    continue;
                }
                if (index >= rolesPerPage)
                {
                    hasNext = true;
                    break;
                }

                var r = role;
                var button = designers[index % 6].AddButton(new Vector2(1.65f, 0.36f), role.Name, Helpers.cs(role.Color, Language.Language.GetString("role." + role.LocalizeName + ".name")));
                button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => onClick(r)));
                button.transform.GetChild(0).gameObject.GetComponent<TMPro.TextMeshPro>().fontStyle = TMPro.FontStyles.Bold;
                designers[index % 6].CustomUse(-0.07f);
                index++;
            }

            designer.CustomUse(designers[0].Used);

            designer.AddPageTopic(page, page > 0, hasNext, (p) =>
            {
                designer.screen.Close();
                OpenRolesDialog(roleCondition, page + p, rolesPerPage, onClick);
            });

            return designer;
        }

        static public MSDesigner OpenPlayersDialog(string display, Action<PlayerControl, PassiveButton> setUpFunc,Action<PlayerControl> onClicked)=>OpenPlayersDialog(display,0.4f,0f,setUpFunc,onClicked);
        static public MSDesigner OpenPlayersDialog(string display,float height,float margin,Action<PlayerControl,PassiveButton> setUpFunc,Action<PlayerControl> onClicked)
        {
            var designer = MetaDialog.OpenDialog(new Vector2(9f, (height+0.12f) * 5f + 1f+ margin), display);
            var designers = designer.Split(3, 0.2f);
            int i = 0;

            foreach (var player in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                PlayerControl p = player;

                var button = designers[i].AddPlayerButton(new Vector2(2.7f, height), p, true);
                button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                {
                    onClicked(p);
                }));
                setUpFunc(p, button);

                i = (i + 1) % 3;
            }
            designer.CustomUse(designers[0].Used);
            return designer;
        }

        static private void AddRoleInfo(MSDesigner designer,Roles.Assignable assignable) 
        {
            var designers = designer.SplitVertically(new float[] { 0.01f, 0.55f, 0.45f, 0.01f });
            designers[1].AddTopic(new MSString(designers[1].size.x, Helpers.cs(assignable.Color, Language.Language.GetString("role." + assignable.LocalizeName + ".name")), TMPro.TextAlignmentOptions.TopLeft, TMPro.FontStyles.Bold));
            designers[1].AddTopic(new MSMultiString(designers[1].size.x, 1.2f, Language.Language.GetString("role." + assignable.LocalizeName + ".info"), TMPro.TextAlignmentOptions.TopLeft, TMPro.FontStyles.Normal));
            foreach(var hs in assignable.helpSprite)
                designers[1].AddTopic(new MSSprite(hs.sprite,0.1f,hs.ratio),new MSMultiString(designers[1].size.x-0.8f,1.2f,Language.Language.GetString(hs.localizedName),TMPro.TextAlignmentOptions.Left,TMPro.FontStyles.Normal));
            
            if((assignable.AssignableOnHelp?.TopOption ?? null) != null) designers[2].AddTopic(new MSMultiString(designers[2].size.x, 1.4f, Module.GameOptionStringGenerator.optionsToString(assignable.AssignableOnHelp.TopOption), TMPro.TextAlignmentOptions.TopLeft, TMPro.FontStyles.Normal));
        }

        static public MSDesigner OpenAssignableHelpDialog(Roles.Assignable assignable)
        {
            var designer = MetaDialog.OpenDialog(new Vector2(8f, 4f),"");
            AddRoleInfo(designer,assignable);
            return designer;
        }

        static public MSDesigner OpenHelpDialog(int tab,int arg,List<string>? options=null)
        {
            var designer = MetaDialog.OpenDialog(new Vector2(9f, 5.5f),"");

            var rolesTab = new MSButton(1.2f, 0.4f,"Roles",TMPro.FontStyles.Bold,()=> {
                if (tab != 1)
                {
                    EraseDialog((MetaDialog)designer.screen);
                    OpenHelpDialog(1,0,options);
                }
            });

            var ghostRolesTab = new MSButton(1.2f, 0.4f, "Ghost", TMPro.FontStyles.Bold, () => {
                if (tab != 2)
                {
                    EraseDialog((MetaDialog)designer.screen);
                    OpenHelpDialog(2, 0, options);
                }
            });

            var modifiesTab = new MSButton(1.2f, 0.4f, "Modifies", TMPro.FontStyles.Bold, () => {
                if (tab != 3)
                {
                    EraseDialog((MetaDialog)designer.screen);
                    OpenHelpDialog(3, 0, options);
                }
            });

            var optionsTab = new MSButton(1.2f, 0.4f, "Options", TMPro.FontStyles.Bold, () => {
                if (tab != 4)
                {
                    EraseDialog((MetaDialog)designer.screen);
                    OpenHelpDialog(4, 0, options);
                }
            });

            if (AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started)
            {
                var myTab = new MSButton(1.2f, 0.4f, "My Role", TMPro.FontStyles.Bold, () => {
                    if (tab != 0)
                    {
                        EraseDialog((MetaDialog)designer.screen);
                        OpenHelpDialog(0, 0, options);
                    }
                });

                designer.AddTopic(myTab,rolesTab, ghostRolesTab,modifiesTab, optionsTab);
            }
            else
            {
                if (tab == 0) tab = 1;
                designer.AddTopic(rolesTab, ghostRolesTab,modifiesTab, optionsTab);
            }

            //見出し
            designer.AddTopic(new MSString(4f,new string[] { "My Roles","Roles","Ghost Roles","Modifiers","All Options"}[tab],TMPro.TextAlignmentOptions.Center,TMPro.FontStyles.Bold));

            switch (tab)
            {
                case 0:
                    Roles.Assignable? assignable = null;

                    IEnumerator<MetaScreenContent> myRoleEnumerator()
                    {
                        if (Game.GameData.data == null) yield break;
                        var data = Game.GameData.data.myData.getGlobalData();
                        if (data == null) yield break;

                        if (arg == 0) assignable = data.role;
                        yield return new MSButton(1.3f, 0.36f,
                       Helpers.cs(data.role.Color, Language.Language.GetString("role." + data.role.LocalizeName + ".name")),
                       TMPro.FontStyles.Bold,
                       () => { MetaDialog.EraseDialog(1); OpenHelpDialog(0, 0, options); });

                        if (arg == 1) assignable = (!data.IsAlive && data.role.CanHaveGhostRole && data.ghostRole != null) ? (Roles.Assignable)data.ghostRole : data.role;
                        if (!data.IsAlive && data.role.CanHaveGhostRole && data.ghostRole != null)
                        {
                            yield return new MSButton(1.3f, 0.36f,
                           Helpers.cs(data.ghostRole.Color, Language.Language.GetString("role." + data.ghostRole.LocalizeName + ".name")),
                           TMPro.FontStyles.Bold,
                           () => { MetaDialog.EraseDialog(1); OpenHelpDialog(0, 1, options); });
                        }

                        int index = 2;
                        foreach (var r in data.extraRole)
                        {
                            var extraRole = r;
                            int currentIndex = index;
                            if (arg == index) assignable = extraRole;
                            yield return new MSButton(1.3f, 0.36f,
                      Helpers.cs(extraRole.Color, Language.Language.GetString("role." + extraRole.LocalizeName + ".name")),
                      TMPro.FontStyles.Bold,
                      () => { MetaDialog.EraseDialog(1); OpenHelpDialog(0, currentIndex, options); });

                            index++;
                        }
                    }

                    designer.AddEnumerableTopic(6,1,0, myRoleEnumerator(), (c) => {
                        var text = ((MSButton)c).text;
                        text.fontSizeMin = 0.5f;
                        text.overflowMode = TMPro.TextOverflowModes.Ellipsis;
                    });

                    if(assignable!=null)AddRoleInfo(designer,assignable);

                    break;
                case 1:
                    designer.AddRolesTopic((r) => r.category != Roles.RoleCategory.Complex, (r) => OpenAssignableHelpDialog(r), 5, 6, arg, (p) => {
                        MetaDialog.EraseDialog(1);
                        OpenHelpDialog(tab, arg + p, options);
                    });
                    break;
                case 2:
                    designer.AddGhostRoleTopic((r) => true, (r) => OpenAssignableHelpDialog(r), 5, 6, arg, (p) => {
                        MetaDialog.EraseDialog(1);
                        OpenHelpDialog(tab, arg + p, options);
                    });
                    break;
                case 3:
                    designer.AddModifyTopic((r) => true, (r) => OpenAssignableHelpDialog(r), 5, 6, arg,(p)=> {
                        MetaDialog.EraseDialog(1);
                        OpenHelpDialog(tab, arg + p, options);
                        });
                    break;
                case 4:
                    if (options == null) options = GameOptionStringGenerator.GenerateString(20);

                    var designers = designer.SplitVertically(new float[] { 0.05f, 0.5f, 0.5f, 0.05f });

                    for (int i = 0; i < 2; i++) if (options.Count > i + arg * 2) designers[1+i].AddTopic(new MSMultiString(designers[i+1].size.x,1f,options[i+arg*2],TMPro.TextAlignmentOptions.TopLeft,TMPro.FontStyles.Normal));

                    designer.CustomUse(3.7f);
                    designer.AddPageListTopic(arg,(options.Count+1)/2,(p)=> {
                        MetaDialog.EraseDialog(1);
                        OpenHelpDialog(tab, p, options);
                    });
                    break;
            }

            return designer;
        }

        public DialogueBox dialog { get; }
        public Action<MetaDialog>? updateFunc { get; set; }

        public MetaDialog(DialogueBox dialogueBox):base(dialogueBox.gameObject)
        {
            dialog = dialogueBox;
            updateFunc = null;
        }

    }
}
