"""
collier_fleurs.py — Collier de fleurs (boîte d'accessoires n°1 : `collier_fleurs`, Peu commun, Cou).

Fiche suivie : docs/specs/boites.md §5.6. Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/collier_fleurs.py

Un seul objet, nommé comme l'identifiant. Origine = point d'accroche Cou = `PointCou`,
Unity (0 ; 1,3 ; 0) local à Joueur : position et rotation locales à zéro.
10 fleurs sur un cercle de rayon 0,545 autour du corps, enfilées sur un fin cordon blanc
cassé. Le collier « pend » : il est incliné, le devant 8 cm plus bas que l'arrière
(devant à y = −0,04, arrière à y = +0,04). Chaque fleur regarde vers l'extérieur et un peu
vers le haut (30°) pour bien se montrer à la caméra, qui est au-dessus.
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "collier_fleurs"
EMPLACEMENT = "cou"
MATS = ["M_PetaleRose", "M_PetaleBlanc", "M_CoeurFleur"]  # indices 0, 1, 2
Y_COU = 1.3
RAYON_FLEURS = 0.545
CHUTE = 0.04             # le devant descend de 4 cm, l'arrière monte de 4 cm
NB_FLEURS = 10
R_FLEUR = 0.09           # fiche : Ø 0,14 ; grossi à 0,18 (bible §3.1 : détails à 130 %)
EPAISSEUR = 0.04
PENTE = math.atan2(CHUTE, RAYON_FLEURS)   # inclinaison du collier (≈ 4°)


def contour_fleur(n_petales=5, par_petale=4):
    """Contour 2D d'une fleur à 5 pétales ronds : 4 points par pétale, le rayon est
    maximal au milieu du pétale et tombe à 45 % dans le creux entre deux pétales."""
    pts = []
    for p in range(n_petales):
        centre = math.pi / 2 + 2 * math.pi * p / n_petales
        demi = math.pi / n_petales                       # 36° : demi-largeur d'un pétale
        for f, r in ((-1.0, 0.45), (-0.45, 0.92), (0.0, 1.0), (0.45, 0.92)):
            a = centre + f * demi
            pts.append((R_FLEUR * r * math.cos(a), R_FLEUR * r * math.sin(a)))
    return pts


def point_cercle(theta, rayon):
    """Point du cercle incliné. theta = 0 devant (+Y Blender), en tournant vers +X."""
    x, y = rayon * math.sin(theta), rayon * math.cos(theta)
    return Vector((x, y, Y_COU - CHUTE * (y / rayon)))


def construire():
    C.nouvelle_scene()
    bm = bmesh.new()
    contour = contour_fleur()
    for k in range(NB_FLEURS):
        theta = 2 * math.pi * k / NB_FLEURS
        p = point_cercle(theta, RAYON_FLEURS)
        radial = Vector((math.sin(theta), math.cos(theta), 0))
        avant = (radial * math.cos(math.radians(30)) + Vector((0, 0, math.sin(math.radians(30)))))
        m = C.repere(p, avant) @ Matrix.Rotation(theta * 0.7, 4, "Y")  # pétales pas tous alignés
        C.bm_plaque(bm, contour, EPAISSEUR, m, mat=k % 2)               # rose / blanc cassé
        coeur = m @ Vector((0, EPAISSEUR / 2, 0))
        v = C.bm_sphere(bm, 1.0, (0, 0, 0), segments=6, anneaux=3, echelle=(0.035, 0.022, 0.035),
                        mat=2)
        bmesh.ops.transform(bm, matrix=Matrix.Translation(coeur) @ m.to_3x3().to_4x4(), verts=v)
    fleurs = C.objet_depuis_bmesh("Fleurs", bm, MATS)
    C.lisser_angle(fleurs, 40)

    # Cordon : tore fin (Ø 4 cm), même inclinaison que le collier, caché en partie
    # derrière les fleurs ; il évite les « trous » entre deux fleurs.
    bm = bmesh.new()
    m = Matrix.Translation((0, 0, Y_COU)) @ Matrix.Rotation(-PENTE, 4, "X")
    C.bm_tore(bm, 0.53, 0.02, segments=24, segments_tube=4, mat=1, matrice=m)
    cordon = C.objet_depuis_bmesh("Cordon", bm, MATS)
    C.lisser_tout(cordon)

    obj = C.joindre([fleurs, cordon], NOM)
    return C.vers_accroche(obj, EMPLACEMENT)


if __name__ == "__main__":
    C.finir_accessoire(construire(), NOM, EMPLACEMENT)
