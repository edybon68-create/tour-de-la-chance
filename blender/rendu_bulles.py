"""
rendu_bulles.py — Les 25 « bulles » des boutons (v6, art bible §15.1), rendues dans Blender.

Pourquoi des images Blender ? En USS pur, UI Toolkit ne sait faire ni dégradé rond, ni flou,
ni lumière : les anciennes bulles faisaient « trou plat » (bible §15.1.1). Ici, chaque bulle
est un vrai petit jeton en 3D (face bombée, socle, contour d'encre) photographié de face,
sous la même lumière que les icônes du lot 6 (`rendu_icones.py`). AUCUNE icône dedans :
l'interface pose l'icône (ou la croix, ou le texte) par-dessus.

Lancer (Blender n'a pas besoin d'être ouvert) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/rendu_bulles.py
Seulement quelques bulles (le nom du fichier sans « bulle_ » ni « .png ») :
    BULLES="rond_boutique rect_vert_appui" /Applications/Blender.app/.../Blender --background --python ...
Sans refaire la planche de contrôle : PLANCHE=0

Sorties :
    art/bulles/bulle_*.png          (25 images, fond transparent dès le rendu, sans détourage)
    docs/captures/v6_bulles.png     (planche : chaque bulle sur le ciel, avec une icône posée)

COMMENT ÇA MARCHE (une seule fonction : rendre_bulle(forme, couleurs, appui, chemin))
  1. Unité : 1 unité Blender = 1 pixel « d'affichage » (l'image est rendue en ×2).
     Les mesures du §15.1.5 sont donc tapées telles quelles, en pixels, y vers le bas.
  2. Couches de l'arrière vers l'avant (bible §15.1.3) :
       contour (encre #110D1F) → socle (épaisseur) → face bombée → biseau → lueur →
       étoiles (Renaissance) → lumière renvoyée → reflet → éclat.
     Contour et socle sont des plaques plates en ÉMISSION : leur couleur sort exacte.
     La face est un vrai volume bombé : sa couleur de base (dégradé clair → face → mi) est en
     émission, et un Principled verni (rugosité 0,25, Coat 0,6) ajoute la lumière.
     Reflet, biseau, lueur, éclat : plaques blanches dont la transparence est calculée dans le
     shader à partir de la FORME exacte (fonction de distance) : bords doux garantis.
  3. Lumières : soleils (lumière « à l'infini »). Pourquoi des soleils et pas des lampes de
     zone comme pour les icônes ? Un soleil éclaire pareil partout : la partie centrale d'une
     bulle rectangulaire est donc identique d'une colonne à l'autre (obligatoire pour le
     9-slice). Mêmes couleurs et mêmes directions que les icônes : chaude #FFF1D6 en haut à
     gauche, remplissage violet, liseré lavande #B48CFF.
  4. L'ombre portée est ajoutée juste après le rendu : silhouette du contour, décalée vers le
     bas et floutée (même travail que le nœud Blur du compositeur, mais au pixel près).
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

DOSSIER_BULLES = os.environ.get("SORTIE") or os.path.join(C.DOSSIER_PROJET, "art", "bulles")
DOSSIER_ICONES = os.path.join(C.DOSSIER_PROJET, "art", "icones_detourees")
PLANCHE = os.path.join(C.DOSSIER_CAPTURES, "v6_bulles.png")
ECHELLE = 2            # l'image est rendue à 2 fois la taille d'affichage (bible §15.1.5)
ENCRE = "#110D1F"      # le trait d'encre du style


# ---------------------------------------------------------------------------------------
# 0. Les couleurs (bible §15.1.4) : clair (haut), face, mi (bas), épaisseur
# ---------------------------------------------------------------------------------------
COULEURS = {
    "boutique":      ("#FF8FB8", "#FF4F8B", "#E0336F", "#A3214F"),
    "inventaire":    ("#7DB5FF", "#3B7DD8", "#2F68BE", "#1F4A8C"),
    "index":         ("#5FE0CF", "#1FB5A3", "#17998A", "#0F6B61"),
    "ameliorations": ("#8BEA6B", "#3DBE4F", "#2EA040", "#1E7030"),
    "classement":    ("#B49CFF", "#7B5BF0", "#6545D6", "#4530A8"),
    "renaissance":   ("#6A5AD8", "#3A2C9A", "#2A1F78", "#17104A"),
    "ardoise":       ("#8A84B8", "#5A5488", "#4A4472", "#2E2A4E"),
    "rouge":         ("#FF8A8A", "#F0484E", "#D83A40", "#9A2228"),
    "or":            ("#FFE66B", "#FFC83A", "#F0A820", "#B87410"),
    "vert":          ("#7BE38F", "#2FB85A", "#26A04D", "#1A6E36"),
    "violet":        ("#A08CFF", "#6C4FE0", "#5A3FCC", "#3F2A9E"),
    "aqua":          ("#8FEAFF", "#29B6E8", "#1E9BD0", "#12689A"),
    "gris":          ("#A8A4BC", "#7E7A94", "#6E6A84", "#4E4A64"),
}


# ---------------------------------------------------------------------------------------
# 1. Les mesures (bible §15.1.5), en pixels d'AFFICHAGE, y vers le bas.
#    Une forme = un rectangle à coins arrondis (x0, y0, x1, y1, rayon des coins) :
#    un disque est simplement un carré dont le rayon des coins vaut la moitié du côté.
# ---------------------------------------------------------------------------------------
def disque(cx, cy, d):
    r = d / 2
    return (cx - r, cy - r, cx + r, cy + r, r)


def mesures(forme, appui):
    """Toutes les mesures d'une bulle. `appui` = version enfoncée (la face descend vers le
    socle, qui ne bouge pas : on voit moins d'épaisseur)."""
    if forme == "rond":                                   # grande bulle des colonnes
        dy = 6 if appui else 0
        return dict(
            taille=(160, 160),
            face=disque(80, 70 + dy, 112),
            socle=disque(80, 80, 112),
            contour=6,
            ombre=(3, 4) if appui else (6, 8),            # (décalage, flou)
            ombre_opacite=0.35,
            reflet=("ellipse", 80, 38 + dy, 76, 34),       # (genre, centre x, y, largeur, hauteur)
            reflet_alpha=(0.40, 0.0) if appui else (0.60, 0.0),
            eclat=("ellipse", 52, 34 + dy, 8 if appui else 10, 8 if appui else 10),
            biseau=4, renvoyee=3, lueur=(80, 70 + dy, 44),
            hauteur_dome=0.20 * 56, largeur_dome=56,
        )
    if forme == "petit":                                  # croix, roue dentée, flèches, Route
        dy = 5 if appui else 0
        return dict(
            taille=(96, 96),
            face=disque(48, 40 + dy, 62),
            socle=disque(48, 48, 62),
            contour=5,
            ombre=(2, 3) if appui else (5, 6),
            ombre_opacite=0.30,
            reflet=("ellipse", 48, 23 + dy, 42, 18),
            reflet_alpha=(0.40, 0.0) if appui else (0.60, 0.0),
            eclat=("ellipse", 33, 22 + dy, 6, 6),
            biseau=3, renvoyee=2, lueur=(48, 40 + dy, 24),
            hauteur_dome=0.20 * 31, largeur_dome=31,
        )
    if forme == "rect":                                   # Encaisser, Rejouer, Continuer
        dy = 6 if appui else 0
        return dict(
            taille=(200, 120),
            face=(12, 9 + dy, 188, 95 + dy, 24),
            socle=(12, 19, 188, 105, 24),
            contour=5,
            ombre=(3, 3) if appui else (6, 6),
            ombre_opacite=0.35,
            # bande x 22 → 178, y 13 → 44, coins 16 px
            reflet=("rect", 100, 28.5 + dy, 156, 31, 16),
            reflet_alpha=(0.35, 0.04) if appui else (0.50, 0.06),
            # gélule 16 × 7 entre x 22 et 38, y 17 → 24
            eclat=("rect", 30, 20.5 + dy, 16, 7, 3.5),
            biseau=4, renvoyee=3, lueur=None,
            # la lumière renvoyée : trait y 91 → 94 (appui 97 → 100), de x 30 à 170
            renvoyee_rect=(30, 91 + dy, 170, 94 + dy),
            hauteur_dome=0.20 * 43, largeur_dome=26,
        )
    raise ValueError(forme)


# ---------------------------------------------------------------------------------------
# 2. Outils de géométrie (pixels d'affichage → coordonnées Blender)
# ---------------------------------------------------------------------------------------
class Repere:
    """Passe des pixels d'affichage (origine en haut à gauche, y vers le bas) aux
    coordonnées Blender (origine au centre de l'image, Y vers le haut)."""

    def __init__(self, w, h):
        self.w, self.h = w, h

    def p(self, x, y):
        return (x - self.w / 2, self.h / 2 - y)


def contour_arrondi(x0, y0, x1, y1, r, n=32, avec_normales=False):
    """Points (en pixels d'affichage) du tour d'un rectangle à coins arrondis, dans le sens
    des aiguilles d'une montre à l'écran. Chaque coin a toujours `n` points, même quand le
    rayon devient nul : ainsi deux contours successifs se relient point à point.
    `avec_normales` : renvoie aussi, pour chaque point, la direction « vers l'extérieur »."""
    r = max(0.0, min(r, (x1 - x0) / 2, (y1 - y0) / 2))
    coins = [  # (centre du coin, angle de départ) — angles « écran » (y vers le bas)
        ((x1 - r, y0 + r), -90), ((x1 - r, y1 - r), 0),
        ((x0 + r, y1 - r), 90), ((x0 + r, y0 + r), 180),
    ]
    pts, dirs = [], []
    for (cx, cy), a0 in coins:
        for i in range(n):
            a = math.radians(a0 + 90 * i / (n - 1))
            pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
            dirs.append((math.cos(a), math.sin(a)))
    return (pts, dirs) if avec_normales else pts


def objet_maillage(nom, verts, faces, collection):
    me = bpy.data.meshes.new(nom)
    me.from_pydata(verts, [], faces)
    me.update()
    o = bpy.data.objects.new(nom, me)
    collection.objects.link(o)
    return o


def plaque(nom, forme, z, rep, collection, marge=0.0):
    """Plaque plate (un seul polygone) de la forme donnée, agrandie de `marge` pixels."""
    x0, y0, x1, y1, r = forme
    pts = contour_arrondi(x0 - marge, y0 - marge, x1 + marge, y1 + marge, r + marge)
    verts = [(*rep.p(x, y), z) for x, y in pts]
    return objet_maillage(nom, verts, [list(range(len(verts)))[::-1]], collection)


def face_bombee(nom, forme, hauteur, largeur, rep, collection, anneaux=28):
    """La face : la forme, gonflée comme un coussin. Chaque anneau est la forme rétrécie de
    t pixels, posé à la hauteur z = h·(1 − (1 − t/largeur)²) : pente au bord, plat au centre.
    Pour un disque (largeur = rayon), on obtient une calotte (bible : 20 % du rayon).
    Les normales (ce qui décide de la lumière) sont CALCULÉES à partir de cette formule, et
    non devinées par Blender : sinon les points confondus des coins dessinent des coutures."""
    x0, y0, x1, y1, r = forme
    tmax = min(largeur, (x1 - x0) / 2 - 0.5, (y1 - y0) / 2 - 0.5)
    verts, normales, faces, n_par_anneau = [], [], [], None
    for k in range(anneaux + 1):
        # anneaux serrés près du bord (là où la pente change vite)
        t = tmax * (1 - math.cos(math.pi / 2 * k / anneaux))
        u = min(t / largeur, 1.0)
        z = hauteur * (1 - (1 - u) ** 2)
        pente = 2 * hauteur * (1 - u) / largeur        # dz/dt : montée vers l'intérieur
        pts, dirs = contour_arrondi(x0 + t, y0 + t, x1 - t, y1 - t, r - t, avec_normales=True)
        if n_par_anneau is None:
            n_par_anneau = len(pts)
        base = len(verts)
        verts += [(*rep.p(x, y), z) for x, y in pts]
        for dx, dy in dirs:            # direction écran (y bas) → Blender (Y haut)
            nv = Vector((dx * pente, -dy * pente, 1.0)).normalized()
            normales.append(nv)
        if k > 0:
            prec = base - n_par_anneau
            for i in range(n_par_anneau):
                j = (i + 1) % n_par_anneau
                faces.append([prec + i, base + i, base + j, prec + j])
    # le centre : un polygone plat qui ferme le dernier anneau
    faces.append(list(range(len(verts) - n_par_anneau, len(verts)))[::-1])
    o = objet_maillage(nom, verts, faces, collection)
    for poly in o.data.polygons:
        poly.use_smooth = True
    o.data.normals_split_custom_set_from_vertices(normales)
    return o


def etoile(nom, cx, cy, taille, z, rep, collection):
    """Petite étoile à 4 branches (ciel de la Renaissance)."""
    verts = [(*rep.p(cx, cy), z)]
    for i in range(16):
        a = math.radians(-90 + i * 22.5)
        # rayon : pointe (branches), creux, et un petit arrondi entre les deux
        rr = taille / 2 if i % 4 == 0 else (taille * 0.14 if i % 4 == 2 else taille * 0.18)
        verts.append((*rep.p(cx + rr * math.cos(a), cy + rr * math.sin(a)), z))
    faces = [[0, 1 + (i + 1) % 16, 1 + i] for i in range(16)]
    return objet_maillage(nom, verts, faces, collection)


# ---------------------------------------------------------------------------------------
# 3. Matériaux
# ---------------------------------------------------------------------------------------
def couleur(hexa, assombrir=0.0):
    """Couleur linéaire (ce qu'attend Blender) ; `assombrir` = 0.06 pour « 6 % plus sombre »
    (appliqué sur la valeur sRGB, celle qu'on voit)."""
    hexa = hexa.lstrip("#")
    srgb = [int(hexa[i:i + 2], 16) / 255 * (1 - assombrir) for i in (0, 2, 4)]
    lin = [c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in srgb]
    return (*lin, 1.0)


def mat_emission(nom, hexa, alpha=1.0):
    """Couleur exacte, insensible à la lumière (contour, socle, étoiles)."""
    m = bpy.data.materials.new(nom)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    sortie = nt.nodes.new("ShaderNodeOutputMaterial")
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Color"].default_value = couleur(hexa)
    if alpha >= 1.0:
        nt.links.new(em.outputs[0], sortie.inputs["Surface"])
    else:
        tr = nt.nodes.new("ShaderNodeBsdfTransparent")
        mix = nt.nodes.new("ShaderNodeMixShader")
        mix.inputs[0].default_value = alpha
        nt.links.new(tr.outputs[0], mix.inputs[1])
        nt.links.new(em.outputs[0], mix.inputs[2])
        nt.links.new(mix.outputs[0], sortie.inputs["Surface"])
        regler_transparence(m)
    return m


def regler_transparence(m):
    """Transparence douce (mélange) plutôt que tramée."""
    if hasattr(m, "surface_render_method"):
        m.surface_render_method = "BLENDED"
    else:
        m.blend_method = "BLEND"


def mat_face(nom, clair, face, mi, haut, bas, assombrir):
    """La face : dégradé vertical clair (haut) → face (55 %) → mi (bas) en émission, assombri
    de 8 % sur la pente du bord (bombé), + un vernis Principled qui reçoit les lumières.
    `haut` et `bas` = Y Blender du haut et du bas de la face."""
    m = bpy.data.materials.new(nom)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    N, L = nt.nodes, nt.links
    sortie = N.new("ShaderNodeOutputMaterial")
    geo = N.new("ShaderNodeNewGeometry")
    sep = N.new("ShaderNodeSeparateXYZ")
    L.new(geo.outputs["Position"], sep.inputs[0])
    # position verticale 0 (haut) → 1 (bas)
    carte = N.new("ShaderNodeMapRange")
    carte.inputs["From Min"].default_value = haut
    carte.inputs["From Max"].default_value = bas
    L.new(sep.outputs["Y"], carte.inputs["Value"])
    rampe = N.new("ShaderNodeValToRGB")
    els = rampe.color_ramp.elements
    els[0].position, els[0].color = 0.0, couleur(clair, assombrir)
    els[1].position, els[1].color = 1.0, couleur(mi, assombrir)
    e = els.new(0.55)
    e.color = couleur(face, assombrir)
    L.new(carte.outputs["Result"], rampe.inputs["Fac"])
    # bombé : la pente (normale qui penche) assombrit jusqu'à 8 %
    sepn = N.new("ShaderNodeSeparateXYZ")
    L.new(geo.outputs["Normal"], sepn.inputs[0])
    pente = N.new("ShaderNodeMapRange")
    pente.inputs["From Min"].default_value = 0.80
    pente.inputs["From Max"].default_value = 1.0
    pente.inputs["To Min"].default_value = 0.92
    pente.inputs["To Max"].default_value = 1.0
    L.new(sepn.outputs["Z"], pente.inputs["Value"])
    mul = N.new("ShaderNodeMix")
    mul.data_type = "RGBA"
    mul.blend_type = "MULTIPLY"
    mul.inputs["Factor"].default_value = 1.0
    # (le nœud Mix a plusieurs entrées « A » : celles de type couleur sont les n° 6 et 7)
    L.new(rampe.outputs["Color"], mul.inputs[6])
    L.new(pente.outputs["Result"], mul.inputs[7])
    em = N.new("ShaderNodeEmission")
    em.inputs["Strength"].default_value = 0.86
    L.new(mul.outputs[2], em.inputs["Color"])
    # le vernis : il reçoit la clé chaude (reflet de lampe) et le liseré lavande
    bsdf = N.new("ShaderNodeBsdfPrincipled")
    L.new(mul.outputs[2], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.25
    bsdf.inputs["Coat Weight"].default_value = 0.6
    bsdf.inputs["Coat Roughness"].default_value = 0.25
    bsdf.inputs["Specular IOR Level"].default_value = 0.35
    add = N.new("ShaderNodeAddShader")
    L.new(em.outputs[0], add.inputs[0])
    L.new(bsdf.outputs[0], add.inputs[1])
    L.new(add.outputs[0], sortie.inputs["Surface"])
    return m


class Noeuds:
    """Petits raccourcis pour écrire des calculs dans un shader (une ligne par opération)."""

    def __init__(self, nt):
        self.nt, self.N, self.L = nt, nt.nodes, nt.links

    def math(self, op, a, b=None, clamp=False):
        n = self.N.new("ShaderNodeMath")
        n.operation = op
        n.use_clamp = clamp
        for i, v in enumerate((a, b)):
            if v is None:
                continue
            if isinstance(v, (int, float)):
                n.inputs[i].default_value = v
            else:
                self.L.new(v, n.inputs[i])
        return n.outputs[0]

    def vmath(self, op, a, b=None):
        n = self.N.new("ShaderNodeVectorMath")
        n.operation = op
        for i, v in enumerate((a, b)):
            if v is None:
                continue
            if isinstance(v, (tuple, list)):
                n.inputs[i].default_value = v
            else:
                self.L.new(v, n.inputs[i])
        return n.outputs["Value"] if op in ("LENGTH", "DOT_PRODUCT") else n.outputs["Vector"]

    def lisse(self, x, bord0, bord1):
        """0 avant bord0, 1 après bord1, transition douce (smoothstep) entre les deux."""
        n = self.N.new("ShaderNodeMapRange")
        n.interpolation_type = "SMOOTHSTEP"
        n.inputs["From Min"].default_value = bord0
        n.inputs["From Max"].default_value = bord1
        self.L.new(x, n.inputs["Value"])
        return n.outputs["Result"]

    def separer(self, v):
        n = self.N.new("ShaderNodeSeparateXYZ")
        self.L.new(v, n.inputs[0])
        return n.outputs


def distance_forme(nd, coord, genre, demi_l, demi_h, r=0.0):
    """Distance (en pixels) au bord de la forme : négative dedans, positive dehors.
    `coord` = coordonnées locales de la plaque (en pixels, origine au centre de la forme)."""
    if genre == "ellipse":
        # approximation classique : on « écrase » l'ellipse en cercle unité
        p = nd.vmath("DIVIDE", coord, (demi_l, demi_h, 1.0))
        x, y, _ = nd.separer(p)
        lg = nd.math("SQRT", nd.math("ADD", nd.math("MULTIPLY", x, x), nd.math("MULTIPLY", y, y)))
        return nd.math("MULTIPLY", nd.math("SUBTRACT", lg, 1.0), min(demi_l, demi_h))
    # rectangle à coins arrondis
    q = nd.vmath("SUBTRACT", nd.vmath("ABSOLUTE", coord), (demi_l - r, demi_h - r, 0.0))
    qx, qy, _ = nd.separer(q)
    dehors = nd.vmath("LENGTH", nd.vmath("MAXIMUM", nd.vmath("MULTIPLY", q, (1, 1, 0)), (0, 0, 0)))
    dedans = nd.math("MINIMUM", nd.math("MAXIMUM", qx, qy), 0.0)
    return nd.math("SUBTRACT", nd.math("ADD", dehors, dedans), r)


def mat_douce(nom, construire_alpha, couleur_hex="#FFFFFF", couleur_bas=None):
    """Plaque d'émission dont la transparence est calculée par `construire_alpha(nd, coord)`
    (coord = position locale en pixels). Si `couleur_bas` est donnée, la couleur passe de
    `couleur_hex` (moitié haute) à `couleur_bas` (moitié basse)."""
    m = bpy.data.materials.new(nom)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    nd = Noeuds(nt)
    sortie = nd.N.new("ShaderNodeOutputMaterial")
    tc = nd.N.new("ShaderNodeTexCoord")
    coord = tc.outputs["Object"]
    alpha = construire_alpha(nd, coord)
    em = nd.N.new("ShaderNodeEmission")
    if couleur_bas is None:
        em.inputs["Color"].default_value = couleur(couleur_hex)
    else:
        _, y, _ = nd.separer(coord)
        mix = nd.N.new("ShaderNodeMix")
        mix.data_type = "RGBA"
        mix.inputs[6].default_value = couleur(couleur_hex)     # entrées couleur A et B
        mix.inputs[7].default_value = couleur(couleur_bas)
        f = nd.lisse(y, 6.0, -6.0)      # haut → bas, transition au milieu
        nd.L.new(f, mix.inputs["Factor"])
        nd.L.new(mix.outputs[2], em.inputs["Color"])
    tr = nd.N.new("ShaderNodeBsdfTransparent")
    mixs = nd.N.new("ShaderNodeMixShader")
    nd.L.new(alpha, mixs.inputs[0])
    nd.L.new(tr.outputs[0], mixs.inputs[1])
    nd.L.new(em.outputs[0], mixs.inputs[2])
    nd.L.new(mixs.outputs[0], sortie.inputs["Surface"])
    regler_transparence(m)
    return m


def plaque_rect(nom, cx, cy, demi_l, demi_h, z, rep, collection, mat):
    """Plaque rectangulaire centrée en (cx, cy) ; son origine est au centre (coordonnées
    locales = pixels autour du centre de la forme, ce qu'utilise mat_douce)."""
    verts = [(-demi_l, -demi_h, 0), (demi_l, -demi_h, 0), (demi_l, demi_h, 0), (-demi_l, demi_h, 0)]
    o = objet_maillage(nom, verts, [[0, 1, 2, 3]], collection)
    o.location = (*rep.p(cx, cy), z)
    o.data.materials.append(mat)
    return o


# ---------------------------------------------------------------------------------------
# 4. Scène : moteur, caméra, lumières
# ---------------------------------------------------------------------------------------
def scene_neuve(w, h):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.eevee.taa_render_samples = 64
    # Pas de reflets « écran » (raytracing, occlusion) : ils dépendent de ce qui est à côté
    # dans l'image et rendraient les colonnes du 9-slice légèrement différentes.
    for attr, val in (("use_raytracing", False), ("use_gtao", False),
                      ("use_shadows", False), ("use_fast_gi", False)):
        if hasattr(scene.eevee, attr):
            setattr(scene.eevee, attr, val)
    scene.render.resolution_x = w * ECHELLE
    scene.render.resolution_y = h * ECHELLE
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.filter_size = 1.2
    scene.view_settings.view_transform = "Standard"   # le #hex tapé = le #hex obtenu
    scene.view_settings.look = "None"
    scene.view_settings.exposure = 0.0
    scene.view_settings.gamma = 1.0
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.image_settings.color_depth = "8"
    # un monde très faible (le vernis y trouve un léger reflet, sans éclaircir la face)
    monde = bpy.data.worlds.new("MondeBulles")
    monde.use_nodes = True
    monde.node_tree.nodes["Background"].inputs["Color"].default_value = couleur("#8C82A8")
    monde.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.05
    scene.world = monde
    # caméra orthographique de face, au-dessus de la bulle, qui regarde vers le bas (−Z)
    cd = bpy.data.cameras.new("CameraBulle")
    cd.type = "ORTHO"
    cd.ortho_scale = max(w, h)
    cd.sensor_fit = "AUTO"
    cd.clip_start, cd.clip_end = 1, 1000
    cam = bpy.data.objects.new("CameraBulle", cd)
    cam.location = (0, 0, 500)
    scene.collection.objects.link(cam)
    scene.camera = cam
    lumieres(scene)
    return scene


def soleil(scene, nom, hexa, energie, vers_lumiere):
    d = bpy.data.lights.new(nom, "SUN")
    d.color = couleur(hexa)[:3]
    d.energy = energie
    d.angle = math.radians(12)          # soleil un peu large = reflet de vernis doux
    o = bpy.data.objects.new(nom, d)
    o.rotation_euler = (-Vector(vers_lumiere)).normalized().to_track_quat("-Z", "Y").to_euler()
    scene.collection.objects.link(o)


def lumieres(scene):
    """Mêmes trois lumières que rendu_icones.py (bible §5), vues de face.
    Vecteur = direction de la bulle VERS la lampe (X droite, Y haut, Z vers la caméra)."""
    soleil(scene, "Principale", "#FFF1D6", 1.6, (-0.55, 0.65, 0.75))   # chaude, haut gauche
    soleil(scene, "Remplissage", "#B8A8E8", 0.35, (0.8, -0.35, 0.6))   # violet doux, bas droite
    soleil(scene, "Liseré", "#B48CFF", 1.4, (0.35, 0.9, 0.12))         # lavande rasante, haut


# ---------------------------------------------------------------------------------------
# 5. La bulle
# ---------------------------------------------------------------------------------------
def construire_bulle(forme, nom_couleur, appui):
    M = mesures(forme, appui)
    w, h = M["taille"]
    scene = scene_neuve(w, h)
    col = bpy.data.collections.new("Bulle")
    scene.collection.children.link(col)
    rep = Repere(w, h)
    clair, face, mi, epaisseur = COULEURS[nom_couleur]
    sombre = 0.06 if appui else 0.0          # appuyé : face assombrie de 6 %
    fx0, fy0, fx1, fy1, fr = M["face"]
    sx0, sy0, sx1, sy1, sr = M["socle"]
    hd = M["hauteur_dome"]

    # Socle (épaisseur) = la forme de la face « glissée » jusqu'au socle : ainsi l'épaisseur
    # suit exactement la courbe (plus de croissant bancal).
    socle = (fx0, fy0, sx1, sy1, fr)
    c = M["contour"]
    contour = plaque("Contour", socle, -4, rep, col, marge=c)
    contour.data.materials.append(mat_emission("Encre", ENCRE))
    s = plaque("Socle", socle, -2, rep, col)
    s.data.materials.append(mat_emission("Epaisseur", epaisseur))

    # Face bombée
    fo = face_bombee("Face", M["face"], hd, M["largeur_dome"], rep, col)
    haut, bas = rep.p(0, fy0)[1], rep.p(0, fy1)[1]
    fo.data.materials.append(mat_face("Face", clair, face, mi, haut, bas, sombre))

    # Les plaques d'effets, au-dessus du dôme (z croissant = plus devant)
    fcx, fcy = (fx0 + fx1) / 2, (fy0 + fy1) / 2
    fdl, fdh = (fx1 - fx0) / 2, (fy1 - fy0) / 2
    z = hd + 2

    # Biseau : anneau de N px collé au bord ; moitié haute « clair » 70 %, basse « mi » 60 %
    b = M["biseau"]

    def alpha_biseau(nd, co):
        d = distance_forme(nd, co, "rect", fdl, fdh, fr)
        bande = nd.math("MULTIPLY", nd.lisse(d, -b - 0.8, -b + 0.8), nd.lisse(d, 0.2, -0.6))
        _, y, _ = nd.separer(co)
        force = nd.math("ADD", 0.60, nd.math("MULTIPLY", nd.lisse(y, -6, 6), 0.10))
        return nd.math("MULTIPLY", bande, force)
    plaque_rect("Biseau", fcx, fcy, fdl + 1, fdh + 1, z, rep, col,
                mat_douce("Biseau", alpha_biseau, couleur_hex_ass(clair, sombre),
                          couleur_hex_ass(mi, sombre)))

    # Lueur centrale : disque flou blanc à 14 % derrière l'emplacement de l'icône
    if M["lueur"]:
        lx, ly, lr = M["lueur"]

        def alpha_lueur(nd, co):
            d = distance_forme(nd, co, "ellipse", lr, lr)
            return nd.math("MULTIPLY", nd.lisse(d, 0, -lr * 0.9), 0.14)
        plaque_rect("Lueur", lx, ly, lr + 1, lr + 1, z + 1, rep, col, mat_douce("Lueur", alpha_lueur))

    # Étoiles de la Renaissance (nuit étoilée) : dans la couronne extérieure de la face
    if nom_couleur == "renaissance":
        mat_et = mat_emission("Etoile", "#FFF1D6", alpha=0.70)
        rr = fdl - 14                               # entre 8 et 20 px du bord
        # angles « écran » (0 = droite, 90 = bas) : on évite le haut, où passe le reflet
        for i, (ang, t) in enumerate(((150, 10), (115, 7), (62, 9), (25, 6), (-15, 8), (195, 7))):
            a = math.radians(ang)
            et = etoile(f"Etoile{i}", fcx + rr * math.cos(a), fcy + rr * math.sin(a), t,
                        z + 2, rep, col)
            et.data.materials.append(mat_et)

    # Lumière renvoyée : arc « clair » à 40 % le long du bas de la face
    rv = M["renvoyee"]
    if "renvoyee_rect" in M:
        ax0, ay0, ax1, ay1 = M["renvoyee_rect"]

        def alpha_renv(nd, co):
            x, y, _ = nd.separer(co)
            ligne = nd.math("MULTIPLY", nd.lisse(y, (ay1 - ay0) / 2 + 0.6, (ay1 - ay0) / 2 - 0.6),
                            nd.lisse(y, -(ay1 - ay0) / 2 - 0.6, -(ay1 - ay0) / 2 + 0.6))
            ax = nd.math("ABSOLUTE", x)
            bouts = nd.lisse(ax, (ax1 - ax0) / 2, (ax1 - ax0) / 2 - 10)   # fondu sur 10 px
            return nd.math("MULTIPLY", nd.math("MULTIPLY", ligne, bouts), 0.40)
        plaque_rect("Renvoyee", (ax0 + ax1) / 2, (ay0 + ay1) / 2, (ax1 - ax0) / 2 + 1,
                    (ay1 - ay0) / 2 + 1, z + 3, rep, col,
                    mat_douce("Renvoyee", alpha_renv, couleur_hex_ass(clair, sombre)))
    else:
        def alpha_renv(nd, co):
            d = distance_forme(nd, co, "rect", fdl, fdh, fr)
            bande = nd.math("MULTIPLY", nd.lisse(d, -b - rv - 0.8, -b - rv + 0.6),
                            nd.lisse(d, -b + 0.2, -b - 0.6))
            _, y, _ = nd.separer(co)
            bas_seul = nd.lisse(y, -fdh * 0.25, -fdh * 0.75)
            return nd.math("MULTIPLY", nd.math("MULTIPLY", bande, bas_seul), 0.40)
        plaque_rect("Renvoyee", fcx, fcy, fdl + 1, fdh + 1, z + 3, rep, col,
                    mat_douce("Renvoyee", alpha_renv, couleur_hex_ass(clair, sombre)))

    # Reflet : ovale entièrement dans la face, blanc 60 % en haut → 0 % en bas, bords doux
    genre = M["reflet"][0]
    a_haut, a_bas = M["reflet_alpha"]
    if genre == "ellipse":
        _, rx, ry, rl, rh = M["reflet"]
        rrc = 0.0
    else:
        _, rx, ry, rl, rh, rrc = M["reflet"]

    def alpha_reflet(nd, co):
        d = distance_forme(nd, co, genre, rl / 2, rh / 2, rrc)
        bord = nd.lisse(d, 0.5, -3.0)
        _, y, _ = nd.separer(co)
        # y local : +rh/2 en haut, −rh/2 en bas ; le fondu finit un peu avant le bas
        grad = nd.N.new("ShaderNodeMapRange")
        grad.inputs["From Min"].default_value = rh / 2
        grad.inputs["From Max"].default_value = -rh / 2 * 0.85
        grad.inputs["To Min"].default_value = a_haut
        grad.inputs["To Max"].default_value = a_bas
        grad.clamp = True
        nd.L.new(y, grad.inputs["Value"])
        return nd.math("MULTIPLY", bord, grad.outputs["Result"])
    plaque_rect("Reflet", rx, ry, rl / 2 + 1, rh / 2 + 1, z + 4, rep, col,
                mat_douce("Reflet", alpha_reflet))

    # Éclat : petit point blanc à 90 %, l'étincelle de la lampe
    eg, ex, ey, el, eh = M["eclat"][:5]
    er = M["eclat"][5] if len(M["eclat"]) > 5 else 0.0

    def alpha_eclat(nd, co):
        d = distance_forme(nd, co, eg, el / 2, eh / 2, er)
        return nd.math("MULTIPLY", nd.lisse(d, 0.6, -1.0), 0.90)
    plaque_rect("Eclat", ex, ey, el / 2 + 1, eh / 2 + 1, z + 5, rep, col,
                mat_douce("Eclat", alpha_eclat))
    return scene, M


def couleur_hex_ass(hexa, assombrir):
    """Le même #hex, assombri de `assombrir` (pour les plaques de la version appuyée)."""
    hexa = hexa.lstrip("#")
    v = [round(int(hexa[i:i + 2], 16) * (1 - assombrir)) for i in (0, 2, 4)]
    return "#%02X%02X%02X" % tuple(v)


# ---------------------------------------------------------------------------------------
# 6. Après le rendu : ombre portée, contrôle du 9-slice
# ---------------------------------------------------------------------------------------
def lire_png(chemin):
    """Pixels d'un PNG 8 bits (valeurs sRGB, alpha non prémultiplié), en tableau h × w × 4,
    première ligne = HAUT de l'image (Blender range les lignes à l'envers)."""
    img = bpy.data.images.load(chemin, check_existing=False)
    w, h = img.size
    px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)[::-1].copy()
    bpy.data.images.remove(img)
    return px


def ecrire_png(px, chemin):
    h, w = px.shape[:2]
    img = bpy.data.images.new("sortie", w, h, alpha=True)
    img.pixels[:] = np.clip(px[::-1], 0, 1).ravel()
    img.filepath_raw = chemin
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)


def flou_gaussien(a, sigma):
    """Flou gaussien (séparable : d'abord les lignes, puis les colonnes)."""
    if sigma <= 0:
        return a
    r = int(math.ceil(sigma * 3))
    x = np.arange(-r, r + 1, dtype=np.float32)
    k = np.exp(-x * x / (2 * sigma * sigma))
    k /= k.sum()
    pad = np.pad(a, r, mode="constant")
    lignes = np.apply_along_axis(lambda v: np.convolve(v, k, mode="valid"), 1, pad)
    return np.apply_along_axis(lambda v: np.convolve(v, k, mode="valid"), 0, lignes)


def sur(dessus, dessous):
    """Pose `dessus` sur `dessous` (alpha non prémultiplié)."""
    ad, ab = dessus[..., 3:4], dessous[..., 3:4]
    a = ad + ab * (1 - ad)
    rgb = (dessus[..., :3] * ad + dessous[..., :3] * ab * (1 - ad)) / np.maximum(a, 1e-6)
    return np.concatenate([rgb, a], axis=-1)


def ajouter_ombre(px, M):
    """Ombre portée : la silhouette (contour), décalée vers le bas, floutée, encre à 35 %."""
    dec, flou = M["ombre"]
    a = px[..., 3]
    d = dec * ECHELLE
    decale = np.zeros_like(a)
    decale[d:] = a[:-d]
    # « flou 8 px » (affichage) = rayon visible ~16 px de l'image → sigma 8 px de l'image
    a_ombre = flou_gaussien(decale, flou * ECHELLE / 2) * M["ombre_opacite"]
    encre = [int(ENCRE[i:i + 2], 16) / 255 for i in (1, 3, 5)]
    ombre = np.zeros_like(px)
    ombre[..., :3] = encre
    ombre[..., 3] = a_ombre
    return sur(px, ombre)


def egaliser_9slice(px, gauche=80, droite=80):
    """9-slice : entre x = 80 et x = largeur − 80 (pixels de l'image), toutes les colonnes
    doivent être identiques, sinon l'étirement fait des marches. Avec des soleils et une
    forme droite, c'est déjà le cas à un bruit de rendu près : on mesure l'écart, puis on
    recopie la colonne du milieu pour que ce soit EXACT. Renvoie l'écart maximal mesuré."""
    w = px.shape[1]
    milieu = px[:, w // 2:w // 2 + 1]
    zone = px[:, gauche:w - droite]
    diff = np.abs(zone - milieu).max(axis=2)
    ecart = float(diff.max()) * 255
    if os.environ.get("DIAG"):
        lig, colo = np.unravel_index(diff.argmax(), diff.shape)
        print(f"[diag] écart max en ligne {lig}, colonne {colo + gauche} ; lignes touchées :",
              sorted(set(np.nonzero(diff > 2 / 255)[0].tolist()))[:40])
    px[:, gauche:w - droite] = milieu
    return ecart


def rendre_bulle(forme, nom_couleur, appui, chemin):
    """La fonction de la fiche §15.1.9 : une bulle → un PNG transparent."""
    scene, M = construire_bulle(forme, nom_couleur, appui)
    brut = chemin + ".brut.png"
    scene.render.filepath = brut
    bpy.ops.render.render(write_still=True)
    px = lire_png(brut)
    os.remove(brut)
    ecart = None
    if forme == "rect":
        ecart = egaliser_9slice(px)
    px = ajouter_ombre(px, M)
    if forme == "rect":
        egaliser_9slice(px)          # l'ombre est droite elle aussi : simple sécurité
    ecrire_png(px, chemin)
    return px, ecart


# ---------------------------------------------------------------------------------------
# 7. La liste des 25 bulles
# ---------------------------------------------------------------------------------------
def liste_bulles():
    bulles = []
    for f in ("boutique", "inventaire", "index", "ameliorations", "classement", "renaissance"):
        for appui in (False, True):
            bulles.append(("rond", f, appui))
    for c in ("rouge", "ardoise", "or"):
        for appui in (False, True):
            bulles.append(("petit", c, appui))
    for c in ("vert", "violet", "aqua"):
        for appui in (False, True):
            bulles.append(("rect", c, appui))
    bulles.append(("rect", "gris", False))
    return bulles


def nom_fichier(forme, c, appui):
    return f"bulle_{forme}_{c}{'_appui' if appui else ''}.png"


# ---------------------------------------------------------------------------------------
# 8. La planche de contrôle : chaque bulle sur le ciel (et sur la nuit), icône posée dessus
# ---------------------------------------------------------------------------------------
_cache_icones = {}


def icone(nom, cote):
    """Une icône détourée, réduite à `cote` px (en deux étapes : plus net qu'en une)."""
    cle = (nom, cote)
    if cle not in _cache_icones:
        img = bpy.data.images.load(os.path.join(DOSSIER_ICONES, nom + ".png"), check_existing=False)
        img.scale(cote * 2, cote * 2)
        img.scale(cote, cote)
        w, h = img.size
        _cache_icones[cle] = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)[::-1].copy()
        bpy.data.images.remove(img)
    return _cache_icones[cle]


def coller(fond, img, x, y):
    h, w = img.shape[:2]
    fond[y:y + h, x:x + w] = sur(img, fond[y:y + h, x:x + w])


def croix(cote, appui):
    """La croix de fermeture du §15.1.6 (barres blanches, contour, épaisseur), en ×2."""
    s = ECHELLE
    yy, xx = np.mgrid[0:cote, 0:cote].astype(np.float32) + 0.5
    out = np.zeros((cote, cote, 4), np.float32)

    def barres(cx, cy, demi_l, demi_h):
        d = np.full((cote, cote), 1e9, np.float32)
        for ang in (45, -45):
            a = math.radians(ang)
            u = (xx - cx) * math.cos(a) + (yy - cy) * math.sin(a)
            v = -(xx - cx) * math.sin(a) + (yy - cy) * math.cos(a)
            qx, qy = np.abs(u) - (demi_l - demi_h), np.abs(v) - 0
            dd = np.sqrt(np.maximum(qx, 0) ** 2 + np.maximum(qy, 0) ** 2) - demi_h
            d = np.minimum(d, dd)
        return d
    cx, cy = cote / 2, cote / 2
    for dy, coul, epais in ((3 * s, (0x9A, 0x22, 0x28), 0), (0, (255, 255, 255), 0)):
        d = barres(cx, cy + dy, 18 * s, 5.5 * s)
        a_trait = np.clip(0.5 - (d - 3 * s), 0, 1)
        a_coeur = np.clip(0.5 - d, 0, 1)
        couche = np.zeros_like(out)
        couche[..., :3] = np.array((0x7A, 0x1A, 0x1F)) / 255 if dy == 0 else np.array(coul) / 255
        couche[..., 3] = a_trait
        coeur = np.zeros_like(out)
        coeur[..., :3] = np.array(coul) / 255
        coeur[..., 3] = a_coeur
        out = sur(sur(coeur, couche), out)
    return out


def etirer_9slice(px, largeur, g=80, d=80):
    """Étire une bulle rectangulaire en largeur comme le fera UI Toolkit (9-slice)."""
    h, w = px.shape[:2]
    milieu = px[:, g:w - d]
    n = largeur - g - d
    idx = (np.arange(n) * (milieu.shape[1] / n)).astype(int)
    return np.concatenate([px[:, :g], milieu[:, idx], px[:, w - d:]], axis=1)


def fond_ciel(w, h, haut="#9AD6FF", bas="#6EC3FF"):
    t = np.linspace(0, 1, h, dtype=np.float32)[:, None, None]
    c0 = np.array([int(haut[i:i + 2], 16) / 255 for i in (1, 3, 5)], np.float32)
    c1 = np.array([int(bas[i:i + 2], 16) / 255 for i in (1, 3, 5)], np.float32)
    rgb = np.broadcast_to(c0 * (1 - t) + c1 * t, (h, w, 3))
    return np.concatenate([rgb, np.ones((h, w, 1), np.float32)], axis=-1).copy()


ICONE_ROND = {"boutique": "icone_boutique", "inventaire": "icone_inventaire",
              "index": "icone_index", "ameliorations": "icone_ameliorations",
              "classement": "icone_classement", "renaissance": "icone_rebirth"}


def habiller(forme, c, appui, px):
    """La bulle + ce que l'interface posera dessus (pour juger le rendu final)."""
    px = px.copy()
    s = ECHELLE
    if forme == "rond":
        coller(px, icone(ICONE_ROND[c], 92 * s), 34 * s, (30 if appui else 24) * s)
    elif forme == "petit":
        dy = 5 if appui else 0
        if c == "rouge":
            cr = croix(56 * s, appui)
            coller(px, cr, 20 * s, (12 + dy) * s)
        elif c == "ardoise":
            coller(px, icone("icone_reglages", 64 * s), 16 * s, (8 + dy) * s)
        else:
            coller(px, icone("piece", 52 * s), 22 * s, (14 + dy) * s)
    else:
        dy = 6 if appui else 0
        nom = {"vert": "piece", "violet": "icone_rebirth", "aqua": "diamant", "gris": "piece"}[c]
        ic = icone(nom, 64 * s)
        if c == "gris":
            ic = ic.copy()
            ic[..., :3] = ic[..., :3] * 0.55 + 0.72 * 0.45
        w = px.shape[1]
        coller(px, ic, w // 2 - 32 * s, (20 + dy) * s)
    return px


def planche(rendus):
    s = ECHELLE
    marge, pas = 24 * s, 12 * s
    largeur = 6 * 160 * s + 5 * pas + 2 * marge
    lignes_ciel = [160, 160, 96, 120, 120, 120]
    h_ciel = sum(lignes_ciel) * s + (len(lignes_ciel) - 1) * pas + 2 * marge
    h_nuit = 160 * s + 2 * marge
    fond = np.concatenate([fond_ciel(largeur, h_ciel),
                           fond_ciel(largeur, h_nuit, "#1F1730", "#140F33")], axis=0)

    def get(forme, c, appui):
        k = nom_fichier(forme, c, appui)
        return habiller(forme, c, appui, rendus[k]) if k in rendus else None

    y = marge
    fonctions = list(ICONE_ROND)
    # lignes 1 et 2 : grandes rondes, repos puis appuyées
    for appui in (False, True):
        x = marge
        for f in fonctions:
            b = get("rond", f, appui)
            if b is not None:
                coller(fond, b, x, y)
            x += 160 * s + pas
        y += 160 * s + pas
    # ligne 3 : petites, repos / appuyée côte à côte
    x = marge
    for c in ("rouge", "ardoise", "or"):
        for appui in (False, True):
            b = get("petit", c, appui)
            if b is not None:
                coller(fond, b, x, y)
            x += 96 * s + pas
        x += pas * 3
    y += 96 * s + pas
    # lignes 4 et 5 : rectangulaires repos puis appuyées (+ gris)
    for appui in (False, True):
        x = marge
        for c in ("vert", "violet", "aqua"):
            b = get("rect", c, appui)
            if b is not None:
                coller(fond, b, x, y)
            x += 200 * s + pas
        if not appui and nom_fichier("rect", "gris", False) in rendus:
            coller(fond, get("rect", "gris", False), x, y)
        y += 120 * s + pas
    # ligne 6 : 9-slice étiré (Encaisser 300, Rejouer 280, Continuer 340 px d'affichage)
    x = marge
    for c, lg in (("vert", 300), ("violet", 280), ("aqua", 340)):
        k = nom_fichier("rect", c, False)
        if k in rendus:
            et = etirer_9slice(rendus[k], lg * s)
            coller(fond, habiller("rect", c, False, et), x, y)
        x += lg * s + pas * 2
    # bande de nuit : les grandes rondes sur le ciel de nuit
    y = h_ciel + marge
    x = marge
    for f in fonctions:
        b = get("rond", f, False)
        if b is not None:
            coller(fond, b, x, y)
        x += 160 * s + pas
    ecrire_png(fond, PLANCHE)


# ---------------------------------------------------------------------------------------
# 9. Programme principal
# ---------------------------------------------------------------------------------------
def principal():
    os.makedirs(DOSSIER_BULLES, exist_ok=True)
    choix = os.environ.get("BULLES", "").split()
    rendus, rapport = {}, []
    for forme, c, appui in liste_bulles():
        nom = nom_fichier(forme, c, appui)
        chemin = os.path.join(DOSSIER_BULLES, nom)
        if choix and nom[len("bulle_"):-len(".png")] not in choix:
            if os.path.exists(chemin):
                rendus[nom] = lire_png(chemin)
            continue
        px, ecart = rendre_bulle(forme, c, appui, chemin)
        rendus[nom] = px
        rapport.append((nom, px.shape[1], px.shape[0], ecart))
    for nom, w, h, ecart in rapport:
        info = f"  écart 9-slice avant égalisation : {ecart:.1f}/255" if ecart is not None else ""
        print(f"[bulles] {nom}  {w}×{h}{info}")
    if os.environ.get("PLANCHE", "1") != "0":
        planche(rendus)
        print(f"[bulles] planche : {PLANCHE}")


principal()
