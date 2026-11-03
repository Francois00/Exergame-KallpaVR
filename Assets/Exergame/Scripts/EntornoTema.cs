using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Exergame
{
    /// <summary>
    /// Fondo de cada escenario. Clasico = la sala de siempre. Globos = Plaza de Armas de Arequipa
    /// (catedral de sillar, portales, palmeras y el Misti al fondo). Tambores = jardin con arboles y cerca.
    /// Todo es estatico y lejano (no marea) y se arma por codigo una sola vez.
    /// Los colores del fondo evitan rojo, azul, verde y amarillo intensos para no competir con el tablero.
    /// </summary>
    public static class EntornoTema
    {
        static GameObject sala, decoracion, plaza, jardin;
        static bool guardado;
        static Color ambCielo, ambEcuador, ambSuelo;
        static float intensidadSol = -1f;
        static Material materialLit;

        public static void Aplicar(Escenario e, Material baseLit, Transform tablero = null)
        {
            if (tablero != null) TableroTema(e, tablero);
            if (baseLit != null) materialLit = baseLit;
            if (sala == null) sala = GameObject.Find("Sala");
            if (decoracion == null) decoracion = GameObject.Find("Decoracion");
            if (!guardado)
            {
                ambCielo = RenderSettings.ambientSkyColor;
                ambEcuador = RenderSettings.ambientEquatorColor;
                ambSuelo = RenderSettings.ambientGroundColor;
                if (RenderSettings.sun != null) intensidadSol = RenderSettings.sun.intensity;
                guardado = true;
            }

            bool afuera = e != Escenario.Clasico;
            if (sala != null) sala.SetActive(!afuera);
            if (decoracion != null) decoracion.SetActive(!afuera);
            if (e == Escenario.Globos && plaza == null) plaza = ArmarPlaza();
            if (e == Escenario.Tambores && jardin == null) jardin = ArmarJardin();
            if (plaza != null) plaza.SetActive(e == Escenario.Globos);
            if (jardin != null) jardin.SetActive(e == Escenario.Tambores);

            if (afuera)
            {
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.72f, 0.80f, 0.92f);
                RenderSettings.ambientEquatorColor = new Color(0.68f, 0.68f, 0.66f);
                RenderSettings.ambientGroundColor = new Color(0.42f, 0.40f, 0.36f);
                if (RenderSettings.sun != null) RenderSettings.sun.intensity = 1.15f;
            }
            else
            {
                RenderSettings.ambientSkyColor = ambCielo;
                RenderSettings.ambientEquatorColor = ambEcuador;
                RenderSettings.ambientGroundColor = ambSuelo;
                if (RenderSettings.sun != null && intensidadSol >= 0f) RenderSettings.sun.intensity = intensidadSol;
            }
        }

        // ------------------------------------------------------------ piezas

        static Material Lit(Color c, float brillo = 0.15f)
        {
            var m = materialLit != null ? new Material(materialLit) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c);
            m.SetColor("_Color", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", brillo);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", null);
            return m;
        }

        static Material Textura(Texture2D tex, Vector2 repetir, float brillo = 0.1f)
        {
            var m = Lit(Color.white, brillo);
            m.SetTexture("_BaseMap", tex);
            m.mainTexture = tex;
            m.SetTextureScale("_BaseMap", repetir);
            m.mainTextureScale = repetir;
            return m;
        }

        static GameObject P(PrimitiveType tipo, Transform padre, Vector3 pos, Vector3 escala, Material m, Vector3 rot = default)
        {
            var go = GameObject.CreatePrimitive(tipo);
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(padre, false);
            go.transform.localPosition = pos;
            go.transform.localScale = escala;
            go.transform.localRotation = Quaternion.Euler(rot);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        static void Cielo(Transform padre, Color zenit, Color horizonte)
        {
            var tex = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int i = 0; i < 64; i++)
            {
                float v = i / 63f;               // 0 abajo, 1 arriba
                float k = Mathf.Clamp01((v - 0.5f) / 0.5f);
                tex.SetPixel(0, i, v < 0.5f ? horizonte * 0.92f : Color.Lerp(horizonte, zenit, Mathf.Pow(k, 0.55f)));
            }
            tex.Apply();
            var m = MallasProcedurales.Plano(Color.white);
            m.SetTexture("_BaseMap", tex);
            m.mainTexture = tex;
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
            var go = MallasProcedurales.Crear("Cielo", MallasProcedurales.CieloInterior(400f), m, padre);
            go.transform.localPosition = new Vector3(0f, 0f, 0f);
        }

        static Texture2D TexturaRuido(Color a, Color b, int tam, float escala, bool baldosas = false)
        {
            var tex = new Texture2D(tam, tam, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < tam; y++)
                for (int x = 0; x < tam; x++)
                {
                    float n = Mathf.PerlinNoise(x * escala, y * escala) * 0.7f + Mathf.PerlinNoise(x * escala * 4f + 31f, y * escala * 4f + 17f) * 0.3f;
                    var c = Color.Lerp(a, b, n);
                    if (baldosas && (x % (tam / 4) < 1 || y % (tam / 4) < 1)) c *= 0.8f;   // juntas de las baldosas
                    tex.SetPixel(x, y, c);
                }
            tex.Apply(true);
            return tex;
        }

        static GameObject Suelo(Transform padre, Material m)
        {
            return P(PrimitiveType.Plane, padre, new Vector3(0f, 0f, 20f), new Vector3(40f, 1f, 40f), m);
        }

        static void Palmera(Transform padre, Vector3 pos, float alto, Material tronco, Material hoja)
        {
            P(PrimitiveType.Cylinder, padre, pos + Vector3.up * alto / 2f, new Vector3(0.32f, alto / 2f, 0.32f), tronco);
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f;
                var dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                P(PrimitiveType.Sphere, padre, pos + Vector3.up * (alto + 0.05f) + dir * 1.1f, new Vector3(0.45f, 0.12f, 2.4f), hoja, new Vector3(22f, a, 0f));
            }
        }

        static void Arbol(Transform padre, Vector3 pos, float r, Material tronco, Material copa, Material copa2)
        {
            P(PrimitiveType.Cylinder, padre, pos + Vector3.up * 1.2f, new Vector3(0.35f, 1.2f, 0.35f), tronco);
            P(PrimitiveType.Sphere, padre, pos + new Vector3(0f, 2.4f + r * 0.6f, 0f), new Vector3(r * 2f, r * 1.8f, r * 2f), copa);
            P(PrimitiveType.Sphere, padre, pos + new Vector3(r * 0.55f, 2.2f + r * 0.4f, -r * 0.2f), new Vector3(r * 1.4f, r * 1.3f, r * 1.4f), copa2);
            P(PrimitiveType.Sphere, padre, pos + new Vector3(-r * 0.6f, 2.3f + r * 0.35f, r * 0.1f), new Vector3(r * 1.3f, r * 1.2f, r * 1.3f), copa2);
        }

        /// <summary>Galeria de portales de sillar: arcos oscuros abajo y ventanas arriba. Rotacion 0 = mira a -Z.</summary>
        static void Portal(Transform padre, Vector3 pos, float rotY, float largo, Material sillar, Material oscuro)
        {
            var por = new GameObject("Portales").transform;
            por.SetParent(padre, false);
            por.localPosition = pos;
            por.localRotation = Quaternion.Euler(0f, rotY, 0f);
            P(PrimitiveType.Cube, por, new Vector3(0f, 4.5f, 1.5f), new Vector3(largo, 9f, 3f), sillar);
            P(PrimitiveType.Cube, por, new Vector3(0f, 9.15f, 1.3f), new Vector3(largo + 0.4f, 0.3f, 3.4f), sillar);
            int arcos = Mathf.FloorToInt(largo / 2.8f);
            for (int i = 0; i < arcos; i++)
            {
                float x = -largo / 2f + 1.4f + i * 2.8f;
                P(PrimitiveType.Cube, por, new Vector3(x, 1.6f, -0.02f), new Vector3(2f, 3.2f, 0.2f), oscuro);
                P(PrimitiveType.Sphere, por, new Vector3(x, 3.2f, -0.02f), new Vector3(2f, 1.1f, 0.2f), oscuro);
                P(PrimitiveType.Cube, por, new Vector3(x, 6.2f, -0.02f), new Vector3(1.1f, 1.7f, 0.2f), oscuro);
            }
        }

        // ------------------------------------------------------------ plaza de Arequipa

        static GameObject ArmarPlaza()
        {
            var raiz = new GameObject("EntornoPlaza").transform;
            Cielo(raiz, new Color(0.30f, 0.55f, 0.88f), new Color(0.82f, 0.90f, 0.97f));
            Suelo(raiz, Textura(TexturaRuido(new Color(0.70f, 0.66f, 0.60f), new Color(0.80f, 0.77f, 0.71f), 128, 0.06f, true), new Vector2(60f, 60f)));

            var sillar = Textura(TexturaRuido(new Color(0.88f, 0.86f, 0.80f), new Color(0.96f, 0.95f, 0.91f), 64, 0.12f), new Vector2(4f, 4f));
            var oscuro = Lit(new Color(0.22f, 0.22f, 0.25f));
            var madera = Lit(new Color(0.30f, 0.20f, 0.12f));

            // Catedral al frente: nave con columnas, dos torres con cupula y la puerta.
            var cat = new GameObject("Catedral").transform;
            cat.SetParent(raiz, false);
            cat.localPosition = new Vector3(0f, 0f, 30f);
            P(PrimitiveType.Cube, cat, new Vector3(0f, 5f, 0f), new Vector3(30f, 10f, 3f), sillar);
            P(PrimitiveType.Cube, cat, new Vector3(0f, 10.3f, -0.2f), new Vector3(30.5f, 0.6f, 3.4f), sillar);
            for (int i = -6; i <= 6; i++)
                P(PrimitiveType.Cylinder, cat, new Vector3(i * 2.1f, 4.5f, -1.8f), new Vector3(0.6f, 4.5f, 0.6f), sillar);
            P(PrimitiveType.Cube, cat, new Vector3(0f, 2.3f, -1.55f), new Vector3(2.6f, 4.6f, 0.2f), madera);
            foreach (float x in new[] { -6.3f, 6.3f, -10.4f, 10.4f })
                P(PrimitiveType.Cube, cat, new Vector3(x, 6.5f, -1.55f), new Vector3(1.2f, 2.2f, 0.2f), oscuro);
            foreach (float x in new[] { -13.5f, 13.5f })
            {
                P(PrimitiveType.Cube, cat, new Vector3(x, 9f, 0f), new Vector3(4.5f, 18f, 4.5f), sillar);
                P(PrimitiveType.Cube, cat, new Vector3(x, 19.5f, 0f), new Vector3(3.4f, 3f, 3.4f), sillar);
                P(PrimitiveType.Cube, cat, new Vector3(x, 19.5f, -1.75f), new Vector3(1.3f, 2.2f, 0.1f), oscuro);
                P(PrimitiveType.Sphere, cat, new Vector3(x, 21.6f, 0f), new Vector3(2.8f, 2.4f, 2.8f), sillar);
                P(PrimitiveType.Cube, cat, new Vector3(x, 14f, -2.3f), new Vector3(1.4f, 2.6f, 0.1f), oscuro);
            }

            // Portales alrededor de toda la plaza (vista en 360: tambien a los lados y detras de la persona).
            foreach (float lado in new[] { -1f, 1f })
                Portal(raiz, new Vector3(lado * 14f, 0f, 8f), lado > 0 ? 90f : -90f, 34f, sillar, oscuro);
            Portal(raiz, new Vector3(0f, 0f, -12f), 180f, 31f, sillar, oscuro);

            // Pileta central y palmeras.
            P(PrimitiveType.Cylinder, raiz, new Vector3(0f, 0.3f, 15f), new Vector3(4.2f, 0.3f, 4.2f), sillar);
            P(PrimitiveType.Cylinder, raiz, new Vector3(0f, 1.1f, 15f), new Vector3(0.6f, 0.9f, 0.6f), sillar);
            P(PrimitiveType.Cylinder, raiz, new Vector3(0f, 2.1f, 15f), new Vector3(1.8f, 0.12f, 1.8f), sillar);
            var tronco = Lit(new Color(0.42f, 0.33f, 0.24f));
            var hoja = Lit(new Color(0.20f, 0.36f, 0.20f));
            foreach (var p in new[] { new Vector3(-5f, 0f, 9f), new Vector3(5.5f, 0f, 10f), new Vector3(-8f, 0f, 19f), new Vector3(8.5f, 0f, 20f), new Vector3(-3f, 0f, 23f), new Vector3(3.5f, 0f, 24f),
                                      new Vector3(-8f, 0f, 1f), new Vector3(8f, 0f, 2f), new Vector3(-6f, 0f, -6f), new Vector3(6.5f, 0f, -6.5f), new Vector3(0f, 0f, -7.5f) })
                Palmera(raiz, p, 6.5f, tronco, hoja);
            // Bancas de la plaza.
            foreach (var p in new[] { new Vector3(-4.5f, 0f, -3.5f), new Vector3(4.5f, 0f, -3.5f), new Vector3(-6f, 0f, 5f), new Vector3(6f, 0f, 5.5f) })
            {
                P(PrimitiveType.Cube, raiz, p + Vector3.up * 0.45f, new Vector3(1.8f, 0.08f, 0.5f), madera);
                P(PrimitiveType.Cube, raiz, p + new Vector3(0f, 0.8f, 0.22f), new Vector3(1.8f, 0.5f, 0.06f), madera);
                P(PrimitiveType.Cube, raiz, p + new Vector3(-0.8f, 0.22f, 0f), new Vector3(0.08f, 0.44f, 0.45f), oscuro);
                P(PrimitiveType.Cube, raiz, p + new Vector3(0.8f, 0.22f, 0f), new Vector3(0.08f, 0.44f, 0.45f), oscuro);
            }

            // El Misti al fondo, a la izquierda (cono con nieve), y montanas bajas.
            var volcan = Lit(new Color(0.46f, 0.40f, 0.43f), 0.05f);
            var nieve = Lit(new Color(0.96f, 0.96f, 0.98f), 0.2f);
            var misti = MallasProcedurales.Crear("Misti", MallasProcedurales.Cono(170f, 18f, 78f), volcan, raiz);
            misti.transform.localPosition = new Vector3(-150f, -6f, 300f);
            var cumbre = MallasProcedurales.Crear("Nieve", MallasProcedurales.Cono(48f, 17f, 22f), nieve, raiz);
            cumbre.transform.localPosition = new Vector3(-150f, 50.5f, 300f);
            var cerro = MallasProcedurales.Crear("Cerros", MallasProcedurales.Cono(260f, 60f, 40f), Lit(new Color(0.55f, 0.50f, 0.50f), 0.05f), raiz);
            cerro.transform.localPosition = new Vector3(160f, -8f, 330f);
            AdornosPlaza(raiz);
            return raiz.gameObject;
        }

        // ------------------------------------------------------------ jardin

        static GameObject ArmarJardin()
        {
            var raiz = new GameObject("EntornoJardin").transform;
            Cielo(raiz, new Color(0.32f, 0.58f, 0.88f), new Color(0.84f, 0.92f, 0.97f));
            Suelo(raiz, Textura(TexturaRuido(new Color(0.30f, 0.46f, 0.20f), new Color(0.42f, 0.58f, 0.26f), 128, 0.08f), new Vector2(50f, 50f)));

            // Cerca blanca detras de la mesa.
            var blanco = Lit(new Color(0.95f, 0.95f, 0.93f), 0.3f);
            var cerca = new GameObject("Cerca").transform;
            cerca.SetParent(raiz, false);
            // Cerca alrededor del jardin (vista en 360).
            const float mitad = 9f;
            var centros = new[] { new Vector3(0f, 0f, 5.5f), new Vector3(0f, 0f, -6.5f), new Vector3(-mitad, 0f, -0.5f), new Vector3(mitad, 0f, -0.5f) };
            var rots = new[] { 0f, 0f, 90f, 90f };
            for (int t = 0; t < 4; t++)
            {
                var centro = centros[t];
                float rot = rots[t];
                var tramo = new GameObject("Tramo").transform;
                tramo.SetParent(cerca, false);
                tramo.localPosition = centro;
                tramo.localRotation = Quaternion.Euler(0f, rot, 0f);
                float largo = rot == 0f ? mitad * 2f : 12f;
                int n = Mathf.FloorToInt(largo / 0.42f);
                for (int i = 0; i <= n; i++)
                    P(PrimitiveType.Cube, tramo, new Vector3(-largo / 2f + i * 0.42f, 0.55f, 0f), new Vector3(0.1f, 1.1f, 0.04f), blanco);
                P(PrimitiveType.Cube, tramo, new Vector3(0f, 0.8f, 0.03f), new Vector3(largo, 0.08f, 0.03f), blanco);
                P(PrimitiveType.Cube, tramo, new Vector3(0f, 0.3f, 0.03f), new Vector3(largo, 0.08f, 0.03f), blanco);
            }

            // Setos, arboles y flores de colores suaves (rosa, lila, blanco).
            var seto = Lit(new Color(0.16f, 0.32f, 0.14f));
            var copa = Lit(new Color(0.20f, 0.40f, 0.17f));
            var copa2 = Lit(new Color(0.25f, 0.46f, 0.20f));
            var tronco = Lit(new Color(0.36f, 0.26f, 0.18f));
            for (int i = -6; i <= 6; i++)
            {
                P(PrimitiveType.Sphere, raiz, new Vector3(i * 1.8f, 0.55f, 7.2f), new Vector3(2.2f, 1.4f, 1.4f), seto);
                P(PrimitiveType.Sphere, raiz, new Vector3(i * 1.8f, 0.55f, -8.2f), new Vector3(2.2f, 1.4f, 1.4f), seto);
            }
            for (int i = -3; i <= 3; i++)
            {
                P(PrimitiveType.Sphere, raiz, new Vector3(-10.6f, 0.55f, i * 1.9f), new Vector3(1.4f, 1.4f, 2.2f), seto);
                P(PrimitiveType.Sphere, raiz, new Vector3(10.6f, 0.55f, i * 1.9f), new Vector3(1.4f, 1.4f, 2.2f), seto);
            }
            foreach (var p in new[] { new Vector3(-6f, 0f, 12f), new Vector3(6.5f, 0f, 13f), new Vector3(-1.5f, 0f, 17f), new Vector3(11f, 0f, 18f), new Vector3(-12f, 0f, 17f), new Vector3(3f, 0f, 24f), new Vector3(-8f, 0f, 26f),
                                      new Vector3(-14f, 0f, 4f), new Vector3(14.5f, 0f, 6f), new Vector3(-15f, 0f, -8f), new Vector3(15f, 0f, -7f), new Vector3(-5f, 0f, -13f), new Vector3(5f, 0f, -14f), new Vector3(0f, 0f, -20f), new Vector3(-11f, 0f, -18f), new Vector3(11f, 0f, -19f) })
                Arbol(raiz, p, 1.6f + (Mathf.Abs(p.x) % 3f) * 0.25f, tronco, copa, copa2);

            var flores = new[] { Lit(new Color(1f, 0.72f, 0.82f)), Lit(new Color(0.82f, 0.72f, 0.96f)), Lit(new Color(0.98f, 0.98f, 0.96f)) };
            var centroFlor = Lit(new Color(0.98f, 0.97f, 0.92f));
            var tallo = Lit(new Color(0.22f, 0.40f, 0.18f));
            var azar = new System.Random(4);
            for (int i = 0; i < 40; i++)
            {
                float x = (float)(azar.NextDouble() * 18.0 - 9.0);
                float z = (i % 2 == 0) ? 3.2f + (float)azar.NextDouble() * 2.0f : -5.8f + (float)azar.NextDouble() * 1.5f;
                if (Mathf.Abs(x) < 1.6f && z > 0f && z < 4f) continue;   // nada justo detras del tablero
                // Flor: corola de 5 petalos (estrella) mirando arriba, con centro blanco y tallo corto.
                var flor = MallasProcedurales.Crear("Flor", MallasProcedurales.Estrella(0.11f, 0.06f), flores[i % flores.Length], raiz);
                flor.transform.localPosition = new Vector3(x, 0.16f, z);
                flor.transform.localRotation = Quaternion.Euler(90f, i * 37f, 0f);
                P(PrimitiveType.Sphere, raiz, new Vector3(x, 0.17f, z), new Vector3(0.05f, 0.03f, 0.05f), centroFlor);
                P(PrimitiveType.Cylinder, raiz, new Vector3(x, 0.08f, z), new Vector3(0.015f, 0.08f, 0.015f), tallo);
            }

            // Cerros lejanos.
            var cerro = Lit(new Color(0.48f, 0.56f, 0.48f), 0.05f);
            var c1 = MallasProcedurales.Crear("Cerro", MallasProcedurales.Cono(220f, 40f, 45f), cerro, raiz);
            c1.transform.localPosition = new Vector3(-120f, -6f, 320f);
            var c2 = MallasProcedurales.Crear("Cerro", MallasProcedurales.Cono(200f, 30f, 38f), cerro, raiz);
            c2.transform.localPosition = new Vector3(150f, -6f, 300f);
            var c3 = MallasProcedurales.Crear("Cerro", MallasProcedurales.Cono(230f, 40f, 40f), cerro, raiz);
            c3.transform.localPosition = new Vector3(40f, -6f, -330f);
            AdornosJardin(raiz);
            return raiz.gameObject;
        }

        // ------------------------------------------------------------ tablero y adornos por escenario

        static readonly Dictionary<Renderer, Material> originales = new Dictionary<Renderer, Material>();
        static readonly string[] piezasMarco = { "Base", "Respaldo", "Marco", "DivisorH", "DivisorV" };
        static Transform adornoGlobos, adornoTambores;

        /// <summary>
        /// El mismo tablero (misma posicion y tamano) con otra terminacion segun el escenario:
        /// Globos = blanco con fondo lila; Tambores = negro piano con fondo crema y baquetas al lado.
        /// </summary>
        static void TableroTema(Escenario e, Transform tablero)
        {
            foreach (var r in tablero.GetComponentsInChildren<Renderer>(true))
            {
                bool marco = System.Array.IndexOf(piezasMarco, r.name) >= 0;
                bool fondo = r.name == "Fondo";
                if (!marco && !fondo) continue;
                if (!originales.ContainsKey(r)) originales[r] = r.sharedMaterial;
                if (e == Escenario.Clasico) { r.sharedMaterial = originales[r]; continue; }
                Color c; float brillo;
                if (e == Escenario.Globos) { c = marco ? new Color(0.96f, 0.96f, 0.97f) : new Color(0.90f, 0.86f, 0.97f); brillo = marco ? 0.6f : 0.25f; }
                else { c = marco ? new Color(0.07f, 0.07f, 0.08f) : new Color(0.97f, 0.91f, 0.78f); brillo = marco ? 0.85f : 0.25f; }
                var m = new Material(originales[r]);
                m.SetColor("_BaseColor", c);
                m.SetColor("_Color", c);
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", null);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", brillo);
                r.sharedMaterial = m;
            }

            // (Sin racimo de globos junto al tablero: podia distraer al residente.)
            if (e == Escenario.Tambores && adornoTambores == null) adornoTambores = Baquetas(tablero);
            if (adornoGlobos != null) adornoGlobos.gameObject.SetActive(e == Escenario.Globos);
            if (adornoTambores != null) adornoTambores.gameObject.SetActive(e == Escenario.Tambores);
        }

        /// <summary>Dos baquetas cruzadas sobre la mesa, a la izquierda del tablero.</summary>
        static Transform Baquetas(Transform tablero)
        {
            var raiz = new GameObject("Baquetas").transform;
            raiz.SetParent(tablero, false);
            raiz.position = new Vector3(-0.47f, 0.731f, 0.66f + tablero.position.z);   // sigue al tablero si se acerco
            var madera = Lit(new Color(0.86f, 0.70f, 0.47f), 0.45f);
            foreach (float ang in new[] { 20f, -16f })
            {
                var b = new GameObject("Baqueta").transform;
                b.SetParent(raiz, false);
                b.localRotation = Quaternion.Euler(0f, ang, 90f);
                P(PrimitiveType.Cylinder, b, Vector3.zero, new Vector3(0.018f, 0.16f, 0.018f), madera);
                P(PrimitiveType.Sphere, b, new Vector3(0f, 0.16f, 0f), new Vector3(0.024f, 0.03f, 0.024f), madera);
                raiz.position += Vector3.up * 0.0001f;
            }
            return raiz;
        }

        /// <summary>Racimo de globos pastel (adorno) atado a la mesa, a la izquierda del tablero.</summary>
        static Transform RacimoGlobos(Transform tablero)
        {
            var raiz = new GameObject("RacimoGlobos").transform;
            raiz.SetParent(tablero, false);
            raiz.position = new Vector3(-0.45f, 0.72f, 0.62f);
            var hilo = MallasProcedurales.Plano(new Color(0.95f, 0.95f, 0.96f));
            var cols = new[] { new Color(1f, 0.72f, 0.82f), new Color(0.84f, 0.74f, 0.98f), new Color(0.98f, 0.97f, 0.95f) };
            var off = new[] { new Vector3(-0.05f, 0.50f, 0.02f), new Vector3(0.05f, 0.56f, 0.0f), new Vector3(0f, 0.44f, -0.03f) };
            P(PrimitiveType.Cylinder, raiz, new Vector3(0f, 0.012f, 0f), new Vector3(0.04f, 0.012f, 0.04f), Lit(new Color(0.8f, 0.8f, 0.82f), 0.6f));
            for (int i = 0; i < 3; i++)
            {
                var g = MallasProcedurales.Crear("GloboAdorno", MallasProcedurales.Globo(0.06f, 0.14f), MallasProcedurales.Brillante(cols[i], materialLit), raiz);
                g.transform.localPosition = off[i];
                var h = new GameObject("Hilo").AddComponent<LineRenderer>();
                h.transform.SetParent(raiz, false);
                h.useWorldSpace = false;
                h.positionCount = 2;
                h.SetPosition(0, new Vector3(0f, 0.02f, 0f));
                h.SetPosition(1, off[i] - new Vector3(0f, 0.07f, 0f));
                h.widthMultiplier = 0.002f;
                h.sharedMaterial = hilo;
            }
            return raiz;
        }

        /// <summary>Malla de triangulos (banderines) de un solo color, para pocas llamadas de dibujo.</summary>
        static Mesh MallaBanderines(List<Vector3> esquinas)
        {
            var m = new Mesh();
            var tris = new List<int>();
            for (int i = 0; i < esquinas.Count; i += 3) tris.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 1 });
            m.SetVertices(esquinas);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            return m;
        }

        static void AdornosPlaza(Transform raiz)
        {
            // Banderines de fiesta cruzando la plaza (colores pastel, no compiten con el tablero).
            var colores = new[] { new Color(1f, 0.72f, 0.82f), new Color(0.84f, 0.74f, 0.98f), new Color(0.99f, 0.98f, 0.95f), new Color(1f, 0.82f, 0.68f) };
            var porColor = new List<Vector3>[4];
            for (int k = 0; k < 4; k++) porColor[k] = new List<Vector3>();
            var cuerda = MallasProcedurales.Plano(new Color(0.9f, 0.9f, 0.9f));
            int n = 0;
            foreach (float z in new[] { -6f, 3f, 11f, 19f })
            {
                var lr = new GameObject("Cuerda").AddComponent<LineRenderer>();
                lr.transform.SetParent(raiz, false);
                lr.useWorldSpace = false;
                lr.positionCount = 21;
                lr.widthMultiplier = 0.025f;
                lr.sharedMaterial = cuerda;
                for (int i = 0; i <= 20; i++)
                {
                    float t = i / 20f;
                    float x = Mathf.Lerp(-12.5f, 12.5f, t);
                    float y = 7.2f - Mathf.Sin(t * Mathf.PI) * 1.4f;
                    lr.SetPosition(i, new Vector3(x, y, z));
                    if (i < 20)
                    {
                        float x2 = x + 25f / 20f * 0.5f;
                        float y2 = 7.2f - Mathf.Sin((t + 0.025f) * Mathf.PI) * 1.4f;
                        var lista = porColor[n++ % 4];
                        lista.Add(new Vector3(x2 - 0.38f, y2, z));
                        lista.Add(new Vector3(x2 + 0.38f, y2, z));
                        lista.Add(new Vector3(x2, y2 - 0.75f, z));
                    }
                }
            }
            for (int k = 0; k < 4; k++)
            {
                var m = MallasProcedurales.Plano(colores[k]);
                if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
                MallasProcedurales.Crear("Banderines", MallaBanderines(porColor[k]), m, raiz);
            }

            // Globos pastel que suben lentamente a lo lejos.
            var anim = raiz.gameObject.AddComponent<AmbienteAnimado>();
            var azar = new System.Random(9);
            for (int i = 0; i < 14; i++)
            {
                float ang = (float)(azar.NextDouble() * Mathf.PI * 2.0);
                float dist = 14f + (float)azar.NextDouble() * 14f;
                var g = MallasProcedurales.Crear("GloboLejano", MallasProcedurales.Globo(0.35f, 0.8f), MallasProcedurales.Brillante(colores[i % 4], materialLit), raiz);
                g.transform.localPosition = new Vector3(Mathf.Cos(ang) * dist, 2f + (float)azar.NextDouble() * 20f, Mathf.Sin(ang) * dist + 8f);
                anim.AgregarQueSube(g.transform, 0.35f + (float)azar.NextDouble() * 0.3f, 1.5f, 26f);
            }
        }

        static void AdornosJardin(Transform raiz)
        {
            var blanco = Lit(new Color(0.96f, 0.96f, 0.94f), 0.35f);
            var techo = Lit(new Color(0.32f, 0.36f, 0.38f), 0.3f);
            // Glorieta (kiosco de musica) a la izquierda, al fondo.
            var glo = new GameObject("Glorieta").transform;
            glo.SetParent(raiz, false);
            glo.localPosition = new Vector3(-6.5f, 0f, 11f);
            P(PrimitiveType.Cylinder, glo, new Vector3(0f, 0.18f, 0f), new Vector3(4.2f, 0.18f, 4.2f), blanco);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                P(PrimitiveType.Cylinder, glo, new Vector3(Mathf.Cos(a) * 1.85f, 1.65f, Mathf.Sin(a) * 1.85f), new Vector3(0.14f, 1.3f, 0.14f), blanco);
            }
            P(PrimitiveType.Cylinder, glo, new Vector3(0f, 2.98f, 0f), new Vector3(4.4f, 0.06f, 4.4f), blanco);
            var cono = MallasProcedurales.Crear("Techo", MallasProcedurales.Cono(2.4f, 0.1f, 1.3f, 8), techo, glo);
            cono.transform.localPosition = new Vector3(0f, 3.02f, 0f);
            P(PrimitiveType.Sphere, glo, new Vector3(0f, 4.4f, 0f), Vector3.one * 0.25f, blanco);

            var anim = raiz.gameObject.AddComponent<AmbienteAnimado>();
            // Notas musicales doradas flotando a lo lejos.
            var dorado = MallasProcedurales.Plano(new Color(1f, 0.80f, 0.30f));
            var posNotas = new[] { new Vector3(-4f, 2.6f, 6.5f), new Vector3(3.5f, 2.9f, 7f), new Vector3(-7f, 3.6f, 9f), new Vector3(6.5f, 3.2f, 10f), new Vector3(0.5f, 3.9f, 12f), new Vector3(-6.5f, 2.4f, -6f), new Vector3(6f, 2.8f, -6.5f), new Vector3(-8f, 2.5f, 1f), new Vector3(8f, 2.6f, 2f) };
            foreach (var p in posNotas)
            {
                var nota = new GameObject("NotaFondo").transform;
                nota.SetParent(raiz, false);
                nota.localPosition = p;
                var cab = P(PrimitiveType.Sphere, nota, Vector3.zero, new Vector3(0.36f, 0.26f, 0.12f), dorado);
                cab.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
                P(PrimitiveType.Cube, nota, new Vector3(0.16f, 0.42f, 0f), new Vector3(0.05f, 0.85f, 0.05f), dorado);
                P(PrimitiveType.Cube, nota, new Vector3(0.3f, 0.78f, 0f), new Vector3(0.28f, 0.09f, 0.05f), dorado).transform.localRotation = Quaternion.Euler(0f, 0f, -30f);
                anim.AgregarQueFlota(nota, 0.25f);
            }
            // Mariposas blancas y rosadas sobre las flores.
            var colAlas = new[] { Lit(new Color(0.99f, 0.98f, 0.95f)), Lit(new Color(1f, 0.74f, 0.84f)) };
            var ala = MallasProcedurales.Globo(0.06f, 0.1f);
            var centros = new[] { new Vector3(-3f, 0f, 4.2f), new Vector3(3.5f, 0f, 4.4f), new Vector3(-5.5f, 0f, -5f), new Vector3(5f, 0f, -5.2f), new Vector3(-7.5f, 0f, 1f), new Vector3(7.5f, 0f, 0f) };
            for (int i = 0; i < centros.Length; i++)
            {
                var m = new GameObject("Mariposa").transform;
                m.SetParent(raiz, false);
                var izq = new GameObject("AlaI").transform; izq.SetParent(m, false);
                var der = new GameObject("AlaD").transform; der.SetParent(m, false);
                var a1 = MallasProcedurales.Crear("Ala", ala, colAlas[i % 2], izq);
                a1.transform.localPosition = new Vector3(-0.05f, 0f, 0f); a1.transform.localScale = new Vector3(1f, 1f, 0.15f); a1.transform.localRotation = Quaternion.Euler(0f, 0f, 70f);
                var a2 = MallasProcedurales.Crear("Ala", ala, colAlas[i % 2], der);
                a2.transform.localPosition = new Vector3(0.05f, 0f, 0f); a2.transform.localScale = new Vector3(1f, 1f, 0.15f); a2.transform.localRotation = Quaternion.Euler(0f, 0f, -70f);
                anim.AgregarMariposa(m, izq, der, centros[i], 0.9f + i * 0.1f);
            }
        }
    }
}
