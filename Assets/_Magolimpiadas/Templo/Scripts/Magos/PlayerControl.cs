using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControl : MageControl
{
    public Camera sceneCamera;

    public override MageOrders ReadOrders(Mage mage)
    {
        var o = new MageOrders();
        var t = Keyboard.current;
        var m = Mouse.current;

        if (t != null)
        {
            if (t.aKey.isPressed) o.move -= 1f;
            if (t.dKey.isPressed) o.move += 1f;
            o.run = t.leftShiftKey.isPressed || t.rightShiftKey.isPressed;
            o.jump = t.spaceKey.wasPressedThisFrame || t.wKey.wasPressedThisFrame;
            o.holdJump = t.spaceKey.isPressed || t.wKey.isPressed;
            o.changeMode = t.qKey.wasPressedThisFrame;
            o.useZonePower = t.eKey.wasPressedThisFrame;
        }

        o.aim = mage.ShotPoint + (mage.FacingRight ? Vector2.right : Vector2.left);
        if (m != null)
        {
            o.attackInput = m.leftButton.wasPressedThisFrame;
            o.secondaryPower = m.rightButton.wasPressedThisFrame;
            Camera cam = sceneCamera != null ? sceneCamera : Camera.main;
            if (cam != null)
            {
                Vector3 p = m.position.ReadValue();
                p.z = -cam.transform.position.z;
                o.aim = cam.ScreenToWorldPoint(p);
            }
        }
        return o;
    }
}
