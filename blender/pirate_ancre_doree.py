"""
pirate_ancre_doree.py — Ancre dorée (boîte d'accessoires Pirates : `ancre_doree`,
LÉGENDAIRE, Dos). Fiche : docs/specs/v5-catalogue.md §2 n° 10 ; règles : art-bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/pirate_ancre_doree.py
Sortie : blender/exports/ancre_doree.fbx et docs/captures/v5_cheval_ancre_doree.png.

Comment elle est portée : une grosse CHAÎNE dorée fait le tour de l'encolure en
bandoulière (basse devant, à y = 0,50, sous le médaillon de la boussole et la pointe du
foulard ; haute derrière à droite de l'image), posée sur la surface mesurée par rayons.
Derrière, à droite, 2 maillons pendent de la chaîne et tiennent l'anneau de l'ANCRE
(0,9 m), portée dans le dos en diagonale comme une épée : les pattes DÉPASSENT EN BAS À
GAUCHE de la silhouette (vue de face), l'anneau est en haut à droite.

Les trois niveaux de forme (bible §13.2) :
  - primaire   : l'ancre (verge, jas, bras en arc, pattes en pointe de flèche arrondie) ;
  - secondaire : la chaîne à gros maillons (17 cm) qui alternent de plan ;
  - tertiaire  : les boules au bout du jas, la boule de la croisée, 2 tours de corde
                 rouge enroulés sous le jas.
Matières (3) : or de l'ancre (M5_PirOrAncre, metallic 1, 0,80, émission 0,5 : le code
fait glisser un reflet), doré déco de la chaîne, corde rouge mate (0,15).

Hiérarchie (le code Unity retrouve les pièces par leur nom) :
    ancre_doree         vide, origine = PointDos (attachScale 1, attachOffset 0)
    └── Chaine          la chaîne autour du cou ; origine = PointDos
        └── Anim_Maillon_1      1er maillon qui pend ; origine = son point d'accroche
            └── Anim_Maillon_2  il suit le précédent avec du retard (ressort A,
                │               « chaîne qui se balance »)
                └── Anim_Ancre  l'ancre ; origine = son anneau (suit avec plus de retard)
                    ├── Eclat_1..3  vides : sur l'or (1 éclat / 1,5 s, émission 1,2)
                    └── Fx_Emetteur vide : à la croisée (≤ 10 étincelles dorées)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_pirates as P  # noqa: E402

NOM = "ancre_doree"
POINT = "PointDos"
MATS = ["M5_PirOrAncre", "M5_DoreDeco", "M5_PirCordeRouge"]   # 0, 1, 2
N = 30
A_ACCROCHE = -2.85          # angle de la chaîne où pend l'ancre (derrière, à droite)
LONGUEUR = 0.90


def hauteur(a):
    """Bandoulière : basse devant (0,50), haute derrière (1,10), droite (a < 0) plus haute."""
    return 0.80 - 0.30 * math.cos(a) - 0.10 * math.sin(a)


def ancre(bm):
    """L'ancre dans son repère local : anneau en haut à l'origine, verge vers −Z,
    plan de l'ancre = XZ (devant = +Y). Hauteur totale 0,90."""
    av = set(bm.verts)
    e = 0.06                                    # épaisseur
    C.bm_tore(bm, 0.07, 0.024, segments=10, segments_tube=4, mat=0,
              matrice=Matrix.Rotation(math.radians(90), 4, "X"))           # anneau
    chemin = [Vector((0, 0, -0.07)), Vector((0, 0, -0.45)), Vector((0, 0, -0.78))]
    C.bm_tube(bm, chemin, 0.04, cotes=6, mat=0)                            # verge
    C.bm_tube(bm, [Vector((-0.22, 0, -0.17)), Vector((0.22, 0, -0.17))], 0.03, cotes=6,
              mat=0)                                                       # jas
    for s in (-1, 1):
        C.bm_sphere(bm, 0.045, (s * 0.24, 0, -0.17), segments=6, anneaux=4, mat=0)
    arc = [Vector((0.30 * math.sin(a), 0, -0.55 - 0.25 * math.cos(a)))
           for a in [math.radians(d) for d in range(-80, 81, 20)]]
    C.bm_tube(bm, arc, 0.04, cotes=5, mat=0)                              # bras
    for s in (-1, 1):                                                      # pattes
        a = math.radians(80 * s)
        c = Vector((0.30 * math.sin(a), 0, -0.55 - 0.25 * math.cos(a)))
        m = Matrix.Translation(c) @ Matrix.Rotation(math.radians(-35 * s), 4, "Y")
        C.bm_coussin(bm, [(0, 0.13), (-0.09, -0.02), (-0.05, -0.05), (0.05, -0.05),
                          (0.09, -0.02)], 0.04, 0.07, m, mat=0, anneaux=1)
    C.bm_sphere(bm, 0.06, (0, 0, -0.80), segments=8, anneaux=5, mat=0)    # croisée
    for k in range(2):                                                     # corde
        C.bm_tore(bm, 0.052, 0.018, segments=8, segments_tube=4, mat=2,
                  matrice=Matrix.Translation((0, 0, -0.25 - 0.04 * k))
                  @ Matrix.Rotation(math.radians(8), 4, "X"))
    return [v for v in bm.verts if v not in av]


def construire():
    C.nouvelle_scene()
    # --- la chaîne autour du cou
    tour = P.tour_cou(hauteur, 0.01, n=N)
    bm = bmesh.new()
    nb = P.bm_chaine(bm, [p for p, _ in tour], longueur=0.17, largeur=0.10, fil=0.022,
                     mat=1, ferme=True, hauts=[d for _, d in tour])
    print("Maillons de la chaîne :", nb)
    chaine = C.piece("Chaine", bm, MATS, lissage="angle", pivot=C.U(*C.CHEVAL_ACCROCHES[POINT]))

    # --- point d'accroche : sur la chaîne, derrière à droite
    p_acc = None
    for p, d in P.tour_cou(hauteur, 0.04, n=1, depart=A_ACCROCHE):
        p_acc, d_acc = p, d
    # 3 maillons qui pendent vers l'arrière et le bas
    descente = (d_acc * 1.0 + Vector((0, 0, -0.35))).normalized()
    parent = chaine
    pos = p_acc
    plans = [Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((1, 0, 0))]
    for k in range(2):
        bm = bmesh.new()
        centre = pos + descente * 0.055
        P.bm_maillon(bm, centre, descente, plans[k], longueur=0.13, largeur=0.08, fil=0.02,
                     mat=1)
        o = C.piece(f"Anim_Maillon_{k + 1}", bm, MATS, lissage="angle", pivot=pos)
        C.parenter(o, parent)
        parent = o
        pos = pos + descente * 0.10
    # --- l'ancre : anneau accroché au dernier maillon, verge vers le bas à GAUCHE de
    # l'image (+X Blender), plan de l'ancre face à la caméra (un peu tourné)
    axe = Vector((0.86, 0.0, -0.51)).normalized()          # anneau -> croisée
    bm = bmesh.new()
    v = ancre(bm)
    z = -axe                                                # Z local de l'ancre = vers l'anneau
    x = Vector((0, 1, 0)).cross(z).normalized()
    y = z.cross(x)
    rot = Matrix((x, y, z)).transposed().to_4x4()
    anneau = pos + descente * 0.06
    bmesh.ops.transform(bm, matrix=Matrix.Translation(anneau) @ rot
                        @ Matrix.Translation((0, 0, -0.06)), verts=v)
    ancre_o = C.piece("Anim_Ancre", bm, MATS, lissage="angle", pivot=anneau)
    C.parenter(ancre_o, parent)
    m = Matrix.Translation(anneau) @ rot @ Matrix.Translation((0, 0, -0.06))
    eclats = [m @ Vector((0.24, 0.05, -0.17)), m @ Vector((-0.30, 0.05, -0.62)),
              m @ Vector((0, 0.05, -0.45))]
    for e in C.reperes_effets(eclats, fx=m @ Vector((0, 0.06, -0.80))):
        C.parenter(e, ancre_o)
    return C.racine_v5(NOM, POINT, [chaine])


if __name__ == "__main__":
    P.finir_pirate(construire(), NOM, POINT, y_min=0.2)
