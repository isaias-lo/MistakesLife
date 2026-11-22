"""La casa de tus amigos en el mapa (mapa.casa_amigos): dos casas pareadas de dos pisos, cada una
de su color, como en las villas de Santiago, con antejardín y reja.
"""

import barrio as b

TEJA = (0.38, 0.40, 0.44)
ZOCALO = (0.62, 0.60, 0.56)


def casa(x, color, puerta_a_la_izquierda):
    b.caja("Muros", (x, 0.82, 0.5), (1.4, 1.55, 1.3), color, bisel=0.01)
    b.caja("Zócalo", (x, 0.1, 0.5), (1.42, 0.12, 1.32), ZOCALO)
    frente = 0.5 - 0.65
    puerta = x - 0.35 if puerta_a_la_izquierda else x + 0.35
    ventana = x + 0.25 if puerta_a_la_izquierda else x - 0.25
    b.caja("Puerta", (puerta, 0.42, frente - 0.02), (0.32, 0.7, 0.04), (0.30, 0.22, 0.16))
    b.ventana((ventana, 0.55, frente), 0.5, 0.4)
    b.ventana((x - 0.3, 1.25, frente), 0.4, 0.35)
    b.ventana((x + 0.3, 1.25, frente), 0.4, 0.35)
    b.caja("Camino", (puerta, 0.065, -0.85), (0.34, 0.02, 1.1), (0.70, 0.66, 0.60))


def armar():
    b.vereda()
    b.pasto((0, 0.05, -0.85), (3.0, 0.02, 1.1))
    casa(-0.72, (0.85, 0.80, 0.66), True)
    casa(0.72, (0.70, 0.76, 0.72), False)
    b.techo_dos_aguas("Techo", (0, 1.6, 0.5), 2.8, 1.3, 0.45, TEJA, alero=0.1)
    b.caja("Entrada de autos", (0.95, 0.07, -0.85), (0.9, 0.02, 1.1), (0.72, 0.70, 0.66))
    b.caja("Muro medianero", (0, 0.35, -0.85), (0.04, 0.5, 1.1), (0.80, 0.78, 0.72))
    b.reja((-1.45, 0.06, -1.42), (-1.25, 0.06, -1.42))
    b.reja((-0.9, 0.06, -1.42), (0.5, 0.06, -1.42))
    
    b.arbol((-0.05, 0.06, -1.25), alto=0.9, semilla=5)
    b.auto((0.95, 0.06, -0.8), (0.55, 0.12, 0.12), giro=90)
