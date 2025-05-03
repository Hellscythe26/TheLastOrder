using UnityEngine;
using UnityEngine.UI; // Necesario para trabajar con componentes UI como Image
using System.Collections.Generic; // Opcional si prefieres usar List en lugar de Array

public class MainMenuBackgroundChanger : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Arrastra aquí el componente Image del fondo del menú principal.")]
    [SerializeField] private Image backgroundImageComponent;

    [Header("Background Options")]
    [Tooltip("Arrastra aquí los 4 Sprites que quieres usar como fondo.")]
    [SerializeField] private Sprite[] possibleBackgrounds; // Un array para guardar los 4 Sprites

    // Awake se llama muy temprano, ideal para configurar cosas iniciales
    void Awake()
    {
        // --- Verificaciones iniciales ---
        if (backgroundImageComponent == null)
        {
            //Debug.LogError("Error: No se ha asignado el componente Image del fondo en el Inspector.", this.gameObject);
            return; // Salir si no hay componente de imagen asignado
        }

        if (possibleBackgrounds == null || possibleBackgrounds.Length == 0)
        {
            //Debug.LogError("Error: No se han asignado Sprites de fondo en el array 'Possible Backgrounds' en el Inspector.", this.gameObject);
            return; // Salir si no hay sprites
        }
        // Opcional: Verificar si son exactamente 4 si es un requisito estricto
        // if (possibleBackgrounds.Length != 4) {
        //     Debug.LogWarning("Se esperaba tener 4 imágenes de fondo, pero se encontraron " + possibleBackgrounds.Length, this.gameObject);
        // }


        // --- Lógica de Selección Aleatoria ---

        // 1. Generar un número entero aleatorio entre 0 y el número de imágenes disponibles - 1.
        // Random.Range(minInclusive, maxExclusive) para enteros.
        // Si tienes 4 imágenes, sus índices son 0, 1, 2, 3. Necesitamos un número entre 0 y 3.
        int randomIndex = Random.Range(0, possibleBackgrounds.Length); // Genera 0, 1, 2, o 3 si Length es 4

        // 2. Seleccionar el Sprite usando el índice aleatorio
        Sprite chosenSprite = possibleBackgrounds[randomIndex];

        // 3. Asignar el Sprite elegido al componente Image del fondo
        backgroundImageComponent.sprite = chosenSprite;

        //Debug.Log($"Se ha establecido el fondo del menú principal en: {chosenSprite.name} (Índice: {randomIndex})");
    }
}