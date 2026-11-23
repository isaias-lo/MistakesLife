"""La tarjeta del trabajo colgada de su cordón junto a la puerta (escena.casa.credencial_pega)."""

import comun as c
import muebles


def armar():
    z = 0.52
    muebles.gancho(z)
    c.caja("Cordon", (0.04, 1.24, z), (0.006, 0.2, 0.014), c.material("cordon_rojo", (0.55, 0.18, 0.16), aspereza=0.8))
    c.caja("Porta tarjeta", (0.045, 1.1, z), (0.006, 0.12, 0.085), c.material("plastico", (0.85, 0.88, 0.90), aspereza=0.15), bisel=0.004)
    c.caja("Tarjeta", (0.049, 1.1, z), (0.002, 0.105, 0.075), c.material("tarjeta", (0.96, 0.95, 0.92), aspereza=0.6))
    c.caja("Foto", (0.0505, 1.12, z - 0.018), (0.001, 0.04, 0.03), c.material("foto", (0.55, 0.45, 0.38), aspereza=0.6))
    c.caja("Franja de la empresa", (0.0505, 1.065, z), (0.001, 0.015, 0.075), c.material("franja_empresa", (0.20, 0.35, 0.60), aspereza=0.6))
