/// <summary>
/// Clase estática utilizada para comunicar entre escenas el punto de entrada
/// donde el jugador debe aparecer después de una transición de escena.
/// No se instancia en la escena, actúa como un almacén global temporal.
/// </summary>
public static class PlayerSpawnManager
{
    /// <summary>
    /// Guarda el ID del 'PlayerSpawner' en la escena de destino.
    /// El script 'Player' leerá este ID al cargar la nueva escena para posicionarse.
    /// Se establece justo antes de cargar una nueva escena y se limpia después de su uso.
    /// </summary>
    public static string entryPointID = null;
}