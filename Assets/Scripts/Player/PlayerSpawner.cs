using UnityEngine;

/// <summary>
/// Marca un punto específico en una escena como un posible lugar de aparición (spawn)
/// o punto de entrada para el jugador después de una transición de escena.
/// Se identifica mediante un string 'entryPointIdentifier'.
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    [Tooltip("Identificador único en formato string para este punto de entrada específico. Debe coincidir con el ID usado por el SceneTransitioner.")]
    [SerializeField] public string entryPointIdentifier = "EntradaPorDefecto"; // Público para ser leído por Player.cs

    /// <summary>
    /// Se llama en el editor cuando el GameObject está seleccionado.
    /// Dibuja un Gizmo (una esfera azul) en la posición del spawner para facilitar su visualización en la escena.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan; // Cambiado a cyan para diferenciar de otros gizmos azules.
        Gizmos.DrawSphere(transform.position, 0.4f); // Dibuja una esfera para marcar la posición.
    }
}