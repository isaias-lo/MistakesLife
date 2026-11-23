"""Te preparas para dar la PAES otra vez (escena.casa.facsimiles_paes): la pila de facsímiles del
preu corcheteados, el de arriba corregido con el puntaje en rojo, y el lápiz grafito.
"""

import comun as c
import papeles as p


def facsimil(semilla, puntaje):
    pix = p.hoja(300, 220, (0.97, 0.97, 0.95), semilla)
    p.rect(pix, 0.04, 0.12, 0.06, 0.94, (0.30, 0.33, 0.40))
    for f in range(6):
        y = 0.18 + f * 0.13
        p.renglones(pix, y, y + 0.05, 0.08, 0.92, p.TINTA, semilla + f, paso=0.025, grueso=0.4)
        for k in range(5):
            cx = 0.14 + k * 0.08
            p.borde(pix, y + 0.065, y + 0.095, cx, cx + 0.04, (0.4, 0.4, 0.45), 1)
            if puntaje and (k + f) % 5 == 1:
                p.rect(pix, y + 0.068, y + 0.092, cx + 0.005, cx + 0.035, (0.2, 0.2, 0.22))
    if puntaje:
        p.numero(pix, "712", 0.03, 0.62, 0.09, p.ROJO)
        p.circulo_a_mano(pix, 0.075, 0.74, 0.07, 0.2, p.ROJO, grueso=0.12)
    return pix


def armar():
    x, z = -0.1, 0.18
    metal = c.material("corchete", (0.7, 0.7, 0.73), aspereza=0.3)
    for i in range(4):
        giro = -6 + i * 4
        mat = p.mat_hoja("facsimil_%d" % i, facsimil(110 + i, i == 3))
        abajo = p.MESA + i * 0.003
        o = c.caja("Canto", (x, abajo + 0.001, z), (0.168, 0.002, 0.228), c.material("canto_facsimil", (0.93, 0.92, 0.88), aspereza=0.9))
        c.girar_y(o, giro)
        p.en_mesa("Facsimil", mat, x, z, 0.17, 0.23, abajo=abajo + 0.002, grueso=0.001, giro=giro)
        cx, _, cz = p.girar_punto((x - 0.07, 0, z - 0.1), (x, z), giro)
        o = c.caja("Corchete", (cx, abajo + 0.0032, cz), (0.012, 0.0006, 0.0015), metal)
        c.girar_y(o, giro + 45)
    p.lapiz(0.04, 0.1, 30, abajo=p.MESA)
