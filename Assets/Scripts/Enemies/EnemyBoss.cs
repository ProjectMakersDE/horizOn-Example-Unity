namespace SeagullStorm
{
    /// <summary>
    /// Giant Octopus boss. Spawns at end of run. Uses default EnemyBase behavior
    /// but with much higher HP and damage.
    /// </summary>
    public class EnemyBoss : EnemyBase
    {
        protected override void Die()
        {
            base.Die();

            // Boss kill ends the run as a victory
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EndRun();
                AudioManager.Instance?.PlayGameOver();
                AudioManager.Instance?.PlayMenuMusic();
                GameManager.Instance.ChangeState(GameState.GameOver);
            }
        }
    }
}
