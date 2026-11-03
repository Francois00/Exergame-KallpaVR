using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Exergame.EditorTools
{
    /// <summary>
    /// Cambia el presentador de piezas basicas por Mousey (Mixamo), con animaciones:
    /// reposo, hablar, aplaudir (al acertar) y saludar (al empezar).
    /// Se usa solo desde "Exergame/Construir sala" o desde el menu "Exergame/Presentadora: usar Mousey".
    /// </summary>
    public static class PresentadorMousey
    {
        const string Carpeta = "Assets/Exergame/Personajes/Mousey";
        const string RutaModelo = Carpeta + "/Mousey.fbx";
        const string RutaControlador = Carpeta + "/Mousey.controller";

        // Altura total de Mousey (se ajusta si hace falta para que su mano llegue al letrero).
        public const float Altura = 1.15f;
        const float EscalaMaxima = 1.0f;
        // Mousey se para a un costado del tablero (a la derecha del residente) y estira la mano al centro.
        static readonly Vector3 PosicionCostado = new Vector3(0.62f, 0f, 1.40f);
        // Base de la cabeza: la cara queda casi a la altura de los ojos del residente (1.15 m).
        const float AlturaBaseCabeza = 1.08f;
        // Letrero al centro, por encima del borde del tablero (sin tapar el marco ni los circulos).
        static readonly Vector3 CentroLetrero = new Vector3(0.0f, 1.46f, 0.70f);
        const float EscalaLetrero = 1.15f;
        const string CarpetaTexturas = Carpeta + "/Texturas";

        [MenuItem("Exergame/Presentadora: usar Mousey")]
        static void Menu()
        {
            var p = Object.FindObjectOfType<PresentadorColores>(true);
            if (p == null)
            {
                EditorUtility.DisplayDialog("Mousey", "No hay presentador en la escena. Use primero Exergame > Construir sala.", "OK");
                return;
            }
            if (!Aplicar(p))
            {
                EditorUtility.DisplayDialog("Mousey", "No se encontro " + RutaModelo + ".", "OK");
                return;
            }
            EditorSceneManager.MarkSceneDirty(p.gameObject.scene);
            EditorSceneManager.SaveScene(p.gameObject.scene);
            Selection.activeGameObject = p.gameObject;
            Debug.Log("[Exergame] Presentadora cambiada a Mousey.");
        }

        public static bool Aplicar(PresentadorColores p)
        {
            PrepararTexturas();
            // Antes de crear a Mousey: reimportar texturas despues reconstruiria el modelo y perderia su posicion.
            ReducirTexturas();
            var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModelo);
            if (modelo == null) return false;
            var controlador = Controlador();
            if (controlador == null) return false;

            var raiz = p.transform;
            var previa = raiz.Find("Mousey");
            if (previa != null) Object.DestroyImmediate(previa.gameObject);
            var tarimaPrevia = raiz.Find("Tarima");
            if (tarimaPrevia != null) Object.DestroyImmediate(tarimaPrevia.gameObject);

            // El circulo queda fijo en su sitio; el cuerpo de piezas se oculta.
            if (p.circulo != null && p.circulo.parent != raiz) p.circulo.SetParent(raiz, true);
            if (p.cuerpo != null) p.cuerpo.gameObject.SetActive(false);
            p.cuerpo = null;
            p.boca = null;
            p.ojos = null;
            p.hombroCirculo = null;

            var go = (GameObject)PrefabUtility.InstantiatePrefab(modelo, raiz);
            go.name = "Mousey";
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var hombro = Hueso(go.transform, "RightArm");
            var codo = Hueso(go.transform, "RightForeArm");
            var mano = Hueso(go.transform, "RightHand");

            var cabeza = Hueso(go.transform, "Head");

            // Mousey a un costado, de pie, mirando al residente.
            var limites = Limites(go);
            if (limites.size.y > 0.01f) go.transform.localScale = Vector3.one * (Altura / limites.size.y);
            float escalaBase = go.transform.localScale.x;
            var haciaResidente = new Vector3(-PosicionCostado.x, 0f, -PosicionCostado.z).normalized;
            go.transform.rotation = Quaternion.LookRotation(haciaResidente, Vector3.up);
            go.transform.position = new Vector3(PosicionCostado.x, raiz.position.y, PosicionCostado.z);
            limites = Limites(go);
            go.transform.position += Vector3.up * (raiz.position.y - limites.min.y);
            var adelante = haciaResidente;

            // Borde del letrero por donde lo toma la mano (el lado que da a Mousey).
            float radio = 0.15f * EscalaLetrero;
            var haciaMousey = go.transform.position - CentroLetrero;
            haciaMousey.y = 0f;
            haciaMousey.Normalize();
            var borde = CentroLetrero + haciaMousey * (radio * 0.92f) - Vector3.up * 0.04f + adelante * 0.02f;

            // Escala justa para que la mano llegue (sin pasar de EscalaMaxima; si no llega, queda senalando).
            float alturaTarima = 0f;
            if (hombro != null && codo != null && mano != null && cabeza != null)
            {
                var pie = go.transform.position;
                var desdePie = hombro.position - pie;
                float baseCabeza = cabeza.position.y - pie.y;
                float brazo0 = Vector3.Distance(hombro.position, codo.position) + Vector3.Distance(codo.position, mano.position);
                float mejor = EscalaMaxima;
                for (float e = 1f; e <= EscalaMaxima + 0.001f; e += 0.02f)
                {
                    float tar = Mathf.Max(0f, AlturaBaseCabeza - e * baseCabeza);
                    var h = pie + desdePie * e + Vector3.up * tar;
                    if (Vector3.Distance(h, borde) <= 0.9f * e * brazo0) { mejor = e; break; }
                }
                go.transform.localScale = Vector3.one * (escalaBase * mejor);
                limites = Limites(go);
                go.transform.position += Vector3.up * (raiz.position.y - limites.min.y);
                alturaTarima = Mathf.Max(0f, AlturaBaseCabeza - (cabeza.position.y - raiz.position.y));
                go.transform.position += Vector3.up * alturaTarima;
            }

            var animador = go.GetComponent<Animator>();
            if (animador == null) animador = go.AddComponent<Animator>();
            if (animador.avatar == null)
                animador.avatar = AssetDatabase.LoadAllAssetsAtPath(RutaModelo).OfType<Avatar>().FirstOrDefault();
            animador.runtimeAnimatorController = controlador;
            animador.applyRootMotion = false;
            animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // Rendimiento en el visor: sin sombra propia y texturas a 1024 (se ven igual a esta distancia).
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.updateWhenOffscreen = true;
                smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                smr.quality = SkinQuality.Bone2;
            }

            Transform objetivo = null;
            if (p.circulo != null)
            {
                objetivo = p.circulo.Find("ObjetivoMano");
                if (objetivo == null)
                {
                    objetivo = new GameObject("ObjetivoMano").transform;
                    objetivo.SetParent(p.circulo, false);
                }
                objetivo.localRotation = Quaternion.identity;
            }

            var ik = go.AddComponent<IKManoPresentador>();
            ik.objetivo = objetivo;

            // Letrero al centro; la mano lo sostiene por el borde (o lo senala si no alcanza).
            if (p.circulo != null && objetivo != null)
            {
                p.circulo.localScale = Vector3.one * EscalaLetrero;
                p.circulo.position = CentroLetrero;
                var cam = new Vector3(0f, 1.15f, 0f);
                p.circulo.rotation = Quaternion.LookRotation(cam - CentroLetrero, Vector3.up);
                objetivo.position = borde;
            }

            p.animador = animador;
            p.ikMano = ik;

            MejorarLetrero(p);
            ColocarGlobo(p, raiz);
            if (cabeza != null) ColocarBoca(p, go, cabeza, adelante);

            foreach (var sesion in Object.FindObjectsOfType<SesionExergame>(true))
            {
                sesion.audioPresentacion = Voz("presentacion");
                sesion.audioPracticaInicio = Voz("practica_inicio");
                sesion.audioPracticaIzquierda = Voz("practica_izquierda");
                sesion.audioPracticaDerecha = Voz("practica_derecha");
                sesion.audioPracticaFin = Voz("practica_fin");
                sesion.audioPracticaOtraMano = Voz("practica_otra_mano");
                sesion.textoPresentacion = "¡Hola! Yo le voy a ayudar en el juego.\nEscuche bien y siga mis indicaciones.";
                EditorUtility.SetDirty(sesion);
            }

            if (alturaTarima > 0.01f)
            {
                var tarima = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tarima.name = "Tarima";
                Object.DestroyImmediate(tarima.GetComponent<Collider>());
                tarima.transform.SetParent(raiz, false);
                var pie = raiz.InverseTransformPoint(go.transform.position);
                tarima.transform.localPosition = new Vector3(pie.x, alturaTarima * 0.5f, pie.z);
                tarima.transform.localScale = new Vector3(0.7f, alturaTarima, 0.6f);
                var madera = AssetDatabase.LoadAssetAtPath<Material>("Assets/Exergame/Materiales/MaderaOscura.mat");
                if (madera != null) tarima.GetComponent<Renderer>().sharedMaterial = madera;
            }

            var info = new System.Text.StringBuilder();
            info.Append($"[Exergame] Tarima {alturaTarima:0.00} m, letrero en {(p.circulo != null ? p.circulo.position.ToString() : "-")}. Mousey escala {go.transform.localScale.x:0.0000}, pos {go.transform.position}, alto {Limites(go).size.y:0.00} m. Materiales: ");
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                foreach (var mat in r.sharedMaterials)
                    if (mat != null)
                        info.Append($"{mat.name} [{mat.shader.name}] tex={(mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null ? mat.GetTexture("_BaseMap").name : (mat.mainTexture != null ? mat.mainTexture.name : "ninguna"))}; ");
            Debug.Log(info.ToString());
            EditorUtility.SetDirty(p);
            return true;
        }

        // Las texturas vienen dentro del FBX; se sacan a una carpeta para que los materiales las encuentren.
        static void PrepararTexturas()
        {
            if (AssetDatabase.IsValidFolder(CarpetaTexturas) &&
                AssetDatabase.FindAssets("t:Texture2D", new[] { CarpetaTexturas }).Length > 0) return;
            var imp = AssetImporter.GetAtPath(RutaModelo) as ModelImporter;
            if (imp == null) return;
            if (!AssetDatabase.IsValidFolder(CarpetaTexturas)) AssetDatabase.CreateFolder(Carpeta, "Texturas");
            bool ok = imp.ExtractTextures(CarpetaTexturas);
            AssetDatabase.Refresh();
            imp.materialSearch = ModelImporterMaterialSearch.Local;
            imp.SaveAndReimport();
            Debug.Log("[Exergame] Texturas de Mousey extraidas: " + ok);
        }

        // Letrero bien visible para adultos mayores: color puro (sin sombras) y un borde blanco
        // entre el color y el marco de madera, para que el color resalte sobre cualquier fondo.
        static void MejorarLetrero(PresentadorColores p)
        {
            if (p.colorCirculo == null) return;
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit == null) return;

            p.colorCirculo.sharedMaterial = MaterialPlano("LetreroColor", Color.white, unlit);
            var t = p.colorCirculo.transform;
            t.localScale = new Vector3(0.262f, t.localScale.y, 0.262f);

            var disco = t.parent;
            var borde = disco.Find("BordeBlanco");
            if (borde == null)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.DestroyImmediate(b.GetComponent<Collider>());
                b.name = "BordeBlanco";
                b.transform.SetParent(disco, false);
                borde = b.transform;
            }
            borde.localPosition = Vector3.zero;
            borde.localRotation = Quaternion.identity;
            borde.localScale = new Vector3(0.288f, 0.010f, 0.288f);
            var r = borde.GetComponent<Renderer>();
            r.sharedMaterial = MaterialPlano("LetreroBorde", Color.white, unlit);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static Material MaterialPlano(string nombre, Color color, Shader shader)
        {
            string ruta = Carpeta + "/" + nombre + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.shader = shader;
            m.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(m);
            return m;
        }

        // Globo de dialogo al centro, a la altura de la vista (durante la presentacion no hay tablero).
        static void ColocarGlobo(PresentadorColores p, Transform raiz)
        {
            var punto = raiz.Find("PuntoGlobo");
            if (punto == null)
            {
                punto = new GameObject("PuntoGlobo").transform;
                punto.SetParent(raiz, false);
            }
            punto.position = new Vector3(-0.12f, raiz.position.y + 1.31f, 0.78f);
            punto.rotation = raiz.rotation;
            punto.localScale = Vector3.one * 0.65f;
            p.puntoGlobo = punto;
        }

        // Boca que se abre con la voz: se busca la punta de la nariz en la malla y la boca va justo debajo.
        static void ColocarBoca(PresentadorColores p, GameObject go, Transform cabeza, Vector3 adelante)
        {
            var tope = Hueso(go.transform, "HeadTop_End");
            float altoCabeza = tope != null ? Vector3.Distance(tope.position, cabeza.position) : 0.3f;

            Vector3? nariz = null;
            var puntos = new System.Collections.Generic.List<Vector3>();
            var normales = new System.Collections.Generic.List<Vector3>();
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var malla = new Mesh();
                smr.BakeMesh(malla);
                var vertices = malla.vertices;
                var ns = malla.normals;
                // Segun la version, BakeMesh ya trae la escala o no: se elige la conversion
                // cuyos limites coinciden con los del renderer.
                var conEscala = smr.transform.localToWorldMatrix;
                var sinEscala = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
                var m = ErrorLimites(vertices, conEscala, smr.bounds) <= ErrorLimites(vertices, sinEscala, smr.bounds) ? conEscala : sinEscala;
                for (int iv = 0; iv < vertices.Length; iv++)
                {
                    var w = m.MultiplyPoint3x4(vertices[iv]);
                    if (w.y < cabeza.position.y - 0.05f * altoCabeza || w.y > cabeza.position.y + altoCabeza) continue;
                    var lateral = w - cabeza.position;
                    lateral.y = 0f;
                    lateral -= Vector3.Project(lateral, adelante);
                    if (lateral.magnitude > 0.18f * altoCabeza) continue;
                    puntos.Add(w);
                    normales.Add(ns.Length > iv ? m.MultiplyVector(ns[iv]).normalized : adelante);
                    if (nariz == null || Vector3.Dot(w - nariz.Value, adelante) > 0f) nariz = w;
                }
                Object.DestroyImmediate(malla);
            }
            if (nariz == null) return;

            // Superficie de la cara justo debajo de la nariz.
            // La linea de la sonrisa (dibujada en la cara) esta un poco debajo de la nariz:
            // la boca se abre desde ahi hacia abajo, como una mandibula.
            float yBoca = nariz.Value.y - 0.075f * altoCabeza;
            Vector3? superficie = null;
            var normal = adelante;
            for (int ip = 0; ip < puntos.Count; ip++)
            {
                var w = puntos[ip];
                if (Mathf.Abs(w.y - yBoca) > 0.03f * altoCabeza) continue;
                var lateral = w - nariz.Value;
                lateral.y = 0f;
                lateral -= Vector3.Project(lateral, adelante);
                if (lateral.magnitude > 0.05f * altoCabeza) continue;
                if (superficie == null || Vector3.Dot(w - superficie.Value, adelante) > 0f) { superficie = w; normal = normales[ip]; }
            }
            // La boca se apoya sobre la cara siguiendo su curva (normal de la superficie), un poquito afuera.
            if (Vector3.Dot(normal, adelante) < 0.3f) normal = adelante;
            normal = Vector3.Slerp(normal, adelante, 0.35f).normalized;
            var centro = (superficie ?? (nariz.Value - Vector3.up * 0.075f * altoCabeza)) + normal * 0.008f;

            var viejo = cabeza.Find("BocaHablar");
            if (viejo != null) Object.DestroyImmediate(viejo.gameObject);

            // Pivote en la sonrisa; el ovalo cuelga hacia abajo y crece hacia abajo al hablar.
            var pivote = new GameObject("BocaHablar").transform;
            pivote.SetParent(cabeza, true);
            pivote.position = centro;
            pivote.rotation = Quaternion.LookRotation(normal, Vector3.up);
            var lossy = cabeza.lossyScale;
            pivote.localScale = new Vector3(1f / lossy.x, 1f / lossy.y, 1f / lossy.z);

            float anchoBoca = 0.15f * altoCabeza, altoBoca = 0.085f * altoCabeza;
            var boca = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(boca.GetComponent<Collider>());
            boca.name = "Ovalo";
            boca.transform.SetParent(pivote, false);
            boca.transform.localPosition = new Vector3(0f, -altoBoca * 0.5f, 0f);
            boca.transform.localRotation = Quaternion.identity;
            boca.transform.localScale = new Vector3(anchoBoca, altoBoca, 0.02f * altoCabeza);
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            var r = boca.GetComponent<Renderer>();
            if (unlit != null) r.sharedMaterial = MaterialPlano("BocaInterior", new Color(0.32f, 0.07f, 0.08f), unlit);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.enabled = false;

            var b = go.GetComponent<BocaPresentador>();
            if (b == null) b = go.AddComponent<BocaPresentador>();
            b.voz = p.voz;
            b.boca = pivote;
            b.cabeza = cabeza;
            Debug.Log($"[Exergame] Boca de Mousey: nariz {nariz.Value}, boca {centro}, alto cabeza {altoCabeza:0.000}");
        }

        static void ReducirTexturas()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { CarpetaTexturas }))
            {
                var ruta = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(ruta) is TextureImporter ti) || ti.maxTextureSize <= 1024) continue;
                ti.maxTextureSize = 1024;
                ti.SaveAndReimport();
            }
        }

        static AudioClip Voz(string nombre) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Exergame/Audio/Voz/" + nombre + ".wav");

        static float ErrorLimites(Vector3[] vertices, Matrix4x4 m, Bounds esperado)
        {
            if (vertices.Length == 0) return float.MaxValue;
            var b = new Bounds(m.MultiplyPoint3x4(vertices[0]), Vector3.zero);
            for (int i = 1; i < vertices.Length; i += 7) b.Encapsulate(m.MultiplyPoint3x4(vertices[i]));
            return (b.min - esperado.min).magnitude + (b.max - esperado.max).magnitude;
        }

        static Transform Hueso(Transform raiz, string nombre)
        {
            return raiz.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == nombre || t.name.EndsWith(":" + nombre));
        }

        static Bounds Limites(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds);
            return b;
        }

        static AnimationClip Clip(string nombre)
        {
            return AssetDatabase.LoadAllAssetsAtPath(Carpeta + "/Mousey@" + nombre + ".fbx")
                .OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        }

        static AnimatorController Controlador()
        {
            var existente = AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador);
            if (existente != null) return existente;

            var reposo = Clip("Reposo");
            if (reposo == null)
            {
                Debug.LogWarning("[Exergame] Falta la animacion Mousey@Reposo.fbx");
                return null;
            }
            var hablar = Clip("Hablando");
            var aplaudir = Clip("Aplaudir");
            var saludar = Clip("Saludar");

            var c = AnimatorController.CreateAnimatorControllerAtPath(RutaControlador);
            c.AddParameter("Hablando", AnimatorControllerParameterType.Bool);
            c.AddParameter("Aplaudir", AnimatorControllerParameterType.Trigger);
            c.AddParameter("Saludar", AnimatorControllerParameterType.Trigger);

            var sm = c.layers[0].stateMachine;
            var eReposo = sm.AddState("Reposo");
            eReposo.motion = reposo;
            sm.defaultState = eReposo;

            if (hablar != null)
            {
                var eHablar = sm.AddState("Hablando");
                eHablar.motion = hablar;
                var t1 = eReposo.AddTransition(eHablar);
                t1.hasExitTime = false;
                t1.duration = 0.3f;
                t1.AddCondition(AnimatorConditionMode.If, 0f, "Hablando");
                var t2 = eHablar.AddTransition(eReposo);
                t2.hasExitTime = false;
                t2.duration = 0.4f;
                t2.AddCondition(AnimatorConditionMode.IfNot, 0f, "Hablando");
            }

            Gesto(sm, eReposo, "Aplaudir", aplaudir);
            Gesto(sm, eReposo, "Saludar", saludar);

            var capas = c.layers;
            capas[0].iKPass = true;
            c.layers = capas;

            EditorUtility.SetDirty(c);
            AssetDatabase.SaveAssets();
            return c;
        }

        static void Gesto(AnimatorStateMachine sm, AnimatorState eReposo, string nombre, AnimationClip clip)
        {
            if (clip == null) return;
            var e = sm.AddState(nombre);
            e.motion = clip;
            var entrar = sm.AddAnyStateTransition(e);
            entrar.hasExitTime = false;
            entrar.duration = 0.15f;
            entrar.canTransitionToSelf = false;
            entrar.AddCondition(AnimatorConditionMode.If, 0f, nombre);
            var salir = e.AddTransition(eReposo);
            salir.hasExitTime = true;
            salir.exitTime = 0.9f;
            salir.duration = 0.3f;
        }
    }

    /// <summary>Configura al importar los FBX de Mousey: Humanoid, y reposo/hablar en bucle.</summary>
    public class ImportadorMousey : AssetPostprocessor
    {
        bool EsMousey => assetPath.Replace('\\', '/').Contains("/Personajes/Mousey/");
        bool EsAnimacion => assetPath.Contains("@");

        void OnPreprocessModel()
        {
            if (!EsMousey) return;
            var m = (ModelImporter)assetImporter;
            m.animationType = ModelImporterAnimationType.Human;
            m.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            m.importCameras = false;
            m.importLights = false;
            m.importAnimation = EsAnimacion;
            if (EsAnimacion) m.materialImportMode = ModelImporterMaterialImportMode.None;
        }

        void OnPreprocessAnimation()
        {
            if (!EsMousey || !EsAnimacion) return;
            var m = (ModelImporter)assetImporter;
            bool bucle = assetPath.Contains("Reposo") || assetPath.Contains("Hablando");
            var clips = m.defaultClipAnimations;
            foreach (var c in clips)
            {
                c.loopTime = bucle;
                c.lockRootRotation = true;
                c.lockRootHeightY = true;
                c.lockRootPositionXZ = true;
                c.keepOriginalOrientation = true;
                c.keepOriginalPositionY = true;
                c.keepOriginalPositionXZ = true;
            }
            m.clipAnimations = clips;
        }
    }
}
