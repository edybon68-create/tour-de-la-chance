"""
diamant.py — Le diamant 3D définitif (la monnaie « diamant », spec v5 §17.1 et §10.5).

Il remplace le diamant fabriqué par le code (`DiamondBurst.BuildDiamondMesh`) SANS aucun
réglage : mêmes mesures, même pivot. Le code pose `localScale = 0,2` sur chaque diamant ;
le modèle est donc fait à l'échelle « 1 = rayon de la ceinture », exactement comme le maillage
du code :
    table (le dessus plat)  : rayon 0,50 à la hauteur +0,35
    ceinture (le plus large): rayon 1,00 à la hauteur +0,12
    pointe                   : hauteur −0,75
    pivot (0, 0, 0) entre les deux, axe vertical = axe de rotation du diamant.
Dans le jeu, il fait donc 0,4 m de large après l'échelle 0,2 du code.

Style : comme le pictogramme diamant de l'interface (`art/icones_detourees/diamant.png`) :
une couronne claire et brillante, une ceinture fine, un pavillon plus foncé qui finit en
pointe. 8 côtés, facettes nettes (ombrage plat), ≤ 150 triangles (spec : 64 ici).
Couleurs de la spec : bleu `#4FC3FF`, facettes `#B8F0FF`, petit reflet rose `#FF8AD8`,
smoothness 0,95 (rugosité 0,05).

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/diamant.py
Sorties :
    TourDeLaChance-Unity/Assets/Models/diamant.glb   (demandé)
    TourDeLaChance-Unity/Assets/Models/diamant.fbx   (le format que Unity lit sans paquet :
                                                      le projet n'a pas d'importeur glTF)
    docs/captures/blender_diamant.png                (miniature de contrôle)
"""

import math
import os
import sys

import bmesh
import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

COTES = 8
TABLE_R, TABLE_Z = 0.5, 0.35
CEINTURE_R, CEINTURE_Z = 1.0, 0.12
CEINTURE_BAS_Z = 0.05           # la ceinture a une petite épaisseur (bande verticale)
PAVILLON_R, PAVILLON_Z = 0.62, -0.28   # anneau intermédiaire du pavillon (facettes en étoile)
POINTE_Z = -0.75


def matiere(nom, hexa, emission=0.0):
    """Matière brillante (rugosité 0,05 = smoothness 0,95) avec un éclat propre : sans lui,
    les facettes du dessous (tournées vers le sol) ne reflètent que la nuit et sortent noires."""
    m = bpy.data.materials.new(nom)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = C.hex_vers_lineaire(hexa)
    b.inputs["Roughness"].default_value = 0.05
    b.inputs["Metallic"].default_value = 0.1
    b.inputs["Coat Weight"].default_value = 0.6
    if emission:
        b.inputs["Emission Color"].default_value = C.hex_vers_lineaire(hexa)
        b.inputs["Emission Strength"].default_value = emission
    m.diffuse_color = C.hex_vers_lineaire(hexa)
    return m


def anneau(bm, r, z, decalage=0.0):
    return [bm.verts.new((r * math.cos((k + decalage) * 2 * math.pi / COTES),
                          r * math.sin((k + decalage) * 2 * math.pi / COTES), z))
            for k in range(COTES)]


def construire():
    C.nouvelle_scene()
    mats = [
        matiere("M_DiamantTable", "#B8F0FF", 0.25),   # 0 : la table, très claire
        matiere("M_DiamantClair", "#8ADCFF", 0.2),    # 1 : couronne, une facette sur deux
        matiere("M_Diamant", "#4FC3FF", 0.45),         # 2 : bleu de la monnaie
        matiere("M_DiamantSombre", "#2F8FD8", 0.6),  # 3 : pavillon (le bas, plus foncé)
        matiere("M_DiamantRose", "#FF8AD8", 0.45),     # 4 : le petit reflet rose
    ]
    bm = bmesh.new()
    table = anneau(bm, TABLE_R, TABLE_Z)
    # La ceinture est tournée d'un demi-pas : la couronne se découpe en triangles alternés
    # (une facette « étoile » pointe en haut, la suivante en bas), comme une vraie taille.
    haut = anneau(bm, CEINTURE_R, CEINTURE_Z, 0.5)
    bas = anneau(bm, CEINTURE_R, CEINTURE_BAS_Z, 0.5)
    pav = anneau(bm, PAVILLON_R, PAVILLON_Z, 0.0)
    pointe = bm.verts.new((0, 0, POINTE_Z))

    def face(vs, m):
        f = bm.faces.new(vs)
        f.material_index = m
        return f

    face(table, 0)                                            # la table (octogone)
    for k in range(COTES):
        k1 = (k + 1) % COTES
        # couronne : triangle « pointe en haut » puis « pointe en bas »
        face((table[k], haut[k], table[k1]), 1 if k % 2 == 0 else 2)
        face((table[k], haut[(k - 1) % COTES], haut[k]), 2 if k % 2 == 0 else 1)
        # ceinture : petite bande ; une seule facette rose (le reflet)
        face((haut[k], bas[k], bas[k1], haut[k1]), 4 if k == 1 else 2)
        # pavillon : facettes en étoile vers l'anneau intermédiaire, puis vers la pointe
        face((pav[k], bas[k], pav[k1]), 2)
        face((bas[(k - 1) % COTES], pav[k], bas[k]), 3)
        face((pav[k], pointe, pav[k1]), 3 if k % 2 == 0 else 2)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new("Diamant")
    bm.to_mesh(me)
    bm.free()
    for m in mats:
        me.materials.append(m)
    for p in me.polygons:
        p.use_smooth = False                                  # facettes nettes
    corps = bpy.data.objects.new("Diamant", me)
    bpy.context.scene.collection.objects.link(corps)
    racine = C.vide("DiamantMonnaie")
    C.parenter(corps, racine)
    return racine


def exporter_glb(racine, chemin):
    """Export glTF binaire : Y vers le haut (export_yup), échelle 1, matières incluses."""
    C.selectionner(C.descendants(racine), racine)
    bpy.ops.export_scene.gltf(filepath=chemin, export_format="GLB", use_selection=True,
                              export_yup=True, export_apply=True, export_cameras=False,
                              export_lights=False)
    print("Exporté :", chemin)


if __name__ == "__main__":
    r = construire()
    total = C.rapport(r)
    assert total <= 150, total
    # vue presque de côté (14° au-dessus) : de plus haut, le pavillon est caché par la ceinture
    C.rendre_miniature(r, "blender_diamant.png", direction=(0.35, 1.0, 0.26))
    exporter_glb(r, os.path.join(C.DOSSIER_MODELES, "diamant.glb"))
    C.exporter_fbx(r, "diamant.fbx")
