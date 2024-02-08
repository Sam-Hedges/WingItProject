using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BaseManager : MonoBehaviour
{
    private float currentHealth;
    [Range(0f, 1000f)]public float maximumHealth;
    public Image healthBar;

    public static BaseManager instance;

    void Awake()
    {
        instance = GetComponent<BaseManager>();
    }

    // Use this for initialization
    void Start()
    {
        currentHealth = maximumHealth;
    }

    public void Damage(int damage)
    {
        currentHealth -= damage;

        healthBar.fillAmount = currentHealth / maximumHealth;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        
    }
}