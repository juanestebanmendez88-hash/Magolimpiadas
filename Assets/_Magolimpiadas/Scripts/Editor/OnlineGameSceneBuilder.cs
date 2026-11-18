using Fusion;
using ArcaneGames.Network;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArcaneGames.EditorTools
{
    public static class OnlineGameSceneBuilder
    {
        private const string GameScenePath = MageAssetTools.ScenesFolder + "/LaberintoMinotauro.unity";
        private const string TrialsObjectName = "Pruebas en línea";
        private const string SharedWorldObjectName = "Mundo en línea";

        [MenuItem("Juegos Arcanos/5. Preparar la escena del juego para el modo en línea")]
        public static void PrepareGameScene()
        {
            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var minotaur = Object.FindFirstObjectByType<Minotaur>(FindObjectsInactive.Include);
            if (minotaur == null)
            {
                Debug.LogError("[JuegosArcanos] The game scene has no Minotaur.");
                return;
            }

            AddIfMissing<NetworkObject>(minotaur.gameObject);
            AddIfMissing<MinotaurSync>(minotaur.gameObject);
            var interpolatedTransform = minotaur.GetComponent<NetworkTransform>();
            if (interpolatedTransform != null)
                Undo.DestroyObjectImmediate(interpolatedTransform);

            var trials = Object.FindFirstObjectByType<TrialDirector>(FindObjectsInactive.Include);
            GameObject trialsObject = trials != null ? trials.gameObject : new GameObject(TrialsObjectName);
            if (trials == null)
                Undo.RegisterCreatedObjectUndo(trialsObject, "Create online trials");
            AddIfMissing<NetworkObject>(trialsObject);
            AddIfMissing<TrialDirector>(trialsObject);
            AddIfMissing<TempleSync>(trialsObject);
            AddIfMissing<PingPongSync>(trialsObject);
            AddIfMissing<SwimRaceSync>(trialsObject);
            AddIfMissing<ArtisticSwimmingSync>(trialsObject);

            var arrival = Object.FindFirstObjectByType<IslandArrivalSync>(FindObjectsInactive.Include);
            GameObject sharedWorldObject = arrival != null ? arrival.gameObject : new GameObject(SharedWorldObjectName);
            if (arrival == null)
                Undo.RegisterCreatedObjectUndo(sharedWorldObject, "Create shared world sync");
            AddIfMissing<NetworkObject>(sharedWorldObject);
            AddIfMissing<IslandArrivalSync>(sharedWorldObject);
            AddIfMissing<PickupSync>(sharedWorldObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[JuegosArcanos] Game scene prepared for online play.");
        }

        private static void AddIfMissing<T>(GameObject target) where T : Component
        {
            if (target.GetComponent<T>() == null)
                Undo.AddComponent<T>(target);
        }
    }
}
