using System.Collections;
using TMPro;
using UnityEngine;

namespace Exergame
{
    /// <summary>
    /// Felicitaciones para motivar al residente:
    /// - en cada acierto: sonido alegre, confeti y una frase (varia para no repetirse);
    /// - cada 3 aciertos seguidos: celebracion un poco mas grande con aplausos;
    /// - si se equivoca: un animo suave ("¡Casi! ¡Siga asi!"), sin sonido de error;
    /// - al terminar: "¡Felicitaciones!" con fanfarria, aplausos y fuegos de confeti.
    /// Todo dura poco y no tapa el tablero; el tiempo de reaccion se mide antes, al tocar.
    /// </summary>
    public static class Felicitacion
    {
        static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int IdColor = Shader.PropertyToID("_Color");
        static AudioClip sonido;

        static readonly Color[] colores =
        {
            new Color(1f, 0.82f, 0.25f), new Color(1f, 1f, 1f), new Color(1f, 0.55f, 0.75f),
            new Color(0.55f, 0.88f, 1f), new Color(0.75f, 0.55f, 1f)
        };

        public static readonly string[] Frases = { "¡Muy bien!", "¡Excelente!", "¡Bravo!", "¡Así se hace!", "¡Perfecto!", "¡Qué bien!" };
        static readonly string[] Animos = { "¡Casi!\n¡Siga así!", "¡Ánimo!\nUsted puede", "¡Casi!\nIntentemos otra vez" };
        static int ultimaFrase = -1, ultimoAnimo = -1;
        static readonly Color verde = new Color(0.12f, 0.6f, 0.22f);
        static readonly Color dorado = new Color(0.95f, 0.62f, 0.05f);

        public static string FraseAlAzar()
        {
            int i;
            do i = Random.Range(0, Frases.Length); while (i == ultimaFrase && Frases.Length > 1);
            ultimaFrase = i;
            return Frases[i];
        }

        public static void Mostrar(MonoBehaviour anfitrion, Vector3 posicion, Material materialBase, AudioSource fuente, float volumen, string frase = null)
        {
            if (fuente != null) fuente.PlayOneShot(Sonido(), volumen);
            if (anfitrion == null) return;
            anfitrion.StartCoroutine(Confeti(posicion, materialBase, 28, 1f));
            anfitrion.StartCoroutine(Texto(frase ?? FraseAlAzar(), PosicionTexto, verde, 1f, 1.3f));
        }

        /// <summary>Animo suave cuando se equivoca (sin sonido de error, sin castigo).</summary>
        public static void Animo(MonoBehaviour anfitrion)
        {
            if (anfitrion == null) return;
            int i;
            do i = Random.Range(0, Animos.Length); while (i == ultimoAnimo && Animos.Length > 1);
            ultimoAnimo = i;
            anfitrion.StartCoroutine(Texto(Animos[i], PosicionTexto, new Color(0.18f, 0.38f, 0.75f), 0.8f, 1.4f));
        }

        /// <summary>Racha de aciertos seguidos: celebracion mas grande.</summary>
        public static void Racha(MonoBehaviour anfitrion, int seguidos, Material materialBase, AudioSource fuente)
        {
            if (anfitrion == null) return;
            if (fuente != null) fuente.PlayOneShot(SonidosTema.Aplausos(), 0.55f);
            anfitrion.StartCoroutine(Confeti(PosicionTexto + new Vector3(-0.22f, -0.05f, 0f), materialBase, 30, 1.2f));
            anfitrion.StartCoroutine(Confeti(PosicionTexto + new Vector3(0.22f, -0.05f, 0f), materialBase, 30, 1.2f));
            anfitrion.StartCoroutine(Texto($"¡{seguidos} seguidos!\n¡Fantástico!", PosicionTexto + Vector3.up * 0.09f, dorado, 1.15f, 1.8f, 0.4f));
        }

        /// <summary>Final de la sesion: fanfarria, aplausos, fuegos de confeti y el resultado en positivo.</summary>
        public static IEnumerator Final(MonoBehaviour anfitrion, int aciertos, int total, Material materialBase, AudioSource fuente)
        {
            if (anfitrion == null) yield break;
            if (fuente != null)
            {
                fuente.PlayOneShot(SonidosTema.Fanfarria(), 0.8f);
                fuente.PlayOneShot(SonidosTema.Aplausos(), 0.5f);
            }
            string texto = total > 0 ? $"¡Felicitaciones!\nHoy acertó {aciertos} de {total}" : "¡Felicitaciones!";
            anfitrion.StartCoroutine(Texto(texto, PosicionTexto + new Vector3(0f, -0.05f, 0.02f), dorado, 1.6f, 4.2f, 0.25f));
            var puntos = new[] { new Vector3(-0.35f, 1.45f, 1.0f), new Vector3(0.35f, 1.5f, 1.0f), new Vector3(0f, 1.6f, 1.1f), new Vector3(-0.2f, 1.55f, 0.9f), new Vector3(0.22f, 1.42f, 0.9f) };
            foreach (var p in puntos)
            {
                anfitrion.StartCoroutine(Confeti(p, materialBase, 36, 1.5f, true));
                yield return new WaitForSeconds(0.45f);
            }
            yield return new WaitForSeconds(2.2f);
        }

        // Encima del tablero, un poco hacia la persona. Desplazamiento: lo pone la sesion si la maqueta esta mas lejos.
        public static Vector3 Desplazamiento;
        static Vector3 PosicionTexto => new Vector3(0f, 1.33f, 0.60f) + Desplazamiento;

        // Arpegio ascendente do-mi-sol-do, suave.
        static AudioClip Sonido()
        {
            if (sonido != null) return sonido;
            const int fm = 44100;
            float[] notas = { 523.25f, 659.25f, 783.99f, 1046.5f };
            const float paso = 0.11f, cola = 0.45f;
            int muestras = Mathf.CeilToInt(fm * (paso * (notas.Length - 1) + cola));
            var datos = new float[muestras];
            for (int n = 0; n < notas.Length; n++)
            {
                int inicio = Mathf.RoundToInt(fm * paso * n);
                for (int i = inicio; i < muestras; i++)
                {
                    float t = (i - inicio) / (float)fm;
                    float env = Mathf.Clamp01(t / 0.006f) * Mathf.Exp(-t * 6.5f);
                    float onda = Mathf.Sin(2f * Mathf.PI * notas[n] * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * notas[n] * 2f * t);
                    datos[i] += env * onda * 0.22f;
                }
            }
            sonido = AudioClip.Create("Felicitacion", muestras, 1, fm, false);
            sonido.SetData(datos, 0);
            return sonido;
        }

        static IEnumerator Confeti(Vector3 origen, Material materialBase, int cantidad, float vida, bool fuegos = false)
        {
            var piezas = new Transform[cantidad];
            var velocidades = new Vector3[cantidad];
            var giros = new Vector3[cantidad];
            var materiales = new Material[cantidad];
            var escala = Vector3.one * 0.022f;

            for (int i = 0; i < cantidad; i++)
            {
                var go = GameObject.CreatePrimitive(i % 2 == 0 ? PrimitiveType.Cube : PrimitiveType.Sphere);
                go.name = "Confeti";
                Object.Destroy(go.GetComponent<Collider>());
                var r = go.GetComponent<Renderer>();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var c = colores[i % colores.Length];
                materiales[i] = materialBase != null ? new Material(materialBase) : MallasProcedurales.Plano(c);
                materiales[i].SetColor(IdBaseColor, c);
                materiales[i].SetColor(IdColor, c);
                r.sharedMaterial = materiales[i];
                piezas[i] = go.transform;
                piezas[i].position = origen;
                piezas[i].localScale = escala;
                piezas[i].rotation = Random.rotation;
                // Fuegos: se abren en todas direcciones. Confeti: hacia arriba y hacia la persona (-Z).
                velocidades[i] = fuegos
                    ? Random.onUnitSphere * Random.Range(0.6f, 1.0f) + Vector3.up * 0.3f
                    : new Vector3(Random.Range(-0.7f, 0.7f), Random.Range(0.9f, 1.6f), Random.Range(-0.8f, -0.2f));
                giros[i] = Random.insideUnitSphere * 540f;
            }

            float t = 0f;
            while (t < vida)
            {
                float dt = Time.deltaTime;
                t += dt;
                float k = t / vida;
                for (int i = 0; i < cantidad; i++)
                {
                    velocidades[i] += Vector3.down * (fuegos ? 1.2f : 2.2f) * dt;
                    velocidades[i] *= fuegos ? 0.985f : 1f;
                    piezas[i].position += velocidades[i] * dt;
                    piezas[i].Rotate(giros[i] * dt, Space.Self);
                    piezas[i].localScale = escala * (k < 0.65f ? 1f : Mathf.Lerp(1f, 0f, (k - 0.65f) / 0.35f));
                }
                yield return null;
            }

            for (int i = 0; i < cantidad; i++)
            {
                if (piezas[i] != null) Object.Destroy(piezas[i].gameObject);
                if (materiales[i] != null) Object.Destroy(materiales[i]);
            }
        }

        static IEnumerator Texto(string texto, Vector3 inicio, Color color, float escala, float dur, float retraso = 0f)
        {
            if (retraso > 0f) yield return new WaitForSeconds(retraso);
            var go = new GameObject("Felicitacion");
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = texto;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(0.6f, 0.16f);
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0.1f;
            tmp.fontSizeMax = 1.2f;
            tmp.color = color;
            tmp.outlineColor = new Color32(255, 255, 255, 255);
            tmp.outlineWidth = 0.25f;
            go.transform.position = inicio;

            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                float pop = k < 0.12f ? Mathf.SmoothStep(0.4f, 1.1f, k / 0.12f) : Mathf.Lerp(1.1f, 1f, Mathf.Clamp01((k - 0.12f) / 0.12f));
                go.transform.localScale = Vector3.one * pop * escala;
                go.transform.position = inicio + Vector3.up * 0.05f * k;
                var c = tmp.color;
                c.a = k < 0.75f ? 1f : Mathf.Lerp(1f, 0f, (k - 0.75f) / 0.25f);
                tmp.color = c;
                yield return null;
            }
            Object.Destroy(go);
        }
    }
}
