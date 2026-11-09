using System;
using System.Collections;
using UnityEngine;

namespace Game.Inventory.Demo
{
    /// <summary>
    /// 範例用的可使用物品：演示冷卻與數量變化。
    /// 將此元件掛在場景物件或 Prefab 上，再綁到方格即可測試。
    /// </summary>
    public class DemoUsableItem : MonoBehaviour, Game.Inventory.IUsableItem
    {
        [Header("Config")]
        [SerializeField] private Sprite icon;
        [SerializeField] private int startCount = 3; // -1 代表無限
        [SerializeField, Min(0f)] private float cooldownSeconds = 5f;

        public Sprite Icon => icon;
        public int CurrentCount => startCount;
        public float CooldownSeconds => cooldownSeconds;
        public bool IsOnCooldown => cooldownRemaining > 0f;
        public float CooldownRemaining => cooldownRemaining;

        public event Action<float> OnCooldownStarted;
        public event Action<float> OnCooldownTick;
        public event Action OnCooldownEnded;
        public event Action<int> OnCountChanged;

        private float cooldownRemaining;
        private Coroutine cooldownRoutine;

        public bool TryUse()
        {
            if (IsOnCooldown) return false;
            if (startCount == 0) return false;

            // 成功使用：扣數量（若非無限）
            if (startCount > 0)
            {
                startCount--;
                OnCountChanged?.Invoke(startCount);
            }

            // 觸發冷卻
            StartCooldown(cooldownSeconds);

            // 這裡可以放實際效果（回血/丟東西/射擊等）
            Debug.Log($"[DemoUsableItem] Used. count={startCount}");
            return true;
        }

        private void StartCooldown(float seconds)
        {
            cooldownRemaining = Mathf.Max(0f, seconds);
            OnCooldownStarted?.Invoke(cooldownRemaining);
            if (cooldownRoutine != null) StopCoroutine(cooldownRoutine);
            cooldownRoutine = StartCoroutine(CooldownTicks());
        }

        private IEnumerator CooldownTicks()
        {
            while (cooldownRemaining > 0f)
            {
                cooldownRemaining -= Time.deltaTime;
                if (cooldownRemaining < 0f) cooldownRemaining = 0f;
                OnCooldownTick?.Invoke(cooldownRemaining);
                yield return null;
            }
            OnCooldownEnded?.Invoke();
            cooldownRoutine = null;
        }
    }
}


