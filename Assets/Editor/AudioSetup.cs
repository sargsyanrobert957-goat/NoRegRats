using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Assigns the sound effects and music in Assets/Audio/ to the scene's Sfx and Music components.
    /// Safe to run more than once: it only sets the clip lists and music tracks (volumes and
    /// anything else you changed in the Inspector are kept), and adds a Sfx entry for any sound the
    /// scene doesn't have yet (older scenes were saved before Gunshot existed).
    /// Runs on MovementTest; Level1 has its own menu item.
    /// </summary>
    public static class AudioSetup
    {
        const string SfxFolder = "Assets/Audio/SFX/";
        const string MusicFolder = "Assets/Audio/Music/";

        // Sound -> clip files. A sound with several clips plays one at random each time.
        static readonly (Sfx.Sound sound, float defaultVolume, string[] files)[] SfxClips =
        {
            (Sfx.Sound.Jump, 0.7f, new[] { "Jump.ogg" }),
            (Sfx.Sound.Land, 0.6f, new[] { "Land.ogg" }),
            (Sfx.Sound.Step, 0.35f, new[] { "Step.ogg" }),
            (Sfx.Sound.Punch, 0.7f, new[] { "Punch.ogg" }),
            (Sfx.Sound.Hit, 0.8f, new[] { "Hit.ogg" }),
            (Sfx.Sound.RatPoof, 0.8f, new[] { "RatPoof.ogg" }),
            (Sfx.Sound.Hurt, 0.9f, new[] { "Hurt.ogg" }),
            (Sfx.Sound.KingThrow, 0.8f, new[] { "KingThrow.ogg" }),
            (Sfx.Sound.KingDefeated, 1f, new[] { "KingDefeated.ogg" }),
            (Sfx.Sound.Heart, 1f, new[] { "Heart.ogg" }),
            (Sfx.Sound.Gate, 0.8f, new[] { "Gate.ogg" }),
            (Sfx.Sound.Click, 0.6f, new[] { "Click1.ogg", "Click2.ogg" }),
            (Sfx.Sound.Gunshot, 0.8f, new[] { "Gunshot.ogg" }),
            (Sfx.Sound.Push, 0.6f, new[] { "Push.ogg" }),
        };

        [MenuItem("Tools/NoRegRats/Assign Audio (MovementTest)")]
        public static void RunMovementTest() => Run("Assets/Scenes/MovementTest.unity");

        [MenuItem("Tools/NoRegRats/Assign Audio (Level1)")]
        public static void RunLevel1() => Run("Assets/Scenes/Level1.unity");

        static void Run(string scenePath)
        {
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(scenePath);
            var sfx = Object.FindAnyObjectByType<Sfx>();
            var music = Object.FindAnyObjectByType<Music>();
            if (sfx == null || music == null)
            {
                Debug.LogError("AudioSetup: no Sfx / Music in " + scenePath + ". Run Tools > NoRegRats > Set Up Effects And Audio first.");
                return;
            }

            int missing = 0;
            AssignSfx(sfx, ref missing);
            AssignMusic(music, ref missing);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("AudioSetup: done for " + scenePath + (missing > 0 ? " (" + missing + " clip(s) missing, see warnings)" : "."));
        }

        static void AssignSfx(Sfx sfx, ref int missing)
        {
            var serialized = new SerializedObject(sfx);
            SerializedProperty list = serialized.FindProperty("sounds");

            foreach (var (sound, defaultVolume, files) in SfxClips)
            {
                SerializedProperty entry = FindEntry(list, sound);
                if (entry == null)
                {
                    list.arraySize++;
                    entry = list.GetArrayElementAtIndex(list.arraySize - 1);
                    entry.FindPropertyRelative("sound").enumValueIndex = (int)sound;
                    entry.FindPropertyRelative("volume").floatValue = defaultVolume;
                }

                var clips = new List<AudioClip>();
                foreach (string file in files)
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxFolder + file);
                    if (clip == null)
                    {
                        Debug.LogWarning("AudioSetup: missing " + SfxFolder + file);
                        missing++;
                        continue;
                    }
                    clips.Add(clip);
                }

                SerializedProperty clipList = entry.FindPropertyRelative("clips");
                clipList.arraySize = clips.Count;
                for (int i = 0; i < clips.Count; i++)
                    clipList.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static SerializedProperty FindEntry(SerializedProperty list, Sfx.Sound sound)
        {
            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("sound").enumValueIndex == (int)sound)
                    return entry;
            }
            return null;
        }

        static void AssignMusic(Music music, ref int missing)
        {
            var serialized = new SerializedObject(music);
            SetTrack(serialized, "levelTrack", "Level.ogg", ref missing);
            SetTrack(serialized, "bossTrack", "Boss.ogg", ref missing);
            SetTrack(serialized, "menuTrack", "Menu.ogg", ref missing);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetTrack(SerializedObject serialized, string property, string file, ref int missing)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicFolder + file);
            if (clip == null)
            {
                Debug.LogWarning("AudioSetup: missing " + MusicFolder + file);
                missing++;
                return;
            }
            serialized.FindProperty(property).objectReferenceValue = clip;
        }
    }
}
