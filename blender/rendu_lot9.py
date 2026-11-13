"""
rendu_lot9.py — Les deux décorations de fête du lot 9 (V7), rendues dans Blender, vues de face.

    art/ui/guirlande_fanions.png   1600 × 400 (4:1), affichée 640 × 160 (art bible §17.2.6)
    art/ui/ruban_palier.png        1200 × 400 (3:1), 9-slice, bords de 120 px (§17.2.6)

Lancer (Blender n'a pas besoin d'être ouvert) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/rendu_lot9.py
Seulement une image :  IMAGES="guirlande" ...    Écrire ailleurs (essais) :  SORTIE=<dossier> ...

Les deux icônes du lot (sablier, 4e pack) sont rendues par rendu_icones.py, comme les autres
icônes. Les objets eux-mêmes sont fabriqués dans objets_lot9.py.

COMMENT ÇA MARCHE
  1. On construit la décoration (objets_lot9.py), 1 unité Blender = 100 pixels de l'image.
  2. Caméra ORTHOGRAPHIQUE de face (pas de perspective : la décoration se pose à plat dans
     l'interface), placée du côté −Y et qui regarde vers +Y.
  3. Les trois lumières des icônes et des dalles : principale chaude #FFF1D6 en haut à gauche
     et devant, remplissage violet doux, liseré lavande #B48CFF derrière.
  4. Rendu sur fond TRANSPARENT (transparence exacte : rien à détourer), puis contour d'encre
     #110D1F de 4 px AFFICHÉS (même trait que les dalles), avec la fonction contour() de
     detourer_icones.py. Pas d'ombre portée.
"""

import math
import os
import sys

import bpy
import numpy as np
from mathutils import Vector

DOSSIER_BLENDER = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, DOSSIER_BLENDER)
import commun as C  # noqa: E402
import detourer_icones as D  # noqa: E402
import objets_lot9 as O  # noqa: E402

SORTIE = os.environ.get("SORTIE") or os.path.join(C.DOSSIER_PROJET, "art", "ui")

# nom : (fonction qui construit, largeur, hauteur en px, largeur affichée en px)
IMAGES = {
    "guirlande_fanions": (O.construire_guirlande, 1600, 400, 640),
    "ruban_palier": (O.construire_ruban, 1200, 400, 600),
}
CONTOUR_AFFICHE = 4      # pixels d'encre une fois l'image affichée à sa taille de jeu


def soleil(scene, nom, hexa, energie, vers_lumiere, angle=10.0, ombre=True):
    d = bpy.data.lights.new(nom, "SUN")
    d.color = C.hex_vers_lineaire(hexa)[:3]
    d.energy = energie
    d.angle = math.radians(angle)
    d.use_shadow = ombre
    o = bpy.data.objects.new(nom, d)
    o.rotation_euler = (-Vector(vers_lumiere)).normalized().to_track_quat("-Z", "Y").to_euler()
    scene.collection.objects.link(o)


def preparer(scene, largeur, hauteur):
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    ee = scene.eevee
    ee.taa_render_samples = 64
    for attr, val in (("use_shadows", True), ("use_raytracing", False), ("use_gtao", True)):
        if hasattr(ee, attr):
            setattr(ee, attr, val)
    scene.render.resolution_x, scene.render.resolution_y = largeur, hauteur
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    monde = bpy.data.worlds.new("MondeLot9")
    monde.use_nodes = True
    monde.node_tree.nodes["Background"].inputs["Color"].default_value = C.hex_vers_lineaire("#D8D0F0")
    monde.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.35
    scene.world = monde
    # caméra de face : 1 unité = 100 px, donc l'image fait largeur/100 unités de large
    cd = bpy.data.cameras.new("CameraFace")
    cd.type = "ORTHO"
    cd.ortho_scale = largeur / 100.0
    cd.sensor_fit = "HORIZONTAL"
    cam = bpy.data.objects.new("CameraFace", cd)
    cam.location = (0, -20, 0)
    cam.rotation_euler = (math.radians(90), 0, 0)
    scene.collection.objects.link(cam)
    scene.camera = cam
    # lumières (vecteur = de l'objet VERS la lampe ; la caméra est du côté −Y)
    soleil(scene, "Principale", "#FFF1D6", 3.2, (-0.55, -0.8, 0.75), angle=12)
    soleil(scene, "Remplissage", "#B8A8E8", 0.7, (0.75, -0.6, 0.2), angle=30, ombre=False)
    soleil(scene, "Liseré", "#B48CFF", 1.4, (0.3, 1.0, 0.45), angle=15, ombre=False)


def rendre(nom):
    fabrique, largeur, hauteur, affiche = IMAGES[nom]
    racine = fabrique()
    scene = bpy.context.scene
    preparer(scene, largeur, hauteur)
    tris = sum(C.triangles(o) for o in C.descendants(racine))
    os.makedirs(SORTIE, exist_ok=True)
    brut = os.path.join(SORTIE, f"_brut_{nom}.png")
    scene.render.filepath = brut
    bpy.ops.render.render(write_still=True)
    px = D.lire(brut)
    os.remove(brut)
    couleur, alpha = px[:, :, :3], px[:, :, 3]
    # contrôle : rien ne doit toucher les bords (sinon le contour serait coupé)
    marge = int(round(CONTOUR_AFFICHE * largeur / affiche)) + 2
    bords = max(alpha[:marge].max(), alpha[-marge:].max(), alpha[:, :marge].max(), alpha[:, -marge:].max())
    epaisseur = int(round(CONTOUR_AFFICHE * largeur / affiche))
    couleur, alpha = D.contour(couleur, alpha, epaisseur)
    D.ecrire(os.path.join(SORTIE, f"{nom}.png"), np.concatenate([couleur, alpha[:, :, None]], 2))
    print(f"IMAGE {nom} : {largeur} x {hauteur}, contour {epaisseur} px, triangles={tris}, "
          f"bords libres={bords < 0.01} -> {os.path.join(SORTIE, nom + '.png')}")


def principal():
    choix = os.environ.get("IMAGES", "").split() or list(IMAGES)
    for nom in choix:
        nom = nom if nom in IMAGES else next(n for n in IMAGES if n.startswith(nom))
        rendre(nom)


if __name__ == "__main__":
    principal()
