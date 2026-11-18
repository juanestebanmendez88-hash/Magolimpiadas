using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Labyrinth.Editor
{
    public static class BowConfigurator
    {
        const string ScenePath = "Assets/_Magolimpiadas/Escenas/LaberintoMinotauro.unity";
        const string BowName = "Arco MagaNebulosa";

        [MenuItem("Magolimpiadas/Configurar Arco y Maga sin Antorcha")]
        public static void Configure()
        {
            var avatar = LevelConfigurator.PlayerMageAvatar();
            foreach (var path in new[] { LevelConfigurator.PlayerMageIdleFree, LevelConfigurator.PlayerMageWalkFree, LevelConfigurator.PlayerMageRunFree })
                LevelConfigurator.ImportHumanoid(path, avatar, true);
            var control = AssetDatabase.LoadAssetAtPath<AnimatorController>(LevelConfigurator.PlayerMageControlPath);
            LevelConfigurator.AddWithoutTorch(control);
            AssetDatabase.SaveAssets();

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var playerMage = Object.FindAnyObjectByType<PlayerMage>();
            playerMage.torch = playerMage.GetComponentsInChildren<Transform>(true).First(t => t.name == "Antorcha").gameObject;
            EditorUtility.SetDirty(playerMage);

            var group = GameObject.Find(BowName);
            if (group == null) throw new System.InvalidOperationException("No encontré '" + BowName + "' en la escena");
            var children = group.transform.Cast<Transform>().ToList();
            var effects = children.Where(h => h.GetComponentInChildren<ParticleSystem>(true) != null).ToList();
            var bow = children.First(h => !effects.Contains(h) && h.GetComponentInChildren<MeshRenderer>(true) != null);

            var collectible = group.GetComponent<Collectible>();
            if (collectible == null) collectible = group.AddComponent<Collectible>();
            collectible.item = bow;
            collectible.turnOffToCollect = effects.Select(e => e.gameObject).ToArray();
            collectible.lengthOnBack = 1.5f;
            collectible.separation = 0.3f;
            EditorUtility.SetDirty(collectible);

            var rs = bow.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            foreach (var e in effects) b.Encapsulate(e.position);
            var zone = group.GetComponent<CapsuleCollider>();
            if (zone == null) zone = group.AddComponent<CapsuleCollider>();
            zone.isTrigger = true;
            zone.direction = 1;
            zone.center = group.transform.InverseTransformPoint(b.center);
            zone.height = b.size.y / Mathf.Max(0.01f, group.transform.lossyScale.y) + 0.5f;
            zone.radius = (Mathf.Max(b.extents.x, b.extents.z) + 0.8f) / Mathf.Max(0.01f, group.transform.lossyScale.x);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LAB] Arco recolectable: '" + bow.name + "' con " + effects.Count + " efecto(s); zona " + zone.center.ToString("F2") + " alto " + zone.height.ToString("F1") + " radio " + zone.radius.ToString("F1")
                      + " | arco en " + b.center.ToString("F1") + " tamaño " + b.size.ToString("F1"));
        }
    }
}
