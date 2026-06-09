using System.Collections;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    public GameObject itemPrefab;
    [Tooltip("Tempo em segundos entre o surgimento de cada item")]
    public float spawnInterval = 3f;
    public bool isSpawning = true;

    private void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            if (isSpawning && itemPrefab != null)
            {
                Instantiate(itemPrefab, transform.position, Quaternion.identity);
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }
}
