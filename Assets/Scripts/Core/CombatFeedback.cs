using UnityEngine;
using TMPro;
namespace SeagullStorm
{
    public class CombatFeedback : MonoBehaviour
    {
        private float _age;
        private TMP_Text _text;
        public static void Hit(Vector3 pos, int damage, bool player = false)
        {
            var go = new GameObject("Damage feedback");
            go.transform.position = pos + Vector3.up * 16f;
            var text = go.AddComponent<TextMeshPro>();
            text.text = damage.ToString();
            text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF") ?? TMP_Settings.defaultFontAsset;
            text.fontSize = 14;
            text.fontStyle = FontStyles.Bold;
            text.color = player ? GameColors.Orange : Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.renderer.sortingOrder = 100;
            go.AddComponent<CombatFeedback>()._text = text;
        }
        private void Update()
        {
            _age += Time.deltaTime;
            transform.position += Vector3.up * Time.deltaTime * 28f;
            if (_text != null) _text.alpha = 1f - _age / 0.65f;
            if (_age >= 0.65f) Destroy(gameObject);
        }
    }
}
