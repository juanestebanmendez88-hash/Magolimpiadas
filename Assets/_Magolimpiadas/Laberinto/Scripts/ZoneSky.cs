using System.Collections.Generic;
using UnityEngine;

public class ZoneSky : MonoBehaviour
{
    public Transform follow;

    public List<Bounds> labyrinthZones = new List<Bounds>();

    public List<GameObject> labyrinths = new List<GameObject>();

    public Material labyrinthSky;
    public Color labyrinthAmbience = new Color(0.24f, 0.26f, 0.38f);
    public float labyrinthSun = 0.35f;
    public Color labyrinthSunColor = new Color(0.62f, 0.7f, 1f);

    public Material islandSky;
    public Color islandAmbience = new Color(0.56f, 0.53f, 0.62f);
    public float islandSun = 1.5f;
    public Color islandSunColor = new Color(1f, 0.93f, 0.83f);

    public Camera sceneCamera;
    public float viewDistance = 85f;
    public bool fog = false;
    public Color fogColor = new Color(0.06f, 0.07f, 0.14f);
    public float fogStart = 35f;
    public float fogEnd = 80f;

    public Light sun;
    public float transitionDuration = 1.5f;

    int zone = -1;
    float blend;

    public bool InLabyrinth => zone == 1;

    float islandView;
    bool islandFog;
    FogMode islandFogMode;
    Color islandFogColor;
    float islandFogStart, islandFogEnd;

    void Start()
    {
        if (sceneCamera == null) sceneCamera = Camera.main;
        islandView = sceneCamera != null ? sceneCamera.farClipPlane : 1000f;
        islandFog = RenderSettings.fog;
        islandFogMode = RenderSettings.fogMode;
        islandFogColor = RenderSettings.fogColor;
        islandFogStart = RenderSettings.fogStartDistance;
        islandFogEnd = RenderSettings.fogEndDistance;

        int z = CurrentZone();
        blend = z;
        Apply(z, true);
    }

    void Update()
    {
        int z = CurrentZone();
        if (z != zone) Apply(z, false);
        float meta = zone;
        if (!Mathf.Approximately(blend, meta))
        {
            blend = Mathf.MoveTowards(blend, meta, Time.deltaTime / Mathf.Max(0.01f, transitionDuration));
            ApplyLight();
        }
    }

    int CurrentZone()
    {
        if (follow == null) return zone < 0 ? 0 : zone;
        Vector3 p = follow.position;
        foreach (var b in labyrinthZones)
        {
            Vector3 q = p; q.y = b.center.y;
            if (b.Contains(q)) return 1;
        }
        return 0;
    }

    void Apply(int newValue, bool instantly)
    {
        zone = newValue;
        var sky = newValue == 1 ? labyrinthSky : islandSky;
        if (sky != null && RenderSettings.skybox != sky)
        {
            RenderSettings.skybox = sky;
            DynamicGI.UpdateEnvironment();
        }
        ShowLabyrinths(newValue == 1);
        if (instantly) { blend = newValue; ApplyLight(); }
    }

    readonly List<Renderer> hidden = new List<Renderer>();

    void ShowLabyrinths(bool visible)
    {
        if (visible)
        {
            foreach (var r in hidden) if (r != null) r.forceRenderingOff = false;
            hidden.Clear();
            return;
        }
        foreach (var l in labyrinths)
        {
            if (l == null) continue;
            foreach (var r in l.GetComponentsInChildren<Renderer>(true))
            {
                if (r.forceRenderingOff) continue;
                if (r.GetComponentInParent<BotMage3D>(true) != null || r.GetComponentInParent<PlayerMage>(true) != null) continue;
                r.forceRenderingOff = true;
                hidden.Add(r);
            }
        }
    }

    void ApplyLight()
    {
        float s = Mathf.SmoothStep(0f, 1f, blend);
        RenderSettings.ambientLight = Color.Lerp(islandAmbience, labyrinthAmbience, s);
        if (sun != null)
        {
            sun.intensity = Mathf.Lerp(islandSun, labyrinthSun, s);
            sun.color = Color.Lerp(islandSunColor, labyrinthSunColor, s);
        }

        if (sceneCamera != null) sceneCamera.farClipPlane = Mathf.Lerp(islandView, Mathf.Min(viewDistance, islandView), s);
        if (fog && s > 0.001f)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = islandFog ? Color.Lerp(islandFogColor, fogColor, s) : fogColor;
            RenderSettings.fogStartDistance = Mathf.Lerp(islandView, fogStart, s);
            RenderSettings.fogEndDistance = Mathf.Lerp(islandView * 1.5f, fogEnd, s);
        }
        else
        {
            RenderSettings.fog = islandFog;
            RenderSettings.fogMode = islandFogMode;
            RenderSettings.fogColor = islandFogColor;
            RenderSettings.fogStartDistance = islandFogStart;
            RenderSettings.fogEndDistance = islandFogEnd;
        }
    }
}
