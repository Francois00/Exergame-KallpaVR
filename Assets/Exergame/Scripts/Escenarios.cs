using UnityEngine;

namespace Exergame
{
    /// <summary>
    /// Escenario (mapa) que elige el evaluador. La tarea, el tablero 2x2, el orden de colores
    /// y la regla son iguales en todos: solo cambian el objeto que se toca, el fondo y la recompensa.
    /// </summary>
    public enum Escenario { Clasico, Globos, Tambores }

    public static class Escenarios
    {
        public static Escenario Leer(string texto, Escenario siFalla = Escenario.Clasico)
        {
            if (string.IsNullOrEmpty(texto)) return siFalla;
            switch (texto.Trim().ToLowerInvariant())
            {
                case "globos": case "globo": case "plaza": return Escenario.Globos;
                case "tambores": case "tambor": case "jardin": return Escenario.Tambores;
                case "clasico": case "sala": return Escenario.Clasico;
                default: return siFalla;
            }
        }

        public static string Nombre(Escenario e)
        {
            switch (e)
            {
                case Escenario.Globos: return "Globos en la plaza";
                case Escenario.Tambores: return "Tambores en el jardín";
                default: return "Clásico (sala)";
            }
        }
    }

    /// <summary>Sonidos generados por codigo (no hacen falta archivos de audio).</summary>
    public static class SonidosTema
    {
        const int FM = 44100;
        static AudioClip pop, boing, apagado, fanfarria, aplausos;
        static readonly AudioClip[] tambores = new AudioClip[4];

        static AudioClip Crear(string nombre, float[] datos)
        {
            var c = AudioClip.Create(nombre, datos.Length, 1, FM, false);
            c.SetData(datos, 0);
            return c;
        }

        /// <summary>"Pop" suave y corto (no asusta): golpe grave + soplo de aire.</summary>
        public static AudioClip Pop()
        {
            if (pop != null) return pop;
            int n = (int)(FM * 0.18f);
            var d = new float[n];
            var azar = new System.Random(3);
            float filtro = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)FM;
                float ruido = (float)azar.NextDouble() * 2f - 1f;
                filtro += (ruido - filtro) * 0.25f;   // paso bajo: menos chillon
                float golpe = Mathf.Sin(2f * Mathf.PI * (170f - 60f * t * 6f) * t) * Mathf.Exp(-t * 35f);
                d[i] = (filtro * 0.9f * Mathf.Exp(-t * 55f) + golpe * 0.55f) * Mathf.Clamp01(t / 0.002f) * 0.6f;
            }
            return pop = Crear("Pop", d);
        }

        /// <summary>"Boing" de goma: cuando toca otro objeto (sin castigo, gracioso).</summary>
        public static AudioClip Boing()
        {
            if (boing != null) return boing;
            int n = (int)(FM * 0.45f);
            var d = new float[n];
            float fase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)FM;
                float f = 190f * (1f + 0.28f * Mathf.Sin(2f * Mathf.PI * 9f * t) * Mathf.Exp(-t * 4f));
                fase += 2f * Mathf.PI * f / FM;
                d[i] = Mathf.Sin(fase) * Mathf.Exp(-t * 6f) * Mathf.Clamp01(t / 0.01f) * 0.32f;
            }
            return boing = Crear("Boing", d);
        }

        // Do-Mi-Sol-Do: los cuatro tambores juntos suenan armoniosos.
        static readonly float[] notas = { 130.81f, 164.81f, 196.00f, 261.63f };

        /// <summary>Tambor afinado segun el color.</summary>
        public static AudioClip Tambor(ColorJuego c)
        {
            int k = (int)c;
            if (tambores[k] != null) return tambores[k];
            float f0 = notas[k];
            int n = (int)(FM * 0.7f);
            var d = new float[n];
            var azar = new System.Random(11 + k);
            float fase = 0f, fase2 = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)FM;
                float f = f0 * (1f + 0.45f * Mathf.Exp(-t * 28f));
                fase += 2f * Mathf.PI * f / FM;
                fase2 += 2f * Mathf.PI * f * 1.52f / FM;
                float cuerpo = Mathf.Sin(fase) * Mathf.Exp(-t * 5.5f) + 0.3f * Mathf.Sin(fase2) * Mathf.Exp(-t * 9f);
                float golpe = ((float)azar.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 90f) * 0.35f;
                d[i] = (cuerpo * 0.5f + golpe) * Mathf.Clamp01(t / 0.002f);
            }
            return tambores[k] = Crear("Tambor_" + c, d);
        }

        /// <summary>Golpe apagado y corto (cuando no era ese tambor).</summary>
        public static AudioClip TamborApagado()
        {
            if (apagado != null) return apagado;
            int n = (int)(FM * 0.25f);
            var d = new float[n];
            float fase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)FM;
                fase += 2f * Mathf.PI * 92f * (1f + 0.3f * Mathf.Exp(-t * 40f)) / FM;
                d[i] = Mathf.Sin(fase) * Mathf.Exp(-t * 20f) * Mathf.Clamp01(t / 0.002f) * 0.35f;
            }
            return apagado = Crear("TamborApagado", d);
        }

        /// <summary>Fanfarria corta para el final de la sesion.</summary>
        public static AudioClip Fanfarria()
        {
            if (fanfarria != null) return fanfarria;
            float[] f = { 523.25f, 659.25f, 783.99f, 1046.5f, 783.99f, 1046.5f };
            float[] ini = { 0f, 0.14f, 0.28f, 0.42f, 0.70f, 0.84f };
            int n = (int)(FM * 2.2f);
            var d = new float[n];
            for (int k = 0; k < f.Length; k++)
            {
                int i0 = (int)(ini[k] * FM);
                float largo = k == f.Length - 1 ? 1.3f : 0.4f;
                for (int i = i0; i < n && i < i0 + (int)(largo * FM); i++)
                {
                    float t = (i - i0) / (float)FM;
                    float env = Mathf.Clamp01(t / 0.01f) * Mathf.Exp(-t * (k == f.Length - 1 ? 2.2f : 6f));
                    d[i] += env * (Mathf.Sin(2f * Mathf.PI * f[k] * t) + 0.35f * Mathf.Sin(2f * Mathf.PI * f[k] * 2f * t) + 0.12f * Mathf.Sin(2f * Mathf.PI * f[k] * 3f * t)) * 0.17f;
                }
            }
            return fanfarria = Crear("Fanfarria", d);
        }

        /// <summary>Aplausos suaves.</summary>
        public static AudioClip Aplausos()
        {
            if (aplausos != null) return aplausos;
            int n = (int)(FM * 1.8f);
            var d = new float[n];
            var azar = new System.Random(5);
            for (int c = 0; c < 70; c++)
            {
                int i0 = (int)(azar.NextDouble() * (n - FM * 0.05f) * (0.3 + 0.7 * azar.NextDouble()));
                float amp = 0.10f + (float)azar.NextDouble() * 0.08f;
                float filtro = 0f;
                for (int i = i0; i < n && i < i0 + (int)(FM * 0.04f); i++)
                {
                    float t = (i - i0) / (float)FM;
                    float r = (float)azar.NextDouble() * 2f - 1f;
                    filtro += (r - filtro) * 0.6f;
                    d[i] += filtro * Mathf.Exp(-t * 110f) * amp;
                }
            }
            for (int i = 0; i < n; i++) d[i] *= Mathf.Clamp01((n - i) / (FM * 0.6f));
            return aplausos = Crear("Aplausos", d);
        }
    }
}
