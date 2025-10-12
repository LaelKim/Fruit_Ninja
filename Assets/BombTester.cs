// BombTester.cs - À ajouter à un GameObject vide dans la scène
using UnityEngine;

public class BombTester : MonoBehaviour
{
    public BombController bombToTest;
    public KeyCode testKey = KeyCode.B;
    
    void Update()
    {
        if (Input.GetKeyDown(testKey) && bombToTest != null)
        {
            bombToTest.OnTouch();
            Debug.Log($"💣 Bomb touched! (Key {testKey} pressed)");
        }
        
        // Reset avec R
        if (Input.GetKeyDown(KeyCode.R) && bombToTest != null)
        {
            // Forcer le reset si besoin
            Debug.Log("🔄 Reset bomb manually");
            // Vous pouvez ajouter une méthode Reset() dans BombController si nécessaire
        }
    }
}