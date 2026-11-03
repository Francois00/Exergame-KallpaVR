using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Exergame.EditorTools
{
    /// <summary>
    /// Permite pedir acciones al editor abierto desde fuera (por ejemplo un asistente o un script):
    /// escribir "build" o "play" en Temp/exergame_orden.txt. El resultado queda en Temp/exergame_resultado.txt.
    /// </summary>
    [InitializeOnLoad]
    public static class DisparadorExterno
    {
        const string RutaOrden = "Temp/exergame_orden.txt";
        const string RutaResultado = "Temp/exergame_resultado.txt";
        static double proximaRevision;

        static DisparadorExterno()
        {
            EditorApplication.update += Revisar;
        }

        static void Revisar()
        {
            if (EditorApplication.timeSinceStartup < proximaRevision) return;
            proximaRevision = EditorApplication.timeSinceStartup + 1.0;
            if (!File.Exists(RutaOrden)) return;

            string orden = File.ReadAllText(RutaOrden).Trim().ToLowerInvariant();
            File.Delete(RutaOrden);
            EditorApplication.delayCall += () => Ejecutar(orden);
        }

        static void Ejecutar(string orden)
        {
            try
            {
                switch (orden)
                {
                    case "build":
                        File.WriteAllText(RutaResultado, "compilando " + DateTime.Now);
                        ConfigurarQuest2.SoloCrearApk();
                        bool ok = File.Exists("Builds/ExergameVR.apk") &&
                                  File.GetLastWriteTime("Builds/ExergameVR.apk") > DateTime.Now.AddMinutes(-60);
                        File.WriteAllText(RutaResultado, (ok ? "ok " : "fallo ") + DateTime.Now);
                        break;
                    case "android":
                        File.WriteAllText(RutaResultado, "cambiando a android " + DateTime.Now);
                        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                        File.WriteAllText(RutaResultado, "android " + EditorUserBuildSettings.activeBuildTarget + " " + DateTime.Now);
                        break;
                    case "configurar":
                        ConfigurarQuest2.Configurar();
                        File.WriteAllText(RutaResultado, "configurado " + DateTime.Now);
                        break;
                    case "puerto":
                        var panel = UnityEngine.Object.FindObjectOfType<PanelEvaluador>(true);
                        if (panel == null) { File.WriteAllText(RutaResultado, "fallo sin PanelEvaluador en la escena"); break; }
                        Undo.RecordObject(panel, "Puerto del panel");
                        panel.puerto = 8095;
                        EditorUtility.SetDirty(panel);
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
                        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(panel.gameObject.scene);
                        File.WriteAllText(RutaResultado, "puerto " + panel.puerto + " " + DateTime.Now);
                        break;
                    case "play":
                        EditorApplication.isPlaying = true;
                        File.WriteAllText(RutaResultado, "play " + DateTime.Now);
                        break;
                    default:
                        File.WriteAllText(RutaResultado, "orden desconocida: " + orden);
                        break;
                }
            }
            catch (Exception e)
            {
                File.WriteAllText(RutaResultado, "fallo " + e.Message);
                Debug.LogException(e);
            }
        }
    }
}
