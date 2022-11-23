namespace Nebula.Roles;

public interface ExtraAssignable
{
    /// <summary>
    /// ExtraRoleを割り振ります。割り振りアルゴリズムは各ロールに委ねられています。
    /// </summary>
    /// <param name="gameData"></param>
    public void Assignment(Patches.AssignMap assignMap);

    public byte assignmentPriority { get; }
    public Module.CustomGameMode ValidGamemode { get; }

    public Module.CustomOption? RegisterAssignableOption(Role role);

    public void EditSpawnableRoleShower(ref string suffix, Role role);

}