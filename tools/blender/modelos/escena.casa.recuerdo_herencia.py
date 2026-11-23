"""El reloj de pared antiguo que heredaste, con su péndulo, en el muro (escena.casa.recuerdo_herencia)."""

import comun as c


def armar():
    veta = c.textura_madera("veta_reloj", claro=(0.42, 0.28, 0.17), oscuro=(0.30, 0.19, 0.11), semilla=91, vertical=True)
    madera = c.material("madera_reloj", (1, 1, 1), aspereza=0.45, textura=veta)
    esfera = c.material("esfera_reloj", (0.96, 0.93, 0.85), aspereza=0.5)
    negro = c.material("punteros", (0.05, 0.05, 0.05), aspereza=0.4)
    bronce = c.material("bronce", (0.80, 0.63, 0.28), aspereza=0.25)
    vidrio = c.material("vidrio_reloj", (0.75, 0.80, 0.82), aspereza=0.05)
    z = 1.35
    c.uv_cubo(c.caja("Caja", (0.045, 1.78, z), (0.08, 0.62, 0.3), madera, bisel=0.008), escala=1.0)
    c.uv_cubo(c.caja("Remate", (0.05, 2.11, z), (0.09, 0.05, 0.34), madera, bisel=0.01), escala=1.0)
    c.cilindro("Esfera", (0.087, 1.95, z), 0.11, 0.006, esfera, eje="x", lados=32)
    c.cilindro("Bisel", (0.086, 1.95, z), 0.12, 0.004, bronce, eje="x", lados=32)
    c.caja("Horario", (0.092, 1.975, z), (0.003, 0.055, 0.01), negro)
    c.caja("Minutero", (0.092, 1.95, z + 0.04), (0.003, 0.008, 0.085), negro)
    c.caja("Vidrio", (0.087, 1.66, z), (0.004, 0.3, 0.22), vidrio)
    c.caja("Vara", (0.075, 1.68, z), (0.004, 0.2, 0.008), bronce)
    c.cilindro("Pendulo", (0.075, 1.57, z), 0.045, 0.008, bronce, eje="x", lados=24)
