using UnityEngine;
using UnityEngine.UI;

public class MageTextIndicator : MonoBehaviour
{
    public Mage mage;
    public Text modeText;
    public Text controlsText;
    public Text healthText;

    public string controls = "Q cambiar modo · E usar poder · Tab cambiar de mago";
    public string secondaryControls = "Clic derecho: hielo";

    Mage listening;

    void OnEnable() { Listen(mage); Refresh(); }
    void OnDisable() { Listen(null); }

    public void Show(Mage isNew)
    {
        mage = isNew;
        Listen(isNew);
        Refresh();
    }

    void Listen(Mage m)
    {
        if (listening != null) { listening.OnChangeMode -= OnChange; listening.OnChangeHealth -= OnChange; }
        listening = m;
        if (listening != null) { listening.OnChangeMode += OnChange; listening.OnChangeHealth += OnChange; }
    }

    void OnChange(Mage m) => Refresh();

    void Refresh()
    {
        if (mage == null || mage.data == null) return;
        string mode = mage.Mode == MageMode.Power ? "PODER" : "ARCO";
        if (modeText != null) modeText.text = mage.data.displayName + " · Modo: " + mode;
        if (controlsText != null)
            controlsText.text = mage.data.hasSecondaryPower ? controls + " · " + secondaryControls : controls;
        if (healthText != null)
        {
            healthText.text = mage.Alive ? "Vida: " + Mathf.CeilToInt(mage.Health) + " / " + Mathf.CeilToInt(mage.MaxHealth) : "¡Caíste! Volviendo al punto de control...";
            healthText.color = mage.Health > mage.MaxHealth * 0.3f ? Color.white : new Color(1f, 0.45f, 0.45f);
        }
    }
}
