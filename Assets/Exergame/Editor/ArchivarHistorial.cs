using UnityEditor;
using UnityEngine;

namespace Exergame.EditorTools
{
    /// <summary>
    /// Menu para sacar del historial las sesiones de prueba guardadas en esta PC (Unity con Meta Link).
    /// No borra: mueve los CSV a la carpeta Archivo_pruebas.
    /// </summary>
    public static class ArchivarHistorial
    {
        [MenuItem("Exergame/Archivar historial de pruebas (esta PC)")]
        static void Archivar()
        {
            string carpeta = Application.persistentDataPath;
            int n = ArchivoDatos.Contar(carpeta);
            if (n == 0)
            {
                EditorUtility.DisplayDialog("Exergame", "No hay sesiones en el historial de esta PC.\n\n" + carpeta, "OK");
                return;
            }
            if (!EditorUtility.DisplayDialog("Exergame",
                    $"Se sacaran del historial {n} sesion(es) guardadas en esta PC.\n\nNo se borran: se mueven a la carpeta \"{ArchivoDatos.NombreCarpetaArchivo}\".\n\n{carpeta}",
                    "Archivar", "Cancelar")) return;
            int m = ArchivoDatos.ArchivarTodo(carpeta);
            EditorUtility.DisplayDialog("Exergame", $"Listo: {m} sesion(es) archivadas. El historial quedo vacio.", "OK");
            EditorUtility.RevealInFinder(System.IO.Path.Combine(carpeta, ArchivoDatos.NombreCarpetaArchivo));
        }
    }
}
