using UnityEditor;
using UnityEngine;

public static class ScenePortalEditor
{
	[MenuItem("GameObject/Portals/Create Portal → Sunken ship", false, 10)]
	public static void CreatePortalToSunkenShip()
	{
		CreatePortal("Sunken ship");
	}

	private static void CreatePortal(string targetScene)
	{
		GameObject go = new GameObject($"Portal_{targetScene}");
		Undo.RegisterCreatedObjectUndo(go, "Create Portal");

		// 放到場景中心或當前選取物件下
		if (Selection.activeTransform != null)
		{
			go.transform.SetParent(Selection.activeTransform, false);
			go.transform.localPosition = Vector3.zero;
		}
		else
		{
			go.transform.position = Vector3.zero;
		}

		// Collider
		BoxCollider2D col = go.AddComponent<BoxCollider2D>();
		col.isTrigger = true;
		col.size = new Vector2(2f, 2f);

		// ScenePortal 組件
		ScenePortal portal = go.AddComponent<ScenePortal>();
		portal.targetSceneName = targetScene;
		portal.useLoadingScene = true;
		portal.requireInteractKey = false;

		Selection.activeObject = go;
		EditorGUIUtility.PingObject(go);
	}
}


