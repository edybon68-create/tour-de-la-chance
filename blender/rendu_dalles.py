"""
rendu_dalles.py — Les 12 dalles de L'Ascension (v6.5, art bible §16.3), rendues dans Blender.

Une dalle = L'ÉTAGE DU JEU vu de 3/4 (le disque de pierre de la tour, ses dalles en rayons,
sa margelle claire, l'étoile violette, ses coffres), entouré du ciel du palier d'altitude
(Palier1..4 : plein jour, dans les nuages, au-dessus des nuages, étoiles). Noms de fichiers
gardés : herbe = palier 1, pierre = palier 2, nuages = palier 3, galaxie = palier 4
(plages d'étages : GameManager.TierForFloor). L'interface pose ensuite par-dessus
les pièces, les diamants, le drapeau du record et le cheval : ils ne sont PAS dans les images.

Lancer (Blender n'a pas besoin d'être ouvert) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/rendu_dalles.py
Seulement quelques dalles (le nom du fichier sans « dalle_ » ni « .png ») :
    DALLES="herbe_a pierre_palier" /Applications/Blender.app/.../Blender --background --python ...
Sans refaire la planche de contrôle : PLANCHE=0

Sorties :
    art/dalles/dalle_<decor>_<variante>.png   (12 images, fond transparent, contour et ombre)
    docs/captures/v65_dalles.png              (planche : les dalles empilées sur leur fond,
                                               avec pièces, diamants et drapeau posés)
La copie vers TourDeLaChance-Unity/Assets/UI/Dalles/ se fait à la main (ou par le chef de
projet), comme pour les bulles.

COMMENT ÇA MARCHE (une fonction : rendre_dalle(decor, variante))
  1. Unités : 1 unité Blender = 1 mètre. Le DESSUS de la plaque est à z = 0, son centre à
     l'origine ; l'avant de la dalle regarde vers −Y (vers la caméra).
  2. L'étage = un disque de Ø 2 m (2,6 m pour un palier), 18 cm d'épaisseur, qui touche les
     mêmes pixels que l'ancienne plaque carrée : les points d'ancrage ne changent pas.
  3. Rien n'est posé sur le sol (sauf les coffres du fond et l'estrade du palier) : les
     « places libres » du §16.3.4 restent vides. Le décor d'altitude (nuages, étoiles,
     planètes) est dans les coins de l'image ; `caser()` le ramène dedans s'il dépasse.
     (Les fonctions de la v1 — plaque(), lanterne(), cristal()… — restent disponibles.)
  4. Caméra orthographique (pas de perspective : toutes les dalles ont exactement la même
     largeur à l'écran et s'empilent pile), inclinée de 28° au-dessus de l'horizon, à
     520 pixels d'image par mètre. Le script VÉRIFIE que les 4 coins du dessus tombent sur
     les pixels prévus par la bible (± 2 px), sinon il s'arrête.
  5. Lumière des icônes et des bulles : soleil chaud #FFF1D6 en haut à gauche et un peu
     devant, remplissage violet doux, liseré lavande #B48CFF derrière. Des « soleils »
     (lumière à l'infini) : chaque dalle est éclairée exactement pareil, où qu'elle soit.
  6. Après le rendu (numpy) : contour d'encre #110D1F de 8 px d'image autour de la
     silhouette, puis ombre portée (silhouette décalée de 20 px vers le bas, floutée, encre
     à 35 %). Même travail que le compositeur de Blender, mais au pixel près.
"""

import math
import os
import random
import sys

import bmesh
import bpy
import numpy as np
from bpy_extras.object_utils import world_to_camera_view
from mathutils import Matrix, Vector

DOSSIER_BLENDER = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, DOSSIER_BLENDER)
import commun as C  # noqa: E402

DOSSIER_DALLES = os.environ.get("SORTIE") or os.path.join(C.DOSSIER_PROJET, "art", "dalles")
DOSSIER_ICONES = os.path.join(C.DOSSIER_PROJET, "art", "icones_detourees")
PLANCHE = os.path.join(C.DOSSIER_CAPTURES, "v65_dalles.png")
ECHELLE = 2              # l'image est rendue à 2 fois sa taille d'affichage
PX_PAR_M = 260           # pixels AFFICHÉS par mètre (520 dans l'image rendue)
ENCRE = "#110D1F"
INCLINAISON = 28.0       # degrés au-dessus de l'horizon
EPAISSEUR = 0.18         # épaisseur totale de la plaque (m)
LEVRE = 0.04             # débord du dessus sur le corps (m)
H_DESSUS = 0.075         # épaisseur de la couche « dessus » (herbe, dallage…)
DEMI_CHEMIN = 0.21       # le chemin va de x = −0,21 à +0,21

# Les mesures de chaque format (bible §16.3.3 et §16.3.6), en pixels AFFICHÉS.
FORMATS = {
    #           taille (m), image affichée, haut du dessus (centre), points de contrôle
    "normale": dict(cote=2.0, image=(560, 340), centre_y=166,
                    coins=((20, 44), (540, 44), (20, 288), (540, 288))),
    "palier": dict(cote=2.6, image=(716, 430), centre_y=210,
                   coins=((20, 52), (696, 52), (20, 369), (696, 369))),
}
CENTRE_PLACE = 0.10      # la place ronde du palier est centrée en y = +0,10 m (§16.3.6)
R_PLACE = 0.70
R_PIEDESTAL = 0.45
H_PIEDESTAL = 0.12


# ---------------------------------------------------------------------------------------
# 1. Matériaux : un par couleur (mis en cache), couleurs unies, aspect « jouet » mat-satiné
# ---------------------------------------------------------------------------------------
_MATS = {}


def M(hexa, rugosite=0.55, metal=0.0, emission=0.0, alpha=1.0):
    """Matériau Principled d'une couleur unie. `emission` > 0 : la matière brille de sa
    propre couleur (lanterne, cristaux, joints lumineux de la galaxie)."""
    cle = (hexa, rugosite, metal, emission, alpha)
    if cle in _MATS:
        return _MATS[cle]
    m = bpy.data.materials.new(f"M_{hexa.lstrip('#')}_{len(_MATS)}")
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = C.hex_vers_lineaire(hexa)
    b.inputs["Roughness"].default_value = rugosite
    b.inputs["Metallic"].default_value = metal
    # un léger vernis : le petit reflet « jouet » sur les arrondis (§3.1)
    b.inputs["Coat Weight"].default_value = 0.15
    b.inputs["Coat Roughness"].default_value = 0.3
    if emission > 0:
        b.inputs["Emission Color"].default_value = C.hex_vers_lineaire(hexa)
        b.inputs["Emission Strength"].default_value = emission
    if alpha < 1.0:
        b.inputs["Alpha"].default_value = alpha
        m.surface_render_method = "BLENDED"
    m.diffuse_color = C.hex_vers_lineaire(hexa)
    _MATS[cle] = m
    return m


def M_queue(hexa):
    """Queue de comète : couleur lumineuse qui devient transparente vers le bout.
    La coordonnée « Generated » Z va de 0 (base du cône = côté tête) à 1 (pointe)."""
    m = bpy.data.materials.new("M_QueueComete")
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    sortie = nt.nodes.new("ShaderNodeOutputMaterial")
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Color"].default_value = C.hex_vers_lineaire(hexa)
    em.inputs["Strength"].default_value = 1.2
    tr = nt.nodes.new("ShaderNodeBsdfTransparent")
    coord = nt.nodes.new("ShaderNodeTexCoord")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    carte = nt.nodes.new("ShaderNodeMapRange")
    carte.inputs["From Min"].default_value = 0.0
    carte.inputs["From Max"].default_value = 1.0
    carte.inputs["To Min"].default_value = 0.9
    carte.inputs["To Max"].default_value = 0.0
    mix = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(coord.outputs["Generated"], sep.inputs[0])
    nt.links.new(sep.outputs["Z"], carte.inputs["Value"])
    nt.links.new(carte.outputs["Result"], mix.inputs[0])
    nt.links.new(tr.outputs[0], mix.inputs[1])
    nt.links.new(em.outputs[0], mix.inputs[2])
    nt.links.new(mix.outputs[0], sortie.inputs["Surface"])
    m.surface_render_method = "BLENDED"
    return m


# ---------------------------------------------------------------------------------------
# 2. Petites briques de géométrie (bmesh) → objets
# ---------------------------------------------------------------------------------------
COLLECTION = None


def objet(nom, bm, mats, lisse=False, angle=40.0, biseau=0.0, seg=2, mat_biseau=None,
          loc=(0, 0, 0), rot=(0, 0, 0), ech=(1, 1, 1)):
    """Transforme un bmesh en objet de la scène.
    - mats : liste de matériaux (l'indice de chaque face choisit sa couleur) ;
    - lisse : ombrage doux, en gardant vives les arêtes plus marquées que `angle` ;
    - biseau : largeur du biseau « jouet » (modificateur Bevel, non appliqué : rendu seul) ;
    - mat_biseau : matériau des faces du biseau (ex. la lèvre éclairée de l'herbe)."""
    me = bpy.data.meshes.new(nom)
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new(nom, me)
    COLLECTION.objects.link(o)
    for m in mats:
        me.materials.append(m)
    if lisse:
        me.shade_smooth()
        me.set_sharp_from_angle(angle=math.radians(angle))
    if biseau > 0:
        b = o.modifiers.new("Biseau", "BEVEL")
        b.width = biseau
        b.segments = seg
        b.limit_method = "ANGLE"
        b.angle_limit = math.radians(30)
        b.use_clamp_overlap = True
        if mat_biseau is not None:
            me.materials.append(mat_biseau)
            b.material = len(me.materials) - 1
    o.location, o.rotation_euler, o.scale = loc, rot, ech
    return o


def colorer_par_normale(bm, seuil=0.7, haut=0, cotes=1):
    """Faces tournées vers le haut → matériau `haut`, les autres → `cotes`."""
    for f in bm.faces:
        f.normal_update()
        f.material_index = haut if f.normal.z > seuil else cotes


def bm_boite(x0, x1, y0, y1, z0, z1):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co.x = x0 if v.co.x < 0 else x1
        v.co.y = y0 if v.co.y < 0 else y1
        v.co.z = z0 if v.co.z < 0 else z1
    return bm


def boite(nom, x0, x1, y0, y1, z0, z1, m_haut, m_cotes=None, biseau=0.0, seg=2,
          mat_biseau=None, rot_z=0.0):
    bm = bm_boite(x0, x1, y0, y1, z0, z1)
    mats = [m_haut]
    if m_cotes is not None:
        colorer_par_normale(bm)
        mats.append(m_cotes)
    if rot_z:
        c = Vector(((x0 + x1) / 2, (y0 + y1) / 2, 0))
        bmesh.ops.rotate(bm, verts=bm.verts, cent=c, matrix=Matrix.Rotation(rot_z, 3, "Z"))
    return objet(nom, bm, mats, biseau=biseau, seg=seg, mat_biseau=mat_biseau)


def bm_cylindre(r, h, cotes=16, r_haut=None, z0=0.0):
    """Cylindre (ou tronc de cône si r_haut) posé sur z0, de hauteur h."""
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=cotes,
                          radius1=r, radius2=r if r_haut is None else r_haut, depth=h)
    bmesh.ops.translate(bm, verts=bm.verts, vec=(0, 0, z0 + h / 2))
    return bm


def cylindre(nom, x, y, z0, r, h, m_cotes, m_haut=None, cotes=16, r_haut=None, biseau=0.0,
             seg=2, lisse=True, mat_biseau=None):
    bm = bm_cylindre(r, h, cotes, r_haut, z0)
    mats = [m_cotes]
    if m_haut is not None:
        mats = [m_haut, m_cotes]
        colorer_par_normale(bm, 0.9)
    o = objet(nom, bm, mats, lisse=lisse, biseau=biseau, seg=seg, mat_biseau=mat_biseau)
    o.location = (x, y, 0)
    return o


def sphere(nom, x, y, z, r, m, ech=(1, 1, 1), u=16, v=10):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=u, v_segments=v, radius=r)
    o = objet(nom, bm, [m], lisse=True, angle=180)
    o.location, o.scale = (x, y, z), ech
    return o


def tore(nom, x, y, z, R, r, m, seg=24, seg_tube=8, ech=(1, 1, 1), rot=(0, 0, 0)):
    bm = bmesh.new()
    anneaux = []
    for i in range(seg):
        a = 2 * math.pi * i / seg
        anneau = []
        for j in range(seg_tube):
            b = 2 * math.pi * j / seg_tube
            rr = R + r * math.cos(b)
            anneau.append(bm.verts.new((rr * math.cos(a), rr * math.sin(a), r * math.sin(b))))
        anneaux.append(anneau)
    for i in range(seg):
        a1, a2 = anneaux[i], anneaux[(i + 1) % seg]
        for j in range(seg_tube):
            bm.faces.new((a1[j], a2[j], a2[(j + 1) % seg_tube], a1[(j + 1) % seg_tube]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    o = objet(nom, bm, [m], lisse=True, angle=180)
    o.location, o.scale, o.rotation_euler = (x, y, z), ech, rot
    return o


def prisme(nom, pts, epaisseur, mats, matrice=Matrix(), biseau=0.0, seg=1, lisse=False):
    """Plaque plate découpée selon un contour 2D (x, y), épaisse de `epaisseur` en z,
    puis placée par `matrice`. Sert aux fanions, flèche, étoiles, dalles de la place…"""
    bm = bmesh.new()
    bas = [bm.verts.new((x, y, 0)) for x, y in pts]
    f = bm.faces.new(bas)
    f.normal_update()
    if f.normal.z < 0:
        f.normal_flip()
    ext = bmesh.ops.extrude_face_region(bm, geom=[f])
    nv = [g for g in ext["geom"] if isinstance(g, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, verts=nv, vec=(0, 0, epaisseur))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    if len(mats) > 1:
        colorer_par_normale(bm, 0.9)
    bmesh.ops.transform(bm, matrix=matrice, verts=bm.verts)
    return objet(nom, bm, mats, lisse=lisse, biseau=biseau, seg=seg)


def secteur(r0, r1, a0, a1, n=6):
    """Contour d'une part d'anneau (dalle de la place ronde)."""
    ext = [(r1 * math.cos(a0 + (a1 - a0) * i / n), r1 * math.sin(a0 + (a1 - a0) * i / n))
           for i in range(n + 1)]
    inte = [(r0 * math.cos(a1 - (a1 - a0) * i / n), r0 * math.sin(a1 - (a1 - a0) * i / n))
            for i in range(n + 1)]
    return ext + inte


def etoile_2d(r, creux=0.45, branches=5):
    pts = []
    for i in range(branches * 2):
        a = math.pi / 2 + math.pi * i / branches
        rr = r if i % 2 == 0 else r * creux
        pts.append((rr * math.cos(a), rr * math.sin(a)))
    return pts


def face_camera(x, y, z, angle_z=0.0):
    """Matrice qui dresse une plaque (plan XY) face à la caméra (plan XZ, incliné de 28°)."""
    return (Matrix.Translation((x, y, z)) @ Matrix.Rotation(math.radians(90 - INCLINAISON), 4, "X")
            @ Matrix.Rotation(angle_z, 4, "Z"))


def tube(nom, a, b, r, m, cotes=6):
    """Petit cylindre d'un point a à un point b (cordes, poteaux penchés, queue…)."""
    a, b = Vector(a), Vector(b)
    d = b - a
    bm = bm_cylindre(r, d.length, cotes, z0=0)
    o = objet(nom, bm, [m], lisse=True, angle=60)
    o.location = a
    o.rotation_euler = d.to_track_quat("Z", "Y").to_euler()
    return o


# ---------------------------------------------------------------------------------------
# 3. Petits décors réutilisables
# ---------------------------------------------------------------------------------------
def touffe(x, y, m, h=0.03):
    """Touffe d'herbe : 3 petites pointes (≤ 3 cm : autorisées même sur les places libres)."""
    for i, (dx, dy, hh) in enumerate(((0, 0, 1.0), (0.018, 0.01, 0.75), (-0.017, 0.008, 0.8))):
        o = cylindre(f"Touffe_{x:.2f}_{i}", x + dx, y + dy, 0, 0.012, h * hh, m, cotes=5,
                     r_haut=0.001, lisse=False)


def fleur(x, y, couleur, h=0.07, coeur="#FFD54A"):
    tube(f"Tige_{x:.2f}_{y:.2f}", (x, y, 0), (x, y, h), 0.007, M("#4E9E3A"))
    for i in range(5):
        a = 2 * math.pi * i / 5
        sphere(f"Petale_{x:.2f}_{y:.2f}_{i}", x + 0.024 * math.cos(a), y + 0.024 * math.sin(a) * 0.5,
               h + 0.024 * math.sin(a) * 0.85, 0.019, M(couleur), u=8, v=6)
    sphere(f"Coeur_{x:.2f}_{y:.2f}", x, y - 0.01, h, 0.016, M(coeur), u=8, v=6)


def caillou(x, y, r, m, ech=(1.0, 0.85, 0.55)):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=1, radius=r)
    o = objet(f"Caillou_{x:.2f}_{y:.2f}", bm, [m], lisse=True, angle=60)
    o.location, o.scale = (x, y, r * ech[2] * 0.3), ech
    return o


def nuage(nom, x, y, z, taille, m_haut, m_bas=None, boules=None):
    """Petit nuage = grappe de boules (la plus grosse au milieu)."""
    boules = boules or ((0, 0, 0, 1.0), (-0.75, 0.05, -0.15, 0.72), (0.75, -0.03, -0.18, 0.68),
                        (0.3, 0.25, 0.1, 0.6))
    for i, (dx, dy, dz, k) in enumerate(boules):
        sphere(f"{nom}_{i}", x + dx * taille, y + dy * taille, z + dz * taille, taille * k,
               m_haut if (m_bas is None or dz > -0.1) else m_bas, ech=(1, 1, 0.85))


def lanterne(nom, x, y, echelle=1.0):
    """Lanterne courte : socle et toit de métal sombre, verre chaud qui brille."""
    k = echelle
    metal = M("#4A4E5A", 0.45, 0.6)
    boite(f"{nom}_Socle", x - 0.075 * k, x + 0.075 * k, y - 0.075 * k, y + 0.075 * k, 0, 0.035 * k,
          metal, biseau=0.008)
    boite(f"{nom}_Verre", x - 0.055 * k, x + 0.055 * k, y - 0.055 * k, y + 0.055 * k, 0.035 * k,
          0.17 * k, M("#FFE2B0", 0.3, emission=0.9))
    for sx in (-1, 1):
        for sy in (-1, 1):
            boite(f"{nom}_Montant_{sx}_{sy}", x + sx * 0.06 * k - 0.012 * k, x + sx * 0.06 * k + 0.012 * k,
                  y + sy * 0.06 * k - 0.012 * k, y + sy * 0.06 * k + 0.012 * k, 0.035 * k, 0.17 * k, metal)
    toit = cylindre(f"{nom}_Toit", x, y, 0.17 * k, 0.1 * k, 0.07 * k, metal, cotes=4, r_haut=0.015 * k,
                    lisse=False, biseau=0.006)
    toit.rotation_euler.z = math.radians(45)
    tore(f"{nom}_Anneau", x, y, 0.26 * k, 0.022 * k, 0.007 * k, metal, ech=(1, 1, 1),
         rot=(math.radians(90), 0, 0))


def montgolfiere(nom, x, y, z_ballon, r, couleurs, z_nacelle=0.0, cotes=12):
    """Ballon à côtes de 2 couleurs (fuseaux alternés), nacelle en osier et 4 cordes."""
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=cotes, v_segments=10, radius=r)
    for v in bm.verts:
        if v.co.z < 0:                     # le bas se resserre en poire
            k = 1 + 0.55 * v.co.z / r
            v.co.x *= k
            v.co.y *= k
        v.co.z *= 1.12
    for f in bm.faces:
        c = f.calc_center_median()
        a = (math.atan2(c.y, c.x) + 2 * math.pi) % (2 * math.pi)
        f.material_index = int(a / (2 * math.pi / cotes)) % 2
    o = objet(f"{nom}_Ballon", bm, [M(couleurs[0], 0.45), M(couleurs[1], 0.45)], lisse=True, angle=180)
    o.location = (x, y, z_ballon)
    o.rotation_euler.z = math.radians(8)
    hn = 0.06 * r / 0.13
    cn = 0.055 * r / 0.13
    boite(f"{nom}_Nacelle", x - cn, x + cn, y - cn, y + cn, z_nacelle, z_nacelle + hn,
          M("#8C5933", 0.7), biseau=0.01 * r / 0.13)
    bas = z_ballon - r * 1.12 * 0.92
    for sx in (-1, 1):
        for sy in (-1, 1):
            tube(f"{nom}_Corde_{sx}_{sy}", (x + sx * cn * 0.8, y + sy * cn * 0.8, z_nacelle + hn),
                 (x + sx * r * 0.32, y + sy * r * 0.32, bas), 0.004, M("#6B4226"))


def planete(nom, x, y, z, r, couleur, anneau=None):
    sphere(f"{nom}_Boule", x, y, z, r, M(couleur, 0.4), u=20, v=12)
    if anneau:
        tore(f"{nom}_Anneau", x, y, z, r * 1.6, r * 0.12, M(anneau, 0.4), seg=32, ech=(1, 1, 0.35),
             rot=(math.radians(-20), math.radians(12), 0))


def cristal(nom, x, y, h, r, m, inclinaison=(0, 0)):
    """Prisme hexagonal à pointe (cristal)."""
    bm = bm_cylindre(r, h * 0.72, 6, z0=0)
    pointe = bmesh.new()
    bm2 = bm_cylindre(r, h * 0.28, 6, r_haut=0.0005, z0=h * 0.72)
    me_tmp = bpy.data.meshes.new("tmp")
    bm2.to_mesh(me_tmp)
    bm2.free()
    bm.from_mesh(me_tmp)
    bpy.data.meshes.remove(me_tmp)
    pointe.free()
    o = objet(nom, bm, [m], lisse=False)
    o.location = (x, y, -0.01)
    o.rotation_euler = (math.radians(inclinaison[0]), math.radians(inclinaison[1]), math.radians(15))
    return o


def amas_cristaux(nom, x, y, couleur, hauteur=0.42, em=0.45):
    # (émission réduite par rapport à la fiche : à 1,5, la vue « Standard » blanchit le
    #  cristal et on perd sa couleur ; à 0,45 il brille encore et reste violet / turquoise)
    m = M(couleur, 0.25, emission=em)
    pieces = ((0, 0, 1.0, 0.055, (0, 0)), (-0.07, 0.03, 0.62, 0.045, (8, -22)),
              (0.07, -0.02, 0.7, 0.045, (-6, 20)), (0.02, 0.07, 0.5, 0.04, (-20, 6)),
              (-0.03, -0.06, 0.4, 0.035, (18, -8)))
    for i, (dx, dy, k, r, inc) in enumerate(pieces):
        cristal(f"{nom}_{i}", x + dx, y + dy, hauteur * k, r, m, inc)


def poussiere(zones, n, graine, r=(0.008, 0.014), z=(0.004, 0.02)):
    """Poussière d'étoiles : petites sphères lumineuses, seulement dans les `zones`
    (rectangles x0, x1, y0, y1), jamais sur les places libres."""
    rnd = random.Random(graine)
    m = M("#FFF1D6", 0.3, emission=3.0)
    for i in range(n):
        x0, x1, y0, y1 = zones[i % len(zones)]
        sphere(f"Poussiere_{i}", rnd.uniform(x0, x1), rnd.uniform(y0, y1), rnd.uniform(*z),
               rnd.uniform(*r), m, u=6, v=4)


def mat_fanion(nom, pied, sommet, couleurs, n=4):
    """Mât de fête foraine : un poteau, et une guirlande de fanions qui descend vers le sol."""
    x, y = pied
    tube(f"{nom}_Mat", (x, y, 0), (x, y, sommet[2] + 0.02), 0.016, M("#8C5933", 0.7), cotes=8)
    sphere(f"{nom}_Boule", x, y, sommet[2] + 0.035, 0.026, M("#C99A1E", 0.35, 1.0))
    a = Vector((x, y, sommet[2]))            # haut du mât
    b = Vector((sommet[0], sommet[1], 0.04))  # le fil descend vers ce point, près du sol
    tube(f"{nom}_Fil", a, b, 0.004, M("#FFF4DC"))
    for i in range(n):
        t = (i + 0.6) / (n + 0.4)
        p = a.lerp(b, t)
        tri = [(-0.035, 0.0), (0.035, 0.0), (0.0, -0.075)]
        tri = [(-0.03, 0.0), (0.03, 0.0), (0.0, -0.065)]
        prisme(f"{nom}_Fanion_{i}", tri, 0.008, [M(couleurs[i % len(couleurs)], 0.6)],
               face_camera(p.x, p.y, p.z, 0))


# ---------------------------------------------------------------------------------------
# 4. La plaque commune (dessus, chemin, corps) — §16.3.2
# ---------------------------------------------------------------------------------------
def plaque(cote, palier, c):
    """`c` = couleurs du décor (dictionnaire). Construit le « gâteau » et son chemin."""
    L = cote / 2
    # Les « emporte-pièces » du chemin et de la place : invisibles au rendu, ils creusent
    # le dessus (booléen). Leurs faces donnent la couleur des bords du creux.
    coupe = bpy.data.collections.new("Coupe")
    COLLECTION.children.link(coupe)
    # (le booléen en mode « INDEX » donne aux parois du creux le matériau n° 3 de la cible :
    #  les emporte-pièces ont donc leurs faces au n° 3, et le dessus a « bord » en n° 3)
    bm = bm_boite(-DEMI_CHEMIN, DEMI_CHEMIN, -L - 0.2, L + 0.2, -0.3, 0.2)
    for f in bm.faces:
        f.material_index = 3
    o = objet("Coupe_Chemin", bm, [M(c["bord"])] * 4)
    COLLECTION.objects.unlink(o)
    coupe.objects.link(o)
    if palier:
        o2 = cylindre("Coupe_Place", 0, CENTRE_PLACE, -0.3, R_PLACE, 0.5, M(c["bord"]), cotes=48,
                      lisse=False)
        for _ in range(3):
            o2.data.materials.append(M(c["bord"]))
        for f in o2.data.polygons:
            f.material_index = 3
        COLLECTION.objects.unlink(o2)
        coupe.objects.link(o2)
    for ob in coupe.objects:
        ob.hide_render = True
        ob.display_type = "WIRE"

    # Le dessus : déborde de LEVRE sur le corps, biseau 3 cm sur 2 segments (§16.3.2)
    d = boite("Dessus", -L, L, -L, L, -H_DESSUS, 0.0, M(c["dessus"]), M(c.get("dessus_cotes", c["dessus"])),
              biseau=0.03, seg=2, mat_biseau=M(c["levre"]))
    # La lèvre ne projette pas d'ombre sur le corps : sinon la moitié haute du corps
    # (couleur claire) sortirait plus sombre que la moitié basse.
    d.visible_shadow = False
    d.data.materials.append(M(c["bord"]))          # n° 3 : parois du creux
    bo = d.modifiers.new("Creux", "BOOLEAN")
    bo.operation = "DIFFERENCE"
    bo.operand_type = "COLLECTION"
    bo.collection = coupe
    bo.solver = "EXACT"
    bo.material_mode = "INDEX"

    # Le remplissage du chemin (1 cm plus bas que le dessus : il est « creusé »)
    e = 0.003     # léger retrait : la paroi du creux (couleur « bord ») reste visible
    boite("Chemin", -DEMI_CHEMIN + e, DEMI_CHEMIN - e, -L, L, -H_DESSUS, -0.01,
          M(c["chemin"]), biseau=0.01, seg=2)
    if palier:
        cylindre("Place", 0, CENTRE_PLACE, -H_DESSUS, R_PLACE - e, H_DESSUS - 0.009,
                 M(c.get("place_fond", c["chemin"])), cotes=48, lisse=True)

    # Le corps : moitié haute et moitié basse (deux couleurs de terre, de pierre…)
    Lc = L - LEVRE
    zm = -(EPAISSEUR + H_DESSUS) / 2
    if c.get("corps_haut"):
        boite("Corps_Haut", -Lc, Lc, -Lc, Lc, zm, -H_DESSUS + 0.005, M(c["corps_haut"]),
              biseau=0.02, seg=2)
        boite("Corps_Bas", -Lc, Lc, -Lc, Lc, -EPAISSEUR, zm, M(c["corps_bas"]), biseau=0.03, seg=2)
    return L


def place_libre_palier(x, y):
    """Vrai si (x, y) est dans une place libre du palier (on n'y pose rien de haut)."""
    if abs(x) < DEMI_CHEMIN + 0.04:
        return True
    if math.hypot(x, y - CENTRE_PLACE) < R_PLACE + 0.02:
        return True
    return math.hypot(x + 0.32, y - 0.95) < 0.17


def dalles_place(cote, couleur, joint=None, r0=R_PIEDESTAL + 0.02, n=10, epaisseur=0.007,
                 emission=0.0):
    """Anneau de dalles en parts de tarte autour du piédestal (la place ronde)."""
    for i in range(n):
        a0 = 2 * math.pi * i / n + 0.025
        a1 = 2 * math.pi * (i + 1) / n - 0.025
        prisme(f"Dalle_Place_{i}", secteur(r0, R_PLACE - 0.03, a0, a1), epaisseur,
               [M(couleur, 0.5, emission=emission)],
               Matrix.Translation((0, CENTRE_PLACE, -0.009)), biseau=0.004)


def pas_chemin(L, palier, faire, pas=0.4, marge=0.03):
    """Appelle faire(y0, y1) pour chaque tronçon du chemin. Sur un palier, le chemin est
    coupé en deux morceaux (avant et arrière de la place ronde), chacun partagé en tronçons
    égaux d'environ `pas` mètres."""
    if palier:
        bout = math.sqrt(R_PLACE ** 2 - DEMI_CHEMIN ** 2)   # la place coupe le chemin ici
        morceaux = [(-L, CENTRE_PLACE - bout), (CENTRE_PLACE + bout, L)]
    else:
        morceaux = [(-L, L)]
    for a, b in morceaux:
        n = max(1, int(round((b - a) / pas)))
        for k in range(n):
            y0 = a + (b - a) * k / n + marge / 2
            y1 = a + (b - a) * (k + 1) / n - marge / 2
            faire(y0, y1)


def piedestal(m_cotes, m_haut, cotes=32, biseau=0.02):
    return cylindre("Piedestal", 0, CENTRE_PLACE, -0.01, R_PIEDESTAL, H_PIEDESTAL + 0.01, m_cotes,
                    m_haut, cotes=cotes, biseau=biseau, seg=2)


# ---------------------------------------------------------------------------------------
# 5. L'étage de la tour (commun aux 12 dalles) — §16.3.2 (v2)
# ---------------------------------------------------------------------------------------
# La dalle EST l'étage du jeu, vu en 3/4 : le grand disque de pierre bleu-gris (dalles en
# rayons, margelle claire, étoile violette au centre, coffres, flanc de gros blocs).
# Couleurs reprises du sol du jeu (blender/sol.py, commun.py). La pierre est la même sur
# les 12 images ; seuls le décor AUTOUR et la lumière changent avec le palier d'altitude.
# Le disque a le même diamètre que l'ancienne plaque (2 m, palier 2,6 m) : bords gauche,
# droit, avant et arrière aux mêmes pixels, donc mêmes points d'ancrage et même empilement.
DALLE = "#5B6380"          # M_PierreDalle
DALLE_VAR = "#535A75"      # M_PierreDalleVariante
JOINT = "#3C4258"          # M_PierreJoint
CLAIRE = "#8A90B0"         # M_PierreClaire (margelle, estrade)
FLANC = "#434A63"          # M_PierreFlanc
INCRUSTATION = "#6A4C9C"   # M_Incrustation (l'étoile violette du centre)
MARGELLE_L = 0.08          # largeur de la margelle (bord clair)
MARGELLE_H = 0.035         # elle dépasse du dallage, comme dans le jeu


def anneau_secteurs(nom, r0, r1, n, z0, z1, choisir_mat, decalage=0.0, joint=0.012, biseau=0.008,
                    ecart_z=None):
    """Un anneau découpé en `n` dalles (parts d'anneau), chacune un prisme biseauté.
    `choisir_mat(i)` donne la matière de la dalle i ; `ecart_z(i)` la fait descendre un peu."""
    for i in range(n):
        a0 = 2 * math.pi * (i + decalage) / n
        a1 = 2 * math.pi * (i + 1 + decalage) / n
        da = joint / max(r0, 0.05)                 # le joint garde la même largeur partout
        pts = secteur(r0 + joint, r1 - joint, a0 + da, a1 - da, n=max(2, int(24 / n * 4)))
        dz = ecart_z(i) if ecart_z else 0.0
        prisme(f"{nom}_{i}", pts, z1 - z0, [choisir_mat(i)], Matrix.Translation((0, 0, z0 - dz)),
               biseau=biseau)


def coffre(nom, x, y, echelle=1.0, angle=0.0):
    """Petit coffre comme ceux du jeu (coffre.fbx) : bois brun, cerclages de métal sombre,
    serrure de laiton. L'avant regarde la caméra (−Y)."""
    k = echelle
    bois, bois_f = M("#6B4226", 0.6), M("#5A3620", 0.6)
    metal, laiton = M("#3C4258", 0.45, 0.6), M("#C99A1E", 0.35, 1.0)
    w, d, h = 0.13 * k, 0.085 * k, 0.1 * k
    rot = Matrix.Translation((x, y, 0)) @ Matrix.Rotation(angle, 4, "Z")
    objs = [boite(f"{nom}_Corps", -w, w, -d, d, 0.0, h, bois, biseau=0.01 * k)]
    bm = bm_cylindre(d, 2 * w, 12, z0=-w)
    bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0), matrix=Matrix.Rotation(math.pi / 2, 3, "Y"))
    for v in bm.verts:                     # on garde la moitié haute : couvercle bombé
        v.co.z = max(v.co.z, 0.0)
    bmesh.ops.translate(bm, verts=bm.verts, vec=(0, 0, h))
    objs.append(objet(f"{nom}_Couvercle", bm, [bois_f], lisse=True, angle=50, biseau=0.006 * k))
    for i, sx in enumerate((-0.62, 0.62)):
        # cerclage : une bande sur le corps + un demi-anneau qui épouse le couvercle bombé
        objs.append(boite(f"{nom}_Cerclage_{i}", sx * w - 0.014 * k, sx * w + 0.014 * k, -d - 0.006 * k,
                          d + 0.006 * k, -0.002, h, metal, biseau=0.004 * k))
        bm = bm_cylindre(d + 0.006 * k, 0.028 * k, 12, z0=-0.014 * k)
        bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0), matrix=Matrix.Rotation(math.pi / 2, 3, "Y"))
        for v in bm.verts:
            v.co.z = max(v.co.z, 0.0)
        bmesh.ops.translate(bm, verts=bm.verts, vec=(sx * w, 0, h))
        objs.append(objet(f"{nom}_Cerclage_Haut_{i}", bm, [metal], lisse=True, angle=50))
    objs.append(boite(f"{nom}_Serrure", -0.026 * k, 0.026 * k, -d - 0.014 * k, -d + 0.01 * k,
                      h - 0.05 * k, h + 0.012 * k, laiton, biseau=0.006 * k))
    for o in objs:
        o.matrix_world = rot @ o.matrix_world


def etage_tour(cote, palier, variante):
    R = cote / 2
    rnd = random.Random(11 if variante == "a" else 23 if variante == "b" else 37)
    joint, claire = M(JOINT, 0.9), M(CLAIRE, 0.7)
    violet = M(INCRUSTATION, 0.5, 0.3)
    # Le noyau : un disque sombre (la couleur des joints) sous le dallage
    cylindre("Noyau", 0, 0, -0.075, R - 0.02, 0.075 - 0.012, joint, cotes=64, lisse=True)
    # Les dalles en rayons (comme le sol du jeu) : couronnes décalées d'une demi-dalle
    couronnes = ([(0.0, 0.32, 1), (0.32, 0.62, 8), (0.62, R - MARGELLE_L, 16)] if not palier else
                 [(0.0, 0.34, 1), (0.34, 0.74, 10), (0.74, R - MARGELLE_L, 20)])
    for c, (r0, r1, n) in enumerate(couronnes):
        if n == 1:
            cylindre("Dalle_Centre", 0, 0, -0.016, r1 - 0.012, 0.016, M(DALLE, 0.8), cotes=32, biseau=0.008)
            continue
        anneau_secteurs(f"Dalle_C{c}", r0, r1, n, -0.016, 0.0,
                        lambda i: M(DALLE_VAR if rnd.random() < 0.33 else DALLE, 0.8),
                        decalage=0.5 * (c % 2) + (0.25 if variante == "b" else 0.0),
                        ecart_z=lambda i: rnd.uniform(0.0, 0.006))
    # La margelle : 16 (ou 20) blocs clairs surélevés, qui débordent de 4 cm sur le flanc
    anneau_secteurs("Margelle", R - MARGELLE_L, R, 16 if not palier else 20, -0.05, MARGELLE_H,
                    lambda i: claire, decalage=0.25, joint=0.008, biseau=0.014)
    # Le flanc : la muraille de la tour, 2 rangées de gros blocs décalés
    cylindre("Flanc_Noyau", 0, 0, -EPAISSEUR, R - LEVRE - 0.012, EPAISSEUR - 0.04, joint, cotes=64)
    for r, (z0, z1) in enumerate(((-0.178, -0.118), (-0.114, -0.052))):
        anneau_secteurs(f"Bloc_R{r}", R - LEVRE - 0.03, R - LEVRE, 24 if not palier else 30, z0, z1,
                        lambda i: M(FLANC, 0.85), decalage=0.5 * r, joint=0.006, biseau=0.006)
    # L'étoile violette incrustée au centre (à plat : la place du cheval reste libre)
    cy = CENTRE_PLACE if palier else 0.0
    r_et = (R_PLACE - 0.04) if palier else 0.3
    prisme("Etoile_Centre", etoile_2d(r_et, creux=0.62, branches=8), 0.004, [violet],
           Matrix.Translation((0, cy, 0.0)) @ Matrix.Rotation(math.pi / 8, 4, "Z"))
    if not palier:
        cylindre("Etoile_Coeur", 0, 0, 0.0, 0.1, 0.005, M("#7E62B4", 0.5, 0.3), cotes=24)
    # Les coffres (au fond, hors des places libres) ; au palier : l'estrade du cheval au centre
    if palier:
        piedestal(M("#7A80A0", 0.7), claire)
        tore("Piedestal_Filet", 0, CENTRE_PLACE, 0.1, R_PIEDESTAL + 0.004, 0.012, violet, seg=48)
        coffre("Coffre_G", -0.88, 0.8, 1.2, math.radians(22))
        coffre("Coffre_D", 0.88, 0.8, 1.2, math.radians(-22))
    else:
        coffre("Coffre_G", -0.56, 0.54, 1.0, math.radians(14 if variante == "a" else 8))
        coffre("Coffre_D", 0.56, 0.54, 1.0, math.radians(-14 if variante == "a" else -8))
    return R


# ---------------------------------------------------------------------------------------
# 5 bis. Le décor AUTOUR de l'étage, selon le palier d'altitude du jeu
#   (Assets/Data/Paliers/Palier1..4.asset, AltitudeDecor.cs, GameManager.TierForFloor).
#   Règle : rien n'est posé sur le sol de l'étage. Les nuages et les étoiles sont dans les
#   coins vides de l'image, derrière le disque et dessous.
# ---------------------------------------------------------------------------------------
def caser(fn, *args, **kw):
    """Construit un élément du décor (fn), puis le RAMÈNE dans l'image s'il en dépasse.
    Pourquoi ? Les coins libres autour du disque sont petits : un nuage coupé net par le bord
    de l'image ferait une ligne droite très laide. On mesure sa boîte à l'écran
    (world_to_camera_view) et on le glisse vers l'intérieur : en x le long de X, en y le long
    du « haut » de la caméra (il garde sa place en profondeur)."""
    avant = set(COLLECTION.objects)
    fn(*args, **kw)
    nouveaux = [o for o in COLLECTION.objects if o not in avant]
    scene = bpy.context.scene
    cam = scene.camera
    w = scene.render.resolution_x / ECHELLE
    h = scene.render.resolution_y / ECHELLE
    bpy.context.view_layer.update()
    xs, ys = [], []
    for o in nouveaux:
        for c in o.bound_box:
            v = world_to_camera_view(scene, cam, o.matrix_world @ Vector(c))
            xs.append(v.x * w)
            ys.append((1 - v.y) * h)
    if not xs:
        return
    marge, marge_bas = 7, 5
    dx = max(0, marge - min(xs)) - max(0, max(xs) - (w - marge))
    dy = max(0, marge - min(ys)) - max(0, max(ys) - (h - marge_bas))   # pixels vers le bas
    if dx or dy:
        a = math.radians(INCLINAISON)
        haut = Vector((0, math.sin(a), math.cos(a)))
        dep = Vector((dx / PX_PAR_M, 0, 0)) - haut * (dy / PX_PAR_M)
        for o in nouveaux:
            if o.parent is None:
                o.location += dep


def nuage_palier(nom, x, y, z, taille, haut, bas, boules=None):
    """Nuage du jeu : boules dodues, ventre plus sombre (la couleur cloudColor du palier)."""
    mh, mb = M(haut, 0.7), M(bas, 0.7)
    boules = boules or ((0, 0, 0, 1.0, mh), (-0.9, 0.05, -0.22, 0.75, mh), (0.9, -0.05, -0.25, 0.72, mh),
                        (0.35, 0.25, 0.22, 0.62, mh), (-0.45, -0.2, -0.45, 0.6, mb), (0.5, -0.2, -0.5, 0.55, mb))
    for i, (dx, dy, dz, k, m) in enumerate(boules):
        sphere(f"{nom}_{i}", x + dx * taille, y + dy * taille, z + dz * taille, taille * k, m,
               ech=(1.2, 1, 0.82))


def oiseau(nom, x, y, z, taille=1.0, couleur="#F4F8FC", angle=0.0):
    """Oiseau stylisé en vol (deux ailes en V), dressé face à la caméra."""
    k = taille
    for s in (-1, 1):
        pts = [(0, 0), (s * 0.07 * k, 0.035 * k), (s * 0.085 * k, 0.022 * k), (s * 0.012 * k, -0.014 * k)]
        if s < 0:
            pts = pts[::-1]
        prisme(f"{nom}_Aile_{s}", pts, 0.008, [M(couleur, 0.5)], face_camera(x, y, z, angle), biseau=0.002)


def etoile_flottante(nom, x, y, z, r, couleur="#FFF1D6", em=1.0):
    prisme(nom, etoile_2d(r, 0.45), 0.012, [M(couleur, 0.3, emission=em)], face_camera(x, y, z, 0.15),
           biseau=0.003)


def coins(R, palier, variante):
    """Les 4 coins vides autour du disque (x, y) : arrière gauche/droit, avant gauche/droit."""
    k = 1.3 if palier else 1.0
    return dict(ag=(-0.86 * k, 0.86 * k), ad=(0.86 * k, 0.86 * k), vg=(-0.92 * k, -0.78 * k),
                vd=(0.92 * k, -0.78 * k))


def autour_jour(variante, palier, R):
    """Palier 1, étages 1-3, « En plein jour » : ciel bleu, quelques nuages LOINTAINS."""
    k = 1.3 if palier else 1.0
    h, b = "#F2EEF8", "#D8CCE6"                 # cloudColor du palier 1 (#D8CCE6)
    s = 1 if variante != "b" else -1
    caser(nuage_palier, "Nuage_Loin_1", -s * 0.86 * k, 0.95 * k, 0.12, 0.075, h, b)
    caser(nuage_palier, "Nuage_Loin_2", s * 0.96 * k, 0.78 * k, 0.04, 0.055, h, b)
    caser(nuage_palier, "Nuage_Loin_Bas", s * 0.95 * k, -0.85 * k, -0.28, 0.06, h, b)
    if palier:
        caser(nuage_palier, "Nuage_Loin_3", s * 0.8 * k, 0.98 * k, 0.16, 0.06, h, b)
        caser(nuage_palier, "Nuage_Loin_Bas2", -s * 0.95 * k, -0.88 * k, -0.3, 0.055, h, b)


def autour_dans_nuages(variante, palier, R):
    """Palier 2, étages 4-6, « Dans les nuages ! » : nuages pêche AUTOUR et SOUS l'étage,
    lumière chaude de fin de journée."""
    k = 1.3 if palier else 1.0
    h, b = "#FFF2EA", "#F7D3C2"                 # cloudColor du palier 2 (#F7D3C2), ventre rosé
    s = 1 if variante != "b" else -1
    caser(nuage_palier, "Nuage_AvG", -0.9 * k, -0.86 * k, -0.24, 0.1 * k, h, "#F0C0B4")
    caser(nuage_palier, "Nuage_AvD", 0.92 * k, -0.8 * k, -0.2, 0.09 * k, h, "#F0C0B4")
    caser(nuage_palier, "Nuage_Ar", -s * 0.9 * k, 0.9 * k, 0.06, 0.09 * k, h, b)
    if palier:
        caser(nuage_palier, "Nuage_Ar2", s * 0.92 * k, 0.88 * k, 0.04, 0.08 * k, h, b)
    else:
        caser(nuage_palier, "Nuage_Sous", -s * 0.55, -1.02, -0.3, 0.075, h, "#F0C0B4")


def autour_mer_nuages(variante, palier, R):
    """Palier 3, étages 7-9, « Au-dessus des nuages ! » : une mer de nuages lavande EN
    DESSOUS de l'étage (elle dépasse tout autour du bas du flanc), premières étoiles."""
    k = 1.3 if palier else 1.0
    h, b = "#E4DAF0", "#B8A8D8"                 # cloudColor du palier 3 (#D8C8E8)
    # des grappes de nuages tout autour du bas du flanc (celles de devant passent sous l'étage)
    angles = (165, 195, 228, 262, 298, 332, 15) if not palier else (165, 192, 222, 255, 285, 318, 348, 15)
    for i, ang in enumerate(angles):
        a = math.radians(ang + (8 if variante == "b" else 0))
        caser(nuage_palier, f"Mer_{i}", R * 1.02 * math.cos(a), R * 1.02 * math.sin(a), -0.27,
              0.1 * k, h, b)
    for i, (x, y, z, r) in enumerate(((-0.9 * k, 0.92 * k, 0.12, 0.028), (0.95 * k, 0.8 * k, 0.16, 0.022),
                                      (0.78 * k, 1.0 * k, 0.05, 0.018))):
        caser(etoile_flottante, f"Etoile_{i}", x if variante != "b" else -x, y, z, r, em=0.9)


def autour_etoiles(variante, palier, R):
    """Palier 4, étages 10 et plus, « Dans les étoiles ! » : nuit étoilée, petites planètes,
    nuages sombres très loin dessous."""
    k = 1.3 if palier else 1.0
    s = 1 if variante != "b" else -1
    rnd = random.Random(3 if variante == "a" else 8 if variante == "b" else 13)
    pos = [(-0.95, 0.9, 0.18), (-0.78, 0.98, 0.04), (0.98, 0.7, 0.1), (0.85, 1.0, 0.22),
           (-1.02, -0.5, -0.08), (1.02, -0.35, -0.12), (-0.98, 0.2, -0.05), (0.99, 0.25, 0.0)]
    for i, (x, y, z) in enumerate(pos):
        caser(etoile_flottante, f"Etoile_{i}", x * k * s, y * k, z, rnd.uniform(0.02, 0.034),
                         "#FFF1D6" if i % 3 else "#D9C8FF", em=1.1)
    caser(planete, "Planete_1", s * 0.88 * k, 0.86 * k, 0.12, 0.07, "#B48CFF", "#FFD54A")
    if palier:
        caser(planete, "Planete_2", -s * 0.9 * k, 0.88 * k, 0.1, 0.06, "#2BB8C4")
    for i, x in enumerate((-0.92, 0.95)):
        caser(nuage_palier, f"Nuage_Loin_{i}", x * k, -0.85 * k, -0.3, 0.07 * k, "#8A80B0", "#5E5488")


AUTOUR = {"herbe": autour_jour, "pierre": autour_dans_nuages, "nuages": autour_mer_nuages,
          "galaxie": autour_etoiles}


def construire_dalle(decor, variante, palier, cote):
    R = etage_tour(cote, palier, variante)
    AUTOUR[decor](variante, palier, R)


# Les noms de fichiers ne changent pas (le programmeur les utilise déjà) :
#   herbe   = palier 1 « En plein jour »          (étages 1-3)
#   pierre  = palier 2 « Dans les nuages ! »      (étages 4-6)
#   nuages  = palier 3 « Au-dessus des nuages ! » (étages 7-9)
#   galaxie = palier 4 « Dans les étoiles ! »     (étages 10 et plus)
# (les plages suivent GameManager.TierForFloor)
DECORS = {d: (lambda v, p, c, d=d: construire_dalle(d, v, p, c)) for d in ("herbe", "pierre", "nuages", "galaxie")}

# Lumière de chaque palier, reprise des assets Palier1..4 : (couleur de la lumière, énergie),
# remplissage (ambientEquator), liseré, (ciel = ambientSky, force)
AMBIANCES = {
    "herbe": (("#FFF4E0", 2.9), ("#90A8C8", 0.5), ("#BFDDF5", 1.2), ("#BFDDF5", 0.32)),
    "pierre": (("#FFEAD4", 2.8), ("#B8A8C8", 0.5), ("#FFB090", 1.2), ("#E0D4D8", 0.3)),
    "nuages": (("#EEEAFF", 2.75), ("#8A80B8", 0.5), ("#B48CFF", 1.4), ("#C0B8E0", 0.32)),
    "galaxie": (("#F2ECFF", 2.65), ("#6A8AA0", 0.5), ("#B48CFF", 1.9), ("#A098D0", 0.32)),
}


# ---------------------------------------------------------------------------------------
# 6. Scène : moteur, caméra, lumières (§16.3.3)
# ---------------------------------------------------------------------------------------
def scene_neuve(fmt, decor):
    global COLLECTION
    bpy.ops.wm.read_factory_settings(use_empty=True)
    _MATS.clear()
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    ee = scene.eevee
    ee.taa_render_samples = 64
    for attr, val in (("use_shadows", True), ("use_raytracing", False),
                      ("shadow_ray_count", 3), ("shadow_step_count", 8), ("use_gtao", True)):
        if hasattr(ee, attr):
            setattr(ee, attr, val)
    w, h = fmt["image"]
    scene.render.resolution_x = w * ECHELLE
    scene.render.resolution_y = h * ECHELLE
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.filter_size = 1.2
    scene.view_settings.view_transform = "Standard"     # le #hex tapé = le #hex obtenu
    scene.view_settings.look = "None"
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.image_settings.color_depth = "8"
    # un ciel lavande faible : ombres jamais noires, reflet doux sur le laiton
    monde = bpy.data.worlds.new("MondeDalles")
    monde.use_nodes = True
    monde.node_tree.nodes["Background"].inputs["Color"].default_value = C.hex_vers_lineaire(AMBIANCES[decor][3][0])
    monde.node_tree.nodes["Background"].inputs["Strength"].default_value = AMBIANCES[decor][3][1]
    scene.world = monde

    COLLECTION = bpy.data.collections.new("Dalle")
    scene.collection.children.link(COLLECTION)

    # Caméra orthographique : rotation X 62° (vue plongeante de 28°), Z = 0 (face à la dalle).
    # Ortho Scale = largeur de l'image en mètres = largeur en pixels / 520.
    cd = bpy.data.cameras.new("CameraDalle")
    cd.type = "ORTHO"
    cd.ortho_scale = w * ECHELLE / (PX_PAR_M * ECHELLE)
    cd.sensor_fit = "HORIZONTAL"
    cd.clip_start, cd.clip_end = 0.1, 100
    cam = bpy.data.objects.new("CameraDalle", cd)
    a = math.radians(INCLINAISON)
    vise = Vector((0, math.cos(a), -math.sin(a)))      # direction du regard
    haut = Vector((0, math.sin(a), math.cos(a)))       # le « haut » de l'image
    # Le centre du dessus doit tomber `dy` pixels AU-DESSUS du centre de l'image (§16.3.6) :
    # on recule la caméra le long du regard, et on la descend de dy le long du « haut ».
    dy = (h / 2 - fmt["centre_y"]) / PX_PAR_M
    cam.location = -vise * 10 - haut * dy
    cam.rotation_euler = (math.radians(90 - INCLINAISON), 0, 0)
    scene.collection.objects.link(cam)
    scene.camera = cam
    lumieres(scene, decor)
    return scene, cam


def soleil(scene, nom, hexa, energie, vers_lumiere, angle=10.0, ombre=True):
    d = bpy.data.lights.new(nom, "SUN")
    d.color = C.hex_vers_lineaire(hexa)[:3]
    d.energy = energie
    d.angle = math.radians(angle)        # soleil un peu large = ombres douces
    d.use_shadow = ombre
    o = bpy.data.objects.new(nom, d)
    o.rotation_euler = (-Vector(vers_lumiere)).normalized().to_track_quat("-Z", "Y").to_euler()
    scene.collection.objects.link(o)


def lumieres(scene, decor):
    """Les trois lumières des icônes et des bulles. Vecteur = de la dalle VERS la lampe
    (X droite, Y vers le fond, Z haut ; la caméra est du côté −Y)."""
    (cle, e1), (rem, e2), (lis, e3), _ = AMBIANCES[decor]   # la couleur change avec l'altitude
    soleil(scene, "Principale", cle, e1, (-0.55, -0.45, 0.85), angle=12)   # haut gauche, devant
    soleil(scene, "Remplissage", rem, e2, (0.7, -0.6, 0.35), angle=30, ombre=False)
    soleil(scene, "Liseré", lis, e3, (0.3, 1.0, 0.35), angle=15, ombre=False)


# ---------------------------------------------------------------------------------------
# 7. Contrôle du cadrage (§16.3.3 : les 4 coins du dessus à ± 2 px)
# ---------------------------------------------------------------------------------------
def verifier_coins(scene, cam, fmt):
    """Les 4 bords du disque (gauche, droit, arrière, avant, au niveau du sol) doivent tomber
    sur les mêmes pixels que les bords de l'ancienne plaque carrée (§16.3.6) : ainsi les
    points d'ancrage et l'empilement ne changent pas."""
    L = fmt["cote"] / 2
    w, h = fmt["image"]
    (gx_, haut_), (dx_, _), (_, avant_), _ = fmt["coins"]
    attendus = (((-L, 0), (gx_, fmt["centre_y"])), ((L, 0), (dx_, fmt["centre_y"])),
                ((0, L), (w / 2, haut_)), ((0, -L), (w / 2, avant_)))
    bpy.context.view_layer.update()
    ecarts = []
    for (x, y), (px_x, px_y) in attendus:
        v = world_to_camera_view(scene, cam, Vector((x, y, 0)))
        gx, gy = v.x * w, (1 - v.y) * h
        ecarts.append(max(abs(gx - px_x), abs(gy - px_y)))
    if max(ecarts) > 2.0:
        raise RuntimeError(f"Cadrage faux : écart de {max(ecarts):.2f} px sur les bords")
    return max(ecarts)


# ---------------------------------------------------------------------------------------
# 8. Après le rendu : contour d'encre et ombre portée (numpy)
# ---------------------------------------------------------------------------------------
def lire_png(chemin):
    img = bpy.data.images.load(chemin)
    w, h = img.size
    px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)
    bpy.data.images.remove(img)
    return px


def ecrire_png(px, chemin):
    h, w = px.shape[:2]
    img = bpy.data.images.new("Sortie", w, h, alpha=True)
    img.colorspace_settings.name = "sRGB"
    img.pixels = np.clip(px, 0, 1).ravel()
    img.filepath_raw = chemin
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)


def decaler(a, dx, dy):
    """Décale un tableau 2D (dy > 0 : vers le BAS de l'image ; les lignes numpy de Blender
    vont du bas vers le haut, d'où le signe)."""
    out = np.zeros_like(a)
    h, w = a.shape
    sy = -dy
    ys0, ys1 = max(0, -sy), min(h, h - sy)
    xs0, xs1 = max(0, -dx), min(w, w - dx)
    out[ys0 + sy:ys1 + sy, xs0 + dx:xs1 + dx] = a[ys0:ys1, xs0:xs1]
    return out


def dilater(a, r):
    """Agrandit la silhouette de r pixels (disque) : c'est l'épaisseur du contour."""
    out = a.copy()
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            d = math.hypot(dx, dy)
            if 0 < d <= r + 0.5:
                k = min(1.0, r + 0.5 - d + 0.5)       # bord du disque adouci
                out = np.maximum(out, decaler(a, dx, dy) * k)
    return out


def flou_gaussien(a, sigma):
    n = int(sigma * 3)
    x = np.arange(-n, n + 1)
    k = np.exp(-x * x / (2 * sigma * sigma))
    k /= k.sum()
    h, w = a.shape
    p = np.pad(a, n)                     # bords à zéro : rien ne « revient » de l'autre côté
    tmp = np.zeros((h + 2 * n, w), dtype=a.dtype)
    for i, kk in zip(x, k):
        tmp += p[:, n + i:n + i + w] * kk
    out = np.zeros_like(a)
    for i, kk in zip(x, k):
        out += tmp[n + i:n + i + h, :] * kk
    return out


def sur(dessus, dessous):
    """Pose `dessus` sur `dessous` (alpha « droit », couleurs sRGB)."""
    a1, a2 = dessus[..., 3:4], dessous[..., 3:4]
    a = a1 + a2 * (1 - a1)
    rgb = (dessus[..., :3] * a1 + dessous[..., :3] * a2 * (1 - a1)) / np.maximum(a, 1e-6)
    return np.concatenate([rgb, a], axis=2)


def contour_et_ombre(px):
    encre = np.array([int(ENCRE[i:i + 2], 16) / 255 for i in (1, 3, 5)], dtype=np.float32)
    alpha = px[..., 3]
    sil = dilater(alpha, 4 * ECHELLE)                         # contour : 4 px affichés
    trait = np.zeros_like(px)
    trait[..., :3] = encre
    trait[..., 3] = sil
    ombre = np.zeros_like(px)
    ombre[..., :3] = encre
    ombre[..., 3] = flou_gaussien(decaler(sil, 0, 10 * ECHELLE), 6 * ECHELLE) * 0.35
    return sur(sur(px, trait), ombre)


# ---------------------------------------------------------------------------------------
# 9. Une dalle
# ---------------------------------------------------------------------------------------
def rendre_dalle(decor, variante):
    palier = variante == "palier"
    fmt = FORMATS["palier" if palier else "normale"]
    scene, cam = scene_neuve(fmt, decor)
    DECORS[decor](variante, palier, fmt["cote"])
    ecart = verifier_coins(scene, cam, fmt)
    os.makedirs(DOSSIER_DALLES, exist_ok=True)
    chemin = os.path.join(DOSSIER_DALLES, f"dalle_{decor}_{variante}.png")
    brut = os.path.join(DOSSIER_DALLES, f"_brut_{decor}_{variante}.png")
    scene.render.filepath = brut
    bpy.ops.render.render(write_still=True)
    px = lire_png(brut)
    os.remove(brut)
    # contrôle : rien ne doit toucher le haut de l'image (§16.3.3, « marges du haut »)
    haut = px[-3:, :, 3].max()
    px = contour_et_ombre(px)
    ecrire_png(px, chemin)
    if os.environ.get("APERCU"):          # essai : la dalle posée sur la couleur de son décor
        hexa = FONDS[decor]
        f = np.zeros_like(px)
        f[...] = [int(hexa[i:i + 2], 16) / 255 for i in (1, 3, 5)] + [1.0]
        ecrire_png(sur(px, f), os.path.join(os.environ["APERCU"], f"apercu_{decor}_{variante}.png"))
    n_obj = sum(1 for o in COLLECTION.all_objects if o.type == "MESH" and not o.hide_render)
    print(f"DALLE {decor}_{variante} : coins à {ecart:.2f} px, haut libre={haut < 0.01}, "
          f"{n_obj} pièces -> {chemin}")
    return chemin


# ---------------------------------------------------------------------------------------
# 10. Planche de contrôle (§16.3.7) : chaque décor sur sa couleur de fond, dalles empilées,
#     pièces, diamants, drapeau posés sur leurs points.
# ---------------------------------------------------------------------------------------
FONDS = {"herbe": "#2C5AA0", "pierre": "#403A66", "nuages": "#5D4FA6", "galaxie": "#17112F"}
# (couleurs des bandes du fond de L'Ascension, §16.2.2 : le programmeur peut les ajuster
#  au nouveau thème « ciel » des étages 10-19)


def reduire(px, w, h):
    """Réduit une image (h, w, 4) à la taille demandée (via Blender), alpha prémultiplié."""
    hh, ww = px.shape[:2]
    img = bpy.data.images.new("tmp", ww, hh, alpha=True)
    pm = px.copy()
    pm[..., :3] *= pm[..., 3:4]
    img.pixels = pm.ravel()
    img.scale(w, h)
    out = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)
    bpy.data.images.remove(img)
    out[..., :3] /= np.maximum(out[..., 3:4], 1e-6)
    return out


def coller(fond, img, x, y):
    """Colle `img` (h, w, 4) avec son coin HAUT-gauche au pixel (x, y) (y vers le bas)."""
    H = fond.shape[0]
    h, w = img.shape[:2]
    y0 = H - (y + h)
    x0 = x
    ys0, xs0 = max(0, -y0), max(0, -x0)
    ys1, xs1 = min(h, H - y0), min(w, fond.shape[1] - x0)
    if ys1 <= ys0 or xs1 <= xs0:
        return
    zone = fond[y0 + ys0:y0 + ys1, x0 + xs0:x0 + xs1]
    fond[y0 + ys0:y0 + ys1, x0 + xs0:x0 + xs1] = sur(img[ys0:ys1, xs0:xs1], zone)


def planche(decors):
    icones = {}
    for nom, t in (("piece", 64), ("diamant", 56), ("drapeau_record", 96)):
        icones[nom] = reduire(lire_png(os.path.join(DOSSIER_ICONES, f"{nom}.png")), t, t)
    col_w = 760
    ordre = ("palier", "b", "a", "b")          # de haut en bas, comme la route
    hauteur_col = 430 + 340 * 3 + 40
    fond = np.zeros((hauteur_col, col_w * len(decors), 4), dtype=np.float32)
    for c, decor in enumerate(decors):
        hexa = FONDS[decor]
        coul = np.array([int(hexa[i:i + 2], 16) / 255 for i in (1, 3, 5)] + [1.0], dtype=np.float32)
        fond[:, c * col_w:(c + 1) * col_w] = coul
        y = 20
        prec = None
        for v in ordre:
            chemin = os.path.join(DOSSIER_DALLES, f"dalle_{decor}_{v}.png")
            if not os.path.exists(chemin):
                continue
            px = lire_png(chemin)
            w, h = px.shape[1] // 2, px.shape[0] // 2
            img = reduire(px, w, h)
            # marge négative entre deux dalles (§16.3.6)
            if prec is not None:
                y += {("palier", "n"): -28, ("n", "n"): -19, ("n", "palier"): -36}[
                    (prec, "palier" if v == "palier" else "n")]
            x = c * col_w + (col_w - w) // 2
            coller(fond, img, x, y)
            if v == "palier":
                coller(fond, icones["drapeau_record"], x + 275 - int(96 * 0.37), y + 94 - int(96 * 0.84))
            else:
                coller(fond, icones["piece"], x + 124 - 32, y + 206 - 64)
                coller(fond, icones["piece"], x + 124 - 32, y + 206 - 64 - 18)
                coller(fond, icones["diamant"], x + 436 - 28 - 22, y + 206 - 56)
                coller(fond, icones["diamant"], x + 436 - 28 + 22, y + 206 - 56)
            y += h
            prec = "palier" if v == "palier" else "n"
    ecrire_png(fond, PLANCHE)
    print(f"PLANCHE -> {PLANCHE}")


def principal():
    toutes = [f"{d}_{v}" for d in DECORS for v in ("a", "b", "palier")]
    choix = os.environ.get("DALLES", "").split() or toutes
    for nom in choix:
        decor, variante = nom.split("_")
        rendre_dalle(decor, variante)
    if os.environ.get("PLANCHE", "1") != "0":
        planche([d for d in DECORS if any(n.startswith(d) for n in choix)] or list(DECORS))


if __name__ == "__main__":
    principal()
