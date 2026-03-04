using UnityEngine;

namespace SeagullStorm.Gameplay
{
    public class MapGenerator : MonoBehaviour
    {
        [SerializeField] private Sprite sandTileSprite;
        [SerializeField] private Color sandColor = new Color(0.949f, 0.824f, 0.663f, 1f);
        [SerializeField] private int mapSize = 40;
        [SerializeField] private float tileSize = 1f;

        private void Start()
        {
            GenerateMap();
        }

        private void GenerateMap()
        {
            float halfSize = mapSize * tileSize / 2f;

            for (int x = 0; x < mapSize; x++)
            {
                for (int y = 0; y < mapSize; y++)
                {
                    var tile = new GameObject($"Tile_{x}_{y}");
                    tile.transform.parent = transform;
                    tile.transform.position = new Vector3(
                        x * tileSize - halfSize,
                        y * tileSize - halfSize,
                        0.1f);

                    var sr = tile.AddComponent<SpriteRenderer>();
                    sr.sprite = sandTileSprite;
                    sr.color = sandColor;
                    sr.sortingLayerName = "Background";

                    if (sr.sprite == null)
                    {
                        // Fallback: create a colored quad
                        sr.color = sandColor;
                    }
                }
            }
        }
    }
}
