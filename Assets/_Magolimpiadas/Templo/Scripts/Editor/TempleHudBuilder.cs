using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class TempleHudBuilder
{
    const string UI = "Assets/_Magolimpiadas/Arte/UI";
    const string Fantasy = UI + "/FantasyUI";
    const string TitleFont = "Assets/_Magolimpiadas/Fuentes/TMP/F_Titulos.asset";
    const string TextFont = "Assets/_Magolimpiadas/Fuentes/TMP/F_Texto.asset";
    const string Heart = UI + "/Iconos/Corazon_Pixel.png";
    const string Name = "HUD Templo";
    static readonly Color Golden = new Color(1f, 0.84f, 0.38f);
    static readonly Color Lavender = new Color(0.86f, 0.82f, 1f);

    [MenuItem("Magolimpiadas/Templo 2D/Interfaz con imágenes")]
    static void Menu() => Debug.Log("[HUD Templo] " + Create() + " Guarda la escena (Ctrl+S).");

    public static string Create()
    {
        Edges(Fantasy + "/slots/slot_common.png", 28);
        Edges(Fantasy + "/slots/slot_selected.png", 28);
        var heart = CreateHeart();
        var fT = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitleFont);
        var fX = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TextFont);
        var panel = Load(Fantasy + "/panels/tooltip.png");
        var key = Load(Fantasy + "/slots/slot_common.png");
        var active = Load(Fantasy + "/slots/slot_selected.png");

        var old = Object.FindObjectsByType<TempleHud>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        var go = new GameObject(Name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Undo.RegisterCreatedObjectUndo(go, "HUD Templo");
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 101;
        var esc = go.GetComponent<CanvasScaler>();
        esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        esc.referenceResolution = new Vector2(1920, 1080);
        esc.matchWidthOrHeight = 0.5f;
        var hud = go.AddComponent<TempleHud>();
        var r = go.transform;
        hud.font = fX;
        hud.normalBackground = key;
        hud.activeBackground = active;
        hud.star = Load(UI + "/Iconos/Star.png");
        hud.arrows = AssetDatabase.LoadAllAssetsAtPath("Assets/_Magolimpiadas/Templo/assets/objetos/flecha_der.png").OfType<Sprite>().OrderBy(s => s.name).ToArray();

        var pm = Box("Panel mago", r, panel, new Vector2(0f, 1f), new Vector2(18f, -14f), new Vector2(742f, 112f));
        hud.magePanel = pm;
        var frame = Box("Marco retrato", pm, Load(Fantasy + "/panels/portrait_frame.png"), new Vector2(0f, 1f), new Vector2(10f, -8f), new Vector2(96f, 96f));
        var mask = Box("Máscara", frame, null, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70f, 70f));
        mask.gameObject.AddComponent<RectMask2D>();
        mask.GetComponent<Image>().color = new Color(0.12f, 0.1f, 0.22f);
        hud.portrait = Box("Retrato", mask, null, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70f, 70f)).GetComponent<Image>();
        hud.portrait.preserveAspect = true;
        hud.displayName = Txt("Nombre", pm, fT, 34f, Color.white, "Maga Nebulosa", new Vector2(116f, -8f), new Vector2(290f, 42f), TextAlignmentOptions.Left);
        var heartIc = Box("Corazón", pm, heart, new Vector2(0f, 1f), new Vector2(116f, -60f), new Vector2(32f, 30f));
        heartIc.GetComponent<Image>().preserveAspect = true;
        var track = Box("Barra de vida", pm, Load(Fantasy + "/controls/slider_track.png"), new Vector2(0f, 1f), new Vector2(152f, -58f), new Vector2(250f, 34f));
        track.GetComponent<Image>().type = Image.Type.Sliced;
        var fill = Box("Relleno", track, Load(Fantasy + "/controls/slider_fill.png"), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one; fill.pivot = new Vector2(0f, 0.5f);
        fill.offsetMin = new Vector2(10f, 9f); fill.offsetMax = new Vector2(-10f, -9f);
        var fillImg = fill.GetComponent<Image>();
        fillImg.type = Image.Type.Sliced;
        fillImg.color = new Color(0.35f, 0.9f, 1f);
        var container = new GameObject("Contenedor", typeof(RectTransform)).GetComponent<RectTransform>();
        container.SetParent(track, false);
        container.anchorMin = Vector2.zero; container.anchorMax = Vector2.one;
        container.offsetMin = new Vector2(10f, 9f); container.offsetMax = new Vector2(-10f, -9f);
        fill.SetParent(container, false);
        fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one; fill.offsetMin = fill.offsetMax = Vector2.zero;
        hud.healthFill = fill;
        hud.healthImage = fillImg;
        hud.healthText = Txt("Texto vida", track, fX, 20f, Color.white, "100/100", Vector2.zero, new Vector2(250f, 34f), TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f));
        var sep = Box("Separador", pm, null, new Vector2(0f, 1f), new Vector2(414f, -14f), new Vector2(3f, 84f));
        sep.GetComponent<Image>().color = new Color(Golden.r, Golden.g, Golden.b, 0.5f);
        CreateKeyIcon(pm, key, "Q", new Vector2(426f, -30f), new Vector2(50f, 50f), fT);
        hud.bowBackground = Box("Arco", pm, key, new Vector2(0f, 1f), new Vector2(484f, -24f), new Vector2(120f, 62f)).GetComponent<Image>();
        hud.bowBackground.type = Image.Type.Sliced;
        hud.bowIcon = Icon(hud.bowBackground.transform, null, new Vector2(8f, -13f), new Vector2(36f, 36f));
        hud.bowText = Txt("Texto", hud.bowBackground.transform, fX, 24f, Color.white, "Arco", new Vector2(48f, -12f), new Vector2(64f, 38f), TextAlignmentOptions.Left);
        Shrink(hud.bowText, 16f);
        hud.powerBackground = Box("Poder", pm, active, new Vector2(0f, 1f), new Vector2(610f, -24f), new Vector2(120f, 62f)).GetComponent<Image>();
        hud.powerBackground.type = Image.Type.Sliced;
        hud.powerIcon = Icon(hud.powerBackground.transform, null, new Vector2(8f, -13f), new Vector2(36f, 36f));
        hud.powerText = Txt("Texto", hud.powerBackground.transform, fX, 24f, Color.white, "Poder", new Vector2(48f, -12f), new Vector2(64f, 38f), TextAlignmentOptions.Left);
        Shrink(hud.powerText, 16f);
        Shrink(hud.displayName, 22f);

        var pa = Box("Panel acción", r, panel, new Vector2(0f, 1f), new Vector2(18f, -132f), new Vector2(470f, 58f));
        hud.actionGroup = pa.gameObject.AddComponent<CanvasGroup>();
        CreateKeyIcon(pa, key, "E", new Vector2(8f, -7f), new Vector2(44f, 44f), fT);
        var gear = Icon(pa, Load(UI + "/Iconos/Gear.png"), new Vector2(62f, -12f), new Vector2(34f, 34f));
        gear.color = Golden;
        hud.actionText = Txt("Texto acción", pa, fX, 26f, Color.white, "Poder de zona", new Vector2(106f, -9f), new Vector2(350f, 40f), TextAlignmentOptions.Left);
        Shrink(hud.actionText, 18f);
        var ps = Box("Panel hielo", r, panel, new Vector2(0f, 1f), new Vector2(18f, -196f), new Vector2(330f, 52f));
        var mouse = Icon(ps, Load(UI + "/Iconos/Mouse-Right.png"), new Vector2(10f, -8f), new Vector2(36f, 36f));
        mouse.color = Lavender;
        Txt("Texto hielo", ps, fX, 24f, new Color(0.7f, 0.92f, 1f), "Clic derecho · Hielo", new Vector2(54f, -8f), new Vector2(270f, 36f), TextAlignmentOptions.Left);
        hud.secondaryPanel = ps.gameObject;
        hud.leftColumn = pa;

        var pc = Box("Consejo", r, panel, new Vector2(0f, 1f), new Vector2(776f, -14f), new Vector2(680f, 110f));
        hud.tipPanel = pc;
        hud.tipGroup = pc.gameObject.AddComponent<CanvasGroup>();
        hud.mainTip = Txt("Principal", pc, fX, 28f, Golden, "", new Vector2(0f, -14f), new Vector2(640f, 42f), TextAlignmentOptions.Center, new Vector2(0.5f, 1f));
        Stretch(hud.mainTip.rectTransform, 20f);
        hud.mainTip.enableAutoSizing = true;
        hud.mainTip.fontSizeMin = 18f;
        hud.mainTip.fontSizeMax = 28f;
        var divisor = Box("Divisor", pc, Load(Fantasy + "/banners/divider.png"), new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(400f, 26f));
        divisor.GetComponent<Image>().preserveAspect = true;
        hud.tipDivider = divisor.gameObject;
        hud.secondaryTip = Txt("Secundario", pc, fX, 22f, Lavender, "", new Vector2(0f, -78f), new Vector2(640f, 30f), TextAlignmentOptions.Center, new Vector2(0.5f, 1f));
        Stretch(hud.secondaryTip.rectTransform, 20f);
        hud.secondaryTip.enableAutoSizing = true;
        hud.secondaryTip.fontSizeMin = 16f;
        hud.secondaryTip.fontSizeMax = 22f;

        var coinsPanelRect = Box("Panel monedas", r, panel, new Vector2(1f, 1f), new Vector2(-18f, -14f), new Vector2(300f, 206f));
        hud.coinsPanel = coinsPanelRect;
        var coin = AssetDatabase.LoadAllAssetsAtPath("Assets/_Magolimpiadas/Templo/assets/objetos/moneda.png").OfType<Sprite>().OrderBy(s => s.name.Length).ThenBy(s => s.name).FirstOrDefault();
        Icon(coinsPanelRect, coin, new Vector2(16f, -12f), new Vector2(36f, 36f));
        Txt("Título", coinsPanelRect, fT, 32f, Golden, "Monedas", new Vector2(60f, -10f), new Vector2(220f, 40f), TextAlignmentOptions.Left);
        var line = Box("Línea", coinsPanelRect, null, new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(260f, 2f));
        line.GetComponent<Image>().color = new Color(Golden.r, Golden.g, Golden.b, 0.45f);
        var rows = new GameObject("Filas", typeof(RectTransform)).GetComponent<RectTransform>();
        rows.SetParent(coinsPanelRect, false);
        rows.anchorMin = rows.anchorMax = rows.pivot = new Vector2(0f, 1f);
        rows.anchoredPosition = new Vector2(6f, -62f);
        rows.sizeDelta = new Vector2(288f, 140f);
        hud.coinsRows = rows;

        var down = new GameObject("Abajo", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
        down.SetParent(r, false);
        down.anchorMin = Vector2.zero; down.anchorMax = Vector2.one; down.offsetMin = down.offsetMax = Vector2.zero;
        hud.bottomGroup = down.GetComponent<CanvasGroup>();
        hud.bottomGroup.interactable = false;
        hud.bottomGroup.blocksRaycasts = false;
        var pt = Box("Panel Tab", down, panel, new Vector2(0f, 0f), new Vector2(18f, 14f), new Vector2(420f, 60f));
        CreateKeyIcon(pt, key, "Tab", new Vector2(8f, -8f), new Vector2(70f, 44f), fT);
        hud.nextPortrait = Icon(pt, null, new Vector2(86f, -8f), new Vector2(44f, 44f));
        hud.nextPortrait.preserveAspect = true;
        Txt("Texto", pt, fX, 26f, Color.white, "Cambiar de mago", new Vector2(140f, -10f), new Vector2(270f, 40f), TextAlignmentOptions.Left);
        var pe = Box("Panel Esc", down, panel, new Vector2(1f, 0f), new Vector2(-18f, 14f), new Vector2(540f, 60f));
        CreateKeyIcon(pe, key, "Esc", new Vector2(8f, -8f), new Vector2(70f, 44f), fT);
        Icon(pe, Load(Fantasy + "/menu_icons/icon_back.png"), new Vector2(86f, -11f), new Vector2(38f, 38f));
        Txt("Texto", pe, fX, 26f, Color.white, "Volver al mundo principal", new Vector2(134f, -10f), new Vector2(396f, 40f), TextAlignmentOptions.Left);

        string report = "creé \"" + Name + "\"";
        var world = Object.FindFirstObjectByType<WorldSwitcher>(FindObjectsInactive.Include);
        if (world != null)
        {
            Undo.RecordObject(world, "HUD Templo");
            var list = (world.items2D ?? new GameObject[0]).Where(o => o != null && o.GetComponent<TempleHud>() == null).ToList();
            list.Add(go);
            world.items2D = list.ToArray();
            world.exitNotice = "";
            EditorUtility.SetDirty(world);
            report += "; se enciende con el templo";
        }
        var indicator = Object.FindFirstObjectByType<MageTextIndicator>(FindObjectsInactive.Include);
        if (indicator != null)
        {
            foreach (var t in new[] { indicator.modeText, indicator.controlsText, indicator.healthText })
                if (t != null) { Undo.RecordObject(t.gameObject, "HUD Templo"); t.gameObject.SetActive(false); }
            report += "; apagué los textos de IndicadorMagoTexto";
        }
        foreach (var c in Object.FindObjectsByType<CoinCounter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(c, "HUD Templo");
            c.enabled = false;
        }
        go.SetActive(false);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return report + ".";
    }

    static Sprite Load(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();

    static RectTransform Box(string displayName, Transform parent, Sprite s, Vector2 anchor, Vector2 pos, Vector2 sizeValue)
    {
        var go = new GameObject(displayName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = s;
        img.raycastTarget = false;
        if (s != null && s.border.sqrMagnitude > 0f) img.type = Image.Type.Sliced;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeValue;
        return rt;
    }

    static Image Icon(Transform parent, Sprite s, Vector2 pos, Vector2 sizeValue)
    {
        var img = Box("Icono", parent, s, new Vector2(0f, 1f), pos, sizeValue).GetComponent<Image>();
        img.preserveAspect = true;
        img.type = Image.Type.Simple;
        return img;
    }

    static void CreateKeyIcon(Transform parent, Sprite background, string letter, Vector2 pos, Vector2 sizeValue, TMP_FontAsset f)
    {
        var t = Box("Tecla " + letter, parent, background, new Vector2(0f, 1f), pos, sizeValue);
        Txt("Letra", t, f, letter.Length > 1 ? 24f : 30f, Golden, letter, Vector2.zero, sizeValue, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f));
    }

    static TextMeshProUGUI Txt(string displayName, Transform parent, TMP_FontAsset f, float sizeValue, Color c, string text, Vector2 pos, Vector2 box,
                               TextAlignmentOptions align, Vector2? anchor = null)
    {
        var go = new GameObject(displayName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (f != null) t.font = f;
        t.fontSize = sizeValue;
        t.color = c;
        t.text = text;
        t.alignment = align;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.outlineWidth = 0.2f;
        t.outlineColor = new Color32(15, 8, 35, 255);
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor ?? new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        return t;
    }

    static void Shrink(TMP_Text t, float min)
    {
        if (t == null) return;
        t.enableAutoSizing = true;
        t.fontSizeMax = t.fontSize;
        t.fontSizeMin = min;
        t.overflowMode = TextOverflowModes.Ellipsis;
    }

    static void Stretch(RectTransform rt, float margin)
    {
        float heightValue = rt.sizeDelta.y;
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(-2f * margin, heightValue);
    }

    static void Edges(string path, int edge)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null || ti.spriteBorder.x > 0f) return;
        ti.spriteBorder = new Vector4(edge, edge, edge, edge);
        ti.SaveAndReimport();
    }

    static Sprite CreateHeart()
    {
        string[] drawing =
        {
            ".OO...OO.",
            "ORRO.ORRO",
            "ORWRRRRRO",
            "ORRRRRRRO",
            ".ORRRRRO.",
            "..ORRRO..",
            "...ORO...",
            "....O....",
        };
        if (AssetDatabase.LoadAssetAtPath<Sprite>(Heart) != null) return AssetDatabase.LoadAssetAtPath<Sprite>(Heart);
        int w = drawing[0].Length, h = drawing.Length;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                char c = drawing[h - 1 - y][x];
                tex.SetPixel(x, y, c == 'O' ? new Color(0.24f, 0.04f, 0.1f) : c == 'R' ? new Color(0.93f, 0.22f, 0.32f) : c == 'W' ? Color.white : Color.clear);
            }
        tex.Apply();
        File.WriteAllBytes(Heart, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(Heart, ImportAssetOptions.ForceUpdate);
        var ti = (TextureImporter)AssetImporter.GetAtPath(Heart);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(Heart);
    }
}
