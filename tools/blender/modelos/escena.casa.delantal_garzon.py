"""El delantal negro largo del restorán con la libreta de comandas en el bolsillo (escena.casa.delantal_garzon)."""

import comun as c
import muebles


def armar():
    z = 0.3
    muebles.gancho(z)
    negro = c.material("delantal", (1, 1, 1), aspereza=0.95, textura=c.textura_tela("tela_delantal", (0.12, 0.12, 0.14), semilla=83))
    c.caja("Tira del cuello", (0.04, 1.3, z), (0.004, 0.1, 0.12), negro)
    c.uv_cubo(c.caja("Delantal", (0.045, 1.0, z), (0.012, 0.62, 0.3), negro, bisel=0.004), escala=0.5)
    c.caja("Bolsillo", (0.054, 0.93, z), (0.006, 0.12, 0.22), c.material("bolsillo", (0.17, 0.17, 0.19), aspereza=0.9))
    c.caja("Libreta de comandas", (0.058, 0.99, z - 0.06), (0.006, 0.09, 0.06), c.material("libreta", (0.96, 0.95, 0.90), aspereza=0.8))
    for lado in (-1, 1):
        c.caja("Amarra", (0.045, 1.02, z + lado * 0.2), (0.004, 0.02, 0.12), negro)
