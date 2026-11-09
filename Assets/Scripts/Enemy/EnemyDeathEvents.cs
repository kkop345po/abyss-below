using UnityEngine;
using UnityEngine.Events;

public class EnemyDeathEvents : MonoBehaviour
{
	[Header("事件")]
	public UnityEvent onDied;

	// 供其他腳本（例如 Enemy）在死亡時呼叫
	public void InvokeDied()
	{
		onDied?.Invoke();
	}
}


