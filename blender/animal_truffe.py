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
MATS = ["M5_TruffeNoire", "M5_TruffeNarine", "M5_Reflet", "M5_NezRose"]  # 0..3


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
    n = Vector((0.0, math.cos(math.radians(14)), 0.25 - math.sin(math.radians(14)))).normalized()   # V7 : un peu vers la caméra
    centre = p + n * 0.03
    m = C.repere(centre, n)
    bm = bmesh.new()
    # V7 (lot 10) : plus grosse, très bombée et lisse, comme la nouvelle icône
    C.bm_coussin(bm, contour_truffe(0.38, 0.28, n=28), 0.06, 0.20, m, mat=0, anneaux=4)
    # narines : deux virgules creusées (posées dans le bombé, mat)
    for s in (-1, 1):
        # V7 : narines OVALES nettes (icône), posées sur le devant bombé
        C.bm_coussin(bm, C.contour_ellipse(0.028, 0.042, n=14), 0.01, 0.015,
                     m @ Matrix.Translation((s * 0.06, 0.098, 0.0)) @ Matrix.Rotation(math.radians(-s * 15), 4, "Y"),
                     mat=1, anneaux=1, dos_plat=True)
    # sillon : une petite barre mate sous la truffe, au milieu
    # V7 (lot 10) : petit bout de langue rose qui dépasse sous la truffe (icône)
    C.bm_coussin(bm, C.contour_ellipse(0.045, 0.05, n=16), 0.02, 0.04,
                 m @ Matrix.Translation((0.0, 0.03, -0.15)), mat=3, anneaux=2)
    C.bm_reflet(bm, m @ Matrix.Translation((0.07, 0.07, 0.05)), mat=2, echelle=1.1)
    truffe = C.piece("Anim_Truffe", bm, MATS, lissage="tout", pivot=centre)
    return C.racine_v5(NOM, POINT, [truffe])


if __name__ == "__main__":
    A.finir_animal(construire(), NOM, POINT)
