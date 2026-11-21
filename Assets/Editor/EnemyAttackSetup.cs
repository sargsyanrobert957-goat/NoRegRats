using UnityEditor;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Tools > NoRegRats > Set Up Enemy Attacks: gives the chain rat (Rat1) and the machete rat (Rat3)
    /// their RatFighter attacks, with the artist's frames for the wind-up and the strike, and sets
    /// their standing drawings so they no longer loop the attack frames while walking. Only adds a
    /// RatFighter where there isn't one, so tuning done in the Inspector afterwards is kept. The gun
    /// rat's patterns need no setup (their defaults are in RatShooter).
    /// </summary>
    public static class EnemyAttackSetup
    {
        const string Prefabs = "Assets/Prefabs/Enemies/";
        const string Art = "Assets/Art/Rats/";

        struct Settings
        {
            public float noticeRange, reach, strikeHeight, windUpTime, secondWindUpTime, strikeTime, recoverTime, lungeSpeed, lungeTime, approachSpeed;
            public int damage;
            public Vector2 restTime;
            public (RatFighter.Move move, float weight)[] moves;
            public string standing;
            public string[] windUp, strike;
        }

        [MenuItem("Tools/NoRegRats/Set Up Enemy Attacks")]
        public static void Run()
        {
            // The chain rat: a quick claw swipe, close in. Its chain is the necklace, so it fights by hand.
            SetUp("Rat1", new Settings
            {
                noticeRange = 3f, approachSpeed = 2.4f, reach = 0.75f, strikeHeight = 1.2f, damage = 1,
                windUpTime = 0.35f, secondWindUpTime = 0.18f, strikeTime = 0.16f, recoverTime = 0.3f,
                lungeSpeed = 5f, lungeTime = 0.22f, restTime = new Vector2(0.6f, 1.2f),
                moves = new[] { (RatFighter.Move.Strike, 3f), (RatFighter.Move.DoubleStrike, 2f), (RatFighter.Move.LungeStrike, 2f) },
                standing = "Rat1/Rat1_Standing",
                windUp = new[] { "Rat1/Rat1_Anim_2", "Rat1/Rat1_Anim_3", "Rat1/Rat1_Anim_4", "Rat1/Rat1_Anim_5" },
                strike = new[] { "Rat1/Rat1_Anim_6", "Rat1/Rat1_Anim_7" },
            });
            // The machete rat: a long overhead slash, slower to wind up, and it feints.
            SetUp("Rat3", new Settings
            {
                noticeRange = 4f, approachSpeed = 2f, reach = 1.6f, strikeHeight = 1.4f, damage = 2,
                windUpTime = 0.45f, secondWindUpTime = 0.2f, strikeTime = 0.2f, recoverTime = 0.4f,
                lungeSpeed = 5.5f, lungeTime = 0.25f, restTime = new Vector2(0.8f, 1.5f),
                moves = new[] { (RatFighter.Move.Strike, 3f), (RatFighter.Move.DoubleStrike, 2f), (RatFighter.Move.LungeStrike, 2f), (RatFighter.Move.Feint, 1.5f) },
                standing = "Rat3/Rat3_Standing",
                windUp = new[] { "Rat3/Rat3_Anim_1", "Rat3/Rat3_Anim_2", "Rat3/Rat3_Anim_3" },
                strike = new[] { "Rat3/Rat3_Anim_4", "Rat3/Rat3_Anim_5" },
            });
            AssetDatabase.SaveAssets();
            Debug.Log("EnemyAttackSetup: done.");
        }

        static void SetUp(string prefabName, Settings s)
        {
            string path = Prefabs + prefabName + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var animator = root.GetComponent<RatAnimator>();
                var animatorSettings = new SerializedObject(animator);
                animatorSettings.FindProperty("standingFrame").objectReferenceValue = Sprite(s.standing);
                animatorSettings.ApplyModifiedPropertiesWithoutUndo();

                if (root.GetComponent<RatFighter>() == null)
                {
                    var fighter = root.AddComponent<RatFighter>();
                    var so = new SerializedObject(fighter);
                    so.FindProperty("noticeRange").floatValue = s.noticeRange;
                    so.FindProperty("approachSpeed").floatValue = s.approachSpeed;
                    so.FindProperty("reach").floatValue = s.reach;
                    so.FindProperty("strikeHeight").floatValue = s.strikeHeight;
                    so.FindProperty("damage").intValue = s.damage;
                    so.FindProperty("windUpTime").floatValue = s.windUpTime;
                    so.FindProperty("secondWindUpTime").floatValue = s.secondWindUpTime;
                    so.FindProperty("strikeTime").floatValue = s.strikeTime;
                    so.FindProperty("recoverTime").floatValue = s.recoverTime;
                    so.FindProperty("lungeSpeed").floatValue = s.lungeSpeed;
                    so.FindProperty("lungeTime").floatValue = s.lungeTime;
                    so.FindProperty("restTime").vector2Value = s.restTime;
                    SerializedProperty moves = so.FindProperty("moves");
                    moves.arraySize = s.moves.Length;
                    for (int i = 0; i < s.moves.Length; i++)
                    {
                        SerializedProperty entry = moves.GetArrayElementAtIndex(i);
                        entry.FindPropertyRelative("move").enumValueIndex = (int)s.moves[i].move;
                        entry.FindPropertyRelative("weight").floatValue = s.moves[i].weight;
                    }
                    so.FindProperty("ratAnimator").objectReferenceValue = animator;
                    Sprites(so.FindProperty("windUpFrames"), s.windUp);
                    Sprites(so.FindProperty("strikeFrames"), s.strike);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void Sprites(SerializedProperty list, string[] names)
        {
            list.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = Sprite(names[i]);
        }

        static Sprite Sprite(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + name + ".png");
            if (sprite == null)
                Debug.LogError("EnemyAttackSetup: missing " + Art + name + ".png");
            return sprite;
        }
    }
}
