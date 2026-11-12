"""
animal_oreilles_chat.py — Oreilles de chat (boîte « Animaux : accessoires » : `oreilles_chat`,
Commun, Tête). docs/specs/v5-catalogue.md §4 n°01, bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/animal_oreilles_chat.py
Sortie : blender/exports/oreilles_chat.fbx et docs/captures/v5_cheval_oreilles_chat.png.

Trois niveaux de forme (bible §13.2) :
  - primaire   : deux GRANDES oreilles triangulaires arrondies (0,36 de haut), posées DEVANT
                 les oreilles du cheval (elles les cachent presque de face) ;
  - secondaire : l'intérieur rose bombé, le serre-tête noir satiné qui épouse le crâne ;
  - tertiaire  : deux bouts arrondis du serre-tête derrière les joues, le bombé de
                 l'intérieur rose qui ressort de 2 cm.
Matières : serre-tête satiné 0,55, oreilles mates 0,15 (contraste de matière), rose mat.

Hiérarchie :
    oreilles_chat          vide, origine = PointChapeau (attachScale 1, attachOffset 0)
    └── SerreTete          l'arceau
        ├── Anim_OreilleGauche   origine = sa base (ressort, ±10°)
        └── Anim_OreilleDroite
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_animaux as A  # noqa: E402

NOM = "oreilles_chat"
POINT = "PointChapeau"
MATS = ["M5_SerreTeteNoir", "M5_OreilleChat", "M5_AnimRoseMat"]  # 0..2
PLAN_Z = -0.03             # le serre-tête passe juste devant les oreilles du cheval (Unity z)
CENTRE = (0.0, 1.58, PLAN_Z)


def arceau(bm):
    """Arceau : demi-cercle vertical qui suit le crâne (rayons lissés), du bas de la joue
    gauche au bas de la joue droite, avec un bout arrondi de chaque côté."""
    chemin, hauts = [], []
    for k in range(17):
        t = k / 16
        e = math.radians(-5 + 190 * t)            # de la joue gauche (+X) à la droite
        ang = math.radians(90) if e <= math.pi / 2 else math.radians(-90)
        el = e if e <= math.pi / 2 else math.pi - e
        p, d = A.surface(CENTRE, ang, el, 0.012, pente=0.6, fenetre=(0.0, 0.21))
        chemin.append(p)
        hauts.append(d)
    sec = C.contour_rectangle_arrondi(0.065, 0.045, 0.018, par_coin=1)
    C.bm_balayage_surface(bm, chemin, hauts, sec, mat=0)
    for p in (chemin[0], chemin[-1]):
        C.bm_sphere(bm, 0.04, p, segments=8, anneaux=5, mat=0)
    return chemin, hauts


def oreille(cote, chemin, hauts):
    """Une oreille : triangle arrondi épais, penché vers l'extérieur, intérieur rose."""
    k = 5 if cote > 0 else 11
    base, d = chemin[k], hauts[k]
    bm = bmesh.new()
    contour = []
    # triangle aux coins arrondis (x = côté, z = haut), base 0,30, hauteur 0,36
    coins = [(-0.15, 0.0), (0.15, 0.0), (0.02, 0.36)]
    for i in range(3):
        a, b, c = Vector(coins[i - 1]), Vector(coins[i]), Vector(coins[(i + 1) % 3])
        r = 0.05 if i < 2 else 0.035
        u, w = (a - b).normalized(), (c - b).normalized()
        for s in range(4):
            t = s / 3
            p = b + (u * (1 - t) + w * t).normalized() * r * 0.9 + (u + w).normalized() * r * 0.15
            contour.append((p.x, p.y))
    cx = sum(p[0] for p in contour) / len(contour)
    cz = sum(p[1] for p in contour) / len(contour)
    contour = [(x - cx, z - cz) for x, z in contour]
    C.bm_coussin(bm, contour, 0.07, 0.10, Matrix.Translation((0, 0, cz)), mat=1, anneaux=2)
    interieur = [(x * 0.72, z * 0.72) for x, z in contour]
    C.bm_coussin(bm, interieur, 0.02, 0.045, Matrix.Translation((0, 0.045, cz - 0.03)),
                 mat=2, anneaux=1, dos_plat=True)
    # pose : base sur l'arceau, penchée vers l'extérieur de 14°, un peu vers l'arrière
    incl = Matrix.Rotation(math.radians(-14 * cote), 4, "Y") @ Matrix.Rotation(
        math.radians(-8), 4, "X")
    bmesh.ops.transform(bm, matrix=Matrix.Translation(base - d * 0.02) @ incl, verts=bm.verts)
    nom = "Anim_OreilleGauche" if cote > 0 else "Anim_OreilleDroite"
    return C.piece(nom, bm, MATS, lissage="tout", pivot=base)


def construire():
    C.nouvelle_scene()
    bm = bmesh.new()
    chemin, hauts = arceau(bm)
    st = C.piece("SerreTete", bm, MATS, lissage="tout", pivot=C.U(*C.CHEVAL_ACCROCHES[POINT]))
    for cote in (+1, -1):
        C.parenter(oreille(cote, chemin, hauts), st)
    return C.racine_v5(NOM, POINT, [st])


if __name__ == "__main__":
    A.finir_animal(construire(), NOM, POINT)
