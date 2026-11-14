"""
pirate_tricorne.py — Tricorne (boîte d'accessoires Pirates : `tricorne`, Super rare, Tête).
Fiche : docs/specs/v5-catalogue.md §2 n° 01 ; règles v5 : docs/art-bible.md §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/pirate_tricorne.py
Sortie : blender/exports/tricorne.fbx et docs/captures/v5_cheval_tricorne.png.

Les trois niveaux de forme (bible §13.2) :
  - primaire   : le bord relevé en TROIS murs (vu de dessus : un triangle aux coins
                 arrondis, une corne pointée vers l'avant) autour d'une calotte basse ;
  - secondaire : le galon doré qui suit tout le bord (un jonc rond de 5 cm : il attrape la
                 lumière sur les trois cornes) et la grande plume rouge (le détail-signature,
                 exagérée : 0,55 m), piquée côté gauche du cheval (= droite de l'image) ;
  - tertiaire  : une cocarde dorée à la base de la plume, trois boutons dorés au bout des
                 cornes.
Matières (3) : feutre bleu nuit satiné (smoothness 0,40 : un objet sombre doit garder un
bord lisible, bible §13.4), doré déco (metallic 1, 0,80), plume rouge mate (0,15).

Hiérarchie (le code Unity retrouve les pièces par leur nom) :
    tricorne            vide, origine = PointChapeau (attachScale 1, attachOffset 0)
    └── Chapeau         calotte + bord + galon + boutons + cocarde ; origine = centre de la base
        ├── Anim_Plume  la plume ; origine = sa base dans la cocarde (ressort A, et
        │               balancement doux ±5° en 2,4 s : bible §13.5 A et B)
        └── Eclat_1..3  vides : sur les boutons des cornes (1 éclat / 2,5 s, émission 0,5)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_pirates as P  # noqa: E402

NOM = "tricorne"
POINT = "PointChapeau"
MATS = ["M5_PirFeutreNuit", "M5_DoreDeco", "M5_PirPlumeRouge"]  # 0, 1, 2
BASE = (0.0, 1.84, 0.05)       # centre de la base du chapeau (Unity)
R_IN = 0.30                    # rayon de la calotte à la base (contient les oreilles)
N = 30                         # tranches autour du chapeau
INCLINAISON = math.radians(7)  # le chapeau penche un peu en arrière (la corne avant monte)
A_AVANT = math.pi / 2          # angle Blender de la corne avant (+Y = devant)


def coin(a):
    """1 sur une corne, 0 au milieu d'un mur (vu de dessus, 3 cornes)."""
    return (math.cos(3 * (a - A_AVANT)) + 1) / 2


def r_bord(a):
    return 0.43 + 0.13 * coin(a) ** 1.5


def h_bord(a):
    """Hauteur du bord extérieur : les murs montent haut, les cornes restent basses."""
    return 0.05 + 0.27 * (1 - coin(a)) ** 0.8


def point_bord(a, f, dz=0.0):
    """Point du bord à la fraction f (0 = contre la calotte, 1 = bord extérieur). Le bord
    se relève en courbe (f²) : un mur de feutre qui remonte, pas une rampe droite."""
    r = R_IN - 0.01 + (r_bord(a) - R_IN + 0.01) * f
    # le mur penche vers l'intérieur en haut (on « roule » le feutre)
    r -= 0.09 * (1 - coin(a)) * f ** 3
    z = h_bord(a) * f ** 2.2 + dz
    return Vector((r * math.cos(a), r * math.sin(a), z))


def bord(bm):
    fr = [0.0, 0.45, 0.8, 1.0]
    loops = []
    for f in fr:                       # dessus du bord (vers l'intérieur du chapeau)
        loops.append([point_bord(2 * math.pi * k / N, f, 0.018) for k in range(N)])
    for f in reversed(fr):             # dessous
        loops.append([point_bord(2 * math.pi * k / N, f, -0.018) for k in range(N)])
    loops.append(loops[0])
    rangs = C.bm_anneaux(bm, loops, mats=[0] * (len(loops) - 1))
    v = [x for r in rangs for x in r]
    bmesh.ops.remove_doubles(bm, verts=v, dist=1e-5)
    return [x for x in v if x.is_valid]


def calotte(bm):
    prof = [(R_IN - 0.01, -0.03), (R_IN, 0.02), (R_IN - 0.01, 0.12), (0.26, 0.20),
            (0.17, 0.26), (0.0, 0.28)]
    av = set(bm.verts)
    C.bm_revolution(bm, prof, 14, mats=[0] * 5)
    return [v for v in bm.verts if v not in av]


def galon(bm):
    chemin = [point_bord(2 * math.pi * k / N, 1.0) for k in range(N)]
    chemin.append(chemin[0])
    return C.bm_tube(bm, chemin, 0.028, cotes=4, mat=1, boucher=False)


def construire():
    C.nouvelle_scene()
    base = C.U(*BASE)
    incl = Matrix.Translation(base) @ Matrix.Rotation(INCLINAISON, 4, "X")
    bm = bmesh.new()
    v = calotte(bm) + bord(bm) + galon(bm)
    # boutons dorés au bout des 3 cornes (tertiaire, Ø 7 cm)
    eclats = []
    for k in range(3):
        a = A_AVANT + 2 * math.pi * k / 3
        p = point_bord(a, 1.0, 0.03)
        v += C.bm_sphere(bm, 0.04, p, segments=6, anneaux=3, mat=1)
        eclats.append(incl @ (p + Vector((0, 0, 0.03))))
    # cocarde : sur le mur côté droit de l'image (Blender −X), un peu devant
    a_pl = A_AVANT + math.radians(58)           # mur avant-droit (vu de la caméra)
    p_mur = point_bord(a_pl, 0.78, 0.03)
    n_mur = Vector((math.cos(a_pl), math.sin(a_pl), 0.35)).normalized()
    m_c = C.repere(p_mur + n_mur * 0.01, n_mur)
    v += C.bm_etoile(bm, 0.065, 0.03, m_c, mat=1, branches=8, creux=0.72, bombe=0.012)
    bmesh.ops.transform(bm, matrix=incl, verts=list(set(v)))
    chapeau = C.piece("Chapeau", bm, MATS, lissage="angle", pivot=base)

    # La plume : part de la cocarde, monte et se recourbe vers l'arrière de la calotte
    bm = bmesh.new()
    p0 = p_mur + n_mur * 0.02
    # V7 : plus large, plus courbée et plus de tranches (le QA : « une simple pointe rouge »),
    # avec une 2e plume plus courte derrière pour le volume « panache ».
    # V7 (lot 4) : panache plus large et plus duveteux (3 plumes), comme l'icône
    P.bm_plume(bm, p0, Vector((-0.25, -0.10, 1.0)), 0.60, 0.44,
               courbe=Vector((0.12, -0.42, -0.10)), mat=2, cotes=8, n=11,
               haut=Vector((math.cos(a_pl), math.sin(a_pl), 0)), plat=0.35)
    P.bm_plume(bm, p0 + Vector((0, -0.02, -0.01)), Vector((-0.45, -0.25, 0.8)), 0.42, 0.24,
               courbe=Vector((-0.05, -0.30, -0.12)), mat=2, cotes=8, n=9,
               haut=Vector((math.cos(a_pl + 0.5), math.sin(a_pl + 0.5), 0)), plat=0.35)
    P.bm_plume(bm, p0 + Vector((0.01, -0.01, 0.0)), Vector((0.05, -0.30, 0.9)), 0.48, 0.32,
               courbe=Vector((0.1, -0.25, -0.10)), mat=2, cotes=8, n=9,
               haut=Vector((math.cos(a_pl - 0.5), math.sin(a_pl - 0.5), 0)), plat=0.4)
    bmesh.ops.transform(bm, matrix=incl, verts=bm.verts)
    plume = C.piece("Anim_Plume", bm, MATS, lissage="tout", pivot=incl @ p0)
    C.parenter(plume, chapeau)
    for e in C.reperes_effets(eclats):
        C.parenter(e, chapeau)
    return C.racine_v5(NOM, POINT, [chapeau])


if __name__ == "__main__":
    P.finir_pirate(construire(), NOM, POINT)
