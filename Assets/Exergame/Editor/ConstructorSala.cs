using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using Object = UnityEngine.Object;

namespace Exergame.EditorTools
{
    public static class ConstructorSala
    {
        const string CarpetaRaiz = "Assets/Exergame";
        const string CarpetaEscenas = CarpetaRaiz + "/Scenes";
        const string CarpetaMateriales = CarpetaRaiz + "/Materiales";
        const string CarpetaTexturas = CarpetaRaiz + "/Texturas";
        const string RutaEscena = CarpetaEscenas + "/SalaExergame.unity";
        const string RutaLogo = CarpetaTexturas + "/logo_ucsm.png";

        static readonly string[] RutasRig =
        {
            "Assets/Samples/XR Interaction Toolkit/2.5.2/Starter Assets/Prefabs/XR Interaction Setup.prefab",
            "Assets/VRTemplateAssets/Prefabs/Setup/Complete XR Origin Set Up Variant.prefab"
        };

        static readonly string[] ProveedoresLocomocion =
        {
            "TeleportationProvider", "ContinuousMoveProvider", "DynamicMoveProvider", "ContinuousTurnProvider",
            "SnapTurnProvider", "GrabMoveProvider", "TwoHandedGrabMoveProvider", "ClimbProvider"
        };

        [MenuItem("Exergame/Construir sala")]
        public static void Construir()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaEscena) != null &&
                !EditorUtility.DisplayDialog("Construir sala",
                    "La escena SalaExergame ya existe. Se va a reconstruir desde cero. Continuar?", "Si", "Cancelar"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            CrearCarpetas();
            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            ConfigurarAmbiente();
            var m = CrearMateriales();

            ConstruirSala(m);
            ConstruirDecoracion(m);
            ConstruirMesaPrincipal(m);
            var botones = ConstruirPanelBotones(m);
            var presentador = ConstruirPresentador(m);
            ConstruirSombrasFalsas(m);
            ConstruirIluminacion();
            ColocarRig(m.mano);
            ConstruirTarea(botones, presentador);

            EditorSceneManager.SaveScene(escena, RutaEscena);
            AgregarABuild();
            AssetDatabase.SaveAssets();
            Debug.Log("[Exergame] Sala construida en " + RutaEscena);
        }

        static void CrearCarpetas()
        {
            CrearCarpeta("Assets", "Exergame");
            CrearCarpeta(CarpetaRaiz, "Scenes");
            CrearCarpeta(CarpetaRaiz, "Materiales");
            CrearCarpeta(CarpetaRaiz, "Texturas");
        }

        static void CrearCarpeta(string padre, string nombre)
        {
            if (!AssetDatabase.IsValidFolder(padre + "/" + nombre)) AssetDatabase.CreateFolder(padre, nombre);
        }

        static void ConfigurarAmbiente()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.62f, 0.60f);
            RenderSettings.ambientEquatorColor = new Color(0.52f, 0.50f, 0.46f);
            RenderSettings.ambientGroundColor = new Color(0.32f, 0.26f, 0.20f);
            RenderSettings.fog = false;
        }

        class Materiales
        {
            public Material pared, piso, techo, zocalo, marco, puerta, manija, vidrio, lampara;
            public Material maderaClara, maderaOscura, mesa, metal, caja1, caja2, placa;
            public Material amarillo, azul, naranja, verde, aro;
            public Material mano, piel, camisa, pantalon, pelo, zapato, ojo, boca, cartelBorde, cartelColor;
        }

        static Materiales CrearMateriales()
        {
            return new Materiales
            {
                pared = Mat("Pared", new Color(0.90f, 0.88f, 0.83f), 0.1f),
                piso = Mat("Piso", new Color(0.42f, 0.29f, 0.18f), 0.35f),
                techo = Mat("Techo", new Color(0.85f, 0.83f, 0.78f), 0.05f),
                zocalo = Mat("Zocalo", new Color(0.33f, 0.23f, 0.15f), 0.3f),
                marco = Mat("Marco", new Color(0.23f, 0.20f, 0.17f), 0.3f),
                puerta = Mat("Puerta", new Color(0.95f, 0.94f, 0.91f), 0.3f),
                manija = Mat("Manija", new Color(0.35f, 0.58f, 0.72f), 0.6f),
                vidrio = Mat("Vidrio", new Color(0.43f, 0.63f, 0.76f), 0.9f, new Color(0.18f, 0.28f, 0.36f)),
                lampara = Mat("Lampara", new Color(0.92f, 0.92f, 0.90f), 0.2f, new Color(1.2f, 1.15f, 1.05f)),
                maderaClara = Mat("MaderaClara", new Color(0.86f, 0.75f, 0.56f), 0.25f),
                maderaOscura = Mat("MaderaOscura", new Color(0.66f, 0.53f, 0.36f), 0.25f),
                mesa = Mat("Mesa", new Color(0.73f, 0.50f, 0.26f), 0.3f),
                metal = Mat("Metal", new Color(0.62f, 0.62f, 0.62f), 0.6f, null, 0.8f),
                caja1 = Mat("Caja1", new Color(0.79f, 0.71f, 0.55f), 0.15f),
                caja2 = Mat("Caja2", new Color(0.86f, 0.80f, 0.64f), 0.15f),
                placa = Mat("Placa", Color.white, 0.2f),
                amarillo = Mat("BotonAmarillo", new Color(0.91f, 0.78f, 0.22f), 0.55f, Color.black),
                azul = Mat("BotonAzul", new Color(0.36f, 0.37f, 0.80f), 0.55f, Color.black),
                naranja = Mat("BotonNaranja", new Color(0.91f, 0.43f, 0.22f), 0.55f, Color.black),
                verde = Mat("BotonVerde", new Color(0.37f, 0.71f, 0.35f), 0.55f, Color.black),
                aro = Mat("AroBoton", new Color(0.55f, 0.43f, 0.28f), 0.3f),
                mano = Mat("Mano", new Color(0.93f, 0.80f, 0.70f), 0.35f),
                piel = Mat("Piel", new Color(0.87f, 0.70f, 0.56f), 0.3f),
                camisa = Mat("Camisa", new Color(0.24f, 0.52f, 0.60f), 0.2f),
                pantalon = Mat("Pantalon", new Color(0.25f, 0.27f, 0.35f), 0.2f),
                pelo = Mat("Pelo", new Color(0.20f, 0.15f, 0.12f), 0.3f),
                zapato = Mat("Zapato", new Color(0.15f, 0.13f, 0.12f), 0.4f),
                ojo = Mat("Ojo", new Color(0.08f, 0.08f, 0.08f), 0.8f),
                boca = Mat("Boca", new Color(0.45f, 0.15f, 0.15f), 0.3f),
                cartelBorde = Mat("CartelBorde", new Color(0.97f, 0.97f, 0.95f), 0.2f),
                cartelColor = Mat("CartelColor", new Color(0.6f, 0.6f, 0.6f), 0.2f)
            };
        }

        static Shader ShaderLit()
        {
            var s = Shader.Find("Universal Render Pipeline/Lit");
            return s != null ? s : Shader.Find("Standard");
        }

        static Material Mat(string nombre, Color color, float suavidad, Color? emision = null, float metalico = 0f)
        {
            string ruta = CarpetaMateriales + "/" + nombre + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (mat == null)
            {
                mat = new Material(ShaderLit());
                AssetDatabase.CreateAsset(mat, ruta);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            mat.SetFloat("_Smoothness", suavidad);
            mat.SetFloat("_Metallic", metalico);
            if (emision.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emision.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static GameObject Grupo(string nombre, Transform padre = null)
        {
            var go = new GameObject(nombre);
            if (padre != null) go.transform.SetParent(padre, false);
            return go;
        }

        static GameObject Pieza(string nombre, Transform padre, Vector3 pos, Vector3 escala, Material mat,
            PrimitiveType tipo = PrimitiveType.Cube, bool conCollider = true, bool proyectaSombra = true, bool estatico = true)
        {
            var go = GameObject.CreatePrimitive(tipo);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.localPosition = pos;
            go.transform.localScale = escala;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            if (!proyectaSombra) r.shadowCastingMode = ShadowCastingMode.Off;
            if (!conCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            if (estatico) GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            return go;
        }

        static void ConstruirSala(Materiales m)
        {
            var sala = Grupo("Sala").transform;
            Pieza("Piso", sala, new Vector3(0f, -0.05f, 1f), new Vector3(6f, 0.1f, 6f), m.piso, proyectaSombra: false);
            Pieza("Techo", sala, new Vector3(0f, 3.05f, 1f), new Vector3(6f, 0.1f, 6f), m.techo, proyectaSombra: false);
            Pieza("ParedFrente", sala, new Vector3(0f, 1.5f, 4.05f), new Vector3(6f, 3f, 0.1f), m.pared, proyectaSombra: false);
            Pieza("ParedAtras", sala, new Vector3(0f, 1.5f, -2.05f), new Vector3(6f, 3f, 0.1f), m.pared, proyectaSombra: false);
            Pieza("ParedIzquierda", sala, new Vector3(-3.05f, 1.5f, 1f), new Vector3(0.1f, 3f, 6f), m.pared, proyectaSombra: false);
            Pieza("ParedDerecha", sala, new Vector3(3.05f, 1.5f, 1f), new Vector3(0.1f, 3f, 6f), m.pared, proyectaSombra: false);

            Pieza("FranjaTecho", sala, new Vector3(0f, 2.85f, 3.99f), new Vector3(6f, 0.3f, 0.02f), m.techo, conCollider: false, proyectaSombra: false);
            Pieza("ZocaloFrente", sala, new Vector3(0f, 0.06f, 3.99f), new Vector3(6f, 0.12f, 0.02f), m.zocalo, conCollider: false, proyectaSombra: false);
            Pieza("ZocaloAtras", sala, new Vector3(0f, 0.06f, -1.99f), new Vector3(6f, 0.12f, 0.02f), m.zocalo, conCollider: false, proyectaSombra: false);
            Pieza("ZocaloIzquierda", sala, new Vector3(-2.99f, 0.06f, 1f), new Vector3(0.02f, 0.12f, 6f), m.zocalo, conCollider: false, proyectaSombra: false);
            Pieza("ZocaloDerecha", sala, new Vector3(2.99f, 0.06f, 1f), new Vector3(0.02f, 0.12f, 6f), m.zocalo, conCollider: false, proyectaSombra: false);

            var lamparas = Grupo("Lamparas", sala).transform;
            float[] xs = { -1.8f, 0f, 1.8f };
            foreach (var x in xs)
                Pieza("Lampara", lamparas, new Vector3(x, 2.98f, 1.6f), new Vector3(1.2f, 0.03f, 0.12f), m.lampara, conCollider: false, proyectaSombra: false);
        }

        static void ConstruirDecoracion(Materiales m)
        {
            var deco = Grupo("Decoracion").transform;

            var puerta = Grupo("Puerta", deco).transform;
            Pieza("Marco", puerta, new Vector3(-1.9f, 1.07f, 3.99f), new Vector3(1.0f, 2.14f, 0.02f), m.marco, conCollider: false);
            Pieza("Hoja", puerta, new Vector3(-1.9f, 1.05f, 3.975f), new Vector3(0.84f, 2.0f, 0.02f), m.puerta, conCollider: false);
            Pieza("Manija", puerta, new Vector3(-1.6f, 1.05f, 3.955f), new Vector3(0.035f, 0.26f, 0.03f), m.manija, conCollider: false);

            var ventana = Grupo("Ventana", deco).transform;
            Pieza("Marco", ventana, new Vector3(1.6f, 1.9f, 3.99f), new Vector3(1.5f, 1.15f, 0.02f), m.marco, conCollider: false);
            Pieza("VidrioIzq", ventana, new Vector3(1.255f, 1.9f, 3.975f), new Vector3(0.62f, 0.97f, 0.02f), m.vidrio, conCollider: false, proyectaSombra: false);
            Pieza("VidrioDer", ventana, new Vector3(1.945f, 1.9f, 3.975f), new Vector3(0.62f, 0.97f, 0.02f), m.vidrio, conCollider: false, proyectaSombra: false);

            ConstruirLogo(deco);

            var mesita = Grupo("MesaAuxiliar", deco).transform;
            Pieza("Tablero", mesita, new Vector3(-1.45f, 0.68f, 3.45f), new Vector3(0.95f, 0.04f, 0.5f), m.mesa);
            foreach (var p in new[] { new Vector2(-1.85f, 3.25f), new Vector2(-1.05f, 3.25f), new Vector2(-1.85f, 3.65f), new Vector2(-1.05f, 3.65f) })
                Pieza("Pata", mesita, new Vector3(p.x, 0.33f, p.y), new Vector3(0.04f, 0.66f, 0.04f), m.metal);
            Pieza("Caja", mesita, new Vector3(-1.6f, 0.82f, 3.45f), new Vector3(0.26f, 0.24f, 0.22f), m.caja1);
            Pieza("CajaChica", mesita, new Vector3(-1.3f, 0.77f, 3.45f), new Vector3(0.2f, 0.14f, 0.18f), m.caja2);
        }

        static void ConstruirLogo(Transform deco)
        {
            var logo = Grupo("Logo", deco).transform;
            Pieza("Placa", logo, new Vector3(-1.9f, 2.55f, 3.985f), new Vector3(1.1f, 0.4f, 0.02f), CrearMaterialPlaca(), conCollider: false);

            var textura = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaLogo);
            if (textura != null)
            {
                var shaderUnlit = Shader.Find("Universal Render Pipeline/Unlit");
                string ruta = CarpetaMateriales + "/LogoUCSM.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                if (mat == null)
                {
                    mat = new Material(shaderUnlit != null ? shaderUnlit : Shader.Find("Unlit/Texture"));
                    AssetDatabase.CreateAsset(mat, ruta);
                }
                mat.SetTexture("_BaseMap", textura);
                mat.SetTexture("_MainTex", textura);
                EditorUtility.SetDirty(mat);
                float alto = 0.34f;
                float ancho = Mathf.Min(1.04f, alto * textura.width / Mathf.Max(1f, textura.height));
                Pieza("Imagen", logo, new Vector3(-1.9f, 2.55f, 3.972f), new Vector3(ancho, alto, 1f), mat, PrimitiveType.Quad, conCollider: false, proyectaSombra: false);
                return;
            }

            var texto = new GameObject("TextoLogo");
            texto.transform.SetParent(logo, false);
            texto.transform.localPosition = new Vector3(-1.9f, 2.55f, 3.97f);
            var tmp = texto.AddComponent<TextMeshPro>();
            tmp.rectTransform.sizeDelta = new Vector2(1.0f, 0.34f);
            tmp.text = "Universidad Catolica\nde Santa Maria";
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0.2f;
            tmp.fontSizeMax = 10f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.08f, 0.50f, 0.25f);
            tmp.fontStyle = FontStyles.Bold;
        }

        static Material CrearMaterialPlaca() => AssetDatabase.LoadAssetAtPath<Material>(CarpetaMateriales + "/Placa.mat");

        static void ConstruirMesaPrincipal(Materiales m)
        {
            var mesa = Grupo("MesaPrincipal").transform;
            Pieza("Tablero", mesa, new Vector3(0f, 0.70f, 0.5f), new Vector3(1.2f, 0.04f, 0.7f), m.mesa);
            foreach (var p in new[] { new Vector2(-0.55f, 0.2f), new Vector2(0.55f, 0.2f), new Vector2(-0.55f, 0.8f), new Vector2(0.55f, 0.8f) })
                Pieza("Pata", mesa, new Vector3(p.x, 0.34f, p.y), new Vector3(0.04f, 0.68f, 0.04f), m.metal);
        }

        // Cuanto se aleja el tablero de la persona (metros). Mas alto = hay que estirar mas los brazos.
        const float AdelantoTablero = 0.12f;

        static BotonExergame[] ConstruirPanelBotones(Materiales m)
        {
            var raiz = Grupo("TableroFrontal").transform;
            Pieza("Base", raiz, new Vector3(0f, 0.735f, 0.52f + AdelantoTablero), new Vector3(0.6f, 0.03f, 0.16f), m.maderaOscura, estatico: false);

            var panel = Grupo("PanelBotones", raiz).transform;
            panel.localPosition = new Vector3(0f, 1.0f, 0.55f + AdelantoTablero);
            panel.localRotation = Quaternion.Euler(-70f, 0f, 0f);

            Pieza("Respaldo", panel, new Vector3(0f, -0.04f, -0.02f), new Vector3(0.3f, 0.05f, 0.4f), m.maderaOscura, conCollider: false, estatico: false);
            Pieza("Marco", panel, Vector3.zero, new Vector3(0.54f, 0.03f, 0.54f), m.maderaOscura, estatico: false);
            Pieza("Fondo", panel, new Vector3(0f, 0.002f, 0f), new Vector3(0.5f, 0.03f, 0.5f), m.maderaClara, conCollider: false, estatico: false);
            Pieza("DivisorH", panel, new Vector3(0f, 0.018f, 0f), new Vector3(0.5f, 0.006f, 0.018f), m.maderaOscura, conCollider: false, estatico: false);
            Pieza("DivisorV", panel, new Vector3(0f, 0.018f, 0f), new Vector3(0.018f, 0.006f, 0.5f), m.maderaOscura, conCollider: false, estatico: false);

            var circulo = Mat("Circulo", new Color(0.6f, 0.6f, 0.6f), 0.55f);
            return new[]
            {
                CrearBoton(panel, Posicion.SupIzq, new Vector3(-0.125f, 0f, 0.125f), circulo, m.aro),
                CrearBoton(panel, Posicion.InfIzq, new Vector3(-0.125f, 0f, -0.125f), circulo, m.aro),
                CrearBoton(panel, Posicion.SupDer, new Vector3(0.125f, 0f, 0.125f), circulo, m.aro),
                CrearBoton(panel, Posicion.InfDer, new Vector3(0.125f, 0f, -0.125f), circulo, m.aro)
            };
        }

        static BotonExergame CrearBoton(Transform panel, Posicion posicion, Vector3 pos, Material mat, Material aro)
        {
            var boton = new GameObject("Circulo_" + posicion);
            boton.transform.SetParent(panel, false);
            boton.transform.localPosition = pos + new Vector3(0f, 0.015f, 0f);

            Pieza("Aro", boton.transform, new Vector3(0f, 0.004f, 0f), new Vector3(0.19f, 0.004f, 0.19f), aro, PrimitiveType.Cylinder, conCollider: false, estatico: false);
            var tapa = Pieza("Tapa", boton.transform, new Vector3(0f, 0.016f, 0f), new Vector3(0.16f, 0.01f, 0.16f), mat, PrimitiveType.Cylinder, conCollider: false, estatico: false);

            var col = boton.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.02f, 0f);
            col.size = new Vector3(0.19f, 0.04f, 0.19f);

            var interactable = boton.AddComponent<XRSimpleInteractable>();
            boton.AddComponent<AudioSource>();
            var logica = boton.AddComponent<BotonExergame>();
            logica.posicion = posicion;
            logica.tapa = tapa.GetComponent<Renderer>();
            var punto = new GameObject("PuntoSenal").transform;
            punto.SetParent(boton.transform, false);
            punto.localPosition = new Vector3(0.02f, 0.03f, 0.03f);
            logica.puntoSenal = punto;

            ConfigurarPoke(boton, col, interactable);
            return logica;
        }

        static void ConfigurarPoke(GameObject boton, Collider col, Component interactable)
        {
            var tipo = Type.GetType("UnityEngine.XR.Interaction.Toolkit.Filtering.XRPokeFilter, Unity.XR.Interaction.Toolkit");
            if (tipo == null)
            {
                Debug.LogWarning("[Exergame] No se encontro XRPokeFilter. Los botones funcionaran con rayo y agarre, pero no tocandolos.");
                return;
            }

            var filtro = boton.AddComponent(tipo);
            var so = new SerializedObject(filtro);
            var pInteractable = so.FindProperty("m_Interactable");
            if (pInteractable != null) pInteractable.objectReferenceValue = interactable;
            var pCollider = so.FindProperty("m_PokeCollider");
            if (pCollider != null) pCollider.objectReferenceValue = col;

            bool direccionAsignada = false;
            var it = so.GetIterator();
            while (it.Next(true))
            {
                if (it.propertyType == SerializedPropertyType.Boolean && it.name == "m_UseConstant" &&
                    it.propertyPath.ToLowerInvariant().Contains("pokeconfiguration"))
                {
                    it.boolValue = true;
                    continue;
                }
                if (it.propertyType != SerializedPropertyType.Enum) continue;
                if (!it.name.ToLowerInvariant().Contains("pokedirection")) continue;
                if (!it.propertyPath.Contains("Constant")) continue;
                int idx = Array.IndexOf(it.enumNames, "NegativeY");
                if (idx >= 0)
                {
                    it.enumValueIndex = idx;
                    direccionAsignada = true;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            if (!direccionAsignada)
                Debug.LogWarning("[Exergame] No se pudo fijar la direccion del poke en " + boton.name + ". Revisar XR Poke Filter en el Inspector (Poke Direction = Negative Y).");
        }

        // Detras del tablero, casi al centro: cara a ~10 grados y circulo mostrado justo encima del tablero (todo dentro de +-16 grados).
        static readonly Vector3 PosicionPresentador = new Vector3(0.26f, 0f, 1.45f);

        static PresentadorColores ConstruirPresentador(Materiales m)
        {
            var raiz = new GameObject("Presentador");
            raiz.transform.position = PosicionPresentador;
            raiz.transform.LookAt(new Vector3(0f, 0f, 0f));

            var cuerpo = Grupo("Cuerpo", raiz.transform).transform;
            foreach (var x in new[] { -0.09f, 0.09f })
            {
                Pieza("Pierna", cuerpo, new Vector3(x, 0.42f, 0f), new Vector3(0.13f, 0.42f, 0.13f), m.pantalon, PrimitiveType.Capsule, conCollider: false, estatico: false);
                Pieza("Zapato", cuerpo, new Vector3(x, 0.03f, 0.04f), new Vector3(0.12f, 0.06f, 0.2f), m.zapato, conCollider: false, estatico: false);
            }
            Pieza("Torso", cuerpo, new Vector3(0f, 1.12f, 0f), new Vector3(0.38f, 0.33f, 0.24f), m.camisa, PrimitiveType.Capsule, conCollider: false, estatico: false);
            Pieza("Cuello", cuerpo, new Vector3(0f, 1.47f, 0f), new Vector3(0.1f, 0.04f, 0.1f), m.piel, PrimitiveType.Cylinder, conCollider: false, estatico: false);
            Pieza("Cabeza", cuerpo, new Vector3(0f, 1.63f, 0f), new Vector3(0.26f, 0.26f, 0.26f), m.piel, PrimitiveType.Sphere, conCollider: false, estatico: false);
            Pieza("Pelo", cuerpo, new Vector3(0f, 1.70f, -0.015f), new Vector3(0.28f, 0.2f, 0.28f), m.pelo, PrimitiveType.Sphere, conCollider: false, estatico: false);
            var ojoIzq = Pieza("OjoIzq", cuerpo, new Vector3(-0.05f, 1.65f, 0.118f), new Vector3(0.035f, 0.035f, 0.035f), m.ojo, PrimitiveType.Sphere, conCollider: false, estatico: false);
            var ojoDer = Pieza("OjoDer", cuerpo, new Vector3(0.05f, 1.65f, 0.118f), new Vector3(0.035f, 0.035f, 0.035f), m.ojo, PrimitiveType.Sphere, conCollider: false, estatico: false);
            var boca = Pieza("Boca", cuerpo, new Vector3(0f, 1.57f, 0.12f), new Vector3(0.07f, 0.018f, 0.02f), m.boca, conCollider: false, estatico: false);

            var hombroIzq = Grupo("HombroIzq", cuerpo).transform;
            hombroIzq.localPosition = new Vector3(-0.24f, 1.40f, 0f);
            Pieza("Brazo", hombroIzq, new Vector3(0f, -0.27f, 0f), new Vector3(0.09f, 0.27f, 0.09f), m.camisa, PrimitiveType.Capsule, conCollider: false, estatico: false);
            Pieza("Mano", hombroIzq, new Vector3(0f, -0.56f, 0f), new Vector3(0.09f, 0.09f, 0.09f), m.piel, PrimitiveType.Sphere, conCollider: false, estatico: false);

            var hombroDer = Grupo("HombroDer", cuerpo).transform;
            hombroDer.localPosition = new Vector3(0.24f, 1.40f, 0f);
            Pieza("Brazo", hombroDer, new Vector3(0f, -0.27f, 0f), new Vector3(0.09f, 0.27f, 0.09f), m.camisa, PrimitiveType.Capsule, conCollider: false, estatico: false);
            Pieza("Mano", hombroDer, new Vector3(0f, -0.56f, 0f), new Vector3(0.1f, 0.1f, 0.1f), m.piel, PrimitiveType.Sphere, conCollider: false, estatico: false);

            var circulo = Grupo("CirculoEnMano", cuerpo).transform;
            circulo.localPosition = new Vector3(0.24f, 1.55f, 0.56f);
            var disco = Grupo("Disco", circulo).transform;
            disco.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Pieza("Aro", disco, Vector3.zero, new Vector3(0.30f, 0.008f, 0.30f), m.aro, PrimitiveType.Cylinder, conCollider: false, estatico: false);
            var color = Pieza("Color", disco, Vector3.zero, new Vector3(0.25f, 0.012f, 0.25f), m.cartelColor, PrimitiveType.Cylinder, conCollider: false, estatico: false);
            Pieza("Mango", circulo, new Vector3(0f, -0.19f, -0.02f), new Vector3(0.025f, 0.05f, 0.025f), m.maderaOscura, PrimitiveType.Cylinder, conCollider: false, estatico: false);

            var voz = raiz.AddComponent<AudioSource>();
            voz.playOnAwake = false;
            voz.spatialBlend = 1f;
            voz.minDistance = 1f;
            voz.maxDistance = 8f;

            var presentador = raiz.AddComponent<PresentadorColores>();
            presentador.hombroCirculo = hombroDer;
            presentador.anguloBrazoArriba = -84f;
            presentador.circulo = circulo;
            presentador.colorCirculo = color.GetComponent<Renderer>();
            presentador.boca = boca.transform;
            presentador.ojos = new[] { ojoIzq.transform, ojoDer.transform };
            presentador.cuerpo = cuerpo;
            presentador.voz = voz;
            PresentadorMousey.Aplicar(presentador);
            return presentador;
        }

        static Material MaterialIcono()
        {
            string ruta = CarpetaMateriales + "/Icono.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (mat == null)
            {
                var unlit = Shader.Find("Universal Render Pipeline/Unlit");
                mat = new Material(unlit != null ? unlit : Shader.Find("Unlit/Texture"));
                AssetDatabase.CreateAsset(mat, ruta);
            }
            return mat;
        }

        static void ConstruirSombrasFalsas(Materiales m)
        {
            var grupo = Grupo("SombrasFalsas").transform;
            var sombra = Mat("SombraFalsa", new Color(0.28f, 0.19f, 0.12f), 0.3f);
            Pieza("Mesa", grupo, new Vector3(0f, 0.002f, 0.5f), new Vector3(1.3f, 0.001f, 0.8f), sombra, PrimitiveType.Cylinder, conCollider: false, proyectaSombra: false);
            Pieza("MesaAuxiliar", grupo, new Vector3(-1.45f, 0.002f, 3.45f), new Vector3(1.0f, 0.001f, 0.55f), sombra, PrimitiveType.Cylinder, conCollider: false, proyectaSombra: false);
            Pieza("Presentador", grupo, new Vector3(PosicionPresentador.x, 0.002f, PosicionPresentador.z), new Vector3(0.55f, 0.001f, 0.45f), sombra, PrimitiveType.Cylinder, conCollider: false, proyectaSombra: false);
        }

        const string CarpetaVoz = CarpetaRaiz + "/Audio/Voz/";

        static AudioClip A(string nombre) => AssetDatabase.LoadAssetAtPath<AudioClip>(CarpetaVoz + nombre + ".wav");

        static void ConstruirTarea(BotonExergame[] botones, PresentadorColores presentador)
        {
            var go = new GameObject("SesionExergame");
            var sesion = go.AddComponent<SesionExergame>();
            var panel = go.AddComponent<PanelEvaluador>();
            panel.sesion = sesion;
            sesion.panel = panel;
            sesion.botones = botones;
            sesion.presentador = presentador;
            sesion.calibrador = Object.FindObjectOfType<CalibradorSentado>();
            sesion.materialBotonMenu = AssetDatabase.LoadAssetAtPath<Material>(CarpetaMateriales + "/Placa.mat");

            var seleccion = new GameObject("PosicionMenuSupervisor").transform;
            seleccion.position = new Vector3(0f, 1.25f, 0.75f);
            seleccion.rotation = Quaternion.LookRotation(seleccion.position - new Vector3(0f, 1.15f, 0f));
            seleccion.SetParent(go.transform, true);
            sesion.raizSeleccion = seleccion;

            sesion.vocesColores = new[] { A("rojo"), A("azul"), A("verde"), A("amarillo") };
            sesion.audioBienvenida = A("bienvenida");
            sesion.audioEsteEs = A("este_es");
            sesion.audioExplicacion = A("explicacion");
            sesion.audioRecordatorio = A("recordatorio");
            sesion.audioPresentacion = A("presentacion");
            sesion.audioPracticaInicio = A("practica_inicio");
            sesion.audioPracticaIzquierda = A("practica_izquierda");
            sesion.audioPracticaDerecha = A("practica_derecha");
            sesion.audioPracticaFin = A("practica_fin");
            sesion.audioPracticaOtraMano = A("practica_otra_mano");
            sesion.audioCambioRegla = A("cambio_regla");
            sesion.audioContinuamos = A("continuamos");
            sesion.audioFin = A("fin");

            const string rutaLista = CarpetaRaiz + "/Participantes.asset";
            var lista = AssetDatabase.LoadAssetAtPath<ListaParticipantes>(rutaLista);
            if (lista == null)
            {
                lista = ScriptableObject.CreateInstance<ListaParticipantes>();
                AssetDatabase.CreateAsset(lista, rutaLista);
            }
            if (lista.participantes.Count == 0 || lista.participantes.All(p => p.codigo == "P000"))
            {
                lista.participantes.Clear();
                for (int i = 1; i <= 7; i++)
                    lista.participantes.Add(new Participante { codigo = $"P0{i}", nombre = $"Participante {i}", saludo = A($"saludo_P0{i}") });
                EditorUtility.SetDirty(lista);
            }
            sesion.lista = lista;

            const string rutaGuion = CarpetaRaiz + "/GuionSesion.asset";
            var guion = AssetDatabase.LoadAssetAtPath<GuionSesion>(rutaGuion);
            if (guion == null)
            {
                guion = ScriptableObject.CreateInstance<GuionSesion>();
                guion.CargarProvisional();
                AssetDatabase.CreateAsset(guion, rutaGuion);
            }
            sesion.guion = guion;

            int faltantes = sesion.vocesColores.Count(c => c == null);
            if (faltantes > 0)
                Debug.LogWarning($"[Exergame] Faltan {faltantes} audios de colores en {CarpetaVoz}");
            if (guion.provisional)
                Debug.LogWarning("[Exergame] El guion de ensayos es PROVISIONAL. Copiar el guion oficial en Assets/Exergame/GuionSesion.asset");
        }

        static void ConstruirIluminacion()
        {
            var grupo = Grupo("Iluminacion").transform;

            var sol = new GameObject("LuzPrincipal");
            sol.transform.SetParent(grupo, false);
            sol.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var luzSol = sol.AddComponent<Light>();
            luzSol.type = LightType.Directional;
            luzSol.color = new Color(1f, 0.96f, 0.9f);
            luzSol.intensity = 0.9f;
            luzSol.shadows = LightShadows.None;
            RenderSettings.sun = luzSol;

            var techo = new GameObject("LuzTecho");
            techo.transform.SetParent(grupo, false);
            techo.transform.position = new Vector3(0f, 2.6f, 1.2f);
            var luzTecho = techo.AddComponent<Light>();
            luzTecho.type = LightType.Point;
            luzTecho.color = new Color(1f, 0.95f, 0.88f);
            luzTecho.intensity = 1.2f;
            luzTecho.range = 6f;
            luzTecho.shadows = LightShadows.None;
        }

        static void ColocarRig(Material materialMano)
        {
            GameObject prefab = null;
            foreach (var ruta in RutasRig)
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                if (prefab != null) break;
            }

            if (prefab == null)
            {
                Debug.LogError("[Exergame] No se encontro el prefab del rig XR. Arrastra 'XR Interaction Setup' a la escena manualmente.");
                return;
            }

            var rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            rig.transform.position = Vector3.zero;
            rig.transform.rotation = Quaternion.identity;

            int desactivados = 0;
            foreach (var mb in rig.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                string nombreTipo = mb.GetType().Name;
                if (ProveedoresLocomocion.Any(p => nombreTipo.Contains(p)))
                {
                    mb.enabled = false;
                    desactivados++;
                }
            }
            int manos = 0;
            foreach (var control in rig.GetComponentsInChildren<XRBaseController>(true))
            {
                string n = control.name.ToLowerInvariant();
                bool esMano = n.Contains("left") || n.Contains("right");
                if (!esMano || n.Contains("teleport") || control.GetComponent<ManoVirtual>() != null) continue;
                var mano = control.gameObject.AddComponent<ManoVirtual>();
                mano.esIzquierda = n.Contains("left") || n.Contains("izq");
                mano.materialMano = materialMano;
                manos++;
            }
            var origen = rig.GetComponentInChildren<XROrigin>(true);
            if (origen != null && origen.GetComponent<CalibradorSentado>() == null)
                origen.gameObject.AddComponent<CalibradorSentado>();
            Debug.Log($"[Exergame] Rig XR colocado. Locomocion desactivada: {desactivados}. Manos virtuales: {manos}");
        }

        static void AgregarABuild()
        {
            var escenas = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes.Where(s => s.path != RutaEscena));
            escenas.Insert(0, new EditorBuildSettingsScene(RutaEscena, true));
            EditorBuildSettings.scenes = escenas.ToArray();
        }
    }
}
