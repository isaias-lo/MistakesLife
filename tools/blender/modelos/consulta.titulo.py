"""Tu título de psicología enmarcado en la consulta (consulta.titulo): marco oscuro, diploma y sello."""

import comun as c  # noqa: F401
import oficinas as o
import papeles as p  # noqa: F401


def armar():
    pix = p.hoja(180, 240, (0.96, 0.94, 0.88), 97)
    p.rect(pix, 0.12, 0.2, 0.25, 0.75, (0.2, 0.18, 0.15))
    p.renglones(pix, 0.32, 0.7, 0.12, 0.88, (0.35, 0.32, 0.28), 97, paso=0.08, grueso=0.3)
    p.sello(pix, 0.82, 0.78, 0.08, (0.80, 0.62, 0.22), 97, opaco=0.9)
    c.caja('Marco', (0, 0, 0), (0.6, 0.45, 0.035), o._mat('marco_titulo', (0.20, 0.13, 0.08), 0.45), bisel=0.005)
    c.caja('Paspartu', (0, 0, -0.018), (0.53, 0.38, 0.004), o._mat('paspartu_titulo', (0.92, 0.90, 0.86), 0.9))
    lam = c.caja('Diploma', (0, 0, -0.021), (0.44, 0.3, 0.002), p.mat_hoja('titulo_consulta', pix))
    c.uv_plano_frente(lam)
