# Configuración de ejemplo (valores ficticios donde hay datos sensibles)

Copiar a `docs/configuracion.local.md` (ignorado por Git) para anotar los datos reales del equipo.
Los valores de parámetros son los que hoy tiene el código y la escena `SalaExergame.unity`.

## Panel del evaluador (`Assets/Exergame/Scripts/PanelEvaluador.cs`)

| Campo | Valor |
|---|---|
| `puerto` | 8095 |
| URL en la PC | `http://localhost:8095` (sin PIN) |
| URL en la red | `http://192.168.0.100:8095` ← ejemplo ficticio; la IP real la arma el juego |
| PIN | `<PIN>` (4 dígitos al azar, guardado en PlayerPrefs `exergame_pin`) |
| `abrirNavegadorEnPC` | true |

## Sesión (`SesionExergame` en la escena)

| Campo | Valor |
|---|---|
| `segundosParaResponder` | 10 |
| `marcarTardiaSegunGuion` | false |
| `umbralErrorNivel` | 0.5 |
| `segundosInicioDesdePanel` | 3 |
| `segundosColocarVisor` | 5 |
| `segundosGloboSinAudio` | 5 |
| `segundosMaxPorGlobo` | 25 |
| `pausaEntreEnsayos` | 2 |
| `sesionesRV` | 6 |
| `erroresSeguidosParaRepetir` | 2 |
| `escenario` | Clasico (Clasico / Globos / Tambores) |
| `modo` | Proximidad (Proximidad / Completa / Limitaciones) |
| `acercarTablero` / `bajarTablero` / `inclinacionPanel` | 0.15 m / 0.04 m / -78° |

## Calibración y toque

| Script.campo | Valor |
|---|---|
| `CalibradorSentado.alturaOjos` | 1.15 m |
| `CalibradorSentado.autoRecentrar` | false |
| `BotonExergame.mitadZona` | 0.12 m (±0,12 m) |
| `BotonExergame.profundidadZona` | 0.16 m |
| Histéresis (código, `BotonExergame.cs`) | 0.02 m |
| `GloboPractica.radioToque` | 0.13 m |

## Participantes (`Assets/Exergame/Participantes.asset`)

```
P01  Participante 1   SI=Rojo  SD=Azul  II=Verde  ID=Amarillo   (ejemplo)
P02  Participante 2   ...
```

Solo códigos anónimos. No escribir nombres reales en este archivo.

## Equipo (ficticio)

| Dato | Ejemplo |
|---|---|
| Red WiFi | `RED_EJEMPLO` |
| IP del visor | `192.168.0.101` |
| Keystore Android | `ruta/ficticia/exergame.keystore` (no se versiona) |
