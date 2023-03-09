namespace Nebula.Module;

public class MetaDialog : MetaScreen
{

    static public List<MetaDialog> dialogOrder = new List<MetaDialog>();
    static public MetaDialog? activeDialogue { get => dialogOrder.Count == 0 ? null : dialogOrder[dialogOrder.Count - 1]; }
    static public bool AnyDialogShown => dialogOrder.Count != 0;
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
        if (dialogOrder.Count < num) num = dialogOrder.Count;
        if (num == 0) return;

        for (int i = 0; i < num; i++)
        {
            dialogOrder[dialogOrder.Count - 1 - i].dialog.Hide();
            GameObject.Destroy(dialogOrder[dialogOrder.Count - 1 - i].dialog.gameObject);
        }
        dialogOrder.RemoveRange(dialogOrder.Count - num, num);
    }

    static public void EraseDialog(MetaDialog dialog)
    {
        if (!dialogOrder.Contains(dialog)) return;
        int index = dialogOrder.IndexOf(dialog);

        for (int i = dialogOrder.Count - 1; i >= index; i--)
        {
            dialogOrder[i].dialog.Hide();
            GameObject.Destroy(dialogOrder[i].dialog.gameObject);
        }
        dialogOrder.RemoveRange(index, dialogOrder.Count - index);
    }

    public override void Close()
    {
        EraseDialog(this);
    }



    static public MSDesigner GenerateIndependentDialog(Vector2 size,string title,string objName,Transform parent,float offsetZ,bool withBackground)
    {
        DialogueBox dialogue = GameObject.Instantiate(HudManager.Instance.Dialogue);
        dialogue.name = objName;
        dialogue.transform.SetParent(parent);

        SpriteRenderer renderer = dialogue.gameObject.transform.GetChild(0).GetComponent<SpriteRenderer>();
        SpriteRenderer closeButton = dialogue.gameObject.transform.GetChild(1).GetComponent<SpriteRenderer>();
        GameObject fullScreen = renderer.transform.GetChild(0).gameObject;
        if (withBackground)
        {
            fullScreen.GetComponent<PassiveButton>().OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            fullScreen.GetComponent<SpriteRenderer>().color = new Color(0f, 0f, 0f, 0.35f);
        }
        else
        {
            fullScreen.GetComponent<PassiveButton>().enabled = false;
            fullScreen.GetComponent<SpriteRenderer>().color = new Color(0f, 0f, 0f, 0f);
        }

        renderer.gameObject.AddComponent<BoxCollider2D>().size = size;
        renderer.color = new Color(1f, 1f, 1f, 0.8f);
        renderer.size = size;

        closeButton.transform.localPosition = new Vector3(-size.x / 2f - 0.3f, size.y / 2f - 0.3f, -10f);
        dialogue.transform.localScale = new Vector3(1, 1, 1);
        dialogue.transform.localPosition = new Vector3(0f, 0f, -50f);
        if (dialogue.transform.parent == HudManager.Instance.transform) dialogue.transform.localPosition += new Vector3(0, 0, -500f);
        var metaDialog = new MetaDialog(dialogue);

        dialogue.target.rectTransform.sizeDelta = size * 1.66f - new Vector2(0.7f, 0.7f);
        dialogue.Show(title);

        return new MSDesigner(metaDialog, size, title.Length > 0 ? dialogue.target.GetPreferredHeight() + 0.1f : 0.2f);
    }

    static public MSDesigner OpenDialog(Vector2 size, string title)
    {
        Transform parent = activeDialogue?.dialog.transform ?? HudManager.Instance.transform;
        float offsetZ = parent == HudManager.Instance.transform ? -500f : 0f;
        var result = GenerateIndependentDialog(size, title, "Dialogue" + dialogOrder.Count, parent, offsetZ,true);
        dialogOrder.Add((MetaDialog)result.screen);
        return result;
    }

    static public MSDesigner OpenMapDialog(byte mapId, bool canChangeMap, Action<GameObject, byte>? mapDecorator)
    {
        float[] rates = { 0.74f, 0.765f, 0.74f, 1f, 1.005f };
        string[] mapNames = { "The Skeld", "MIRA HQ", "Polus", "Undefined", "Airship" };
        byte changeMapId(bool incrementFlag)
        {
            int id = mapId;
            while (true)
            {
                id += incrementFlag ? 1 : -1;

                if (id < 0) id = 4;
                if (id > 4) id = 0;

                if (id != 3) break;
            }
            return (byte)id;
        }

        var dialog = OpenDialog(new Vector2(8.8f, canChangeMap ? 5.4f : 5f), "");
        if (canChangeMap) dialog.AddTopic(
            new MSButton(0.5f, 0.5f, "<", TMPro.FontStyles.Bold, () =>
            {
                EraseDialog(1);
                OpenMapDialog(changeMapId(false), true, mapDecorator);
            }),
            new MSString(2f, mapNames[mapId], TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold),
            new MSButton(0.5f, 0.5f, ">", TMPro.FontStyles.Bold, () =>
            {
                EraseDialog(1);
                OpenMapDialog(changeMapId(true), true, mapDecorator);
            })
            );
        dialog.CustomUse(0.12f);
        MSSprite sprite = new MSSprite(new SpriteLoader(Map.MapData.MapDatabase[mapId].GetMapSprite()), 0f, rates[mapId]);
        dialog.AddTopic(sprite);
        sprite.renderer.material = Map.MapData.MapDatabase[0].GetMapMaterial();
        sprite.renderer.color = new Color(0.05f, 0.2f, 1f, 1f);

        if (mapDecorator != null) mapDecorator(sprite.renderer.gameObject, mapId);

        return dialog;
    }

    static public MSDesigner OpenPlayerDialog(Vector2 size, PlayerControl player)
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

    static public MSDesigner OpenRolesDialog(Predicate<Roles.Role> roleCondition, int page, int rolesPerPage, Action<Roles.Role> onClick)
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
            var text = button.transform.GetChild(0).gameObject.GetComponent<TMPro.TextMeshPro>();
            text.fontStyle = TMPro.FontStyles.Bold;
            text.fontSizeMin /= 2f;
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

    static public MSDesigner OpenPlayersDialog(string display, Action<PlayerControl, PassiveButton> setUpFunc, Action<PlayerControl> onClicked) => OpenPlayersDialog(display, 0.4f, 0f, setUpFunc, onClicked);
    static public MSDesigner OpenPlayersDialog(string display, float height, float margin, Action<PlayerControl, PassiveButton> setUpFunc, Action<PlayerControl> onClicked)
    {
        var designer = MetaDialog.OpenDialog(new Vector2(9f, (height + 0.12f) * 5f + 1f + margin), display);
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

    static private void AddRoleInfo(MSDesigner designer, Roles.Assignable assignable)
    {
        var designers = designer.SplitVertically(new float[] { 0.01f, 0.55f, 0.45f, 0.01f });
        designers[1].AddTopic(new MSString(designers[1].size.x, Helpers.cs(assignable.Color, Language.Language.GetString("role." + assignable.LocalizeName + ".name")), TMPro.TextAlignmentOptions.TopLeft, TMPro.FontStyles.Bold));
        designers[1].AddTopic(new MSMultiString(designers[1].size.x, 1.2f, Language.Language.GetString("role." + assignable.LocalizeName + ".info"), TMPro.TextAlignmentOptions.TopLeft, TMPro.FontStyles.Normal));
        foreach (var hs in assignable.helpSprite)
            designers[1].AddTopic(new MSSprite(hs.sprite, 0.1f, hs.ratio), new MSMultiString(designers[1].size.x - 0.8f, 1.2f, Language.Language.GetString(hs.localizedName), TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Normal));
        foreach (var hb in assignable.helpButton)
            designers[1].AddTopic(new MSButton(4f,0.4f,Language.Language.GetString(hb.Item1),TMPro.FontStyles.Bold,hb.Item2));
        if ((assignable.AssignableOnHelp?.TopOption ?? null) != null) designers[2].AddTopic(new MSMultiString(designers[2].size.x, 1.4f, Module.GameOptionStringGenerator.optionsToString(assignable.AssignableOnHelp.TopOption), TMPro.TextAlignmentOptions.TopLeft, TMPro.FontStyles.Normal));
    }

    static public MSDesigner OpenAssignableHelpDialog(Roles.Assignable assignable)
    {
        var designer = MetaDialog.OpenDialog(new Vector2(8f, 4f), "");
        AddRoleInfo(designer, assignable);
        return designer;
    }

    public static class HelpSearchFilter
    {
        public static bool OnlyCurrentGameMode = true;
        public static bool OnlySpawnable = true;
        public static bool OnlyImpostor = false;
        public static bool OnlyCrewmate = false;
        public static bool OnlyNeutral = false;

        public static bool ShouldShowCategory(Roles.RoleCategory category)
        {
            if(OnlyImpostor || OnlyCrewmate || OnlyNeutral)
            {
                if (OnlyImpostor && category == Roles.RoleCategory.Impostor) return true;
                if (OnlyCrewmate && category == Roles.RoleCategory.Crewmate) return true;
                if (OnlyNeutral && category == Roles.RoleCategory.Neutral) return true;
                return false;
            }
            return true;
        }

        public static void AddFilterTopic(MSDesigner designer,Action refresher,bool ShowSideFilter)
        {
            List<MetaScreenContent> contents = new();
            contents.Add(new MSButton(1.6f, 0.4f, Language.Language.GetString("help.assignable.filter.gameMode"), TMPro.FontStyles.Bold, () =>
            {
                OnlyCurrentGameMode = !OnlyCurrentGameMode;
                refresher();
            }, OnlyCurrentGameMode ? Color.yellow : Color.gray));
            contents.Add(new MSButton(1.6f, 0.4f, Language.Language.GetString("help.assignable.filter.spawnable"), TMPro.FontStyles.Bold, () =>
            {
                OnlySpawnable = !OnlySpawnable;
                refresher();
            }, OnlySpawnable ? Color.yellow : Color.gray));
            if (ShowSideFilter)
            {
                contents.Add(new MSButton(1.6f, 0.4f, Language.Language.GetString("help.assignable.filter.crewmate"), TMPro.FontStyles.Bold, () =>
                {
                    OnlyCrewmate = !OnlyCrewmate;
                    if (OnlyCrewmate)
                    {
                        OnlyImpostor = false;
                        OnlyNeutral = false;
                    }
                    refresher();
                }, OnlyCrewmate ? Color.yellow : Color.gray));
                contents.Add(new MSButton(1.6f, 0.4f, Language.Language.GetString("help.assignable.filter.impostor"), TMPro.FontStyles.Bold, () =>
                {
                    OnlyImpostor = !OnlyImpostor;
                    if (OnlyImpostor)
                    {
                        OnlyCrewmate = false;
                        OnlyNeutral = false;
                    }
                    refresher();
                }, OnlyImpostor ? Color.yellow : Color.gray));
                contents.Add(new MSButton(1.6f, 0.4f, Language.Language.GetString("help.assignable.filter.neutral"), TMPro.FontStyles.Bold, () =>
                {
                    OnlyNeutral = !OnlyNeutral;
                    if (OnlyNeutral)
                    {
                        OnlyCrewmate = false;
                        OnlyImpostor = false;
                    }
                    refresher();
                }, OnlyNeutral ? Color.yellow : Color.gray));
            }
            designer.AddTopic(contents.ToArray());
            designer.CustomUse(0.2f);
        }
    }

    static public MSDesigner OpenHelpDialog(int tab, int arg, List<string>? options = null)
    {
        var designer = MetaDialog.OpenDialog(new Vector2(9f, 5.5f), "");

        var rolesTab = new MSButton(1.2f, 0.4f, Language.Language.GetString("help.index.roles"), TMPro.FontStyles.Bold, () =>
        {
            if (tab != 1)
            {
                EraseDialog((MetaDialog)designer.screen);
                OpenHelpDialog(1, 0, options);
            }
        });

        var ghostRolesTab = new MSButton(1.2f, 0.4f, Language.Language.GetString("help.index.ghostRoles"), TMPro.FontStyles.Bold, () =>
        {
            if (tab != 2)
            {
                EraseDialog((MetaDialog)designer.screen);
                OpenHelpDialog(2, 0, options);
            }
        });

        var modifiesTab = new MSButton(1.2f, 0.4f, Language.Language.GetString("help.index.modifiers"), TMPro.FontStyles.Bold, () =>
        {
            if (tab != 3)
            {
                EraseDialog((MetaDialog)designer.screen);
                OpenHelpDialog(3, 0, options);
            }
        });

        var optionsTab = new MSButton(1.2f, 0.4f, Language.Language.GetString("help.index.options"), TMPro.FontStyles.Bold, () =>
        {
            if (tab != 4)
            {
                EraseDialog((MetaDialog)designer.screen);
                OpenHelpDialog(4, 0, options);
            }
        });

        var helpTab = new MSButton(1.2f, 0.4f, Language.Language.GetString("help.index.help"), TMPro.FontStyles.Bold, () =>
        {
            if (tab != 5)
            {
                EraseDialog((MetaDialog)designer.screen);
                OpenHelpDialog(5, 0, options);
            }
        });

        if (AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started)
        {
            var myTab = new MSButton(1.2f, 0.4f, Language.Language.GetString("help.index.myRoles"), TMPro.FontStyles.Bold, () =>
            {
                if (tab != 0)
                {
                    EraseDialog((MetaDialog)designer.screen);
                    OpenHelpDialog(0, 0, options);
                }
            });

            designer.AddTopic(myTab, rolesTab, ghostRolesTab, modifiesTab, optionsTab, helpTab);
        }
        else
        {
            if (tab == 0) tab = 1;
            designer.AddTopic(rolesTab, ghostRolesTab, modifiesTab, optionsTab, helpTab);
        }

        //見出し
        designer.AddTopic(new MSString(4f, new string[] { Language.Language.GetString("help.header.myRoles"), Language.Language.GetString("help.header.roles"), Language.Language.GetString("help.header.ghostRoles"), Language.Language.GetString("help.header.modifiers"), Language.Language.GetString("help.header.options"), Language.Language.GetString("help.header.help") }[tab], TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold));

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

                    if (arg == 1) assignable = (data.ShouldBeGhostRole) ? (Roles.Assignable)data.ghostRole : data.role;
                    if (data.ShouldBeGhostRole)
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

                designer.AddEnumerableTopic(6, 1, 0, myRoleEnumerator(), (c) =>
                {
                    var text = ((MSButton)c).text;
                    text.fontSizeMin = 0.5f;
                    text.overflowMode = TMPro.TextOverflowModes.Ellipsis;
                });

                if (assignable != null) AddRoleInfo(designer, assignable);

                break;
            case 1:
                HelpSearchFilter.AddFilterTopic(designer, () =>
                {
                    MetaDialog.EraseDialog(1);
                    OpenHelpDialog(tab, arg, options);
                }, true);
                designer.AddRolesTopic((r) => r.category != Roles.RoleCategory.Complex && r.ShowInHelpWindow, (r) => OpenAssignableHelpDialog(r), 5, 6, arg, (p) =>
                {
                    MetaDialog.EraseDialog(1);
                    OpenHelpDialog(tab, arg + p, options);
                });
                break;
            case 2:
                HelpSearchFilter.AddFilterTopic(designer, () =>
                {
                    MetaDialog.EraseDialog(1);
                    OpenHelpDialog(tab, arg, options);
                }, false);
                designer.AddGhostRoleTopic((r) => r.ShowInHelpWindow, (r) => OpenAssignableHelpDialog(r), 5, 6, arg, (p) =>
                {
                    MetaDialog.EraseDialog(1);
                    OpenHelpDialog(tab, arg + p, options);
                });
                break;
            case 3:
                HelpSearchFilter.AddFilterTopic(designer, () =>
                {
                    MetaDialog.EraseDialog(1);
                    OpenHelpDialog(tab, arg, options);
                }, false);
                designer.AddModifyTopic((r) => r.ShowInHelpWindow, (r) => OpenAssignableHelpDialog(r), 5, 6, arg, (p) =>
                {
                    MetaDialog.EraseDialog(1);
                    OpenHelpDialog(tab, arg + p, options);
                });
                break;
            case 4:
                if (options == null) options = GameOptionStringGenerator.GenerateString(20);

                if (arg == 0)
                {
                    var subDesigner = designer.Split(1);
                    subDesigner[0].AddTopic(new MSButton(1.2f, 0.4f, "Spawn Points", TMPro.FontStyles.Bold, () =>
                    {
                        OpenMapDialog(GameOptionsManager.Instance.CurrentGameOptions.MapId, AmongUsClient.Instance.GameState != AmongUsClient.GameStates.Started, (obj, id) => Map.MapData.MapDatabase[id].SetUpSpawnPointInfo(obj));
                    }));
                }
                else
                {
                    var designers = designer.SplitVertically(new float[] { 0.05f, 0.5f, 0.5f, 0.05f });
                    for (int i = 0; i < 2; i++) if (options.Count > i + (arg - 1) * 2) designers[1 + i].AddTopic(new MSMultiString(designers[i + 1].size.x, 1f, options[i + (arg - 1) * 2], TMPro.TextAlignmentOptions.TopLeft, TMPro.FontStyles.Normal));
                }

                designer.CustomUse(3.7f);
                designer.AddPageListTopic(arg, 1 + (options.Count + 1) / 2, (p) =>
                {
                    MetaDialog.EraseDialog(1);
                    OpenHelpDialog(tab, p, options);
                });
                break;
            case 5:
                //ヘルプ画面
                var helpDesigners = designer.SplitVertically(new float[] { 0.3f, 0.4f, 0.3f });
                helpDesigners[1].AddTopic(new MSString(5f, Language.Language.GetString("help.contents"), 2f, 2f, TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold));
                if (HelpContent.rootContent != null && HelpContent.rootContent.ContentGenerator != null) HelpContent.rootContent.ContentGenerator(helpDesigners[1]);
                break;
        }

        return designer;
    }

    public DialogueBox dialog { get; }
    public Action<MetaDialog>? updateFunc { get; set; }

    public MetaDialog(DialogueBox dialogueBox) : base(dialogueBox.gameObject)
    {
        dialog = dialogueBox;
        updateFunc = null;
    }

}