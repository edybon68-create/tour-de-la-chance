"""
planete.py — Petite planète (boîte d'accessoires n°1 : `planete`, Secret, Dos).

Fiche suivie : docs/specs/boites.md §5.6 et art-bible.md §4.1 (émission 0,5 sur l'anneau).
Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/planete.py

La planète ne colle pas au dos : elle tourne en orbite autour de la tête. L'emplacement Dos
sert seulement à savoir avec quoi elle se combine. Hiérarchie produite (le code Unity anime,
pas d'animation Blender) :
    planete            objet vide, origine = point d'accroche Dos = `PointDos`,
                       Unity (0 ; 1,3 ; 0,5) local à Joueur : position et rotation locales à zéro
    └── Inclinaison    objet vide FIXE au centre de l'orbite : position locale (0 ; 0,6 ; −0,5)
        │              (= (0 ; 1,9 ; 0) local à Joueur, au-dessus du centre de la tête),
        │              penché de 15° : l'orbite descend devant (passe devant le visage, vers
        │              y ≈ 1,67) et remonte derrière (y ≈ 2,13)
        └── Orbite     objet vide, rotation locale nulle : LE CODE LE FAIT TOURNER autour de
            │          son axe Y local, 360° en 6 s (60°/s)
            └── Planete  la boule Ø 0,30 + son anneau Ø 0,52 incliné de 20° (une seule
                         pièce, 2 matériaux) ; pivot au centre de la boule, position locale
                         (0,9 ; 0 ; 0) = rayon de l'orbite ; LE CODE LA FAIT TOURNER sur
                         elle-même autour de son axe Y local à 90°/s (l'anneau penché
                         « valse » : c'est ce qui rend la rotation visible).
Au repos (avant animation), la planète est à droite de la tête vue de la caméra.
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "planete"
EMPLACEMENT = "dos"
MATS = ["M_Planete", "M_AnneauPlanete"]  # indices 0, 1
CENTRE_ORBITE = (0.0, 1.9, 0.0)   # Unity, local à Joueur
RAYON_ORBITE = 0.9
INCLINAISON_ORBITE = 15.0
INCLINAISON_ANNEAU = 20.0


def planete():
    bm = bmesh.new()
    C.bm_sphere(bm, 0.15, (0, 0, 0), segments=16, anneaux=10, mat=0)
    boule = C.objet_depuis_bmesh("Boule", bm, MATS)
    C.lisser_tout(boule)

    bm = bmesh.new()
    # Anneau plat : Ø extérieur 0,52, intérieur 0,38 (bande de 7 cm), 4 cm d'épaisseur.
    profil = [(0.19, -0.02), (0.26, -0.02), (0.26, 0.02), (0.19, 0.02), (0.19, -0.02)]
    C.bm_revolution(bm, profil, 24, mats=[1] * 4)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    # Penché de 20° en roulis (autour de l'axe avant-arrière) : la silhouette de Saturne.
    bmesh.ops.transform(bm, matrix=Matrix.Rotation(math.radians(INCLINAISON_ANNEAU), 4, "Y"),
                        verts=bm.verts)
    anneau = C.objet_depuis_bmesh("Anneau", bm, MATS)
    C.biseau(anneau, largeur=0.008, segments=1, angle=40)
    C.appliquer(anneau)
    C.lisser_angle(anneau, 40)
    return C.joindre([boule, anneau], "Planete")


def construire():
    C.nouvelle_scene()
    inclinaison = C.vide("Inclinaison", C.perso(*CENTRE_ORBITE))
    # Devant (+Y Blender) vers le bas : rotation négative autour de X.
    inclinaison.rotation_euler = (math.radians(-INCLINAISON_ORBITE), 0, 0)
    import bpy
    bpy.context.view_layer.update()

    orbite = C.vide("Orbite")
    orbite.matrix_world = inclinaison.matrix_world.copy()
    C.parenter(orbite, inclinaison)

    corps = planete()
    # Unity x = +0,9 (droite de l'image) = Blender x = -0,9, dans le repère penché.
    corps.matrix_world = orbite.matrix_world @ Matrix.Translation((-RAYON_ORBITE, 0, 0))
    C.parenter(corps, orbite)
    return C.racine_accroche(NOM, EMPLACEMENT, [inclinaison])


if __name__ == "__main__":
    C.finir_accessoire(construire(), NOM, EMPLACEMENT)
