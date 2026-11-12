"""
animal_lunettes_hibou.py — Lunettes hibou (boîte « Animaux : accessoires » :
`lunettes_hibou`, Peu commun, Visage). docs/specs/v5-catalogue.md §4 n°07, bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/animal_lunettes_hibou.py
Sortie : blender/exports/lunettes_hibou.fbx et docs/captures/v5_cheval_lunettes_hibou.png.

Trois niveaux de forme :
  - primaire   : deux GRANDS verres ronds (Ø 0,28) dans une monture épaisse et ronde ;
  - secondaire : des sourcils en plumes orange (3 plumes en éventail) au-dessus de chaque
                 verre, vers l'extérieur ; le pont en arc ; les branches qui longent la tête ;
  - tertiaire  : les yeux du cheval « dessinés » sur les verres, AGRANDIS (grosse pupille
                 dans le brun de la
                 monture + reflet) : l'air savant du hibou ; rivets dorés aux charnières.
Les verres sont teintés ambre et opaques (fiche du catalogue) : on voit les yeux dessinés,
pas ceux du cheval, qui sont juste derrière (même place, même regard).
Matières : monture satinée 0,55, verres vernis 0,95 ambre, plumes mates 0,15, doré déco.

Hiérarchie :
    lunettes_hibou       vide, origine = PointVisage (attachScale 1, attachOffset 0)
    └── Anim_Lunettes    monture + verres + branches ; origine = le pont (petit ressort)
        ├── Anim_SourcilGauche   origine = le bout intérieur du sourcil (ils se haussent
        └── Anim_SourcilDroit    de 12° au survol d'un coffre)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_animaux as A  # noqa: E402

NOM = "lunettes_hibou"
POINT = "PointVisage"
MATS = ["M5_MontureHibou", "M5_VerreAmbre", "M5_MontureHibou", "M5_Reflet", "M5_PlumeHibou",
        "M5_DoreDeco"]  # 0..5 ; la pupille (2) est dans la matière de la monture
# (3 couleurs + reflet + doré, bible §13.7 Peu commun)
R_VERRE = 0.14
ECART_X = 0.185
AVANCE = 0.055
OUVERTURE = 14.0


def repere_verre(s):
    """s = +1 : verre de gauche de l'image (+X Blender = Unity x négatif)."""
    a = math.radians(OUVERTURE)
    centre = C.U(-s * ECART_X, C.CHEVAL_YEUX_Y + 0.01, C.CHEVAL_YEUX_AVANT - AVANCE)
    return C.repere(centre, Vector((s * math.sin(a), math.cos(a), 0.0)))


def verre(bm, m, s):
    C.bm_coussin(bm, C.contour_ellipse(R_VERRE, R_VERRE, n=14), 0.03, 0.045, m, mat=1, anneaux=1)
    C.bm_tore(bm, R_VERRE + 0.012, 0.032, segments=16, segments_tube=5, mat=0,
              matrice=m @ Matrix.Rotation(math.radians(90), 4, "X"))
    # œil agrandi dessiné sur le verre, qui regarde un peu vers le centre
    pupille = C.contour_ellipse(0.075, 0.09, n=12)
    C.bm_coussin(bm, pupille, 0.012, 0.025, m @ Matrix.Translation((-s * 0.01, 0.028, -0.005)),
                 mat=2, anneaux=1, dos_plat=True)
    C.bm_sphere(bm, 0.022, m @ Vector((-s * 0.01 + 0.028, 0.045, 0.035)), segments=6, anneaux=4,
                mat=3)
    C.bm_sphere(bm, 0.011, m @ Vector((-s * 0.01 - 0.02, 0.042, -0.035)), segments=5, anneaux=3,
                mat=3)


def sourcil(m, s):
    """3 plumes orange en éventail au-dessus du verre, du centre vers l'extérieur."""
    bm = bmesh.new()
    pivot = m @ Vector((-s * 0.06, 0.02, R_VERRE + 0.035))
    for k, (dx, ang, L) in enumerate(((0.0, 62, 0.16), (0.06, 48, 0.19), (0.12, 34, 0.17))):
        base = m @ Vector((-s * 0.06 + s * dx, 0.025, R_VERRE + 0.03))
        d = (m.to_3x3() @ Vector((s * math.cos(math.radians(ang)) * 1.0, 0.1,
                                  math.sin(math.radians(ang)) * 0.75))).normalized()
        A.bm_goutte(bm, L, 0.045, segments=5, bandes=((1.0, 4),), aplati=0.45,
                    matrice=A.matrice_vers(base, d, m.to_3x3() @ Vector((0, 1, 0))))
    nom = "Anim_SourcilGauche" if s > 0 else "Anim_SourcilDroit"
    return C.piece(nom, bm, MATS, lissage="tout", pivot=pivot)


def branche(bm, m, s):
    depart = m @ Vector((s * (R_VERRE + 0.03), -0.01, 0.03))
    chemin = [depart]
    centre = (0.0, C.CHEVAL_YEUX_Y + 0.05, 0.03)
    for ang, ecart in ((60, 0.02), (74, 0.016), (88, 0.015), (102, 0.015), (116, -0.02)):
        p, _ = A.surface(centre, math.radians(s * ang), 0.0, ecart, fenetre=(0.14, 0.14))
        chemin.append(p)
    C.bm_tube(bm, chemin, 0.022, cotes=5, mat=0)
    C.bm_sphere(bm, 0.035, depart, segments=6, anneaux=4, mat=5)


def construire():
    C.nouvelle_scene()
    bm = bmesh.new()
    reps = {}
    for s in (+1, -1):
        m = repere_verre(s)
        reps[s] = m
        verre(bm, m, s)
        branche(bm, m, s)
    pa = reps[1] @ Vector((-(R_VERRE + 0.02), 0, 0.05))
    pb = reps[-1] @ Vector(((R_VERRE + 0.02), 0, 0.05))
    milieu = (pa + pb) / 2 + Vector((0, 0.02, 0.035))
    C.bm_tube(bm, [pa, pa.lerp(milieu, 0.6), milieu, pb.lerp(milieu, 0.6), pb], 0.026, cotes=6)
    lun = C.piece("Anim_Lunettes", bm, MATS, lissage="angle", pivot=milieu)
    for s in (+1, -1):
        C.parenter(sourcil(reps[s], s), lun)
    return C.racine_v5(NOM, POINT, [lun])


if __name__ == "__main__":
    A.finir_animal(construire(), NOM, POINT)
