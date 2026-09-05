# Sistema de runner y decisiones — dogseye

Referencia de lo construido sobre el runner: generación de terreno, preguntas de
decisión, puentes entre secciones, efectos de cámara, final de partida y muerte.

Valores tomados de la escena `Runner` el 2026-09-05.

---

## 1. La idea en una frase

El jugador corre por segmentos que se generan solos. Cada cierto tiempo aparece una
pregunta; la respuesta decide si el terreno cambia de ancho a estrecho o al revés.
Entre pregunta y respuesta hay un **puente**: las notas se apagan, la cámara hace un
dolly zoom y, si el terreno cambia, el jugador acelera hasta llegar al nuevo tramo.

## 2. Dos relojes independientes

| Sistema | Qué lo mueve | Dónde vive |
|---|---|---|
| Ritmo | tiempo del chart JSON (`ChartManager`) | `Player > -- RHYTHM SYSTEM --` |
| Terreno | distancia recorrida (`SegmentGenerator`) | `LevelController` |

Es deliberado: el equipo quiere que la música y el terreno vayan por su lado.

---

## 3. Scripts

Todos en `Assets/Scripts/runner/` salvo donde se indique.

| Script | Dónde | Qué hace |
|---|---|---|
| `SegmentGenerator` | LevelController | Crea segmentos al cruzar triggers. Expone `CambiarTipoDeSegmento()` |
| `SegmentSpawnTrigger` | en los 4 prefabs | Al final del segmento: pide el siguiente |
| `SegmentDespawner` | en los 4 prefabs | A la entrada: destruye el segmento X s después |
| `DecisionManager` | LevelController | Pregunta, respuesta, puentes. El cerebro de todo |
| `RhythmSystemToggle` | LevelController | Apaga y enciende el sistema de notas |
| `RhythmResumeTrigger` | solo transiciones | Devuelve las notas al entrar en la transición |
| `TransitionRush` | Player | Acelerón indefinido hasta `Detener()` |
| `RushStopTrigger` | solo transiciones | Frena el acelerón antes de llegar |
| `DollyZoomEffect` | Main Camera | Dolly zoom + alejamiento + punto de fuga |
| `SpeedLinesHUD` | SpeedLinesUI | Líneas radiales procedurales |
| `GameEndManager` | LevelController | Final de partida, fundido a blanco, reinicio |
| `PlayerDeathManager` | Player | Muerte, fundido a negro, reinicio |
| `BasicMovement` | Player | Movimiento hacia delante, con multiplicador |

---

## 4. El ciclo de una pregunta

1. **Se lanza** — con la tecla `P`, o desde `Transitions.NextTransition()` cuando
   acaba una sección musical.
2. `GameEndManager.IntentarTerminar()` — si ya hay 3 decisiones, **la pregunta no
   sale** y arranca el final.
3. Aparece el panel, cuenta atrás de 5 s. `RhythmSystemToggle.Desactivar()` apaga
   las notas.
4. **Se responde** — flecha izquierda = buena, derecha = mala. Si se acaba el
   tiempo se elige al azar.
5. `SegmentGenerator.CambiarTipoDeSegmento()` decide si hay transición:

   **Con transición** (el terreno cambia):
   se coloca el prefab de transición, arranca el acelerón y las líneas, sale el
   texto de respuesta. El texto se quita al cruzar el `RushStopTrigger`, y las
   notas vuelven en el `RhythmResumeTrigger`.

   **Sin transición** (mismo terreno):
   solo dolly zoom por tiempo, 2 s. Ni acelerón ni líneas. Al acabar se quita el
   texto y vuelven las notas.

**Regla de mapeo:** buena → Narrow, mala → Wide. Si ya estás en ese tipo, no pasa
nada y se sigue generando lo mismo.

---

## 5. El driver único de los efectos

```
intensidad = InverseLerp(1, MultiplicadorMaximo, VelocidadActual / PlayerSpeed)
```

Lo leen cada frame `SpeedLinesHUD` y `DollyZoomEffect`. Consecuencias:

- Los efectos empiezan y acaban con el acelerón **sin sincronizar nada**.
- Siguen solos las rampas `TiempoSubida` (0.6 s) y `TiempoBajada` (0.8 s).
- Si cambias `MultiplicadorMaximo`, todo se reescala gratis.
- En el puente sin transición la velocidad no cambia → intensidad 0 → **las líneas
  y el acelerón quedan fuera sin tener que excluirlos**.

`DollyZoomEffect` tiene además un **canal manual** (`LanzarManual(duracion)`) para
ese caso. En `LateUpdate` se queda con el mayor de los dos.

---

## 6. Triggers de los prefabs

Los 4 prefabs comparten suelo: **168 de largo**, z local de **-76.4 a 91.6**.
El jugador avanza en +Z.

| Trigger | z local | En qué prefabs | Qué hace |
|---|---|---|---|
| `RushStopTrigger` | **-127.4** | solo transiciones | Frena el acelerón. Sobresale 51 u por detrás |
| `DespawnTrigger` | -75.4 | los 4 | Destruye el segmento 10 s después |
| `RhythmResumeTrigger` | -74.4 | solo transiciones | Devuelve las notas |
| `SpawnTrigger` | +90.6 | los 4 | Pide el siguiente segmento |

> **El `RushStopTrigger` en -127.4 está fuera del suelo a propósito.** Cae sobre el
> segmento anterior, para que el jugador frene **antes** de pisar la transición.
> El margen de 51 u sale de la distancia de frenado real:
> `(60 + 24)/2 × 0.8 = 33.6 u`, por 1.5 de colchón.
> **Si cambias `PlayerSpeed` o `MultiplicadorMaximo`, hay que recalcularlo.**

---

## 7. Valores actuales del Inspector

### Player

| Componente | Campo | Valor |
|---|---|---|
| `BasicMovement` | `PlayerSpeed` | 24 u/s |
| `TransitionRush` | `MultiplicadorMaximo` | 2.5 |
| | `TiempoSubida` / `TiempoBajada` | 0.6 / 0.8 s |
| | `SegundosDeSeguridad` | 20 s |
| `PlayerDeathManager` | `TeclaDeMuerte` | M (temporal) |
| | `SegundosDeFrenado` | 1.5 s |
| | `DuracionFundido` / texto | 1.2 / 0.6 s |
| | `MusicaAlMorir` | FundirSalida |

### Main Camera — `DollyZoomEffect`

| Campo | Valor |
|---|---|
| `DeltaFOV` | 25 (60° → 85°) |
| `CompensacionDolly` | 1 (dolly zoom puro) |
| `RetrocesoMaximo` | 6 u |
| `IntensidadManualMaxima` | 1 |
| `TiempoSubidaManual` / `Bajada` | 0.4 / 0.6 s |
| `DesplazarPuntoDeFuga` | activado |
| `AlturaPuntoDeFuga` | **-0.21** |

Neto de la cámara en el pico: el dolly acerca 3.8 u y el retroceso aleja 6, así
que acaba **2.2 u más lejos** que en reposo. Si quieres el alejamiento más
evidente, sube `RetrocesoMaximo` o baja `CompensacionDolly`.

### LevelController

| Componente | Campo | Valor |
|---|---|---|
| `SegmentGenerator` | `SegmentosIniciales` | 2 |
| | `LargoSegmento` / `Zpos` | 168 / 168 |
| `DecisionManager` | `TeclaPregunta` | P |
| | `SegundosParaElegir` | 5 s |
| | `SegundosPuenteSinTransicion` | 2 s |
| `GameEndManager` | `DecisionesParaFinal` | 3 |
| | `DuracionFundido` / texto | 1.5 / 0.8 s |

### SpeedLinesUI — `SpeedLinesHUD`

`LineasPorSegundo` 55 · `MaxLineas` 60 · `RadioDeNacimiento` 160-380 ·
`Grosor` 2-6 · `Largo` 70-190 · `EstiramientoAlAlejarse` 2.2 · `OpacidadMaxima` 0.85

> Si las líneas tapan el centro y molestan, sube `RadioDeNacimiento`.

### Escena

Niebla Linear, 100 → 260, color de horizonte. **No es decorativa**: tapa el punto
donde aparecen los segmentos nuevos, a 337 u del jugador. Si la quitas, se verá el
pop-in.

---

## 8. Final de partida

`DecisionesParaFinal = 3`, pero **la partida no acaba al contestar la tercera**:
acaba en la pregunta que vendría después, o sea al terminar esa sección musical.
El corte está en `LanzarPregunta()`.

> Como el corte está ahí, **la tecla P también termina la partida** si ya hay 3
> decisiones. Si no lo queréis, hay que separar los dos caminos.

Pantalla: fundido a blanco (1.5 s) → textos (0.8 s) → `R` reinicia.

Textos, todos en `GameEndManager`:

- `FinalesPorSecuencia` — 8 entradas `BBB BBM BMB BMM MBB MBM MMB MMM`,
  **todas vacías**. `B` = buena, `M` = mala, en orden de partida.
- `TextoFinalPorDefecto` — para las que dejéis sin escribir.
- Resumen de conteo: `PlantillaResumen` con `{buenas}`, `{malas}`, `{total}`, y
  cuatro frases singular/plural donde `{n}` es el número. El 0 usa plural.

Si cambias `DecisionesParaFinal`, botón derecho → **"Generar todas las
combinaciones"** regenera las 2ⁿ conservando lo ya escrito.

---

## 9. Muerte

Provisional con la tecla **M** hasta que el sistema de notas dé una vida fiable.

Secuencia: notas fuera → música en fundido → frenado de 1.5 s → silencio
garantizado → negro (1.2 s) → texto (0.6 s) → `R`.

Para activarla de verdad, en `LifeManager.UpdateObserver()`:

```csharp
if (currentLife <= 0f)
{
    //if (muerte != null) muerte.Morir();   // <- quitar el //
}
```

La referencia ya está asignada en la escena. Al descomentarla, poner
`UsarTeclaDePrueba` a `false`.

`PlayerDeathManager.Reiniciar()` llama a `GameEndManager.Reiniciar()`: **no hay
lógica duplicada**, si tocas una tocas las dos.

---

## 10. Avisos abiertos

- **`Transitions.WinTransition()` y `LoseTransition()` cargan escenas `"Victoria"`
  y `"Derrota"` que no existen.** Fallarán si algo las llama.
- **`MusicManager` y `GameConfig` son singletons con `DontDestroyOnLoad`.**
  Sobreviven a la recarga y `ASingleton` no limpia `Instance` en `OnDestroy`.
  No los destruyáis: `StopMusic()` o `FadeOut()`.
- **`Runner` se añadió a Build Settings** (índice 1). Sin eso `LoadScene` peta al
  pulsar R. `RhythmTesting` sigue en el 0, así que la build arranca igual.
- **`RushStopTrigger.LogAlDispararse` está activado** en los dos prefabs de
  transición. Quitadlo cuando el frenado esté ajustado.
- `Note.cs` tiene un warning de variable `accuracy` sin usar. Inofensivo.
