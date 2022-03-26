using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleController : MonoBehaviour
{
    public void OnServerButtonClicked()
    {
        SceneManager.LoadScene("Scenes/Server");
    }

    public void OnClientButtonClicked()
    {
        SceneManager.LoadScene("Scenes/Client");
    }
}
