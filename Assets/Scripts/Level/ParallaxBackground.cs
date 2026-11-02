using System;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// The graph-paper backdrop: a tiled sprite that drifts slower than the level as the camera
    /// moves (parallax) and takes its tint from how deep the camera is, so the subway and the
    /// sewers feel different. Make the sprite big enough to cover the view across the whole level.
    /// Above ground a plain sky and a drawn city cover the paper, so it only shows underground. The
    /// city is split into layers that drift at different speeds, the nearest fastest. They're made
    /// here from sprites in a Resources folder and need nothing in the scene; without those sprites
    /// the paper shows everywhere.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ParallaxBackground : MonoBehaviour
    {
        [Serializable]
        struct SkylineLayer
        {
            [Tooltip("End of the sprites' names, e.g. Near for City1_Near.")]
            public string suffix;
            [Tooltip("0 = scrolls with the level, 1 = fixed to the camera. Higher looks further away.")]
            [Range(0f, 1f)] public float parallax;
        }

        // Enough pictures side by side to cover the street at the fastest layer, on wide screens too.
        const int FirstTile = -1;
        const int LastTile = 2;

        [SerializeField] Transform cameraTransform;
        [Tooltip("0 = scrolls with the level, 1 = fixed to the camera.")]
        [SerializeField, Range(0f, 1f)] float parallax = 0.6f;
        [Tooltip("Tint by depth: left end = at or below Low Y (sewers), right end = at or above High Y (subway).")]
        [SerializeField] Gradient tint = new Gradient();
        [SerializeField] float lowY = -20f;
        [SerializeField] float highY = -2f;

        [Header("City Skyline")]
        [Tooltip("Sprite names in a Resources folder, placed side by side in turn. Each has one sprite per layer, with its pivot at the bottom centre.")]
        [SerializeField] string[] skylinePictures = { "City1", "City2" };
        [Tooltip("The city's layers, from the back to the front.")]
        [SerializeField] SkylineLayer[] skylineLayers =
        {
            new SkylineLayer { suffix = "Far", parallax = 0.9f },
            new SkylineLayer { suffix = "Mid", parallax = 0.75f },
            new SkylineLayer { suffix = "Near", parallax = 0.55f },
        };
        [Tooltip("Width of one picture in world units; its height follows.")]
        [SerializeField] float skylineWidth = 34f;
        [Tooltip("World Y of the bottom of the pictures and the sky. Keep it behind the sidewalk.")]
        [SerializeField] float skylineBottom = -0.2f;
        [Tooltip("How far right of the camera the middle of the first picture starts.")]
        [SerializeField] float skylineStartOffset = 6.3f;
        [Tooltip("World X range the sky covers: wider than the street, up to the shaft's right wall.")]
        [SerializeField] Vector2 skyRange = new Vector2(-8f, 68f);
        [Tooltip("Height of the sky in world units, enough to pass the camera's top bound.")]
        [SerializeField] float skyHeight = 24f;
        [Tooltip("Should match the top of the pictures so no join shows.")]
        [SerializeField] Color skyColor = Color.white;

        SpriteRenderer spriteRenderer;
        Vector3 startPosition;
        Vector3 cameraStart;
        Transform[] skyline;

        void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

        void Start()
        {
            if (cameraTransform == null)
                cameraTransform = Camera.main.transform;
            startPosition = transform.position;
            cameraStart = cameraTransform.position;
            BuildSkyline();
        }

        void LateUpdate()
        {
            Vector3 cameraPosition = cameraTransform.position;
            Vector3 moved = cameraPosition - cameraStart;
            transform.position = startPosition + new Vector3(moved.x * parallax, moved.y * parallax, 0f);
            spriteRenderer.color = tint.Evaluate(Mathf.InverseLerp(lowY, highY, cameraPosition.y));
            if (skyline == null)
                return;
            for (int i = 0; i < skyline.Length; i++)
                skyline[i].position = new Vector3(cameraStart.x + skylineStartOffset + moved.x * skylineLayers[i].parallax, skylineBottom, 0f);
        }

        void BuildSkyline()
        {
            if (Resources.Load<Sprite>(SpriteName(0, 0)) == null)
                return;

            var sky = AddLayer("Sky", Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0f), 4f), 1, null);
            sky.color = skyColor;
            sky.transform.position = new Vector3((skyRange.x + skyRange.y) * 0.5f, skylineBottom, 0f);
            sky.transform.localScale = new Vector3(skyRange.y - skyRange.x, skyHeight, 1f);

            skyline = new Transform[skylineLayers.Length];
            for (int layer = 0; layer < skylineLayers.Length; layer++)
            {
                skyline[layer] = new GameObject("Skyline " + skylineLayers[layer].suffix).transform;
                for (int tile = FirstTile; tile <= LastTile; tile++)
                {
                    Sprite sprite = Resources.Load<Sprite>(SpriteName(tile, layer));
                    float scale = skylineWidth / sprite.bounds.size.x;
                    SpriteRenderer part = AddLayer("Tile", sprite, 2 + layer, skyline[layer]);
                    part.transform.localPosition = new Vector3(tile * skylineWidth, 0f, 0f);
                    part.transform.localScale = new Vector3(scale, scale, 1f);
                }
            }
        }

        string SpriteName(int tile, int layer)
        {
            int picture = (tile - FirstTile + 1) % skylinePictures.Length;
            return skylinePictures[picture] + "_" + skylineLayers[layer].suffix;
        }

        // A sprite drawn just in front of the paper, with the same material so it's lit the same.
        SpriteRenderer AddLayer(string layerName, Sprite sprite, int orderAbovePaper, Transform parent)
        {
            var layer = new GameObject(layerName).AddComponent<SpriteRenderer>();
            layer.transform.SetParent(parent, false);
            layer.sprite = sprite;
            layer.sharedMaterial = spriteRenderer.sharedMaterial;
            layer.sortingLayerID = spriteRenderer.sortingLayerID;
            layer.sortingOrder = spriteRenderer.sortingOrder + orderAbovePaper;
            return layer;
        }
    }
}
