"""
echarpe.py — Écharpe rayée (boîte d'accessoires n°1 : `echarpe`, Peu commun, Cou).

Fiche suivie : docs/specs/boites.md §5.6. Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/echarpe.py

Un seul objet, nommé comme l'identifiant. Origine = point d'accroche Cou = `PointCou`,
Unity (0 ; 1,3 ; 0) local à Joueur : position et rotation locales à zéro.
  - l'anneau entoure le corps (rayon moyen 0,535, 14 cm de haut, 8 cm d'épaisseur) ; son
    bord intérieur (0,495) est juste DANS la capsule (rayon 0,507 ici) : aucun jour visible ;
  - le pan pend devant, à gauche de l'image (Unity x = −0,2), collé au corps, un peu de biais.
Rayures : 12 sur l'anneau, 4 sur le pan, vert menthe / blanc cassé.
"""

import math
import os
import sys

import bmesh
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "echarpe"
EMPLACEMENT = "cou"
MATS = ["M_LaineMenthe", "M_LaineBlanche"]  # indices 0, 1
SEGMENTS = 24            # 12 rayures de 2 segments
Y_COU = 1.3


def anneau():
    bm = bmesh.new()
    z0 = Y_COU
    profil = [(0.495, z0 - 0.06), (0.52, z0 - 0.07), (0.555, z0 - 0.07), (0.577, z0 - 0.045),
              (0.577, z0 + 0.045), (0.555, z0 + 0.07), (0.52, z0 + 0.07), (0.495, z0 + 0.06),
              (0.495, z0 - 0.06)]
    C.bm_revolution(bm, profil, SEGMENTS, mats=[0] * (len(profil) - 1))
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    pas = 2 * math.pi / 12
    for f in bm.faces:
        c = f.calc_center_median()
        a = math.atan2(c.y, c.x) % (2 * math.pi)
        f.material_index = int(a // pas) % 2
    o = C.objet_depuis_bmesh("Anneau", bm, MATS)
    C.lisser_angle(o, 50)
    return o


def pan():
    """Pan de 0,35 × 0,15, 5 cm d'épaisseur, qui suit la surface du corps en descendant."""
    bm = bmesh.new()
    n = 8
    chemin = []
    for k in range(n + 1):
        t = k / n
        y = Y_COU - 0.02 - 0.35 * t           # commence caché sous l'anneau
        x = -0.20 - 0.05 * t                  # un peu de biais vers l'extérieur
        r = C.rayon_corps(y) + 0.03
        z = -math.sqrt(r * r - x * x)
        chemin.append(C.perso(x, y, z))
    radial = Vector((chemin[0].x, chemin[0].y, 0)).normalized()
    section = C.contour_rectangle_arrondi(0.15, 0.05, 0.02, par_coin=1)  # fiche : 0,12, élargi pour la lisibilité
    verts = C.bm_balayage(bm, chemin, section, haut=radial, mat=0)
    haut, bas = chemin[0].z, chemin[-1].z
    for f in bm.faces:
        t = (haut - f.calc_center_median().z) / (haut - bas)   # 0 en haut, 1 en bas
        f.material_index = min(int(t * 4), 3) % 2
    o = C.objet_depuis_bmesh("Pan", bm, MATS)
    C.lisser_angle(o, 50)
    return o


def construire():
    C.nouvelle_scene()
    obj = C.joindre([anneau(), pan()], NOM)
    return C.vers_accroche(obj, EMPLACEMENT)


if __name__ == "__main__":
    C.finir_accessoire(construire(), NOM, EMPLACEMENT)
