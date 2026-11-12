"""
animal_echarpe_renard.py — Écharpe renard (boîte « Animaux : accessoires » : `echarpe_renard`,
Peu commun, Cou). docs/specs/v5-catalogue.md §4 n°08, bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/animal_echarpe_renard.py
Sortie : blender/exports/echarpe_renard.fbx et docs/captures/v5_cheval_echarpe_renard.png.

Un petit renard en fausse fourrure ENROULÉ autour du cou (le boudin épouse le cou, comme
l'écharpe v5), qui dort la tête posée sur le poitrail ; sa queue pend devant à gauche.

Trois niveaux de forme :
  - primaire   : le boudin de fourrure (21 cm de haut, 12 cm d'épaisseur) avec des
                 « mèches » en relief (bosses de 3 cm tous les 30°) ;
  - secondaire : la tête endormie (museau qui pointe vers la gauche de l'image, joues
                 blanches, 2 oreilles), la GROSSE queue touffue (0,46) au bout blanc ;
  - tertiaire  : yeux fermés en arc, truffe vernie, bouts d'oreilles foncés, intérieur
                 d'oreilles clair.
Matières : fourrure orange mate 0,15, fourrure blanc cassé mate, noir verni 0,90 (truffe,
yeux, bouts d'oreilles) : contraste de matière mat / verni.

Hiérarchie :
    echarpe_renard     vide, origine = PointCou (attachScale 1, attachOffset 0)
    └── Fourrure       le boudin + la tête du renard
        └── Anim_Queue la queue ; origine = sa racine (elle se balance ±8°, 2,4 s, ressort)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_animaux as A  # noqa: E402

NOM = "echarpe_renard"
POINT = "PointCou"
MATS = ["M5_FourrureRenard", "M5_FourrureBlanche", "M5_AnimNoirVerni"]  # 0..2
Y_MILIEU, PENTE = 1.02, 0.05
SECTION = [(-0.012, -0.09), (0.035, -0.105), (0.09, -0.085), (0.12, -0.03), (0.12, 0.03),
           (0.09, 0.085), (0.035, 0.105), (-0.012, 0.09)]


def tete(bm, bord):
    """Tête endormie, posée sur le devant du boudin, un peu à droite de l'image."""
    p, d = bord(-0.30)
    c = p + d * 0.10 + Vector((0, 0, 0.0))
    # repère : avant = d, le museau pointe vers la gauche de l'image (+X Blender)
    rot = C.repere((0, 0, 0), d).to_3x3().to_4x4()
    # x local = côté, y = avant, z = haut ; tête × 1,5 (le détail-signature, bible §13.2)
    T = Matrix.Translation(c) @ rot @ Matrix.Diagonal((1.5, 1.5, 1.5, 1.0))
    v = A.bm_ellipsoide(bm, (0, 0, 0), (0.15, 0.115, 0.12), segments=10, anneaux=7, mat=0)
    bmesh.ops.transform(bm, matrix=T, verts=v)
    # joues blanches (deux boules) et museau allongé vers +X local, blanc dessous
    for x, z, r in ((0.07, -0.04, 0.075), (-0.06, -0.05, 0.07)):
        v = A.bm_ellipsoide(bm, (x, 0.05, z), (r, r * 0.8, r * 0.75), segments=8, anneaux=5, mat=1)
        bmesh.ops.transform(bm, matrix=T, verts=v)
    v = A.bm_ellipsoide(bm, (0.15, 0.03, -0.025), (0.11, 0.07, 0.065), segments=8, anneaux=6, mat=0,
                        rot=Matrix.Rotation(math.radians(-10), 4, "Y"))
    bmesh.ops.transform(bm, matrix=T, verts=v)
    v = C.bm_sphere(bm, 0.03, (0.25, 0.035, -0.01), segments=8, anneaux=5, mat=2)
    bmesh.ops.transform(bm, matrix=T, verts=v)
    # yeux fermés : deux petits arcs noirs vernis (vers la caméra)
    for x in (0.045, -0.055):
        arc = [Vector((x + 0.035 * math.cos(t), 0.112, 0.035 - 0.02 * math.sin(t)))
               for t in (math.pi * 0.1, math.pi * 0.5, math.pi * 0.9)]
        arc = [T @ q for q in arc]
        C.bm_tube(bm, arc, 0.011, cotes=5, mat=2)
    # oreilles : petits cônes aplatis, bout noir, couchés vers l'arrière-haut
    for x in (0.06, -0.08):
        av = set(bm.verts)
        A.bm_goutte(bm, 0.16, 0.07, segments=6, bandes=((0.72, 0), (1.0, 2)), aplati=0.55)
        nv = [q for q in bm.verts if q not in av]
        bmesh.ops.transform(bm, matrix=T @ Matrix.Translation((x, -0.02, 0.08))
                            @ Matrix.Rotation(math.radians(-35), 4, "X")
                            @ Matrix.Rotation(math.radians(20 if x > 0 else -20), 4, "Y"),
                            verts=nv)
    return c


def queue(bord):
    """La queue : un tube touffu qui part du boudin devant à gauche et pend sur le poitrail."""
    p, d = bord(0.75)
    racine = p - d * 0.02
    chemin = []
    for k in range(7):
        t = k / 6
        q = racine + d * (0.06 * math.sin(t * math.pi) + 0.04 * t) \
            + Vector((0.05 * t, 0.0, -0.46 * t)) + Vector((0, 0, 0.03 * math.sin(t * 6)))
        chemin.append(q)
    bm = bmesh.new()
    rayons = [0.6, 0.9, 1.0, 1.0, 0.9, 0.7, 0.3]
    C.bm_tube(bm, chemin, 0.085, cotes=8, mat=0, rayons=rayons)
    zb = chemin[0].z
    for f in bm.faces:
        if f.calc_center_median().z < zb - 0.34:
            f.material_index = 1
    return C.piece("Anim_Queue", bm, MATS, lissage="tout", pivot=racine)


def construire():
    C.nouvelle_scene()
    bm = bmesh.new()
    bord = A.anneau_cou(bm, Y_MILIEU, PENTE, SECTION, segments=24,
                        bosse_fn=lambda a, j: 0.03 * max(0.0, math.cos(12 * a)))
    tete(bm, bord)
    fourrure = C.piece("Fourrure", bm, MATS, lissage="tout",
                       pivot=C.U(*C.CHEVAL_ACCROCHES[POINT]))
    C.parenter(queue(bord), fourrure)
    return C.racine_v5(NOM, POINT, [fourrure])


if __name__ == "__main__":
    A.finir_animal(construire(), NOM, POINT, y_min=0.55)
