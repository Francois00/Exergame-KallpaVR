using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace Exergame
{
    public class SesionExergame : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Los 4 circulos del tablero")]
        public BotonExergame[] botones = new BotonExergame[4];
        public PresentadorColores presentador;
        public CalibradorSentado calibrador;
        public PanelEvaluador panel;
        public ListaParticipantes lista;
        public GuionSesion guion;

        [Header("Seleccion del supervisor")]
        public Transform raizSeleccion;
        public Material materialBotonMenu;
        [Tooltip("Segundos para pasar el visor al participante cuando se elige en el menu dentro del visor.")]
        public float segundosColocarVisor = 5f;
        [Tooltip("Segundos de espera cuando se inicia desde el celular (el participante ya tiene el visor puesto).")]
        public float segundosInicioDesdePanel = 3f;
        [Tooltip("En el editor de Unity (pruebas en la PC) mostrar el menu de participantes en la escena, para elegir con el mouse.")]
        public bool mostrarMenuEnEditor = true;
        public int sesionesRV = 6;

        [Header("Audio de colores (Rojo, Azul, Verde, Amarillo)")]
        public AudioClip[] vocesColores = new AudioClip[4];

        [Header("Audio de instrucciones")]
        public AudioClip audioBienvenida;
        [Tooltip("La presentadora se presenta y desea suerte (despues del saludo). Opcional.")]
        public AudioClip audioPresentacion;
        [Tooltip("Texto del globo de dialogo cuando la presentadora se presenta.")]
        [TextArea] public string textoPresentacion = "¡Hola! Yo le voy a ayudar en el juego.\n¡Siga mis indicaciones!";
        [Tooltip("Si no hay audio de presentacion, cuanto tiempo se muestra el globo.")]
        public float segundosGloboSinAudio = 5f;
        public bool mostrarPresentacion = true;

        [Header("Practica antes de jugar: un globo a la izquierda y otro a la derecha")]
        public bool practicaAlcance = true;
        public AudioClip audioPracticaInicio;
        public AudioClip audioPracticaIzquierda;
        public AudioClip audioPracticaDerecha;
        public AudioClip audioPracticaFin;
        [Tooltip("Cuando toca el globo con la mano equivocada.")]
        public AudioClip audioPracticaOtraMano;
        [Tooltip("Si nadie toca el globo en este tiempo, se sigue igual (tambien se puede saltar desde el panel).")]
        public float segundosMaxPorGlobo = 25f;
        [Tooltip("Colores que no se usan en el juego, para no confundir.")]
        public Color colorGloboIzquierdo = new Color(0.56f, 0.27f, 0.88f);   // morado
        public Color colorGloboDerecho = new Color(1.0f, 0.52f, 0.06f);      // naranja
        public AudioClip audioEsteEs;
        public AudioClip audioExplicacion;
        [Tooltip("Recordatorio corto de la regla para las sesiones 2 en adelante.")]
        public AudioClip audioRecordatorio;
        public AudioClip audioCambioRegla;
        public AudioClip audioContinuamos;
        public AudioClip audioFin;

        [Header("Escenario (lo elige el evaluador en el panel)")]
        [Tooltip("Clasico = circulos en la sala. Globos = plaza de Arequipa. Tambores = jardin. La tarea es la misma.")]
        public Escenario escenario = Escenario.Clasico;

        [Header("Alcance del tablero (adultos mayores sentados)")]
        [Tooltip("Cuanto se acerca el tablero a la persona respecto a como esta en la escena (metros).")]
        public float acercarTablero = 0.15f;
        [Tooltip("Cuanto se baja el tablero (metros).")]
        public float bajarTablero = 0.04f;
        [Tooltip("Inclinacion del panel de 4 casilleros: -90 = vertical. Mas vertical = la fila de arriba queda mas cerca.")]
        public float inclinacionPanel = -78f;
        [Tooltip("Al acercar el tablero, la presentadora se corre un poco hacia afuera para que no quede tapada. El letrero se queda en su sitio.")]
        public Vector3 moverPresentadora = new Vector3(0.16f, 0f, -0.06f);

        [Header("Modo de alcance (lo elige el evaluador en el panel)")]
        [Tooltip("Proximidad = maqueta cerca, mano real. Completa = maqueta lejos, mano adelantada 15 cm. Limitaciones = maqueta lejos, palo de 30 cm.")]
        public ModoAlcance modo = ModoAlcance.Proximidad;
        [Tooltip("Movilidad completa y limitaciones: cuanto se aleja la maqueta respecto a proximidad (m), para que se vean los 4 objetos y el letrero a la vez.")]
        public float alejarMaquetaModos = 0.25f;
        [Tooltip("Movilidad completa y limitaciones: cuanto se mueve la presentadora con su letrero, para quedar detras de la maqueta.")]
        public Vector3 moverPresentadoraModos = new Vector3(0f, 0f, 0.25f);

        [Header("Manos libres: tocar sin limite para acostumbrarse (no se registra)")]
        public AudioClip audioManosLibresClasico;
        public AudioClip audioManosLibresGlobos;
        public AudioClip audioManosLibresTambores;
        [Tooltip("Segundos hasta que el circulo, globo o tambor vuelve a estar listo para tocarlo otra vez.")]
        public float segundosReponerManosLibres = 1.2f;

        [Header("Si se equivoca seguido, la presentadora repite la regla")]
        [Tooltip("Errores seguidos (con la misma regla) para repetir la instruccion. 0 = no repetir.")]
        public int erroresSeguidosParaRepetir = 2;
        [Tooltip("Recordatorio de la regla 'toque el color que ve'. Si falta, se usa el audio del cambio de regla.")]
        public AudioClip audioRecuerdeMuestra;

        [Header("Motivacion")]
        [Tooltip("Cada 3 aciertos seguidos: aplausos, mas confeti y '¡3 seguidos!'.")]
        public bool felicitarRachas = true;
        [Tooltip("Si se equivoca: un animo suave en texto (sin sonido de error).")]
        public bool animarEnErrores = true;
        [Tooltip("Tira de estrellas delante del tablero que se llena con los aciertos.")]
        public bool mostrarProgreso = true;
        [Tooltip("Al terminar: '¡Felicitaciones! Hoy acerto X de 16' con fanfarria y aplausos.")]
        public bool celebrarFinal = true;

        [Header("Opciones")]
        [Tooltip("Mostrar y nombrar cada color antes de empezar. Recomendado en la primera sesion de RV.")]
        public bool familiarizarEnPrimeraSesion = true;
        public float pausaEntreEnsayos = 2f;
        [Tooltip("Segundos maximos para responder. Si no responde, se registra 'sin_respuesta' y pasa al siguiente ensayo.")]
        public float segundosParaResponder = 10f;
        [Tooltip("Si esta marcado, una respuesta despues de la ventana del guion (4-6 s) cuenta como 'tardia'. Desmarcado: vale cualquier respuesta dentro de los segundos para responder.")]
        public bool marcarTardiaSegunGuion = false;
        [Tooltip("Entre ensayos, si la cabeza esta girada mas de estos grados, el tablero vuelve suavemente al frente.")]
        public float gradosRecentrarEntreEnsayos = 0f;
        [Tooltip("Sonido alegre, confeti y '¡Muy bien!' en cada acierto.")]
        public bool felicitarAciertos = true;
        [Tooltip("Segundos que se queda la pantalla final antes de volver al menu del supervisor.")]
        public float segundosAntesDeVolverAlMenu = 4f;
        [Range(0f, 1f)] public float umbralErrorNivel = 0.5f;

        public bool EnCurso { get; private set; }
        public bool Pausado => pausaFoco || pausaManual;

        string fase = "menu";
        bool pausaFoco, pausaManual, terminar, saltar;
        Participante elegidoPanel;
        int sesionPanel;
        Escenario? escenarioPanel;
        string tipoPanel;
        string tipoSesion = "real";
        ModoAlcance? modoPanel;
        Actividad? actividadPanel;
        Actividad actividad = Actividad.Prueba;
        bool pasarAPrueba, yaSePresento, manosLibresActivas;
        int toquesLibres, erroresSeguidos;
        Vector3 tableroBase, presentadorBase;
        ProgresoLogros progreso;
        int racha;
        EnsayoGuion ensayoActual;

        Participante participante;
        int numeroSesion = 1;
        RegistroSesion registro;
        BotonExergame tocado;
        Mano manoTocado;
        float tiempoToque;
        bool escuchando;
        bool interrumpido;
        Transform[] manos;
        Vector3[] ultimaPosicion;
        float recorrido;

        void OnEnable() { BotonExergame.AlgunBotonEmpujado += AlEmpujar; }
        void OnDisable() { BotonExergame.AlgunBotonEmpujado -= AlEmpujar; }

        void Awake() { Application.runInBackground = true; }

        void OnApplicationFocus(bool foco)
        {
            // En la PC (editor), hacer clic en el panel del navegador no debe pausar el juego.
            if (Application.isEditor) return;
            if (!EnCurso) return;
            if (!foco)
            {
                pausaFoco = true;
                if (escuchando) interrumpido = true;
                registro?.EscribirResumen(false);
            }
            else
            {
                pausaFoco = false;
            }
        }

        void OnApplicationPause(bool pausa) { OnApplicationFocus(!pausa); }

        IEnumerator Start()
        {
            Habilitar(false);
            var mv = FindObjectsOfType<ManoVirtual>();
            manos = mv.Select(m => m.transform).ToArray();
            ultimaPosicion = new Vector3[manos.Length];

            if (Application.isEditor && GetComponent<PruebaConMouse>() == null) gameObject.AddComponent<PruebaConMouse>();
            yield return new WaitForSeconds(0.5f);
            tablero = GameObject.Find("TableroFrontal");
            AjustarAlcance();
            if (tablero != null) tableroBase = tablero.transform.position;
            if (presentador != null) presentadorBase = presentador.transform.position;

            // Ciclo: menu -> (manos libres) -> prueba -> vuelve al menu para el siguiente participante.
            while (true)
            {
                PrepararSiguiente();
                if (tablero != null) tablero.SetActive(false);
                if (presentador != null) presentador.gameObject.SetActive(false);
                yield return MenuSupervisor();
                ColocarSegunModo();
                AplicarEscenario(escenario);
                // Con presentacion, el tablero aparece despues de que la presentadora se presenta.
                if (tablero != null) tablero.SetActive(actividad == Actividad.Prueba && !mostrarPresentacion && !practicaAlcance);
                if (presentador != null) presentador.gameObject.SetActive(true);
                fase = "colocando";
                yield return new WaitForSeconds(elegidoPanel != null ? segundosInicioDesdePanel : segundosColocarVisor);
                // Centra cuando la persona ya tiene el visor y la cabeza quieta (no mientras lo acomodan).
                if (calibrador != null) yield return calibrador.RecentrarCuandoQuieto(4f);
                yield return new WaitForSeconds(0.3f);
                if (actividad == Actividad.ManosLibres)
                {
                    yield return ManosLibres();
                    // Desde el panel se puede pasar directo a la prueba, sin volver al menu.
                    if (pasarAPrueba && !terminar)
                    {
                        actividad = Actividad.Prueba;
                        yield return new WaitForSeconds(0.6f);
                        yield return Sesion();
                    }
                }
                else yield return Sesion();
                yield return new WaitForSeconds(segundosAntesDeVolverAlMenu);
            }
        }

        // Deja todo listo para elegir otro participante.
        void PrepararSiguiente()
        {
            fase = "menu";
            terminar = false;
            pausaManual = false;
            interrumpido = false;
            escuchando = false;
            elegidoPanel = null;
            sesionPanel = 0;
            escenarioPanel = null;
            tipoPanel = null;
            modoPanel = null;
            actividadPanel = null;
            actividad = Actividad.Prueba;
            pasarAPrueba = false;
            yaSePresento = false;
            manosLibresActivas = false;
            toquesLibres = 0;
            erroresSeguidos = 0;
            if (progreso != null) progreso.Limpiar();
            ensayoActual = null;
            tocado = null;
            Habilitar(false);
            if (presentador != null) presentador.BajarCirculo();
        }

        // Tablero mas cerca, mas bajo y mas vertical: la fila de arriba se alcanza sin estirarse tanto.
        // (Mueve todo el tablero junto: casilleros, zonas de toque y globos de practica.)
        bool alcanceAjustado;
        void AjustarAlcance()
        {
            if (alcanceAjustado || tablero == null) return;
            alcanceAjustado = true;
            tablero.transform.position += new Vector3(0f, -bajarTablero, -acercarTablero);
            var panelBotones = tablero.transform.Find("PanelBotones");
            if (panelBotones != null) panelBotones.localRotation = Quaternion.Euler(inclinacionPanel, 0f, 0f);
            if (presentador != null && moverPresentadora != Vector3.zero)
            {
                var letrero = presentador.circulo;
                Vector3 posLetrero = letrero != null ? letrero.position : Vector3.zero;
                presentador.transform.position += moverPresentadora;
                if (letrero != null && letrero.IsChildOf(presentador.transform)) letrero.position = posLetrero;
            }
            Debug.Log($"[Exergame] Tablero acercado {acercarTablero:0.00} m, bajado {bajarTablero:0.00} m, inclinacion {inclinacionPanel:0}.");
        }

        // Proximidad: maqueta cerca (como siempre). Completa y limitaciones: la maqueta y la presentadora
        // (con su letrero) se van mas atras, para que se vean los 4 objetos y el color del letrero a la vez.
        void ColocarSegunModo()
        {
            ManoVirtual.FijarModo(modo);
            bool lejos = ModosJuego.MaquetaLejos(modo);
            if (tablero != null) tablero.transform.position = tableroBase + (lejos ? Vector3.forward * alejarMaquetaModos : Vector3.zero);
            if (presentador != null) presentador.transform.position = presentadorBase + (lejos ? moverPresentadoraModos : Vector3.zero);
            Felicitacion.Desplazamiento = lejos ? new Vector3(0f, -0.03f, alejarMaquetaModos * 0.8f) : Vector3.zero;
            if (progreso != null) progreso.transform.position = PosicionProgreso();
            Debug.Log($"[Exergame] Modo {ModosJuego.Nombre(modo)}: maqueta {(lejos ? "lejos (+" + alejarMaquetaModos.ToString("0.00") + " m)" : "cerca")}.");
        }

        Vector3 PosicionProgreso() =>
            new Vector3(0f, 0.745f, 0.525f - acercarTablero + (ModosJuego.MaquetaLejos(modo) ? alejarMaquetaModos : 0f));

        // Para el panel: si durante la sesion la persona mira mucho rato hacia un costado, se avisa al evaluador.
        float yawSuave;
        public bool Descentrado => EnCurso && Mathf.Abs(yawSuave) > 25f;

        void Update()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                var f = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
                if (f.sqrMagnitude > 0.01f)
                    yawSuave = Mathf.Lerp(yawSuave, Vector3.SignedAngle(Vector3.forward, f, Vector3.up), 1f - Mathf.Exp(-Time.deltaTime * 0.5f));
            }
            if (!escuchando || manos == null) return;
            for (int i = 0; i < manos.Length; i++)
            {
                if (manos[i] == null) continue;
                recorrido += Vector3.Distance(manos[i].position, ultimaPosicion[i]);
                ultimaPosicion[i] = manos[i].position;
            }
        }

        IEnumerator MenuSupervisor()
        {
            var opciones = lista != null ? lista.participantes : new List<Participante>();
            if (opciones.Count == 0) opciones = new List<Participante> { new Participante { codigo = "P00", nombre = "Prueba" } };

            Participante elegido = null;
            string acceso = panel != null ? $"\nPanel en el celular: {panel.Url}   PIN {panel.Pin}" : "";

            // El residente solo ve una bienvenida: el evaluador inicia todo desde el panel (celular o laptop).
            // El link y el PIN van en un cartel pequeno DETRAS de la persona (solo lo ve quien se pone el visor
            // y gira, o sea el evaluador al preparar el equipo).
            // Respaldo sin panel: mantener los dos botones laterales (grip) 3 s; en la PC, la tecla M.
            {
                var espera = CrearBienvenida();
                Transform cartel = null;
                if (panel != null)
                {
                    cartel = new GameObject("CartelEvaluador").transform;
                    var cam = Camera.main != null ? Camera.main.transform : null;
                    var baseP = cam != null ? new Vector3(cam.position.x, 1.25f, cam.position.z) : new Vector3(0f, 1.25f, 0f);
                    var atras = cam != null ? -Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized : Vector3.back;
                    if (atras.sqrMagnitude < 0.01f) atras = Vector3.back;
                    cartel.position = baseP + atras * 1.1f;
                    cartel.rotation = Quaternion.LookRotation(atras, Vector3.up);
                    CrearTexto(cartel, Vector3.zero, new Vector2(0.9f, 0.16f),
                        panel.SinRed
                            ? $"Evaluador: visor sin WiFi. Cable USB y en la laptop: {panel.Url}\nPIN {panel.Pin}"
                            : $"Evaluador - panel: {panel.Url}\nPIN {panel.Pin}", 0.3f);
                }
                float desde = -1f;
                while (elegidoPanel == null)
                {
                    if (AmbosGripsPresionados())
                    {
                        if (desde < 0f) desde = Time.time;
                        else if (Time.time - desde >= 3f) break;
                    }
                    else desde = -1f;
                    if (Application.isEditor && UnityEngine.InputSystem.Keyboard.current != null &&
                        UnityEngine.InputSystem.Keyboard.current.mKey.wasPressedThisFrame) break;
                    yield return null;
                }
                Destroy(espera.gameObject);
                if (cartel != null) Destroy(cartel.gameObject);
            }

            var menu = CrearMenu("Supervisor: elija al participante" + acceso);
            if (elegidoPanel != null) menu.gameObject.SetActive(false);
            for (int i = 0; i < opciones.Count; i++)
            {
                var p = opciones[i];
                int fila = i / 2, col = i % 2;
                CrearOpcion(menu, new Vector3(col == 0 ? -0.2f : 0.2f, 0.06f - fila * 0.1f, 0f), p.codigo + "  " + p.nombre, () => elegido = p);
            }
            while (elegido == null && elegidoPanel == null) yield return null;
            Destroy(menu.gameObject);

            int sesion = 0;
            if (elegidoPanel != null)
            {
                elegido = elegidoPanel;
                sesion = sesionPanel;
                if (escenarioPanel.HasValue) escenario = escenarioPanel.Value;
                tipoSesion = tipoPanel ?? "real";
                if (modoPanel.HasValue) modo = modoPanel.Value;
                actividad = actividadPanel ?? Actividad.Prueba;
            }
            else
            {
            menu = CrearMenu(elegido.codigo + " - Numero de sesion RV");
            for (int i = 1; i <= sesionesRV; i++)
            {
                int n = i;
                int fila = (i - 1) / 3, col = (i - 1) % 3;
                CrearOpcion(menu, new Vector3(-0.27f + col * 0.27f, 0.06f - fila * 0.1f, 0f), "Sesion " + n, () => sesion = n, 0.24f);
            }
            while (sesion == 0 && elegidoPanel == null) yield return null;
            if (sesion == 0) sesion = sesionPanel;
            Destroy(menu.gameObject);

            if (elegidoPanel == null)
            {
                Escenario? elegidoEsc = null;
                menu = CrearMenu(elegido.codigo + " - Escenario");
                var opcionesEsc = new[] { Escenario.Clasico, Escenario.Globos, Escenario.Tambores };
                for (int i = 0; i < opcionesEsc.Length; i++)
                {
                    var esc = opcionesEsc[i];
                    CrearOpcion(menu, new Vector3(-0.3f + i * 0.3f, 0.06f, 0f), Escenarios.Nombre(esc), () => elegidoEsc = esc, 0.28f);
                }
                while (elegidoEsc == null && elegidoPanel == null) yield return null;
                if (elegidoEsc.HasValue) escenario = elegidoEsc.Value;
                // Desde el menu de la escena (pruebas en la PC) se marca como prueba; se puede cambiar en el panel.
                tipoSesion = Application.isEditor ? "prueba" : "real";
                Destroy(menu.gameObject);
            }
            }

            participante = elegido;
            numeroSesion = sesion;
            if (!participante.combinatoria.EsValida())
                Debug.LogError("[Exergame] La combinatoria de " + participante.codigo + " repite colores. Revisar Participantes.asset");
            Debug.Log($"[Exergame] {participante.codigo}, sesion RV {numeroSesion}, escenario {escenario}, modo {ModosJuego.Nombre(modo)}, " +
                      $"{(actividad == Actividad.ManosLibres ? "manos libres" : "prueba")}. Coloque el visor al participante.");
        }

        IEnumerator Sesion()
        {
            EnCurso = true;
            fase = "instrucciones";
            registro = new RegistroSesion(participante.codigo, participante.combinatoria.Texto(), numeroSesion)
            {
                umbralErrorNivel = umbralErrorNivel,
                guionProvisional = guion == null || guion.provisional,
                totalPlaneados = guion != null ? guion.ensayos.Count : 0,
                escenario = escenario.ToString(),
                tipo = tipoSesion,
                modo = modo.ToString()
            };
            racha = 0;
            erroresSeguidos = 0;
            if (mostrarProgreso)
            {
                if (progreso == null) progreso = ProgresoLogros.Crear(PosicionProgreso());
                progreso.transform.position = PosicionProgreso();
                progreso.Iniciar(guion != null ? guion.ensayos.Count : 16);
            }

            foreach (var b in botones)
            {
                b.AsignarColor(participante.combinatoria.En(b.posicion));
                b.FijarActivo(true);
                b.Reponer();
            }

            // 1) La presentadora sola (sin tablero) saluda y se presenta.
            // 2) Aparece el tablero y empieza el juego.
            bool yaSaludo = false;
            presentador.Saludar();
            // Si antes hubo manos libres, la presentadora ya se presento: no se repite.
            if (mostrarPresentacion && !yaSePresento)
            {
                fase = "presentacion";
                yield return Presentacion();
                yaSaludo = audioPresentacion != null;
            }
            else if (yaSePresento) yaSaludo = true;
            if (practicaAlcance && !terminar)
            {
                fase = "practica";
                yield return Practica();
                yaSaludo |= audioPracticaFin != null;
            }
            if (mostrarPresentacion || practicaAlcance)
            {
                yield return AparecerTablero();
                fase = "instrucciones";
            }
            var saludo = participante.saludo != null ? participante.saludo : (yaSaludo ? null : audioBienvenida);
            yield return Hablar(saludo);
            yield return new WaitForSeconds(0.3f);

            if (familiarizarEnPrimeraSesion && numeroSesion == 1)
                yield return Familiarizar();

            bool primeraVez = numeroSesion == 1 || audioRecordatorio == null;
            yield return Hablar(primeraVez ? audioExplicacion : audioRecordatorio);
            yield return new WaitForSeconds(0.6f);

            var ensayos = guion != null ? guion.ensayos : new List<EnsayoGuion>();
            Regla reglaAnterior = Regla.Escucha;
            for (int i = 0; i < ensayos.Count; i++)
            {
                if (terminar) break;
                var e = ensayos[i];
                ensayoActual = e;
                if (e.regla != reglaAnterior)
                {
                    yield return new WaitForSeconds(0.5f);
                    yield return Hablar(audioCambioRegla);
                    yield return new WaitForSeconds(1.2f);
                    reglaAnterior = e.regla;
                    erroresSeguidos = 0;
                }

                yield return EsperarSiPausado();
                if (terminar) break;
                fase = "ensayo";
                yield return Ensayo(e);
                if (terminar) break;
                if (interrumpido)
                {
                    if (terminar) break;
                    i--;
                    yield return EsperarSiPausado();
                    if (calibrador != null) calibrador.Recentrar();
                    yield return Hablar(audioContinuamos);
                    yield return new WaitForSeconds(1f);
                    continue;
                }
                // Entre ensayos: si la persona quedo mirando de costado, se le vuelve a poner todo al frente.
                if (calibrador != null && gradosRecentrarEntreEnsayos > 0f) calibrador.RecentrarSuaveSiDesviado(gradosRecentrarEntreEnsayos);
                yield return new WaitForSeconds(pausaEntreEnsayos);
                // Se equivoco varias veces seguidas: la presentadora le recuerda la regla de ahora
                // (si el siguiente ensayo sigue con la misma regla; si cambia, ya se anuncia el cambio).
                bool mismaReglaDespues = i + 1 < ensayos.Count && ensayos[i + 1].regla == e.regla;
                if (erroresSeguidosParaRepetir > 0 && erroresSeguidos >= erroresSeguidosParaRepetir && mismaReglaDespues && !terminar)
                {
                    erroresSeguidos = 0;
                    yield return EsperarSiPausado();
                    yield return RepetirRegla(e.regla);
                }
            }

            foreach (var b in botones) { b.FijarActivo(true); b.Reponer(); }
            registro.EscribirResumen(!terminar);
            fase = "fin";
            ensayoActual = null;
            if (celebrarFinal && !terminar)
            {
                if (presentador != null) presentador.Celebrar();
                yield return Felicitacion.Final(this, registro.Resultados.Count(x => x.correcto), registro.Resultados.Count, MaterialBase(), Efectos());
            }
            yield return Hablar(audioFin);
            if (participante.despedida != null) yield return Hablar(participante.despedida);
            EnCurso = false;
            Debug.Log("[Exergame] Sesion terminada.");
        }

        IEnumerator Familiarizar()
        {
            for (int c = 0; c < 4; c++)
            {
                presentador.LevantarCirculo(Colores.Valor((ColorJuego)c));
                yield return new WaitForSeconds(0.3f);
                yield return Hablar(audioEsteEs);
                yield return Hablar(vocesColores[c]);
                yield return new WaitForSeconds(0.4f);
                presentador.BajarCirculo();
                yield return new WaitForSeconds(0.35f);
            }
            yield return new WaitForSeconds(0.2f);
        }

        IEnumerator Ensayo(EnsayoGuion e)
        {
            interrumpido = false;
            var comb = participante.combinatoria;
            foreach (var b in botones)
            {
                b.Reponer();
                bool activo = !e.soloRojoAzul || b.ColorActual == ColorJuego.Rojo || b.ColorActual == ColorJuego.Azul;
                b.FijarActivo(activo);
            }

            var senalado = Boton(comb.Donde(e.Mostrado));
            var correcto = Boton(comb.Donde(e.Objetivo));

            tocado = null;
            manoTocado = Mano.Ninguna;
            recorrido = 0f;
            for (int i = 0; i < manos.Length; i++) if (manos[i] != null) ultimaPosicion[i] = manos[i].position;

            presentador.LevantarCirculo(Colores.Valor(e.Mostrado));
            presentador.Decir(vocesColores[(int)e.anunciado]);
            float t0 = Time.realtimeSinceStartup;
            escuchando = true;
            Habilitar(true);

            float limite = segundosParaResponder > 0f ? segundosParaResponder : float.MaxValue;
            float ventana = marcarTardiaSegunGuion && e.ventanaSegundos > 0f ? Mathf.Min(e.ventanaSegundos, limite) : limite;
            saltar = false;
            while (tocado == null && !interrumpido && !terminar && !saltar && Time.realtimeSinceStartup - t0 < limite) yield return null;
            if (terminar && tocado == null) interrumpido = true;

            escuchando = false;
            Habilitar(false);
            presentador.BajarCirculo();

            var r = new ResultadoEnsayo
            {
                numero = e.numero, nivel = e.nivel, bloque = e.bloque, regla = e.regla,
                anunciado = e.anunciado, mostrado = e.Mostrado, incongruente = e.incongruente,
                posicionSenalada = senalado.posicion, posicionCorrecta = correcto.posicion,
                recorridoManoM = recorrido, interrumpido = interrumpido
            };

            if (tocado != null)
            {
                float tr = (tiempoToque - t0) * 1000f;
                bool manoOk = (manoTocado == Mano.Izquierda) == tocado.LadoIzquierdo;
                bool circuloOk = tocado == correcto;
                r.hayRespuesta = true;
                r.posicionTocada = tocado.posicion;
                r.colorTocado = tocado.ColorActual;
                r.mano = manoTocado;
                r.manoCorrecta = manoOk;
                r.trMs = tr;
                r.siguioDistractor = e.incongruente && tocado == senalado && e.regla == Regla.Escucha
                                     || e.incongruente && tocado.ColorActual == e.anunciado && e.regla == Regla.Muestra;
                if (tr > ventana * 1000f) r.tipoError = "tardia";
                else if (!circuloOk) r.tipoError = "circulo_equivocado";
                else if (!manoOk) r.tipoError = "mano_equivocada";
                else r.tipoError = "";
                r.correcto = r.tipoError == "";
                if (r.correcto)
                {
                    racha++;
                    if (felicitarAciertos) tocado.Felicitar();
                    if (felicitarAciertos && presentador != null) presentador.Celebrar();
                    if (felicitarRachas && racha > 0 && racha % 3 == 0) Felicitacion.Racha(this, racha, MaterialBase(), Efectos());
                }
                else
                {
                    racha = 0;
                    tocado.Fallar();
                    if (animarEnErrores) Felicitacion.Animo(this);
                }
            }
            else
            {
                r.tipoError = interrumpido ? "interrumpido" : saltar ? "saltado_evaluador" : "sin_respuesta";
                if (!interrumpido) racha = 0;
            }

            registro.Agregar(r);
            if (progreso != null && !r.interrumpido) progreso.Marcar(registro.Resultados.Count - 1, r.correcto);
            // Errores seguidos (para repetir la regla). Un ensayo saltado por el evaluador no cuenta.
            if (!r.interrumpido && r.tipoError != "saltado_evaluador") erroresSeguidos = r.correcto ? 0 : erroresSeguidos + 1;
        }

        // "Recuerde: ..." con el globo de dialogo y la voz de la regla que se esta jugando.
        IEnumerator RepetirRegla(Regla regla)
        {
            if (presentador == null) yield break;
            if (registro != null) registro.repeticionesRegla++;
            fase = "repitiendo";
            Debug.Log("[Exergame] Varios errores seguidos: se repite la regla " + regla);
            presentador.MostrarGlobo(regla == Regla.Escucha
                ? "Recuerde:\ntoque el color\nque ESCUCHA."
                : "Recuerde:\ntoque el color que VE\nen el letrero.");
            var audio = regla == Regla.Escucha
                ? (audioRecordatorio != null ? audioRecordatorio : audioExplicacion)
                : (audioRecuerdeMuestra != null ? audioRecuerdeMuestra : audioCambioRegla);
            yield return HablarOEsperar(audio, 3f);
            yield return new WaitForSeconds(0.5f);
            presentador.OcultarGlobo();
            yield return new WaitForSeconds(0.4f);
        }

        // Manos libres: el residente toca los circulos, globos o tambores cuantas veces quiera para acostumbrarse.
        // Cada uno reacciona como en su escenario (cae, revienta, suena) y vuelve a aparecer. No se registra nada.
        // Termina con "Terminar" o "Pasar a la prueba" en el panel.
        IEnumerator ManosLibres()
        {
            EnCurso = true;
            registro = null;   // el panel no muestra resultados de la sesion anterior
            foreach (var b in botones)
            {
                b.AsignarColor(participante.combinatoria.En(b.posicion));
                b.FijarActivo(true);
                b.Reponer();
            }
            presentador.Saludar();
            if (mostrarPresentacion && !terminar)
            {
                fase = "presentacion";
                yield return Presentacion();
                yaSePresento = true;
            }
            yield return AparecerTablero();
            fase = "libre";
            if (!terminar && !pasarAPrueba)
            {
                presentador.MostrarGlobo(TextoManosLibres());
                yield return HablarOEsperar(AudioManosLibres(), 3.5f);
            }
            toquesLibres = 0;
            manosLibresActivas = true;
            while (!terminar && !pasarAPrueba)
            {
                Habilitar(!Pausado);
                yield return null;
            }
            manosLibresActivas = false;
            Habilitar(false);
            presentador.OcultarGlobo();
            foreach (var b in botones) b.Reponer();
            Debug.Log($"[Exergame] Manos libres terminado: {toquesLibres} toques.");
            if (!pasarAPrueba)
            {
                fase = "fin";
                yield return Hablar(audioFin);
            }
            EnCurso = false;
        }

        string TextoManosLibres()
        {
            switch (escenario)
            {
                case Escenario.Globos: return "¡Vamos a practicar!\nReviente los globos\nlas veces que quiera.";
                case Escenario.Tambores: return "¡Vamos a practicar!\nToque los tambores\nlas veces que quiera.";
                default: return "¡Vamos a practicar!\nEmpuje los círculos\nlas veces que quiera.";
            }
        }

        AudioClip AudioManosLibres()
        {
            switch (escenario)
            {
                case Escenario.Globos: return audioManosLibresGlobos;
                case Escenario.Tambores: return audioManosLibresTambores;
                default: return audioManosLibresClasico;
            }
        }

        IEnumerator ToqueLibre(BotonExergame boton)
        {
            toquesLibres++;
            boton.Felicitar();
            yield return new WaitForSeconds(segundosReponerManosLibres);
            if (manosLibresActivas) boton.Reponer();
        }

        BotonExergame Boton(Posicion p) => botones.First(b => b.posicion == p);

        public void PausarDesdePanel()
        {
            if (!EnCurso) return;
            pausaManual = true;
            if (escuchando) interrumpido = true;
        }

        public void ContinuarDesdePanel()
        {
            pausaManual = false;
            pausaFoco = false;
        }

        public void TerminarDesdePanel()
        {
            if (!EnCurso && fase != "colocando") return;
            terminar = true;
            pausaManual = false;
            pausaFoco = false;
        }

        public void RecentrarDesdePanel()
        {
            if (calibrador != null) calibrador.Recentrar();
        }

        // Repite la instruccion (solo fuera de un ensayo, para no alterar el tiempo de reaccion).
        public void RepetirDesdePanel()
        {
            if (escuchando || presentador == null) return;
            if (fase == "libre") { presentador.Decir(AudioManosLibres()); return; }
            bool primeraVez = numeroSesion == 1 || audioRecordatorio == null;
            presentador.Decir(fase == "ensayo" && ensayoActual != null && ensayoActual.regla == Regla.Muestra
                ? (audioRecuerdeMuestra != null ? audioRecuerdeMuestra : audioCambioRegla)
                : (primeraVez ? audioExplicacion : audioRecordatorio));
        }

        /// <summary>Desde manos libres, empezar la prueba con el mismo participante, modo y escenario.</summary>
        public void PasarAPruebaDesdePanel()
        {
            if (fase == "libre" || (actividad == Actividad.ManosLibres && fase == "presentacion")) pasarAPrueba = true;
        }

        public void SaltarEnsayoDesdePanel()
        {
            if (escuchando || enPractica) saltar = true;
        }

        public void VolumenDesdePanel(float delta)
        {
            AudioListener.volume = Mathf.Clamp(AudioListener.volume + delta, 0.2f, 1f);
        }

        public void IniciarDesdePanel(string codigo, int sesion, string escenarioTexto = null, string tipo = null,
                                      string modoTexto = null, string actividadTexto = null)
        {
            if (fase != "menu" || lista == null) return;
            var p = lista.participantes.FirstOrDefault(x => x.codigo == codigo);
            if (p == null || sesion < 1) return;
            sesionPanel = sesion;
            escenarioPanel = string.IsNullOrEmpty(escenarioTexto) ? (Escenario?)null : Escenarios.Leer(escenarioTexto, escenario);
            tipoPanel = tipo == "prueba" ? "prueba" : "real";
            modoPanel = string.IsNullOrEmpty(modoTexto) ? (ModoAlcance?)null : ModosJuego.LeerModo(modoTexto, modo);
            actividadPanel = ModosJuego.LeerActividad(actividadTexto);
            elegidoPanel = p;
        }

        /// <summary>Pone el escenario: objetos del tablero, letrero de la presentadora y fondo.</summary>
        public void AplicarEscenario(Escenario e)
        {
            escenario = e;
            var baseLit = MaterialBase();
            foreach (var b in botones)
            {
                if (b == null) continue;
                EstimuloTema.De(b).Aplicar(e, baseLit);
                b.FijarActivo(b.Activo);
            }
            if (presentador != null) presentador.FijarTema(e, baseLit);
            EntornoTema.Aplicar(e, baseLit, tablero != null ? tablero.transform : null);
            if (e != Escenario.Clasico && Camera.main != null && Camera.main.farClipPlane < 600f) Camera.main.farClipPlane = 600f;
            Debug.Log("[Exergame] Escenario: " + Escenarios.Nombre(e));
        }

        Material materialBaseCache;
        Material MaterialBase()
        {
            if (materialBaseCache == null && botones != null)
                foreach (var b in botones)
                {
                    var r = b != null ? b.tapa : null;
                    if (r != null) { materialBaseCache = r.sharedMaterial; break; }
                }
            return materialBaseCache;
        }

        AudioSource Efectos()
        {
            if (fuenteEfectos == null)
            {
                fuenteEfectos = gameObject.AddComponent<AudioSource>();
                fuenteEfectos.playOnAwake = false;
            }
            return fuenteEfectos;
        }

        public string EstadoJson()
        {
            var c = System.Globalization.CultureInfo.InvariantCulture;
            var sb = new System.Text.StringBuilder("{");
            var res = registro != null ? registro.Resultados : new List<ResultadoEnsayo>();
            int completados = res.Count;
            int aciertos = res.Count(r => r.correcto);
            var trs = res.Where(r => r.correcto && r.trMs >= 0).Select(r => r.trMs).ToList();
            int total = guion != null ? guion.ensayos.Count : 0;

            sb.Append($"\"escenario\":\"{escenario}\",\"tipo\":\"{tipoSesion}\",\"descentrado\":{(Descentrado ? "true" : "false")},");
            sb.Append($"\"modo\":\"{modo}\",\"actividad\":\"{(actividad == Actividad.ManosLibres ? "libre" : "prueba")}\",\"toquesLibres\":{toquesLibres},");
            sb.Append($"\"repeticionesRegla\":{(registro != null ? registro.repeticionesRegla : 0)},");
            sb.Append($"\"fase\":\"{fase}\",\"pausado\":{(Pausado ? "true" : "false")},\"escuchando\":{(escuchando ? "true" : "false")},");
            sb.Append($"\"volumen\":{Mathf.RoundToInt(AudioListener.volume * 100)},");
            sb.Append($"\"participante\":\"{PanelEvaluador.Escapar(participante?.codigo)}\",\"sesion\":{(participante != null ? numeroSesion : 0)},");
            sb.Append($"\"total\":{total},\"completados\":{completados},\"aciertos\":{aciertos},");
            sb.Append($"\"precision\":{(completados > 0 ? (aciertos * 100f / completados).ToString("0.0", c) : "0")},");
            sb.Append($"\"trPromedio\":{(trs.Count > 0 ? trs.Average().ToString("0", c) : "0")},");
            if (ensayoActual != null)
                sb.Append($"\"ensayo\":{ensayoActual.numero},\"nivel\":{ensayoActual.nivel},\"bloque\":\"{ensayoActual.bloque}\",\"regla\":\"{ensayoActual.regla}\",");
            else
                sb.Append("\"ensayo\":0,\"nivel\":0,\"bloque\":\"\",\"regla\":\"\",");
            sb.Append($"\"sesionesRV\":{sesionesRV},\"participantes\":[");
            if (lista != null)
                sb.Append(string.Join(",", lista.participantes.Select(p =>
                    $"{{\"codigo\":\"{PanelEvaluador.Escapar(p.codigo)}\",\"nombre\":\"{PanelEvaluador.Escapar(p.nombre)}\"}}")));
            sb.Append("],\"resultados\":[");
            sb.Append(string.Join(",", res.Select(r =>
                $"{{\"n\":{r.numero},\"nivel\":{r.nivel},\"bloque\":\"{r.bloque}\",\"regla\":\"{r.regla}\"," +
                $"\"anunciado\":\"{r.anunciado}\",\"mostrado\":\"{r.mostrado}\",\"incongruente\":{(r.incongruente ? "true" : "false")}," +
                $"\"tocado\":\"{(r.hayRespuesta ? r.colorTocado.ToString() : "")}\",\"mano\":\"{(r.mano == Mano.Ninguna ? "" : r.mano.ToString())}\"," +
                $"\"tr\":{r.trMs.ToString("0", c)},\"correcto\":{(r.correcto ? "true" : "false")},\"error\":\"{r.tipoError}\"}}")));
            sb.Append("]}");
            return sb.ToString();
        }


        IEnumerator Hablar(AudioClip audio)
        {
            if (audio == null) yield break;
            presentador.Decir(audio);
            yield return null;
            float limite = Time.time + audio.length + 2f;
            while (presentador.EstaHablando && Time.time < limite) yield return null;
        }

        GameObject tablero;
        bool enPractica;
        AudioSource fuenteEfectos;

        // Practica: el residente toca un globo a la izquierda y otro a la derecha, donde estaran
        // los circulos del tablero. Asi reconoce hasta donde estirar cada mano antes de jugar.
        // No se registra como ensayo.
        IEnumerator Practica()
        {
            if (fuenteEfectos == null)
            {
                fuenteEfectos = gameObject.AddComponent<AudioSource>();
                fuenteEfectos.playOnAwake = false;
            }
            Material materialBase = null;
            var r = botones != null && botones.Length > 0 ? botones[0].GetComponentInChildren<Renderer>(true) : null;
            if (r != null) materialBase = r.sharedMaterial;

            presentador.MostrarGlobo("Antes de empezar,\nvamos a practicar.");
            yield return HablarOEsperar(audioPracticaInicio, 2.5f);

            foreach (bool izquierda in new[] { true, false })
            {
                if (terminar) break;
                var lado = botones.Where(b => b.LadoIzquierdo == izquierda).ToArray();
                var pos = lado.Length > 0 ? lado.Aggregate(Vector3.zero, (s, b) => s + b.transform.position) / lado.Length
                                          : new Vector3(izquierda ? -0.125f : 0.125f, 1.0f, 0.67f);
                pos += Vector3.back * 0.03f;

                var globo = GloboPractica.Crear(pos, izquierda ? colorGloboIzquierdo : colorGloboDerecho, materialBase, izquierda,
                                                izquierda ? "IZQUIERDA" : "DERECHA");
                presentador.Senalar(globo.transform);
                string indicacion = izquierda ? "Toque el globo MORADO\ncon su mano IZQUIERDA."
                                              : "Muy bien. Ahora toque el globo\nNARANJA con su mano DERECHA.";
                presentador.MostrarGlobo(indicacion);
                enPractica = true;
                saltar = false;
                yield return Hablar(izquierda ? audioPracticaIzquierda : audioPracticaDerecha);

                float t0 = Time.time;
                int avisos = 0;
                while (!globo.Tocado && !terminar && !saltar && Time.time - t0 < segundosMaxPorGlobo)
                {
                    if (globo.IntentosManoEquivocada > avisos)
                    {
                        // Toco con la otra mano: se le recuerda con cual (y se le da mas tiempo).
                        avisos = globo.IntentosManoEquivocada;
                        presentador.MostrarGlobo(izquierda ? "¡Con la otra mano!\nUse su mano IZQUIERDA." : "¡Con la otra mano!\nUse su mano DERECHA.");
                        yield return HablarOEsperar(audioPracticaOtraMano, 2f);
                        if (globo.Tocado) break;
                        presentador.MostrarGlobo(indicacion);
                        yield return Hablar(izquierda ? audioPracticaIzquierda : audioPracticaDerecha);
                        t0 = Time.time;
                    }
                    yield return EsperarSiPausado();
                    yield return null;
                }
                enPractica = false;
                presentador.DejarDeSenalar();

                if (globo.Tocado)
                {
                    Felicitacion.Mostrar(this, globo.transform.position, materialBase, fuenteEfectos, 0.8f);
                    presentador.Celebrar();
                    Debug.Log("[Exergame] Practica: globo " + (izquierda ? "izquierdo" : "derecho") + " tocado con la mano " + (globo.ConManoIzquierda ? "izquierda" : "derecha"));
                }
                StartCoroutine(globo.Reventar());
                yield return new WaitForSeconds(1.2f);
            }

            presentador.MostrarGlobo("¡Excelente! Ya está listo.\n¡Vamos a jugar!");
            yield return HablarOEsperar(audioPracticaFin, 2.5f);
            presentador.OcultarGlobo();
        }

        IEnumerator HablarOEsperar(AudioClip audio, float segundos)
        {
            if (audio != null) yield return Hablar(audio);
            else yield return new WaitForSeconds(segundos);
        }

        IEnumerator AparecerTablero()
        {
            if (tablero == null || tablero.activeSelf) yield break;
            var escala = tablero.transform.localScale;
            tablero.transform.localScale = escala * 0.01f;
            tablero.SetActive(true);
            const float dur = 0.6f;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / dur);
                tablero.transform.localScale = escala * Mathf.Lerp(0.01f, 1f, k);
                yield return null;
            }
            tablero.transform.localScale = escala;
            yield return new WaitForSeconds(0.4f);
        }

        // La presentadora se presenta con un globo de dialogo (y su voz, si esta grabada).
        IEnumerator Presentacion()
        {
            if (!string.IsNullOrEmpty(textoPresentacion)) presentador.MostrarGlobo(textoPresentacion);
            if (audioPresentacion != null) yield return Hablar(audioPresentacion);
            else yield return new WaitForSeconds(segundosGloboSinAudio);
            yield return new WaitForSeconds(0.4f);
            presentador.OcultarGlobo();
        }

        IEnumerator EsperarSiPausado()
        {
            while (Pausado && !terminar) yield return null;
        }

        void AlEmpujar(BotonExergame boton, Mano mano)
        {
            if (manosLibresActivas) { StartCoroutine(ToqueLibre(boton)); return; }
            if (!escuchando || tocado != null) return;
            tocado = boton;
            manoTocado = mano;
            tiempoToque = Time.realtimeSinceStartup;
        }

        void Habilitar(bool activo)
        {
            foreach (var b in botones) if (b != null) b.Habilitado = activo;
        }

        static readonly List<UnityEngine.XR.InputDevice> controlesGrip = new List<UnityEngine.XR.InputDevice>();

        static bool AmbosGripsPresionados()
        {
            int presionados = 0;
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(UnityEngine.XR.InputDeviceCharacteristics.Controller, controlesGrip);
            foreach (var c in controlesGrip)
                if (c.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool g) && g) presionados++;
            return presionados >= 2;
        }

        [Header("Pantalla de espera")]
        [Tooltip("Distancia del cartel de bienvenida (m). Lejos para que no se sienta encima de la cara.")]
        public float distanciaBienvenida = 2.0f;
        public float alturaBienvenida = 1.2f;

        // Cartel de bienvenida: a 2 m, tamano moderado y con fondo claro (antes estaba a 75 cm y llenaba la vista).
        Transform CrearBienvenida()
        {
            var raiz = new GameObject("MenuSupervisor").transform;
            raiz.position = new Vector3(0f, alturaBienvenida, distanciaBienvenida);
            var fondo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fondo.name = "FondoBienvenida";
            Destroy(fondo.GetComponent<Collider>());
            fondo.transform.SetParent(raiz, false);
            fondo.transform.localPosition = new Vector3(0f, 0f, 0.01f);
            fondo.transform.localScale = new Vector3(1.2f, 0.42f, 1f);
            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(sh != null ? sh : Shader.Find("Unlit/Color"));
            var crema = new Color(0.98f, 0.96f, 0.91f);
            mat.SetColor("_BaseColor", crema);
            mat.color = crema;
            var r = fondo.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            CrearTexto(raiz, Vector3.zero, new Vector2(1.1f, 0.34f), "<b>¡Bienvenido!</b>\n<size=70%>En un momento empezamos.</size>", 1.2f);
            return raiz;
        }

        Transform CrearMenu(string titulo)
        {
            var raiz = new GameObject("MenuSupervisor").transform;
            if (raizSeleccion != null) raiz.SetPositionAndRotation(raizSeleccion.position, raizSeleccion.rotation);
            else raiz.position = new Vector3(0f, 1.25f, 0.75f);
            CrearTexto(raiz, new Vector3(0f, 0.19f, 0f), new Vector2(0.95f, 0.13f), titulo);
            return raiz;
        }

        void CrearOpcion(Transform raiz, Vector3 pos, string texto, System.Action alElegir, float ancho = 0.36f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Opcion";
            go.transform.SetParent(raiz, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(ancho, 0.08f, 0.02f);
            var rend = go.GetComponent<Renderer>();
            if (materialBotonMenu != null) rend.sharedMaterial = materialBotonMenu;
            var mat = rend.material;
            go.AddComponent<OpcionMenu>().alElegir = alElegir;
            var inter = go.AddComponent<XRSimpleInteractable>();
            inter.hoverEntered.AddListener(_ => mat.color = new Color(0.75f, 0.9f, 1f));
            inter.hoverExited.AddListener(_ => mat.color = Color.white);
            inter.selectEntered.AddListener(_ => alElegir());
            CrearTexto(raiz, pos + new Vector3(0f, 0f, -0.012f), new Vector2(ancho - 0.02f, 0.07f), texto);
        }

        static void CrearTexto(Transform padre, Vector3 pos, Vector2 tamano, string texto, float tamanoMax = 0.5f)
        {
            var go = new GameObject("Texto");
            go.transform.SetParent(padre, false);
            go.transform.localPosition = pos;
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.rectTransform.sizeDelta = tamano;
            tmp.text = texto;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0.1f;
            tmp.fontSizeMax = tamanoMax;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.1f, 0.1f, 0.12f);
        }
    }
}
