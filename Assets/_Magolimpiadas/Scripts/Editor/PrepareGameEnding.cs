using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PrepareGameEnding
{
    const string GameScene = "Assets/_Magolimpiadas/Escenas/LaberintoMinotauro.unity";

    [MenuItem("Magolimpiadas/Preparar nivel 4 y ceremonia final")]
    static void Menu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        All();
    }

    public static void All()
    {
        Debug.Log("[Final] Efectos: " + EffectsBuilder.Prepare());
        var scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
        Debug.Log("[Final] Audio: " + AudioBuilder.Prepare());
        Debug.Log("[Final] Natación artística: " + ArtisticSwimmingBuilder.Prepare(true));
        Debug.Log("[Final] Interfaz del templo: " + TempleHudBuilder.Create());
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Final] Ceremonia: " + FinalCeremonyBuilder.Create());
        EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        Debug.Log("[Final] Listo.");
    }
}
