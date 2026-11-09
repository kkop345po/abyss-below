using System;
using UnityEngine;

namespace Game.Inventory
{
    /// <summary>
    /// 可使用物品的最小介面：提供使用、冷卻、數量等事件/屬性。
    /// </summary>
    public interface IUsableItem
    {
        /// <summary>
        /// 圖示（用於方格/快捷欄顯示）。
        /// </summary>
        Sprite Icon { get; }

        /// <summary>
        /// 目前堆疊數量（-1 代表無限或不顯示）。
        /// </summary>
        int CurrentCount { get; }

        /// <summary>
        /// 總冷卻秒數（啟動時的標稱值）。
        /// </summary>
        float CooldownSeconds { get; }

        /// <summary>
        /// 是否處於冷卻中。
        /// </summary>
        bool IsOnCooldown { get; }

        /// <summary>
        /// 冷卻剩餘秒數（未冷卻時為 0）。
        /// </summary>
        float CooldownRemaining { get; }

        /// <summary>
        /// 嘗試使用；成功回傳 true。
        /// </summary>
        bool TryUse();

        /// <summary>
        /// 冷卻結束／重置時通知 UI 清除遮罩（專案已停用使用冷卻，僅供初始化）。
        /// </summary>
        event Action OnCooldownEnded;

        /// <summary>
        /// 數量改變事件（參數：當前數量）。
        /// </summary>
        event Action<int> OnCountChanged;
    }
}


