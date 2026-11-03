using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Exergame.Herramientas
{
    /// <summary>
    /// Diagnostico del proyecto: revisa version de Unity, paquetes, render pipeline (materiales morados),
    /// scripts del juego, XR Rig, escena, personaje y voces. Arregla lo que puede y guarda un reporte.
    /// Esta en su propio ensamblado, sin dependencias: funciona aunque los scripts del juego tengan errores.
    /// </summary>
    public class DiagnosticoProyecto : EditorWindow
    {
        public const string VersionUnity = "2022.3.62f3";
        const string RutaUrp = "Assets/Settings/Project Configuration/Performance URP Config.asset";
        const string RutaEscena = "Assets/Exergame/Scenes/SalaExergame.unity";
        const string RutaMousey = "Assets/Exergame/Personajes/Mousey/Mousey.fbx";

        enum Estado { Ok, Aviso, Error }

        class Resultado
        {
            public Estado estado;
            public string tema, detalle, solucion;
        }

        readonly List<Resultado> resultados = new List<Resultado>();
        Vector2 scroll;
        static bool revisadoAlAbrir;

        [MenuItem("Exergame/Diagnostico del proyecto", priority = 0)]
        public static void Abrir()
        {
            var w = GetWindow<DiagnosticoProyecto>("Diagnostico Exergame");
            w.minSize = new Vector2(560, 420);
            w.Revisar();
        }

        // Al abrir el proyecto: si hay errores graves, se abre la ventana sola (una vez por sesion).
        [InitializeOnLoadMethod]
        static void RevisarAlAbrir()
        {
            if (revisadoAlAbrir || SessionState.GetBool("ExergameDiagnosticoVisto", false)) return;
            revisadoAlAbrir = true;
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                var tmp = CreateInstance<DiagnosticoProyecto>();
                tmp.Revisar();
                bool grave = tmp.resultados.Any(r => r.estado == Estado.Error);
                DestroyImmediate(tmp);
                SessionState.SetBool("ExergameDiagnosticoVisto", true);
                if (grave) Abrir();
            };
        }

        void OnGUI()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Diagnostico del proyecto Exergame VR", EditorStyles.boldLabel);
            int err = resultados.Count(r => r.estado == Estado.Error), av = resultados.Count(r => r.estado == Estado.Aviso);
            EditorGUILayout.HelpBox(err == 0 && av == 0 ? "Todo en orden." :
                $"{err} error(es) y {av} aviso(s). Pulse \"Arreglar automaticamente\" y luego revise lo que quede en rojo.",
                err > 0 ? MessageType.Error : av > 0 ? MessageType.Warning : MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Volver a revisar", GUILayout.Height(26))) Revisar();
                GUI.enabled = err + av > 0;
                if (GUILayout.Button("Arreglar automaticamente", GUILayout.Height(26))) { Arreglar(); Revisar(); }
                GUI.enabled = true;
                if (GUILayout.Button("Abrir escena del juego", GUILayout.Height(26))) AbrirEscena();
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var r in resultados)
            {
                var color = r.estado == Estado.Ok ? new Color(0.25f, 0.65f, 0.3f) : r.estado == Estado.Aviso ? new Color(0.9f, 0.6f, 0.1f) : new Color(0.85f, 0.2f, 0.2f);
                var estilo = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = color } };
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField((r.estado == Estado.Ok ? "OK  " : r.estado == Estado.Aviso ? "AVISO  " : "ERROR  ") + r.tema, estilo);
                EditorGUILayout.LabelField(r.detalle, EditorStyles.wordWrappedLabel);
                if (r.estado != Estado.Ok && !string.IsNullOrEmpty(r.solucion))
                    EditorGUILayout.LabelField("Solucion: " + r.solucion, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.LabelField("Reporte guardado en: " + RutaReporte(), EditorStyles.miniLabel);
        }

        static string RutaReporte() => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Diagnostico_Exergame.txt");

        void Agregar(Estado e, string tema, string detalle, string solucion = "")
            => resultados.Add(new Resultado { estado = e, tema = tema, detalle = detalle, solucion = solucion });

        static Type Tipo(string nombre) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(nombre, false)).FirstOrDefault(t => t != null);

        public void Revisar()
        {
            resultados.Clear();

            // 1. Version de Unity
            if (Application.unityVersion == VersionUnity)
                Agregar(Estado.Ok, "Version de Unity", "Unity " + Application.unityVersion + ".");
            else
                Agregar(Estado.Error, "Version de Unity",
                    $"Este proyecto se hizo con Unity {VersionUnity} y esta abierto con {Application.unityVersion}. Con otra version los paquetes y materiales pueden romperse (todo morado).",
                    $"Instalar Unity {VersionUnity} desde Unity Hub (Instalaciones > Instalar editor > Archivo) con Android Build Support, y abrir el proyecto con esa version.");

            // 2. Paquetes (si faltan, todo se ve morado y los scripts no compilan)
            var paquetes = new (string tipo, string nombre)[]
            {
                ("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset", "Universal Render Pipeline (URP)"),
                ("Unity.XR.CoreUtils.XROrigin", "XR Core Utilities"),
                ("UnityEngine.XR.Interaction.Toolkit.XRBaseInteractable", "XR Interaction Toolkit"),
                ("UnityEngine.XR.OpenXR.OpenXRSettings", "OpenXR"),
                ("TMPro.TextMeshPro", "TextMesh Pro"),
                ("UnityEngine.InputSystem.Mouse", "Input System"),
            };
            var faltan = paquetes.Where(p => Tipo(p.tipo) == null).Select(p => p.nombre).ToList();
            if (faltan.Count == 0)
                Agregar(Estado.Ok, "Paquetes", "URP, XR Interaction Toolkit, OpenXR, TextMesh Pro e Input System estan instalados.");
            else
                Agregar(Estado.Error, "Paquetes", "Faltan: " + string.Join(", ", faltan) + ". Sin URP todo se ve morado; sin XR los scripts del juego no compilan.",
                    "Conectarse a internet y pulsar \"Arreglar automaticamente\" (vuelve a descargar los paquetes). Si sigue: cerrar Unity, borrar la carpeta Library del proyecto y abrirlo de nuevo con internet.");

            // 3. Scripts del juego
            bool juego = Tipo("Exergame.SesionExergame") != null;
            bool editor = Tipo("Exergame.EditorTools.PresentadorMousey") != null;
            if (juego && editor)
                Agregar(Estado.Ok, "Scripts del juego", "Los scripts del juego y los menus de Exergame estan compilados.");
            else
                Agregar(Estado.Error, "Scripts del juego",
                    "Los scripts del juego no estan compilados (por eso Unity \"no reconoce\" el juego y no aparecen los menus de la sala).",
                    "Casi siempre es porque faltan paquetes (punto anterior). Revisar la Consola (Window > General > Console): el primer error en rojo dice cual falta. Si Unity ofrecio \"Safe Mode\", salir de el despues de arreglar los paquetes.");

            // 4. Render pipeline asignado (materiales morados)
            var rp = GraphicsSettings.defaultRenderPipeline;
            bool urpOk = rp != null && rp.GetType().Name.Contains("Universal");
            var niveles = QualitySettings.names;
            var sinRp = new List<string>();
            for (int i = 0; i < niveles.Length; i++)
                if (RpDeNivel(i) == null && rp == null) sinRp.Add(niveles[i]);
            if (urpOk && sinRp.Count == 0)
                Agregar(Estado.Ok, "Render Pipeline (colores)", "URP asignado: " + rp.name + ".");
            else
                Agregar(Estado.Error, "Render Pipeline (colores)",
                    rp == null ? "No hay URP asignado en Project Settings > Graphics: todo se ve morado." : "El pipeline asignado no es URP: " + rp.GetType().Name + ".",
                    "\"Arreglar automaticamente\" asigna " + RutaUrp + " en Graphics y en todos los niveles de calidad.");

            // 5. Materiales morados (shader roto)
            var rotos = MaterialesRotos();
            if (rotos.Count == 0)
                Agregar(Estado.Ok, "Materiales", "Ningun material del juego tiene el shader roto.");
            else
                Agregar(urpOk ? Estado.Aviso : Estado.Error, "Materiales", rotos.Count + " material(es) con shader roto (se ven morados). Ej.: " + string.Join(", ", rotos.Take(5).Select(m => m.name)),
                    "Primero arreglar URP y paquetes. Luego \"Arreglar automaticamente\" les pone el shader URP/Lit.");

            // 6. XR Rig (Starter Assets)
            bool rig = AssetDatabase.FindAssets("\"XR Interaction Setup\" t:Prefab").Length > 0;
            bool controles = AssetDatabase.FindAssets("\"XR Controller Left\" t:Prefab").Length > 0;
            if (rig && controles)
                Agregar(Estado.Ok, "XR Rig (manos y controles)", "Starter Assets de XR Interaction Toolkit presentes.");
            else
                Agregar(Estado.Error, "XR Rig (manos y controles)", "Faltan los Starter Assets (XR Interaction Setup / controles): no habra manos ni controles en VR.",
                    "Window > Package Manager > XR Interaction Toolkit > Samples > Starter Assets > Import.");

            // 7. Escena, personaje, voces
            bool escena = File.Exists(RutaEscena);
            Agregar(escena ? Estado.Ok : Estado.Error, "Escena del juego", escena ? RutaEscena : "No se encontro " + RutaEscena + ".",
                "Copiar de nuevo la carpeta Assets completa desde el comprimido.");
            bool mousey = File.Exists(RutaMousey);
            int texturas = Directory.Exists("Assets/Exergame/Personajes/Mousey/Texturas") ? Directory.GetFiles("Assets/Exergame/Personajes/Mousey/Texturas", "*.png").Length : 0;
            Agregar(mousey && texturas >= 2 ? Estado.Ok : Estado.Aviso, "Presentadora (Mousey)",
                mousey ? $"Modelo presente, {texturas} textura(s)." : "Falta el modelo de Mousey.",
                "Si Mousey se ve blanco o fuera de lugar: menu Exergame > Presentadora: usar Mousey.");
            int voces = Directory.Exists("Assets/Exergame/Audio/Voz") ? Directory.GetFiles("Assets/Exergame/Audio/Voz", "*.wav").Length : 0;
            Agregar(voces >= 11 ? Estado.Ok : Estado.Aviso, "Voces", voces + " audio(s) de voz en Assets/Exergame/Audio/Voz.",
                "Deben estar las 11 voces grabadas (colores e instrucciones).");

            // 8. Android / visor
            bool android = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android);
            Agregar(android ? Estado.Ok : Estado.Aviso, "Modulo Android (para el APK)",
                android ? "Android Build Support instalado." : "No esta instalado Android Build Support: se puede probar en el editor, pero no crear el APK.",
                "Unity Hub > Instalaciones > (engranaje) Anadir modulos > Android Build Support (con OpenJDK y SDK/NDK).");
            bool targetAndroid = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;
            Agregar(targetAndroid ? Estado.Ok : Estado.Aviso, "Plataforma activa",
                "Plataforma: " + EditorUserBuildSettings.activeBuildTarget + ".",
                "Para el visor: File > Build Settings > Android > Switch Platform (o menu Exergame > Configurar para Meta Quest 2).");

            // 9. Disco y rutas largas
            try
            {
                var raiz = Directory.GetParent(Application.dataPath).FullName;
                var drive = new DriveInfo(Path.GetPathRoot(raiz));
                double gb = drive.AvailableFreeSpace / 1e9;
                Agregar(gb >= 8 ? Estado.Ok : Estado.Aviso, "Espacio en disco", $"{gb:0.0} GB libres en {drive.Name}.",
                    "Liberar espacio: Unity necesita varios GB para la carpeta Library y para compilar el APK.");
                // Rapido: usa la lista de assets que Unity ya tiene en memoria (sin recorrer el disco).
                int largo = AssetDatabase.GetAllAssetPaths().Where(a => a.StartsWith("Assets/"))
                    .Select(a => raiz.Length + 1 + a.Length + 5).DefaultIfEmpty(0).Max();
                Agregar(largo < 240 ? Estado.Ok : Estado.Aviso, "Ruta del proyecto",
                    $"Ruta mas larga: {largo} caracteres (proyecto en {raiz}).",
                    "Windows falla con rutas de mas de 260 caracteres: mover el proyecto a una carpeta corta, por ejemplo D:\\Exergame.");
            }
            catch (Exception e) { Agregar(Estado.Aviso, "Disco", "No se pudo revisar: " + e.Message); }

            GuardarReporte();
            Repaint();
        }

        static RenderPipelineAsset RpDeNivel(int i)
        {
            var m = typeof(QualitySettings).GetMethod("GetRenderPipelineAssetAt", new[] { typeof(int) });
            return m != null ? m.Invoke(null, new object[] { i }) as RenderPipelineAsset : null;
        }

        static List<Material> MaterialesRotos()
        {
            var lista = new List<Material>();
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Exergame" }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (m == null) continue;
                if (m.shader == null || m.shader.name == "Hidden/InternalErrorShader" || !m.shader.isSupported) lista.Add(m);
            }
            return lista;
        }

        void Arreglar()
        {
            var log = new StringBuilder();

            // Paquetes: volver a resolver (necesita internet).
            if (Tipo("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset") == null || Tipo("UnityEngine.XR.Interaction.Toolkit.XRBaseInteractable") == null)
            {
                UnityEditor.PackageManager.Client.Resolve();
                log.AppendLine("Se pidio a Unity volver a descargar los paquetes (Package Manager > Resolve).");
            }

            // URP en Graphics y en todos los niveles de calidad.
            var urp = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(RutaUrp);
            if (urp == null)
            {
                var g = AssetDatabase.FindAssets("t:RenderPipelineAsset").FirstOrDefault();
                if (g != null) urp = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(g));
            }
            if (urp != null && urp.GetType().Name.Contains("Universal"))
            {
                GraphicsSettings.defaultRenderPipeline = urp;
                int actual = QualitySettings.GetQualityLevel();
                for (int i = 0; i < QualitySettings.names.Length; i++)
                {
                    if (RpDeNivel(i) != null) continue;
                    QualitySettings.SetQualityLevel(i, false);
                    QualitySettings.renderPipeline = urp;
                }
                QualitySettings.SetQualityLevel(actual, false);
                AssetDatabase.SaveAssets();
                log.AppendLine("URP asignado: " + urp.name);
            }

            // Materiales rotos -> URP/Lit (si URP ya funciona).
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit != null)
            {
                foreach (var m in MaterialesRotos())
                {
                    var c = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
                    m.shader = lit;
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                    EditorUtility.SetDirty(m);
                    log.AppendLine("Material reparado: " + m.name);
                }
                AssetDatabase.SaveAssets();
            }

            // Starter Assets: abrir el Package Manager en XR Interaction Toolkit.
            if (AssetDatabase.FindAssets("\"XR Controller Left\" t:Prefab").Length == 0)
            {
                UnityEditor.PackageManager.UI.Window.Open("com.unity.xr.interaction.toolkit");
                log.AppendLine("Se abrio el Package Manager: importar Samples > Starter Assets.");
            }

            // Plataforma Android (solo si el modulo esta instalado).
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
                BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android) &&
                EditorUtility.DisplayDialog("Plataforma", "Cambiar la plataforma a Android (para el Meta Quest 2)? Puede tardar unos minutos.", "Si", "Ahora no"))
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                log.AppendLine("Plataforma cambiada a Android.");
            }

            Debug.Log("[Exergame] Diagnostico - arreglos:\n" + (log.Length > 0 ? log.ToString() : "Nada que arreglar automaticamente."));
        }

        static void AbrirEscena()
        {
            if (!File.Exists(RutaEscena)) { EditorUtility.DisplayDialog("Escena", "No se encontro " + RutaEscena, "OK"); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(RutaEscena);
        }

        void GuardarReporte()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("DIAGNOSTICO EXERGAME VR - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                sb.AppendLine("Equipo: " + SystemInfo.deviceName + " | " + SystemInfo.operatingSystem + " | GPU: " + SystemInfo.graphicsDeviceName);
                sb.AppendLine("Proyecto: " + Directory.GetParent(Application.dataPath).FullName);
                sb.AppendLine();
                foreach (var r in resultados)
                {
                    sb.AppendLine($"[{(r.estado == Estado.Ok ? "OK" : r.estado == Estado.Aviso ? "AVISO" : "ERROR")}] {r.tema}: {r.detalle}");
                    if (r.estado != Estado.Ok && !string.IsNullOrEmpty(r.solucion)) sb.AppendLine("        Solucion: " + r.solucion);
                }
                File.WriteAllText(RutaReporte(), sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }
    }
}
