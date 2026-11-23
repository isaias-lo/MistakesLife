"""Tele plana colgada en el muro (mueble.tele): 1,20 x 0,70 m, la pantalla mira a la pieza (-z).
El ancla queda en el centro de la tele, como la forma simple.
"""

import comun as c


def armar():
    marco = c.material("marco_tele", (0.06, 0.06, 0.07), aspereza=0.35)
    pantalla = c.material("pantalla_tele", (0.09, 0.11, 0.14), aspereza=0.12)
    soporte = c.material("soporte_tele", (0.15, 0.15, 0.16), aspereza=0.4)

    c.caja("Marco", (0, 0, 0.005), (1.2, 0.7, 0.04), marco, bisel=0.006)
    c.caja("Pantalla", (0, 0.01, -0.016), (1.17, 0.65, 0.004), pantalla)
    c.caja("Logo", (0, -0.335, -0.016), (0.06, 0.008, 0.003), c.material("logo_tele", (0.6, 0.6, 0.62), aspereza=0.3))
    c.caja("Soporte de muro", (0, 0, 0.04), (0.3, 0.25, 0.03), soporte)
    # Luz de encendido.
    c.caja("Luz", (0.5, -0.338, -0.016), (0.008, 0.004, 0.003), c.material("luz_tele", (0.85, 0.2, 0.15), aspereza=0.3))
