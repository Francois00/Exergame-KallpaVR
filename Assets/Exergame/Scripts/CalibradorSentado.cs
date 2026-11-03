using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

namespace Exergame
{
    /// <summary>
    /// Mantiene el tablero siempre al frente de la persona sentada.
    /// Recentra: al iniciar, al ponerse el visor, cuando el sistema del Quest recentra,
    /// al mantener B o Y, y automaticamente si la persona queda mirando muy de costado.
    /// </summary>
    public class CalibradorSentado : MonoBehaviour
    {
        [Tooltip("Altura virtual de los ojos de una persona sentada, en metros.")]
        public float alturaOjos = 1.15f;
        public Vector3 posicionUsuario = Vector3.zero;
        public float esperaInicial = 1.5f;
        [Tooltip("Segundos que hay que mantener el boton B o Y para recentrar.")]
        public float segundosParaRecentrar = 2f;

        [Header("Recentrado automatico")]
        [Tooltip("Si la cabeza queda girada mas de estos grados respecto al tablero...")]
        public float gradosParaAutoRecentrar = 35f;
        [Tooltip("...durante estos segundos seguidos, se vuelve a poner el tablero al frente.")]
        public float segundosParaAutoRecentrar = 1.5f;
        [Tooltip("Desactivado: el personaje y el tablero se quedan fijos en su sitio aunque la persona mueva la cabeza. Recentrar desde el panel.")]
        public bool autoRecentrar = false;
        [Tooltip("Segundos que tarda en volver suavemente el tablero al frente (sin saltos bruscos).")]
        public float duracionGiroSuave = 0.8f;

        [Header("Comodidad (evitar mareo)")]
        [Tooltip("Al recentrar, la vista se oscurece un instante para no ver el mundo moverse.")]
        public bool oscurecerAlRecentrar = true;
        [Tooltip("El visor tiene que estar fuera de la cabeza al menos estos segundos para recentrar al volver a ponerlo.")]
        public float segundosSinVisorParaRecentrar = 2.5f;
        [Tooltip("Recentrar solo cuando el Quest/Link avisa que cambio el origen. Desactivado: con Meta Link este aviso llega seguido y movia el mundo.")]
        public bool recentrarAlCambiarOrigen = false;
        [Tooltip("Recentrar al volver a ponerse el visor. Desactivado: se recentraba mientras lo acomodaban y el tablero quedaba de costado.")]
        public bool recentrarAlPonerseVisor = false;
        [Tooltip("Ignora avisos repetidos del sistema para recentrar dentro de este tiempo.")]
        public float segundosEntreRecentrados = 3f;

        float ultimoRecentrado = -100f;
        float sinVisorDesde = -1f;
        GameObject telon;
        bool recentrando;

        XROrigin origen;
        float presionadoDesde = -1f;
        float giradoDesde = -1f;
        bool presenteAntes;
        bool recentrarPendiente;
        Coroutine giroSuave;
        readonly List<InputDevice> controles = new List<InputDevice>();
        readonly List<XRInputSubsystem> subsistemas = new List<XRInputSubsystem>();

        IEnumerator Start()
        {
            origen = GetComponent<XROrigin>();
            if (origen == null) origen = GetComponentInChildren<XROrigin>();
            DesactivarDesplazamiento();
            yield return new WaitForSeconds(esperaInicial);
            Recentrar();

            SubsystemManager.GetSubsystems(subsistemas);
            foreach (var s in subsistemas) s.trackingOriginUpdated += AlCambiarOrigen;
        }

        void OnDestroy()
        {
            foreach (var s in subsistemas) if (s != null) s.trackingOriginUpdated -= AlCambiarOrigen;
        }

        // El Quest recentro por su cuenta (boton Meta, limite estacionario): volvemos a alinear el tablero.
        void AlCambiarOrigen(XRInputSubsystem s)
        {
            if (recentrarAlCambiarOrigen && Time.time - ultimoRecentrado > segundosEntreRecentrados) recentrarPendiente = true;
        }

        // Nada de joystick para moverse o girar: la persona esta sentada y el mundo no debe moverse.
        void DesactivarDesplazamiento()
        {
            string[] tipos = { "Teleportation", "MoveProvider", "TurnProvider", "GrabMoveProvider", "ClimbProvider", "TunnelingVignette" };
            foreach (var mb in FindObjectsOfType<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                string n = mb.GetType().Name;
                foreach (var t in tipos)
                    if (n.Contains(t)) { mb.enabled = false; break; }
            }
        }

        void Update()
        {
            if (recentrarPendiente)
            {
                recentrarPendiente = false;
                Recentrar();
            }

            RevisarBotonRecentrar();
            RevisarVisorPuesto();
            RevisarGiro();
        }

        void RevisarBotonRecentrar()
        {
            bool presionado = false;
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, controles);
            foreach (var c in controles)
                if (c.TryGetFeatureValue(CommonUsages.secondaryButton, out bool b) && b) presionado = true;

            if (!presionado) { presionadoDesde = -1f; return; }
            if (presionadoDesde < 0f) presionadoDesde = Time.time;
            else if (Time.time - presionadoDesde >= segundosParaRecentrar)
            {
                Recentrar();
                presionadoDesde = float.MaxValue;
            }
        }

        // Cuando le ponen el visor a la persona, se recentra hacia donde ella mira.
        void RevisarVisorPuesto()
        {
            var visor = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (!visor.isValid || !visor.TryGetFeatureValue(CommonUsages.userPresence, out bool presente)) return;
            // Solo si de verdad se quito el visor (no por un parpadeo del sensor).
            if (!presente)
            {
                if (sinVisorDesde < 0f) sinVisorDesde = Time.time;
            }
            else
            {
                if (recentrarAlPonerseVisor && !presenteAntes && sinVisorDesde >= 0f && Time.time - sinVisorDesde >= segundosSinVisorParaRecentrar)
                    StartCoroutine(RecentrarLuego(0.8f));
                sinVisorDesde = -1f;
            }
            presenteAntes = presente;
        }

        IEnumerator RecentrarLuego(float segundos)
        {
            yield return new WaitForSeconds(segundos);
            Recentrar();
        }

        void RevisarGiro()
        {
            if (!autoRecentrar || origen == null || origen.Camera == null) { giradoDesde = -1f; return; }
            var adelante = origen.Camera.transform.forward;
            adelante.y = 0f;
            if (adelante.sqrMagnitude < 0.01f) { giradoDesde = -1f; return; }

            float angulo = Mathf.Abs(Vector3.SignedAngle(Vector3.forward, adelante, Vector3.up));
            if (angulo < gradosParaAutoRecentrar) { giradoDesde = -1f; return; }
            if (giradoDesde < 0f) giradoDesde = Time.time;
            else if (Time.time - giradoDesde >= segundosParaAutoRecentrar && giroSuave == null)
            {
                giradoDesde = -1f;
                giroSuave = StartCoroutine(RecentrarSuave());
            }
        }

        // Gira el mundo poco a poco hasta que el tablero y el personaje quedan otra vez al frente.
        IEnumerator RecentrarSuave()
        {
            var adelante = origen.Camera.transform.forward;
            adelante.y = 0f;
            float total = Vector3.SignedAngle(adelante, Vector3.forward, Vector3.up);
            float hecho = 0f, t = 0f;
            while (t < duracionGiroSuave && origen != null)
            {
                t += Time.deltaTime;
                float objetivo = total * Mathf.SmoothStep(0f, 1f, t / duracionGiroSuave);
                origen.RotateAroundCameraUsingOriginUp(objetivo - hecho);
                hecho = objetivo;
                yield return null;
            }
            if (origen != null) origen.MoveCameraToWorldLocation(new Vector3(posicionUsuario.x, alturaOjos, posicionUsuario.z));
            giroSuave = null;
            Debug.Log("[Exergame] Vista recentrada suavemente");
        }

        /// <summary>Si la cabeza esta girada mas de 'grados', vuelve a poner el tablero al frente suavemente.</summary>
        public void RecentrarSuaveSiDesviado(float grados)
        {
            if (origen == null || origen.Camera == null || giroSuave != null) return;
            var adelante = origen.Camera.transform.forward;
            adelante.y = 0f;
            if (adelante.sqrMagnitude < 0.01f) return;
            if (Mathf.Abs(Vector3.SignedAngle(Vector3.forward, adelante, Vector3.up)) < grados) return;
            giroSuave = StartCoroutine(RecentrarSuave());
        }

        /// <summary>
        /// Espera a que la cabeza este quieta (menos de 8 grados por segundo durante 0.8 s) y recien ahi centra.
        /// Asi el centro queda hacia donde la persona mira de verdad, no mientras le acomodan el visor.
        /// </summary>
        public IEnumerator RecentrarCuandoQuieto(float maxSegundos)
        {
            if (origen == null || origen.Camera == null) { Recentrar(); yield break; }
            float quietoDesde = -1f, inicio = Time.time;
            var antes = origen.Camera.transform.forward;
            while (Time.time - inicio < maxSegundos)
            {
                yield return null;
                var ahora = origen.Camera.transform.forward;
                float vel = Vector3.Angle(antes, ahora) / Mathf.Max(Time.deltaTime, 0.001f);
                antes = ahora;
                if (vel < 8f) { if (quietoDesde < 0f) quietoDesde = Time.time; else if (Time.time - quietoDesde >= 0.8f) break; }
                else quietoDesde = -1f;
            }
            Recentrar();
            yield return new WaitForSeconds(0.4f);
        }

        public void Recentrar()
        {
            if (origen == null || origen.Camera == null) return;
            if (oscurecerAlRecentrar && Application.isPlaying)
            {
                if (!recentrando) StartCoroutine(RecentrarConTelon());
                return;
            }
            RecentrarYa();
        }

        // Pantalla negra un instante: la persona no ve el mundo girar (evita mareo).
        IEnumerator RecentrarConTelon()
        {
            recentrando = true;
            MostrarTelon(true);
            yield return new WaitForSeconds(0.2f);
            RecentrarYa();
            yield return new WaitForSeconds(0.15f);
            MostrarTelon(false);
            recentrando = false;
        }

        void MostrarTelon(bool ver)
        {
            if (telon == null && ver && origen != null && origen.Camera != null)
            {
                telon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                telon.name = "TelonRecentrar";
                Destroy(telon.GetComponent<Collider>());
                telon.transform.SetParent(origen.Camera.transform, false);
                telon.transform.localScale = Vector3.one * 0.4f;
                var sh = Shader.Find("Universal Render Pipeline/Unlit");
                var mat = new Material(sh != null ? sh : Shader.Find("Unlit/Color"));
                mat.SetColor("_BaseColor", Color.black);
                mat.SetColor("_Color", Color.black);
                if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
                var r = telon.GetComponent<Renderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            if (telon != null) telon.SetActive(ver);
        }

        void RecentrarYa()
        {
            if (origen == null || origen.Camera == null) return;
            ultimoRecentrado = Time.time;
            if (giroSuave != null) { StopCoroutine(giroSuave); giroSuave = null; }
            origen.MatchOriginUpCameraForward(Vector3.up, Vector3.forward);
            origen.MoveCameraToWorldLocation(new Vector3(posicionUsuario.x, alturaOjos, posicionUsuario.z));
            giradoDesde = -1f;
            Debug.Log("[Exergame] Vista recentrada para usuario sentado");
        }
    }
}
