using UnityEngine;
using UnityEngine.UI; // Needed for UI if you're displaying score on the screen.

public class PlayerLanding : MonoBehaviour
{
    public Transform playerTransform; // Assign the Sand's transform in the Unity Editor.
    public Text scoreText; // Assign the UI Text element for displaying the score.

    // This function is triggered when the player's collider touches another collider.
    void OnTriggerEnter2D(Collider2D collision)
    {
        
        // Check if the player landed on the "Sand" object.
        if (collision.gameObject.tag == "Player")
        {   
            // Calculate the distance in x-axis from the player to the Sand.
            float distanceX = Mathf.Abs(transform.position.x - playerTransform.position.x);
            Debug.Log("Collision detected with: " + collision.gameObject.name + distanceX);
            // Update the score text with the distance.
            scoreText.text = "Score: " + distanceX.ToString("F2"); // "F2" for 2 decimal places.
        }
    }
}
