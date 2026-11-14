"""
animal_ailes_chauve_souris.py — Ailes de chauve-souris (boîte « Animaux : accessoires » :
`ailes_chauve_souris`, Rare, Dos). docs/specs/v5-catalogue.md §4 n°10, bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/animal_ailes_chauve_souris.py
Sortie : blender/exports/ailes_chauve_souris.fbx et docs/captures/v5_cheval_ailes_chauve_souris.png.

Même pose que les ailes d'ange v5 (charnière sur l'arrière du cou, l'aile part vers
l'extérieur ET vers l'arrière) : elles dépassent largement des deux côtés vus de face.

Trois niveaux de forme :
  - primaire   : deux grandes ailes (0,78 × 0,58) au bord de fuite en FESTONS POINTUS
                 (3 arcs creux entre les doigts) ;
  - secondaire : les os en relief, violet sombre verni : le bras jusqu'au poignet, puis 3
                 doigts en éventail ; la membrane plus claire, mate ;
  - tertiaire  : une griffe dorée au poignet (le pouce), une petite boule au bout de chaque
                 doigt, une articulation ronde au poignet.
Matières : os vernis 0,75, membrane mate 0,35, doré déco (griffes).

Hiérarchie :
    ailes_chauve_souris     vide, origine = PointDos (attachScale 1, attachOffset 0)
    ├── Anim_AileGauche     origine = la charnière (battement « saccadé » ±18°, 0,7 s, avec
    │   └── Eclat_1, Eclat_3     une petite pause ; l'aile droite en miroir)
    └── Anim_AileDroite
        └── Eclat_2         (vides sur les griffes : Rare, 1 éclat toutes les 4 s)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_animaux as A  # noqa: E402

NOM = "ailes_chauve_souris"
POINT = "PointDos"
# V7 (lot 10) : membrane LILAS et os prune (nouvelle icône), plus de violet presque noir
MATS = ["M7_OsPrune", "M7_MembraneLilas", "M7_OrVif", "M7_FermoirViolet"]  # 0..3
X_CH, Y_CH = 0.13, 1.05
RECUL = math.radians(14)      # V7 : bien ouvertes, vues de face
LEVE = math.radians(10)
# V7 : ailes 15 % plus grandes
POIGNET = Vector((0.30, 0.35))
DOIGTS = [Vector((0.92, 0.34)), Vector((0.80, -0.02)), Vector((0.53, -0.28))]
BAS = Vector((0.07, -0.12))


def contour_membrane():
    """Contour (u, v) : racine -> poignet -> doigt 1 -> creux -> doigt 2 -> creux -> doigt 3
    -> creux -> bas de la racine. Sens inverse des aiguilles d'une montre."""
    pts = [Vector((0.0, 0.06)), POIGNET * 0.5 + Vector((0, 0.03)), POIGNET + Vector((0, 0.02))]
    pts.append(POIGNET.lerp(DOIGTS[0], 0.5) + Vector((0, 0.035)))
    suite = DOIGTS + [BAS]
    for a, b in zip(suite, suite[1:]):
        pts.append(a)
        milieu = a.lerp(b, 0.5)
        vers_racine = (POIGNET * 0.6 - milieu).normalized()
        for t, prof in ((0.3, 0.07), (0.5, 0.10), (0.7, 0.07)):
            pts.append(a.lerp(b, t) + vers_racine * prof)
    pts.append(BAS)
    pts = pts[::-1]          # du coup : sens horaire -> on inverse pour l'anti-horaire
    cu = sum(p.x for p in pts) / len(pts)
    cv = sum(p.y for p in pts) / len(pts)
    return [(p.x - cu, p.y - cv) for p in pts], (cu, cv)


def repere_aile():
    u = Vector((math.cos(RECUL), -math.sin(RECUL), 0.0))
    rot = Matrix.Rotation(LEVE, 4, u.cross(Vector((0, 0, 1))).normalized())
    u = (rot.to_3x3() @ u).normalized()
    z = Vector((0, 0, 1))
    y = z.cross(u).normalized()
    z = u.cross(y)
    return Matrix((u, y, z)).transposed().to_4x4()


def charniere(cote):
    depart = C.U(-cote * X_CH, Y_CH, 3.0)
    hit, _, _, _ = A.arbre().ray_cast(depart, Vector((0, 1, 0)))
    return hit + Vector((0, 0.02, 0))


def aile(cote):
    h = charniere(cote)
    m = Matrix.Translation(h) @ Matrix.Diagonal((cote, 1, 1, 1)) @ repere_aile()
    bm = bmesh.new()
    contour, (cu, cv) = contour_membrane()
    C.bm_coussin(bm, contour, 0.03, 0.055, Matrix.Translation((cu, 0, cv)), mat=1, anneaux=2)

    def P(uv, y=0.03):
        return Vector((uv.x, y, uv.y))
    C.bm_tube(bm, [P(Vector((0.0, 0.05))), P(POIGNET * 0.5 + Vector((0, 0.03))), P(POIGNET)],
              0.042, cotes=8, mat=0, rayons=[1.2, 1.0, 1.0])
    C.bm_sphere(bm, 0.045, P(POIGNET), segments=8, anneaux=5, mat=0)
    for d in DOIGTS:
        mid = POIGNET.lerp(d, 0.5)
        C.bm_tube(bm, [P(POIGNET), P(mid, 0.035), P(d)], 0.032, cotes=8, mat=0,
                  rayons=[1.0, 0.85, 0.6])
        C.bm_sphere(bm, 0.033, P(d), segments=8, anneaux=5, mat=0)
    # V7 : plus de griffe dorée (absente de l'icône)
    bmesh.ops.transform(bm, matrix=m, verts=bm.verts)
    if cote < 0:
        bmesh.ops.reverse_faces(bm, faces=bm.faces[:])
    nom = "Anim_AileGauche" if cote > 0 else "Anim_AileDroite"
    obj = C.piece(nom, bm, MATS, lissage="tout", pivot=h)
    griffe = m @ Vector((POIGNET.x - 0.02, 0.06, POIGNET.y + 0.11))
    bout = m @ P(DOIGTS[0])
    return obj, [griffe, bout]


def construire():
    C.nouvelle_scene()
    gauche, eg = aile(+1)
    droite, ed = aile(-1)
    e1, e2, e3 = C.reperes_effets([eg[0], ed[0], eg[1]])
    C.parenter(e1, gauche)
    C.parenter(e3, gauche)
    C.parenter(e2, droite)
    # V7 (lot 10) : petit fermoir violet rond entre les deux ailes, étoile dorée (icône)
    bm = bmesh.new()
    p = (charniere(1) + charniere(-1)) / 2 + Vector((0, -0.04, 0.02))
    m = C.repere(p, Vector((0, -1, 0.35)).normalized())
    C.bm_coussin(bm, C.contour_ellipse(0.09, 0.08, n=16), 0.04, 0.08, m, mat=3, anneaux=2)
    C.bm_etoile(bm, 0.045, 0.02, m @ Matrix.Translation((0, 0.045, 0)), mat=2, bombe=0.01)
    fermoir = C.piece("Fermoir", bm, MATS, lissage="tout", pivot=p)
    return C.racine_v5(NOM, POINT, [gauche, droite, fermoir])


if __name__ == "__main__":
    A.finir_animal(construire(), NOM, POINT, y_min=0.45)
