using UnityEngine;
namespace SeagullStorm
{
    /// <summary>Shared transparent artwork; atlas sprites remain a fallback when an asset is absent.</summary>
    public class SeagullArt : MonoBehaviour
    {
        public static Sprite Load(string name) => Resources.Load<Sprite>("SeagullArt/" + name);
        public static void CreateArena(Transform parent)
        {
            var sprite = Load("arena_background");
            if (sprite == null || parent == null) return;
            foreach (var tile in parent.GetComponentsInChildren<UnityEngine.Tilemaps.TilemapRenderer>()) tile.enabled = false;
            var arena = new GameObject("Shared island artwork");
            arena.transform.SetParent(parent, false);
            arena.transform.position = Vector3.zero;
            arena.transform.localScale = Vector3.one;
            var renderer = arena.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -100;
        }
        private void Start()
        {
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer == null) return;
            string key = GetComponent<PlayerController>() != null ? "seagull_hero" :
                GetComponent<EnemyBoss>() != null ? "enemy_octopus" :
                GetComponent<EnemyCrab>() != null ? "enemy_crab" :
                GetComponent<EnemyJellyfish>() != null ? "enemy_jellyfish" :
                GetComponent<EnemyPirate>() != null ? "enemy_pirate" : "pickup_shell";
            var sprite = Load(key);
            if (sprite != null)
            {
                renderer.sprite = sprite;
                var animator = GetComponent<Animator>();
                if (animator != null) animator.enabled = false;
                var oldAnimation = GetComponent<PlayerAnimator>();
                if (oldAnimation != null) oldAnimation.enabled = false;
                if (GetComponent<PlayerController>() != null || GetComponent<EnemyCrab>() != null)
                    if (GetComponent<SeagullCutoutRig>() == null) gameObject.AddComponent<SeagullCutoutRig>();
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, __) => AttachAll();
            AttachAll();
        }
        private static void AttachAll()
        {
            foreach (var p in Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Attach(p.gameObject);
            foreach (var p in Object.FindObjectsByType<EnemyBase>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Attach(p.gameObject);
            foreach (var p in Object.FindObjectsByType<XPPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Attach(p.gameObject);
        }
        public static void Attach(GameObject go) { if (go.GetComponent<SeagullArt>() == null) go.AddComponent<SeagullArt>(); }
    }
}
