# dogseye

Juego de ritmo con runner infinito. Unity 6000.0.76f1, URP, new Input System.
Escena principal: `Assets/Scenes/Runner.unity`.

## Estructura

Dos sistemas que corren en **relojes independientes**, y así lo queremos:

- **Ritmo** — `ChartManager` lanza notas por tiempo del chart JSON. Vive en
  `-- RHYTHM SYSTEM --`, que cuelga del `Player`.
- **Terreno** — `SegmentGenerator` crea segmentos por distancia recorrida, no por
  tiempo. Vive en `LevelController`.

No los acoples sin hablarlo: es una decisión explícita del equipo.

Scripts del runner en `Assets/Scripts/runner/`. El sistema de notas en
`Assets/Scripts/NotePool/` y `Assets/Scripts/Charts/`.

## Reglas al trabajar aquí

- **Verifica contra la escena antes de tocar nada.** El proyecto es de varias
  personas y cambia entre sesiones. Ya ha pasado que `ChartManager.cs` se
  reescribiera a mitad de una sesión.
- **No metas `.cs` de copia de seguridad bajo `Assets/`.** Unity los compila y
  chocan las clases. Hay backups en `Escritorio/dogseye_backup_claude/`.
- Nada de temporizadores nuevos para los efectos del puente: todos leen
  `BasicMovement.VelocidadActual / PlayerSpeed`. Ver abajo.

## El puente entre secciones

Al responder una pregunta pasa una de dos cosas:

| | Cambia el terreno | No cambia |
|---|---|---|
| Termina cuando | se cruza el `RushStopTrigger` | pasan `SegundosPuenteSinTransicion` (2 s) |
| Acelerón y líneas | sí | no |
| Dolly zoom | sí (por velocidad) | sí (canal manual, por tiempo) |
| Notas vuelven | en el `RhythmResumeTrigger` | al acabar el temporizador |

**`SpeedLinesHUD`, `DollyZoomEffect` y el acelerón comparten un único driver:**
`InverseLerp(1, MultiplicadorMaximo, VelocidadActual / PlayerSpeed)`.
Por eso arrancan y paran juntos sin sincronizar nada, y por eso las líneas no
salen en el puente sin transición: ahí la velocidad no cambia, así que dan 0.
Si añades otro efecto al puente, engánchalo al mismo sitio.

## Trampas conocidas

- **`-- RHYTHM SYSTEM --` está apagado durante todo el puente.** Cualquier UI que
  deba verse ahí (líneas de velocidad, textos) tiene que ir en otro canvas.
  El HUD de vida vive dentro, así que también se oculta.
- **Si cambias `PlayerSpeed` o `MultiplicadorMaximo` hay que recolocar el
  `RushStopTrigger`** de los dos prefabs de transición. El margen tiene que ser
  mayor que la distancia de frenado: `(v_rush + v_normal)/2 * TiempoBajada`, y
  usamos x1.5 de colchón. Ahora son 51 unidades.
- **`MusicManager` y `GameConfig` son `ASingleton` con `DontDestroyOnLoad`.**
  Sobreviven a la recarga de escena y `ASingleton` no limpia `Instance` en
  `OnDestroy`. No los destruyas: usa `StopMusic()` o `FadeOut()`.
- **El final de partida salta al CONTESTAR la última pregunta**, dentro de
  `Resolver()`: se va directo al fundido en blanco, sin cambio de terreno ni
  sección musical extra. `LanzarPregunta()` sigue llamando a `IntentarTerminar()`
  como red de seguridad, para que la tecla P no saque una pregunta con la partida
  ya acabada.
- **`Transitions.WinTransition()` y `LoseTransition()` cargan escenas `"Victoria"`
  y `"Derrota"` que no existen** en el proyecto. Si algo las llama, peta.
- Al reinstanciar componentes en el Player, ojo con los duplicados: dos
  `TransitionRush` a la vez se pisaban escribiendo el multiplicador cada frame.
  Los que no admiten duplicado llevan `[DisallowMultipleComponent]`.

## Textos del juego

Todos en el Inspector, ninguno hardcodeado:

- `LevelController > DecisionManager` — `TextoPregunta`, `TextoOpcionBuena`,
  `TextoOpcionMala`
- `LevelController > GameEndManager` — `FinalesPorSecuencia` (8 combos BBB…MMM,
  vacías), `PlantillaResumen` y las 4 frases singular/plural, `TextoReinicio`
- `Player > PlayerDeathManager` — `TextoMuerte`

## Pruebas rápidas sin jugar la partida

- **P** lanza una pregunta · **M** mata al jugador · **R** reinicia en las pantallas finales
- Botón derecho sobre el componente: `TransitionRush` → "Lanzar acelerón",
  `GameEndManager` → "Terminar partida (prueba)" y "Generar todas las
  combinaciones", `PlayerDeathManager` → "Morir (prueba)"

## Temporal, pendiente de quitar

- `PlayerDeathManager.UsarTeclaDePrueba` (tecla M). En `LifeManager` hay una
  llamada comentada `//if (muerte != null) muerte.Morir();` lista para
  descomentar cuando el sistema de notas dé una vida fiable. La referencia ya
  está asignada en la escena, solo hay que quitar el `//`.
- `RushStopTrigger.LogAlDispararse` deja rastro en consola. Desmárcalo cuando el
  frenado esté ajustado.
