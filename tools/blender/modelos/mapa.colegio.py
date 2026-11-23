"""El colegio (mapa.colegio): pabellón de dos pisos con pasillo techado y ventanas en fila, un
ala lateral, el patio de cemento con la cancha marcada, el mástil y la reja del colegio.
"""

import barrio as b

MURO = (0.90, 0.86, 0.76)
FRANJA = (0.56, 0.30, 0.24)
PATIO = (0.70, 0.69, 0.66)
LINEA = (0.92, 0.92, 0.90)


def armar():
    b.vereda()
    # Pabellón principal al fondo, de dos pisos.
    b.caja("Pabellón", (0, 0.75, 1.0), (3.0, 1.4, 0.9), MURO, bisel=0.01)
    b.caja("Techo", (0, 1.48, 1.0), (3.1, 0.06, 1.0), (0.45, 0.45, 0.47))
    frente = 1.0 - 0.45
    for piso, y in enumerate((0.42, 1.05)):
        b.caja("Franja", (0, y - 0.3 + 0.03, frente - 0.01), (3.0, 0.05, 0.03), FRANJA)
        for i in range(7):
            b.ventana((-1.2 + i * 0.4, y, frente), 0.26, 0.32)
    b.caja("Pasillo techado", (0, 0.62, frente - 0.2), (3.0, 0.04, 0.4), (0.60, 0.60, 0.62))
    for i in range(6):
        b.caja("Pilar", (-1.4 + i * 0.56, 0.31, frente - 0.38), (0.05, 0.62, 0.05), MURO)
    # Ala lateral de un piso.
    b.caja("Ala", (-1.15, 0.4, 0.0), (0.7, 0.7, 1.1), MURO, bisel=0.01)
    b.caja("Techo del ala", (-1.15, 0.77, 0.0), (0.78, 0.05, 1.18), (0.45, 0.45, 0.47))
    for z in (-0.3, 0.15):
        b.ventana((-0.8, 0.42, z), 0.3, 0.3, frente="+x")
    # Patio con la cancha marcada.
    b.caja("Patio", (0.35, 0.05, -0.55), (2.3, 0.02, 1.7), PATIO)
    for z in (-1.25, 0.15):
        b.caja("Línea", (0.35, 0.065, z), (1.9, 0.01, 0.025), LINEA)
    for x in (-0.6, 1.3):
        b.caja("Línea", (x, 0.065, -0.55), (0.025, 0.01, 1.4), LINEA)
    b.caja("Línea", (0.35, 0.065, -0.55), (0.025, 0.01, 1.4), LINEA)
    for x in (-0.5, 1.2):
        b.cilindro("Poste del aro", (x, 0.35, -0.55), 0.02, 0.6, b.FIERRO, lados=6)
        b.caja("Tablero", (x + (0.04 if x < 0 else -0.04), 0.62, -0.55), (0.02, 0.16, 0.24), (0.92, 0.92, 0.90))
    b.cilindro("Mástil", (1.4, 0.7, 0.3), 0.015, 1.3, (0.78, 0.78, 0.78), lados=6)
    b.reja((-1.55, 0.06, -1.45), (-0.3, 0.06, -1.45), alto=0.3)
    b.reja((0.3, 0.06, -1.45), (1.55, 0.06, -1.45), alto=0.3)
    b.arbol((-1.2, 0.06, -1.05), alto=1.0, semilla=61)
