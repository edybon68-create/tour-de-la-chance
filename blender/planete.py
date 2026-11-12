"""
planete.py — Petite planète (boîte d'accessoires n°1 : `planete`, Secret, Dos).
VERSION v5 « plus jeu moderne » (docs/art-bible.md §13.8 fiche 16), faite pour le cheval.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/planete.py
Sortie : blender/exports/planete.fbx et docs/captures/v5_cheval_planete.png.

Les tailles de la fiche (boule Ø 0,40, anneaux Ø 0,70 et 0,82, orbite 0,9) étaient données
« dans le modèle », avant l'échelle 0,8 qu'avait la planète sur le cheval. Le modèle v5 est
exporté à l'échelle 1 : on les multiplie donc par 0,8 ici (boule Ø 0,32, anneaux Ø 0,56 et
0,66 séparés par un vide de 3 cm, lune Ø 0,075 à 0,26) : à l'écran, la planète a exactement
la taille voulue par le DA (un tiers plus grosse que l'ancienne).

L'orbite (rayon 0,85, un tour en 6 s) est centrée AU-DESSUS de la tête (y = 2,02) et penchée
de 20° : HAUTE devant (y ≈ 2,31), basse derrière (y ≈ 1,73, cachée par la tête). Pourquoi ?
Une orbite basse devant faisait traverser le museau par les grands anneaux ; à hauteur des
yeux, la planète les cachait. Ici elle passe au-dessus des lignes de regard de la caméra,
reste dans l'enveloppe (1,18 m de chaque côté) et laisse 10 cm autour d'un chapeau porté.

Les trois niveaux de forme (bible §13.2) :
  - primaire   : la boule lavande satinée (SANS émission) et ses deux anneaux qui luisent ;
  - secondaire : 2 bandes en relief violet magique, la petite lune blanc cassé mate ;
  - tertiaire  : 3 cratères (un bourrelet et un fond plus sombre).

Hiérarchie (le code Unity retrouve les pièces par leur nom ; noms v3 gardés pour
AccessoryMotion : « Orbite » tourne à 60°/s, « Planete » tourne sur elle-même à 90°/s ;
le matériau « M_AnneauPlanete » est émissif) :
    planete                  vide, origine = PointDos (attachScale 1, attachOffset 0)
    └── Inclinaison          vide FIXE au centre de l'orbite, penché de 20° (devant en haut)
        └── Orbite           vide : LE CODE LE FAIT TOURNER autour de son axe Y local
            ├── Planete      la boule ; pivot au centre, à 0,85 du centre de l'orbite
            │   ├── AnneauPrincipal   anneau Ø 0,56 (émission 0,5 ↔ 1,2, période 2 s)
            │   └── AnneauExterieur   anneau Ø 0,66 (même chose, décalé d'une demi-période)
            ├── Anim_OrbiteLune  vide au centre de la planète, penché de 30° : à faire
            │   │                tourner autour de son Y local, un tour en 2 s
            │   └── Lune     la petite lune, à 0,26 du centre
            ├── Eclat_1..3   vides : sur les anneaux (1 éclat par seconde, arc-en-ciel)
            └── Fx_Emetteur  vide : au centre de la planète (traînée de poussière d'étoile,
                             ≤ 16 petites étoiles laissées sur l'orbite)
"""

import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "planete"
POINT = "PointDos"
MATS = ["M5_Planete", "M5_BandePlanete", "M_AnneauPlanete", "M5_LuneMat"]  # 0..3
K = 0.8                                  # tailles de la fiche × 0,8 (voir plus haut)
CENTRE_ORBITE = (0.0, 2.02, 0.05)        # Unity, local à Joueur
RAYON_ORBITE = 0.85
INCLINAISON_ORBITE = 20.0                # devant en HAUT
INCLINAISON_ANNEAU = 20.0
R_BOULE = 0.20 * K


def boule():
    bm = bmesh.new()
    n = 12
    profil, mats = [], []
    for k in range(n + 1):
        a = math.pi * k / n
        r, z = R_BOULE * math.sin(a), R_BOULE * math.cos(a)
        profil.append((r, z))
    # 2 bandes en relief : entre les anneaux 4-5 et 7-8 du profil, +1,2 cm
    def bande(a, i, r, z):
        return (r * 1.0 + (0.012 if i in (4, 5, 8, 9) else 0.0), z)
    mats = [1 if i in (4, 8) else 0 for i in range(n)]
    C.bm_revolution(bm, profil, 18, mats=mats, rayon_fn=bande)
    arbre = C.arbre_bm(bm)
    for ang, el in ((0.4, 0.75), (2.4, -0.15), (4.2, 0.35)):
        d = C.direction_angles(ang, el)
        p, nn = C.poser_sur(arbre, Vector((0, 0, 0)), d, 0.0)
        m = C.repere(p, nn)
        cr = C.bm_tore(bm, 0.04, 0.012, segments=8, segments_tube=4, mat=0)
        bmesh.ops.transform(bm, matrix=m @ Matrix.Rotation(math.radians(90), 4, "X"), verts=cr)
        C.bm_disque(bm, 0.032, 0.01, m @ Matrix.Translation((0, -0.002, 0)), cotes=8, mat=1)
    return bm


def anneau(r_in, r_out, ep=0.03):
    bm = bmesh.new()
    prof = [(r_in, -ep / 2), (r_out, -ep / 2), (r_out + 0.008, 0.0), (r_out, ep / 2),
            (r_in, ep / 2), (r_in, -ep / 2)]
    C.bm_revolution(bm, prof, 40, mats=[2] * 5)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bmesh.ops.transform(bm, matrix=Matrix.Rotation(math.radians(INCLINAISON_ANNEAU), 4, "Y"),
                        verts=bm.verts)
    return bm


def construire():
    C.nouvelle_scene()
    inclinaison = C.vide("Inclinaison", C.U(*CENTRE_ORBITE))
    # Devant (+Y Blender) vers le HAUT : rotation positive autour de X.
    inclinaison.rotation_euler = (math.radians(INCLINAISON_ORBITE), 0, 0)
    bpy.context.view_layer.update()
    orbite = C.vide("Orbite")
    orbite.matrix_world = inclinaison.matrix_world.copy()
    C.parenter(orbite, inclinaison)
    # Unity x = +0,85 (droite de l'image) = Blender x = -0,85, dans le repère penché.
    m_pl = orbite.matrix_world @ Matrix.Translation((-RAYON_ORBITE, 0, 0))
    centre = m_pl.translation.copy()

    def poser(bm, nom):
        bmesh.ops.translate(bm, vec=centre, verts=bm.verts)
        return C.piece(nom, bm, MATS, lissage="tout" if nom in ("Planete", "Lune") else "angle",
                       pivot=centre)

    planete = poser(boule(), "Planete")
    a1 = poser(anneau(0.20, 0.28), "AnneauPrincipal")      # Ø 0,56 (0,70 × 0,8)
    a2 = poser(anneau(0.305, 0.33, ep=0.022), "AnneauExterieur")   # Ø 0,66, vide de 2,5 cm
    for a in (a1, a2):
        C.parenter(a, planete)
    C.parenter(planete, orbite)

    lune_orbite = C.vide("Anim_OrbiteLune", centre)
    lune_orbite.rotation_euler = (0, math.radians(30), 0)
    bpy.context.view_layer.update()
    C.parenter(lune_orbite, orbite)
    bm = bmesh.new()
    pos_lune = lune_orbite.matrix_world @ Vector((0.0, 0.26, 0.0))
    C.bm_sphere(bm, 0.075 / 2, pos_lune, segments=8, anneaux=6, mat=3)
    lune = C.piece("Lune", bm, MATS, lissage="tout", pivot=pos_lune)
    C.parenter(lune, lune_orbite)

    eclats = [centre + Vector((0.27, 0.05, 0.1)), centre + Vector((-0.3, -0.05, -0.1)),
              centre + Vector((0.05, 0.31, 0.0))]
    for e in C.reperes_effets(eclats, fx=centre):
        C.parenter(e, orbite)
    return C.racine_v5(NOM, POINT, [inclinaison])


if __name__ == "__main__":
    C.finir_v5(construire(), NOM, POINT)
