using System.Collections.Generic;
using UnityEngine;

namespace Exergame
{
    public static class GeneradorTonos
    {
        static readonly Dictionary<int, AudioClip> cache = new Dictionary<int, AudioClip>();
        static AudioClip click;

        public static AudioClip Click()
        {
            if (click != null) return click;
            const int frecuenciaMuestreo = 44100;
            int muestras = Mathf.CeilToInt(frecuenciaMuestreo * 0.06f);
            var datos = new float[muestras];
            var azar = new System.Random(7);
            for (int i = 0; i < muestras; i++)
            {
                float t = i / (float)frecuenciaMuestreo;
                float caida = Mathf.Exp(-t * 90f);
                float onda = Mathf.Sin(2f * Mathf.PI * 1400f * t) * 0.6f + ((float)azar.NextDouble() * 2f - 1f) * 0.25f;
                datos[i] = caida * onda * 0.5f;
            }
            click = AudioClip.Create("ClickNeutro", muestras, 1, frecuenciaMuestreo, false);
            click.SetData(datos, 0);
            return click;
        }

        public static AudioClip Crear(float frecuencia, float duracion)
        {
            int clave = Mathf.RoundToInt(frecuencia * 10f) * 1000 + Mathf.RoundToInt(duracion * 100f);
            if (cache.TryGetValue(clave, out var existente) && existente != null) return existente;

            const int frecuenciaMuestreo = 44100;
            int muestras = Mathf.CeilToInt(frecuenciaMuestreo * duracion);
            var datos = new float[muestras];
            for (int i = 0; i < muestras; i++)
            {
                float t = i / (float)frecuenciaMuestreo;
                float ataque = Mathf.Clamp01(t / 0.008f);
                float caida = Mathf.Exp(-t * 7f);
                float onda = Mathf.Sin(2f * Mathf.PI * frecuencia * t)
                           + 0.25f * Mathf.Sin(2f * Mathf.PI * frecuencia * 2f * t)
                           + 0.1f * Mathf.Sin(2f * Mathf.PI * frecuencia * 3f * t);
                datos[i] = ataque * caida * onda * 0.45f;
            }

            var clip = AudioClip.Create("Tono_" + frecuencia.ToString("0"), muestras, 1, frecuenciaMuestreo, false);
            clip.SetData(datos, 0);
            cache[clave] = clip;
            return clip;
        }
    }
}
