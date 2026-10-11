using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// One-click setup for the push and crouch test in MovementTest: makes the crates pushable and
    /// adds a crawl tunnel that is too low to walk through standing up. Safe to run more than once.
    /// </summary>
    static class MovementTestSetup
    {
        const string ScenePath = "Assets/Scenes/MovementTest.unity";

        [MenuItem("Tools/NoRegRats/Set Up Push And Crouch Test")]
        static void Run()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                EditorUtility.DisplayDialog("Set Up Push And Crouch Test", $"Open {ScenePath} first.", "OK");
                return;
            }

            MakePushable("Crate", 2.5f);
            MakePushable("Crate Stacked", 2.5f);
            MakePushable("Crate Big", 1.5f);
            AddCrawlTunnel();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("MovementTest: crates are pushable and the crawl tunnel is in place.");
        }

        static void MakePushable(string name, float pushSpeed)
        {
            GameObject crate = GameObject.Find(name);
            if (crate == null)
            {
                Debug.LogWarning($"MovementTest setup: no object named \"{name}\".");
                return;
            }

            if (!crate.TryGetComponent(out PushableBox box))
                box = Undo.AddComponent<PushableBox>(crate);
            Undo.RecordObject(crate.GetComponent<Rigidbody2D>(), "Configure crate");
            box.ApplyDefaultBodySettings();

            var serialized = new SerializedObject(box);
            serialized.FindProperty("pushSpeed").floatValue = pushSpeed;
            serialized.ApplyModifiedProperties();
        }

        // A block over the ground between x = 33.5 and 38.5 with a 1.4-unit gap underneath: lower
        // than the standing player (1.8), higher than the crouched one (1.2).
        static void AddCrawlTunnel()
        {
            if (GameObject.Find("Crawl Tunnel") != null)
                return;
            GameObject template = GameObject.Find("Low Ceiling");
            if (template == null)
            {
                Debug.LogWarning("MovementTest setup: no \"Low Ceiling\" to copy the crawl tunnel from.");
                return;
            }

            GameObject tunnel = Object.Instantiate(template, template.transform.parent);
            tunnel.name = "Crawl Tunnel";
            tunnel.transform.localPosition = new Vector3(36f, 1.9f, 0f);
            tunnel.transform.localScale = new Vector3(5f, 1f, 1f);
            Undo.RegisterCreatedObjectUndo(tunnel, "Add crawl tunnel");
        }
    }
}
