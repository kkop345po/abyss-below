using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    [SerializeField] private int coinAmount = 10;

    private ItemFloatEffect floatEffect;
    private bool picked;

    private void Start()
    {
        floatEffect = GetComponent<ItemFloatEffect>();
        if (floatEffect == null)
            floatEffect = gameObject.AddComponent<ItemFloatEffect>();

        // 不再強制 isTrigger = false：金幣常用 Trigger，玩家與金幣任一為 Trigger 時需 OnTriggerEnter2D
    }

    public void SetAmount(int amount)
    {
        coinAmount = amount;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        TryPickup();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        TryPickup();
    }

    private void TryPickup()
    {
        if (picked) return;
        picked = true;

        CoinManager.Instance?.AddCoins(coinAmount);

        var am = AudioManager.GetInstance();
        if (am != null) am.PlayPickupCoinSfx();

        if (PickupFeedbackManager.Instance != null)
            PickupFeedbackManager.Instance.ShowPickup("金幣", coinAmount);

        Destroy(gameObject);
    }
}
