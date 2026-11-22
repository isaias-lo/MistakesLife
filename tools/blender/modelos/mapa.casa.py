"""Tu casa en el mapa del barrio (mapa.casa): casa de un piso con techo de tejas, antejardín con
pasto y la reja de fierro de tantas casas de Santiago. Va sobre su manzana (MapBuilder.cs).
"""

import barrio as b

MURO = (0.90, 0.85, 0.75)
TEJA = (0.62, 0.30, 0.22)
ZOCALO = (0.66, 0.62, 0.56)
PUERTA = (0.38, 0.26, 0.17)


def armar():
    b.vereda()
    b.pasto((0, 0.05, -0.85), (3.0, 0.02, 1.1))
    # La casa: 2,1 de frente por 1,5 de fondo, un piso.
    b.caja("Muros", (0, 0.6, 0.45), (2.1, 1.1, 1.5), MURO, bisel=0.01)
    b.caja("Zócalo", (0, 0.1, 0.45), (2.14, 0.12, 1.54), ZOCALO)
    b.techo_dos_aguas("Techo", (0, 1.15, 0.45), 2.1, 1.5, 0.5, TEJA, alero=0.12)
    frente = 0.45 - 0.75
    b.caja("Puerta", (0.45, 0.45, frente - 0.02), (0.36, 0.75, 0.04), PUERTA)
    b.caja("Manilla", (0.32, 0.45, frente - 0.045), (0.03, 0.03, 0.02), (0.75, 0.70, 0.55))
    b.ventana((-0.45, 0.65, frente), 0.6, 0.45)
    b.ventana((0.95, 0.68, frente), 0.18, 0.35)
    b.caja("Peldaño", (0.45, 0.07, frente - 0.12), (0.5, 0.06, 0.2), ZOCALO)
    # Camino de baldosas hasta la reja.
    b.caja("Camino", (0.45, 0.065, -0.95), (0.4, 0.02, 1.05), (0.70, 0.66, 0.60))
    # Reja al frente, con el portón abierto donde llega el camino.
    b.reja((-1.45, 0.06, -1.42), (0.2, 0.06, -1.42))
    b.reja((0.7, 0.06, -1.42), (1.45, 0.06, -1.42))
    b.reja((-1.45, 0.06, -1.42), (-1.45, 0.06, -0.3))
    b.reja((1.45, 0.06, -1.42), (1.45, 0.06, -0.3))
    b.arbol((-1.0, 0.06, -0.95), alto=1.1, semilla=3)
    b.maceta((0.85, 0.06, -0.55))
    b.maceta((0.05, 0.06, -0.55))
