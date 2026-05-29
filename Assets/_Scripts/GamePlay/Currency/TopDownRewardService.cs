using UnityEngine;

public class TopDownRewardService : MonoBehaviour
{
    [SerializeField] private TopDownSessionWallet sessionWallet;

    private void Awake()
    {
        if (sessionWallet == null)
        {
            sessionWallet = GetComponent<TopDownSessionWallet>();
        }
    }

    public void GrantEnemyKillReward(EnemyController enemy)
    {
        if (enemy == null)
        {
            return;
        }

        if (sessionWallet == null)
        {
            Debug.LogWarning("[TopDownRewardService] SessionWallet is missing.");
            return;
        }

        sessionWallet.AddSessionRewards(enemy.Rewards);
    }
}