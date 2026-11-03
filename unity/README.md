# Proyecto de Unity

Aquí vive la parte visual del juego: escenas, 3D, interfaz y sonido. **Unity solo dibuja**: todas las reglas de la vida están en el núcleo (`core/`), que Unity usa ya compilado (`Assets/Plugins/MistakesLife/MistakesLife.Core.dll`) junto con todos los paquetes en un solo archivo (`Assets/MistakesLife/Resources/packs.bundle.json`).

Versión de Unity: **Unity 6 LTS** (6000.0.x).

## Abrirlo por primera vez

1. Instala **Unity Hub** y, desde ahí, **Unity 6 LTS** marcando el módulo **Android Build Support** (con OpenJDK y Android SDK & NDK).
2. Descarga el repositorio a tu computador (con GitHub Desktop o `git clone`).
3. En Unity Hub: **Add → Add project from disk** y elige esta carpeta `unity/`. Ábrelo con Unity 6.
4. La primera vez tarda unos minutos: Unity crea `ProjectSettings/` y archivos `.meta`. Esos **sí** se suben al repositorio.
5. Aprieta **Play** (▶). Aparece una pantalla de prueba con tu personaje, el botón "Seguir viviendo", los eventos con sus opciones y el diario.

Si algo sale en rojo en la ventana **Console**, copia el mensaje y compártelo en el hilo.

## Cuando cambian el núcleo o los paquetes

El núcleo y los paquetes se copian aquí con un comando (necesita el SDK de .NET 8):

```bash
dotnet run --project tools/MistakesLife.UnityExport
```

O desde Unity: menú **MistakesLife → Actualizar núcleo y paquetes**. Los agentes lo corren en cada PR que cambia `core/` o `packs/`, así normalmente no tienes que hacerlo tú.

## Carpetas

| Carpeta | Qué es |
|---|---|
| `Assets/MistakesLife/Scripts/` | Código de pantalla (solo dibuja y llama a `LifePresenter` del núcleo) |
| `Assets/MistakesLife/Editor/` | Ayudas para el editor (el menú de actualizar) |
| `Assets/MistakesLife/Resources/` | `packs.bundle.json`, generado: no se edita a mano |
| `Assets/Plugins/MistakesLife/` | `MistakesLife.Core.dll`, generado: no se edita a mano |
| `Packages/manifest.json` | Paquetes de Unity que usa el proyecto (entre ellos Newtonsoft JSON, que necesita el núcleo) |
