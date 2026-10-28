# Eva2_DesarrolloVideojuegos-Diego_Ariel
Repositorio para la Prueba 02 de Arquitectura y Lógica de un Videojuego
Asignatura: Desarrollo de Videojuegos

1. Objetivo del Juego

Nox, un espíritu de luz, debe recuperar 5 fragmentos perdidos a través de plataformas peligrosas para volver a encender el faro de Puerto Bruma. El juego cuenta con una progresión narrativa, si el jugador tiene que recolectar los 5 fragmentos y llegar a la puerta, el faro se enciende. Si Nox pierde sus 3 vidas al caer o tocar trampas, el juego termina (GameOver).

2. Plataforma Seleccionada y Justificación

Plataforma: PC (Windows).

3. Configuración Clave

Versión del Motor: Unity 2022.3 LTS 
Renderización (Pipeline): Se configuró un proyecto 2D URP (Universal Render Pipeline).

Físicas: Implementación de físicas mediante Rigidbody2D, Tilemap Collider 2D y Composite Collider 2D.

Gestión de Entradas: Configurado en Project Settings > Player > Active Input Handling como Both para evitar errores de compatibilidad con los scripts clásicos.

4. Instrucciones de Ejecución

Descargar la carpeta completa llamada Build desde este repositorio.

Extraer los archivos (si están comprimidos en .zip).

Hacer doble clic en el archivo ejecutable .exe (ej: FarolDeBruma.exe).

Jugar directamente desde el Menú Principal.

5. Controles

Movimiento: Teclas A / D o Flechas Izquierda / Derecha.

Salto: Barra Espaciadora.

Navegación UI: Mouse (Clic izquierdo para botones de menú).

6. Dependencias y Recursos

TextMeshPro (TMP Essentials): Obligatorio para la renderización de las fuentes en el HUD y los textos de historia.

2D Tilemap Editor / 2D Sprite: Para la construcción del "Relato Gráfico" del nivel mediante grillas.

Recursos (Licencias): Los sprites y efectos de sonido utilizados provienen de fuentes libres de derechos.

7. Riesgos y Mitigaciones

Riesgo:Estos paquetes generaron errores de GUI y bloqueos de Assembly que impedían renderizar la ventana de juego y compilar el proyecto.

Mitigación: Se identificaron los errores en la consola y se procedió a desinstalar los paquetes experimentales desde el Package Manager de Unity, limpiando las dependencias huérfanas. Tras esto, la consola quedó limpia y el motor permitió compilar el .exe para Windows sin problemas.

8. Roles de la Dupla

Ariel Venegas: [Breve descripción de lo que hizo, ej: Implementación de la lógica del menú, configuración de Canvas y textos con TextMeshPro, búsqueda de recursos de audio].

Diego Monteleone: Construcción del nivel con Tilemaps (Relato Gráfico), configuración de físicas y colisiones (Tilemap Collider, Rigidbody del jugador), troubleshooting de dependencias (eliminación de paquetes defectuosos) y compilación del Build final.
