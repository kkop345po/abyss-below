using UnityEngine;
using System.Collections;

namespace Ambience
{
    /// <summary>
    /// 背景氛圍生成器
    /// 用於在指定範圍內隨機生成裝飾性物件（如遠處的鳥、氣泡、閃光等）
    /// </summary>
    public class BackgroundAmbienceSpawner : MonoBehaviour
    {
        [Header("生成設定")]
        [Tooltip("要生成的物件預製體 (Prefab)")]
        public GameObject prefab;

        [Tooltip("生成範圍大小 (寬, 高)")]
        public Vector2 spawnAreaSize = new Vector2(10f, 5f);

        [Header("時間控制 (秒)")]
        [Tooltip("最短生成間隔")]
        public float minInterval = 2f;
        [Tooltip("最長生成間隔")]
        public float maxInterval = 5f;

        [Header("隨機變化")]
        [Tooltip("最小縮放倍率 (1 = 原大小)")]
        public float minScale = 1.0f;
        [Tooltip("最大縮放倍率")]
        public float maxScale = 1.5f;

        [Header("生命週期")]
        [Tooltip("生成後幾秒自動銷毀 (0 = 不自動銷毀，由 Prefab 自行控制)")]
        public float autoDestroyTime = 5f;

        private void Start()
        {
            if (prefab == null)
            {
                Debug.LogWarning($"[BackgroundAmbienceSpawner] 未設定 Prefab，腳本將不會運作。物件: {name}");
                return;
            }

            StartCoroutine(SpawnRoutine());
        }

        private IEnumerator SpawnRoutine()
        {
            // 遊戲開始時先隨機等待一下，避免所有生成器同時啟動
            yield return new WaitForSeconds(Random.Range(0f, 2f));

            while (true)
            {
                SpawnObject();

                // 等待下一次生成
                float waitTime = Random.Range(minInterval, maxInterval);
                yield return new WaitForSeconds(waitTime);
            }
        }

        private void SpawnObject()
        {
            // 1. 計算隨機位置
            // 在 spawnAreaSize 的矩形範圍內隨機選一點
            // transform.position 是中心點
            float randomX = Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
            float randomY = Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
            Vector3 spawnPos = transform.position + new Vector3(randomX, randomY, 0f);

            // 2. 生成物件
            GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
            
            // 為了整潔，將生成的物件設為此生成器的子物件 (可選)
            // obj.transform.SetParent(transform); 

            // 3. 隨機縮放
            float randomScale = Random.Range(minScale, maxScale);
            obj.transform.localScale = new Vector3(randomScale, randomScale, 1f);

            // 4. 自動銷毀 (如果設定大於 0)
            if (autoDestroyTime > 0f)
            {
                Destroy(obj, autoDestroyTime);
            }
        }

        // 在編輯器中繪製生成範圍，方便調整
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f); // 青色半透明
            Gizmos.DrawCube(transform.position, new Vector3(spawnAreaSize.x, spawnAreaSize.y, 1f));
            
            Gizmos.color = Color.cyan; // 青色邊框
            Gizmos.DrawWireCube(transform.position, new Vector3(spawnAreaSize.x, spawnAreaSize.y, 1f));
        }
    }
}
