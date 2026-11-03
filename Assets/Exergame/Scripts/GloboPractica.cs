using System.Collections;
using UnityEngine;

namespace Exergame
{
    /// <summary>
    /// Globo de practica (antes de jugar): el residente estira la mano y lo toca.
    /// Se detecta con la punta del dedo o la palma de cualquiera de las dos manos
    /// (en el editor tambien con clic del mouse).
    /// </summary>
    public class GloboPractica : MonoBehaviour
    {
        public float radioToque = 0.13f;
        [Tooltip("Mano con la que hay que tocarlo: el de la izquierda con la izquierda, el de la derecha con la derecha.")]
        public bool esIzquierdo;
        public bool Tocado { get; private set; }
        public bool ConManoIzquierda { get; private set; }
        /// <summary>Veces que lo tocaron con la mano equivocada.</summary>
        public int IntentosManoEquivocada { get; private set; }

        Transform cuerpo;
        // Una mano solo cuenta si antes estuvo fuera del globo (evita toques falsos
        // si un control quedo apoyado justo ahi).
        readonly System.Collections.Generic.HashSet<ManoVirtual> manosListas = new System.Collections.Generic.HashSet<ManoVirtual>();
        readonly System.Collections.Generic.HashSet<ManoVirtual> manosDentro = new System.Collections.Generic.HashSet<ManoVirtual>();
        Vector3 escala;
        float fase;

        Transform aro;
        Transform etiqueta;
        Transform camara;

        /// <summary>Crea un globo de color con su etiqueta ("IZQUIERDA"/"DERECHA") y un aro que late.</summary>
        public static GloboPractica Crear(Vector3 posicion, Color color, Material materialBase, bool esIzquierdo, string textoEtiqueta)
        {
            var raiz = new GameObject("GloboPractica");
            raiz.transform.position = posicion;
            var globo = raiz.AddComponent<GloboPractica>();
            globo.esIzquierdo = esIzquierdo;

            var cuerpo = new GameObject("Cuerpo").transform;
            cuerpo.SetParent(raiz.transform, false);

            // Globo brillante con forma de gota y su nudito.
            var mat = MallasProcedurales.Brillante(color, materialBase);
            var bola = MallasProcedurales.Crear("Globo", MallasProcedurales.Globo(0.085f, 0.21f), mat, cuerpo);
            bola.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            Pieza(PrimitiveType.Sphere, cuerpo, new Vector3(0f, -0.108f, 0f), new Vector3(0.026f, 0.018f, 0.026f), mat);

            // Hilo ondulado.
            var hilo = new GameObject("Hilo");
            hilo.transform.SetParent(cuerpo, false);
            var lr = hilo.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.widthMultiplier = 0.0035f;
            lr.material = MallasProcedurales.Plano(new Color(0.92f, 0.92f, 0.95f));
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            const int puntos = 24;
            lr.positionCount = puntos;
            for (int i = 0; i < puntos; i++)
            {
                float t = i / (float)(puntos - 1);
                lr.SetPosition(i, new Vector3(Mathf.Sin(t * Mathf.PI * 2f) * 0.01f, -0.115f - t * 0.055f, 0f));
            }

            // Etiqueta del color del globo con el lado escrito en grande.
            var etiqueta = new GameObject("Etiqueta").transform;
            etiqueta.SetParent(raiz.transform, false);
            etiqueta.localPosition = new Vector3(0f, -0.20f, 0f);
            var oscuro = Color.Lerp(color, Color.black, 0.35f);
            var sombra = MallasProcedurales.Crear("Sombra", MallasProcedurales.RectanguloRedondeado(0.20f, 0.065f, 0.02f), MallasProcedurales.Plano(oscuro), etiqueta);
            sombra.transform.localPosition = new Vector3(0.004f, -0.004f, 0.002f);
            MallasProcedurales.Crear("Fondo", MallasProcedurales.RectanguloRedondeado(0.20f, 0.065f, 0.02f), MallasProcedurales.Plano(color), etiqueta);
            var txt = new GameObject("Texto");
            txt.transform.SetParent(etiqueta, false);
            txt.transform.localPosition = new Vector3(0f, 0f, -0.003f);
            var tmp = txt.AddComponent<TMPro.TextMeshPro>();
            tmp.text = textoEtiqueta;
            tmp.rectTransform.sizeDelta = new Vector2(0.185f, 0.055f);
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0.05f;
            tmp.fontSizeMax = 0.4f;
            tmp.fontStyle = TMPro.FontStyles.Bold;
            tmp.color = Color.white;

            // Aro que late alrededor del globo: "toque aqui".
            var aro = MallasProcedurales.Crear("Aro", MallasProcedurales.Anillo(0.125f, 0.138f), MallasProcedurales.Plano(Color.Lerp(color, Color.white, 0.45f)), raiz.transform);

            var col = raiz.AddComponent<SphereCollider>();
            col.radius = 0.11f;
            col.isTrigger = true;

            globo.cuerpo = cuerpo;
            globo.aro = aro.transform;
            globo.etiqueta = etiqueta;
            globo.escala = Vector3.one;
            cuerpo.localScale = Vector3.zero;
            etiqueta.localScale = Vector3.zero;
            aro.transform.localScale = Vector3.zero;
            globo.StartCoroutine(globo.Aparecer());
            return globo;
        }

        static void Pieza(PrimitiveType tipo, Transform padre, Vector3 pos, Vector3 escala, Material mat)
        {
            var go = GameObject.CreatePrimitive(tipo);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(padre, false);
            go.transform.localPosition = pos;
            go.transform.localScale = escala;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        IEnumerator Aparecer()
        {
            for (float t = 0f; t < 0.4f; t += Time.deltaTime)
            {
                float x = t / 0.4f;
                float k = 1f + 2.2f * Mathf.Pow(x - 1f, 3f) + 1.2f * Mathf.Pow(x - 1f, 2f);
                cuerpo.localScale = escala * k;
                if (etiqueta != null) etiqueta.localScale = Vector3.one * k;
                yield return null;
            }
            cuerpo.localScale = escala;
            if (etiqueta != null) etiqueta.localScale = Vector3.one;
        }

        void Update()
        {
            // Flota suavemente.
            fase += Time.deltaTime;
            if (cuerpo != null)
            {
                cuerpo.localPosition = Vector3.up * Mathf.Sin(fase * 2f) * 0.012f;
                if (!sacudiendo) cuerpo.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(fase * 1.3f) * 4f);
            }
            // El aro y la etiqueta miran al residente; el aro late suavemente.
            if (camara == null && Camera.main != null) camara = Camera.main.transform;
            if (camara != null)
            {
                var mirar = Quaternion.LookRotation(transform.position - camara.position, Vector3.up);
                if (aro != null) aro.rotation = mirar;
                if (etiqueta != null) etiqueta.rotation = mirar;
            }
            if (aro != null && !Tocado)
            {
                float pulso = Mathf.Repeat(fase * 0.8f, 1f);
                aro.localScale = Vector3.one * Mathf.Lerp(0.9f, 1.2f, pulso) * (cuerpo != null ? cuerpo.localScale.x : 1f);
            }
            if (Tocado) return;

            foreach (var mano in ManoVirtual.Todas)
            {
                if (mano == null || !mano.isActiveAndEnabled) continue;
                bool dentro = Cerca(mano.PuntaDedo, 0f) || Cerca(mano.Palma, 0f);
                if (!manosListas.Contains(mano))
                {
                    if (!(Cerca(mano.PuntaDedo, 0.03f) || Cerca(mano.Palma, 0.03f))) manosListas.Add(mano);
                    continue;
                }
                bool estabaDentro = manosDentro.Contains(mano);
                if (dentro && !estabaDentro)
                {
                    manosDentro.Add(mano);
                    Tocar(mano.esIzquierda);
                    mano.Vibrar(mano.esIzquierda == esIzquierdo ? 0.6f : 0.2f, 0.15f);
                    if (Tocado) return;
                }
                else if (!dentro && estabaDentro && !(Cerca(mano.PuntaDedo, 0.03f) || Cerca(mano.Palma, 0.03f)))
                {
                    manosDentro.Remove(mano);
                }
            }
        }

        bool Cerca(Transform punto, float margen) => punto != null && Vector3.Distance(punto.position, transform.position) < radioToque + margen;

        /// <summary>Marca el globo como tocado (tambien lo usa la prueba con mouse).</summary>
        public void Tocar(bool manoIzquierda)
        {
            // Recien aparecido no cuenta (evita toques arrastrados del globo anterior).
            if (Tocado || fase < 0.6f) return;
            if (manoIzquierda != esIzquierdo)
            {
                // Mano equivocada: el globo no se revienta, solo se sacude.
                IntentosManoEquivocada++;
                StartCoroutine(Sacudir());
                Debug.Log("[Exergame] Practica: globo " + (esIzquierdo ? "izquierdo" : "derecho") + " tocado con la mano equivocada");
                return;
            }
            Tocado = true;
            ConManoIzquierda = manoIzquierda;
        }

        bool sacudiendo;

        IEnumerator Sacudir()
        {
            sacudiendo = true;
            for (float t = 0f; t < 0.5f && cuerpo != null; t += Time.deltaTime)
            {
                cuerpo.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 40f) * 14f * (1f - t / 0.5f));
                yield return null;
            }
            sacudiendo = false;
        }

        /// <summary>El globo se achica y desaparece.</summary>
        public IEnumerator Reventar()
        {
            var inicio = cuerpo != null ? cuerpo.localScale : Vector3.one;
            for (float t = 0f; t < 0.25f && cuerpo != null; t += Time.deltaTime)
            {
                cuerpo.localScale = inicio * (1f + t * 1.2f) * (1f - t / 0.25f);
                if (etiqueta != null) etiqueta.localScale = Vector3.one * (1f - t / 0.25f);
                if (aro != null) aro.localScale = Vector3.zero;
                yield return null;
            }
            Destroy(gameObject);
        }

    }
}
