# Project Overview
- Game Title: MARS ALIVE.
- High-Level Concept: Juego educativo 2D point-and-click sobre exploración de una base marciana. Esta entrega añade únicamente herramientas provisionales del Invernadero; no desarrolla plantas, crecimiento, recursos ni misiones.
- Players: Un jugador, público de 7 a 12 años.
- Inspiration / Reference Games: No se han especificado juegos de referencia; conservar el estilo 8-bit existente.
- Tone / Art Direction: Exploración educativa relajada; iconos de 16×16, marcos de píxeles y Press Start 2P.
- Target Platform: StandaloneWindows64; mantener compatibilidad con clic y tap.
- Screen Orientation / Resolution: Horizontal, referencia de Canvas 1920×1080.
- Render Pipeline: Universal Render Pipeline; interfaz Screen Space – Overlay existente, sin modificar cámaras ni ajustes gráficos globales.

# Game Mechanics
## Core Gameplay Loop
Dentro del Invernadero: seleccionar regadera → hacer clic en el escenario → completar efecto de riego → habilitar pala → seleccionar pala y excavar. El botón MISION queda sin acción. Fuera del Invernadero no se muestran herramientas.

**Decisión confirmada:** `haRegado` se conserva al salir y regresar durante la misma partida, sin guardado en disco. Esto evita repetir acciones por navegar. Salir deselecciona y cancela efectos en curso, pero no elimina un riego ya completado.

No generar plantas, crecimiento, barras, gasto de agua, energía, progreso o contenido de misiones. Los eventos dejan disponibles puntos de integración para futuras instrucciones.

## Controls and Input Methods
- Mantener el New Input System confirmado en el proyecto y su EventSystem/InputSystemUIInputModule.
- Selección y acciones mediante eventos uGUI. `AreaJuego` recibe IPointerClickHandler: no sondeo global de clics, para no activar herramientas al pulsar botones o HUD.
- Cursor de herramienta mediante Image de UI sin raycasts y posición obtenida de una InputAction de puntero. Ocultar/restaurar el cursor del sistema solo mientras corresponda; restaurarlo al deseleccionar, abrir mapa, salir de habitación, deshabilitar el sistema o perder foco.
- Preferir cursor de UI frente a Cursor.SetCursor: permite sprites reemplazables y comportamiento uniforme con animaciones sin límites del cursor nativo. No requiere paquetes adicionales.
- Selección excluyente: regadera o pala, nunca ambas; segundo clic en la seleccionada la deselecciona.
- Bloquear acciones durante viñetas o mapa y durante una animación de uso, sin acumular clics.
- Pala sin riego: permanece clicable para sacudirse por pasos y mostrar `WATER FIRST!` durante 1,5 segundos; atenuación visual, no Button.interactable=false.

# UI
Jerarquía final, de abajo a arriba:
```
TutorialCanvas
├── Fondo                         (sin cambiar su imagen)
├── InvernaderoUI                 (solo activo en Invernadero)
│   ├── AreaJuego                 (pantalla completa, transparente)
│   ├── Efectos                   (sin raycasts)
│   ├── ColumnaHerramientas
│   │   ├── Regadera
│   │   ├── Pala
│   │   └── Mision
│   ├── AvisoWaterFirst
│   └── CursorHerramienta         (sin raycasts)
├── HUD                           (existente, siempre visible)
├── MapaOverlay                   (existente)
└── Vinetas                       (existentes)
```
- Columna izquierda con posiciones y separación editables; colocarla fuera de Progreso y Simbolos. Mantener zonas de botones de tamaño fijo para que la selección no desplace otros controles.
- Iconos: 16×16, mostrados a 64×64 en reposo (×4) y 80×80 seleccionados (×5). No interpolar ni usar escalas intermedias. Conservar el Canvas Scaler existente: no prometer escalado físico entero en todas las resoluciones de pantalla.
- Marco negro y relleno gris oscuro; marco blanco al seleccionar. MISION conserva su tamaño.
- Reutilizar `Assets/Fonts/PressStart2P/PressStart2P_TMP.asset`.
- Efectos por pasos a aproximadamente 12 fps; coordenadas de partículas redondeadas a la cuadrícula de UI. Regadera: 0°, −15°, −30°, regreso y diez gotas azules. Pala: descenso/ascenso discretos y ocho partículas de tierra.
- No añadir efectos suaves, sonidos, instrucciones extra ni botones adicionales.

# Key Asset & Context
## Estado comprobado
- `Assets/Scripts/UI/RoomManager.cs`: siete métodos públicos que llaman a `CambiarFondo(Sprite)`; inicia Centro de control. No contiene todavía referencias al InvernaderoUI.
- `Assets/Scripts/UI/MapaOverlay.cs`: `AbrirMapa`, `CerrarMapa`, `AlternarMapa`, `EstaAbierto`; apertura ya bloqueada por viñetas. No ofrece evento de apertura actualmente.
- `Assets/Scripts/UI/VinetaManager.cs`: propiedad pública `VinetaAbierta`; conservar código, textos y secuencia.
- `Assets/Scenes/Tutorial_P2.unity`: existen Fondo, HUD, MapaOverlay y Vinetas; no se encontró InvernaderoUI en la escena guardada.
- Fondo disponible: `Assets/Art/UI/Rooms/Invernadero.png`; no reemplazar ni modificar.
- Dependencias inspeccionadas mediante AssetDatabase.GetDependencies; scripts y recursos existentes leídos. No hay plan anterior en Assets/Plans.

## Archivos nuevos
- `Assets/Scripts/UI/PixelArt.cs`: fábrica de sprites provisionales por código, identificados como placeholders. Regadera y pala de 16×16, filtrado Point, sin compresión; también pequeños recursos de marco/gota/tierra si son necesarios. Gestionar la destrucción de recursos generados, sin destruir sprites proporcionados por el usuario.
- `Assets/Scripts/UI/InvernaderoTools.cs`: selección, restricciones, cursor, corutinas, mensajes y eventos. Campos serializados de Sprite para sustituir regadera y pala; referencias de panel, botones, imágenes, efectos, mapa y viñetas. Implementar el receptor de clic de AreaJuego en este componente, colocando el controlador en AreaJuego y enlazando los botones mediante sus métodos públicos.
- Eventos públicos `OnRegado(Vector2 posicion)` y `OnExcavar(Vector2 posicion)`, con posición local de AreaJuego documentada en el código.
- `haRegado`: estado accesible de lectura, conservado en memoria durante la escena. Registrar el riego y emitir su evento al completar la acción; si se cancela antes, no emitir evento ni conceder un riego nuevo.
- Métodos `SeleccionarRegadera()`, `SeleccionarPala()`, `Deseleccionar()`, `OnMisionPresionado()`; este último contiene únicamente `// TODO: acción pendiente`.

## Cambios mínimos existentes
- `RoomManager.cs`: añadir referencia serializada a InvernaderoUI/herramientas; MostrarInvernadero muestra el panel y las otras rutas lo ocultan y deseleccionan, centralizando el comportamiento sin cambiar nombres, firmas ni asignaciones de sprites.
- `MapaOverlay.cs`: añadir un evento de apertura y emitirlo cuando la apertura sea efectiva. InvernaderoTools se suscribe para deseleccionar inmediatamente. Este ajuste mínimo evita modificar hotspots o depender de un sondeo con retraso; no cambiar la lógica de mapa, Esc ni bloqueo de viñetas.
- `Tutorial_P2.unity`: añadir y conectar InvernaderoUI y referencias. No modificar StartScreen, otros fondos, hotspots ni viñetas.
- Nota de integración comprobada: `TutorialP2Setup.ApplyLayout()` fijaba HUD como segundo hijo. Ajustar únicamente ese orden para reservar InvernaderoUI debajo del HUD cuando exista; sin este ajuste, su Awake alteraría la jerarquía solicitada.

# Implementation Steps
1. **Revalidar estado e impacto antes de editar.** Confirmar escenas sin cambios pendientes, backend de entrada, dependencias de archivos a modificar y font asset. Si no está disponible la herramienta de dependencias, consultar AssetDatabase.GetDependencies y buscar referencias de scripts/escenas. Dependencias: ninguna.
2. **Crear PixelArt.cs.** Implementar generación determinista de iconos y pequeños sprites necesarios; textura RGBA sin mipmaps, Point y tamaños documentados. Campos de reemplazo en herramientas priorizan recursos del usuario. Dependencias: paso 1.
3. **Crear InvernaderoTools.cs.** Selección excluyente, ×4/×5, frame seleccionado, pala bloqueada visualmente, advertencia de 1,5 s y cursor. Eventos de puntero con New Input System y clic de AreaJuego, sin capturar clics globales. Dependencias: pasos 1–2.
4. **Implementar efectos discretos.** Corutinas con espera de aproximadamente 1/12 s, giros y desplazamientos por pasos; diez gotas y ocho partículas. Evitar uso concurrente; finalizar, cancelar y limpiar objetos transitorios correctamente. Eventos emitidos una vez por acción completada y estado de riego conservado. Dependencias: paso 3.
5. **Añadir integración mínima de habitación y mapa.** Actualizar RoomManager para visibilidad/deselección; añadir y suscribir el evento de apertura efectiva de MapaOverlay. Mantener inmutables los hotspots y VinetaManager. Suscribir/desuscribir de forma segura al activar/desactivar el panel. Dependencias: pasos 3–4.
6. **Construir y guardar InvernaderoUI.** AreaJuego primero, efectos sin raycasts, columna de tres botones, aviso y cursor. Enlazar callbacks; ubicar panel entre Fondo y HUD y dejarlo oculto al iniciar Centro de control. Asignar referencias y Press Start 2P. Guardar escena en la misma operación. Dependencias: pasos 2–5.
7. **Comprobar lógica y composición.** Revisar activación por habitación, escala discreta, bloqueo/aviso, eventos, cursor y limpieza; validar diseño sin duplicar objetos Button/Selectable para evitar repetir incidencias de las vistas temporales anteriores. Dependencias: paso 6.
8. **Prueba de ejecución y entrega.** Ejecutar pruebas de corutinas, eventos y clic/tap si la configuración lo permite. Si la recarga de dominio sigue deshabilitada y bloquea el método de prueba automático, solicitar autorización antes de cambiar esa configuración o indicar con precisión lo pendiente; no asumir aprobación. Entregar pasos de Inspector y resultados reales, no afirmar verificación temporal sin Play Mode. Dependencias: paso 7.

# Verification & Testing
- Al arrancar Tutorial_P2: Centro de control, InvernaderoUI oculto, cursor normal, HUD y tutorial existentes intactos.
- Acceder por hotspot Invernadero: fondo original, tres botones visibles; resto de habitaciones los oculta y deselecciona.
- Seleccionar/deseleccionar regadera; pasar a pala tras regar; verificar tamaños exactos 64/80 y exclusión mutua.
- Pala sin riego: no excava, sacudida discreta y texto exacto WATER FIRST! durante 1,5 s; segundo intento no deja corutinas o mensajes solapados.
- Clic en AreaJuego seleccionado: efecto apropiado y un solo evento con posición local correcta. Clic en botones, MISION, HUD, mapa o viñeta: ningún evento de uso.
- Riego completo establece haRegado; salir/regresar lo conserva. Cancelar primer riego a mitad no establece haRegado ni emite OnRegado.
- Abrir mapa deselecciona inmediatamente; una apertura rechazada por viñeta no cambia estados indebidamente. Cerrar mapa no reselecciona herramientas.
- Viñeta abierta impide selección y uso. MISION no cambia nada.
- Cambiar habitación o perder foco durante animación: detener efectos, limpiar partículas y restaurar cursor; sin acciones tardías ni objetos huérfanos.
- Cursor e imágenes de partículas no interceptan raycasts; exactamente tres controles nuevos, ninguna planta ni barra de recurso.
- Verificar ×4/×5 en unidades del Canvas, Point y ausencia de mipmaps; no cambiar calidad global para imponer pixel-perfect a todo el juego.
- Comprobar compilación y consola; probar ratón y tap, incluso en resoluciones/aspectos distintos, y documentar cualquier verificación que no pueda ejecutarse.
