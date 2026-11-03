using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Exergame
{
    /// <summary>
    /// Saca sesiones del historial sin borrarlas: los CSV se mueven a la carpeta "Archivo_pruebas"
    /// (dentro de la carpeta de datos). El historial del panel solo lee la carpeta principal.
    /// Si algun dia se necesitan, se pueden devolver moviendo los archivos de vuelta.
    /// </summary>
    public static class ArchivoDatos
    {
        public const string NombreCarpetaArchivo = "Archivo_pruebas";

        static string Destino(string carpeta)
        {
            var d = Path.Combine(carpeta, NombreCarpetaArchivo, DateTime.Now.ToString("yyyyMMdd_HHmm"));
            Directory.CreateDirectory(d);
            return d;
        }

        static void Mover(string origen, string carpetaDestino)
        {
            if (!File.Exists(origen)) return;
            var dst = Path.Combine(carpetaDestino, Path.GetFileName(origen));
            if (File.Exists(dst)) dst = Path.Combine(carpetaDestino, Path.GetFileNameWithoutExtension(origen) + "_" + DateTime.Now.Ticks + ".csv");
            File.Move(origen, dst);
        }

        public static int Contar(string carpeta) =>
            Directory.Exists(carpeta) ? Directory.GetFiles(carpeta, "*_resumen.csv").Length : 0;

        /// <summary>Archiva todas las sesiones de la carpeta principal. Devuelve cuantas sesiones movio.</summary>
        public static int ArchivarTodo(string carpeta)
        {
            if (!Directory.Exists(carpeta)) return 0;
            var resumenes = Directory.GetFiles(carpeta, "*_resumen.csv");
            var ensayos = Directory.GetFiles(carpeta, "*_ensayos.csv");
            if (resumenes.Length + ensayos.Length == 0) return 0;
            var d = Destino(carpeta);
            foreach (var f in resumenes.Concat(ensayos)) Mover(f, d);
            Debug.Log($"[Exergame] {resumenes.Length} sesion(es) archivadas en {d}");
            return resumenes.Length;
        }

        /// <summary>Archiva una sesion (su resumen y sus ensayos) por el nombre del archivo de resumen.</summary>
        public static bool ArchivarSesion(string carpeta, string nombreResumen)
        {
            if (string.IsNullOrEmpty(nombreResumen) || nombreResumen.IndexOfAny(new[] { '/', '\\', ':' }) >= 0 ||
                nombreResumen.Contains("..") || !nombreResumen.EndsWith("_resumen.csv")) return false;
            var resumen = Path.Combine(carpeta, nombreResumen);
            if (!File.Exists(resumen)) return false;
            var d = Destino(carpeta);
            Mover(resumen, d);
            Mover(Path.Combine(carpeta, nombreResumen.Replace("_resumen.csv", "_ensayos.csv")), d);
            return true;
        }
    }
}
