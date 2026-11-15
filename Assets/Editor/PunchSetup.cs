using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Switches the hero from shooting to punching: the Player prefab loses the old shooter (whose
    /// script is gone) and gets PlayerPunch, and the Rat King prefab stops a step short of the hero
    /// so he can be punched without standing in his bite. The same goes for any Player or Rat King
    /// in Level1 and MovementTest that carries its own copy of those values. Safe to run again.
    /// </summary>
    public static class PunchSetup
    {
        const string KingPrefabPath = "Assets/Prefabs/Enemies/RatKing.prefab";
        const float KingStopDistance = 1.7f;
        static readonly string[] Scenes = { "Assets/Scenes/Level1.unity", "Assets/Scenes/MovementTest.unity" };

        [MenuItem("Tools/NoRegRats/Set Up Punching")]
        public static void Run()
        {
            CombatSetup.SetUpPlayer();

            GameObject king = PrefabUtility.LoadPrefabContents(KingPrefabPath);
            try
            {
                SetStopDistance(king.GetComponent<RatKing>());
                PrefabUtility.SaveAsPrefabAsset(king, KingPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(king);
            }

            foreach (string path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                // Prefab instances lost the shooter with their prefab; this catches plain objects.
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                        if (!PrefabUtility.IsPartOfPrefabInstance(t.gameObject) && GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0)
                            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                foreach (RatKing sceneKing in Object.FindObjectsByType<RatKing>(FindObjectsInactive.Include))
                    SetStopDistance(sceneKing);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("PunchSetup: done.");
        }

        static void SetStopDistance(RatKing king)
        {
            var serialized = new SerializedObject(king);
            serialized.FindProperty("stopDistance").floatValue = KingStopDistance;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
