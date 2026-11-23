"""La guitarra del taller de música apoyada al lado del sillón (escena.casa.guitarra): guitarra
acústica de madera con boca, puente, cuerdas y clavijero.
"""

import comun as c


def armar():
    tapa = c.material("tapa_guitarra", (0.72, 0.50, 0.28), aspereza=0.35)
    aros = c.material("aros_guitarra", (0.38, 0.22, 0.12), aspereza=0.4)
    mastil = c.material("mastil", (0.22, 0.14, 0.09), aspereza=0.5)
    negro = c.material("boca", (0.06, 0.05, 0.04), aspereza=0.8)
    metal = c.material("cuerdas", (0.55, 0.53, 0.48), aspereza=0.2)
    x, z = 0.65, -0.95
    partes = [
        c.esfera("Cuerpo de abajo", (x, 0.175, z), 0.17, aros, escala=(1.0, 1.0, 0.3)),
        c.esfera("Cuerpo de arriba", (x, 0.395, z), 0.13, aros, escala=(1.0, 1.0, 0.3)),
        c.esfera("Tapa de abajo", (x, 0.175, z - 0.006), 0.165, tapa, escala=(1.0, 1.0, 0.28)),
        c.esfera("Tapa de arriba", (x, 0.395, z - 0.006), 0.125, tapa, escala=(1.0, 1.0, 0.28)),
        c.cilindro("Boca", (x, 0.305, z - 0.035), 0.04, 0.01, negro, eje="z", lados=20),
        c.caja("Puente", (x, 0.135, z - 0.045), (0.1, 0.015, 0.01), mastil),
        c.caja("Mastil", (x, 0.725, z - 0.01), (0.05, 0.5, 0.025), mastil, bisel=0.004),
        c.caja("Clavijero", (x, 1.015, z - 0.005), (0.07, 0.13, 0.02), mastil, bisel=0.006),
        c.caja("Cuerdas", (x, 0.58, z - 0.048), (0.02, 0.88, 0.001), metal),
    ]
    for p in partes:
        # Toda la guitarra se inclina 8° hacia el sillón, girando desde el suelo.
        u = (-p.location[0], p.location[2], -p.location[1])
        import math
        a = math.radians(-8)
        dx, dy = u[0] - x, u[1]
        p.location = c.unity(x + dx * math.cos(a) - dy * math.sin(a), dx * math.sin(a) + dy * math.cos(a), u[2])
        c.girar_z(p, -8)
