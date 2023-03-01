using LibCpp2IL;
using Nebula.Module;
using static Nebula.Module.CustomOption;

namespace Nebula.Roles.Template;

public class HasBilateralness : Role
{
    public Module.CustomOption numOfSecondarySide;
    public Module.CustomOption chanceToSpawnAsSecondarySide;
    public bool AssignedDefinitively => numOfSecondarySide.selection != 0;

    protected Role FirstRole = null, SecondaryRole = null;

    public int ChanceOfSecondarySide()
    {
        return chanceToSpawnAsSecondarySide.getSelection();
    }

    //Complexなロールカテゴリーについてのみ呼ばれます。
    public override Patches.AssignRoles.RoleAllocation[] GetComplexAllocations()
    {
        if (FirstRole == null || SecondaryRole == null) return null;
        if (!TopOption.getBool()) return null;

        Patches.AssignRoles.RoleAllocation[] result = new Patches.AssignRoles.RoleAllocation[(int)RoleCountOption.getFloat()];

        int chance = RoleChanceOption.getSelection() + 1;
        int secondary;

        if (AssignedDefinitively)
        {
            //決定的な割り当て
            secondary = (int)numOfSecondarySide.getFloat();
        }
        else
        {
            //ランダム性のある割り当て
            secondary = Helpers.CalcProbabilityCount(ChanceOfSecondarySide(), result.Length);
        }

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = new Patches.AssignRoles.RoleAllocation(i < secondary ? SecondaryRole : FirstRole, chance);
            if (i == 0 && RoleChanceSecondaryOption.getSelection() != 0) chance = RoleChanceSecondaryOption.getSelection();
        }

        return result;
    }

    public override void LoadOptionData()
    {
        numOfSecondarySide = CreateOption(Color.white, "numOfSecondarySide", CustomOptionHolder.GetStringMixedSelections("option.display.random", 0, 15, 1, 15, 1).ToArray(), "option.display.random").HiddenOnDisplay(true).HiddenOnMetaScreen(true);
        chanceToSpawnAsSecondarySide = CreateOption(Color.white, "chanceToSpawnAsSecondarySide", CustomOptionHolder.ratesWithoutTerminal).AddInvPrerequisite(numOfSecondarySide).HiddenOnDisplay(true).HiddenOnMetaScreen(true);

        RoleCountOption.DisplayValueDecorator = (orig, option) => {
            if (numOfSecondarySide.selection == 0)
            {
                int seconProb=((int)chanceToSpawnAsSecondarySide.getSelection() + 1) * 10;
                string persentStr = Language.Language.GetString("option.suffix.percent");
                return orig + " (" + Language.Language.GetString("role." + LocalizeName + ".prefix.primary") + ": " + (100 - seconProb).ToString() + persentStr + ", " + Language.Language.GetString("role." + LocalizeName + ".prefix.secondary") + ": " + seconProb.ToString() + persentStr + ")";
            }
            else
            {
                int primNum = Mathf.Max((int)RoleCountOption.getFloat() - (int)numOfSecondarySide.getFloat(), 0);
                int seconNum = Mathf.Min((int)RoleCountOption.getFloat(), (int)numOfSecondarySide.getFloat());
                return "(" + Language.Language.GetString("role." + LocalizeName + ".prefix.primary") + ": " + primNum.ToString() + ", " + Language.Language.GetString("role." + LocalizeName + ".prefix.secondary") + ": " + seconNum.ToString() + ")";
            }
        };
        TopOption.preOptionScreenBuilder = (refresher) =>
        {
            var origOption = GetStandardTopOption(refresher);
            var countOption = origOption.SubArray(1, 5).ToList();
            var chanceOption = origOption.SubArray(7, origOption.Length - 7).ToList();
            chanceOption.Insert(0, new Module.MSMargin(0.2f));
            chanceOption.Insert(1, 
                new MSOptionString(RoleChanceOption, 2f, RoleChanceOption.getName(), 2f, 0.8f, TMPro.TextAlignmentOptions.MidlineRight, TMPro.FontStyles.Bold)
            );

            countOption.Insert(0, new Module.MSMargin(0.6f));
            countOption.Add(new Module.MSString(0.2f, "(", TMPro.TextAlignmentOptions.MidlineRight, TMPro.FontStyles.Bold));
            countOption.Add(new Module.MSString(1.3f, numOfSecondarySide.getName(), 2f, 1f, TMPro.TextAlignmentOptions.MidlineRight, TMPro.FontStyles.Bold, true,true));
            countOption.Add(new MSString(0.2f, ":", TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold));
            countOption.Add(
                  new Module.MSButton(0.4f, 0.4f, "<<", TMPro.FontStyles.Bold, () => {
                      if (numOfSecondarySide.selection == 0)
                          numOfSecondarySide.addSelection(RoleCountOption!.selection + 1);
                      else
                          numOfSecondarySide.addSelection(-1);
                      refresher();
                  }));
            countOption.Add(new Module.MSString(0.65f, numOfSecondarySide.getString(), 2f, 0.6f, TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold, true, true));
            countOption.Add(
                  new Module.MSButton(0.4f, 0.4f, ">>", TMPro.FontStyles.Bold, () => {
                      if (numOfSecondarySide.selection > RoleCountOption!.selection + 1)
                          numOfSecondarySide.updateSelection(0);
                      else
                          numOfSecondarySide.addSelection(1);
                      refresher();
                  })
                  );
            countOption.Add(new Module.MSString(0.2f, ")", TMPro.TextAlignmentOptions.MidlineRight, TMPro.FontStyles.Bold));

            if (!AssignedDefinitively)
            {
                countOption.InsertRange(countOption.Count - 1,
                    new Module.MetaScreenContent[] {
                        new Module.MSButton(0.4f, 0.4f, "<<", TMPro.FontStyles.Bold, () =>
                            {
                                chanceToSpawnAsSecondarySide.addSelection(-1);
                                refresher();
                            }),
                        new Module.MSString(0.6f, chanceToSpawnAsSecondarySide.getString(), 2f, 0.6f, TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold, true, true),
                        new Module.MSButton(0.4f, 0.4f, ">>", TMPro.FontStyles.Bold, () =>
                            {
                                chanceToSpawnAsSecondarySide.addSelection(1);
                                refresher();
                            })
                    }
                );
            }
            else {
                countOption.Add(new Module.MSMargin(0.66f + 0.5f + 0.5f));
            }

            return new Module.MetaScreenContent[][] { countOption.ToArray(), chanceOption.ToArray() };
        };
    }

    public override void SpawnableTest(ref Dictionary<Role, int> DefinitiveRoles, ref HashSet<Role> SpawnableRoles)
    {
        if (!TopOption.getBool()) return;

        if (AssignedDefinitively)
        {
            if (RoleChanceOption.getSelection() == 9)
            {
                DefinitiveRoles[FirstRole] = Mathf.Max((int)RoleCountOption!.getFloat() - (int)numOfSecondarySide.getFloat(), 0);
                DefinitiveRoles[SecondaryRole] = Mathf.Min((int)RoleCountOption!.getFloat(), (int)numOfSecondarySide.getFloat());
            }
            else
                foreach (var role in GetComplexAllocations())
                    SpawnableRoles.Add(role.role);
        }
        else
        {
            float chance = chanceToSpawnAsSecondarySide.getSelection();
            if (chance != 10f)
            {
                if (RoleChanceOption.getSelection() == 9)
                    DefinitiveRoles[FirstRole] = 0;
                else
                    SpawnableRoles.Add(FirstRole);
            }
            if (chance != 0f)
            {
                if (RoleChanceOption.getSelection() == 9)
                    DefinitiveRoles[SecondaryRole] = 0;
                else
                    SpawnableRoles.Add(SecondaryRole);
            }
        }
    }

    public HasBilateralness(string name, string localizeName, Color color) :
        base(name, localizeName, color, RoleCategory.Complex,
            Side.Crewmate, Side.Crewmate, new HashSet<Side>(), new HashSet<Side>(),
            new HashSet<Patches.EndCondition>(),
            false, VentPermission.CanNotUse, false, false, false)
    {
    }
}

public class BilateralnessRole : Role
{
    private bool IsSecondaryRole;
    HasBilateralness FRole;
    Func<HasBilateralness> GetFRoleFunc;

    protected BilateralnessRole(string name, string localizeName, Color color, RoleCategory category,
        Side side, Side introMainDisplaySide, HashSet<Side> introDisplaySides, HashSet<Side> introInfluenceSides,
        HashSet<Patches.EndCondition> winReasons,
        bool hasFakeTask, VentPermission canUseVents, bool canMoveInVents,
        bool ignoreBlackout, bool useImpostorLightRadius, Func<HasBilateralness> bilateralness, bool isSecondary) :
        base(name, localizeName, color, category,
            side, introMainDisplaySide, introDisplaySides, introInfluenceSides,
            winReasons,
            hasFakeTask, canUseVents, canMoveInVents,
            ignoreBlackout, useImpostorLightRadius)
    {
        IsSecondaryRole = isSecondary;
        GetFRoleFunc = bilateralness;
    }

    public override void LoadOptionData()
    {
        FRole = GetFRoleFunc.Invoke();
    }

    public override bool CanHaveExtraAssignable(ExtraAssignable extraRole)
    {
        return FRole.CanHaveExtraAssignable(extraRole);
    }

    public override bool IsSpawnable()
    {
        if (!FRole.TopOption.getBool()) return false;

        if (FRole.AssignedDefinitively)
        {
            return FRole.GetComplexAllocations().Any(role => role.role == this);
        }
        else
        {
            return FRole.chanceToSpawnAsSecondarySide.getSelection() != (IsSecondaryRole ? 0 : 10);
        }

        return true;
    }

    public override void SpawnableTest(ref Dictionary<Role, int> DefinitiveRoles, ref HashSet<Role> SpawnableRoles)
    {

    }
}