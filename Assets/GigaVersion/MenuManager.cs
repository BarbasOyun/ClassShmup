using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public Button playButton;
    public String sceneToLoad;

    void Start()
    {
        playButton.onClick.AddListener(LoadScene);
    }

    private void LoadScene()
    {
        EnemyManager.instance.RemoveAll();
        EnemyManager.instance.ResetIncrement();
        SceneManager.LoadScene(sceneToLoad);
    }

    void Update()
    {
        
    }
}
