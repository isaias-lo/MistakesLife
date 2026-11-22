"""Arma todos los modelos de tools/blender/modelos/ y los exporta a Unity como .fbx.

Uso (necesita el módulo bpy de Blender: `pip install bpy`, o el programa Blender):
    python tools/blender/armar_modelos.py               # todos los modelos
    python tools/blender/armar_modelos.py mueble.mesa   # solo uno

Cada archivo modelos/<id>.py queda en
unity/Assets/MistakesLife/Resources/Modelos/<id>.fbx, y el juego lo usa en vez de la forma
simple de ese id (Models.cs). Si se borra el .fbx, vuelve la forma simple.
"""

import importlib.util
import os
import sys

AQUI = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, AQUI)

import comun  # noqa: E402

RAIZ = os.path.normpath(os.path.join(AQUI, "..", ".."))
CARPETA_MODELOS = os.path.join(AQUI, "modelos")
DESTINO = os.path.join(RAIZ, "unity", "Assets", "MistakesLife", "Resources", "Modelos")


def ids_disponibles():
    return sorted(n[:-3] for n in os.listdir(CARPETA_MODELOS) if n.endswith(".py") and not n.startswith("_"))


def cargar(id_modelo):
    ruta = os.path.join(CARPETA_MODELOS, id_modelo + ".py")
    spec = importlib.util.spec_from_file_location("modelo_" + id_modelo.replace(".", "_"), ruta)
    modulo = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(modulo)
    return modulo


def armar(id_modelo):
    comun.escena_vacia()
    cargar(id_modelo).armar()


def main(pedidos):
    ids = pedidos or ids_disponibles()
    for id_modelo in ids:
        armar(id_modelo)
        ruta = os.path.join(DESTINO, id_modelo + ".fbx")
        comun.exportar_fbx(ruta)
        print(f"{id_modelo} -> {os.path.relpath(ruta, RAIZ)}")


if __name__ == "__main__":
    main([a for a in sys.argv[1:] if not a.startswith("-")])
