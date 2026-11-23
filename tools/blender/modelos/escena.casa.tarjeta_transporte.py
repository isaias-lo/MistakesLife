"""La tarjeta del transporte colgada de un gancho junto a la puerta (escena.casa.tarjeta_transporte)."""

import comun as c
import muebles


def armar():
    z = 0.7
    muebles.gancho(z)
    c.caja("Cordon", (0.04, 1.25, z), (0.006, 0.18, 0.01), c.material("cordon_rojo", (0.80, 0.22, 0.20), aspereza=0.8))
    c.caja("Tarjeta", (0.045, 1.13, z), (0.004, 0.054, 0.086), c.material("tarjeta_azul", (0.20, 0.45, 0.80), aspereza=0.3), bisel=0.003)
    c.caja("Franja", (0.0475, 1.13, z), (0.001, 0.012, 0.086), c.material("franja_blanca", (0.95, 0.95, 0.95), aspereza=0.3))
