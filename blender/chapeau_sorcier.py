"""
chapeau_sorcier.py — Chapeau de sorcier (boîte d'accessoires n°1 : `chapeau_sorcier`, Rare, Tête).
VERSION v5 « plus jeu moderne » (docs/art-bible.md §13.8 fiche 10), fait SUR le cheval.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/chapeau_sorcier.py
Sortie : blender/exports/chapeau_sorcier.fbx et docs/captures/v5_cheval_chapeau_sorcier.png.

Les trois niveaux de forme (bible §13.2) :
  - primaire   : le grand bord ondulé (6 vagues de 4 cm) et le cône dont la pointe fait un
                 S (2 plis, le bout retombe vers l'arrière gauche, pour être vu de face) ;
  - secondaire : le ruban jaune satiné et la grande étoile Ø 0,20 bombée, vernie, avec un
                 reflet peint — le détail-signature ;
  - tertiaire  : 3 petites étoiles jaunes semées sur le cône ; une pièce rapiécée carrée
                 (12 cm) violette avec 4 points de couture (« vieux chapeau aimé »).
Matières : feutre mat (0,20), ruban satin (0,50), étoiles vernies (0,90), reflet.

Hiérarchie (le code Unity retrouve les pièces par leur nom) :
    chapeau_sorcier     vide, origine = PointChapeau (attachScale 1, attachOffset 0)
    └── Chapeau         bord + cône + ruban + étoiles + pièce ; origine = centre de la base
        ├── Anim_Pointe le dernier tiers de la pointe ; origine = le 2e pli (ressort, et
        │               balancement ±6° en 2,5 s : bible §13.5 types A et B)
        ├── Eclat_1     vides : points d'éclat sur les branches de la grande étoile
        ├── Eclat_2     (un éclat toutes les 4 s, sans émission : rareté Rare)
        └── Eclat_3
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "chapeau_sorcier"
POINT = "PointChapeau"
MATS = ["M5_FeutreViolet", "M5_RubanJaune", "M5_JauneVerni", "M5_Rapiece", "M5_Reflet"]
BX, BY, BZ = C.CHAPEAU_BASE[0], C.CHAPEAU_BASE[1] + 0.08, C.CHAPEAU_BASE[2]
R_CONE = 0.31
# colonne du cône : (avant Blender y, hauteur z) et facteur de rayon
# (x, avant y, hauteur z) en Blender, base du chapeau à l'origine. La pointe retombe vers
# l'arrière ET vers la gauche de l'image (+X) : droit vers l'arrière, la caméra du jeu (qui
# est devant) ne verrait jamais le S.
COLONNE = [((0.0, 0.0, 0.0), 1.0), ((0.0, 0.0, 0.20), 0.80), ((0.0, 0.03, 0.38), 0.60),
           ((0.03, 0.02, 0.53), 0.43), ((0.10, -0.04, 0.64), 0.30),
           ((0.20, -0.10, 0.70), 0.20),                                       # 2e pli
           ((0.29, -0.15, 0.67), 0.12), ((0.34, -0.19, 0.59), 0.035)]
PLI = 5                  # indice du 2e pli : la pièce Anim_Pointe commence ici


def colonne(i):
    p, f = COLONNE[i]
    return Vector(p), f


def cone(bm, debut, fin):
    chemin = [colonne(i)[0] for i in range(debut, fin + 1)]
    rayons = [colonne(i)[1] for i in range(debut, fin + 1)]
    return C.bm_tube(bm, chemin, R_CONE, cotes=12, mat=0, haut=Vector((1, 1, 0)).normalized(),
                     boucher=True, rayons=rayons)


def bord(bm):
    n = 36
    loops = []
    for r_, dz in ((R_CONE - 0.03, 0.018), (0.54, 0.018), (0.56, 0.0), (0.54, -0.018),
                   (R_CONE - 0.03, -0.018)):
        loop = []
        for k in range(n):
            a = 2 * math.pi * k / n
            vague = 0.04 * math.sin(6 * a) * max(0.0, (r_ - R_CONE) / (0.56 - R_CONE))
            loop.append((r_ * math.cos(a), r_ * math.sin(a), dz + vague))
        loops.append(loop)
    loops.append(loops[0])
    rangs = C.bm_anneaux(bm, loops, mats=[0] * 5)
    v = [x for r in rangs for x in r]
    bmesh.ops.remove_doubles(bm, verts=v, dist=1e-5)
    return [x for x in v if x.is_valid]


def ruban(bm):
    prof = [(R_CONE - 0.004, 0.015), (R_CONE + 0.012, 0.025), (R_CONE * 0.93 + 0.012, 0.11),
            (R_CONE * 0.91, 0.12)]
    v = C.bm_revolution(bm, prof, 12, mats=[1] * 3)
    return [x for r in v for x in r]


def construire():
    C.nouvelle_scene()
    base = C.U(BX, BY, BZ)
    bm = bmesh.new()
    v = cone(bm, 0, PLI)
    arbre = C.arbre_bm(bm)          # le cône seul, pour poser les détails dessus
    v += bord(bm) + ruban(bm)
    # grande étoile Ø 0,20 sur le ruban, devant (un peu à gauche de l'image)
    d = C.direction_angles(0.25, math.radians(10))
    p, n = C.poser_sur(arbre, Vector((0, 0, 0.07)), d, 0.03)
    m_et = C.repere(p, Vector((n.x, n.y, 0)).normalized())
    v += C.bm_etoile(bm, 0.10, 0.035, m_et, mat=2, bombe=0.02)
    v += C.bm_reflet(bm, m_et @ Matrix.Translation((0.02, 0.05, 0.02)), mat=4, echelle=0.55)
    eclats = [m_et @ Vector((0.10 * math.cos(math.pi / 2 + 2 * math.pi * k / 5), 0.04,
                             0.10 * math.sin(math.pi / 2 + 2 * math.pi * k / 5)))
              for k in (0, 1, 4)]
    # 3 petites étoiles sur le cône
    for ang, h in ((-0.45, 0.28), (0.6, 0.36), (-0.1, 0.47)):
        p, n = C.poser_sur(arbre, Vector((0, 0.01, h)), C.direction_angles(ang), 0.005)
        v += C.bm_etoile(bm, 0.045, 0.025, C.repere(p, n), mat=2)
    # pièce rapiécée (12 cm) et 4 points de couture, à droite de l'image
    p, n = C.poser_sur(arbre, Vector((0, 0, 0.24)), C.direction_angles(-0.9), 0.006)
    m_p = C.repere(p, n) @ Matrix.Rotation(math.radians(12), 4, "Y")
    v += C.bm_plaque(bm, C.contour_rectangle_arrondi(0.12, 0.12, 0.015, par_coin=1), 0.02,
                     m_p, mat=3)
    for sx, sz in ((-1, -1), (-1, 1), (1, -1), (1, 1)):
        v += C.bm_boite(bm, (0.02, 0.015, 0.02), m_p @ Vector((sx * 0.045, 0.012, sz * 0.045)),
                        mat=1)
    bmesh.ops.translate(bm, vec=base, verts=list(set(v)))
    chapeau = C.piece("Chapeau", bm, MATS, lissage="angle", pivot=base)

    bm = bmesh.new()
    vp = cone(bm, PLI, len(COLONNE) - 1)
    bmesh.ops.translate(bm, vec=base, verts=vp)
    pointe = C.piece("Anim_Pointe", bm, MATS, lissage="tout", pivot=base + colonne(PLI)[0])
    C.parenter(pointe, chapeau)
    for e in C.reperes_effets([base + e for e in eclats]):
        C.parenter(e, chapeau)
    return C.racine_v5(NOM, POINT, [chapeau])


if __name__ == "__main__":
    C.finir_v5(construire(), NOM, POINT)
