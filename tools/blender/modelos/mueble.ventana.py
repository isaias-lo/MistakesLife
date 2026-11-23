"""Ventana del muro del fondo (mueble.ventana): 1,40 x 0,90 m de aluminio con dos hojas
correderas, vidrio y alféizar. El centro del vidrio queda a 1,60 m, como la forma simple.
"""

import comun as c


def armar():
    aluminio = c.material("aluminio", (0.80, 0.80, 0.79), aspereza=0.35)
    vidrio = c.material("vidrio", (0.62, 0.78, 0.90), aspereza=0.05)
    alfeizar = c.material("alfeizar", (0.88, 0.86, 0.82), aspereza=0.6)

    y, ancho, alto = 1.6, 1.4, 0.9
    c.caja("Vidrio", (0, y, 0.0), (ancho - 0.04, alto - 0.04, 0.01), vidrio)
    for dy in (-alto / 2, alto / 2):
        c.caja("Marco horizontal", (0, y + dy, -0.01), (ancho + 0.04, 0.05, 0.06), aluminio, bisel=0.003)
    for dx in (-ancho / 2, ancho / 2):
        c.caja("Marco vertical", (dx, y, -0.01), (0.05, alto, 0.06), aluminio, bisel=0.003)
    # Las dos hojas se cruzan al medio.
    c.caja("Hoja que corre", (0.02, y, -0.03), (0.04, alto - 0.04, 0.03), aluminio, bisel=0.002)
    c.caja("Hoja fija", (-0.02, y, -0.005), (0.04, alto - 0.04, 0.03), aluminio, bisel=0.002)
    c.caja("Cierre", (0.045, y, -0.05), (0.015, 0.06, 0.015), c.material("cierre", (0.2, 0.2, 0.2), aspereza=0.4))
    c.caja("Alfeizar", (0, y - alto / 2 - 0.04, -0.06), (ancho + 0.12, 0.03, 0.14), alfeizar, bisel=0.004)
