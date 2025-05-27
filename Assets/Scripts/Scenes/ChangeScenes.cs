using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para usar SceneManager.

/// <summary>
/// Proporciona una funcionalidad simple para cambiar a otra escena del juego.
/// </summary>
public class ChangeScenes : MonoBehaviour
{
    /// <summary>
    /// Carga una escena especificada por su nombre.
    /// Este método es público para poder ser llamado desde eventos de UI (como OnClick de un botón)
    /// o desde otros scripts.
    /// </summary>
    /// <param name="sceneName">El nombre exacto del archivo de escena que se desea cargar (debe estar en Build Settings).</param>
    public void LoadSceneByName(string sceneName) // Nombre del método cambiado a LoadSceneByName para mayor claridad
    {
        // Verifica si el nombre de la escena proporcionado no es nulo o vacío.
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("ChangeScenes.LoadSceneByName: El nombre de la escena no puede ser nulo o vacío.", this);
            return;
        }

        // Utiliza SceneManager para cargar la escena especificada.
        SceneManager.LoadScene(sceneName);
    }

    public void gameEntry(string name)
    {
        LoadSceneByName(name);
    }
}