"""
chapeau_bonnet.py — Bonnet de laine (boîte d'accessoires n°1 : `chapeau_bonnet`, Commun, Tête).
VERSION v5 « plus jeu moderne » (docs/art-bible.md §13.8 fiche 3), fait SUR le cheval.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/chapeau_bonnet.py
Sortie : blender/exports/chapeau_bonnet.fbx et docs/captures/v5_cheval_chapeau_bonnet.png.

Les trois niveaux de forme (bible §13.2) :
  - primaire   : le dôme turquoise « mou », dont le haut retombe un peu vers l'arrière
                 gauche (l'asymétrie de vie permise sur UN élément) ;
  - secondaire : le revers côtelé (16 côtes en relief de 2 cm), une bande blanc cassé de
                 6 cm à mi-hauteur (contraste de valeur) ;
  - tertiaire  : le gros pompon Ø 0,30 à 12 bosses ; une petite étiquette carrée vernie
                 cousue sur le revers, à gauche : le seul point brillant.
Matières : laine mate (0,15) partout, étiquette vernie (0,85).

Hiérarchie (le code Unity retrouve les pièces par leur nom) :
    chapeau_bonnet      vide, origine = PointChapeau (attachScale 1, attachOffset 0)
    └── Bonnet          dôme + revers + étiquette ; origine = centre de la base
        └── Anim_Pompon le pompon ; origine = sa base (ressort, bible §13.5 type A)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "chapeau_bonnet"
POINT = "PointChapeau"
MATS = ["M5_LaineTurquoise", "M5_BlancMat", "M5_TurquoiseVerni"]  # 0, 1, 2
BX, BY, BZ = C.CHAPEAU_BASE[0], C.CHAPEAU_BASE[1] + 0.05, C.CHAPEAU_BASE[2]
R0 = C.CHAPEAU_RAYON + 0.02
HAUT = 0.46
REVERS = 0.17           # hauteur du revers (V7 : gros revers blanc, comme l'icône)


def mou(co):
    """Le haut du bonnet retombe vers l'arrière gauche (plus on monte, plus il glisse)."""
    t = max(0.0, co.z) / HAUT
    return co + Vector((0.07, -0.06, -0.04)) * t * t


def dome(bm):
    profil = [(0.0, HAUT), (0.14, HAUT - 0.02), (0.25, HAUT - 0.08), (0.31, HAUT - 0.15),
              (0.335, 0.30), (0.34, 0.27), (0.345, 0.24), (R0 + 0.03, 0.16), (R0 + 0.02, 0.08),
              (R0 - 0.01, 0.08)]
    # V7 (lot 4) : plus de bande blanche au milieu (absente de l'icône) : dôme turquoise uni
    mats = [0] * 9

    def bande(a, i, r, z):
        return (r, z)
    v_avant = set(bm.verts)
    C.bm_revolution(bm, profil, 28, mats=mats, rayon_fn=bande)
    v = [x for x in bm.verts if x not in v_avant]
    for x in v:
        x.co = mou(x.co)
    return v


def revers(bm):
    """Revers côtelé : un gros boudin de laine, 16 côtes verticales de 2 cm."""
    profil = [(R0 - 0.02, REVERS - 0.01), (R0 + 0.035, REVERS), (R0 + 0.06, REVERS - 0.03),
              (R0 + 0.06, 0.02), (R0 + 0.035, -0.01), (R0 - 0.03, -0.01)]

    def cotes(a, i, r, z):
        if 1 <= i <= 4:
            return r + 0.012 * (0.5 + 0.5 * math.cos(16 * a)), z   # V7 : côtes plus douces
        return r, z
    v_avant = set(bm.verts)
    C.bm_revolution(bm, profil, 48, mats=[1] * 5, rayon_fn=cotes)
    return [x for x in bm.verts if x not in v_avant]


def construire():
    C.nouvelle_scene()
    base = C.U(BX, BY, BZ)
    bm = bmesh.new()
    v = dome(bm) + revers(bm)
    # V7 (lot 4) : l'étiquette vernie est retirée (absente de l'icône, plus épuré).
    bmesh.ops.translate(bm, vec=base, verts=list(set(v)))
    bonnet = C.piece("Bonnet", bm, MATS, lissage="tout", pivot=base)

    bm = bmesh.new()
    rp = 0.15
    sommet = mou(Vector((0, 0, HAUT)))
    v = C.bm_pompon(bm, sommet + Vector((0, 0, rp * 0.8)), rp, mat=1, bosses=0.09)
    bmesh.ops.translate(bm, vec=base, verts=v)
    pompon = C.piece("Anim_Pompon", bm, MATS, lissage="tout", pivot=base + sommet)
    C.parenter(pompon, bonnet)
    return C.racine_v5(NOM, POINT, [bonnet])


if __name__ == "__main__":
    C.finir_v5(construire(), NOM, POINT)
