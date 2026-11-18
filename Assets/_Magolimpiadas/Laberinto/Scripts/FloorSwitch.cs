using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class FloorSwitch : MonoBehaviour
{
    public Transform lever;
    public Vector3 activatedRotation = new Vector3(0f, 0f, 25f);
    public float leverDuration = 0.5f;
    public bool pivotAtBase = false;

    public Renderer[] partsWithColor;
    public Color offColor = new Color(0.85f, 0.12f, 0.08f);
    public Color activatedColor = new Color(0.15f, 1f, 0.3f);
    [Range(0f, 8f)] public float glow = 0.9f;

    public GameObject[] activate;
    public float appearanceDuration = 1f;
    public UnityEvent onActivate;

    public bool Activated { get; private set; }

    private Material[] materials;

    void Start()
    {
        materials = new Material[partsWithColor.Length];
        for (int i = 0; i < partsWithColor.Length; i++)
            if (partsWithColor[i] != null) materials[i] = partsWithColor[i].material;
        PutColor(offColor);

        foreach (var item in activate)
            if (item != null) item.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!Activated && other.GetComponent<PlayerMage>() != null) Activate();
    }

    public void Activate()
    {
        if (Activated) return;
        Activated = true;
        StartCoroutine(LowerLever());
        PutColor(activatedColor);
        foreach (var item in activate)
            if (item != null) StartCoroutine(Appear(item));
        onActivate?.Invoke();
    }

    IEnumerator LowerLever()
    {
        if (lever == null) yield break;
        float half = pivotAtBase ? 0f : lever.localScale.y * 0.5f;
        Quaternion initialRotation = lever.localRotation;
        Quaternion finalRotation = Quaternion.Euler(activatedRotation);
        Vector3 hinge = lever.localPosition - initialRotation * Vector3.up * half;

        for (float t = 0f; t < 1f; t += Time.deltaTime / leverDuration)
        {
            Quaternion rotation = Quaternion.Slerp(initialRotation, finalRotation, Mathf.SmoothStep(0f, 1f, t));
            lever.localRotation = rotation;
            lever.localPosition = hinge + rotation * Vector3.up * half;
            yield return null;
        }
        lever.localRotation = finalRotation;
        lever.localPosition = hinge + finalRotation * Vector3.up * half;
    }

    IEnumerator Appear(GameObject item)
    {
        Vector3 scale = item.transform.localScale;
        item.transform.localScale = Vector3.zero;
        item.SetActive(true);
        for (float t = 0f; t < 1f; t += Time.deltaTime / appearanceDuration)
        {
            float s = 1f - Mathf.Pow(1f - t, 3f);
            item.transform.localScale = scale * s;
            yield return null;
        }
        item.transform.localScale = scale;
    }

    void PutColor(Color color)
    {
        if (materials == null) return;
        foreach (var m in materials)
        {
            if (m == null) continue;
            m.SetColor("_BaseColor", color);
            m.SetColor("_EmissionColor", color * glow);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
    }
}
