using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class BossHealthBar : MonoBehaviour
{
    const float FillSpeed = 1.5f;

    public GameObject sceneBar;
    public RectTransform sceneFill;
    public float showDistance = 16f;

    Enemy enemy;
    float shownHealth = 1f;

    float Health => enemy.maxHealth > 0f ? Mathf.Clamp01(enemy.Health / enemy.maxHealth) : 0f;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    void OnDisable()
    {
        if (sceneBar != null) sceneBar.SetActive(false);
    }

    void Update()
    {
        if (sceneBar == null || enemy == null) return;
        bool show = enemy.CurrentState == Enemy.State.Active && MageNearby();
        if (sceneBar.activeSelf != show)
        {
            if (show) shownHealth = Health;
            sceneBar.SetActive(show);
        }
        if (show) ApplyFill(sceneFill, ref shownHealth, Health);
    }

    public static void ApplyFill(RectTransform fill, ref float shown, float health)
    {
        if (fill == null) return;
        shown = Mathf.MoveTowards(shown, health, Time.deltaTime * FillSpeed);
        fill.anchorMax = new Vector2(shown, fill.anchorMax.y);
    }

    bool MageNearby()
    {
        foreach (var mage in Mage.All)
            if (mage != null && mage.Alive && Vector2.Distance(mage.transform.position, enemy.Center) < showDistance) return true;
        return false;
    }
}
