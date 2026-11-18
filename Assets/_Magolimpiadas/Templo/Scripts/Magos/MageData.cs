using UnityEngine;

public enum MagicElement { None, Fire, Nebula, Plant, Lightning, Ice }

[System.Serializable]
public class SpriteAnimation
{
    public Sprite[] frames;
    public float fps = 10f;
    public bool repeat = true;

    public bool Empty => frames == null || frames.Length == 0;
    public float Duration => Empty ? 0f : frames.Length / Mathf.Max(0.01f, fps);
}

[System.Serializable]
public class ProjectileData
{
    public MagicElement element = MagicElement.Nebula;
    public Color color = new Color(0.7f, 0.35f, 1f);
    public float speed = 14f;
    public float reach = 12f;
    public float size = 0.6f;
    public float damage = 10f;
    public bool slows = false;
}

[CreateAssetMenu(menuName = "Magolimpiadas/Datos de mago", fileName = "DatosMago")]
public class MageData : ScriptableObject
{
    public string displayName = "Maga Nebulosa";
    public MagicElement element = MagicElement.Nebula;
    public Color color = new Color(0.7f, 0.35f, 1f);

    public float maxHealth = 100f;

    public float walkSpeed = 4.5f;
    public float runSpeed = 7.5f;
    public float acceleration = 60f;
    public float airAcceleration = 35f;
    public float jumpHeight = 3.2f;
    public float gravity = 30f;
    public float fallMultiplier = 1.5f;
    public float maxFallSpeed = 20f;
    public float jumpMargin = 0.12f;
    public float jumpMemory = 0.12f;

    public SpriteAnimation idle;
    public SpriteAnimation walk;
    public SpriteAnimation run;
    public SpriteAnimation air;
    public SpriteAnimation attack;
    public int attackLaunchFrame = 3;

    public SpriteAnimation climb;

    public SpriteAnimation bowIdle;
    public SpriteAnimation walkBow;
    public SpriteAnimation runBow;
    public SpriteAnimation airBow;
    public SpriteAnimation bowShot;
    public int arrowLaunchFrame = 4;

    public ProjectileData power = new ProjectileData();
    public float attackWait = 0.25f;
    public Vector2 shotPoint = new Vector2(0.9f, 1.15f);

    public bool hasSecondaryPower = false;
    public ProjectileData secondaryPower = new ProjectileData { element = MagicElement.Ice, color = new Color(0.55f, 0.85f, 1f), slows = true };
    public SpriteAnimation secondaryAttack;
    public int secondaryLaunchFrame = 3;

    public Sprite arrow;
    public float arrowSpeed = 20f;
    public float arrowReach = 18f;
}
