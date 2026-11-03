using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Exergame.EditorTools
{
    /// <summary>
    /// Asigna las voces grabadas de la presentacion y la practica (frases 12 a 17 del guion)
    /// a la sesion de la escena abierta. No mueve nada de la escena.
    /// </summary>
    public static class AsignarVoces
    {
        const string Carpeta = "Assets/Exergame/Audio/Voz/";

        static int n;

        static AudioClip C(string nombre)
        {
            var c = AssetDatabase.LoadAssetAtPath<AudioClip>(Carpeta + nombre + ".wav");
            if (c != null) n++; else Debug.LogWarning("[Exergame] Falta la voz " + nombre + ".wav");
            return c;
        }

        // Al abrir Unity: si la escena tiene esas voces vacias y los archivos existen, se asignan solas.
        [InitializeOnLoadMethod]
        static void AlAbrir()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                var sesion = Object.FindObjectOfType<SesionExergame>(true);
                if (sesion == null) return;
                bool faltan = sesion.audioPresentacion == null || sesion.audioPracticaInicio == null || sesion.audioPracticaIzquierda == null
                              || sesion.audioPracticaDerecha == null || sesion.audioPracticaFin == null || sesion.audioPracticaOtraMano == null
                              || sesion.audioRecuerdeMuestra == null || sesion.audioManosLibresClasico == null
                              || sesion.audioManosLibresGlobos == null || sesion.audioManosLibresTambores == null;
                if (faltan && AssetDatabase.LoadAssetAtPath<AudioClip>(Carpeta + "presentacion.wav") != null) Asignar(false);
            };
        }

        [MenuItem("Exergame/Asignar voces grabadas (12 a 21)")]
        static void AsignarMenu() => Asignar(true);

        public static void Asignar(bool avisar)
        {
            var sesion = Object.FindObjectOfType<SesionExergame>(true);
            if (sesion == null)
            {
                EditorUtility.DisplayDialog("Exergame", "Abra la escena SalaExergame primero.", "OK");
                return;
            }
            Undo.RecordObject(sesion, "Asignar voces");
            n = 0;
            sesion.audioPresentacion = C("presentacion");
            sesion.audioPracticaInicio = C("practica_inicio");
            sesion.audioPracticaIzquierda = C("practica_izquierda");
            sesion.audioPracticaDerecha = C("practica_derecha");
            sesion.audioPracticaFin = C("practica_fin");
            sesion.audioPracticaOtraMano = C("practica_otra_mano");
            // Recordatorio de la regla "toque el color que ve" y frases de manos libres (18 a 21).
            sesion.audioRecuerdeMuestra = C("recuerde_muestra");
            sesion.audioManosLibresClasico = C("manos_libres_clasico");
            sesion.audioManosLibresGlobos = C("manos_libres_globos");
            sesion.audioManosLibresTambores = C("manos_libres_tambores");
            EditorUtility.SetDirty(sesion);
            EditorSceneManager.MarkSceneDirty(sesion.gameObject.scene);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[Exergame] Voces asignadas: {n} de 10 (presentacion, practica, regla y manos libres).");
            if (avisar) EditorUtility.DisplayDialog("Exergame", $"Voces asignadas: {n} de 10.", "OK");
        }
    }
}
