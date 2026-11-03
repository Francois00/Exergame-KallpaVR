using System.Collections;
using UnityEngine;

namespace Exergame
{
    /// <summary>
    /// Tira de estrellas delante del tablero: una por ensayo. Se ilumina en dorado con cada acierto,
    /// asi el residente ve su avance sin numeros. Las que no fueron acierto quedan blancas (sin marca de error).
    /// </summary>
    public class ProgresoLogros : MonoBehaviour
    {
        public float ancho = 0.54f;
        public float radio = 0.015f;

        Transform[] estrellas;
        Renderer[] rend;
        Material matPendiente, matHecha, matDorada;
        Transform camara;

        public static ProgresoLogros Crear(Vector3 centro)
        {
            var go = new GameObject("ProgresoLogros");
            go.transform.position = centro;
            return go.AddComponent<ProgresoLogros>();
        }

        public void Iniciar(int total)
        {
            Limpiar();
            if (matPendiente == null)
            {
                matPendiente = MallasProcedurales.Plano(new Color(0.55f, 0.53f, 0.50f));
                matHecha = MallasProcedurales.Plano(new Color(0.97f, 0.96f, 0.92f));
                matDorada = MallasProcedurales.Plano(new Color(1f, 0.78f, 0.12f));
            }
            total = Mathf.Max(1, total);
            estrellas = new Transform[total];
            rend = new Renderer[total];
            float paso = total > 1 ? ancho / (total - 1) : 0f;
            var malla = MallasProcedurales.Estrella(radio, radio * 0.45f);
            for (int i = 0; i < total; i++)
            {
                var go = MallasProcedurales.Crear("Estrella" + (i + 1), malla, matPendiente, transform);
                go.transform.localPosition = new Vector3(-ancho / 2f + i * paso, 0f, 0f);
                go.transform.localScale = Vector3.one * 0.75f;
                estrellas[i] = go.transform;
                rend[i] = go.GetComponent<Renderer>();
            }
            gameObject.SetActive(true);
        }

        public void Marcar(int indice, bool acierto)
        {
            if (estrellas == null || indice < 0 || indice >= estrellas.Length) return;
            rend[indice].sharedMaterial = acierto ? matDorada : matHecha;
            StartCoroutine(Saltar(estrellas[indice], acierto ? 1.25f : 0.9f));
        }

        IEnumerator Saltar(Transform t, float final)
        {
            for (float k = 0f; k < 1f; k += Time.deltaTime / 0.35f)
            {
                t.localScale = Vector3.one * Mathf.Lerp(0.75f, final, k) * (1f + Mathf.Sin(k * Mathf.PI) * 0.6f);
                yield return null;
            }
            t.localScale = Vector3.one * final;
        }

        public void Limpiar()
        {
            StopAllCoroutines();
            if (estrellas != null) foreach (var e in estrellas) if (e != null) Destroy(e.gameObject);
            estrellas = null;
        }

        void Update()
        {
            if (estrellas == null) return;
            if (camara == null && Camera.main != null) camara = Camera.main.transform;
            if (camara == null) return;
            // Las estrellas miran al residente.
            var hacia = transform.position - camara.position;
            if (hacia.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(hacia, Vector3.up);
        }

        void OnDestroy()
        {
            foreach (var m in new[] { matPendiente, matHecha, matDorada }) if (m != null) Destroy(m);
        }
    }
}
