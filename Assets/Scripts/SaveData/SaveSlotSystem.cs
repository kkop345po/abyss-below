using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public static class SaveSlotSystem
{
    private const int AUTO_SAVE_SLOT = 0; // 自動存檔槽
    
    private static string GetPath(int slot)
    {
        string dir = Path.Combine(Application.persistentDataPath, "Saves");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        return Path.Combine(dir, $"SaveSlot_{slot}.json");
    }
    
    private static string GetThumbnailPath(int slot)
    {
        string dir = Path.Combine(Application.persistentDataPath, "Saves");
        return Path.Combine(dir, $"Thumbnail_{slot}.png");
    }

    public static void SaveToSlot(int slot, Texture2D thumbnail = null)
    {
        SaveSlotData data = SaveSlotData.CreateCurrent();

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetPath(slot), json);
        
        // 保存縮圖（使用反射以兼容不同 Unity 版本）
        if (thumbnail != null)
        {
            byte[] pngData = null;
            
            try
            {
                // 方法 1：嘗試使用 ImageConversion.EncodeToPNG（Unity 2017.2+）
                var imageConversionType = System.Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule");
                if (imageConversionType == null)
                    imageConversionType = System.Type.GetType("UnityEngine.ImageConversion");
                
                if (imageConversionType != null)
                {
                    var encodeMethod = imageConversionType.GetMethod("EncodeToPNG", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                        null, new System.Type[] { typeof(Texture2D) }, null);
                    
                    if (encodeMethod != null)
                    {
                        pngData = (byte[])encodeMethod.Invoke(null, new object[] { thumbnail });
                    if (pngData != null && pngData.Length > 0)
                    {
                        File.WriteAllBytes(GetThumbnailPath(slot), pngData);
                        Debug.Log($"[SaveSlotSystem] 縮圖已保存到槽位 {slot}（使用 ImageConversion）");
                        goto SaveComplete; // 成功後跳過其他嘗試
                    }
                    }
                }
                
                // 方法 2：嘗試使用 Texture2D.EncodeToPNG（舊版本 Unity）
                var encodeMethod2 = typeof(Texture2D).GetMethod("EncodeToPNG", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                if (encodeMethod2 != null)
                {
                    pngData = (byte[])encodeMethod2.Invoke(thumbnail, null);
                    if (pngData != null && pngData.Length > 0)
                    {
                        File.WriteAllBytes(GetThumbnailPath(slot), pngData);
                        Debug.Log($"[SaveSlotSystem] 縮圖已保存到槽位 {slot}（使用 Texture2D.EncodeToPNG）");
                        goto SaveComplete; // 成功後跳過其他嘗試
                    }
                }
                
                // 如果所有方法都失敗
                Debug.LogWarning($"[SaveSlotSystem] 無法保存縮圖到槽位 {slot}：當前 Unity 版本可能不支援 PNG 編碼。存檔數據已保存，但沒有縮圖。");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SaveSlotSystem] 保存縮圖失敗: {e.Message}。存檔數據已保存，但沒有縮圖。");
            }
            
            SaveComplete:;
        }
        
        Debug.Log($"[SaveSlotSystem] 保存到槽位 {slot}");
    }

    public static void LoadFromSlot(int slot)
    {
        string path = GetPath(slot);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SaveSlotSystem] 槽位 {slot} 不存在存檔");
            return;
        }

        string json = File.ReadAllText(path);
        SaveSlotData data = JsonUtility.FromJson<SaveSlotData>(json);
        
        // 應用數據到遊戲
        data.ApplyToGame();

        SaveSystem.SavePlayerData();

        string active = SceneManager.GetActiveScene().name;
        string targetScene = SceneLoader.SanitizeGameSceneName(data.sceneName);
        if (!string.IsNullOrEmpty(targetScene) && targetScene != active)
        {
            SceneLoader.BeginLoadThroughLoadingScreen(targetScene);
        }
        else
        {
            if (active == data.sceneName)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    player.transform.position = data.playerPosition;
            }

            CoinManager.Instance?.UpdateCoinUI();
            InventoryManager.NotifyInventoryChanged();
            PlayerStateManager.Instance?.RequestResyncPlayerVitals();
        }

        Debug.Log($"[SaveSlotSystem] 從槽位 {slot} 載入");
    }
    
    public static bool DeleteSlot(int slot)
    {
        try
        {
            string savePath = GetPath(slot);
            string thumbPath = GetThumbnailPath(slot);
            
            if (File.Exists(savePath))
                File.Delete(savePath);
            
            if (File.Exists(thumbPath))
                File.Delete(thumbPath);
            
            Debug.Log($"[SaveSlotSystem] 刪除槽位 {slot}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSlotSystem] 刪除失敗: {e.Message}");
            return false;
        }
    }

    /// <summary>刪除 persistentDataPath/Saves 內所有手動／自動存檔 JSON 與縮圖（與 PlayerPrefs.DeleteAll 一併使用）。</summary>
    public static void DeleteAllSaveFiles()
    {
        string dir = Path.Combine(Application.persistentDataPath, "Saves");
        if (!Directory.Exists(dir))
            return;
        try
        {
            foreach (string f in Directory.GetFiles(dir, "SaveSlot_*.json"))
            {
                try { File.Delete(f); }
                catch (System.Exception e) { Debug.LogWarning($"[SaveSlotSystem] 刪除失敗 {f}: {e.Message}"); }
            }
            foreach (string f in Directory.GetFiles(dir, "Thumbnail_*.png"))
            {
                try { File.Delete(f); }
                catch (System.Exception e) { Debug.LogWarning($"[SaveSlotSystem] 刪除縮圖失敗 {f}: {e.Message}"); }
            }
            Debug.Log("[SaveSlotSystem] 已刪除所有存檔檔案");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSlotSystem] DeleteAllSaveFiles: {e.Message}");
        }
    }

    public static bool HasSaveSlot(int slot)
    {
        return File.Exists(GetPath(slot));
    }

    public static SaveSlotData GetSlotPreview(int slot)
    {
        string path = GetPath(slot);
        if (!File.Exists(path)) return null;

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<SaveSlotData>(json);
    }
    
    public static Texture2D LoadThumbnail(int slot)
    {
        try
        {
            string path = GetThumbnailPath(slot);
            if (!File.Exists(path))
            {
                Debug.Log($"[SaveSlotSystem] 槽位 {slot} 的縮圖檔案不存在: {path}");
                return null;
            }
            
            byte[] data = File.ReadAllBytes(path);
            if (data == null || data.Length == 0)
            {
                Debug.LogWarning($"[SaveSlotSystem] 槽位 {slot} 的縮圖檔案為空");
                return null;
            }
            
            Texture2D tex = new Texture2D(2, 2);
            
            // 方法 1：嘗試使用 ImageConversion.LoadImage（Unity 2017.2+）
            var imageConversionType = System.Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule");
            if (imageConversionType == null)
                imageConversionType = System.Type.GetType("UnityEngine.ImageConversion");
            
            if (imageConversionType != null)
            {
                var loadMethod = imageConversionType.GetMethod("LoadImage", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                    null, new System.Type[] { typeof(Texture2D), typeof(byte[]) }, null);
                if (loadMethod != null)
                {
                    bool result = (bool)loadMethod.Invoke(null, new object[] { tex, data });
                    if (result)
                    {
                        Debug.Log($"[SaveSlotSystem] 成功載入縮圖到槽位 {slot}（使用 ImageConversion）");
                        return tex;
                    }
                }
            }
            
            // 方法 2：嘗試使用 Texture2D.LoadImage（舊版本）
            var loadImageMethod = typeof(Texture2D).GetMethod("LoadImage", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                null, new System.Type[] { typeof(byte[]) }, null);
            if (loadImageMethod != null)
            {
                bool result = (bool)loadImageMethod.Invoke(tex, new object[] { data });
                if (result)
                {
                    Debug.Log($"[SaveSlotSystem] 成功載入縮圖到槽位 {slot}（使用 Texture2D.LoadImage）");
                    return tex;
                }
            }
            
            Debug.LogWarning($"[SaveSlotSystem] 無法載入縮圖到槽位 {slot}：找不到可用的載入方法");
            UnityEngine.Object.Destroy(tex);
            return null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSlotSystem] 載入縮圖失敗 (槽位 {slot}): {e.Message}\n{e.StackTrace}");
            return null;
        }
    }
    
    /// <summary>
    /// 捕獲場景縮圖（用於存檔預覽）
    /// </summary>
    public static Texture2D CaptureThumbnail(int width = 320, int height = 180)
    {
        RenderTexture rt = new RenderTexture(width, height, 24);
        Camera mainCam = Camera.main;
        
        if (mainCam == null)
        {
            Debug.LogWarning("[SaveSlotSystem] 找不到主攝像機");
            return null;
        }
        
        RenderTexture previous = RenderTexture.active;
        mainCam.targetTexture = rt;
        Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        
        mainCam.Render();
        RenderTexture.active = rt;
        screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenshot.Apply();
        
        mainCam.targetTexture = null;
        RenderTexture.active = previous;
        rt.Release();
        
        return screenshot;
    }
    
    /// <summary>
    /// 自動存檔（使用 Slot 0）
    /// </summary>
    public static void AutoSave()
    {
        SaveToSlot(AUTO_SAVE_SLOT);
        Debug.Log("[SaveSlotSystem] 自動存檔完成");
    }
    
    /// <summary>
    /// 載入自動存檔
    /// </summary>
    public static void LoadAutoSave()
    {
        LoadFromSlot(AUTO_SAVE_SLOT);
    }
}
