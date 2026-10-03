using UnityEngine;
using UnityEngine.Rendering;

namespace SeagullStorm
{
    /// <summary>Joint poses from the accepted shared cutout mockup. Physics stays on the actor.</summary>
    public sealed class SeagullCutoutRig : MonoBehaviour
    {
        public enum Motion { Idle, Walk, Attack, Hurt }
        public bool Ready { get; private set; }
        public Motion CurrentMotion { get; private set; }

        private static readonly string[] GullNames = { "tail", "backWing", "leftFoot", "rightFoot", "body", "frontWing", "head" };
        private static readonly Vector2[] GullPivots = { new(159, 355), new(310, 304), new(215, 422), new(278, 422), new(260, 366), new(228, 307), new(247, 282) };
        private static readonly string[] CrabNames = { "leftBack", "rightBack", "leftMiddle", "rightMiddle", "leftFront", "rightFront", "backClaw", "body", "frontClaw" };
        private static readonly Vector2[] CrabPivots = { new(84, 124), new(195, 133), new(88, 136), new(192, 145), new(98, 145), new(182, 153), new(84, 118), new(144, 116), new(169, 130) };
        private Transform _model, _lean;
        private Transform[] _joints;
        private Vector3[] _restPositions;
        private SpriteRenderer[] _parts;
        private SpriteRenderer _source;
        private PlayerController _player;
        private bool _gull;
        private float _ppu, _phase, _attackTime, _hurtTime;
        private Vector3 _lastPosition;

        private void Awake()
        {
            _source = GetComponent<SpriteRenderer>();
            _player = GetComponent<PlayerController>();
            _gull = _player != null;
            if (_source == null || (!_gull && GetComponent<EnemyCrab>() == null)) return;
            var names = _gull ? GullNames : CrabNames;
            var pivots = _gull ? GullPivots : CrabPivots;
            string folder = "SeagullArt/Cutout/" + (_gull ? "gull/" : "crab/");
            var sprites = new Sprite[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                sprites[i] = Resources.Load<Sprite>(folder + names[i]);
                if (sprites[i] == null)
                {
                    Debug.LogWarning("[SeagullStorm] Missing cutout part: " + folder + names[i]);
                    return;
                }
            }

            _ppu = _gull ? 16f : 8f;
            Vector2 center = _gull ? new Vector2(256, 256) : new Vector2(128, 128);
            Vector2 rootPivot = _gull ? new Vector2(256, 420) : new Vector2(140, 132);
            var visual = new GameObject("Cutout rig");
            visual.transform.SetParent(transform, false);
            var sorting = visual.AddComponent<SortingGroup>();
            sorting.sortingLayerID = _source.sortingLayerID;
            sorting.sortingOrder = _source.sortingOrder;
            CreateShadow(visual.transform, center);
            _model = new GameObject("Facing and hop").transform;
            _model.SetParent(visual.transform, false);
            _lean = new GameObject("Body lean").transform;
            _lean.SetParent(_model, false);
            _lean.localPosition = ToWorld(rootPivot - center);
            _joints = new Transform[names.Length];
            _restPositions = new Vector3[names.Length];
            _parts = new SpriteRenderer[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                var joint = new GameObject(names[i]).transform;
                joint.SetParent(_lean, false);
                joint.localPosition = ToWorld(pivots[i] - rootPivot);
                var image = new GameObject("Artwork");
                image.transform.SetParent(joint, false);
                image.transform.localPosition = ToWorld(center - pivots[i]);
                var part = image.AddComponent<SpriteRenderer>();
                part.sprite = sprites[i];
                part.sharedMaterial = _source.sharedMaterial;
                part.sortingLayerID = _source.sortingLayerID;
                part.sortingOrder = i;
                _joints[i] = joint;
                _restPositions[i] = joint.localPosition;
                _parts[i] = part;
            }
            _source.enabled = false;
            Ready = true;
            ResetMotion();
            ApplyPose(false);
        }

        private Vector3 ToWorld(Vector2 pixelOffset) => new(pixelOffset.x / _ppu, -pixelOffset.y / _ppu, 0);

        private void CreateShadow(Transform parent, Vector2 center)
        {
            var sprite = Resources.Load<Sprite>("SeagullArt/Cutout/shadow");
            if (sprite == null) return;
            var shadow = new GameObject("Contact shadow");
            shadow.transform.SetParent(parent, false);
            Vector2 feet = _gull ? new Vector2(256, 477) : new Vector2(130, 200);
            shadow.transform.localPosition = ToWorld(feet + Vector2.up * 4 - center);
            shadow.transform.localScale = new Vector3((_gull ? 130f : 110f) / _ppu / sprite.bounds.size.x, 18f / _ppu / sprite.bounds.size.y, 1);
            var renderer = shadow.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = _source.sharedMaterial;
            renderer.sortingLayerID = _source.sortingLayerID;
            renderer.sortingOrder = -1;
        }

        private void OnEnable() => ResetMotion();
        private void OnDisable() => ResetMotion();
        private void ResetMotion()
        {
            _phase = _gull ? 0 : .9f;
            _attackTime = _hurtTime = 0;
            CurrentMotion = Motion.Idle;
            _lastPosition = transform.position;
            if (Ready) ApplyPose(false);
        }
        public void PlayAttack() { if (_attackTime <= 0) _attackTime = .6f; }
        public void PlayHurt() { _hurtTime = .42f; }

        private void LateUpdate()
        {
            if (!Ready) return;
            bool running = GameManager.Instance?.CurrentState == GameState.Run;
            bool moving = (_player != null && _player.MoveDirection.sqrMagnitude > .01f) || (transform.position - _lastPosition).sqrMagnitude > .0001f;
            _lastPosition = transform.position;
            if (!running) return;
            _phase += Time.deltaTime;
            _attackTime = Mathf.Max(0, _attackTime - Time.deltaTime);
            _hurtTime = Mathf.Max(0, _hurtTime - Time.deltaTime);
            ApplyPose(moving);
        }

        private void Pose(int index, float radians = 0, float dx = 0, float dy = 0)
        {
            _joints[index].localPosition = _restPositions[index] + ToWorld(new Vector2(dx, dy));
            _joints[index].localRotation = Quaternion.Euler(0, 0, -radians * Mathf.Rad2Deg);
        }

        private void ApplyPose(bool moving)
        {
            CurrentMotion = _hurtTime > 0 ? Motion.Hurt : _attackTime > 0 ? Motion.Attack : moving ? Motion.Walk : Motion.Idle;
            bool attack = CurrentMotion == Motion.Attack, hurt = CurrentMotion == Motion.Hurt;
            bool walk = moving && !hurt;
            float phase = hurt ? .42f - _hurtTime : attack ? .6f - _attackTime : _phase;
            float step = walk ? Mathf.Sin(_phase * (_gull ? 8 : 10)) : 0;
            float punch = attack ? Mathf.Pow(Mathf.Max(0, Mathf.Sin(phase * 5)), 2) : 0;
            float hop = walk ? -Mathf.Abs(step) * (_gull ? 7 : 4) : Mathf.Sin(phase * 2) * (_gull ? 1.6f : 1.8f);
            _model.localPosition = ToWorld(new Vector2(0, hop));
            float face = _source.flipX ? -1 : 1;
            _model.localScale = new Vector3(face * (_gull ? 1 : -1), 1, 1);
            float lean = hurt ? Mathf.Sin(phase * (_gull ? 19 : 21)) * (_gull ? .085f : .09f) : step * (_gull ? .018f : .015f);
            _lean.localRotation = Quaternion.Euler(0, 0, -lean * Mathf.Rad2Deg);
            if (_gull)
            {
                float rear = attack ? -.53f * punch : walk ? Mathf.Sin(_phase * 8 + .4f) * .15f : Mathf.Sin(phase * 2) * .035f;
                float front = attack ? .57f * punch : walk ? -Mathf.Sin(_phase * 8 + .4f) * .16f : Mathf.Sin(phase * 2 + 1) * .025f;
                Pose(0, -rear * .35f);
                Pose(1, rear);
                Pose(2, step * .16f, -step * 5, -Mathf.Max(step, 0) * 4);
                Pose(3, -step * .16f, step * 5, -Mathf.Max(-step, 0) * 4);
                Pose(4);
                Pose(5, front);
                Pose(6, hurt ? -.10f : walk ? -step * .018f : Mathf.Sin(phase * 2) * .014f, attack ? punch * 3 : 0, attack ? -punch * 4 : 0);
            }
            else
            {
                for (int i = 0; i < 6; i++) Pose(i, step * (i % 2 == 1 ? 1 : -1) * (.09f + i % 3 * .035f));
                Pose(6, attack ? -.35f * punch : Mathf.Sin(phase * 2) * .05f);
                Pose(7);
                Pose(8, attack ? .33f * punch : Mathf.Sin(phase * 2 + 1) * .04f, attack ? -punch * 6 : 0, attack ? -punch * 4 : 0);
            }
            foreach (var part in _parts) part.color = _source.color;
            _source.enabled = false;
        }

    }
}
