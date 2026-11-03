using UnityEngine;

namespace Exergame
{
    /// <summary>
    /// Mueve la boca del personaje al ritmo de la voz: lee el volumen del audio que suena
    /// y abre o cierra una boca (un ovalo oscuro pegado a la cara). La cabeza acompana
    /// con un movimiento suave, como al hablar.
    /// </summary>
    public class BocaPresentador : MonoBehaviour
    {
        public AudioSource voz;
        [Tooltip("Ovalo oscuro de la boca (hijo del hueso de la cabeza).")]
        public Transform boca;
        public Transform cabeza;
        [Tooltip("Volumen (RMS) desde el que la boca empieza a abrirse.")]
        public float umbral = 0.012f;
        [Tooltip("Volumen (RMS) con el que la boca queda abierta del todo.")]
        public float volumenMaximo = 0.11f;
        [Tooltip("Grados que asiente la cabeza con cada silaba.")]
        public float gradosCabeza = 2.5f;

        readonly float[] muestras = new float[256];
        Vector3 escalaBoca;
        Renderer rendererBoca;
        float apertura;

        void Awake()
        {
            if (boca != null)
            {
                escalaBoca = boca.localScale;
                rendererBoca = boca.GetComponentInChildren<Renderer>(true);
            }
        }

        void LateUpdate()
        {
            float objetivo = 0f;
            if (voz != null && voz.isPlaying)
            {
                voz.GetOutputData(muestras, 0);
                float suma = 0f;
                for (int i = 0; i < muestras.Length; i++) suma += muestras[i] * muestras[i];
                float rms = Mathf.Sqrt(suma / muestras.Length);
                objetivo = Mathf.Clamp01((rms - umbral) / Mathf.Max(0.001f, volumenMaximo - umbral));
            }

            // Abre rapido y cierra un poco mas lento: se ve natural.
            float velocidad = objetivo > apertura ? 28f : 16f;
            apertura = Mathf.Lerp(apertura, objetivo, 1f - Mathf.Exp(-velocidad * Time.deltaTime));

            if (boca != null)
            {
                // El pivote esta en la sonrisa: al escalar en Y la boca se abre hacia abajo.
                boca.localScale = new Vector3(escalaBoca.x * (0.8f + 0.2f * apertura),
                                              escalaBoca.y * Mathf.Lerp(0.1f, 1f, apertura),
                                              escalaBoca.z);
                if (rendererBoca != null) rendererBoca.enabled = apertura > 0.05f;
            }

            if (cabeza != null && gradosCabeza > 0f)
                cabeza.rotation = Quaternion.AngleAxis(apertura * gradosCabeza, transform.right) * cabeza.rotation;
        }
    }
}
