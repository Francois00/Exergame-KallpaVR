using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace Exergame
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    [RequireComponent(typeof(AudioSource))]
    public class BotonExergame : MonoBehaviour
    {
        [Header("Posicion en el tablero")]
        public Posicion posicion;
        public Renderer tapa;
        [Tooltip("Punto al que apunta el personaje. Si esta vacio, se usa la tapa.")]
        public Transform puntoSenal;

        [Header("Respuesta")]
        [Tooltip("Solo cuenta si se empuja con el dedo (no con rayo ni agarre).")]
        public bool soloEmpujeConDedo = true;
        [Tooltip("En el editor de Unity (pruebas en la PC) tambien acepta rayo + gatillo.")]
        public bool permitirRayoEnEditor = true;

        [Header("Toque por cercania (mas facil para adultos mayores)")]
        [Tooltip("Cuenta el toque cuando la punta del dedo o la palma entra en la zona del circulo, sin importar el angulo.")]
        public bool toquePorCercania = true;
        [Tooltip("Mitad del ancho de la zona de toque, en metros (0.12 = todo el cuadrante).")]
        public float mitadZona = 0.12f;
        [Tooltip("Hasta que distancia por delante del circulo ya cuenta el toque, en metros.")]
        public float profundidadZona = 0.16f;
        [Tooltip("Cuanto puede atravesar el tablero la mano y seguir contando, en metros.")]
        public float atravesarZona = 0.06f;
        public Color colorInactivo = new Color(0.55f, 0.55f, 0.55f);

        [Header("Sonido y vibracion")]
        [Range(0f, 1f)] public float volumen = 0.7f;
        [Range(0f, 1f)] public float amplitudVibracion = 0.35f;
        public float duracionVibracion = 0.1f;

        public static event Action<BotonExergame, Mano> AlgunBotonEmpujado;

        /// <summary>Objeto del escenario (globo, tambor). Nulo o clasico = circulo de color.</summary>
        [NonSerialized] public EstimuloTema tema;
        bool ConTema => tema != null && !tema.EsClasico;

        public ColorJuego ColorActual { get; private set; }
        public bool Activo { get; private set; } = true;
        public bool Habilitado { get; set; }
        public bool Caido { get; private set; }
        public bool LadoIzquierdo => Colores.EsIzquierda(posicion);
        public Vector3 PuntoSenal => (puntoSenal != null ? puntoSenal : tapa.transform).position;

        static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");

        XRSimpleInteractable interactable;
        AudioSource fuente;
        AudioClip sonido;
        Material material;
        Vector3 posicionReposo;
        Quaternion rotacionReposo;
        Vector3 escalaReposo;
        Coroutine rutina;
        readonly HashSet<ManoVirtual> dentro = new HashSet<ManoVirtual>();
        bool estabaHabilitado;

        void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            fuente = GetComponent<AudioSource>();
            fuente.playOnAwake = false;
            fuente.spatialBlend = 1f;
            fuente.minDistance = 0.5f;
            if (tapa == null) tapa = GetComponentInChildren<Renderer>();
            material = tapa.material;
            posicionReposo = tapa.transform.localPosition;
            rotacionReposo = tapa.transform.localRotation;
            escalaReposo = tapa.transform.localScale;
            sonido = GeneradorTonos.Click();
        }

        void OnEnable() { interactable.selectEntered.AddListener(AlSeleccionar); }

        void Update()
        {
            if (!toquePorCercania) return;
            bool listo = Habilitado && Activo && !Caido;
            if (!listo) { estabaHabilitado = false; return; }

            foreach (var m in ManoVirtual.Todas)
            {
                if (m == null) continue;
                // Histeresis: para entrar hay que pasar 2 cm dentro; para salir, 2 cm fuera. Evita toques por temblor en el borde.
                bool yaDentro = dentro.Contains(m);
                float margen = yaDentro ? 0.02f : -0.02f;
                bool ahora = EnZona(m.PuntaDedo, margen) || EnZona(m.Palma, margen);
                if (!estabaHabilitado)
                {
                    // Al empezar el ensayo, una mano que ya estaba apoyada no cuenta: tiene que entrar.
                    if (ahora) dentro.Add(m); else dentro.Remove(m);
                    continue;
                }
                if (ahora && !dentro.Contains(m))
                {
                    dentro.Add(m);
                    m.Vibrar(amplitudVibracion, duracionVibracion);
                    Empujar(m.esIzquierda ? Mano.Izquierda : Mano.Derecha);
                    return;
                }
                if (!ahora) dentro.Remove(m);
            }
            estabaHabilitado = true;
        }

        bool EnZona(Transform punto, float margen)
        {
            if (punto == null) return false;
            var p = transform.InverseTransformPoint(punto.position);
            return Mathf.Abs(p.x) <= mitadZona + margen && Mathf.Abs(p.z) <= mitadZona + margen
                   && p.y <= profundidadZona + margen && p.y >= -atravesarZona - margen;
        }
        void OnDisable() { interactable.selectEntered.RemoveListener(AlSeleccionar); }

        public void AsignarColor(ColorJuego color)
        {
            ColorActual = color;
            AplicarColor();
        }

        public void FijarActivo(bool activo)
        {
            Activo = activo;
            AplicarColor();
        }

        void AplicarColor()
        {
            var c = Activo ? Colores.Valor(ColorActual) : colorInactivo;
            if (material != null) material.SetColor(IdBaseColor, c);
            if (tema != null) tema.Colorear(c, ColorActual);
        }

        void AlSeleccionar(SelectEnterEventArgs args)
        {
            if (!Habilitado || !Activo || Caido) return;
            bool esDedo = args.interactorObject.GetType().Name == "XRPokeInteractor";
            bool rayoPermitido = permitirRayoEnEditor && Application.isEditor;
            if (soloEmpujeConDedo && !esDedo && !rayoPermitido) return;

            var mano = DetectarMano(args.interactorObject);
            Vibrar(args.interactorObject);
            Empujar(mano);
        }

        public void EmpujarSimulado(Mano mano) => Empujar(mano);

        /// <summary>Sonido alegre, confeti y una frase de felicitacion desde este casillero.</summary>
        public void Felicitar(string frase = null)
        {
            if (ConTema) tema.Celebrar();
            Felicitacion.Mostrar(this, ConTema ? tema.PuntoEfecto : tapa.transform.parent.position, material, fuente, 0.8f, frase);
        }

        /// <summary>No era este objeto (o fue con la otra mano): reaccion suave, sin castigo.</summary>
        public void Fallar()
        {
            if (ConTema) tema.Fallar();
        }

        void Empujar(Mano mano)
        {
            if (!Habilitado || !Activo || Caido) return;
            if (ConTema)
            {
                Caido = true;          // un solo toque por ensayo
                tema.AlTocar();
            }
            else
            {
                fuente.PlayOneShot(sonido, volumen);
                Caer();
            }
            AlgunBotonEmpujado?.Invoke(this, mano);
        }

        public void Caer()
        {
            if (rutina != null) StopCoroutine(rutina);
            Caido = true;
            rutina = StartCoroutine(RutinaCaer());
        }

        IEnumerator RutinaCaer()
        {
            var t = tapa.transform;
            float d = 0f;
            const float empuje = 0.08f;
            while (d < empuje)
            {
                d += Time.deltaTime;
                t.localPosition = posicionReposo - Vector3.up * Mathf.Lerp(0f, 0.014f, d / empuje);
                yield return null;
            }

            d = 0f;
            const float caida = 0.35f;
            Vector3 inicio = t.localPosition;
            while (d < caida)
            {
                d += Time.deltaTime;
                float k = d / caida;
                t.localPosition = inicio - Vector3.up * 0.02f * k - Vector3.forward * 0.12f * k * k;
                t.localRotation = rotacionReposo * Quaternion.Euler(85f * k, 0f, 0f);
                t.localScale = escalaReposo * (1f - 0.6f * k);
                yield return null;
            }
            tapa.enabled = false;
            rutina = null;
        }

        public void Reponer()
        {
            if (rutina != null) StopCoroutine(rutina);
            rutina = null;
            Caido = false;
            var t = tapa.transform;
            t.localPosition = posicionReposo;
            t.localRotation = rotacionReposo;
            t.localScale = escalaReposo;
            tapa.enabled = !ConTema;
            if (ConTema) tema.Reponer();
        }

        static Mano DetectarMano(IXRInteractor interactor)
        {
            if (interactor == null) return Mano.Ninguna;
            var mano = interactor.transform.GetComponentInParent<ManoVirtual>();
            if (mano != null) return mano.esIzquierda ? Mano.Izquierda : Mano.Derecha;
            for (var t = interactor.transform; t != null; t = t.parent)
                if (t.name.ToLowerInvariant().Contains("left")) return Mano.Izquierda;
            return Mano.Derecha;
        }

        void Vibrar(IXRInteractor interactor)
        {
            var control = interactor?.transform.GetComponentInParent<XRBaseController>();
            if (control != null) control.SendHapticImpulse(amplitudVibracion, duracionVibracion);
        }

        void OnDestroy()
        {
            if (material != null) Destroy(material);
        }
    }
}
