using UnityEngine;

[ExecuteAlways]
public class MagicGlow : MonoBehaviour
{
    public Color color = new Color(1f, 0.57f, 0.11f);
    [Range(0f, 15f)] public float intensity = 7.5f;
    [Range(0f, 1f)] public float beat = 0.25f;
    [Range(0f, 5f)] public float beatSpeed = 0.8f;

    public string colorProperty = "_EmissionColor";

    private MaterialPropertyBlock block;
    private Renderer[] renders;

    void OnEnable()
    {
        renders = GetComponentsInChildren<Renderer>(true);
        Apply();
    }

    void OnValidate()
    {
        renders = GetComponentsInChildren<Renderer>(true);
        Apply();
    }

    void Update()
    {
        if (Application.isPlaying && beat > 0f && beatSpeed > 0f) Apply();
    }

    void Apply()
    {
        if (renders == null) return;
        if (block == null) block = new MaterialPropertyBlock();

        float time = Application.isPlaying ? Time.time : 0f;
        float factor = 1f + beat * Mathf.Sin(time * beatSpeed * 2f * Mathf.PI);
        Color glow = color * intensity * factor;
        glow.a = 1f;

        foreach (Renderer render in renders)
        {
            if (render == null) continue;
            render.GetPropertyBlock(block);
            block.SetColor(colorProperty, glow);
            render.SetPropertyBlock(block);
        }
    }
}
