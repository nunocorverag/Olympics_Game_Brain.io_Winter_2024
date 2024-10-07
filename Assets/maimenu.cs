using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class maimenu : MonoBehaviour
{
    public void FenceGame ()
    {
        SceneManager.LoadScene(2);
    }

    public void JumpGame ()
    {
        SceneManager.LoadScene(1);
    }
}
