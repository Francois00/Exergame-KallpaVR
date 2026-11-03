using System.Collections;
using UnityEngine;

namespace Exergame
{
    /// <summary>
    /// Cambia lo que se ve en un casillero del tablero segun el escenario:
    /// Clasico = circulo de color (como siempre), Globos = globo que flota y revienta,
    /// Tambores = tambor que suena. El casillero, la zona de toque y el registro no cambian.
    /// Coordenadas del boton: X a la derecha, Z hacia arriba del tablero, Y hacia la persona.
    /// </summary>
    [RequireComponent(typeof(BotonExergame))]
    public class EstimuloTema : MonoBehaviour
    {
        public Escenario Tema { get; private set; } = Escenario.Clasico;
        public bool EsClasico => Tema == Escenario.Clasico;

        /// <summary>Donde salen el confeti y los efectos.</summary>
        public Vector3 PuntoEfecto => cuerpo != null ? cuerpo.position : transform.TransformPoint(0f, 0.08f, 0f);

        [Range(0f, 1f)] public float volumen = 0.75f;

        BotonExergame boton;
        AudioSource fuente;
        Transform raiz, cuerpo, nudo, parche;
        LineRenderer hilo;
        Material matColor, matClaro, matMetal, matHilo, matDorado;
        Color colorActual = Color.white;
        ColorJuego colorJuego;
        float fase, apretar, animInflar = 1f;
        bool reventado, sacudiendo;
        Vector3 posParche;

        public static EstimuloTema De(BotonExergame b)
        {
            var t = b.GetComponent<EstimuloTema>();
            return t != null ? t : b.gameObject.AddComponent<EstimuloTema>();
        }

        void Awake() { Iniciar(); }

        // El tablero puede estar oculto cuando se agrega este componente (Awake aun no corrio).
        void Iniciar()
        {
            if (boton == null) boton = GetComponent<BotonExergame>();
            if (fuente == null) fuente = GetComponent<AudioSource>();
            if (fase == 0f) fase = Random.value * 10f + 0.01f;
        }

        // ---------------------------------------------------------------- armado

        public void Aplicar(Escenario e, Material materialBase)
        {
            Iniciar();
            if (raiz != null) Destroy(raiz.gameObject);
            raiz = cuerpo = nudo = parche = null;
            hilo = null;
            Tema = e;
            boton.tema = this;
            reventado = false;
            animInflar = 1f;

            var aro = transform.Find("Aro");
            if (aro != null) aro.gameObject.SetActive(e == Escenario.Clasico);
            if (boton.tapa != null) boton.tapa.enabled = e == Escenario.Clasico && !boton.Caido;
            if (e == Escenario.Clasico) return;

            if (matColor == null)
            {
                matColor = MallasProcedurales.Brillante(colorActual, materialBase);
                matClaro = MallasProcedurales.Brillante(colorActual, materialBase);
                matMetal = MallasProcedurales.Brillante(new Color(0.88f, 0.88f, 0.90f), materialBase);
                if (matMetal.HasProperty("_Metallic")) matMetal.SetFloat("_Metallic", 0.35f);
                if (matMetal.HasProperty("_Smoothness")) matMetal.SetFloat("_Smoothness", 0.75f);
                matHilo = MallasProcedurales.Plano(new Color(0.93f, 0.93f, 0.95f));
                matDorado = MallasProcedurales.Plano(new Color(1f, 0.80f, 0.22f));
            }

            raiz = new GameObject("Tema_" + e).transform;
            raiz.SetParent(transform, false);
            if (e == Escenario.Globos) ArmarGlobo(); else ArmarTambor();
            Colorear(colorActual, colorJuego);
        }

        void ArmarGlobo()
        {
            cuerpo = new GameObject("Globo").transform;
            cuerpo.SetParent(raiz, false);
            cuerpo.localPosition = new Vector3(0f, 0.075f, 0.012f);
            MallasProcedurales.Crear("Forma", MallasProcedurales.Globo(0.074f, 0.17f), matColor, cuerpo);
            nudo = Pieza(PrimitiveType.Sphere, cuerpo, new Vector3(0f, -0.088f, 0f), new Vector3(0.02f, 0.014f, 0.02f), matColor).transform;

            var h = new GameObject("Hilo");
            h.transform.SetParent(raiz, false);
            hilo = h.AddComponent<LineRenderer>();
            hilo.useWorldSpace = true;
            hilo.positionCount = 8;
            hilo.widthMultiplier = 0.0025f;
            hilo.sharedMaterial = matHilo;
            hilo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        const float RadioTambor = 0.072f, AltoTambor = 0.11f;

        void ArmarTambor()
        {
            // Tambor inclinado hacia arriba: se ve el casco de color con sus cuerdas en zigzag,
            // los aros metalicos y el parche claro arriba (como un tambor de verdad).
            cuerpo = new GameObject("Tambor").transform;
            cuerpo.SetParent(raiz, false);
            cuerpo.localPosition = new Vector3(0f, 0.095f, -0.015f);
            cuerpo.localRotation = Quaternion.Euler(50f, 0f, 0f);
            float r = RadioTambor, h = AltoTambor;
            Pieza(PrimitiveType.Cylinder, cuerpo, Vector3.zero, new Vector3(r * 2f, h / 2f, r * 2f), matColor);
            foreach (float y in new[] { h / 2f - 0.004f, -h / 2f + 0.004f })
                Pieza(PrimitiveType.Cylinder, cuerpo, new Vector3(0f, y, 0f), new Vector3(r * 2f + 0.014f, 0.006f, r * 2f + 0.014f), matMetal);
            parche = Pieza(PrimitiveType.Cylinder, cuerpo, new Vector3(0f, h / 2f + 0.0035f, 0f), new Vector3(r * 1.96f, 0.0015f, r * 1.96f), matClaro).transform;
            posParche = parche.localPosition;
            // Punto central del parche (donde se golpea).
            Pieza(PrimitiveType.Cylinder, cuerpo, new Vector3(0f, h / 2f + 0.0045f, 0f), new Vector3(r * 0.55f, 0.0012f, r * 0.55f), matColor).transform.SetParent(parche, true);

            var c = new GameObject("Cuerdas");
            c.transform.SetParent(cuerpo, false);
            var lr = c.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.widthMultiplier = 0.0038f;
            lr.sharedMaterial = matHilo;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            const int zigs = 14;
            lr.positionCount = zigs * 2 + 1;
            for (int i = 0; i <= zigs * 2; i++)
            {
                float a = i * Mathf.PI / zigs;
                float y = (i % 2 == 0) ? h / 2f - 0.012f : -h / 2f + 0.012f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * (r + 0.0025f), y, Mathf.Sin(a) * (r + 0.0025f)));
            }
        }

        static GameObject Pieza(PrimitiveType tipo, Transform padre, Vector3 pos, Vector3 escala, Material mat)
        {
            var go = GameObject.CreatePrimitive(tipo);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(padre, false);
            go.transform.localPosition = pos;
            go.transform.localScale = escala;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        // ---------------------------------------------------------------- color

        public void Colorear(Color c, ColorJuego juego)
        {
            colorActual = c;
            colorJuego = juego;
            if (matColor == null) return;
            matColor.SetColor("_BaseColor", c);
            matColor.SetColor("_Color", c);
            var claro = Color.Lerp(c, Color.white, Tema == Escenario.Tambores ? 0.72f : 0.28f);
            matClaro.SetColor("_BaseColor", claro);
            matClaro.SetColor("_Color", claro);
        }

        // ---------------------------------------------------------------- animacion continua

        void Update()
        {
            if (EsClasico || cuerpo == null) return;
            fase += Time.deltaTime;
            if (Tema == Escenario.Globos)
            {
                // Flota y se balancea suave; mira siempre "hacia arriba" del mundo.
                var centro = transform.TransformPoint(new Vector3(0f, 0.075f, 0.012f));
                cuerpo.position = centro + Vector3.up * Mathf.Sin(fase * 1.7f) * 0.006f;
                var frente = Vector3.ProjectOnPlane(-transform.up, Vector3.up);
                if (frente.sqrMagnitude < 0.001f) frente = Vector3.forward;
                var giro = sacudiendo ? cuerpo.rotation : Quaternion.LookRotation(frente, Vector3.up) * Quaternion.Euler(0f, 0f, Mathf.Sin(fase * 1.2f) * 4f);
                if (!sacudiendo) cuerpo.rotation = giro;
                float k = reventado ? 0f : animInflar;
                cuerpo.localScale = new Vector3(1f + apretar * 0.12f, 1f - apretar * 0.15f, 1f + apretar * 0.12f) * k;
                if (hilo != null)
                {
                    hilo.enabled = !reventado && k > 0.5f;
                    var desde = nudo.position;
                    var hasta = transform.TransformPoint(new Vector3(0f, 0.012f, -0.10f));
                    for (int i = 0; i < hilo.positionCount; i++)
                    {
                        float t = i / (float)(hilo.positionCount - 1);
                        var p = Vector3.Lerp(desde, hasta, t) + transform.right * Mathf.Sin(t * Mathf.PI * 2f + fase * 2f) * 0.006f * (1f - t);
                        hilo.SetPosition(i, p);
                    }
                }
            }
            else if (parche != null)
            {
                parche.localPosition = posParche - Vector3.up * apretar * 0.006f;
                cuerpo.localScale = Vector3.one * (1f + apretar * 0.04f);
            }
            apretar = Mathf.MoveTowards(apretar, 0f, Time.deltaTime * 8f);
        }

        // ---------------------------------------------------------------- reacciones

        /// <summary>Al tocarlo (antes de saber si acerto): el globo se aprieta, el parche se hunde.</summary>
        public void AlTocar()
        {
            apretar = 1f;
        }

        /// <summary>Acierto: el globo revienta / el tambor suena y salen notas.</summary>
        public void Celebrar()
        {
            if (EsClasico) return;
            if (Tema == Escenario.Globos) StartCoroutine(Reventar());
            else StartCoroutine(Sonar());
        }

        /// <summary>No era ese: el globo se sacude con un "boing"; el tambor da un golpe apagado.</summary>
        public void Fallar()
        {
            if (EsClasico) return;
            if (Tema == Escenario.Globos)
            {
                fuente.PlayOneShot(SonidosTema.Boing(), volumen * 0.8f);
                StartCoroutine(Sacudir());
            }
            else
            {
                fuente.PlayOneShot(SonidosTema.TamborApagado(), volumen * 0.8f);
                apretar = 0.6f;
            }
        }

        /// <summary>Vuelve a estar listo para el siguiente ensayo (el globo se vuelve a inflar).</summary>
        public void Reponer()
        {
            if (EsClasico) return;
            if (reventado)
            {
                reventado = false;
                StartCoroutine(Inflar());
            }
            if (boton.tapa != null) boton.tapa.enabled = false;
        }

        IEnumerator Inflar()
        {
            for (float t = 0f; t < 0.35f; t += Time.deltaTime)
            {
                float x = t / 0.35f;
                animInflar = 1f + 1.6f * Mathf.Pow(x - 1f, 3f) + 0.6f * Mathf.Pow(x - 1f, 2f);
                yield return null;
            }
            animInflar = 1f;
        }

        IEnumerator Sacudir()
        {
            sacudiendo = true;
            var baseRot = cuerpo.rotation;
            for (float t = 0f; t < 0.55f && cuerpo != null; t += Time.deltaTime)
            {
                cuerpo.rotation = baseRot * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 38f) * 13f * (1f - t / 0.55f));
                yield return null;
            }
            sacudiendo = false;
        }

        IEnumerator Reventar()
        {
            fuente.PlayOneShot(SonidosTema.Pop(), volumen);
            var origen = cuerpo.position;
            reventado = true;

            // Pedacitos de goma que salen volando y un aro de luz que se abre.
            const int n = 9;
            var trozos = new Transform[n];
            var vel = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(go.GetComponent<Collider>());
                go.GetComponent<Renderer>().sharedMaterial = matColor;
                go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trozos[i] = go.transform;
                trozos[i].position = origen;
                trozos[i].localScale = new Vector3(0.03f, 0.01f, 0.018f);
                trozos[i].rotation = Random.rotation;
                float a = i * Mathf.PI * 2f / n + Random.Range(-0.2f, 0.2f);
                vel[i] = (transform.right * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a)) * Random.Range(0.6f, 1.0f) + transform.up * 0.3f;
            }
            var aro = MallasProcedurales.Crear("Destello", MallasProcedurales.Anillo(0.085f, 0.1f), matDorado, null);
            aro.transform.position = origen + transform.up * 0.02f;
            var cam = Camera.main;
            if (cam != null) aro.transform.rotation = Quaternion.LookRotation(aro.transform.position - cam.transform.position);

            for (float t = 0f; t < 0.4f; t += Time.deltaTime)
            {
                float k = t / 0.4f;
                for (int i = 0; i < n; i++)
                {
                    vel[i] += Vector3.down * 2f * Time.deltaTime;
                    trozos[i].position += vel[i] * Time.deltaTime;
                    trozos[i].localScale = new Vector3(0.03f, 0.01f, 0.018f) * (1f - k);
                }
                aro.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.9f, Mathf.Sqrt(k)) * (k < 0.7f ? 1f : (1f - k) / 0.3f);
                yield return null;
            }
            foreach (var tr in trozos) Destroy(tr.gameObject);
            Destroy(aro);
        }

        IEnumerator Sonar()
        {
            fuente.PlayOneShot(SonidosTema.Tambor(colorJuego), volumen);
            apretar = 1f;

            // El parche brilla un instante.
            var claro = Color.Lerp(colorActual, Color.white, 0.72f);
            // Ondas de luz sobre el parche y tres notas musicales que suben.
            var ondas = new GameObject[2];
            for (int i = 0; i < 2; i++)
            {
                ondas[i] = MallasProcedurales.Crear("Onda", MallasProcedurales.Anillo(0.05f, 0.058f), matDorado, cuerpo);
                ondas[i].transform.localPosition = new Vector3(0f, AltoTambor / 2f + 0.006f, 0f);
                ondas[i].transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            var notas = new Transform[3];
            var inicio = new Vector3[3];
            for (int i = 0; i < 3; i++)
            {
                notas[i] = CrearNota().transform;
                inicio[i] = transform.TransformPoint(new Vector3(-0.07f + i * 0.07f, 0.12f, 0.02f + 0.02f * i));
                notas[i].position = inicio[i];
                notas[i].localScale = Vector3.zero;
            }
            var cam = Camera.main;

            for (float t = 0f; t < 1.2f; t += Time.deltaTime)
            {
                float k = t / 1.2f;
                var brillo = Color.Lerp(Color.white, claro, Mathf.Clamp01(t / 0.35f));
                matClaro.SetColor("_BaseColor", brillo);
                for (int i = 0; i < 2; i++)
                {
                    float ki = Mathf.Clamp01(t / 0.45f - i * 0.4f);
                    ondas[i].transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.45f, ki) * (ki > 0f && ki < 1f ? 1f : 0f);
                }
                for (int i = 0; i < 3; i++)
                {
                    float ki = Mathf.Clamp01(k * 1.3f - i * 0.12f);
                    notas[i].position = inicio[i] + Vector3.up * 0.22f * ki + transform.right * Mathf.Sin(ki * 6f + i) * 0.015f;
                    float esc = ki < 0.15f ? ki / 0.15f : (ki > 0.75f ? (1f - ki) / 0.25f : 1f);
                    notas[i].localScale = Vector3.one * esc;
                    if (cam != null) notas[i].rotation = Quaternion.LookRotation(notas[i].position - cam.transform.position, Vector3.up);
                }
                yield return null;
            }
            matClaro.SetColor("_BaseColor", claro);
            foreach (var o in ondas) Destroy(o);
            foreach (var nt in notas) Destroy(nt.gameObject);
        }

        GameObject CrearNota()
        {
            var nota = new GameObject("Nota");
            var cabeza = Pieza(PrimitiveType.Sphere, nota.transform, Vector3.zero, new Vector3(0.03f, 0.022f, 0.012f), matDorado);
            cabeza.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
            Pieza(PrimitiveType.Cube, nota.transform, new Vector3(0.013f, 0.035f, 0f), new Vector3(0.005f, 0.07f, 0.005f), matDorado);
            Pieza(PrimitiveType.Cube, nota.transform, new Vector3(0.024f, 0.062f, 0f), new Vector3(0.022f, 0.007f, 0.005f), matDorado)
                .transform.localRotation = Quaternion.Euler(0f, 0f, -30f);
            return nota;
        }

        void OnDestroy()
        {
            foreach (var m in new[] { matColor, matClaro, matMetal, matHilo, matDorado })
                if (m != null) Destroy(m);
        }
    }
}
