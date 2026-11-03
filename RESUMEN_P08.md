# Práctica 08 — Verificación del informe contra el proyecto real

Fecha: 2026-11-02 · Proyecto: Exergame VR (Unity 2022.3.62f3, namespace `Exergame`) · Visor: Meta Quest 2
Informe revisado: `Practica08_Implementacion_Integracion_Exergame_RV.docx` (fecha de elaboración 31/10/2026). Solo lectura, no se versiona.
Repositorio: https://github.com/Francois00/Exergame-KallpaVR

---

## (a) Tabla G — Control de versiones

| Elemento | Evidencia real |
|---|---|
| URL del repositorio | https://github.com/Francois00/Exergame-KallpaVR.git |
| Ramas | `main` (estable) e `incremento-p08` (trabajo del incremento) |
| Mecanismo de integración | `git merge --no-ff incremento-p08` en `main` (merge local, **no** pull request) |
| Etiqueta | `v0.8.0` (anotada): "Incremento Práctica 08: implementación e integración" |
| Convención de commits | `tipo: mensaje` (chore, docs, feat, merge) |
| Historial | ver tabla de commits y `EVIDENCIA_GIT.txt` |

| Commit | Rama | Mensaje | Contenido principal |
|---|---|---|---|
| `53c7244` | main | chore: .gitignore, README y estructura del repositorio | `.gitignore`, `README.md`, `.vsconfig` |
| `0275832` | incremento-p08 | docs: configuración de ejemplo y documentación | `docs/`, `Documentacion/`, `LEEME.txt` |
| `f264610` | incremento-p08 | feat: paquetes y ajustes del proyecto | `Packages/`, `ProjectSettings/`, `Assets/Settings`, `Assets/XR`, `Assets/XRI` |
| `2cb477a` | incremento-p08 | feat: recursos de la plantilla VR de Unity | `Assets/Samples`, `Assets/VRTemplateAssets`, `Assets/TextMesh Pro`, `Assets/Scenes` |
| `6eb7e94` | incremento-p08 | feat: scripts del juego, guion y participantes | `Assets/Exergame/Scripts/*` (excepto panel y registro), `GuionSesion.asset`, `Participantes.asset` |
| `3053e13` | incremento-p08 | feat: panel web, registro CSV y resumen de métricas | `PanelEvaluador.cs`, `RegistroSesion.cs`, `ArchivoDatos.cs`, `Resources/panel_evaluador.html` |
| `b207653` | incremento-p08 | feat: escenas, recursos y audio | `Scenes/SalaExergame.unity`, `Audio/Voz`, `Personajes/Mousey`, `Materiales`, `Texturas` |
| `4529a42` | incremento-p08 | chore: herramientas de editor | `Assets/Exergame/Editor/*`, `Assets/Exergame/Herramientas/Editor/*` |
| (ver EVIDENCIA_GIT.txt) | incremento-p08 | docs: matriz de trazabilidad y script de evidencia | `RESUMEN_P08.md`, `tools/evidencia_git.sh` |
| (ver EVIDENCIA_GIT.txt) | main | merge: incremento Práctica 08 | integración `--no-ff` |

---

## (b) Matriz I — Trazabilidad (rutas reales bajo `Assets/Exergame/`)

| Req. | Componente | Archivo real | Commit | Evidencia | Estado |
|---|---|---|---|---|---|
| RF-01 Registrar aciertos y errores | Control de sesión; Registro | `Scripts/SesionExergame.cs` (`Ensayo()`: clasificación); `Scripts/RegistroSesion.cs` | `6eb7e94`; `3053e13` | Columnas `correcto`, `tipo_error` del `_ensayos.csv` | Implementado |
| RF-02 Tiempo de reacción | Control de sesión | `Scripts/SesionExergame.cs` (TR = (`tiempoToque` − `t0`) × 1000, `Time.realtimeSinceStartup`) | `6eb7e94` | Columna `tr_ms` | Implementado; precisión sin medir |
| RF-03 Protocolo sentado | Calibración; Tablero; Rig | `Scripts/CalibradorSentado.cs` (1,15 m); `Scripts/BotonExergame.cs`; `Editor/ConstructorSala.cs` (desactiva proveedores de locomoción); `Scenes/SalaExergame.unity` | `6eb7e94`; `4529a42`; `b207653` | Fotos de sesiones sentadas | Implementado |
| RF-04 Costo de doble tarea | Registro de datos | `Scripts/RegistroSesion.cs` (`cdt_pct`, `cdt_bloque_A_pct`, `ccr_pct`) | `3053e13` | `_resumen.csv` | Implementado; **CDT por mano: no hay código → Pendiente** |
| RF-05 Iniciar/pausar/terminar | Panel del evaluador | `Scripts/PanelEvaluador.cs`; `Resources/panel_evaluador.html`; `Scripts/SesionExergame.cs` (`*DesdePanel`) | `3053e13`; `6eb7e94` | Capturas del panel (puerto 8095) | Implementado |
| RF-06 Registro exportable CSV | Registro de datos | `Scripts/RegistroSesion.cs` (incluye la clase `ResultadoEnsayo`); `Scripts/ArchivoDatos.cs` | `3053e13` | `_ensayos.csv` y `_resumen.csv` | Implementado |
| RF-07 Reporte al propietario | Panel del evaluador | `Resources/panel_evaluador.html` (pestaña «Resultados», «Descargar Excel» = CSV) | `3053e13` | Captura de Resultados | Parcial (no hay reporte de indicadores del piloto) |
| RNF-01 SUS ≥ 68 | Panel; Práctica | `Scripts/PanelEvaluador.cs`; `Scripts/GloboPractica.cs` | `3053e13`; `6eb7e94` | SUS del piloto | Pendiente de medir (no hay código de medición) |
| RNF-02 ≥ 95 % sin fallos | Control de sesión | `Scripts/SesionExergame.cs` (`OnApplicationFocus`, resumen parcial `EscribirResumen(false)`) | `6eb7e94` | Tasa de fallos del piloto | Pendiente de medir |
| RNF-03 SSQ < 20 | Calibración | `Scripts/CalibradorSentado.cs` | `6eb7e94` | SSQ del piloto | Pendiente de medir |
| RNF-04 Standalone en Quest 2 | Plataforma | `Editor/ConfigurarQuest2.cs`; `Editor/PermisoInternetAndroid.cs`; `ProjectSettings/ProjectSettings.asset` | `4529a42`; `f264610` | APK instalado en el visor | Implementado (APK del 24/10 debe recompilarse) |
| RNF-05 ±50 ms | Control de sesión | `Scripts/SesionExergame.cs` (reloj `realtimeSinceStartup`) | `6eb7e94` | Comparación con referencia externa | Pendiente de verificar |
| RNF-06 Pseudonimización | Participantes; Registro | `Scripts/Participante.cs`; `Scripts/RegistroSesion.cs` (solo recibe `codigo`) | `6eb7e94`; `3053e13` | CSV solo con código | Implementado |
| Importador CSV → SQLite | Datos | — (no existe ningún .py) | — | — | Pendiente |
| Pruebas automatizadas | Calidad | — (Test Framework 1.1.33 instalado, sin tests) | — | — | Pendiente |

---

## (c) Tabla F — Configuración real (script y valor serializado en `SalaExergame.unity`)

| Parámetro | Informe | Real | Dónde |
|---|---|---|---|
| Puerto del panel | 8080 | **8095** | `PanelEvaluador.puerto` |
| PIN | dev "ficticio (0000)" | Aleatorio 1000–9999 la primera vez, guardado en PlayerPrefs `exergame_pin`; **no se configura en el Inspector**; no se pide desde localhost | `PanelEvaluador.Awake` |
| Umbral de error de posición | 0,50 | **No existe.** `umbralErrorNivel = 0.5` decide el `nivel_maximo_alcanzado` (tasa de error > 50 % en un nivel) | `SesionExergame` → `RegistroSesion` |
| Ventana general | 10 s | 10 s | `segundosParaResponder` |
| `marcarTardiaSegunGuion` | desactivado | false → nunca hay «tardía» | `SesionExergame` |
| Altura de ojos | 1,15 m | 1,15 m | `CalibradorSentado.alturaOjos` |
| Zona e histéresis | ±0,12 m; 2 cm | ±0,12 m (`mitadZona`), profundidad 0,16 m; histéresis 2 cm fija en código | `BotonExergame` |
| «Esperas de presentación y práctica» | 3 s y 5 s | 3 s y 5 s son para **colocarse el visor** (inicio desde el panel / desde el menú del visor). Además: globo de presentación sin audio 5 s; práctica hasta 25 s por globo | `SesionExergame` |
| Sesiones RV | 1 a 6 | 1 a 6 | `sesionesRV` |
| Modo de sesión | Mouse / Prueba / Residente | Campo `tipo` = `real` o `prueba` (desde el menú de la escena en el editor se marca `prueba`) | `SesionExergame` |
| Escenario | Clásico, Globos, Tambores | Igual (enum `Escenario`) | `Escenarios.cs` |

## (d) Tabla E — Rutas HTTP reales (`PanelEvaluador.cs`)

El servidor no distingue el método HTTP; la columna indica el que usa `panel_evaluador.html`. PIN por cabecera `X-Pin` o `?pin=`; sin PIN desde localhost; si falla → `401 {"error":"pin"}`.

| Método | Ruta | Parámetros | Respuesta |
|---|---|---|---|
| GET | `/`, `/index.html` | — | HTML del panel (sin PIN) |
| GET | `/api/ping` | — | `{"ok":true}` (sin PIN) |
| GET | `/api/acceso` | — | `{"url","pin","sinRed"}` solo desde localhost (QR) |
| GET | `/api/estado` | — | JSON de estado (se actualiza cada 0,4 s) |
| GET | `/api/historial` | — | arreglo de resúmenes (se relee cada 5 s) |
| GET | `/api/ensayos` | `archivo` | CSV de ensayos |
| POST | `/api/iniciar` | `codigo`, `sesion`, `escenario`, `tipo`, `modo`, `actividad` | `{"ok":true}` |
| POST | `/api/pausar`, `/api/continuar`, `/api/terminar`, `/api/recentrar`, `/api/repetir`, `/api/saltar`, `/api/aprueba` | — | `{"ok":true}` |
| POST | `/api/volumen` | `d` | `{"ok":true}` |
| POST | `/api/marcar` | `archivo`, `tipo` (real/prueba) | `{"ok":true}` / 404 |
| POST | `/api/archivar` | `archivo` o `todo=1` | `{"ok":true,"archivadas":n}` |

Estado (`SesionExergame.EstadoJson`): `escenario, tipo, descentrado, modo, actividad, toquesLibres, repeticionesRegla, fase, pausado, escuchando, volumen, participante, sesion, total, completados, aciertos, precision, trPromedio, ensayo, nivel, bloque, regla, sesionesRV, participantes[{codigo,nombre}], resultados[{n,nivel,bloque,regla,anunciado,mostrado,incongruente,tocado,mano,tr,correcto,error}]`. El navegador consulta cada 1 s.

---

## (e) Diferencias entre el informe y el proyecto real

**Ya conocidas (confirmadas):** puerto 8095; no existe umbral de error de posición (solo `umbralErrorNivel = 0.5`); 3 s y 5 s son para colocarse el visor; `marcarTardiaSegunGuion = false`; estructura `Assets/Exergame/...`; importador CSV→SQLite no escrito; sin pruebas automatizadas.

**Nuevas, por sección:**

| # | Sección del informe | Dice | Real |
|---|---|---|---|
| 1 | D, Tabla 6; E, Tabla 8 | CSV de ensayos con **25 variables** | **28 columnas** (se agregaron `escenario`, `tipo_sesion`, `modo_alcance`) |
| 2 | D, Tabla 6; E, Tabla 8 | Resumen con **28 métricas** | **32 filas** `metrica,valor` (incluye `ensayos_abandonados`, `tr_promedio_bloque_A/B_ms`, `tr_desviacion_estandar_ms`, `umbral_error_nivel`, `repeticiones_regla`…) |
| 3 | 1.2, C (Tabla 5), H (Tabla 12) | **25 audios** de voz | **35 .wav** en `Audio/Voz` (el último diagnóstico guardado contó 31) |
| 4 | H, Tabla 12 | Diagnóstico del 2026-10-20: plataforma Android OK, 294,7 GB, ruta 165 caracteres | El `Diagnostico_Exergame.txt` del proyecto es del **2026-10-24**: **[AVISO] plataforma StandaloneWindows64**, 64,3 GB, ruta de 226 caracteres (el del 20/10 no está en el proyecto) |
| 5 | H, paso 2 | `Assets/Scenes/SalaExergame.unity` | `Assets/Exergame/Scenes/SalaExergame.unity` |
| 6 | H paso 4; J escenario E6; Tabla 4 | Menú «Exergame > Prueba con mouse»; `PruebaConMouse` en `Assets/Editor` | **No hay ese menú**: `PruebaConMouse` está en `Scripts/` y se agrega solo al dar Play en el editor |
| 7 | H, paso 3 | «Diagnóstico del proyecto» con 13/13 OK | Menú real «Diagnostico del proyecto»; no se puede correr en `-batchmode` |
| 8 | H, pasos 6 y 9 | «Compilar e instalar en Quest (USB)», «Crear paquete para compartir (.zip)» | Coinciden; también existe «Solo crear el APK» |
| 9 | H, paso 7; F; E | `http://<IP>:8080` | `http://<IP>:8095` |
| 10 | F, Tabla 9 | PIN de desarrollo «Ficticio (0000)» | El PIN no es configurable: se genera al azar (1000–9999) y se guarda en PlayerPrefs |
| 11 | D, convenciones | Una clase principal por archivo, mismo nombre | No siempre: `ColorJuego.cs` contiene `Combinatoria`, `Colores` y 4 enums; `RegistroSesion.cs` contiene `ResultadoEnsayo`; `Escenarios.cs` contiene `SonidosTema`; `ModoJuego.cs` no tiene clase `ModoJuego` (`ModosJuego`, `ModoAlcance`, `Actividad`) |
| 12 | B, Tabla 4 | `PresentadorMousey` junto a la presentadora (runtime) | Está en `Editor/` (herramienta de editor + `ImportadorMousey`) |
| 13 | B, Tabla 4 | `Assets/Data` con los .asset | No existe; `Participantes.asset` y `GuionSesion.asset` están en `Assets/Exergame/` |
| 14 | D, robustez; J, E7 | Pausa automática al perder el foco | Existe, pero **se desactiva en el editor** (`if (Application.isEditor) return;`): con Meta Link desde Unity no pausa; solo en el APK |
| 15 | E, Tabla 8 | Órdenes: iniciar, pausar, continuar, terminar, recentrar, repetir, saltar, volumen | Además `aprueba` (pasar de manos libres a la prueba), `marcar`, `archivar`, `ensayos`, `historial`, `acceso`, `ping`; e iniciar recibe también `tipo` y `actividad` (manos libres) |
| 16 | 1.2 / D | 10 módulos funcionales | La Tabla 6 lista 8; el código tiene además escenarios (`Escenarios`, `EstimuloTema`, `EntornoTema`, `AmbienteAnimado`), progreso (`ProgresoLogros`) y modos de alcance (`ModoJuego`) |
| 17 | B, Tabla 3; G, Tabla 10 | Ramas `feature/<módulo>` y `fix/<tema>` con pull request; etiqueta `v0.8.0-incremento-practica08`; commits `tipo(módulo): …` | Rama `incremento-p08` con merge `--no-ff` local; etiqueta `v0.8.0`; commits `tipo: mensaje` |
| 18 | G | `bash tools/evidencia_git.sh 2026-08-01` | El script acepta ahora esa fecha opcional (filtra el historial desde ese día) |
| 19 | Anexos A y B | Archivos en la carpeta `repo_P08` | Están en la raíz del repositorio (`README.md`, `.gitignore`, `docs/`) |
| 20 | RNF-06 | El nombre solo en el menú | En los CSV solo va el código ✔; el nombre **sí viaja** al panel web (`/api/estado` → `participantes[].nombre`). Hoy son genéricos («Participante 1»…) |
| 21 | — | — | `Build Settings` incluye también `Assets/Scenes/SampleScene.unity` (de la plantilla) activa; `MetricsLogger.cs` y `TareaVozCirculo.cs` están vacíos; los Starter Assets son de la versión 2.5.2 con XRI 2.6.4 |
| 22 | D, clasificación | círculo, mano, correcto, sin respuesta, saltado, interrumpido | Coincide; en el código «tardía» se evalúa primero, pero con `marcarTardiaSegunGuion = false` la ventana es 10 s y nunca se cumple |
| 23 | RNF-05 / RF-02 | TR medido con el reloj del juego | `t0` se toma cuando se ordena reproducir la voz (`presentador.Decir`), no al inicio real del audio; el toque se detecta por cuadro (frame) |

## Pendientes

- Guion **provisional** (`GuionSesion.asset: provisional: 1`); combinatorias reales de P01 a P07.
- Voces temporales (TTS) → grabaciones definitivas.
- Importador Python CSV → SQLite (no escrito).
- Pruebas automatizadas (Test Framework instalado, sin tests).
- CDT por mano (RF-04) y reporte para el propietario (RF-07).
- Recompilar el APK (el de `Builds/` es del 24/10) y nuevo diagnóstico en plataforma Android.
- Medir SUS, SSQ, tasa de fallos y precisión ±50 ms.
- Decidir retroalimentación y ventana de respuesta (4/6 s vs 10 s).
- Corregir en el informe las diferencias 1–23.
