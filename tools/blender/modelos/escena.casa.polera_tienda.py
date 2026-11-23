"""La polera de la tienda colgada junto a la puerta, con la piocha del nombre (escena.casa.polera_tienda)."""

import comun as c
import muebles


def armar():
    z = 0.3
    muebles.gancho(z)
    muebles.colgada("polera_tienda", z, 1.32, 0.42, 0.3, (0.20, 0.24, 0.32))
    c.caja("Piocha", (0.062, 1.2, z + 0.08), (0.004, 0.03, 0.07), c.material("piocha", (0.95, 0.94, 0.90), aspereza=0.4), bisel=0.002)
