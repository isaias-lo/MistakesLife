"""La maleta de vacaciones parada al lado del ropero (escena.casa.maleta): maleta de cabina rígida
con ruedas, manilla extensible y correa.
"""

import comun as c


def armar():
    casco = c.material("casco_maleta", (0.50, 0.16, 0.15), aspereza=0.35)
    negro = c.material("negro_maleta", (0.10, 0.10, 0.11), aspereza=0.5)
    correa = c.material("correa_maleta", (0.82, 0.72, 0.44), aspereza=0.8)
    x, z = 0.45, -0.3
    c.caja("Maleta", (x, 0.31, z), (0.36, 0.5, 0.2), casco, bisel=0.03, segmentos=4)
    for dz in (-0.05, 0.05):
        c.caja("Nervio", (x, 0.31, z + dz), (0.37, 0.48, 0.01), casco, bisel=0.004)
    c.caja("Correa", (x, 0.31, z - 0.1), (0.37, 0.035, 0.01), correa)
    for dx in (-0.14, 0.14):
        c.cilindro("Barra de la manilla", (x + dx * 0.5, 0.62, z + 0.07), 0.007, 0.14, negro, lados=8)
    c.caja("Manilla", (x, 0.69, z + 0.07), (0.16, 0.02, 0.03), negro, bisel=0.006)
    for dx in (-0.14, 0.14):
        for dz in (-0.07, 0.07):
            c.cilindro("Rueda", (x + dx, 0.03, z + dz), 0.025, 0.02, negro, eje="x", lados=12)
