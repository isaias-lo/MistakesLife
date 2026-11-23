"""Tienes un negocio con un socio (escena.casa.negocio): la caja chica metálica con su manilla y
cerradura, y el cuaderno de cuentas de tapa roja abierto en las columnas de entradas y salidas.
"""

import comun as c
import papeles as p


def columnas(semilla):
    pix = p.hoja(240, 170, (0.96, 0.95, 0.90), semilla)
    for k in range(1, 20):
        p.rect(pix, k * 0.05, k * 0.05 + 0.003, 0.03, 0.97, (0.70, 0.78, 0.88))
    for x in (0.18, 0.62, 0.8):
        p.rect(pix, 0.03, 0.97, x, x + 0.006, (0.85, 0.45, 0.45))
    p.renglones(pix, 0.08, 0.8, 0.2, 0.6, p.AZUL_LAPIZ, semilla, paso=0.05, grueso=0.25, mano=True)
    p.renglones(pix, 0.08, 0.8, 0.64, 0.78, p.AZUL_LAPIZ, semilla + 1, paso=0.05, grueso=0.25, mano=True)
    return pix


def armar():
    verde = c.material("caja_chica", (0.24, 0.40, 0.35), aspereza=0.35)
    metal = c.material("metal_caja", (0.72, 0.72, 0.75), aspereza=0.25)
    c.caja("Caja chica", (-0.25, p.MESA + 0.035, 0.25), (0.2, 0.07, 0.14), verde, bisel=0.004)
    c.caja("Tapa", (-0.25, p.MESA + 0.072, 0.25), (0.202, 0.006, 0.142), verde, bisel=0.002)
    c.caja("Manilla", (-0.25, p.MESA + 0.08, 0.25), (0.08, 0.008, 0.012), metal, bisel=0.003)
    c.caja("Cerradura", (-0.25, p.MESA + 0.055, 0.25 - 0.071), (0.02, 0.016, 0.003), metal, bisel=0.001)
    x, z = -0.02, 0.27
    rojo = c.material("tapa_cuentas", (0.58, 0.18, 0.16), aspereza=0.5)
    c.caja("Tapa del cuaderno", (x, p.MESA + 0.002, z), (0.32, 0.004, 0.22), rojo, bisel=0.002)
    c.caja("Hojas", (x, p.MESA + 0.006, z), (0.31, 0.005, 0.21), c.material("canto_cuentas", (0.93, 0.92, 0.87), aspereza=0.9))
    for lado, semilla in ((-1, 301), (1, 303)):
        p.en_mesa("Hoja de cuentas", p.mat_hoja("cuentas_negocio_%d" % semilla, columnas(semilla)), x + lado * 0.0775, z, 0.15, 0.205, abajo=p.MESA + 0.0085, grueso=0.001)
