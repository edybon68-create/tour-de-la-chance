"""
planche_coffres.py — Planche de contrôle : les 5 raretés de coffre côte à côte, vues par la
caméra du jeu (Bois, Bronze, Argent, Or, Diamant, de gauche à droite).

Lancer (Blender fermé), APRÈS les scripts coffre_<rarete>.py :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/planche_coffres.py

Ne fabrique aucun modèle : il RELIT les FBX déjà exportés (coffre.fbx dans le projet Unity,
en lecture seule, et les 4 variantes dans blender/exports/) et les pose aux positions de
l'étage à 5 coffres de la spec v3 §5.4 (x = −3,6 / −1,8 / 0 / +1,8 / +3,6).
Images produites :
    docs/captures/blender_coffres_planche.png          couvercles fermés
    docs/captures/blender_coffres_planche_ouverts.png  couvercles ouverts à 110° (contrôle
                                                       des charnières : ils doivent tous
                                                       tourner pareil, sans « flotter »)
Il affiche aussi, pour chaque coffre, la position de la Charniere et les bornes du Corps :
elles doivent être identiques pour les 5.
"""

import math
import os
import sys

import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

COFFRES = [
    ("bois", os.path.join(C.DOSSIER_MODELES, "coffre.fbx")),
    ("bronze", os.path.join(C.DOSSIER_EXPORTS, "coffre_bronze.fbx")),
    ("argent", os.path.join(C.DOSSIER_EXPORTS, "coffre_argent.fbx")),
    ("or", os.path.join(C.DOSSIER_EXPORTS, "coffre_or.fbx")),
    ("diamant", os.path.join(C.DOSSIER_EXPORTS, "coffre_diamant.fbx")),
]
POSITIONS_X = (-3.6, -1.8, 0.0, 1.8, 3.6)   # Unity, étage à 5 coffres (spec v3 §5.4)


def nom_de_base(o):
    """« Charniere.003 » -> « Charniere » (Blender numérote les doublons à l'import)."""
    return o.name.split(".")[0]


def rendre(objets, nom_fichier, largeur=1600, hauteur=560):
    """Comme commun.rendre_camera_jeu(), mais en image large : caméra du jeu en
    (0 ; 5 ; −7), elle vise le centre de la rangée et resserre son champ pour tout voir."""
    scene = bpy.context.scene
    bpy.context.view_layer.update()
    pts = [o.matrix_world @ Vector(c) for o in objets if o.type == "MESH" for c in o.bound_box]
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    cible = (mn + mx) / 2
    C.monde_nuit(scene, ambiance=0.8)
    cam_data = bpy.data.cameras.new("CameraJeu")
    cam = bpy.data.objects.new("CameraJeu", cam_data)
    scene.collection.objects.link(cam)
    cam.location = C.depuis_unity(*C.CAMERA_JEU_POSITION)
    cam.rotation_euler = (cible - cam.location).normalized().to_track_quat("-Z", "Y").to_euler()
    bpy.context.view_layer.update()
    inv = cam.matrix_world.inverted()
    rx = max(abs(q.x / -q.z) for q in (inv @ p for p in pts))
    ry = max(abs(q.y / -q.z) for q in (inv @ p for p in pts))
    # Champ horizontal assez large pour la rangée ET assez haut pour les couvercles ouverts.
    cam_data.sensor_fit = "HORIZONTAL"
    cam_data.angle = 2 * math.atan(max(rx, ry * largeur / hauteur) * 1.06)
    scene.camera = cam
    soleil, cj = C.lumieres_jeu(scene)
    C.regler_rendu(scene, largeur, hauteur)
    chemin = os.path.join(C.DOSSIER_CAPTURES, nom_fichier)
    scene.render.filepath = chemin
    bpy.ops.render.render(write_still=True)
    for o in (cam, soleil, cj):
        bpy.data.objects.remove(o, do_unlink=True)
    print("Planche :", chemin)


def main():
    C.nouvelle_scene()
    tous, charnieres = [], []
    print("\n=== Contrôle du gabarit (coordonnées Unity, relatives au pied de chaque coffre) ===")
    for (rarete, chemin), x in zip(COFFRES, POSITIONS_X):
        objs = C.importer_fbx(chemin, (x, 0, 0))
        bpy.context.view_layer.update()
        tous += objs
        for o in objs:
            base = nom_de_base(o)
            if base == "Charniere":
                charnieres.append(o)
                p = o.matrix_world.translation
                print(f"  {rarete:<8} Charniere ({-p.x - x:+.3f}; {p.z:+.3f}; {-p.y:+.3f})", end="")
            if base == "Corps":
                b = [o.matrix_world @ Vector(c) for c in o.bound_box]
                print(f"  Corps x[{min(-v.x for v in b) - x:+.3f};{max(-v.x for v in b) - x:+.3f}]"
                      f" y[{min(v.z for v in b):+.3f};{max(v.z for v in b):+.3f}]"
                      f" z[{min(-v.y for v in b):+.3f};{max(-v.y for v in b):+.3f}]", end="")
        print("  objets :", sorted(nom_de_base(o) for o in objs))
    rendre(tous, "blender_coffres_planche.png")

    # Ouverture à 110° autour de l'axe X de chaque charnière, dans le repère du monde
    # (les objets importés portent une rotation de conversion d'axes : on ne touche pas
    # à leurs angles, on tourne leur matrice autour du point de la charnière).
    for ch in charnieres:
        p = ch.matrix_world.translation.copy()
        ch.matrix_world = (Matrix.Translation(p) @ Matrix.Rotation(math.radians(110), 4, "X")
                           @ Matrix.Translation(-p) @ ch.matrix_world)
    rendre(tous, "blender_coffres_planche_ouverts.png")


if __name__ == "__main__":
    main()
