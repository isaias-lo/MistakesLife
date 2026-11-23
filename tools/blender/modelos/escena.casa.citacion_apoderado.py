"""Te pillaron copiando o haciendo la cimarra (escena.casa.citacion_apoderado): la libreta de
comunicaciones abierta, con la citación al apoderado escrita en rojo por el inspector.
"""

import comun as c
import papeles as p


def pagina(semilla, citacion):
    pix = p.hoja(200, 150, (0.97, 0.96, 0.92), semilla)
    for k in range(1, 12):
        p.rect(pix, k * 0.08, k * 0.08 + 0.004, 0.05, 0.95, (0.72, 0.78, 0.88))
    if citacion:
        p.renglones(pix, 0.1, 0.62, 0.08, 0.92, p.ROJO, semilla, paso=0.08, grueso=0.2, mano=True)
        p.firma(pix, 0.78, 0.6, 0.35, p.ROJO, semilla)
    else:
        p.renglones(pix, 0.1, 0.4, 0.08, 0.92, p.AZUL_LAPIZ, semilla, paso=0.08, grueso=0.2, mano=True)
    return pix


def armar():
    x, z, giro = 0.18, -0.12, 18
    abajo = p.MESA
    azul = c.material("tapa_libreta", (0.20, 0.33, 0.58), aspereza=0.5)
    p.en_mesa("Tapa", azul, x, z, 0.2, 0.14, abajo=abajo, grueso=0.003, giro=giro)
    for lado, semilla, cita in ((-1, 50, False), (1, 51, True)):
        cx, _, cz = p.girar_punto((x + lado * 0.0475, 0, z), (x, z), giro)
        p.en_mesa("Hoja", p.mat_hoja("libreta_%d" % semilla, pagina(semilla, cita)), cx, cz, 0.093, 0.13, abajo=abajo + 0.003, grueso=0.003, giro=giro)
