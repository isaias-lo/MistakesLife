"""Foto de prueba de modelos ya exportados, sin abrir Unity.

Uso:
    python tools/blender/vista_previa.py salida.png mueble.mesa escena.casa.cuaderno_apuntes

Importa los .fbx de unity/Assets/MistakesLife/Resources/Modelos/ (así también revisa que el
archivo exportado se lea bien), los pone en un piso con luz de día y saca una foto desde arriba
en diagonal, parecida a la cámara del juego. Sirve para revisar un modelo nuevo desde el celular.
"""

import math
import os
import sys

AQUI = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, AQUI)

import bpy  # noqa: E402

import comun as c  # noqa: E402
from armar_modelos import DESTINO  # noqa: E402


def preparar_foto(objetivo=(0.0, 0.4, 0.0), distancia=2.6, giro=35, altura=40, ancho=1200, alto=900):
    """Piso, luz y cámara. `objetivo` en coordenadas de Unity."""
    escena = bpy.context.scene
    piso = c.caja("Piso", (0, -0.01, 0), (6, 0.02, 6), c.material("piso", (0.62, 0.52, 0.42), aspereza=0.8))
    piso.modifiers.clear()

    sol = bpy.data.lights.new("Sol", "SUN")
    sol.energy = 3.0
    sol.angle = math.radians(8)
    obj_sol = bpy.data.objects.new("Sol", sol)
    obj_sol.rotation_euler = (math.radians(50), math.radians(10), math.radians(-35))
    escena.collection.objects.link(obj_sol)

    mundo = bpy.data.worlds.new("Cielo")
    mundo.use_nodes = True
    mundo.node_tree.nodes["Background"].inputs["Color"].default_value = (0.75, 0.80, 0.88, 1)
    mundo.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.6
    escena.world = mundo

    cam = bpy.data.cameras.new("Camara")
    cam.lens = 50
    obj_cam = bpy.data.objects.new("Camara", cam)
    escena.collection.objects.link(obj_cam)
    ox, oy, oz = c.unity(*objetivo)
    g, a = math.radians(giro), math.radians(altura)
    obj_cam.location = (ox + distancia * math.cos(a) * math.sin(g), oy - distancia * math.cos(a) * math.cos(g), oz + distancia * math.sin(a))
    mira = bpy.data.objects.new("Mira", None)
    mira.location = (ox, oy, oz)
    escena.collection.objects.link(mira)
    restriccion = obj_cam.constraints.new("TRACK_TO")
    restriccion.target = mira
    escena.camera = obj_cam

    escena.render.engine = "CYCLES"
    escena.cycles.device = "CPU"
    escena.cycles.samples = 48
    escena.cycles.use_denoising = True
    escena.render.resolution_x = ancho
    escena.render.resolution_y = alto
    escena.view_settings.view_transform = "Standard"


def importar(id_modelo):
    bpy.ops.import_scene.fbx(filepath=os.path.join(DESTINO, id_modelo + ".fbx"))


def foto(ruta):
    bpy.context.scene.render.filepath = os.path.abspath(ruta)
    bpy.ops.render.render(write_still=True)


def main(args):
    salida, ids = args[0], args[1:]
    c.escena_vacia()
    for id_modelo in ids:
        importar(id_modelo)
    preparar_foto()
    foto(salida)


if __name__ == "__main__":
    main(sys.argv[1:])
