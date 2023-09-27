using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Extensions;

public static class KillAnimationExtension
{
    static public IEnumerator CoPerformModKill(this KillAnimation killAnim, PlayerControl source, PlayerControl target,bool blink)
    {
        FollowerCamera cam = Camera.main.GetComponent<FollowerCamera>();
        bool isParticipant = PlayerControl.LocalPlayer == source || PlayerControl.LocalPlayer == target;
        PlayerPhysics sourcePhys = source.MyPhysics;
        if (blink) KillAnimation.SetMovement(source, false);
        KillAnimation.SetMovement(target, false);
        DeadBody deadBody = GameObject.Instantiate<DeadBody>(GameManager.Instance.DeadBodyPrefab);
        deadBody.enabled = false;
        deadBody.ParentId = target.PlayerId;
        foreach(var r in deadBody.bodyRenderers) target.SetPlayerMaterialColors(r);
        
        target.SetPlayerMaterialColors(deadBody.bloodSplatter);
        Vector3 vector = target.transform.position + killAnim.BodyOffset;
        vector.z = vector.y / 1000f;
        deadBody.transform.position = vector;
        if (isParticipant)
        {
            cam.Locked = true;
            ConsoleJoystick.SetMode_Task();
            if (PlayerControl.LocalPlayer.AmOwner)
            {
                PlayerControl.LocalPlayer.MyPhysics.inputHandler.enabled = true;
            }
        }

        NebulaGameManager.Instance?.AllRoleAction((r) => r.OnDeadBodyGenerated(deadBody));

        target.Die(DeathReason.Kill, false);
        yield return source.MyPhysics.Animations.CoPlayCustomAnimation(killAnim.BlurAnim);

        if (blink)
        {
            source.NetTransform.SnapTo(target.transform.position);
            sourcePhys.Animations.PlayIdleAnimation();
        }

        KillAnimation.SetMovement(source, true);
        KillAnimation.SetMovement(target, true);
        deadBody.enabled = true;
        if (isParticipant)
        {
            cam.Locked = false;
        }
        yield break;
    }
}
