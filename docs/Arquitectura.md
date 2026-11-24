# Arquitectura

## Estructura

```
Assets/_Project/
├── Animations/
├── Art/UI/            Pantallas de menú y pausa, botones y paneles
├── Audio/
├── Editor/            Generador del proyecto
├── Prefabs/           Cuervo, nido, tesoros, consumibles, enemigos
├── Scenes/            MainMenu, LoadingScene, GameScene, GameOverScene
├── ScriptableObjects/ Objetos y estadísticas de enemigos
└── Scripts/
    ├── Core/
    ├── Player/
    ├── Environment/
    └── UI/
```

## Módulos

| Módulo | Responsabilidad |
|---|---|
| `GameManager` | Pausa manual y automática, fin de partida, cambio de escena |
| `TimeOfDaySpawner` | Aparición aleatoria de enemigos según la hora del sistema |
| `DayNightCycle` | Hora del día y oscurecimiento de la pantalla |
| `AmbientAudio` | Música de día y de noche y grillos, generados por código |
| `DangerAlert` | Alerta en pantalla y música de peligro antes de un enemigo |
| `HurtFeedback` | Sonido de daño y parpadeo rojo del cuervo |
| `ArtScreenFitter` | Ajusta las ilustraciones de menú y pausa para cubrir toda la pantalla |
| `ControlsHint` | Guía de controles al iniciar |
| `DataPersistence` | Guardado local del nido, llaves, récord, contadores, álbum y logros |
| `EnemyBehaviour` | Persecución y daño por contacto |
| `PlayerController` | Vuelo, hambre diaria, efectos de daño, objeto en el pico, lanzamiento |
| `GestureInputManager` | Interpretación de gestos táctiles |
| `TouchReader` | Lectura de entrada compatible con Input System y entrada clásica |
| `FollowCamera` | Cámara ortográfica 2D que sigue al cuervo |
| `ARCameraToggle` | Alternancia entre el mundo 2D y la cámara real; tesoros flotantes y toma de fotos |
| `TreasureCatalog` | Los ocho objetos preciosos del álbum |
| `Achievements` | Definición y evaluación de logros |
| `AchievementTracker` | Cuenta las noches superadas y muestra el aviso de logro |
| `CollectionScreen` | Pantalla con las pestañas Álbum y Logros |
| `NamePrompt` | Pide el nombre de cada foto antes de guardarla |
| `Nest` | Almacena tesoros o consume alimentos |
| `UIManager` | HUD y notas |
| `PauseMenu` | Volumen, brillo y acciones de pausa |
| `LoadingScreenController` | Carga asíncrona de escenas |

## Datos

- `ItemData`: identificador, nombre, tipo (Tesoro, Llave, Alimento) y recuperación de salud.
- `EnemyStats`: franja horaria, vuelo, daño, velocidad y radio de detección.

## Modelo del personaje

El cuervo es un modelo genérico armado con primitivas de Unity. Se encuentra en `Prefabs/CrowModel` y es reemplazable por un modelo definitivo sin modificar la lógica: basta con sustituir el prefab anidado dentro de `Prefabs/Crow`.
