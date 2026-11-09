using UnityEngine;

public abstract class ItemEffectSO : ScriptableObject
{
    [TextArea] public string description;
    
    /// <summary>
    /// 執行效果
    /// </summary>
    /// <param name="user">使用者 GameObject (通常是 Player)</param>
    /// <returns>是否執行成功</returns>
    public abstract bool ExecuteEffect(GameObject user);

    /// <summary>
    /// 檢查是否可以執行（例如：血量滿時不能補血）
    /// </summary>
    public virtual bool CanExecute(GameObject user) => true;
}
