// --- En UIHealthBar.cs ---
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIHealthBar : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public Sprite fullHeart;
    public Sprite halfHeart; // Necesario para la lógica 1 a 1 con medios
    public Sprite emptyHeart;
    // public float pointsPerHeart = 1f; // <-- ELIMINADO
    public GameObject heartPrefab;

    private List<Image> heartImages = new List<Image>();

    private void Start()
    {
        if (Player.Instance != null) // Usa la instancia Singleton de Player.cs
        {
            playerHealth = Player.Instance.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                 Debug.LogError("¡No se encontró el componente PlayerHealth en Player.Instance!", this.gameObject);
                 return;
            }
        }
        else
        {
             Debug.LogError("¡No se encontró Player.Instance! Asegúrate de que el jugador exista y use DontDestroyOnLoad.", this.gameObject);
             return; // Salir si no hay jugador
        }
        if (heartPrefab == null)
        {
            Debug.LogError("Heart Prefab no está asignado en UIHealthBar.", this.gameObject);
            return;
        }

        // Suscribirse a los eventos
        playerHealth.OnHealthChanged.AddListener(UpdateHealthBarSprites);
        playerHealth.OnMaxHealthChanged.AddListener(SetupHearts);
        SetupHearts();
        // La configuración inicial ocurrirá vía eventos desde PlayerHealth.Start()
    }

    // Reconstruye los contenedores de corazón
    void SetupHearts()
    {
        // Limpiar bien
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        heartImages.Clear();

        // Obtener la capacidad máxima ACTUAL de contenedores de corazón
        int numberOfHeartContainers = playerHealth.CurrentMaxHearts;

        // Instanciar los contenedores
        for (int i = 0; i < numberOfHeartContainers; i++)
        {
            GameObject heartInstance = Instantiate(heartPrefab, transform);
            heartInstance.name = "Heart_" + i;
            Image heartImage = heartInstance.GetComponent<Image>();
            if (heartImage != null)
            {
                heartImages.Add(heartImage);
                heartImage.sprite = emptyHeart;
                heartImage.enabled = true;
            }
             else
            {
                 Debug.LogError("El prefab del corazón no tiene un componente Image!", heartInstance);
            }
        }

        // Actualizar los sprites al estado correcto después de crear los contenedores
        UpdateHealthBarSprites(playerHealth.CurrentHealth);
    }

    // Actualiza los sprites (lleno/medio/vacío) usando la lógica 1 a 1
    void UpdateHealthBarSprites(float currentHealthValue) // Recibe vida en "corazones" (ej: 2.5f)
    {
        // Iterar sobre los contenedores de corazón existentes
        for (int i = 0; i < heartImages.Count; i++)
        {
            // Lógica original 1 a 1 (como en tu primer script UIHealthBar.cs)
            if (currentHealthValue >= (i + 1f)) // Si la vida es >= al valor que representa este corazón lleno (i+1)
            {
                heartImages[i].sprite = fullHeart;
            }
            else if (currentHealthValue > i) // Si la vida es > que el inicio de este corazón (i), pero no llega a i+1
            {
                 // Aquí podrías decidir si cualquier valor > i muestra medio corazón,
                 // o si necesitas > i + 0.5f para mostrar medio. La lógica original era "> i",
                 // pero "> i + 0.5f" es más común para representar "al menos la mitad".
                 // Usemos la lógica más precisa: > i + 0.5f para lleno, > i para medio.

                 if (currentHealthValue >= (i + 0.5f)) // Si es al menos la mitad de este corazón
                 {
                    heartImages[i].sprite = halfHeart;
                 }
                 else // Si es más que 'i' pero menos que 'i + 0.5', ¿qué mostrar?
                 {
                     // Podrías mostrar medio corazón también, o vacío.
                     // Lo más intuitivo suele ser:
                     // >= i + 1   -> Lleno
                     // >= i + 0.5 -> Medio
                     // < i + 0.5  -> Vacío (dentro del rango de este corazón 'i')
                     // Ajustemos la lógica para que sea así:
                     heartImages[i].sprite = emptyHeart; // Si no llega ni a la mitad, está vacío
                 }
            }
            else // Si la vida es <= i, este corazón y los siguientes están vacíos
            {
                heartImages[i].sprite = emptyHeart;
            }
        }

        // *** Lógica corregida y más clara para UpdateHealthBarSprites ***
        for (int i = 0; i < heartImages.Count; i++)
        {
            float heartValue = i + 1f; // El valor que representa el final de este corazón (corazón 0 -> 1.0, corazón 1 -> 2.0)
            float halfPoint = i + 0.5f; // El punto medio de este corazón

            if (currentHealthValue >= heartValue)
            {
                heartImages[i].sprite = fullHeart; // Si la vida iguala o supera el valor total de este corazón
            }
            else if (currentHealthValue >= halfPoint)
            {
                heartImages[i].sprite = halfHeart; // Si la vida iguala o supera el punto medio
            }
            else
            {
                heartImages[i].sprite = emptyHeart; // Si no llega ni al punto medio
            }
        }


    }


    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.RemoveListener(UpdateHealthBarSprites);
            playerHealth.OnMaxHealthChanged.RemoveListener(SetupHearts);
        }
    }
}