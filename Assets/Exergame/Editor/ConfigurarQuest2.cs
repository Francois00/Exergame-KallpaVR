using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.OpenXR;

namespace Exergame.EditorTools
{
    public static class ConfigurarQuest2
    {
        const string RutaApk = "Builds/ExergameVR.apk";

        [MenuItem("Exergame/Configurar para Meta Quest 2")]
        public static void Configurar()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                if (!EditorUtility.DisplayDialog("Meta Quest 2",
                        "Se cambiara la plataforma a Android. Puede tardar varios minutos. Continuar?", "Si", "Cancelar"))
                    return;
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            PlayerSettings.companyName = "UCSM";
            PlayerSettings.productName = "Exergame VR";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "pe.edu.ucsm.exergamevr");
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            PlayerSettings.Android.forceInternetPermission = true;

            int android = ActivarFuncionesOpenXR(BuildTargetGroup.Android, new[] { "MetaQuestFeature", "OculusTouchControllerProfile" }, new[] { "OculusQuestFeature" });
            int pc = ActivarFuncionesOpenXR(BuildTargetGroup.Standalone, new[] { "OculusTouchControllerProfile" }, new string[0]);
            PermitirInternetEnQuest();

            AssetDatabase.SaveAssets();
            Debug.Log($"[Exergame] Proyecto configurado para Meta Quest 2. Funciones OpenXR activadas: Android {android}, PC {pc}.");
            EditorUtility.DisplayDialog("Meta Quest 2",
                "Listo. Revisa en Project Settings > XR Plug-in Management > OpenXR (pestaña Android) que no haya advertencias en rojo.", "OK");
        }

        static int ActivarFuncionesOpenXR(BuildTargetGroup grupo, string[] activar, string[] desactivar)
        {
            var ajustes = OpenXRSettings.GetSettingsForBuildTargetGroup(grupo);
            if (ajustes == null)
            {
                Debug.LogWarning("[Exergame] No se encontraron ajustes de OpenXR para " + grupo);
                return 0;
            }
            int cambios = 0;
            foreach (var funcion in ajustes.GetFeatures())
            {
                string nombre = funcion.GetType().Name;
                if (activar.Contains(nombre) && !funcion.enabled) { funcion.enabled = true; cambios++; }
                if (desactivar.Contains(nombre) && funcion.enabled) { funcion.enabled = false; }
                if (activar.Contains(nombre)) EditorUtility.SetDirty(funcion);
            }
            EditorUtility.SetDirty(ajustes);
            return ajustes.GetFeatures().Count(f => f.enabled && activar.Contains(f.GetType().Name));
        }

        // El panel del evaluador abre un servidor web en el visor: sin el permiso INTERNET Android responde "Access denied".
        static void PermitirInternetEnQuest()
        {
            var ajustes = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (ajustes == null) return;
            foreach (var funcion in ajustes.GetFeatures().Where(f => f.GetType().Name == "MetaQuestFeature"))
            {
                var so = new SerializedObject(funcion);
                var prop = so.FindProperty("forceRemoveInternetPermission");
                if (prop == null || !prop.boolValue) continue;
                prop.boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(funcion);
                Debug.Log("[Exergame] Permiso de Internet habilitado para el panel del evaluador en el visor.");
            }
        }

        [MenuItem("Exergame/Solo crear el APK")]
        public static void SoloCrearApk()
        {
            Compilar(BuildOptions.None);
        }

        [MenuItem("Exergame/Compilar e instalar en Quest (USB)")]
        public static void CompilarEInstalar()
        {
            Compilar(BuildOptions.AutoRunPlayer);
        }

        static void Compilar(BuildOptions opcionesBuild)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUtility.DisplayDialog("Meta Quest 2", "Primero ejecuta Exergame > Configurar para Meta Quest 2.", "OK");
                return;
            }

            var escenas = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (escenas.Length == 0)
            {
                EditorUtility.DisplayDialog("Meta Quest 2", "No hay escenas en Build Settings. Ejecuta Exergame > Construir sala.", "OK");
                return;
            }

            PermitirInternetEnQuest();
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory(Path.GetDirectoryName(RutaApk));
            var opciones = new BuildPlayerOptions
            {
                scenes = escenas,
                locationPathName = RutaApk,
                target = BuildTarget.Android,
                options = opcionesBuild
            };
            var reporte = BuildPipeline.BuildPlayer(opciones);
            if (reporte.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Exergame] APK creado: {Path.GetFullPath(RutaApk)} ({reporte.summary.totalSize / (1024 * 1024)} MB)");
                EditorUtility.RevealInFinder(RutaApk);
            }
            else
                Debug.LogError("[Exergame] La compilacion fallo. Revisa los errores en la Console.");
        }
    }
}
