"""
animal_truffe.py — Truffe de chien (boîte « Animaux : accessoires » : `truffe`, Commun,
Visage). docs/specs/v5-catalogue.md §4 n°03, bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/animal_truffe.py
Sortie : blender/exports/truffe.fbx et docs/captures/v5_cheval_truffe.png.

Trois niveaux de forme :
  - primaire   : une GROSSE truffe bombée en triangle arrondi (0,33 × 0,24, 17 cm
                 d'épaisseur : 150 % d'une truffe « logique »), au bout du museau, qui
                 recouvre les narines du cheval ;
  - secondaire : deux narines en virgule, creusées (noir mat), et le sillon du milieu ;
  - tertiaire  : le reflet peint (barre + point) en haut à gauche.
Matières : noir verni 0,90 (truffe) contre noir mat 0,30 (narines) : la différence de
brillance dessine les narines sans changer de couleur ; reflet blanc.

Hiérarchie :
    truffe          vide, origine = PointVisage (attachScale 1, attachOffset 0)
    └── Anim_Truffe la truffe ; origine = son centre (« renifle » : échelle 1 ↔ 1,06 toutes
                    les 4 s)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_animaux as A  # noqa: E402

NOM = "truffe"
POINT = "PointVisage"
MATS = ["M5_TruffeNoire", "M5_TruffeNarine", "M5_Reflet"]  # 0..2


def contour_truffe(l, h, n=20):
    """Triangle très arrondi, pointe en bas (forme de truffe de chiot)."""
    pts = []
    for k in range(n):
        t = 2 * math.pi * k / n
        x = math.cos(t)
        z = math.sin(t)
        largeur = 1.0 - 0.32 * max(0.0, -z)          # plus étroite en bas
        pts.append((x * l / 2 * largeur, z * h / 2 + 0.02 * x * x * (1 if z > 0 else 0)))
    return pts


def construire():
    C.nouvelle_scene()
    p, _ = A.sur_museau(0.0, 1.34, 0.0)
    # normale « moyenne » du bout du museau (il pique de 18° vers le bas) : plus stable
    # que la normale d'une seule face touchée par le rayon
    n = Vector((0.0, math.cos(math.radians(14)), -math.sin(math.radians(14))))
    centre = p + n * 0.03
    m = C.repere(centre, n)
    bm = bmesh.new()
    C.bm_coussin(bm, contour_truffe(0.33, 0.24), 0.05, 0.17, m, mat=0, anneaux=3)
    # narines : deux virgules creusées (posées dans le bombé, mat)
    for s in (-1, 1):
        virgule = [(s * x, z) for x, z in ((0.0, 0.018), (0.035, 0.02), (0.05, 0.0),
                                           (0.035, -0.022), (0.012, -0.03), (0.005, -0.01))]
        if s < 0:
            virgule.reverse()
        C.bm_plaque(bm, virgule, 0.02, m @ Matrix.Translation((s * 0.04, 0.075, -0.015)),
                    mat=1)
    # sillon : une petite barre mate sous la truffe, au milieu
    C.bm_plaque(bm, C.contour_rectangle_arrondi(0.014, 0.06, 0.006, par_coin=1), 0.02,
                m @ Matrix.Translation((0, 0.06, -0.075)), mat=1)
    C.bm_reflet(bm, m @ Matrix.Translation((0.07, 0.07, 0.05)), mat=2, echelle=1.1)
    truffe = C.piece("Anim_Truffe", bm, MATS, lissage="tout", pivot=centre)
    return C.racine_v5(NOM, POINT, [truffe])


if __name__ == "__main__":
    A.finir_animal(construire(), NOM, POINT)
