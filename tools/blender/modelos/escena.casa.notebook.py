"""El notebook abierto sobre la mesa (escena.casa.notebook): base de aluminio gris con teclado
y touchpad, pantalla encendida un poco echada hacia atrás.
"""

import comun as c

MESA = 0.44


def armar():
    aluminio = c.material("aluminio_notebook", (0.32, 0.33, 0.36), aspereza=0.35)
    teclas = c.material("teclas", (0.10, 0.10, 0.11), aspereza=0.6)
    pantalla = c.material("pantalla_notebook", (0.30, 0.52, 0.78), aspereza=0.15)
    x, z = 0.3, 0.15
    c.caja("Base", (x, MESA + 0.009, z), (0.36, 0.018, 0.25), aluminio, bisel=0.004)
    c.caja("Teclado", (x, MESA + 0.0185, z + 0.03), (0.3, 0.002, 0.11), teclas)
    c.caja("Touchpad", (x, MESA + 0.0185, z - 0.075), (0.11, 0.002, 0.065), c.material("touchpad", (0.36, 0.37, 0.40), aspereza=0.3))
    tapa = c.caja("Tapa", (x, MESA + 0.13, z + 0.145), (0.36, 0.24, 0.008), aluminio, bisel=0.003)
    c.girar_x(tapa, -12)
    vidrio = c.caja("Pantalla", (x, MESA + 0.13, z + 0.139), (0.33, 0.2, 0.002), pantalla)
    c.girar_x(vidrio, -12)
