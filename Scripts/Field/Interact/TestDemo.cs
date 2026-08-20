using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestDemo : InteractObject
{
    protected override void OnInteraction()
    {
        SceneManager.LoadScene("TestDemoScene");
    }

    protected override void LoadState(bool isActivated)
    {
    }
}
