"""
animal_oreilles_lapin.py — Oreilles de lapin (boîte « Animaux : accessoires » :
`oreilles_lapin`, Commun, Tête). docs/specs/v5-catalogue.md §4 n°02, bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/animal_oreilles_lapin.py
Sortie : blender/exports/oreilles_lapin.fbx et docs/captures/v5_cheval_oreilles_lapin.png.

Trois niveaux de forme :
  - primaire   : deux LONGUES oreilles (0,50) en ovale allongé, bombées ; celle de droite
                 (écran) est PLIÉE à mi-hauteur et retombe sur le côté (l'asymétrie de vie) ;
  - secondaire : l'intérieur rose, le serre-tête blanc verni qui suit le crâne ;
  - tertiaire  : un petit nœud rose sur le serre-tête, à gauche ; bouts arrondis.
Matières : fourrure blanc cassé mate 0,15, serre-tête verni 0,70, rose mat.

Hiérarchie :
    oreilles_lapin          vide, origine = PointChapeau (attachScale 1, attachOffset 0)
    └── SerreTete
        ├── Anim_OreilleGauche     oreille droite debout ; origine = sa base (ressort)
        └── OreilleDroite          bas de l'oreille pliée ; origine = sa base (ressort)
            └── Anim_OreillePliee  le bout plié ; origine = le PLI (il rebondit au saut)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_animaux as A  # noqa: E402

NOM = "oreilles_lapin"
POINT = "PointChapeau"
MATS = ["M5_SerreTeteBlanc", "M5_LapinBlanc", "M5_AnimRoseMat"]  # 0..2
CENTRE = (0.0, 1.58, -0.03)
LARGEUR = 0.17


def arceau(bm):
    chemin, hauts = [], []
    for k in range(17):
        e = math.radians(-5 + 190 * k / 16)
        ang = math.radians(90) if e <= math.pi / 2 else math.radians(-90)
        el = e if e <= math.pi / 2 else math.pi - e
        p, d = A.surface(CENTRE, ang, el, 0.012, pente=0.6, fenetre=(0.0, 0.21))
        chemin.append(p)
        hauts.append(d)
    sec = C.contour_rectangle_arrondi(0.07, 0.045, 0.018, par_coin=1)
    C.bm_balayage_surface(bm, chemin, hauts, sec, mat=0)
    for p in (chemin[0], chemin[-1]):
        C.bm_sphere(bm, 0.04, p, segments=8, anneaux=5, mat=0)
    # petit nœud rose à gauche de l'image (deux lobes + un centre)
    p, d = chemin[4], hauts[4]
    m = C.repere(p + d * 0.03, d, Vector((0, 1, 0)))
    for s in (-1, 1):
        C.bm_coussin(bm, C.contour_ellipse(0.045, 0.03, n=8), 0.03, 0.05,
                     m @ Matrix.Translation((s * 0.045, 0, 0)), mat=2, anneaux=1)
    C.bm_sphere(bm, 0.028, m @ Vector((0, 0.01, 0)), segments=6, anneaux=4, mat=2)
    return chemin, hauts


def bm_morceau(bm, hauteur, z0=0.0, bout_rond=True, bas_rond=True):
    """Un morceau d'oreille : ovale allongé (x = côté, z = haut) de `hauteur`, bombé,
    intérieur rose sur le devant (+Y)."""
    n = 12
    cont = []
    for k in range(n):
        t = 2 * math.pi * k / n
        x = LARGEUR / 2 * math.sin(t)
        z = -math.cos(t) * hauteur / 2
        if (z < 0 and not bas_rond) or (z > 0 and not bout_rond):
            z = math.copysign(hauteur / 2 * min(1.0, abs(math.cos(t)) * 2.5), z)
        cont.append((x * (1.0 - 0.25 * max(0.0, -z / hauteur)), z))
    cont.reverse()
    C.bm_coussin(bm, cont, 0.06, 0.09, Matrix.Translation((0, 0, z0 + hauteur / 2)), mat=1,
                 anneaux=2)
    inte = [(x * 0.62, z * 0.86) for x, z in cont]
    C.bm_coussin(bm, inte, 0.02, 0.035, Matrix.Translation((0, 0.04, z0 + hauteur / 2)),
                 mat=2, anneaux=1, dos_plat=True)


def pose(cote, chemin, hauts):
    k = 5 if cote > 0 else 11
    base, d = chemin[k], hauts[k]
    incl = Matrix.Rotation(math.radians(-10 * cote), 4, "Y") @ Matrix.Rotation(
        math.radians(-6), 4, "X")
    return Matrix.Translation(base - d * 0.02) @ incl, base


def construire():
    C.nouvelle_scene()
    bm = bmesh.new()
    chemin, hauts = arceau(bm)
    st = C.piece("SerreTete", bm, MATS, lissage="tout", pivot=C.U(*C.CHEVAL_ACCROCHES[POINT]))

    # oreille gauche de l'image : debout, 0,50
    m, base = pose(+1, chemin, hauts)
    bm = bmesh.new()
    bm_morceau(bm, 0.50)
    bmesh.ops.transform(bm, matrix=m, verts=bm.verts)
    C.parenter(C.piece("Anim_OreilleGauche", bm, MATS, lissage="tout", pivot=base), st)

    # oreille droite : bas 0,26 debout, puis le bout (0,26) plié vers l'extérieur et l'avant
    m, base = pose(-1, chemin, hauts)
    bm = bmesh.new()
    bm_morceau(bm, 0.28, bout_rond=False)
    bmesh.ops.transform(bm, matrix=m, verts=bm.verts)
    bas = C.piece("OreilleDroite", bm, MATS, lissage="tout", pivot=base)
    C.parenter(bas, st)
    pli = m @ Vector((0, 0, 0.26))
    bm = bmesh.new()
    bm_morceau(bm, 0.26, bas_rond=False)
    # plié : tourne autour de l'axe avant-arrière, vers la droite de l'image (−X Blender),
    # et un peu vers l'avant ; le dos de l'oreille se voit (comme une vraie oreille pliée)
    pl = m @ Matrix.Translation((0, 0, 0.26)) @ Matrix.Rotation(math.radians(-115), 4, "Y") \
        @ Matrix.Rotation(math.radians(20), 4, "X") @ Matrix.Translation((0, 0, -0.02))
    bmesh.ops.transform(bm, matrix=pl, verts=bm.verts)
    C.parenter(C.piece("Anim_OreillePliee", bm, MATS, lissage="tout", pivot=pli), bas)
    return C.racine_v5(NOM, POINT, [st])


if __name__ == "__main__":
    A.finir_animal(construire(), NOM, POINT)
