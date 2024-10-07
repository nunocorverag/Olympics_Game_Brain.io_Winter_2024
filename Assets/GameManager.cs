using UnityEngine;

public class GameManager : MonoBehaviour
{
    private float totalScore;

    void Start()
    {
        // Inicializamos el score desde PlayerPrefs o con 0 si no existe
        totalScore = PlayerPrefs.GetFloat("TotalScore", 0);
    }

    // M�todo que llamas cuando el jugador completa una prueba
    public void AddScore(float points)
    {
        totalScore += points;
        PlayerPrefs.SetFloat("TotalScore", totalScore);
        PlayerPrefs.Save();
    }

    public float GetScore()
    {
        return totalScore;
    }

    // Mostrar el total acumulado al terminar de jugar
    public void EndGame()
    {
        int finalScore = PlayerPrefs.GetInt("TotalScore", 0);
        Debug.Log("Puntuaci�n total: " + finalScore);

        // Aqu� podr�as mostrar el score final en la UI del juego
        // Ejemplo: scoreText.text = "Puntuaci�n total: " + finalScore;
        
        // Limpiar el score despu�s de mostrarlo si lo deseas
        PlayerPrefs.DeleteKey("TotalScore");
    }
}