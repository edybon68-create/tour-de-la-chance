"""
armoire_skins.py — L'armoire jouet de l'icône « Mes skins » (v6.5, art/prompts/lot8.md).

Ce n'est pas un objet du jeu : ce modèle sert seulement à fabriquer l'icône
`art/icones/icone_mes_skins.png` par rendu Blender (`rendu_icones.py`), quand on ne passe
pas par Gemini. Une petite armoire en bois au toit arrondi, portes grandes ouvertes, où
pendent trois capes de couleurs franches (rose, jaune citron, turquoise) : « mes tenues
rangées ». Bois chaud et capes vives : l'icône ne se perd ni sur le beige ni sur le bleu
du menu de l'Inventaire.

Lancer seul (contrôle du modèle, sans rendu) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/armoire_skins.py
Fabriquer l'icône :
    ICONES="icone_mes_skins" /Applications/Blender.app/.../Blender --background --python blender/rendu_icones.py

Convention du projet : l'avant de l'objet regarde vers +Y, le haut vers +Z, 1 unité = 1 m.
"""

import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

# Mesures (mètres)
LARGEUR = 0.62        # largeur du corps
PROFONDEUR = 0.32
HAUTEUR = 0.62        # hauteur des côtés (sans le toit arrondi)
PLANCHE = 0.04        # épaisseur du bois
PIEDS = 0.06          # hauteur des pieds
BOMBE = 0.13          # hauteur de l'arrondi du toit
OUVERTURE = 105       # angle d'ouverture des portes (degrés)

_mats = {}


def mat(hexa, rugosite=0.4, metal=0.0, vernis=0.5):
    """Matière « jouet verni » : couleur unie, vernis léger (Coat) qui fait briller les arrondis."""
    cle = (hexa, rugosite, metal, vernis)
    if cle not in _mats:
        m = bpy.data.materials.new(f"M_Armoire_{hexa.lstrip('#')}")
        m.use_nodes = True
        b = m.node_tree.nodes["Principled BSDF"]
        b.inputs["Base Color"].default_value = C.hex_vers_lineaire(hexa)
        b.inputs["Roughness"].default_value = rugosite
        b.inputs["Metallic"].default_value = metal
        b.inputs["Coat Weight"].default_value = vernis
        b.inputs["Coat Roughness"].default_value = 0.25
        m.diffuse_color = C.hex_vers_lineaire(hexa)
        _mats[cle] = m
    return _mats[cle]


def objet(nom, bm, mats, racine, biseau=0.0, seg=2, lisse=True, mat_biseau=None):
    me = bpy.data.meshes.new(nom)
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new(nom, me)
    bpy.context.scene.collection.objects.link(o)
    for m in mats:
        me.materials.append(m)
    if lisse:
        me.shade_smooth()
        me.set_sharp_from_angle(angle=math.radians(40))
    if biseau:
        b = o.modifiers.new("Biseau", "BEVEL")
        b.width, b.segments = biseau, seg
        b.limit_method = "ANGLE"
        b.angle_limit = math.radians(30)
        b.use_clamp_overlap = True
        b.harden_normals = False
        if mat_biseau is not None:
            me.materials.append(mat_biseau)
            b.material = len(me.materials) - 1
    o.parent = racine
    return o


def bm_boite(x0, x1, y0, y1, z0, z1):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co.x = x0 if v.co.x < 0 else x1
        v.co.y = y0 if v.co.y < 0 else y1
        v.co.z = z0 if v.co.z < 0 else z1
    return bm


def bm_prisme_xz(contour, y0, y1):
    """Plaque découpée selon un contour dans le plan XZ, épaisse de y0 à y1."""
    bm = bmesh.new()
    avant = [bm.verts.new((x, y0, z)) for x, z in contour]
    f = bm.faces.new(avant)
    ext = bmesh.ops.extrude_face_region(bm, geom=[f])
    nv = [g for g in ext["geom"] if isinstance(g, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, verts=nv, vec=(0, y1 - y0, 0))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm


def construire():
    C.nouvelle_scene()
    _mats.clear()
    racine = bpy.data.objects.new("ArmoireMesSkins", None)
    bpy.context.scene.collection.objects.link(racine)

    # Bois peu verni : un reflet trop clair sur le toit prendrait la couleur lavande du fond,
    # et le détourage le ferait disparaître (trou dans le toit).
    bois, bois_fonce = mat("#8C5933", 0.6, vernis=0.12), mat("#6B4226", 0.6, vernis=0.12)
    creme, violet = mat("#FFF4DC", 0.45), mat("#3A2850", 0.6)
    laiton = mat("#C99A1E", 0.3, 1.0)
    W, D, H, t = LARGEUR / 2, PROFONDEUR / 2, HAUTEUR, PLANCHE
    z0, z1 = PIEDS, PIEDS + H

    # --- Le corps : fond, côtés, plancher (les arêtes biseautées sont plus foncées) ---
    objet("Fond", bm_boite(-W, W, -D, -D + t, z0, z1), [bois], racine, 0.012, mat_biseau=bois_fonce)
    for s, n in ((-1, "G"), (1, "D")):
        x0, x1 = sorted((s * W, s * (W - t)))
        objet(f"Cote_{n}", bm_boite(x0, x1, -D, D, z0, z1), [bois], racine, 0.012, mat_biseau=bois_fonce)
    objet("Plancher", bm_boite(-W, W, -D, D, z0, z0 + t), [bois], racine, 0.012, mat_biseau=bois_fonce)
    # Le fond intérieur violet : les capes ressortent dessus
    objet("Interieur", bm_boite(-W + t, W - t, -D + t, -D + t + 0.006, z0 + t, z1), [violet], racine)

    # --- Le toit arrondi (une arche pleine sur toute la profondeur) ---
    n = 16
    contour = [(-W - 0.015, z1 - t), (W + 0.015, z1 - t)]
    for i in range(n + 1):
        a = i / n * math.pi
        contour.append(((W + 0.015) * math.cos(a), z1 + BOMBE * math.sin(a)))
    objet("Toit", bm_prisme_xz(contour, -D - 0.01, D + 0.015), [bois], racine, 0.016,
          mat_biseau=bois_fonce)

    # --- Les pieds ronds ---
    for sx in (-1, 1):
        for sy in (-1, 1):
            bm = bmesh.new()
            bmesh.ops.create_uvsphere(bm, u_segments=12, v_segments=8, radius=0.04)
            bmesh.ops.scale(bm, vec=(1, 1, 0.9), verts=bm.verts)
            bmesh.ops.translate(bm, vec=(sx * (W - 0.06), sy * (D - 0.06), 0.036), verts=bm.verts)
            objet(f"Pied_{sx}_{sy}", bm, [bois_fonce], racine)

    # --- La tringle de laiton ---
    z_tringle = z1 - 0.07
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=12, radius1=0.014, radius2=0.014,
                          depth=2 * (W - t))
    bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0), matrix=Matrix.Rotation(math.pi / 2, 3, "Y"))
    bmesh.ops.translate(bm, vec=(0, -0.01, z_tringle), verts=bm.verts)
    objet("Tringle", bm, [laiton], racine)

    # --- Les trois capes sur leurs cintres ---
    # (vue de face, de gauche à droite : rose, jaune, turquoise ; +X est à GAUCHE vu de l'avant)
    # (la cape du milieu est un peu en retrait : les capes ne se traversent jamais)
    capes = (("Rose", "#FF6FB5", 0.18, -2, 0.0), ("Jaune", "#F2D03B", 0.0, 0, -0.04),
             ("Turquoise", "#2BB8C4", -0.18, 2, 0.0))
    cols = {"Rose": "#E0559C", "Jaune": "#D9B526", "Turquoise": "#1F9AA6"}
    cintre_bois = mat("#A8703F", 0.45)
    for nom, couleur, x, angle, dy in capes:
        m = mat(couleur, 0.45)
        place = (Matrix.Translation((x, -0.005 + dy, z_tringle)) @ Matrix.Rotation(math.radians(angle), 4, "Y"))
        # cintre : crochet de laiton sur la tringle + triangle de bois épais
        bm = bmesh.new()
        bmesh.ops.create_cone(bm, cap_ends=True, segments=8, radius1=0.008, radius2=0.008, depth=0.045)
        bmesh.ops.translate(bm, vec=(0, 0, -0.02), verts=bm.verts)
        bmesh.ops.transform(bm, matrix=place, verts=bm.verts)
        objet(f"Crochet_{nom}", bm, [laiton], racine)
        tri = [(-0.075, -0.085), (0.075, -0.085), (0.0, -0.04)]
        bm = bm_prisme_xz(tri, -0.012, 0.012)
        bmesh.ops.transform(bm, matrix=place, verts=bm.verts)
        objet(f"Cintre_{nom}", bm, [cintre_bois], racine, 0.01, seg=2)
        # cape : cou étroit, épaules arrondies, bas évasé et ondulé
        pts = [(-0.028, -0.07), (0.028, -0.07)]
        for i in range(1, 6):                       # épaule droite
            a_ = i / 5 * math.pi / 2
            pts.append((0.028 + 0.03 * math.sin(a_), -0.07 - 0.035 * (1 - math.cos(a_))))
        pts.append((0.08, -0.36))
        for i in range(1, 6):                       # coins du bas arrondis, bas droit
            a_ = i / 6 * math.pi / 2
            pts.append((0.08 - 0.03 * (1 - math.cos(a_)), -0.36 - 0.03 * math.sin(a_)))
        for i in range(0, 6):
            a_ = (5 - i) / 6 * math.pi / 2
            pts.append((-0.08 + 0.03 * (1 - math.cos(a_)), -0.36 - 0.03 * math.sin(a_)))
        pts.append((-0.08, -0.36))
        for i in range(5, 0, -1):                   # épaule gauche
            a_ = i / 5 * math.pi / 2
            pts.append((-0.028 - 0.03 * math.sin(a_), -0.07 - 0.035 * (1 - math.cos(a_))))
        bm = bm_prisme_xz(pts, -0.024, 0.024)
        bmesh.ops.transform(bm, matrix=place, verts=bm.verts)
        objet(f"Cape_{nom}", bm, [m], racine, 0.016, seg=3)
        # col montant, un ton plus foncé (pas de petits ronds : on lirait des yeux)
        bm = bm_prisme_xz([(-0.045, -0.06), (0.045, -0.06), (0.04, -0.09), (-0.04, -0.09)], -0.03, 0.03)
        bmesh.ops.transform(bm, matrix=place, verts=bm.verts)
        objet(f"Col_{nom}", bm, [mat(cols[nom], 0.45)], racine, 0.012, seg=2)

    # --- Les portes, grandes ouvertes (charnière à l'avant de chaque côté) ---
    ep = 0.03
    for s, n in ((-1, "G"), (1, "D")):
        # porte fermée = de la charnière (x = s·W) vers le centre ; on la construit à plat,
        # charnière à l'origine, puis on la tourne autour de la charnière.
        larg = W
        piece = []
        bm = bm_boite(0, larg, -ep, 0, z0, z1)        # face extérieure en y = 0, intérieur en −ep
        piece.append(("Porte", bm, [bois], 0.012))
        bm2 = bm_boite(0.045, larg - 0.045, -ep - 0.008, -ep + 0.002, z0 + 0.05, z1 - 0.05)
        piece.append(("Panneau", bm2, [creme], 0.008))
        bm3 = bmesh.new()
        bmesh.ops.create_uvsphere(bm3, u_segments=12, v_segments=8, radius=0.022)
        bmesh.ops.translate(bm3, vec=(larg - 0.05, -ep - 0.024, (z0 + z1) / 2), verts=bm3.verts)
        piece.append(("Bouton", bm3, [laiton], 0.0))
        # porte gauche : construite vers +X puis tournée de +OUVERTURE ;
        # porte droite : construite en miroir (vers −X) puis tournée de −OUVERTURE.
        mir = Matrix.Scale(-1, 4, (1, 0, 0)) if s > 0 else Matrix()
        # la porte de droite (+X, à gauche de l'image) s'ouvre plus : sinon on la voit de chant
        angle_porte = OUVERTURE + (35 if s > 0 else -5)
        tour = Matrix.Rotation(math.radians(angle_porte) * (-s), 4, "Z")
        place = Matrix.Translation((s * W, D + 0.002, 0)) @ tour @ mir
        for nom_p, b, ms, bis in piece:
            bmesh.ops.transform(b, matrix=place, verts=b.verts)
            if s > 0:
                bmesh.ops.reverse_faces(b, faces=b.faces)    # le miroir retourne les faces
            objet(f"{nom_p}_{n}", b, ms, racine, bis, mat_biseau=bois_fonce if nom_p == "Porte" else None)
    bpy.context.view_layer.update()
    return racine


if __name__ == "__main__":
    r = construire()
    print("ARMOIRE triangles =", sum(C.triangles(o) for o in C.descendants(r)))
