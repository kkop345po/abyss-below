/// <summary>
/// NPC 或互動物件預留接口：取得實作後呼叫即可開啟賭博介面（通常由 GambleUIController 實作）。
/// </summary>
public interface IGambleNpcTrigger
{
    void OpenGambleFromNpc();
}
