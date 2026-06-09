using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SKAISceneSearcher : EditorWindow
{
    [MenuItem("SKAI/Search Scene Objects")]
    public static void SearchObjects()
    {
        Debug.ClearDeveloperConsole();
        Debug.Log("🔍 === [SKAI SEARCH] Procurando objetos na cena... ===");

        GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        List<string> matches = new List<string>();

        string[] keywords = { "telão", "tela", "screen", "tv", "board", "video", "sala", "estudo", "aprend", "quad", "monitor" };

        foreach (GameObject go in allObjects)
        {
            string nameLower = go.name.ToLower();
            bool matched = false;
            foreach (string kw in keywords)
            {
                if (nameLower.Contains(kw))
                {
                    matched = true;
                    break;
                }
            }

            if (matched)
            {
                // Mostra o caminho completo na hierarquia
                string path = GetGameObjectPath(go);
                matches.Add($"{path} - Active: {go.activeInHierarchy} - Static: {go.isStatic}");
            }
        }

        Debug.Log($"Encontrados {matches.Count} objetos correspondentes:");
        foreach (string m in matches)
        {
            Debug.Log(m);
        }
        Debug.Log("🔍 =================================================");
    }

    private static string GetGameObjectPath(GameObject obj)
    {
        string path = "/" + obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = "/" + obj.name + path;
        }
        return path;
    }
}
