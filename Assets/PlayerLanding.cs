using UnityEngine;
using TMPro;

public class PlayerLanding : MonoBehaviour
{
    public Transform playerTransform; // Assign the Sand's transform in the Unity Editor.
    public TMP_Text scoreText; // Assign the UI Text element for displaying the score.
    private float highScore = 0; // Variable to store the high score.


    // This function is triggered when the player's collider touches another collider.
    void OnTriggerEnter2D(Collider2D collision)
    {
        
        // Check if the player landed on the "Sand" object.
        if (collision.gameObject.tag == "Player")
        {   
            // Calculate the distance in x-axis from the player to the Sand.
            float distanceX = Mathf.Abs(transform.position.x - playerTransform.position.x) * 100;
            Debug.Log("Collision detected with: " + collision.gameObject.name + distanceX);
            // Update the score text with the distance.
            if (distanceX > highScore)
            {
                // Update the high score.
                highScore = distanceX;

                // Update the high score text.
            }
            scoreText.text = "High Score: " + highScore + "\n" + "Score: " + distanceX.ToString("F2"); // "F2" for 2 decimal places.
        }
    }
    void Awake()
    {
        scoreText.text = "High Score: " + highScore;
    }
}
