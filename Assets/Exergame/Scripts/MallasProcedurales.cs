using System.Collections.Generic;
using UnityEngine;

namespace Exergame
{
    /// <summary>
    /// Formas simples generadas por codigo (sin modelos externos): rectangulo redondeado
    /// con colita (globo de dialogo), anillo y globo con forma de gota.
    /// Las formas planas estan en el plano XY y se ven por ambos lados.
    /// </summary>
    public static class MallasProcedurales
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        static Mesh Guardar(string clave, Mesh m)
        {
            m.name = clave;
            cache[clave] = m;
            return m;
        }

        /// <summary>Rectangulo de esquinas redondeadas; con colita opcional hacia +X (abajo a la derecha).</summary>
        public static Mesh RectanguloRedondeado(float ancho, float alto, float radio, float colita = 0f)
        {
            string clave = $"rect{ancho:0.000}_{alto:0.000}_{radio:0.000}_{colita:0.000}";
            if (cache.TryGetValue(clave, out var hecha) && hecha != null) return hecha;

            var contorno = new List<Vector2>();
            float hx = ancho * 0.5f, hy = alto * 0.5f;
            radio = Mathf.Min(radio, hx, hy);
            const int seg = 8;
            // Esquinas: sup-der, sup-izq, inf-izq, inf-der (sentido antihorario).
            var centros = new[]
            {
                new Vector2(hx - radio, hy - radio), new Vector2(-hx + radio, hy - radio),
                new Vector2(-hx + radio, -hy + radio), new Vector2(hx - radio, -hy + radio)
            };
            for (int c = 0; c < 4; c++)
            {
                for (int i = 0; i <= seg; i++)
                {
                    float a = (c * 90f + i * 90f / seg) * Mathf.Deg2Rad;
                    contorno.Add(centros[c] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radio);
                }
                // Colita en el lado derecho, despues de la esquina inferior derecha.
                if (c == 3 && colita > 0f)
                {
                    contorno.Add(new Vector2(hx, -hy * 0.15f));
                    contorno.Add(new Vector2(hx + colita, -hy * 0.55f));
                    contorno.Add(new Vector2(hx, hy * 0.25f));
                }
            }

            var verts = new List<Vector3> { Vector3.zero };
            foreach (var p in contorno) verts.Add(new Vector3(p.x, p.y, 0f));
            var tris = new List<int>();
            int n = contorno.Count;
            for (int i = 0; i < n; i++)
            {
                int a = 1 + i, b = 1 + (i + 1) % n;
                tris.Add(0); tris.Add(b); tris.Add(a);   // un lado
                tris.Add(0); tris.Add(a); tris.Add(b);   // el otro lado
            }
            var m = new Mesh();
            m.SetVertices(verts);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return Guardar(clave, m);
        }

        /// <summary>Anillo plano (para el aro que "llama" a tocar el globo).</summary>
        public static Mesh Anillo(float radioInterior, float radioExterior, int segmentos = 48)
        {
            string clave = $"anillo{radioInterior:0.000}_{radioExterior:0.000}";
            if (cache.TryGetValue(clave, out var hecha) && hecha != null) return hecha;
            var verts = new List<Vector3>();
            var tris = new List<int>();
            for (int i = 0; i <= segmentos; i++)
            {
                float a = i * Mathf.PI * 2f / segmentos;
                var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                verts.Add(d * radioInterior);
                verts.Add(d * radioExterior);
            }
            for (int i = 0; i < segmentos; i++)
            {
                int a = i * 2, b = a + 1, c = a + 2, d = a + 3;
                tris.AddRange(new[] { a, b, c, c, b, d, a, c, b, c, d, b });
            }
            var m = new Mesh();
            m.SetVertices(verts);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            return Guardar(clave, m);
        }

        /// <summary>Globo con forma de gota (mas ancho arriba), centrado en el origen.</summary>
        public static Mesh Globo(float radio, float alto)
        {
            string clave = $"globo{radio:0.000}_{alto:0.000}";
            if (cache.TryGetValue(clave, out var hecha) && hecha != null) return hecha;
            const int anillos = 24, lados = 32;
            var verts = new List<Vector3>();
            var tris = new List<int>();
            for (int j = 0; j <= anillos; j++)
            {
                float u = j / (float)anillos;                 // 0 abajo, 1 arriba
                float y = -Mathf.Cos(Mathf.PI * u) * alto * 0.5f;
                float r = Mathf.Sin(Mathf.PI * u) * radio * (0.72f + 0.28f * Mathf.Sqrt(u));
                for (int i = 0; i <= lados; i++)
                {
                    float a = i * Mathf.PI * 2f / lados;
                    verts.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r));
                }
            }
            for (int j = 0; j < anillos; j++)
                for (int i = 0; i < lados; i++)
                {
                    int a = j * (lados + 1) + i, b = a + lados + 1;
                    tris.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
                }
            var m = new Mesh();
            m.SetVertices(verts);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return Guardar(clave, m);
        }

        /// <summary>Estrella de 5 puntas en el plano XY, visible por ambos lados.</summary>
        public static Mesh Estrella(float radio, float radioInterior)
        {
            string clave = $"estrella{radio:0.000}_{radioInterior:0.000}";
            if (cache.TryGetValue(clave, out var hecha) && hecha != null) return hecha;
            var verts = new List<Vector3> { Vector3.zero };
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float r = i % 2 == 0 ? radio : radioInterior;
                verts.Add(new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f));
            }
            var tris = new List<int>();
            for (int i = 0; i < 10; i++)
            {
                int a = 1 + i, b = 1 + (i + 1) % 10;
                tris.AddRange(new[] { 0, b, a, 0, a, b });
            }
            var m = new Mesh();
            m.SetVertices(verts);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            return Guardar(clave, m);
        }

        /// <summary>Cono (o cono truncado) con base en y=0, para el volcan.</summary>
        public static Mesh Cono(float radioBase, float radioArriba, float alto, int lados = 48)
        {
            string clave = $"cono{radioBase:0.0}_{radioArriba:0.0}_{alto:0.0}";
            if (cache.TryGetValue(clave, out var hecha) && hecha != null) return hecha;
            var verts = new List<Vector3>();
            var tris = new List<int>();
            for (int i = 0; i <= lados; i++)
            {
                float a = i * Mathf.PI * 2f / lados;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                verts.Add(d * radioBase);
                verts.Add(d * radioArriba + Vector3.up * alto);
            }
            for (int i = 0; i < lados; i++)
            {
                int a = i * 2, b = a + 1, c = a + 2, d = a + 3;
                tris.AddRange(new[] { a, b, c, c, b, d });
            }
            int centro = verts.Count;
            verts.Add(Vector3.up * alto);
            for (int i = 0; i < lados; i++) tris.AddRange(new[] { centro, i * 2 + 3, i * 2 + 1 });
            var m = new Mesh();
            m.SetVertices(verts);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return Guardar(clave, m);
        }

        /// <summary>Esfera vista desde adentro (cielo). UV v: 0 abajo, 1 arriba.</summary>
        public static Mesh CieloInterior(float radio)
        {
            string clave = $"cielo{radio:0.0}";
            if (cache.TryGetValue(clave, out var hecha) && hecha != null) return hecha;
            const int anillos = 24, lados = 48;
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            for (int j = 0; j <= anillos; j++)
            {
                float v = j / (float)anillos;
                float th = Mathf.PI * (v - 0.5f);
                for (int i = 0; i <= lados; i++)
                {
                    float a = i * Mathf.PI * 2f / lados;
                    verts.Add(new Vector3(Mathf.Cos(th) * Mathf.Cos(a), Mathf.Sin(th), Mathf.Cos(th) * Mathf.Sin(a)) * radio);
                    uvs.Add(new Vector2(i / (float)lados, v));
                }
            }
            for (int j = 0; j < anillos; j++)
                for (int i = 0; i < lados; i++)
                {
                    int a = j * (lados + 1) + i, b = a + lados + 1;
                    tris.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });   // caras hacia adentro
                }
            var m = new Mesh();
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return Guardar(clave, m);
        }

        /// <summary>Objeto con la malla y el material dados.</summary>
        public static GameObject Crear(string nombre, Mesh malla, Material mat, Transform padre)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.AddComponent<MeshFilter>().sharedMesh = malla;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        public static Material Plano(Color c)
        {
            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            var m = new Material(sh != null ? sh : Shader.Find("Unlit/Color"));
            m.SetColor("_BaseColor", c);
            m.SetColor("_Color", c);
            return m;
        }

        public static Material Brillante(Color c, Material baseLit)
        {
            var m = baseLit != null ? new Material(baseLit) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c);
            m.SetColor("_Color", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.88f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            return m;
        }
    }
}
