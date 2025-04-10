using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneChanger : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string sceneToLoad = "NextScene"; // Name of the scene to load
    [SerializeField] private float delayBeforeChange = 2f; // Time delay before scene change

    private void OnTriggerEnter(Collider other)
    {
        StartCoroutine(ChangeSceneAfterDelay());
    }

    private IEnumerator ChangeSceneAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeChange);
        SceneManager.LoadScene(sceneToLoad);
    }
}