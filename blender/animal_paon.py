"""
animal_paon.py — Extras 3D du skin Paon (boîte « Animaux : skins » : `animal_paon`,
Légendaire) : LA ROUE. docs/specs/v5-catalogue.md §3.1 et §0.4, bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/animal_paon.py
Sortie : blender/exports/animal_paon_extras.fbx et docs/captures/v5_cheval_animal_paon_extras.png
(roue repliée) + docs/captures/v5_cheval_animal_paon_roue.png (roue déployée, saut de joie).

Ce n'est PAS un accessoire : c'est le prefab `skinExtrasPrefab` du skin (catalogue §0.4),
posé comme enfant direct de `Joueur` (position et rotation locales nulles, échelle 1), sans
collider, retiré quand on change de skin. Le corps bleu irisé du paon est une matière du
code (texture calculée), pas ce fichier.

La roue : 13 grandes plumes (1,15 m : 0,9 dans la fiche, exagérées pour que l'éventail
déployé dépasse au-dessus de la tête), repliées à l'arrière à l'horizontale (elles dépassent
un peu de chaque côté), qui se DÉPLOIENT en éventail vertical derrière le cheval au saut de
joie (0,35 s ease-out-back), restent 1,2 s, puis se replient (0,3 s).
Chaque plume :
  - primaire   : la palme bleu paon (verni métallisé : metallic 0,35, smoothness 0,8),
                 en goutte allongée qui s'élargit vers le bout ;
  - secondaire : l'OCELLE (l'œil de la plume) : anneau vert puis cœur doré bombé,
                 émissifs 0,8 (le code les monte à 1,4 une par une, 0,04 s d'écart) ;
  - tertiaire  : la tige claire (dans la matière dorée), fine et effilée.
3 matières (budget des extras, catalogue §0.2) : M5_PlumePaon, M5_OcelleVert, M5_OcelleOr.

Pour le programmeur (rotations LOCALES, axes Unity) :
  - rotation locale NULLE d'une plume = plume DEBOUT (le long de +Y), face vers la caméra ;
  - pose repliée = la rotation enregistrée dans le fichier (couchée vers l'arrière) ;
  - pose déployée de la plume i (i = 0 à 12, de gauche à droite de l'écran) :
        Quaternion.Euler(0, 0, 84 − 14 × i)     (un éventail de −84° à +84°)
    le vide `Roue` est déjà penché de 10° vers l'arrière : l'éventail ne touche pas la crinière ;
  - au repos, la roue frémit : ±2° sur `Roue` (période 3 s).

Hiérarchie :
    animal_paon_extras       vide, origine = la base du cheval (enfant de Joueur)
    └── Roue                 vide : la base de l'éventail, sur l'arrière du cou (y 0,98)
        └── Anim_Plume_01..13   une plume ; origine = sa base (sa charnière)
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

NOM = "animal_paon_extras"
MATS = ["M5_PlumePaon", "M5_OcelleVert", "M5_OcelleOr"]  # 0..2
BASE = (0.0, 0.98, 0.40)            # Unity
N = 13
L = 1.15                            # 0,9 m dans la fiche, exagéré (bible §13.2)


def plume_bm(longueur):
    """Une plume debout le long de +Z (base en 0), face avant = +Y."""
    bm = bmesh.new()
    # tige : fine, dorée, effilée
    C.bm_tube(bm, [Vector((0, 0, 0)), Vector((0, 0, 0.3 * longueur)), Vector((0, 0, 0.62 * longueur))],
              0.018, cotes=4, mat=2, rayons=[1.0, 0.8, 0.5])
    # palme : goutte allongée, large vers le bout
    cont = []
    n = 10
    for k in range(n):
        t = 2 * math.pi * k / n
        z = -math.cos(t)                         # -1 (bas) .. 1 (haut)
        larg = 0.155 * math.sin(t) * (0.35 + 0.65 * ((z + 1) / 2) ** 0.7)
        cont.append((larg, z * 0.36 * longueur))
    cont.reverse()
    zc = 0.6 * longueur
    C.bm_coussin(bm, cont, 0.02, 0.035, Matrix.Translation((0, 0, zc)), mat=0, anneaux=1)
    # ocelle : anneau vert puis cœur doré bombé, près du bout
    zo = 0.78 * longueur
    C.bm_plaque(bm, C.contour_ellipse(0.10, 0.13, n=8), 0.02,
                Matrix.Translation((0, 0.022, zo)), mat=1)
    C.bm_coussin(bm, C.contour_ellipse(0.06, 0.078, n=8), 0.02, 0.045,
                 Matrix.Translation((0, 0.035, zo - 0.005)), mat=2, anneaux=1, dos_plat=True)
    return bm


def construire():
    C.nouvelle_scene()
    base = C.U(*BASE)
    roue = C.vide("Roue", base)
    roue.rotation_euler = (math.radians(10), 0, 0)     # penchée vers l'arrière
    bpy.context.view_layer.update()
    for i in range(N):
        t = i / (N - 1)
        longueur = L * (0.86 + 0.14 * math.sin(t * math.pi))   # celles du milieu plus longues
        bm = plume_bm(longueur)
        nom = f"Anim_Plume_{i + 1:02d}"
        o = C.objet_depuis_bmesh(nom, bm, MATS)
        C.lisser_tout(o)
        o.matrix_world = roue.matrix_world.copy()
        C.parenter(o, roue)
        # repliée : couchée vers l'arrière (rotation X ≈ 85°), en éventail horizontal
        # (−36° à +36°) ; i = 0 à gauche de l'écran (+X Blender)
        # (couchée, la plume pointe vers l'arrière −Y ; tourner autour de Z d'un angle
        # positif l'emmène vers +X, la gauche de l'écran)
        o.rotation_mode = "XYZ"
        o.rotation_euler = (math.radians(85), 0.0, math.radians(36 - 72 * t))
        bpy.context.view_layer.update()
    return A.racine_joueur(NOM, [roue])


def rendre_deployee(racine):
    """Rendu « saut de joie » : chaque plume debout, en éventail (même calcul que le code)."""
    plumes = sorted([o for o in C.descendants(racine) if o.name.startswith("Anim_Plume_")],
                    key=lambda o: o.name)
    gardees = [o.rotation_euler.copy() for o in plumes]
    for i, o in enumerate(plumes):
        # Unity Euler(0, 0, 84 − 14 i) autour de l'axe avant-arrière (Z Unity = −Y Blender ;
        # Unity tourne « à gauche », Blender « à droite » : le signe se compense). i = 0 ->
        # penchée vers +X Blender, la gauche de l'écran.
        o.rotation_euler = (0.0, math.radians(84 - 14 * i), 0.0)
    bpy.context.view_layer.update()
    C.MONDE_DEGRADE = True
    C.rendre_porte_cheval(racine, "v5_cheval_animal_paon_roue.png", "Joueur", y_min=0.3)
    for o, r in zip(plumes, gardees):
        o.rotation_euler = r
    bpy.context.view_layer.update()


if __name__ == "__main__":
    r = construire()
    rendre_deployee(r)
    A.finir_animal(r, NOM, "Joueur", y_min=0.3)
