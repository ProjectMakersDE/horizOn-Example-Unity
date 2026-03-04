using System.Collections;
using UnityEngine;

namespace SeagullStorm.Managers
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Music")]
        [SerializeField] private AudioClip musicMenu;
        [SerializeField] private AudioClip musicBattle;
        [SerializeField] private AudioClip musicBoss;

        [Header("Weapon SFX")]
        [SerializeField] private AudioClip sfxFeather;
        [SerializeField] private AudioClip sfxScreech;
        [SerializeField] private AudioClip sfxDive;
        [SerializeField] private AudioClip sfxGust;

        [Header("Combat SFX")]
        [SerializeField] private AudioClip sfxPlayerHit;
        [SerializeField] private AudioClip sfxEnemyHit;
        [SerializeField] private AudioClip sfxEnemyAttack;

        [Header("UI SFX")]
        [SerializeField] private AudioClip sfxPickupXp;
        [SerializeField] private AudioClip sfxLevelup;
        [SerializeField] private AudioClip sfxUpgradeSelect;
        [SerializeField] private AudioClip sfxGameOver;

        private AudioSource _musicSourceA;
        private AudioSource _musicSourceB;
        private AudioSource _sfxSource;
        private bool _usingSourceA = true;

        private const float MusicVolume = 0.7f;
        private const float CrossfadeDuration = 0.5f;

        // Polyphony tracking
        private int _pickupXpActive;
        private int _enemyHitActive;
        private const int MaxPickupXpSimultaneous = 3;
        private const int MaxEnemyHitSimultaneous = 5;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _musicSourceA = gameObject.AddComponent<AudioSource>();
            _musicSourceA.loop = true;
            _musicSourceA.volume = MusicVolume;

            _musicSourceB = gameObject.AddComponent<AudioSource>();
            _musicSourceB.loop = true;
            _musicSourceB.volume = 0f;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
        }

        public void PlayMenuMusic()
        {
            CrossfadeTo(musicMenu);
        }

        public void PlayBattleMusic()
        {
            CrossfadeTo(musicBattle);
        }

        public void PlayBossMusic()
        {
            CrossfadeTo(musicBoss);
        }

        private void CrossfadeTo(AudioClip clip)
        {
            if (clip == null) return;

            var incoming = _usingSourceA ? _musicSourceB : _musicSourceA;
            var outgoing = _usingSourceA ? _musicSourceA : _musicSourceB;

            incoming.clip = clip;
            incoming.Play();

            _usingSourceA = !_usingSourceA;
            StartCoroutine(CrossfadeCoroutine(incoming, outgoing));
        }

        private IEnumerator CrossfadeCoroutine(AudioSource incoming, AudioSource outgoing)
        {
            float t = 0f;
            while (t < CrossfadeDuration)
            {
                t += Time.unscaledDeltaTime;
                float progress = t / CrossfadeDuration;
                incoming.volume = Mathf.Lerp(0f, MusicVolume, progress);
                outgoing.volume = Mathf.Lerp(MusicVolume, 0f, progress);
                yield return null;
            }
            incoming.volume = MusicVolume;
            outgoing.volume = 0f;
            outgoing.Stop();
        }

        public void PlayFeather() => PlaySfx(sfxFeather);
        public void PlayScreech() => PlaySfx(sfxScreech);
        public void PlayDive() => PlaySfx(sfxDive);
        public void PlayGust() => PlaySfx(sfxGust);
        public void PlayPlayerHit() => PlaySfx(sfxPlayerHit);
        public void PlayUpgradeSelect() => PlaySfx(sfxUpgradeSelect);
        public void PlayGameOver() => PlaySfx(sfxGameOver);
        public void PlayLevelup() => PlaySfx(sfxLevelup);

        public void PlayEnemyHit()
        {
            if (_enemyHitActive >= MaxEnemyHitSimultaneous) return;
            _enemyHitActive++;
            PlaySfxWithCallback(sfxEnemyHit, () => _enemyHitActive--);
        }

        public void PlayEnemyAttack() => PlaySfx(sfxEnemyAttack);

        public void PlayPickupXp()
        {
            if (_pickupXpActive >= MaxPickupXpSimultaneous) return;
            _pickupXpActive++;
            PlaySfxWithCallback(sfxPickupXp, () => _pickupXpActive--);
        }

        private void PlaySfx(AudioClip clip)
        {
            if (clip == null) return;
            _sfxSource.PlayOneShot(clip);
        }

        private void PlaySfxWithCallback(AudioClip clip, System.Action onDone)
        {
            if (clip == null)
            {
                onDone?.Invoke();
                return;
            }
            _sfxSource.PlayOneShot(clip);
            StartCoroutine(WaitForClip(clip.length, onDone));
        }

        private IEnumerator WaitForClip(float duration, System.Action onDone)
        {
            yield return new WaitForSeconds(duration);
            onDone?.Invoke();
        }
    }
}
