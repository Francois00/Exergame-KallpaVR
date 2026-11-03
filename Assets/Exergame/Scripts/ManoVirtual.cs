using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace Exergame
{
    [DefaultExecutionOrder(100)]
    public class ManoVirtual : MonoBehaviour
    {
        public bool esIzquierda;
        public Material materialMano;
        public bool ocultarControl = true;
        public bool alinearConPoke = true;
        public float velocidadCierre = 8f;

        [Header("Ayuda para llegar a la maqueta (segun el modo)")]
        [Tooltip("Movilidad completa: cuanto se adelanta la mano que se ve (palo invisible), en metros.")]
        public float adelantoCompleta = 0.15f;
        [Tooltip("Con limitaciones: cuanto llega la punta del palo por delante de la punta del dedo, en metros.")]
        public float largoPalo = 0.30f;

        /// <summary>Todas las manos activas (las usa BotonExergame para detectar el toque).</summary>
        public static readonly List<ManoVirtual> Todas = new List<ManoVirtual>();

        /// <summary>Modo de alcance actual (lo pone la sesion antes de empezar).</summary>
        public static ModoAlcance Modo { get; private set; } = ModoAlcance.Proximidad;

        public static void FijarModo(ModoAlcance modo)
        {
            Modo = modo;
            foreach (var m in FindObjectsOfType<ManoVirtual>(true)) m.AplicarModo();
        }

        bool ConPalo => Modo == ModoAlcance.Limitaciones && puntaPalo != null;
        /// <summary>Punta del dedo indice (con limitaciones: la punta del palo).</summary>
        public Transform PuntaDedo => ConPalo ? puntaPalo : indicePunta;
        /// <summary>Centro de la palma (con limitaciones: la punta del palo, la mano no toca).</summary>
        public Transform Palma => ConPalo ? puntaPalo : palma;

        XRBaseController control;
        Transform raiz, palma;
        Transform indiceBase, indiceMedio, indicePunta;
        Transform[] otrosBase = new Transform[3];
        Transform[] otrosMedio = new Transform[3];
        Transform pulgarBase;
        float cierre, cierreIndice;
        Transform palo, puntaPalo;
        Vector3 raizBase;
        Transform puntoPoke;
        Vector3 pokeBase;
        bool listo;

        void OnEnable() { if (!Todas.Contains(this)) Todas.Add(this); }
        void OnDisable() { Todas.Remove(this); }

        public void Vibrar(float amplitud, float duracion)
        {
            if (control != null) control.SendHapticImpulse(amplitud, duracion);
        }

        void Start()
        {
            control = GetComponent<XRBaseController>();
            if (control != null && ocultarControl) control.hideControllerModel = true;
            Construir();
            AplicarPose(0f, 0f);
            if (alinearConPoke) AlinearConPoke();
            raizBase = raiz.localPosition;
            if (puntoPoke != null) pokeBase = puntoPoke.localPosition;
            listo = true;
            AplicarModo();
        }

        // Completa: toda la mano se adelanta (no se ve palo). Limitaciones: la mano queda en su sitio
        // y se ve el palo; toca la punta. El punto de empuje del XR (poke) se lleva al punto que toca.
        void AplicarModo()
        {
            if (!listo) return;
            raiz.localPosition = raizBase + (Modo == ModoAlcance.Completa ? Vector3.forward * adelantoCompleta : Vector3.zero);
            if (palo != null) palo.gameObject.SetActive(Modo == ModoAlcance.Limitaciones);
            if (puntoPoke != null)
            {
                puntoPoke.localPosition = pokeBase;
                if (Modo != ModoAlcance.Proximidad) puntoPoke.position = PuntaDedo.position;
            }
        }

        void Update()
        {
            if (raiz == null) return;
            float agarre = control != null ? control.selectInteractionState.value : 0f;
            float gatillo = control != null ? control.activateInteractionState.value : 0f;
            if (ConPalo) agarre = gatillo = 1f;   // la mano agarra el palo
            cierre = Mathf.MoveTowards(cierre, agarre, Time.deltaTime * velocidadCierre);
            cierreIndice = Mathf.MoveTowards(cierreIndice, gatillo, Time.deltaTime * velocidadCierre);
            AplicarPose(cierre, cierreIndice);
        }

        void AplicarPose(float c, float ci)
        {
            float b = Mathf.Lerp(20f, 80f, c);
            float m = Mathf.Lerp(25f, 95f, c);
            for (int i = 0; i < 3; i++)
            {
                otrosBase[i].localRotation = Quaternion.Euler(b, 0f, 0f);
                otrosMedio[i].localRotation = Quaternion.Euler(m, 0f, 0f);
            }
            indiceBase.localRotation = Quaternion.Euler(Mathf.Lerp(4f, 50f, ci), 0f, 0f);
            indiceMedio.localRotation = Quaternion.Euler(Mathf.Lerp(6f, 65f, ci), 0f, 0f);
            float lado = esIzquierda ? -1f : 1f;
            pulgarBase.localRotation = Quaternion.Euler(Mathf.Lerp(10f, 35f, c), -35f * lado, Mathf.Lerp(0f, -25f, c) * lado);
        }

        void Construir()
        {
            float lado = esIzquierda ? -1f : 1f;
            raiz = new GameObject("ManoVirtual").transform;
            raiz.SetParent(transform, false);

            Parte(PrimitiveType.Sphere, raiz, new Vector3(0f, 0f, -0.048f), new Vector3(0.085f, 0.032f, 0.1f), Quaternion.identity);
            palma = new GameObject("Palma").transform;
            palma.SetParent(raiz, false);
            palma.localPosition = new Vector3(0f, 0f, -0.03f);
            Parte(PrimitiveType.Capsule, raiz, new Vector3(0f, -0.002f, -0.11f), new Vector3(0.055f, 0.03f, 0.04f), Quaternion.Euler(90f, 0f, 0f));

            indiceBase = Dedo("Indice", new Vector3(-0.028f * lado, 0f, 0f), 0.045f, 0.04f, 0.0095f, out indiceMedio, out indicePunta);
            otrosBase[0] = Dedo("Medio", new Vector3(-0.009f * lado, 0f, 0.004f), 0.05f, 0.042f, 0.0095f, out otrosMedio[0], out _);
            otrosBase[1] = Dedo("Anular", new Vector3(0.011f * lado, 0f, 0f), 0.046f, 0.04f, 0.009f, out otrosMedio[1], out _);
            otrosBase[2] = Dedo("Menique", new Vector3(0.03f * lado, -0.002f, -0.008f), 0.036f, 0.032f, 0.008f, out otrosMedio[2], out _);

            pulgarBase = new GameObject("Pulgar").transform;
            pulgarBase.SetParent(raiz, false);
            pulgarBase.localPosition = new Vector3(-0.04f * lado, -0.008f, -0.06f);
            Segmento(pulgarBase, 0.04f, 0.011f);
            var pulgarPunta = new GameObject("Pulgar_2").transform;
            pulgarPunta.SetParent(pulgarBase, false);
            pulgarPunta.localPosition = new Vector3(0f, 0f, 0.04f);
            pulgarPunta.localRotation = Quaternion.Euler(10f, 0f, 0f);
            Segmento(pulgarPunta, 0.03f, 0.0105f);
            ConstruirPalo();
        }

        // Palo de madera que sale del puno hacia adelante, con una punta clara y redonda que es la que toca.
        void ConstruirPalo()
        {
            palo = new GameObject("Palo").transform;
            palo.SetParent(raiz, false);
            float desde = -0.07f;                       // empieza detras del puno, se ve agarrado
            float hasta = 0.09f + largoPalo;            // la punta del dedo esta a ~9 cm de la raiz
            float largo = hasta - desde;
            var madera = Material(new Color(0.62f, 0.44f, 0.26f));
            var cuerpo = Pieza(PrimitiveType.Cylinder, palo, new Vector3(0f, -0.012f, desde + largo * 0.5f),
                new Vector3(0.022f, largo * 0.5f, 0.022f), Quaternion.Euler(90f, 0f, 0f));
            cuerpo.GetComponent<Renderer>().sharedMaterial = madera;
            var bola = Pieza(PrimitiveType.Sphere, palo, new Vector3(0f, -0.012f, hasta), Vector3.one * 0.045f, Quaternion.identity);
            bola.GetComponent<Renderer>().sharedMaterial = Material(new Color(0.97f, 0.95f, 0.9f));
            puntaPalo = bola.transform;
            palo.gameObject.SetActive(false);
        }

        Material Material(Color c)
        {
            var m = materialMano != null ? new Material(materialMano) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c);
            m.SetColor("_Color", c);
            return m;
        }

        Transform Dedo(string nombre, Vector3 nudillo, float largo1, float largo2, float radio, out Transform medio, out Transform punta)
        {
            var baseDedo = new GameObject(nombre).transform;
            baseDedo.SetParent(raiz, false);
            baseDedo.localPosition = nudillo;
            Segmento(baseDedo, largo1, radio);

            medio = new GameObject(nombre + "_2").transform;
            medio.SetParent(baseDedo, false);
            medio.localPosition = new Vector3(0f, 0f, largo1);
            Segmento(medio, largo2, radio * 0.95f);

            punta = new GameObject(nombre + "_Punta").transform;
            punta.SetParent(medio, false);
            punta.localPosition = new Vector3(0f, 0f, largo2 + radio * 0.5f);
            return baseDedo;
        }

        void Segmento(Transform padre, float largo, float radio)
        {
            Parte(PrimitiveType.Capsule, padre, new Vector3(0f, 0f, largo * 0.5f),
                new Vector3(radio * 2f, (largo + radio * 2f) * 0.5f, radio * 2f), Quaternion.Euler(90f, 0f, 0f));
        }

        void Parte(PrimitiveType tipo, Transform padre, Vector3 pos, Vector3 escala, Quaternion rot) => Pieza(tipo, padre, pos, escala, rot);

        GameObject Pieza(PrimitiveType tipo, Transform padre, Vector3 pos, Vector3 escala, Quaternion rot)
        {
            var go = GameObject.CreatePrimitive(tipo);
            go.name = tipo.ToString();
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.SetParent(padre, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = escala;
            var r = go.GetComponent<Renderer>();
            if (materialMano != null) r.sharedMaterial = materialMano;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        void AlinearConPoke()
        {
            puntoPoke = null;
            foreach (var mb in GetComponentsInChildren<XRBaseInteractor>(true))
            {
                if (mb.GetType().Name != "XRPokeInteractor") continue;
                puntoPoke = mb.attachTransform != null ? mb.attachTransform : mb.transform;
                break;
            }
            if (puntoPoke == null || indicePunta == null) return;
            raiz.position += puntoPoke.position - indicePunta.position;
        }
    }
}
