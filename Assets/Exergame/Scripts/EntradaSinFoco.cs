using UnityEngine;
using UnityEngine.InputSystem;

namespace Exergame
{
    /// <summary>
    /// Con Meta Link el juego corre dentro del editor de Unity. Cuando el evaluador usa el panel en Chrome,
    /// Unity pierde el foco y, por defecto, el Input System deja de leer los controles: las manos desaparecen.
    /// Esto hace que los controles y el visor se sigan leyendo aunque Unity no tenga el foco.
    /// </summary>
    public static class EntradaSinFoco
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Configurar()
        {
            Application.runInBackground = true;
            var s = InputSystem.settings;
            if (s == null) return;
            s.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            s.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            Debug.Log("[Exergame] Controles activos aunque Unity no tenga el foco (panel en Chrome).");
        }
    }
}
