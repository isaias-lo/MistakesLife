"""Las tareas del mes en la pega (escena.casa.agenda_pega): la agenda de tapa oscura abierta en la
semana, con lo que falta anotado, tres post-it en el borde y un lápiz encima.
"""

import comun as c
import papeles as p


def semana(semilla):
    pix = p.hoja(320, 220, (0.97, 0.96, 0.92), semilla)
    p.rect(pix, 0.03, 0.06, 0.06, 0.6, (0.3, 0.32, 0.38))
    for dia in range(4):
        y = 0.1 + dia * 0.225
        p.rect(pix, y, y + 0.006, 0.06, 0.94, (0.55, 0.58, 0.65))
        p.renglones(pix, y + 0.04, y + 0.2, 0.08, 0.9, p.AZUL_LAPIZ, semilla + dia, paso=0.05, grueso=0.3, mano=True)
    return pix


def armar():
    tapa = c.material("tapa_agenda", (0.15, 0.18, 0.25), aspereza=0.5)
    x, z, abajo = 0.42, -0.22, p.MESA
    c.caja("Tapa", (x, abajo + 0.002, z), (0.36, 0.004, 0.24), tapa, bisel=0.002)
    c.caja("Hojas", (x, abajo + 0.007, z), (0.34, 0.006, 0.22), c.material("canto_agenda", (0.93, 0.91, 0.86), aspereza=0.9))
    p.en_mesa("Hoja izquierda", p.mat_hoja("agenda_izq", semana(21)), x - 0.0855, z, 0.168, 0.215, abajo=abajo + 0.01, grueso=0.001)
    p.en_mesa("Hoja derecha", p.mat_hoja("agenda_der", semana(31)), x + 0.0855, z, 0.168, 0.215, abajo=abajo + 0.01, grueso=0.001)
    c.cilindro("Cinta marcadora", (x, abajo + 0.0112, z + 0.02), 0.002, 0.2, c.material("cinta_agenda", (0.65, 0.15, 0.15), aspereza=0.6), eje="z", lados=6)
    amarillo = c.material("post_it", (0.97, 0.86, 0.35), aspereza=0.8)
    for i in range(3):
        o = c.caja("Post-it", (0.6, abajo + 0.0115 + i * 0.0004, -0.3 + i * 0.07), (0.05, 0.0008, 0.05), amarillo)
        c.girar_y(o, -4 + i * 5)
    p.lapiz(0.48, -0.19, 30, color=(0.20, 0.22, 0.30), abajo=abajo + 0.011)
