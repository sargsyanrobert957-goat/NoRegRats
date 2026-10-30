using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Dust and paper-scrap puffs: small sprites that drift, grow and fade. One spawner per scene
    /// keeps a fixed pool (the oldest puff is reused when it runs out), so effects never allocate.
    /// Call the static Spawn / Burst from anywhere; they do nothing in a scene without a spawner.
    /// </summary>
    public class PuffSpawner : MonoBehaviour
    {
        [SerializeField] Sprite puffSprite;
        [SerializeField] Material material;
        [SerializeField] int sortingOrder = 8;
        [Tooltip("Puffs alive at once; more than this reuses the oldest.")]
        [SerializeField] int poolSize = 40;
        [Tooltip("Seconds a puff lasts.")]
        [SerializeField] float lifetime = 0.45f;
        [Tooltip("How quickly puffs slow down (fraction of speed kept per second).")]
        [SerializeField, Range(0f, 1f)] float drag = 0.05f;

        struct Puff
        {
            public Transform transform;
            public SpriteRenderer renderer;
            public Vector2 velocity;
            public Color color;
            public float age, size, spin;
        }

        static PuffSpawner instance;

        Puff[] puffs;
        int next;

        void Awake()
        {
            instance = this;
            puffs = new Puff[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject("Puff");
                go.transform.SetParent(transform, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = puffSprite;
                renderer.sharedMaterial = material;
                renderer.sortingOrder = sortingOrder;
                renderer.enabled = false;
                puffs[i] = new Puff { transform = go.transform, renderer = renderer, age = lifetime };
            }
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        /// <summary>One puff of <paramref name="size"/> world units, drifting with <paramref name="velocity"/>.</summary>
        public static void Spawn(Vector2 position, float size, Vector2 velocity, Color color)
        {
            if (instance != null)
                instance.Add(position, size, velocity, color);
        }

        /// <summary>Several puffs flying out from a point, spread over the upper half-circle.</summary>
        public static void Burst(Vector2 position, int count, float size, float speed, Color color)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.Lerp(10f, 170f, (i + Random.value) / count) * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Spawn(position, size * Random.Range(0.7f, 1.2f), direction * speed * Random.Range(0.6f, 1.2f), color);
            }
        }

        void Add(Vector2 position, float size, Vector2 velocity, Color color)
        {
            ref Puff puff = ref puffs[next];
            next = (next + 1) % puffs.Length;
            puff.transform.position = position;
            puff.velocity = velocity;
            puff.color = color;
            puff.size = size;
            puff.age = 0f;
            puff.spin = Random.Range(-180f, 180f);
            puff.renderer.enabled = true;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float keep = Mathf.Pow(drag, dt);
            for (int i = 0; i < puffs.Length; i++)
            {
                ref Puff puff = ref puffs[i];
                if (puff.age >= lifetime)
                    continue;
                puff.age += dt;
                if (puff.age >= lifetime)
                {
                    puff.renderer.enabled = false;
                    continue;
                }
                float k = puff.age / lifetime;
                puff.velocity *= keep;
                puff.transform.position += (Vector3)(puff.velocity * dt);
                puff.transform.Rotate(0f, 0f, puff.spin * dt);
                float scale = puff.size * Mathf.Lerp(0.5f, 1.2f, k);
                puff.transform.localScale = new Vector3(scale, scale, 1f);
                Color color = puff.color;
                color.a *= 1f - k * k;
                puff.renderer.color = color;
            }
        }
    }
}
