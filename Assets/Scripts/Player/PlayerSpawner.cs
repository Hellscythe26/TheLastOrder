// PlayerSpawner.cs
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Tooltip("Identificador único para este punto de entrada específico.")]
    [SerializeField] public string entryPointIdentifier = "EntradaPorDefecto"; // Hazlo público o mantenlo serializado

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.position, 0.5f); // Dibuja una esfera azul
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1f); // Indica dirección si importa
    }
}