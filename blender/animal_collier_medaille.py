"""
animal_collier_medaille.py — Collier à médaille (boîte « Animaux : accessoires » :
`collier_medaille`, Commun, Cou). docs/specs/v5-catalogue.md §4 n°04, bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/animal_collier_medaille.py
Sortie : blender/exports/collier_medaille.fbx et docs/captures/v5_cheval_collier_medaille.png.

Comme l'écharpe v5, le collier ÉPOUSE le vrai cou du cheval (rayons lancés depuis l'axe du
cou, commun_animaux.anneau_cou) : plus bas devant, plus haut derrière, sans jour.

Trois niveaux de forme :
  - primaire   : le collier de cuir rouge, épais (11 cm × 5 cm), bords arrondis ;
  - secondaire : la boucle dorée (cadre + ardillon) devant à gauche de l'image, et la
                 GROSSE médaille en forme d'os (0,22 de large : 180 %) qui pend devant ;
  - tertiaire  : 6 rivets dorés sur le cuir, l'anneau qui tient la médaille, le reflet.
Matières : cuir satiné 0,50, doré déco (metallic 1, 0,80), reflet.

Hiérarchie :
    collier_medaille     vide, origine = PointCou (attachScale 1, attachOffset 0)
    └── Collier          le collier, la boucle, les rivets
        └── Anim_Medaille  l'anneau + l'os ; origine = le haut de l'anneau (elle se balance)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_animaux as A  # noqa: E402

NOM = "collier_medaille"
POINT = "PointCou"
MATS = ["M5_CuirRouge", "M5_DoreDeco", "M5_Reflet"]  # 0..2
Y_MILIEU, PENTE = 1.00, 0.05
SECTION = [(-0.012, -0.05), (0.03, -0.056), (0.055, -0.025), (0.055, 0.025),
           (0.03, 0.056), (-0.012, 0.05)]


def construire():
    C.nouvelle_scene()
    bm = bmesh.new()
    bord = A.anneau_cou(bm, Y_MILIEU, PENTE, SECTION, segments=20)
    # boucle : un cadre doré arrondi + l'ardillon, devant à gauche (a = 0,55)
    p, d = bord(0.55)
    m = C.repere(p + d * 0.005, d)
    cadre = C.contour_rectangle_arrondi(0.11, 0.13, 0.025, par_coin=2)
    chemin = [m @ Vector((x, 0.0, z)) for x, z in cadre]
    C.bm_balayage_surface(bm, chemin, [d] * len(chemin),
                          [(-0.016, -0.012), (0.016, -0.012), (0.016, 0.012), (-0.016, 0.012)],
                          mat=1, ferme=True)
    C.bm_plaque(bm, C.contour_rectangle_arrondi(0.016, 0.10, 0.007, par_coin=1), 0.02,
                m @ Matrix.Translation((0.0, 0.012, 0.0)), mat=1)
    # 6 rivets bombés
    for a in (1.1, 1.6, 2.1, -0.6, -1.1, -1.6):
        p, d = bord(a)
        C.bm_disque(bm, 0.022, 0.012, C.repere(p, d), cotes=5, mat=1, bombe=0.008)
    collier = C.piece("Collier", bm, MATS, lissage="tout", pivot=C.U(*C.CHEVAL_ACCROCHES[POINT]))

    # médaille : anneau + os doré, devant, sous le collier
    p, d = bord(0.0)
    haut = p + Vector((0, 0, -0.035)) + d * 0.01
    bm = bmesh.new()
    anneau = C.bm_tore(bm, 0.032, 0.011, segments=10, segments_tube=5, mat=1)
    bmesh.ops.transform(bm, matrix=Matrix.Translation(haut + Vector((0, 0, -0.025)))
                        @ Matrix.Rotation(math.radians(90), 4, "Y"), verts=anneau)
    centre = haut + Vector((0, 0.015, -0.095))
    n = (d + Vector((0, 0, 0.25))).normalized()
    m = C.repere(centre, n)
    C.bm_coussin(bm, C.contour_rectangle_arrondi(0.15, 0.06, 0.02, par_coin=2), 0.035, 0.055,
                 m, mat=1, anneaux=2)
    for sx in (-1, 1):
        for sz in (-1, 1):
            C.bm_coussin(bm, C.contour_ellipse(0.036, 0.036, n=8), 0.035, 0.06,
                         m @ Matrix.Translation((sx * 0.085, 0.0, sz * 0.027)), mat=1, anneaux=1)
    C.bm_reflet(bm, m @ Matrix.Translation((0.03, 0.03, 0.012)), mat=2, echelle=0.8)
    med = C.piece("Anim_Medaille", bm, MATS, lissage="angle", pivot=haut)
    C.parenter(med, collier)
    return C.racine_v5(NOM, POINT, [collier])


if __name__ == "__main__":
    A.finir_animal(construire(), NOM, POINT, y_min=0.55)
