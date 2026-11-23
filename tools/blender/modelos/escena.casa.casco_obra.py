"""El casco blanco y los zapatos de seguridad junto a la puerta (escena.casa.casco_obra)."""

import comun as c


def armar():
    blanco = c.material("casco", (0.95, 0.95, 0.93), aspereza=0.3)
    cuero = c.material("cuero", (0.33, 0.23, 0.16), aspereza=0.6)
    punta = c.material("puntera", (0.22, 0.16, 0.11), aspereza=0.5)
    suela = c.material("suela_seguridad", (0.12, 0.11, 0.10), aspereza=0.8)
    c.esfera("Casco", (0.32, 0.0, 0.05), 0.13, blanco, escala=(1.0, 0.85, 1.12))
    c.caja("Ala del casco", (0.32, 0.012, 0.05), (0.27, 0.012, 0.31), blanco, bisel=0.006)
    c.caja("Nervio", (0.32, 0.11, 0.05), (0.03, 0.02, 0.26), blanco, bisel=0.008)
    for z in (-0.25, -0.39):
        c.caja("Suela", (0.32, 0.015, z), (0.3, 0.03, 0.115), suela, bisel=0.01)
        c.caja("Bota", (0.3, 0.08, z), (0.24, 0.11, 0.105), cuero, bisel=0.025, segmentos=3)
        c.caja("Cana", (0.22, 0.14, z), (0.1, 0.1, 0.1), cuero, bisel=0.02)
        c.esfera("Puntera", (0.41, 0.06, z), 0.055, punta, escala=(1.0, 0.85, 0.95))
