"""
coffre_or.py — Coffre de rareté OR (spec v3 §5.3.1 et §16).

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/coffre_or.py
Export : blender/exports/coffre_or.fbx (rien n'est écrit dans le projet Unity).
Miniatures : docs/captures/blender_coffre_or.png (et _ouvert.png).

Ce qui le distingue (rang 3, « coffre royal ») :
    - laque violet royal #5B2D82 (brillante : smoothness 0,7) ;
    - ferrures dorées #E8B830 (métal, smoothness 0,8). Volontairement PAS l'or trésor
      #FFC83A : le doré ne couvre que de petites surfaces, et l'or du trésor reste le signal
      de la victoire quand le Corps est repeint ;
    - le couvercle est bordé d'une FRISE : un bandeau doré surmonté d'une rangée de dents
      (comme une petite couronne posée tout autour du couvercle) ;
    - 2 pierres rouges taillées #D62839 serties sur le haut des bandes du couvercle ;
    - cornières pleine hauteur cloutées d'or, socle doré, écusson de serrure doré.
Doublure intérieure : velours rouge sombre #4A1424.
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import coffre_rarete as R  # noqa: E402

STYLE = {
    "corps": "M_CorpsOr",
    "couvercle": "M_CouvercleOr",
    "metal": "M_Dore",
    "interieur": "M_VeloursRouge",
    "segments_dome": 6,
    "plaque": "ecusson",
    "coins": "cornieres",
    "socle": True,
}

FRISE_BAS, FRISE_HAUT = R.HAUTEUR, R.HAUTEUR + 0.06   # bandeau de 6 cm
DENT_L, DENT_H = 0.06, 0.05                           # une dent : 6 cm de base, 5 cm de haut
PAS = 0.095


def libre(x):
    """Pas de dent devant les bandes ni devant le fermoir (elles s'y cogneraient)."""
    return abs(abs(x) - R.X_BANDES) > 0.075 and abs(x) > 0.09


def construire():
    pieces = R.construire(STYLE)
    metal = [STYLE["metal"]]

    # Clous dorés sur les cornières : 3 par cornière.
    bm = bmesh.new()
    for sx in (-1, 1):
        for z in (0.17, 0.40, 0.63):
            R.bm_rivet(bm, (sx * (R.LARGEUR / 2 - 0.055), R.AVANT + 0.02, z), Vector((0, 1, 0)),
                       rayon=0.024, hauteur=0.016)
    pieces["ferrures"].append(C.objet_depuis_bmesh("Clous", bm, metal))

    # Frise du couvercle : bandeau + dents triangulaires, devant et sur les deux côtés.
    relief = 0.015
    bm = bmesh.new()
    R.bm_lisere(bm, FRISE_BAS, FRISE_HAUT, relief)
    # Dents de devant (contour dans le plan XZ, épaisseur en Y).
    y0 = R.COUV_AVANT - 0.004
    n = int((2 * R.COUV_X - 0.06) / PAS)
    for i in range(n + 1):
        x = -R.COUV_X + 0.03 + i * (2 * R.COUV_X - 0.06) / n
        if not libre(x):
            continue
        tri = [(x - DENT_L / 2, FRISE_HAUT - 0.004), (x + DENT_L / 2, FRISE_HAUT - 0.004),
               (x, FRISE_HAUT + DENT_H)]
        C.bm_prisme(bm, tri, relief + 0.004, axe="Y", decalage=y0)
    # Dents des côtés (contour dans le plan YZ, épaisseur en X), de l'avant vers l'arrière.
    longueur = R.COUV_AVANT - R.COUV_ARRIERE - 0.08
    m = int(longueur / PAS)
    for sx in (-1, 1):
        # le prisme va de x0 à x0 + épaisseur : on le colle sur la face du côté concerné
        x0 = R.COUV_X - 0.004 if sx > 0 else -R.COUV_X + 0.004 - (relief + 0.004)
        for i in range(m + 1):
            y = R.COUV_AVANT - 0.04 - i * longueur / m
            tri = [(y - DENT_L / 2, FRISE_HAUT - 0.004), (y + DENT_L / 2, FRISE_HAUT - 0.004),
                   (y, FRISE_HAUT + DENT_H)]
            C.bm_prisme(bm, tri, relief + 0.004, axe="X", decalage=x0)
    o = C.objet_depuis_bmesh("Frise", bm, metal)
    pieces["couvercle"].append(o)

    # Deux rubis sertis sur le haut des bandes du couvercle (chaton doré + pierre taillée).
    bm = bmesh.new()
    rubis = bmesh.new()
    for sx in (-1, 1):
        p, nrm = R.point_dome(math.pi * 0.42, 6, decalage=R.RELIEF)
        p.x = sx * R.X_BANDES
        R.bm_chaton(bm, p, nrm, 0.052, hauteur=0.018)
        verts = R.bm_gemme(rubis, 0.045)
        bmesh.ops.transform(rubis, matrix=Matrix.Translation(p + nrm * 0.03) @ R.orientation(nrm),
                            verts=verts)
    pieces["couvercle"].append(C.objet_depuis_bmesh("Chatons", bm, metal))
    pieces["couvercle"].append(C.objet_depuis_bmesh("Rubis", rubis, ["M_Rubis"]))
    return R.assembler(pieces)


if __name__ == "__main__":
    R.finir(construire(), "or")
