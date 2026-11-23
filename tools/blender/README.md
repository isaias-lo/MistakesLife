# Modelos 3D con Blender (por scripts)

Los objetos de la maqueta se pueden hacer en Blender **solo con código Python**, sin abrir
Blender a mano. Así un agente los arma y Isaias los revisa con fotos desde el celular.

## Cómo funciona

1. Cada modelo es un archivo `modelos/<id>.py` con una función `armar()`.
   El nombre es el id del objeto en el juego (`mueble.mesa`, `escena.casa.cuaderno_apuntes`).
2. `armar_modelos.py` corre esos archivos y deja cada modelo en
   `unity/Assets/MistakesLife/Resources/Modelos/<id>.fbx`.
3. El juego busca primero ese archivo (ver `unity/Assets/MistakesLife/Scripts/Models.cs`).
   Si está, lo usa; si no, dibuja la forma simple de siempre. Borrar el `.fbx` deshace el cambio.

Las medidas se escriben en coordenadas de Unity y desde el ancla del objeto, igual que las
formas simples de `SceneObjectBuilder.cs` y `RoomBuilder.cs`: así cada modelo calza en el mismo
lugar y a la misma altura que la forma que reemplaza.

## Comandos

```bash
pip install bpy                     # Blender como módulo de Python (necesita Python 3.13)
python tools/blender/armar_modelos.py               # arma todos los modelos
python tools/blender/armar_modelos.py mueble.mesa   # arma solo uno
python tools/blender/vista_previa.py foto.png mueble.mesa escena.casa.cuaderno_apuntes
```

`vista_previa.py` vuelve a abrir los `.fbx` exportados y saca una foto con luz de día, para
revisar el modelo sin Unity. La prueba de verdad es la fila "Capturas 3D" del PR, que saca
fotos dentro del juego.

## Piezas comunes

`barrio.py` tiene lo que se repite entre los lugares del mapa del barrio (`mapa.<forma>`) y de
las salidas: vereda, árboles, techos de dos aguas, ventanas, bancas, faroles, rejas y autos. Así
todos los lugares se ven del mismo barrio.

## Reglas

- Las mismas del juego: medidas y objetos reales (un cuaderno universitario de espiral, una mesa
  de comedor de 1,40 m), estilo adulto y sobrio, nada de juguete.
- Sin azar suelto: las texturas (veta de madera, apuntes) usan semillas fijas, para que el mismo
  script dé siempre el mismo modelo.
- Pocos polígonos: es un juego para celular.
