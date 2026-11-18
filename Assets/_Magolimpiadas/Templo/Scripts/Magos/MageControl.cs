using UnityEngine;

public struct MageOrders
{
    public float move;
    public bool run;
    public bool jump;
    public bool holdJump;
    public bool attackInput;
    public bool secondaryPower;
    public bool changeMode;
    public bool useZonePower;
    public Vector2 aim;
}

public abstract class MageControl : MonoBehaviour
{
    public abstract MageOrders ReadOrders(Mage mage);
}
