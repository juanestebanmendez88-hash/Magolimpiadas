using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class SortProject
{
    const string Root = "Assets/_Magolimpiadas";
    const string Packages = "Assets/Paquetes externos";

    static readonly (string fromPoint, string until)[] Movements =
    {
        ("Assets/Laberinto", Root + "/Laberinto"),
        ("Assets/MagOlimpiadas", Root + "/Isla"),
        ("Assets/Templo", Root + "/Templo"),
        ("Assets/PingPong", Root + "/PingPong"),
        ("Assets/Podio", Root + "/Podio"),
        ("Assets/Magos3D", Root + "/Magos3D"),
        ("Assets/Minotauro", Root + "/Minotauro"),
        ("Assets/Sky", Root + "/Cielo"),
        ("Assets/Fonts", Root + "/Fuentes/Otras"),
        ("Assets/Maga Nebulosoa", Root + "/Magos3D/Maga Nebulosoa"),
        ("Assets/Scenes/LaberintoMinotauro.unity", Root + "/Escenas/LaberintoMinotauro.unity"),
        ("Assets/MagoElemental.fbx", Root + "/Magos3D/Modelos/MagoElemental.fbx"),
        ("Assets/Maga_Nebulosa.fbx", Root + "/Magos3D/Modelos/Maga_Nebulosa.fbx"),
        ("Assets/Druida.fbx", Root + "/Magos3D/Modelos/Druida.fbx"),
        ("Assets/Mago_del_Rayo.fbx", Root + "/Magos3D/Modelos/Mago_del_Rayo.fbx"),
        ("Assets/torch.glb", Root + "/Magos3D/Modelos/torch.glb"),
        ("Assets/Excited.fbx", Root + "/Magos3D/Animaciones/Mixamo/Excited.fbx"),
        ("Assets/Sad Idle.fbx", Root + "/Magos3D/Animaciones/Mixamo/Sad Idle.fbx"),
        ("Assets/Swimming (1).fbx", Root + "/Magos3D/Animaciones/Mixamo/Swimming (1).fbx"),
        ("Assets/Swimming To Edge.fbx", Root + "/Magos3D/Animaciones/Mixamo/Swimming To Edge.fbx"),
        ("Assets/Treading Water.fbx", Root + "/Magos3D/Animaciones/Mixamo/Treading Water.fbx"),
        ("Assets/raqueta.glb", Root + "/PingPong/Modelos/raqueta.glb"),
        ("Assets/mesapingpong.glb", Root + "/PingPong/Modelos/mesapingpong.glb"),
        ("Assets/POLYART_Ancient Village/Prefabs/PA_Oldtext.prefab", Root + "/Laberinto/Cortina de runas/PA_Oldtext.prefab"),
        ("Assets/POLYART_Ancient Village/Models/Extra/OldText/PA_Oldtext.fbx", Root + "/Laberinto/Cortina de runas/PA_Oldtext.fbx"),
        ("Assets/POLYART_Ancient Village/Materials/Extra/PA_Oldtext_mtl.mat", Root + "/Laberinto/Cortina de runas/PA_Oldtext_mtl.mat"),
        ("Assets/POLYART_Ancient Village/Textures/Extra/OldText/OldText2.png", Root + "/Laberinto/Cortina de runas/OldText2.png"),
        ("Assets/POLYART_Ancient Village/Shaders/MagicalEffect.shadergraph", Root + "/Laberinto/Cortina de runas/MagicalEffect.shadergraph"),
        ("Assets/Hovl Studio", Packages + "/Hovl Studio"),
        ("Assets/Reversed Interactive", Packages + "/Reversed Interactive"),
        ("Assets/Eric VFX Studio", Packages + "/Eric VFX Studio"),
        ("Assets/portalefectos y mas", Packages + "/portalefectos y mas"),
    };

    static readonly string[] Extra =
    {
        "Assets/TutorialInfo", "Assets/Readme.asset", "Assets/Scenes/SampleScene.unity", "Assets/FireSmall.prefab",
    };

    public static readonly string[] GameScenes =
    {
        Root + "/Escenas/00_Inicio.unity", Root + "/Escenas/LaberintoMinotauro.unity", Root + "/Escenas/99_CeremoniaFinal.unity",
    };

    [MenuItem("Magolimpiadas/Proyecto/Ordenar carpetas")]
    public static void Sort()
    {
        var report = new List<string>();
        foreach (var (fromPoint, until) in Movements)
        {
            if (!Exists(fromPoint)) continue;
            if (Exists(until)) { report.Add("ya existe " + until + " (no muevo " + fromPoint + ")"); continue; }
            CreateFolder(until.Substring(0, until.LastIndexOf('/')));
            string error = AssetDatabase.MoveAsset(fromPoint, until);
            report.Add(string.IsNullOrEmpty(error) ? fromPoint + " -> " + until : "ERROR " + fromPoint + ": " + error);
        }
        foreach (var s in Extra)
            if (Exists(s)) report.Add((AssetDatabase.DeleteAsset(s) ? "borré " : "no pude borrar ") + s);
        if (AssetDatabase.IsValidFolder("Assets/Scenes") && !AssetDatabase.FindAssets("", new[] { "Assets/Scenes" }).Any())
            AssetDatabase.DeleteAsset("Assets/Scenes");
        AssetDatabase.Refresh();
        PutScenesInBuild();
        Debug.Log("[Ordenar] " + string.Join("\n", report));
    }

    public static void PutScenesInBuild()
    {
        EditorBuildSettings.scenes = GameScenes
            .Where(p => AssetDatabase.LoadAssetAtPath<SceneAsset>(p) != null)
            .Select(p => new EditorBuildSettingsScene(p, true))
            .ToArray();
    }

    static bool Exists(string path) => AssetDatabase.IsValidFolder(path) || !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path, AssetPathToGUIDOptions.OnlyExistingAssets));

    static void CreateFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int i = path.LastIndexOf('/');
        string parent = path.Substring(0, i);
        CreateFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(i + 1));
    }
}
