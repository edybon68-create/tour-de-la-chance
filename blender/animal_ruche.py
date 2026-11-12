"""
animal_ruche.py — Abeilles et pot de miel (boîte « Animaux : accessoires » : `ruche`,
Super rare, Tête). docs/specs/v5-catalogue.md §4 n°14, bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/animal_ruche.py
Sortie : blender/exports/ruche.fbx et docs/captures/v5_cheval_ruche.png.

Un petit pot de miel posé sur le crâne, entre les oreilles, et TROIS abeilles rondes qui
tournent autour de la tête sur des orbites différentes.

Trois niveaux de forme :
  - primaire   : le pot ventru crème (Ø 0,26) et les trois abeilles (Ø 0,13 : la fiche dit
                 0,10, exagérées à 130 %) ;
  - secondaire : le miel qui déborde du pot (bourrelet + 3 coulures), les ailes
                 transparentes, les rayures jaune / brun ;
  - tertiaire  : yeux des abeilles, dard, petites antennes, ficelle du pot.
Matière précieuse du Super rare : le miel « pierre précieuse » (verni 0,95), qui luit à 0,4
(≤ 10 % de la surface).

Orbites (le code les fait tourner : composant OrbitPath) : centrées au-dessus de la tête
(y = 2,0), rayons 0,50 / 0,58 / 0,46, un tour en 2,5 / 4 / 3,2 s, inclinées pour être
toujours plus HAUTES DEVANT (y ≥ 1,95 devant le visage) : elles ne passent jamais devant
les yeux. Au saut de joie, le code leur fait dessiner un cœur (trajectoire).

Hiérarchie :
    ruche                    vide, origine = PointChapeau (attachScale 1, attachOffset 0)
    ├── Pot                  pot + miel ; origine = sa base
    │   ├── Eclat_1..3       vides sur le miel (Super rare : 1 éclat / 2,5 s, émission 0,5)
    │   └── Fx_Emetteur      vide au-dessus du pot (≤ 4 particules : petites gouttes de miel)
    ├── Orbite_1..3          vides au centre des orbites, déjà inclinés : le code les fait
    │   │                    tourner autour de leur axe Y local (Unity)
    │   └── Anim_Abeille_N   l'abeille ; origine = son centre (petit tangage)
    │       └── Anim_Ailes_N     les 2 ailes ; origine = le dos (elles « vibrent » : le code
    │                            fait osciller leur échelle latérale très vite, exception
    │                            voulue de la fiche ; 6 pièces animées au total)
"""

import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_animaux as A  # noqa: E402

NOM = "ruche"
POINT = "PointChapeau"
# 5 matières (bible §13.7, Super rare) : yeux et ficelle dans le brun des rayures.
MATS = ["M5_PotMiel", "M5_Miel", "M5_AbeilleJaune", "M5_AbeilleBrune", "M5_AileAbeille",
        "M5_AbeilleBrune", "M5_AbeilleBrune"]  # 0..6
CENTRE_ORBITES = (0.0, 2.0, 0.03)
# rayon, inclinaison vers l'avant (°, devant en haut), inclinaison de côté (°), angle de départ
ORBITES = [(0.50, 14, 8, 30), (0.58, 10, -12, 160), (0.46, 18, 4, 270)]
RB = 0.065                       # rayon d'une abeille


def pot():
    base = C.U(0.0, 1.87, 0.04)
    bm = bmesh.new()
    profil = [(0.0, 0.0), (0.09, 0.0), (0.125, 0.05), (0.135, 0.11), (0.115, 0.17),
              (0.085, 0.195), (0.09, 0.215), (0.075, 0.22), (0.0, 0.205)]
    C.bm_revolution(bm, profil, 14, mats=[0] * 8)
    # ficelle sous le col
    C.bm_tore(bm, 0.093, 0.012, segments=14, segments_tube=4, mat=6,
              matrice=Matrix.Translation((0, 0, 0.19)))
    # miel : bourrelet ondulé sur le haut + 3 coulures
    C.bm_tore(bm, 0.07, 0.03, segments=14, segments_tube=5, mat=1,
              matrice=Matrix.Translation((0, 0, 0.225)),
              rayon_fn=lambda a: 0.03 + 0.008 * math.cos(3 * a))
    for k, (a, L) in enumerate(((0.6, 0.10), (2.4, 0.07), (4.3, 0.12))):
        d = Vector((math.cos(a), math.sin(a), 0))
        haut = d * 0.098 + Vector((0, 0, 0.215))
        chemin = [haut, d * 0.122 + Vector((0, 0, 0.17)), d * 0.135 + Vector((0, 0, 0.215 - L))]
        C.bm_tube(bm, chemin, 0.022, cotes=5, mat=1, rayons=[1.0, 0.9, 1.15])
    bmesh.ops.translate(bm, vec=base, verts=bm.verts)
    obj = C.piece("Pot", bm, MATS, lissage="tout", pivot=base)
    eclats = [base + Vector((0.05, 0.06, 0.26)), base + Vector((0.08, 0.1, 0.12)),
              base + Vector((-0.06, 0.04, 0.26))]
    for e in C.reperes_effets(eclats, fx=base + Vector((0, 0, 0.32))):
        C.parenter(e, obj)
    return obj


def abeille(n, centre):
    """Une abeille ronde construite autour de `centre`, qui vole vers +X Blender (on la
    tourne ensuite dans le sens de son orbite)."""
    bm = bmesh.new()
    A.bm_ellipsoide(bm, (0, 0, 0), (RB * 1.15, RB, RB * 0.95), segments=10, anneaux=6, mat=2)
    for f in bm.faces:                          # 2 rayures brunes + l'arrière brun
        x = f.calc_center_median().x / RB
        if -0.55 < x < -0.2 or 0.1 < x < 0.42 or x < -0.95:
            f.material_index = 3
    A.bm_goutte(bm, 0.035, 0.014, segments=5, bandes=((1.0, 3),),
                matrice=A.matrice_vers(Vector((-RB * 1.08, 0, 0)), Vector((-1, 0, 0))))
    for s in (-1, 1):                           # yeux devant, antennes
        A.oeil_mignon(bm, Vector((RB * 0.95, s * RB * 0.38, RB * 0.2)),
                      Vector((1, s * 0.4, 0.1)), RB * 0.24, 5, 5, hauteur=1.2)
        bout = Vector((RB * 1.3, s * RB * 0.5, RB * 1.5))
        C.bm_tube(bm, [Vector((RB * 0.7, s * RB * 0.3, RB * 0.75)), bout], 0.008, cotes=4, mat=3)
        C.bm_sphere(bm, 0.014, bout, segments=5, anneaux=3, mat=3)
    bmesh.ops.translate(bm, vec=centre, verts=bm.verts)
    corps = C.piece(f"Anim_Abeille_{n}", bm, MATS, lissage="tout", pivot=centre)
    # les deux ailes en UN objet (bible §13.7 : 6 pièces animées au plus au Super rare) :
    # le code fait vibrer son échelle (largeur 1 ↔ 0,35, très vite) au lieu de la tourner
    bm = bmesh.new()
    for s in (1, -1):
        racine = Vector((0.0, s * RB * 0.5, RB * 0.75))
        m = Matrix.Translation(racine) @ Matrix.Rotation(math.radians(s * 25), 4, "X") \
            @ Matrix.Translation((-0.01, s * 0.06, 0.0)) @ Matrix.Rotation(math.radians(90), 4, "X")
        A.bm_aile_plate(bm, C.contour_ellipse(0.045, 0.065, n=8), 0.012, m, mat=4)
    bmesh.ops.translate(bm, vec=centre, verts=bm.verts)
    dos = centre + Vector((0, 0, RB * 0.75))
    C.parenter(C.piece(f"Anim_Ailes_{n}", bm, MATS, lissage="tout", pivot=dos), corps)
    return corps


def construire():
    C.nouvelle_scene()
    enfants = [pot()]
    c = C.U(*CENTRE_ORBITES)
    for n, (r, avant, cote, depart) in enumerate(ORBITES, start=1):
        orb = C.vide(f"Orbite_{n}", c)
        # devant (+Y Blender) vers le haut : rotation positive autour de X ; puis de côté
        orb.rotation_euler = (math.radians(avant), math.radians(cote), 0.0)
        bpy.context.view_layer.update()
        a = math.radians(depart)
        local = Vector((r * math.cos(a), r * math.sin(a), 0.0))
        pos = orb.matrix_world @ local
        ab = abeille(n, pos)
        # elle regarde dans le sens du vol (tangente de l'orbite, sens inverse des aiguilles)
        tangente = (orb.matrix_world.to_3x3() @ Vector((-math.sin(a), math.cos(a), 0))).normalized()
        ab.rotation_euler = Vector((1, 0, 0)).rotation_difference(tangente).to_euler()
        bpy.context.view_layer.update()
        C.parenter(ab, orb)
        enfants.append(orb)
    return C.racine_v5(NOM, POINT, enfants)


if __name__ == "__main__":
    A.finir_animal(construire(), NOM, POINT)
