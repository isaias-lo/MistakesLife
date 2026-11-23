"""Tu celular sobre la mesa (escena.casa.celular): smartphone con marco delgado y pantalla apagada."""

import comun as c


def armar():
    cuerpo = c.material("celular", (0.05, 0.06, 0.10), aspereza=0.3)
    pantalla = c.material("pantalla_celular", (0.08, 0.08, 0.10), aspereza=0.08)
    camara = c.material("camara", (0.15, 0.15, 0.18), aspereza=0.2)
    t = c.caja("Celular", (0.0, 0.4485, 0.36), (0.075, 0.009, 0.155), cuerpo, bisel=0.004, segmentos=3)
    c.girar_y(t, 12)
    p = c.caja("Pantalla", (0.0, 0.4535, 0.36), (0.068, 0.001, 0.146), pantalla)
    c.girar_y(p, 12)
    m = c.caja("Camara", (0.0, 0.4545, 0.432), (0.012, 0.001, 0.004), camara)
    c.girar_y(m, 12)
