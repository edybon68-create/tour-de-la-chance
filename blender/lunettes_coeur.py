"""
lunettes_coeur.py — Lunettes cœur (boîte d'accessoires n°1 : `lunettes_coeur`, Peu commun, Visage).

Fiche suivie : docs/specs/boites.md §5.6. Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/lunettes_coeur.py

VERSION CHEVAL (personnage « cavalier d'échecs », blender/personnage_cheval.py).
Un seul objet, nommé comme l'identifiant. Origine = point d'accroche Visage = `PointVisage`
du cheval, Unity (0 ; 1,68 ; −0,253) local à Joueur : position et rotation locales à zéro,
échelle 1. Même placement que les lunettes de soleil (verres 4 cm devant les yeux, ouverts
de 10°, branches collées au crâne par des rayons). Sortie : blender/exports/lunettes_coeur.fbx.
Construction d'un verre : un cœur blanc cassé (la monture, 0,26 × 0,24) et, par-dessus, un
cœur rose un peu plus petit et un peu plus épais : il dépasse devant et derrière, et le
blanc reste visible tout autour comme un bord de 2 cm.
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "lunettes_coeur"
POINT = "PointVisage"
MATS = ["M_VerreRose", "M_BlancCasse", "M_Reflet"]  # indices 0, 1, 2
EPAISSEUR = 0.04
LARGEUR, HAUTEUR = 0.26, 0.24     # le cœur couvre l'œil du cheval (0,17 × 0,21)
BORD = 0.02
AVANCE = 0.04            # 4 cm devant l'œil
OUVERTURE = 10.0         # degrés vers le côté
MONTEE = 0.025           # un peu plus haut que les yeux (caméra au-dessus)


def contour_coeur(largeur, hauteur, n=28):
    """Contour 2D (x, z) d'un cœur, pointe en bas, centré sur (0, 0).
    Courbe classique : x = 16 sin³t, z = 13 cos t − 5 cos 2t − 2 cos 3t − cos 4t."""
    pts = []
    for k in range(n):
        t = 2 * math.pi * k / n
        x = 16 * math.sin(t) ** 3
        z = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        pts.append((x, z))
    xs, zs = [p[0] for p in pts], [p[1] for p in pts]
    cx, cz = (max(xs) + min(xs)) / 2, (max(zs) + min(zs)) / 2
    sx, sz = largeur / (max(xs) - min(xs)), hauteur / (max(zs) - min(zs))
    # Sens inverse des aiguilles d'une montre vu de devant (+Y) : on inverse la liste.
    return [((x - cx) * sx, (z - cz) * sz) for x, z in reversed(pts)]


def repere_verre_cheval(x_unity):
    """Repère du verre devant l'œil situé en x_unity (Y local = vers l'avant, un peu ouvert)."""
    cote = 1 if x_unity < 0 else -1          # côté Blender (+X = gauche de l'image)
    a = math.radians(OUVERTURE)
    centre = C.depuis_unity(x_unity, C.CHEVAL_YEUX_Y + MONTEE, C.CHEVAL_YEUX_AVANT - AVANCE)
    return C.repere(centre, Vector((cote * math.sin(a), math.cos(a), 0.0)))


def verre(bm, x_unity):
    m = repere_verre_cheval(x_unity)
    C.bm_plaque(bm, contour_coeur(LARGEUR, HAUTEUR), EPAISSEUR, m, mat=1)          # monture
    C.bm_plaque(bm, contour_coeur(LARGEUR - 2 * BORD, HAUTEUR - 2 * BORD),
                EPAISSEUR + 0.008, m @ Matrix.Translation((0, 0, 0.004)), mat=0)    # verre rose
    # Reflet blanc en haut à gauche de l'image (+X local), sur le lobe gauche du cœur.
    rot = Matrix.Rotation(math.radians(35), 4, "Y")
    pos = Matrix.Translation((0.05, EPAISSEUR / 2 + 0.006, 0.035))
    v = C.bm_boite(bm, (0.05, 0.006, 0.02), (0, 0, 0), mat=2)
    bmesh.ops.transform(bm, matrix=m @ pos @ rot, verts=v)
    return m


def construire():
    C.nouvelle_scene()
    arbre = C.arbre_cheval()   # la forme de la tête, pour coller les branches
    bm = bmesh.new()
    section = C.contour_rectangle_arrondi(0.025, 0.04, 0.01, par_coin=1)  # branches 2,5 × 4 cm
    for x_unity in (-C.CHEVAL_YEUX_X, +C.CHEVAL_YEUX_X):
        m = verre(bm, x_unity)
        cote = 1 if x_unity < 0 else -1   # +X Blender = gauche de l'image
        chemin = [m @ Vector((cote * (LARGEUR / 2 - 0.03), -0.005, 0.05))]
        for angle, ecart in ((52, 0.03), (66, 0.022), (80, 0.018), (94, -0.03)):
            p, _ = C.sur_cheval(arbre, math.radians(cote * angle),
                                C.CHEVAL_YEUX_Y + MONTEE + 0.05, ecart)
            chemin.append(p)
        C.bm_balayage(bm, chemin, section, mat=1)
    # Pont blanc entre les deux lobes intérieurs.
    pont = []
    for k in range(5):
        a = math.radians(-8 + 16 * k / 4)
        p, _ = C.sur_cheval(arbre, a, C.CHEVAL_YEUX_Y + MONTEE + 0.06, AVANCE)
        pont.append(p)
    C.bm_balayage(bm, pont, section, mat=1)

    obj = C.objet_depuis_bmesh(NOM, bm, MATS)
    C.lisser_angle(obj, 40)
    return C.vers_accroche_cheval(obj, POINT)


if __name__ == "__main__":
    C.finir_accessoire_cheval(construire(), NOM, POINT)
