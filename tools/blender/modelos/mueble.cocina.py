"""Cocina americana del departamento o la casa (mueble.cocina): mueble bajo de 0,90 x 0,90 m con
cubierta de piedra, encimera de dos platos a gas, puertas con tiradores y zócalo. La espalda va
contra el muro (+z).
"""

import comun as c

ANCHO, ALTO, FONDO = 0.9, 0.9, 0.55


def armar():
    mueble = c.material("mueble_cocina", (0.90, 0.89, 0.86), aspereza=0.5)
    piedra = c.material("cubierta_cocina", (0.30, 0.30, 0.32), aspereza=0.35)
    acero = c.material("acero", (0.62, 0.62, 0.62), aspereza=0.3)
    negro = c.material("fierro_quemador", (0.08, 0.08, 0.09), aspereza=0.5)
    zocalo = c.material("zocalo_cocina", (0.25, 0.25, 0.26), aspereza=0.6)

    c.caja("Zocalo", (0, 0.05, 0.02), (ANCHO - 0.02, 0.1, FONDO - 0.06), zocalo)
    c.caja("Cuerpo", (0, 0.1 + 0.39, 0), (ANCHO, 0.78, FONDO), mueble, bisel=0.004)
    c.caja("Cubierta", (0, 0.91, -0.01), (ANCHO + 0.04, 0.04, FONDO + 0.03), piedra, bisel=0.004)
    for lado in (-1, 1):
        c.caja("Puerta", (lado * ANCHO / 4, 0.5, -FONDO / 2 - 0.008), (ANCHO / 2 - 0.012, 0.74, 0.016), mueble, bisel=0.003)
        c.caja("Tirador", (-lado * 0.04, 0.8, -FONDO / 2 - 0.02), (0.012, 0.012, 0.012), acero)
        c.cilindro("Tirador vertical", (-lado * 0.04, 0.74, -FONDO / 2 - 0.022), 0.006, 0.12, acero, eje="y", lados=8)
    # Encimera a gas de dos platos.
    c.caja("Encimera", (0, 0.935, 0.02), (0.6, 0.01, 0.4), acero, bisel=0.003)
    for x in (-0.15, 0.15):
        c.cilindro("Quemador", (x, 0.945, 0.02), 0.06, 0.012, negro, lados=20)
        for giro in (0, 45, 90, 135):
            rejilla = c.caja("Parrilla", (x, 0.955, 0.02), (0.17, 0.008, 0.012), negro)
            c.girar_y(rejilla, giro)
        c.cilindro("Perilla", (x, 0.86, -FONDO / 2 - 0.02), 0.018, 0.02, negro, eje="z", lados=16)
