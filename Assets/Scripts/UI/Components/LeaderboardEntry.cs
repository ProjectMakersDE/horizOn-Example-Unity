using UnityEngine;
using TMPro;

namespace SeagullStorm
{
    /// <summary>
    /// Single leaderboard row display.
    /// </summary>
    public class LeaderboardEntry : MonoBehaviour
    {
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text scoreText;

        public void Setup(int position, string username, long score)
        {
            if (rankText != null) rankText.text = $"#{position}";
            if (nameText != null) nameText.text = username ?? "???";
            if (scoreText != null) scoreText.text = score.ToString("N0");
        }
    }
}
