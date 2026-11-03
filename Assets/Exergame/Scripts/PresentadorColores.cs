using System.Collections;
using TMPro;
using UnityEngine;

namespace Exergame
{
    public class PresentadorColores : MonoBehaviour
    {
        [Header("Circulo que levanta el personaje")]
        public Transform hombroCirculo;
        public Transform circulo;
        public Renderer colorCirculo;
        public float anguloBrazoAbajo = 0f;
        public float anguloBrazoArriba = -84f;
        public float duracionMovimiento = 0.3f;

        [Header("Cara y cuerpo")]
        public Transform boca;
        public Transform[] ojos;
        public Transform cuerpo;

        [Header("Voz")]
        public AudioSource voz;

        [Header("Personaje animado (opcional, p. ej. Mousey de Mixamo)")]
        [Tooltip("Si se asigna, se usan sus animaciones: reposo, hablar, aplaudir y saludar.")]
        public Animator animador;
        [Tooltip("Lleva la mano del personaje al circulo de color.")]
        public IKManoPresentador ikMano;

        [Header("Letrero y globo de dialogo")]
        [Tooltip("El letrero siempre de frente al residente (se ve como circulo completo).")]
        public bool letreroDeFrente = true;
        [Tooltip("Donde aparece el globo de dialogo de la presentacion.")]
        public Transform puntoGlobo;

        GameObject globo;
        TextMeshPro textoGlobo;
        Transform camara;

        [Tooltip("Animacion de cuerpo al hablar (desactivada: movia a Mousey hacia adelante). La boca se mueve igual.")]
        public bool animarCuerpoAlHablar = false;

        static readonly int IdHablando = Animator.StringToHash("Hablando");
        static readonly int IdAplaudir = Animator.StringToHash("Aplaudir");
        static readonly int IdSaludar = Animator.StringToHash("Saludar");

        static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");

        Material materialCirculo;
        Vector3 escalaCirculo;
        Vector3 escalaBoca;
        Vector3[] escalaOjos;
        Vector3 posicionCuerpo;
        Coroutine rutinaBrazo;
        float salto;
        float anguloActual;
        float proximoParpadeo;

        public bool CirculoArriba { get; private set; }
        public bool EstaHablando => voz != null && voz.isPlaying;

        void Awake()
        {
            if (voz == null) voz = GetComponent<AudioSource>();
            if (colorCirculo != null) materialCirculo = colorCirculo.material;
            if (circulo != null)
            {
                escalaCirculo = circulo.localScale;
                circulo.localScale = Vector3.zero;
            }
            if (boca != null) escalaBoca = boca.localScale;
            if (ojos != null)
            {
                escalaOjos = new Vector3[ojos.Length];
                for (int i = 0; i < ojos.Length; i++) escalaOjos[i] = ojos[i].localScale;
            }
            if (cuerpo != null) posicionCuerpo = cuerpo.localPosition;
            FijarBrazo(anguloBrazoAbajo);
            proximoParpadeo = Time.time + Random.Range(2f, 4f);
        }

        void Update()
        {
            AnimarBoca();
            AnimarParpadeo();
            if (animador != null && animador.isActiveAndEnabled && animador.runtimeAnimatorController != null)
                animador.SetBool(IdHablando, animarCuerpoAlHablar && EstaHablando);
            MirarAlResidente();
            if (cuerpo != null)
                cuerpo.localPosition = posicionCuerpo + Vector3.up * (Mathf.Sin(Time.time * 1.6f) * 0.004f + salto);
        }

        // ---- Letrero segun el escenario: en Globos/Tambores muestra el objeto pintado (sobre fondo crema).
        Escenario temaLetrero = Escenario.Clasico;
        Transform icono;
        Material matIcono, matIconoClaro;
        static readonly Color FondoLetreroTema = new Color(0.96f, 0.93f, 0.85f);

        public void FijarTema(Escenario e, Material baseLit)
        {
            temaLetrero = e;
            if (icono != null) Destroy(icono.gameObject);
            icono = null;
            if (e == Escenario.Clasico || circulo == null) return;
            if (matIcono == null) matIcono = MallasProcedurales.Brillante(Color.white, baseLit);
            icono = new GameObject("IconoTema").transform;
            icono.SetParent(circulo, false);
            // El letrero mira a la persona con su +Z (ver MirarAlResidente): el icono va delante del disco.
            icono.localPosition = new Vector3(0f, 0f, 0.035f);
            icono.localRotation = Quaternion.identity;
            if (e == Escenario.Globos)
            {
                var g = MallasProcedurales.Crear("Globo", MallasProcedurales.Globo(0.062f, 0.15f), matIcono, icono);
                g.transform.localPosition = new Vector3(0f, 0.012f, 0f);
                var nudo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(nudo.GetComponent<Collider>());
                nudo.transform.SetParent(icono, false);
                nudo.transform.localPosition = new Vector3(0f, -0.066f, 0f);
                nudo.transform.localScale = new Vector3(0.016f, 0.012f, 0.016f);
                nudo.GetComponent<Renderer>().sharedMaterial = matIcono;
            }
            else
            {
                var tambor = new GameObject("Tambor").transform;
                tambor.SetParent(icono, false);
                tambor.localRotation = Quaternion.Euler(-40f, 0f, 0f);   // se ve la piel y el costado
                var casco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(casco.GetComponent<Collider>());
                casco.transform.SetParent(tambor, false);
                casco.transform.localScale = new Vector3(0.14f, 0.04f, 0.14f);
                casco.GetComponent<Renderer>().sharedMaterial = matIcono;
                var aro = MallasProcedurales.Crear("Aro", MallasProcedurales.Anillo(0.064f, 0.074f), MallasProcedurales.Plano(new Color(0.85f, 0.85f, 0.88f)), tambor);
                aro.transform.localPosition = new Vector3(0f, 0.041f, 0f);
                if (matIconoClaro == null) matIconoClaro = MallasProcedurales.Brillante(Color.white, baseLit);
                var piel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(piel.GetComponent<Collider>());
                piel.transform.SetParent(tambor, false);
                piel.transform.localPosition = new Vector3(0f, 0.041f, 0f);
                piel.transform.localScale = new Vector3(0.128f, 0.002f, 0.128f);
                piel.GetComponent<Renderer>().sharedMaterial = matIconoClaro;
                // Cuerdas en zigzag y aro inferior, como los tambores del tablero.
                var cuerdas = new GameObject("Cuerdas").AddComponent<LineRenderer>();
                cuerdas.transform.SetParent(tambor, false);
                cuerdas.useWorldSpace = false;
                cuerdas.widthMultiplier = 0.004f;
                cuerdas.sharedMaterial = MallasProcedurales.Plano(new Color(0.96f, 0.95f, 0.92f));
                cuerdas.positionCount = 25;
                for (int i = 0; i < 25; i++)
                {
                    float a = i * Mathf.PI / 12f;
                    cuerdas.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.0715f, i % 2 == 0 ? 0.03f : -0.03f, Mathf.Sin(a) * 0.0715f));
                }
                var aroAbajo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(aroAbajo.GetComponent<Collider>());
                aroAbajo.transform.SetParent(tambor, false);
                aroAbajo.transform.localPosition = new Vector3(0f, -0.037f, 0f);
                aroAbajo.transform.localScale = new Vector3(0.152f, 0.004f, 0.152f);
                aroAbajo.GetComponent<Renderer>().sharedMaterial = aro.GetComponent<Renderer>().sharedMaterial;
                aro.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            foreach (var r in icono.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public void LevantarCirculo(Color color)
        {
            if (icono != null && temaLetrero != Escenario.Clasico)
            {
                if (materialCirculo != null) materialCirculo.SetColor(IdBaseColor, FondoLetreroTema);
                matIcono.SetColor(IdBaseColor, color);
                matIcono.SetColor("_Color", color);
                if (matIconoClaro != null) matIconoClaro.SetColor(IdBaseColor, Color.Lerp(color, Color.white, 0.3f));
            }
            else if (materialCirculo != null) materialCirculo.SetColor(IdBaseColor, color);
            CirculoArriba = true;
            if (ikMano != null) ikMano.pesoDeseado = 1f;
            Mover(anguloBrazoArriba, 1f);
        }

        public void BajarCirculo()
        {
            CirculoArriba = false;
            if (ikMano != null) ikMano.pesoDeseado = 0f;
            Mover(anguloBrazoAbajo, 0f);
        }

        /// <summary>Saltito de alegria cuando el residente acierta.</summary>
        public void Celebrar()
        {
            if (!isActiveAndEnabled) return;
            if (TieneAnimador) animador.SetTrigger(IdAplaudir);
            else StartCoroutine(RutinaSalto());
        }

        Transform objetivoLetrero;
        bool senalando;

        /// <summary>Estira la mano hacia algo (por ejemplo, un globo de practica).</summary>
        public void Senalar(Transform cosa)
        {
            if (ikMano == null || cosa == null) return;
            if (!senalando) objetivoLetrero = ikMano.objetivo;
            senalando = true;
            // Senala desde un poco antes (no tapa el objeto con la mano).
            var punto = new GameObject("PuntoSenalar").transform;
            punto.SetParent(cosa, false);
            var hacia = transform.position - cosa.position;
            if (animador != null) hacia = animador.transform.position - cosa.position;
            hacia.y = 0f;
            punto.position = cosa.position + hacia.normalized * 0.16f + Vector3.up * 0.02f;
            ikMano.objetivo = punto;
            ikMano.pesoDeseado = 1f;
        }

        public void DejarDeSenalar()
        {
            if (ikMano == null || !senalando) return;
            senalando = false;
            ikMano.pesoDeseado = CirculoArriba ? 1f : 0f;
            StartCoroutine(RestaurarObjetivo());
        }

        IEnumerator RestaurarObjetivo()
        {
            // Espera a que la mano baje antes de volver a apuntar al letrero.
            yield return new WaitForSeconds(0.35f);
            if (!senalando && ikMano != null) ikMano.objetivo = objetivoLetrero;
        }

        /// <summary>Saludo con la mano al empezar la sesion.</summary>
        public void Saludar()
        {
            if (isActiveAndEnabled && TieneAnimador) animador.SetTrigger(IdSaludar);
        }

        bool TieneAnimador => animador != null && animador.isActiveAndEnabled && animador.runtimeAnimatorController != null;

        IEnumerator RutinaSalto()
        {
            const float dur = 0.45f;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                salto = Mathf.Sin(t / dur * Mathf.PI) * 0.06f;
                yield return null;
            }
            salto = 0f;
        }

        public void Decir(AudioClip palabra)
        {
            if (voz == null || palabra == null) return;
            voz.Stop();
            voz.clip = palabra;
            voz.Play();
        }

        void Mover(float angulo, float escalaFinal)
        {
            if (rutinaBrazo != null) StopCoroutine(rutinaBrazo);
            rutinaBrazo = StartCoroutine(RutinaBrazo(angulo, escalaFinal));
        }

        IEnumerator RutinaBrazo(float anguloFinal, float escalaFinal)
        {
            float anguloInicial = anguloActual;
            float escalaInicial = circulo != null && escalaCirculo.x > 0f ? circulo.localScale.x / escalaCirculo.x : 0f;
            float t = 0f;
            while (t < duracionMovimiento)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / duracionMovimiento);
                FijarBrazo(Mathf.Lerp(anguloInicial, anguloFinal, k));
                // Al sacar el letrero, "salta" un poquito (crece un 12 % y vuelve): llama la atencion.
                float ke = escalaFinal > escalaInicial ? SaltoSuave(t / duracionMovimiento) : k;
                if (circulo != null) circulo.localScale = escalaCirculo * Mathf.LerpUnclamped(escalaInicial, escalaFinal, ke);
                yield return null;
            }
            FijarBrazo(anguloFinal);
            if (circulo != null) circulo.localScale = escalaCirculo * escalaFinal;
            rutinaBrazo = null;
        }

        static float SaltoSuave(float x)
        {
            x = Mathf.Clamp01(x);
            const float c1 = 1.2f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        Transform Camara()
        {
            if (camara == null && Camera.main != null) camara = Camera.main.transform;
            return camara;
        }

        void MirarAlResidente()
        {
            var cam = Camara();
            if (cam == null) return;
            if (letreroDeFrente && circulo != null && CirculoArriba)
            {
                var hacia = cam.position - circulo.position;
                if (hacia.sqrMagnitude > 0.0001f) circulo.rotation = Quaternion.LookRotation(hacia, Vector3.up);
            }
            if (globo != null && globo.activeSelf)
            {
                var hacia = globo.transform.position - cam.position;
                hacia.y = 0f;
                if (hacia.sqrMagnitude > 0.0001f) globo.transform.rotation = Quaternion.LookRotation(hacia, Vector3.up);
            }
        }

        /// <summary>Globo de dialogo con letras grandes (para la presentacion).</summary>
        public void MostrarGlobo(string texto)
        {
            if (globo == null) CrearGlobo();
            textoGlobo.text = texto;
            globo.SetActive(true);
            MirarAlResidente();
            StartCoroutine(AparecerGlobo());
        }

        public void OcultarGlobo()
        {
            if (globo != null) globo.SetActive(false);
        }

        IEnumerator AparecerGlobo()
        {
            for (float t = 0f; t < 0.3f && globo != null; t += Time.deltaTime)
            {
                globo.transform.localScale = Vector3.one * SaltoSuave(t / 0.3f);
                yield return null;
            }
            if (globo != null) globo.transform.localScale = Vector3.one;
        }

        void CrearGlobo()
        {
            globo = new GameObject("GloboDialogo");
            var ancla = puntoGlobo != null ? puntoGlobo : transform;
            globo.transform.SetParent(ancla, false);
            if (puntoGlobo == null) globo.transform.localPosition = new Vector3(-0.45f, 1.55f, 0.3f);

            // Globo de dialogo: esquinas redondeadas, colita hacia la presentadora, borde y sombra suave.
            const float ancho = 0.62f, alto = 0.26f, radio = 0.05f, colita = 0.07f;
            var sombra = MallasProcedurales.Crear("Sombra", MallasProcedurales.RectanguloRedondeado(ancho + 0.02f, alto + 0.02f, radio + 0.01f, colita),
                                                  MallasProcedurales.Plano(new Color(0.12f, 0.14f, 0.22f)), globo.transform);
            sombra.transform.localPosition = new Vector3(0.012f, -0.014f, 0.008f);
            var borde = MallasProcedurales.Crear("Borde", MallasProcedurales.RectanguloRedondeado(ancho + 0.02f, alto + 0.02f, radio + 0.01f, colita),
                                                 MallasProcedurales.Plano(new Color(0.20f, 0.42f, 0.85f)), globo.transform);
            borde.transform.localPosition = new Vector3(0f, 0f, 0.004f);
            MallasProcedurales.Crear("Fondo", MallasProcedurales.RectanguloRedondeado(ancho, alto, radio, colita - 0.012f),
                                     MallasProcedurales.Plano(new Color(1f, 0.99f, 0.95f)), globo.transform);

            var texto = new GameObject("Texto");
            texto.transform.SetParent(globo.transform, false);
            texto.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            textoGlobo = texto.AddComponent<TextMeshPro>();
            textoGlobo.rectTransform.sizeDelta = new Vector2(ancho - 0.06f, alto - 0.04f);
            textoGlobo.alignment = TextAlignmentOptions.Center;
            textoGlobo.enableAutoSizing = true;
            textoGlobo.fontSizeMin = 0.1f;
            textoGlobo.fontSizeMax = 0.6f;
            textoGlobo.fontStyle = FontStyles.Bold;
            textoGlobo.color = new Color(0.10f, 0.16f, 0.36f);
            textoGlobo.lineSpacing = -8f;

            globo.SetActive(false);
        }

        static Material Plano(Color c)
        {
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            var m = new Material(unlit != null ? unlit : Shader.Find("Unlit/Color"));
            m.SetColor("_BaseColor", c);
            m.SetColor("_Color", c);
            return m;
        }

        void FijarBrazo(float angulo)
        {
            anguloActual = angulo;
            if (hombroCirculo != null) hombroCirculo.localRotation = Quaternion.Euler(angulo, 0f, 0f);
        }

        void AnimarBoca()
        {
            if (boca == null) return;
            float apertura = EstaHablando ? 0.6f + Mathf.Abs(Mathf.Sin(Time.time * 18f)) * 2.2f : 1f;
            boca.localScale = new Vector3(escalaBoca.x, escalaBoca.y * apertura, escalaBoca.z);
        }

        void AnimarParpadeo()
        {
            if (ojos == null || escalaOjos == null) return;
            bool cerrado = Time.time >= proximoParpadeo && Time.time < proximoParpadeo + 0.12f;
            for (int i = 0; i < ojos.Length; i++)
            {
                var e = escalaOjos[i];
                ojos[i].localScale = cerrado ? new Vector3(e.x, e.y * 0.15f, e.z) : e;
            }
            if (Time.time >= proximoParpadeo + 0.12f) proximoParpadeo = Time.time + Random.Range(2.5f, 5f);
        }

        void OnDestroy()
        {
            if (materialCirculo != null) Destroy(materialCirculo);
        }
    }
}
