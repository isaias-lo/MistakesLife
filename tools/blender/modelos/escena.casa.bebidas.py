"""Bebidas de dos litros junto a las bolsas del súper (escena.casa.bebidas)."""

import comun as c


def armar():
    pet = c.material("bebida", (0.30, 0.15, 0.10), aspereza=0.1)
    etiqueta = c.material("etiqueta", (0.82, 0.18, 0.16), aspereza=0.4)
    tapa = c.material("tapa", (0.95, 0.95, 0.95), aspereza=0.4)
    for i in range(2):
        x, z = 1.28 + i * 0.13, -0.1
        c.cilindro("Botella", (x, 0.14, z), 0.052, 0.28, pet, lados=20)
        c.esfera("Hombro", (x, 0.28, z), 0.052, pet, escala=(1, 0.8, 1), lados=20)
        c.cilindro("Cuello", (x, 0.33, z), 0.017, 0.04, pet, lados=12)
        c.cilindro("Tapa", (x, 0.355, z), 0.019, 0.018, tapa, lados=12)
        c.cilindro("Etiqueta", (x, 0.16, z), 0.0535, 0.08, etiqueta, lados=20)
