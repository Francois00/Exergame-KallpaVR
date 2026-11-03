using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Exergame.Herramientas
{
    /// <summary>
    /// Crea un .zip del proyecto listo para pasar a otra PC: solo Assets, Packages,
    /// ProjectSettings y Documentacion (con sus .meta). No incluye Library, Temp, Logs,
    /// UserSettings ni APKs: esas carpetas se regeneran solas en la otra PC.
    /// </summary>
    public static class EmpaquetarProyecto
    {
        static readonly string[] Carpetas = { "Assets", "Packages", "ProjectSettings", "Documentacion" };
        static readonly string[] ExtensionesFuera = { ".apk", ".aab", ".obb", ".tmp" };

        [MenuItem("Exergame/Crear paquete para compartir (.zip)", priority = 1)]
        public static void Crear()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Exergame", "Primero detenga el modo Play.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.SaveAssets();

            string raiz = Directory.GetParent(Application.dataPath).FullName;
            string destinoCarpeta = Directory.GetParent(raiz).FullName;
            string nombre = "Exergame_para_compartir_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".zip";
            string destino = Path.Combine(destinoCarpeta, nombre);

            var archivos = new List<string>();
            foreach (var c in Carpetas)
            {
                string ruta = Path.Combine(raiz, c);
                if (!Directory.Exists(ruta)) continue;
                foreach (var f in Directory.GetFiles(ruta, "*", SearchOption.AllDirectories))
                {
                    string ext = Path.GetExtension(f).ToLowerInvariant();
                    if (Array.IndexOf(ExtensionesFuera, ext) >= 0) continue;
                    if (Path.GetFileName(f).StartsWith("~")) continue;
                    archivos.Add(f);
                }
            }
            // Archivos sueltos utiles en la raiz.
            foreach (var suelto in new[] { "LEEME.txt", "Diagnostico_Exergame.txt" })
            {
                string f = Path.Combine(raiz, suelto);
                if (File.Exists(f)) archivos.Add(f);
            }

            long bytes = 0;
            int hechos = 0;
            try
            {
                if (File.Exists(destino)) File.Delete(destino);
                using (var flujo = new FileStream(destino, FileMode.Create))
                using (var zip = new ZipArchive(flujo, ZipArchiveMode.Create))
                {
                    foreach (var f in archivos)
                    {
                        string relativo = f.Substring(raiz.Length).TrimStart('\\', '/').Replace('\\', '/');
                        if (hechos % 25 == 0 &&
                            EditorUtility.DisplayCancelableProgressBar("Exergame: creando paquete", relativo, hechos / (float)archivos.Count))
                            throw new OperationCanceledException();
                        var entrada = zip.CreateEntry("Exergame/" + relativo, System.IO.Compression.CompressionLevel.Optimal);
                        entrada.LastWriteTime = File.GetLastWriteTime(f);
                        using (var origen = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var dst = entrada.Open())
                            origen.CopyTo(dst);
                        bytes += new FileInfo(f).Length;
                        hechos++;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                EditorUtility.ClearProgressBar();
                if (File.Exists(destino)) File.Delete(destino);
                Debug.Log("[Exergame] Paquete cancelado.");
                return;
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError("[Exergame] No se pudo crear el paquete: " + e.Message);
                EditorUtility.DisplayDialog("Exergame", "No se pudo crear el paquete:\n" + e.Message, "OK");
                return;
            }
            EditorUtility.ClearProgressBar();

            long tamZip = new FileInfo(destino).Length;
            Debug.Log("[Exergame] Paquete creado: " + destino + " (" + hechos + " archivos, " +
                      (bytes / 1048576f).ToString("0.0") + " MB sin comprimir, " + (tamZip / 1048576f).ToString("0.0") + " MB zip)");
            EditorUtility.RevealInFinder(destino);
            EditorUtility.DisplayDialog("Exergame",
                "Paquete creado:\n" + destino + "\n\n" + hechos + " archivos, " + (tamZip / 1048576f).ToString("0") + " MB.\n\n" +
                "En la otra PC: descomprimir, abrir con Unity 2022.3.62f3 (con internet la primera vez) y ejecutar Exergame > Diagnostico del proyecto.",
                "OK");
        }
    }
}
