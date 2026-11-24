# Sistemas de juego

## Juego principal

1. Volar y explorar.
2. Recoger un único objeto en el pico.
3. Volver al nido: los tesoros y llaves se almacenan; el alimento se consume y restaura salud.

## Derrota

La partida termina solo por dos causas:

- No comer durante un día de juego (240 s, valor `dayLengthSeconds` en `PlayerController`). La barra dorada indica el tiempo restante; consumir alimento en el nido la reinicia.
- Perder toda la salud por ataques de enemigos. La salud no baja por otra causa.

## Gestos

| Gesto | Acción |
|---|---|
| Deslizar | Dirección de vuelo |
| Doble toque | Recoger objeto cercano |
| Mantener presionado | Lanzar el objeto con una risa; el objeto se pierde |
| Toque simple | Leer notas (en modo RA, recoger el tesoro o espantar al enemigo tocado) |

Los controles se muestran al iniciar la partida y permanecen visibles: una barra de ayuda inferior y los botones **Recoger** y **Soltar**. El botón **Controles** vuelve a mostrar la guía completa.

Ajustes para pantalla táctil:

- Las distancias se miden en pulgadas, por lo que el comportamiento es el mismo en cualquier densidad de pantalla.
- Un pequeño temblor del dedo no cancela el toque ni el mantener presionado: el movimiento cuenta solo al superar un margen de 0,12 in.
- El doble toque exige que ambos toques estén cerca entre sí y dentro de 0,3 s.
- Solo se sigue el primer dedo; los demás se ignoran.
- El giro se suaviza para evitar sacudidas.
- Los toques sobre botones de la interfaz no activan gestos de juego.

Los umbrales se editan en el componente `GestureInputManager`.

## Modo cámara: galería de tesoros

- El botón **Cámara RA** oculta el escenario virtual y muestra la cámara del teléfono. En este modo no hay enemigos.
- Se navega moviendo el teléfono: la orientación del dispositivo gira la vista. El deslizamiento solo se usa como simulación cuando el equipo no tiene sensor de orientación.
- Flotan alrededor ocho objetos preciosos distintos (gema azul, moneda de oro, perla, rubí, esmeralda, llave antigua, pluma de plata y cristal rosa). Un toque sobre uno toma la foto, con destello y sonido de obturador.
- Tras la foto se pide un nombre para el tesoro (hasta 24 caracteres). **Guardar** lo añade al álbum y **Descartar** cancela. Volver a fotografiar un tesoro reemplaza su foto y su nombre; una marca verde indica los que ya están en el álbum.
- Al activarlo aparece una pantalla con los controles del modo. El botón **Controles** la vuelve a mostrar.
- Todo lo del mundo queda en pausa: enemigos terrestres, veneno, aturdimiento, hambre, reloj de supervivencia, música y grillos.
- Al desactivarlo, el escenario y la cámara de seguimiento se restablecen.

## Álbum y logros

- El botón **Colección** (en partida) y los botones **Álbum** y **Logros** (menú principal) abren una pantalla con dos pestañas. En partida, abrirla pausa el juego.
- El álbum muestra los ocho tesoros: foto y nombre elegido para los fotografiados y un signo de interrogación para los demás.
- Las fotos se guardan como imágenes pequeñas (256 px) en `album/` dentro de la carpeta de datos de la aplicación; los nombres, en el archivo de guardado.

| Logro | Condición |
|---|---|
| Primera noche, Ojos de búho, Guardián nocturno | Sobrevivir 1, 3 y 7 noches |
| Pico curioso, Recolector, Gran acaparador | Recolectar 10, 50 y 100 objetos |
| Primera foto, Coleccionista, Álbum completo | Tener 1, 4 y 8 objetos preciosos en el álbum |

- Una noche superada cuenta tras 180 s de supervivencia dentro del horario nocturno (el tiempo en modo cámara no suma).
- Un objeto recolectado cuenta la primera vez que el cuervo lo toma; soltarlo y volver a tomarlo no suma de nuevo.
- Al desbloquear un logro aparece un aviso en la parte superior. Los logros se definen en `Achievements` y los tesoros en `TreasureCatalog`.

## Enemigos

El tipo de enemigo depende de la hora del sistema. El modo cámara no tiene enemigos.

| Enemigo | Modo | Franja | Daño |
|---|---|---|---|
| Serpiente | Mundo | Día | Veneno: 24 de daño repartido en 6 s |
| Coyote | Mundo | Día | Instantáneo: 20 |
| Búho | Mundo | Noche | Aturdimiento: 18 de daño y 1,5 s sin volar |

Día: 06:00 a 18:59. Noche: 19:00 a 05:59. Las horas límite se configuran en `TimeOfDaySpawner`; los valores de cada enemigo, en sus `EnemyStats`.

## Día y noche

- La hora del sistema define si es de día o de noche, con transición gradual entre las 18:00 y las 20:00 y entre las 5:00 y las 7:00.
- De noche la pantalla se oscurece con un tono azul; en modo RA no se aplica.
- La música cambia entre una melodía luminosa de día y una pieza lenta y grave de noche, con fundido suave.
- De noche se suma un efecto de grillos. Todo el audio se genera por código y respeta los controles de Música y Efectos.
- Los enemigos aparecen en momentos aleatorios, a veces de dos en dos, con un máximo simultáneo.
- Los enemigos terrestres persiguen al cuervo desde que aparecen y desaparecen si no lo alcanzan en 40 s, de modo que siempre pueden aparecer otros nuevos.
- Cada daño reproduce un sonido propio según su tipo (golpe, veneno o descarga) y el cuervo parpadea en rojo hasta que deja de recibirlo.
- Antes de cada aparición se muestra una alerta roja con el nombre del enemigo durante 3 s y la música cambia unos segundos a una pieza tensa de peligro.

## Servicios móviles

- Pausa automática al minimizar la aplicación.
- Pantalla siempre encendida durante la partida.
- Volumen y brillo ajustables desde la pausa. Si no se asigna un `AudioMixer` con los parámetros `MusicVolume` y `SFXVolume`, el volumen se controla de forma global.
- Orientación horizontal.

## Persistencia

Archivo JSON local `raveneer_save.json` con la colección del nido, las llaves obtenidas (máximo 5), el récord de supervivencia, los contadores de noches y objetos, el álbum y los logros. Se guarda al almacenar objetos, al fotografiar, al desbloquear un logro, al registrar un récord y al pausar o cerrar la aplicación.

## Estilo visual

- Todo el arte se genera por código con formas simples de color.
- Cada personaje, enemigo y objeto lleva un borde blanco grueso tipo calcomanía y una sombra suave.
- La paleta es única para todo el juego y se define en `ProjectBootstrap`.
- El menú principal, la carga y el fin de partida usan la ilustración del nido (`Art/UI/MenuNest.png`); la pausa usa `Art/UI/PauseScreen.png`, con zonas táctiles sobre sus botones. Las versiones `*Wide.png` prolongan los costados de la ilustración para que cubra pantallas horizontales; en teléfonos altos la imagen se recorta para llenar la pantalla.
- Botones de madera, paneles nocturnos y títulos amarillos con contorno comparten el mismo estilo en toda la interfaz.
- El icono de la aplicación está en `Art/Icon` (imagen completa y capas adaptables de Android) y se asigna a Android al ejecutar **Raveneer > Build Project**.
