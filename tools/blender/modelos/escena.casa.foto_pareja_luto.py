"""La viudez (escena.casa.foto_pareja_luto): la foto de tu pareja sigue en su lugar, con los
colores ya apagados por el tiempo y una cinta negra cruzada en la esquina.
"""

import importlib.util
import os

import fotos as f


def _foto_pareja():
    ruta = os.path.join(os.path.dirname(os.path.abspath(__file__)), "escena.casa.foto_pareja.py")
    spec = importlib.util.spec_from_file_location("foto_pareja", ruta)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def armar():
    f.marco_fondo("foto_pareja_luto", _foto_pareja().imagen(apagada=True), (0.0, 0.0, 0.0), 0.5, 0.4)
    f.cinta_luto("Cinta negra", (0.2, 0.15, -0.022), (0.14, 0.03, 0.003), giro_z=-45)
