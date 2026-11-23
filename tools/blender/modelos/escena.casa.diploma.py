"""Tu título enmarcado en el muro (escena.casa.diploma): marco de madera oscura con paspartú,
el diploma con su texto y el sello dorado de la institución.
"""

import numpy as np

import comun as c


def diploma():
    alto, ancho = 256, 352
    pix = np.ones((alto, ancho, 4))
    pix[..., :3] = (0.96, 0.94, 0.88)
    pix[10:13, 10:-10, :3] = (0.55, 0.45, 0.25)
    pix[-13:-10, 10:-10, :3] = (0.55, 0.45, 0.25)
    pix[40:52, 90:262, :3] = (0.20, 0.18, 0.15)
    for fila in range(5):
        y = 80 + fila * 22
        pix[y:y + 4, 60:292 - (fila % 2) * 50, :3] = (0.35, 0.32, 0.28)
    pix[215:218, 40:130, :3] = (0.25, 0.22, 0.20)
    pix[215:218, 222:312, :3] = (0.25, 0.22, 0.20)
    return c.imagen("diploma", pix[::-1])


def armar():
    marco = c.material("marco_diploma", (0.22, 0.14, 0.09), aspereza=0.45)
    passe = c.material("paspartu", (0.92, 0.90, 0.86), aspereza=0.9)
    hoja = c.material("hoja_diploma", (1, 1, 1), aspereza=0.9, textura=diploma())
    oro = c.material("sello", (0.85, 0.65, 0.22), aspereza=0.3)
    c.caja("Marco", (0, 0, 0.0), (0.7, 0.5, 0.035), marco, bisel=0.006)
    c.caja("Paspartu", (0, 0, -0.018), (0.62, 0.42, 0.004), passe)
    lamina = c.caja("Diploma", (0, 0, -0.021), (0.5, 0.36, 0.002), hoja)
    c.uv_plano_frente(lamina)
    c.cilindro("Sello", (0.17, -0.12, -0.024), 0.035, 0.004, oro, eje="z", lados=20)
