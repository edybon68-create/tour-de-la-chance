"""
animal_bocal_poisson.py — Bocal à poisson rouge (boîte « Animaux : accessoires » :
`bocal_poisson`, Légendaire, Tête). docs/specs/v5-catalogue.md §4 n°15, bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/animal_bocal_poisson.py
Sortie : blender/exports/bocal_poisson.fbx et docs/captures/v5_cheval_bocal_poisson.png.

Un bocal rond en verre posé sur la tête comme un casque, rempli d'eau bleutée qui luit ;
un poisson rouge y nage en rond autour des algues.

Pourquoi posé AU-DESSUS du crâne (et pas enfilé sur la tête) ?
    Enfilé, l'eau recouvrirait les yeux du cheval (interdit, catalogue §0.2). Le bocal
    (Ø 0,66 : la fiche dit 0,55, +20 %) repose donc sur un coussin bleu entre les oreilles ;
    son bas est assez étroit pour passer entre elles (vérifié en 5 vues).

Trois niveaux de forme :
  - primaire   : la sphère de verre ouverte en haut (bord roulé), l'eau dedans ;
  - secondaire : le poisson rouge (0,27 de long, nageoires en voile), le coussin
                 d'appui, 3 algues ondulées sur un tas de galets ;
  - tertiaire  : gros yeux du poisson, galets colorés, nageoires dorsale et latérales.
Matières (5) : verre transparent 0,3 (smoothness 0,95), eau transparente 0,55 émissive 0,5
(elle « luit doucement »), poisson verni 0,75, algues 0,50, noir verni (yeux). Pas de
sable jaune : il aurait fait une 6e matière (bible §13.7 : 5 au plus) ; à la place, un tas
de galets verts et orange (mêmes matières que les algues et le poisson).

Hiérarchie :
    bocal_poisson           vide, origine = PointChapeau (attachScale 1, attachOffset 0)
    └── Bocal               le verre + le coussin + les galets ; origine = le bas du bocal
        ├── Anim_Eau        l'eau ; origine = centre de sa surface (au saut : la surface
        │                   penche ±8° puis revient, « vague »)
        ├── Anim_Algues     les 3 algues ; origine = leur pied (ondulent ±6°, 2,6 s)
        ├── Anim_OrbitePoisson  vide au centre du bocal : le code le fait tourner autour
        │   │                   de son Y local, un tour en 4 s
        │   └── Anim_Poisson    le poisson ; origine = son centre (au saut de joie : il
        │       │               saute hors de l'eau, 0,25 m, et y retombe)
        │       ├── Anim_QueuePoisson  origine = la racine de la queue (±20°, 0,8 s)
        │       └── Fx_Emetteur        vide devant sa bouche (bulles, réserve de 8)
        └── Eclat_1..3      vides sur le bord du verre (Légendaire : 1 éclat / 1,5 s)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_animaux as A  # noqa: E402

NOM = "bocal_poisson"
POINT = "PointChapeau"
MATS = ["M5_VerreBocal", "M5_EauBocal", "M5_PoissonRouge", "M5_Algue", "M5_AnimNoirVerni"]
R = 0.33                               # rayon du bocal (Ø 0,66 : 120 % de la fiche)
KP = 1.35                              # le poisson, détail-signature, exagéré à 135 %
BAS_Y = 1.93                           # bas du bocal (Unity)
CENTRE_Y = BAS_Y + R * 0.92            # le fond est aplati
NIVEAU = 0.10                          # surface de l'eau au-dessus du centre


def verre(bm, c):
    """Sphère de verre aplatie en bas, ouverte en haut, paroi de 1,2 cm, bord roulé."""
    ext = []
    for k in range(12):
        e = math.radians(-67 + 128 * k / 11)      # du fond plat jusqu'au col
        ext.append((R * math.cos(e), R * math.sin(e)))
    ext = [(R * math.cos(math.radians(-67)) * 0.6, -R * 0.92)] + ext
    inn = [(r - 0.012, z) for r, z in reversed(ext)]
    profil = [(0.0, -R * 0.92)] + ext + inn + [(0.0, -R * 0.92 + 0.012)]
    av = set(bm.verts)
    C.bm_revolution(bm, profil, 18, mats=[0] * (len(profil) - 1))
    nv = [v for v in bm.verts if v not in av]
    haut = R * math.sin(math.radians(61))
    nv += C.bm_tore(bm, R * math.cos(math.radians(61)) + 0.004, 0.022, segments=18,
                    segments_tube=5, mat=0, matrice=Matrix.Translation((0, 0, haut)))
    bmesh.ops.translate(bm, vec=c, verts=nv)
    return haut


def coussin(bm, c):
    """Coussin d'appui (même eau bleutée, opaque visuellement grâce à sa forme) : un tore
    plat sous le bocal, posé sur le crâne. Dans la matière du verre pour ne pas en ajouter."""
    v = C.bm_tore(bm, 0.12, 0.04, segments=14, segments_tube=6, mat=1)
    bmesh.ops.transform(bm, matrix=Matrix.Translation(c + Vector((0, 0, -R * 0.92 - 0.02)))
                        @ Matrix.Diagonal((1, 1, 0.7, 1)), verts=v)


def galets(bm, c):
    rnd = [(0.0, 0.0, 0.06, 3), (0.08, 0.04, 0.05, 2), (-0.07, 0.05, 0.045, 3),
           (0.03, -0.08, 0.05, 2), (-0.06, -0.06, 0.04, 2), (0.1, -0.05, 0.035, 3),
           (-0.11, 0.0, 0.04, 2)]
    for x, y, r, m in rnd:
        A.bm_ellipsoide(bm, c + Vector((x, y, -R * 0.92 + 0.012 + r * 0.45)), (r, r * 0.9, r * 0.6),
                        segments=6, anneaux=4, mat=m)


def eau(c):
    bm = bmesh.new()
    zs = NIVEAU
    profil = [(0.0, zs)]
    e_top = math.asin(zs / (R - 0.014))
    for k in range(8):
        e = e_top - (e_top + math.radians(64)) * k / 7
        profil.append(((R - 0.014) * math.cos(e), (R - 0.014) * math.sin(e)))
    profil.append((0.0, -R * 0.92 + 0.014))
    C.bm_revolution(bm, profil, 18, mats=[1] * (len(profil) - 1))
    bmesh.ops.translate(bm, vec=c, verts=bm.verts)
    return C.piece("Anim_Eau", bm, MATS, lissage="tout", pivot=c + Vector((0, 0, zs)))


def algues(c):
    bm = bmesh.new()
    pied = c + Vector((0, 0, -R * 0.92 + 0.03))
    for k, (x, y, h, ph) in enumerate(((0.0, 0.02, 0.30, 0.0), (0.06, -0.04, 0.24, 1.3),
                                       (-0.06, -0.02, 0.20, 2.4))):
        chemin = [pied + Vector((x + 0.03 * math.sin(ph + t * 5), y, h * t))
                  for t in (0.0, 0.25, 0.5, 0.75, 1.0)]
        C.bm_tube(bm, chemin, 0.024, cotes=5, mat=3, rayons=[1.0, 1.1, 1.0, 0.8, 0.4])
    return C.piece("Anim_Algues", bm, MATS, lissage="tout", pivot=pied)


def poisson(c):
    """Poisson rouge qui nage vers +X Blender (on le pose ensuite sur son orbite)."""
    centre = c + Vector((0.0, 0.15, -0.07))         # devant, à 0,15 du centre du bocal
    bm = bmesh.new()
    A.bm_ellipsoide(bm, (0, 0, 0), (0.085, 0.055, 0.065), segments=10, anneaux=7, mat=2)
    # nageoire dorsale (voile) et 2 nageoires latérales
    dors = [(-0.05, 0.0), (0.03, 0.0), (-0.01, 0.06), (-0.06, 0.05)]
    C.bm_coussin(bm, [(x + 0.012, z - 0.02) for x, z in dors], 0.012, 0.02,
                 Matrix.Translation((0, 0, 0.075)), mat=2, anneaux=1)
    for s in (-1, 1):
        C.bm_coussin(bm, C.contour_ellipse(0.03, 0.018, n=8), 0.01, 0.016,
                     Matrix.Translation((0.0, s * 0.055, -0.03))
                     @ Matrix.Rotation(math.radians(s * 50), 4, "X")
                     @ Matrix.Rotation(math.radians(90), 4, "Z"), mat=2, anneaux=1)
        A.oeil_mignon(bm, Vector((0.055, s * 0.036, 0.02)), Vector((0.5, s, 0.15)), 0.022, 4, 4,
                      hauteur=1.1)
    bmesh.ops.transform(bm, matrix=Matrix.Translation(centre)
                        @ Matrix.Rotation(math.radians(180), 4, "Z")
                        @ Matrix.Diagonal((KP, KP, KP, 1)), verts=bm.verts)
    corps = C.piece("Anim_Poisson", bm, MATS, lissage="tout", pivot=centre)
    # queue en voile, derrière (+X Blender après la rotation de 180°)
    bm = bmesh.new()
    racine = centre + Vector((0.075 * KP, 0, 0))
    voile = [(0.0, 0.01), (0.09, 0.07), (0.12, 0.03), (0.1, 0.0), (0.12, -0.03),
             (0.09, -0.06), (0.0, -0.01)]
    cx = sum(p[0] for p in voile) / len(voile)
    C.bm_coussin(bm, [((x - cx) * KP, z * KP) for x, z in voile], 0.014, 0.026,
                 Matrix.Translation(racine + Vector((cx * KP, 0, 0))), mat=2, anneaux=1)
    C.parenter(C.piece("Anim_QueuePoisson", bm, MATS, lissage="tout", pivot=racine), corps)
    fx = C.reperes_effets([], fx=centre + Vector((-0.11 * KP, 0, 0.02)))[0]
    C.parenter(fx, corps)
    return corps


def construire():
    C.nouvelle_scene()
    c = C.U(0.0, CENTRE_Y, 0.0)
    bm = bmesh.new()
    haut = verre(bm, c)
    coussin(bm, c)
    galets(bm, c)
    bas = c + Vector((0, 0, -R * 0.92))
    bocal = C.piece("Bocal", bm, MATS, lissage="tout", pivot=bas)
    C.parenter(eau(c), bocal)
    C.parenter(algues(c), bocal)
    orbite = C.vide("Anim_OrbitePoisson", c)
    C.parenter(orbite, bocal)
    C.parenter(poisson(c), orbite)
    rb = R * math.cos(math.radians(61))
    eclats = [c + Vector((rb * math.cos(a), rb * math.sin(a), haut + 0.02)) for a in (1.0, 2.4, 0.2)]
    for e in C.reperes_effets(eclats):
        C.parenter(e, bocal)
    return C.racine_v5(NOM, POINT, [bocal])


if __name__ == "__main__":
    A.finir_animal(construire(), NOM, POINT)
