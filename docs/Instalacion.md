# Instalación

## Requisitos

- Unity 6.3 LTS (6000.3) con el módulo Android Build Support (SDK, NDK y OpenJDK).
- Android Studio Panda 2 (2025.3.2).
- Dispositivo Android con cámara, API 26 o superior.

## Importar el proyecto

1. En Unity Hub, seleccione Add > Add project from disk y elija la carpeta `Raveneer`.
2. Si el Hub indica que la versión del editor no está instalada, seleccione la 6000.3.x disponible.
3. Al abrirse por primera vez, el proyecto genera de forma automática prefabs, datos, escenas, etiquetas y la configuración de Android.
4. Para regenerar el contenido en cualquier momento, use el menú **Raveneer > Build Project**.

## Modo RA

No requiere ARCore ni configuración adicional. Usa la cámara trasera y el sensor de orientación del teléfono; la cámara se solicita al activar el modo.

En el editor se simula: fondo oscuro, marcadores de tesoro y giro de vista con deslizamiento.

## Exportar a Android Studio

1. File > Build Profiles > Android > Switch Platform.
2. Verifique que **Export Project** esté activo y seleccione Export.
3. En Android Studio Panda 2, abra la carpeta exportada.

Panda 2 admite Android Gradle Plugin de 7.0 a 9.1. Si Android Studio propone actualizar el plugin, acepte solo versiones dentro de ese rango.

## Compilar directamente desde Unity

Desactive **Export Project** y use Build And Run con el dispositivo conectado.
