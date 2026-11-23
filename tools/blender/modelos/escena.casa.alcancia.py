"""El chanchito de las monedas sobre la mesa (escena.casa.alcancia): de greda rosada, con
patitas, orejas, nariz y ranura.
"""

import comun as c


def armar():
    rosado = c.material("chanchito", (0.93, 0.62, 0.66), aspereza=0.4)
    nariz = c.material("nariz_chanchito", (0.88, 0.50, 0.56), aspereza=0.4)
    negro = c.material("ojos_chanchito", (0.1, 0.08, 0.08), aspereza=0.3)
    x, y, z = -0.6, 0.53, 0.15
    c.esfera("Cuerpo", (x, y, z), 0.08, rosado, escala=(0.95, 0.9, 1.2))
    c.cilindro("Nariz", (x, y - 0.005, z - 0.1), 0.03, 0.03, nariz, eje="z", lados=18)
    for dx in (-0.045, 0.045):
        c.esfera("Oreja", (x + dx, y + 0.065, z - 0.05), 0.022, rosado, escala=(1.0, 0.6, 0.4))
        c.esfera("Ojo", (x + dx * 0.6, y + 0.025, z - 0.088), 0.007, negro, lados=8)
        for dz in (-0.06, 0.06):
            c.cilindro("Pata", (x + dx, 0.465, z + dz), 0.017, 0.05, rosado, lados=10)
    c.caja("Ranura", (x, y + 0.072, z), (0.008, 0.006, 0.05), negro)
