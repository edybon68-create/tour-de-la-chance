"""
objets_lot9.py — Les objets des images du lot 9 (V7), fabriqués en 3D au lieu de Gemini.

Pourquoi en 3D ? Le rendu Blender donne exactement le style « jouet verni » des autres icônes
rendues (même lumière, même cadrage), et surtout une transparence PARFAITE : pas de fond lavande
à détourer, donc aucun liseré ni trou (mandat V7, point 7). Fiches : `art/prompts/lot9.md`
(critères de réussite) et art bible §17.

Ce fichier ne lance rien tout seul : il prête des fonctions `construire_...()` qui
fabriquent un objet dans une scène neuve et renvoient sa racine.
  - construire_sablier()       -> icône `sablier_bientot` (rendu par rendu_icones.py)
  - construire_pack_tresor()   -> icône `pack_diamants_tresor` (rendu par rendu_icones.py)
  - construire_guirlande()     -> décoration `guirlande_fanions` (rendu par rendu_lot9.py)
  - construire_ruban()         -> décoration `ruban_palier` (rendu par rendu_lot9.py)

Unités : 1 unité Blender = 1 mètre pour les icônes (l'échelle n'a pas d'importance : la caméra
cadre l'objet). Pour les deux décorations vues de face : 1 unité = 100 pixels de l'image.
Repère : Z en haut ; l'avant de l'objet regarde vers +Y pour les icônes (comme les autres
icônes rendues), vers −Y (vers la caméra de face) pour les décorations.
"""

import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402


# ---------------------------------------------------------------------------------------
# Petits outils
# ---------------------------------------------------------------------------------------
def matiere(nom, hexa, rugosite=0.4, metal=0.0, emission=0.0, vernis=0.25):
    """Matière d'une couleur unie, avec un léger vernis (le reflet « jouet » de la bible)."""
    m = bpy.data.materials.new(nom)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = C.hex_vers_lineaire(hexa)
    b.inputs["Roughness"].default_value = rugosite
    b.inputs["Metallic"].default_value = metal
    b.inputs["Coat Weight"].default_value = vernis
    b.inputs["Coat Roughness"].default_value = 0.25
    if emission > 0:
        b.inputs["Emission Color"].default_value = C.hex_vers_lineaire(hexa)
        b.inputs["Emission Strength"].default_value = emission
    m.diffuse_color = C.hex_vers_lineaire(hexa)
    return m


def objet(nom, bm, mats, parent=None, lisse=True, angle=40.0, biseau=0.0, seg=2):
    """Transforme un bmesh en objet : ombrage doux (arêtes vives au-delà de `angle`) et,
    si demandé, un biseau « jouet » (modificateur Bevel)."""
    me = bpy.data.meshes.new(nom)
    bm.to_mesh(me)
    bm.free()
    for m in mats:
        me.materials.append(m)
    o = bpy.data.objects.new(nom, me)
    bpy.context.scene.collection.objects.link(o)
    if lisse:
        me.shade_smooth()
        me.set_sharp_from_angle(angle=math.radians(angle))
    if biseau > 0:
        b = o.modifiers.new("Biseau", "BEVEL")
        b.width, b.segments = biseau, seg
        b.limit_method = "ANGLE"
        b.angle_limit = math.radians(30)
        b.use_clamp_overlap = True
    if parent is not None:
        C.parenter(o, parent)
    return o


def revolution(nom, profil, mats_idx, mats, parent, segments=24, angle=40.0):
    bm = bmesh.new()
    C.bm_revolution(bm, profil, segments, mats=mats_idx)
    return objet(nom, bm, mats, parent, angle=angle)


def prisme(nom, contour, epaisseur, mat, parent, biseau=0.03, y=0.0):
    """Forme plate (contour dans le plan XZ) épaissie vers +Y, avec biseau : fanions, queues
    de ruban. `y` = position de la face avant."""
    bm = bmesh.new()
    bas = [bm.verts.new((x, y, z)) for x, z in contour]
    haut = [bm.verts.new((x, y + epaisseur, z)) for x, z in contour]
    bm.faces.new(bas)
    bm.faces.new(list(reversed(haut)))
    n = len(contour)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((bas[i], bas[j], haut[j], haut[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return objet(nom, bm, [mat], parent, lisse=True, angle=35, biseau=biseau, seg=2)


def etincelle(nom, centre, vers_camera, taille, mat, parent):
    """Petit éclat à 4 branches (losange étoilé plat) tourné vers la caméra."""
    bm = bmesh.new()
    pts = []
    for k in range(8):                    # 4 pointes longues, 4 creux courts, en alternance
        r = taille if k % 2 == 0 else taille * 0.22
        a = k * math.pi / 4
        pts.append((r * math.cos(a), r * math.sin(a)))
    c = bm.verts.new((0, 0, 0))
    vs = [bm.verts.new((x, y, 0)) for x, y in pts]
    for i in range(8):
        bm.faces.new((c, vs[i], vs[(i + 1) % 8]))
    o = objet(nom, bm, [mat], parent, lisse=False)
    o.location = centre
    o.rotation_euler = Vector(vers_camera).to_track_quat("Z", "Y").to_euler()
    return o


def vers_camera_icone(elevation=25.0, rotation=20.0):
    """Direction de l'objet vers la caméra des icônes (rendu_icones.py : 25° au-dessus,
    tournée de 20°)."""
    el, az = math.radians(elevation), math.radians(rotation)
    return Vector((math.sin(az) * math.cos(el), math.cos(az) * math.cos(el), math.sin(el)))


# ---------------------------------------------------------------------------------------
# Icône 3 : le sablier « Bientôt disponible »
# ---------------------------------------------------------------------------------------
H_SABLIER = 1.5          # hauteur totale
H_PLATEAU = 0.16         # épaisseur d'un plateau de bois


def construire_sablier():
    """Sablier jouet : deux plateaux de bois, trois colonnes de laiton, deux ampoules de verre
    blanc-lavande OPAQUE (la fiche l'exige : un verre transparent laisserait voir le fond) et
    du sable aqua (la couleur du diamant). Le sable est une « peau » posée juste sur le verre :
    on le voit comme à travers une vitre teintée, sans aucune transparence à détourer."""
    C.nouvelle_scene()
    racine = C.vide("Sablier")
    bois = matiere("Bois", "#8C5933", 0.6, vernis=0.08)
    bois_f = matiere("BoisFonce", "#6B4226", 0.6, vernis=0.08)
    laiton = matiere("Laiton", "#C99A1E", 0.3, metal=0.75)
    verre = matiere("Verre", "#F4F0FF", 0.12, vernis=0.9)
    sable = matiere("Sable", "#4FD8F0", 0.35, emission=0.15)
    blanc = matiere("Eclat", "#FFFFFF", 0.3, emission=3.0)

    # Plateaux : galette au bord arrondi, tranche plus foncée.
    plateau = [(0, 0), (0.50, 0), (0.56, 0.025), (0.585, 0.08), (0.56, 0.135), (0.50, H_PLATEAU),
               (0, H_PLATEAU)]
    for nom, z in (("Plateau_Bas", 0.0), ("Plateau_Haut", H_SABLIER - H_PLATEAU)):
        o = revolution(nom, plateau, [1, 1, 1, 1, 0, 0], [bois, bois_f], racine, segments=32)
        o.location.z = z

    # Trois colonnes de laiton avec une boule à chaque bout.
    L = H_SABLIER - 2 * H_PLATEAU
    for k, a in enumerate((90, 210, 330)):
        x, y = 0.44 * math.cos(math.radians(a)), 0.44 * math.sin(math.radians(a))
        o = revolution(f"Colonne_{k}", [(0, 0), (0.06, 0), (0.06, L), (0, L)], [0, 0, 0],
                       [laiton], racine, segments=12)
        o.location = (x, y, H_PLATEAU)
        for j, z in enumerate((H_PLATEAU + 0.02, H_SABLIER - H_PLATEAU - 0.02)):
            bm = bmesh.new()
            C.bm_sphere(bm, 0.085, (x, y, z), segments=12, anneaux=8)
            objet(f"Boule_{k}_{j}", bm, [laiton], racine)

    # Ampoules de verre : profil du bas, puis le même renversé pour le haut.
    z0, zc = H_PLATEAU, H_SABLIER / 2
    demi = [(0.15, z0), (0.29, z0 + 0.05), (0.37, z0 + 0.16), (0.36, z0 + 0.30), (0.25, z0 + 0.44),
            (0.10, zc - 0.05), (0.055, zc)]
    profil_verre = demi + [(r, H_SABLIER - z) for r, z in reversed(demi[:-1])]
    revolution("Verre", profil_verre, [0] * (len(profil_verre) - 1), [verre], racine, segments=32)

    # Sable : peau aqua juste au-dessus du verre (rayon × 1,006) là où il y a du sable.
    def peau(nom, z_bas, z_haut):
        pts = [(r * 1.006 + 0.002, z) for r, z in profil_verre if z_bas - 1e-6 <= z <= z_haut + 1e-6]
        return revolution(nom, pts, [0] * (len(pts) - 1), [sable], racine, segments=32)

    # bas : le quart inférieur (une partie est cachée par le plateau) ; haut : la moitié basse de l'ampoule du haut + le goulot (le
    # filet de sable qui tombe), pour que le sable relie les deux ampoules.
    pts_bas = [p for p in profil_verre if p[1] <= z0 + 0.31]
    # on coupe pile à mi-hauteur de la bande : un point interpolé au bon endroit
    peau("Sable_Bas", z0, pts_bas[-1][1])
    peau("Sable_Haut", zc - 0.05, H_SABLIER - (z0 + 0.30))

    # Un petit éclat blanc sur l'ampoule du haut (côté éclairé, en haut à gauche).
    cam = vers_camera_icone()
    gauche = Vector((cam.y, -cam.x, 0)).normalized()          # gauche de l'image
    p = Vector((0, 0, H_SABLIER - (z0 + 0.25))) + (cam * 0.36 + gauche * 0.16).normalized() * 0.37
    etincelle("Eclat", p + cam * 0.02, cam, 0.09, blanc, racine)

    # « Comme s'il venait d'être retourné » : penché de 6° vers la droite de l'image.
    racine.rotation_euler = (0, 0, 0)
    bpy.context.view_layer.update()
    axe = cam.copy()
    axe.z = 0
    racine.rotation_euler = (Matrix.Rotation(math.radians(-6), 4, axe.normalized())).to_euler()
    bpy.context.view_layer.update()
    return racine


# ---------------------------------------------------------------------------------------
# Icône 4 : le 4e pack de diamants, le plus gros
# ---------------------------------------------------------------------------------------
def construire_pack_tresor():
    """Une montagne de 22 diamants du jeu (le VRAI modèle, blender/diamant.py) sur un socle de
    pierre bleu-gris cerclé de laiton, avec une auréole de lumière dorée au-dessus (pas une
    couronne : ce serait un accessoire du jeu). Spec v5 « un gros tas de diamants avec une
    couronne de lumière »."""
    import diamant as D
    D.construire()                       # repart d'une scène neuve et crée l'objet « Diamant »
    modele = bpy.data.objects["Diamant"]
    racine = C.vide("PackTresor")
    pierre = matiere("Pierre", "#8A90B0", 0.6)
    pierre_f = matiere("PierreFoncee", "#5B6380", 0.6)
    laiton = matiere("Laiton", "#C99A1E", 0.3, metal=0.75)
    or_ = matiere("Aureole", "#FFC83A", 0.25, metal=0.3, emission=1.6)
    blanc = matiere("Eclat", "#FFFFFF", 0.3, emission=3.0)

    # Socle bas et large, tranche plus foncée, cerclé de laiton.
    R, H = 1.9, 0.34
    socle = [(0, 0), (R - 0.06, 0), (R, 0.06), (R, H - 0.06), (R - 0.06, H), (0, H)]
    revolution("Socle", socle, [1, 1, 1, 0, 0], [pierre, pierre_f], racine, segments=40)
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=False, segments=40, radius1=R + 0.03, radius2=R + 0.03,
                          depth=0.09)
    bmesh.ops.translate(bm, vec=(0, 0, H - 0.04), verts=bm.verts)
    bmesh.ops.solidify(bm, geom=bm.faces[:], thickness=0.05)
    objet("Cercle", bm, [laiton], racine)

    # La montagne : 22 diamants en couches (1 au centre + 9 + 7 + 4 + 1 au sommet), penchés.
    hasard = random.Random(3)
    k = 0.55                                   # taille d'un diamant (rayon de la ceinture)
    places = [(0.0, 0.0, 0.0)]
    for n, r, z in ((9, 1.28, 0.0), (7, 0.80, 0.40), (4, 0.38, 0.80)):
        for i in range(n):
            a = 2 * math.pi * (i + 0.5 * (n % 2)) / n + hasard.uniform(-0.15, 0.15)
            places.append((r * math.cos(a), r * math.sin(a), z))
    places.append((0.0, 0.05, 1.22))
    for i, (x, y, z) in enumerate(places):
        g = bpy.data.objects.new(f"Gemme_{i:02d}", modele.data)     # copie liée (même maillage)
        bpy.context.scene.collection.objects.link(g)
        g.scale = (k, k, k)
        g.location = (x, y, H + 0.32 + z)
        penche = 0.0 if i == len(places) - 1 else 0.55
        g.rotation_euler = (hasard.uniform(-penche, penche), hasard.uniform(-penche, penche),
                            hasard.uniform(0, math.pi))
        C.parenter(g, racine)
    bpy.data.objects.remove(modele)

    # Auréole : un anneau épais qui flotte au-dessus du tas, et 4 petits éclats autour.
    z_aureole = H + 0.32 + 1.22 + 0.75
    bm = bmesh.new()
    rangs = []
    for i in range(32):
        a = 2 * math.pi * i / 32
        centre = Vector((math.cos(a) * 0.70, math.sin(a) * 0.70, 0))
        rangs.append([centre + Vector((math.cos(a) * math.cos(b), math.sin(a) * math.cos(b),
                                       math.sin(b))) * 0.11
                      for b in (2 * math.pi * j / 10 for j in range(10))])
    C.bm_anneaux(bm, rangs + [rangs[0]], fermer=True)
    aureole = objet("Aureole", bm, [or_], racine, angle=180)
    aureole.location.z = z_aureole
    cam = vers_camera_icone()
    for i, (dx, dz) in enumerate(((-1.05, 0.25), (1.0, 0.35), (-0.75, -0.35), (0.85, -0.25))):
        gauche = Vector((cam.y, -cam.x, 0)).normalized()
        p = Vector((0, 0, z_aureole + dz)) - gauche * dx + cam * 0.3
        etincelle(f"Eclat_{i}", p, cam, 0.16 if i < 2 else 0.11, blanc, racine)
    bpy.context.view_layer.update()
    return racine


# ---------------------------------------------------------------------------------------
# Décoration 1 : la guirlande de fanions (vue de face, 1600 × 400, 1 unité = 100 px)
# ---------------------------------------------------------------------------------------
COULEURS_FANIONS = ["#FF5FA2", "#FFD54A", "#3BC8FF", "#6BE34A", "#A35CE0",
                    "#FF5FA2", "#FFD54A", "#3BC8FF", "#6BE34A"]


def corde(x, x0=7.25, z_bout=1.55, z_bas=0.20):
    """Hauteur de la corde (arc en parabole) : z_bout aux bouts, z_bas au milieu."""
    t = x / x0
    return z_bas + (z_bout - z_bas) * t * t


def construire_guirlande():
    """9 fanions épais de jouet, suspendus à une grosse corde de laiton en arc doux, deux
    nœuds ronds aux bouts. Le point bas de la corde est à 45 % de la hauteur (fiche lot 9)."""
    C.nouvelle_scene()
    racine = C.vide("Guirlande")
    laiton = matiere("Corde", "#C99A1E", 0.45, metal=0.35)
    blanc = matiere("Pois", "#FFFFFF", 0.35)
    x0 = 7.25
    # La corde : une courbe épaissie (bevel) qui suit la parabole.
    cd = bpy.data.curves.new("Corde", "CURVE")
    cd.dimensions = "3D"
    cd.bevel_depth = 0.10
    cd.bevel_resolution = 4
    sp = cd.splines.new("POLY")
    n = 60
    sp.points.add(n - 1)
    for i in range(n):
        x = -x0 + 2 * x0 * i / (n - 1)
        sp.points[i].co = (x, -0.05, corde(x), 1)
    co = bpy.data.objects.new("Corde", cd)
    cd.materials.append(laiton)
    bpy.context.scene.collection.objects.link(co)
    C.parenter(co, racine)
    for s in (-1, 1):                                       # nœuds aux deux bouts
        bm = bmesh.new()
        C.bm_sphere(bm, 0.19, (s * x0, -0.05, corde(x0)), segments=16, anneaux=10)
        objet(f"Noeud_{'G' if s < 0 else 'D'}", bm, [laiton], racine)

    # Les fanions : triangles épais (1,15 × 1,35), pointe en bas, accrochés sous la corde.
    hasard = random.Random(5)
    for i, hexa in enumerate(COULEURS_FANIONS):
        x = -6.2 + i * 1.55
        z = corde(x) - 0.06
        pente = math.atan(2 * (1.55 - 0.20) * x / (x0 * x0))   # inclinaison de la corde
        f = prisme(f"Fanion_{i + 1}", [(-0.58, 0.0), (0.58, 0.0), (0.0, -1.35)], 0.16,
                   matiere(f"Fanion_{i + 1}", hexa, 0.45), racine, biseau=0.05, y=-0.08)
        f.location = (x, 0, z)
        # suit la pente de la corde, plus un petit balancement différent pour chacun
        f.rotation_euler = (hasard.uniform(-0.12, 0.12), -pente + hasard.uniform(-0.08, 0.08),
                            hasard.uniform(-0.18, 0.18))
        bm = bmesh.new()
        C.bm_sphere(bm, 0.13, (0, -0.10, -0.33), segments=16, anneaux=8, echelle=(1, 0.45, 1))
        pois = objet(f"Pois_{i + 1}", bm, [blanc], None)
        pois.parent = f
    bpy.context.view_layer.update()
    return racine


# ---------------------------------------------------------------------------------------
# Décoration 2 : le ruban de palier (vue de face, 1200 × 400, 1 unité = 100 px, 9-slice)
# ---------------------------------------------------------------------------------------
def matiere_degrade(nom, bas, milieu, haut, rugosite=0.35):
    """Or en dégradé VERTICAL (bord bas plus foncé, bord haut plus clair). Il ne varie pas de
    gauche à droite : le milieu du ruban peut être étiré (9-slice) sans que ça se voie."""
    m = matiere(nom, milieu, rugosite, metal=0.2)
    nt = m.node_tree
    coord = nt.nodes.new("ShaderNodeTexCoord")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    rampe = nt.nodes.new("ShaderNodeValToRGB")
    e = rampe.color_ramp.elements
    e[0].position, e[0].color = 0.0, C.hex_vers_lineaire(bas)
    e[1].position, e[1].color = 1.0, C.hex_vers_lineaire(haut)
    for pos in (0.22, 0.78):
        e.new(pos).color = C.hex_vers_lineaire(milieu)
    nt.links.new(coord.outputs["Generated"], sep.inputs["Vector"])
    nt.links.new(sep.outputs["Z"], rampe.inputs["Fac"])
    nt.links.new(rampe.outputs["Color"], nt.nodes["Principled BSDF"].inputs["Base Color"])
    return m


def construire_ruban():
    """Bande d'or droite et vide (on écrira le nom du palier dessus dans le jeu), repliée aux
    deux bouts en une courte queue plus foncée à queue d'aronde, un peu plus bas que la bande.
    Les queues restent dans les 120 premiers / derniers pixels (bords du 9-slice)."""
    C.nouvelle_scene()
    racine = C.vide("Ruban")
    bande = matiere_degrade("Bande", "#D99A1A", "#FFC83A", "#FFE38A")
    queue = matiere("Queue", "#D99A1A", 0.4, metal=0.15)
    pli = matiere("Pli", "#B07A10", 0.45, metal=0.1)
    X = 4.95                                   # bout de la bande : 495 px du centre (105 px du bord)
    bm = bmesh.new()
    C.bm_boite(bm, (2 * X, 0.30, 1.50), (0, 0, 0))
    objet("Bande", bm, [bande], racine, lisse=True, angle=35, biseau=0.07, seg=3)
    for s in (-1, 1):
        # queue derrière la bande : de 482 à 582 px du centre, pointe fourchue vers l'extérieur
        contour = [(4.82, 0.35), (5.82, 0.35), (5.40, -0.40), (5.82, -1.15), (4.82, -1.15)]
        if s < 0:
            contour = [(-x, z) for x, z in reversed(contour)]
        prisme(f"Queue_{'G' if s < 0 else 'D'}", contour, 0.22, queue, racine, biseau=0.05, y=0.10)
        # le pli : petit triangle plus sombre là où la bande passe derrière elle-même
        tri = [(4.82, -0.75), (X, -0.75), (X, -1.15)] if s > 0 else [(-X, -1.15), (-X, -0.75), (-4.82, -0.75)]
        prisme(f"Pli_{'G' if s < 0 else 'D'}", tri, 0.20, pli, racine, biseau=0.0, y=-0.02)
    bpy.context.view_layer.update()
    return racine
