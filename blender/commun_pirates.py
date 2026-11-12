"""
commun_pirates.py — Outils partagés par les scripts des objets des « Boîtes Pirates » (v5).

Pourquoi un fichier à part, au lieu d'ajouter ces fonctions à commun.py ?
    Un autre artiste fabrique EN MÊME TEMPS les objets Animaux (avec commun_animaux.py).
    Si nous modifiions tous les deux commun.py, l'un écraserait le travail de l'autre. Ici on
    « complète » commun.py sans le toucher : on l'importe, puis on ajoute nos matières
    (préfixe M5_Pir…, des noms qui ne peuvent pas tomber sur ceux des Animaux) et nos
    briques de forme (anneaux qui épousent le cou, maillons de chaîne, yeux mignons…).

Utilisation dans un script d'objet :
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    import commun as C
    import commun_pirates as P

Références : docs/specs/v5-catalogue.md §1 et §2 (fiches Pirates), docs/art-bible.md §13
(formes à trois niveaux, biseaux, matières M5_, éléments Anim_* / Eclat_* / Fx_Emetteur).

Rappel des axes (voir commun.py) : on construit en coordonnées Blender, base du cheval à
l'origine ; C.U(x, y, z) convertit un point donné en Unity (Y en haut, avant = −Z).
Unity x POSITIF = DROITE de l'image (vue de la caméra du jeu) = Blender x NÉGATIF.
"""

import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

# ---------------------------------------------------------------------------------------
# 1. Matières des objets Pirates (bible §13.4 : roughness Blender = 1 − smoothness Unity)
# ---------------------------------------------------------------------------------------
# nom : (couleur, metallic, roughness[, émission]). L'émission n'est donnée qu'aux
# Légendaires et aux Secrets (catalogue §0.1), jamais au-dessus de 1,5 (bible §4.1).
C.MATERIAUX.update({
    # tricorne
    "M5_PirFeutreNuit": ("#1F2A50", 0.0, 0.60),       # objet sombre : smoothness 0,40 (§13.4)
    "M5_PirPlumeRouge": ("#D64545", 0.0, 0.85),
    # bandana
    "M5_PirBandanaRouge": ("#D64545", 0.0, 0.50),
    # cache-œil, longue-vue
    "M5_PirCuir": ("#5A3A28", 0.0, 0.50),
    "M5_PirCuirFonce": ("#3A2418", 0.0, 0.75),
    # foulard
    "M5_PirMarine": ("#1F3A6E", 0.0, 0.55),
    # boussole
    "M5_PirCadran": ("#F2E6C8", 0.0, 0.50),
    "M5_PirAiguille": ("#D64545", 0.0, 0.15),
    # longue-vue
    "M5_PirLaiton": ("#C99A1E", 1.0, 0.25),
    "M5_PirVerre": ("#9FE3FF", 0.0, 0.05),
    # chapeau de capitaine
    "M5_PirFeutreCapitaine": ("#2A1F3D", 0.0, 0.60),
    "M5_PirAncreLuit": ("#FFC83A", 1.0, 0.20, 0.8),  # l'ancre du chapeau luit (0,8)
    "M5_PirGemme": ("#9FE3FF", 0.0, 0.05),
    # loup corsaire
    "M5_PirVelours": ("#4B2E83", 0.0, 0.90),
    "M5_PirOrLuit": ("#E8B830", 1.0, 0.20, 0.6),     # bordure d'or, reflets qui défilent (0,6)
    "M5_PirRubis": ("#D62839", 0.0, 0.05),
    # perroquet
    "M5_PirAraRouge": ("#D64545", 0.0, 0.45),
    "M5_PirAraBleu": ("#3B7DD8", 0.0, 0.45),
    "M5_PirAraJaune": ("#FFD54A", 0.0, 0.45),
    "M5_PirBecCreme": ("#F4EEE6", 0.0, 0.15),        # bec, joues ET reflets des yeux
    "M5_PirOeilNoir": ("#1F1730", 0.0, 0.10),        # « noir » de la bible (jamais #000)
    # ancre
    "M5_PirOrAncre": ("#FFC83A", 1.0, 0.20, 0.5),    # reflet qui glisse (émission 0,5)
    "M5_PirCordeRouge": ("#D64545", 0.0, 0.85),
    # petit kraken
    "M5_PirKrakenViolet": ("#7A3FC0", 0.0, 0.30),
    "M5_PirKrakenLueur": ("#4FFFE8", 0.0, 0.20, 0.9),
    "M5_PirJoue": ("#FF9FC8", 0.0, 0.60),
    "M5_PirPiece": ("#FFC83A", 1.0, 0.20, 0.6),
    # navire fantôme (transparent : voir TRANSPARENTS)
    "M5_PirSpectre": ("#9AF5E0", 0.0, 0.20, 0.7),
    "M5_PirVoile": ("#DFFFF6", 0.0, 0.40, 0.5),
    "M5_PirLanterne": ("#FFE27A", 0.0, 0.20, 1.4),
    "M5_PirBrume": ("#CFFFF2", 0.0, 0.60, 0.4),
    # extras du skin Kraken (tentacules)
    "M5_PirKrakenPeau": ("#5B2D82", 0.0, 0.15),      # smoothness 0,85 : aspect mouillé
    "M5_PirVentouse": ("#F2B8D8", 0.0, 0.40),
    "M5_PirBiolum": ("#4FFFE8", 0.0, 0.20, 1.3),     # taches qui s'allument tour à tour
    # extras du skin Abysses (lanterne de baudroie)
    "M5_PirAbysses": ("#0A1430", 0.0, 0.10),
    "M5_PirLanterneAbysses": ("#FFF1A8", 0.0, 0.20, 1.5),
    "M5_PirPoissonCyan": ("#4FFFE8", 0.0, 0.20, 1.2),
})

# Opacité des matières transparentes (dans Unity : Surface Type = Transparent, comme le
# skin Cristal ; le programmeur reprend ces valeurs).
TRANSPARENTS = {
    "M5_PirSpectre": 0.6,
    "M5_PirVoile": 0.5,
    "M5_PirBrume": 0.3,
}


def appliquer_transparence():
    """Règle l'opacité des matières transparentes déjà créées (pour les rendus Blender ;
    le FBX transporte aussi la valeur, mais le programmeur doit régler le .mat Unity)."""
    for mat in bpy.data.materials:
        nom = mat.name.split(".")[0]          # « M5_PirVoile.001 » après un import
        if nom in TRANSPARENTS and mat.node_tree:
            bsdf = mat.node_tree.nodes.get("Principled BSDF")
            if bsdf is None:
                continue
            bsdf.inputs["Alpha"].default_value = TRANSPARENTS[nom]
            try:
                mat.surface_render_method = "BLENDED"
            except AttributeError:
                mat.blend_method = "BLEND"


# ---------------------------------------------------------------------------------------
# 2. Le cheval mesuré par rayons
# ---------------------------------------------------------------------------------------
_ARBRE = None
_ARBRE_SANS_OREILLES = None
AXE_COU_Z = 0.02          # l'axe du cou est 2 cm en arrière (Unity z), comme l'écharpe


def arbre():
    """BVH du corps du cheval (chargé une seule fois par script)."""
    global _ARBRE
    if _ARBRE is None:
        _ARBRE = C.arbre_cheval()
    return _ARBRE


def arbre_sans_oreilles():
    """BVH du corps du cheval SANS ses oreilles. Pourquoi ? Un tissu posé sur la tête
    (bandana) mesuré avec les oreilles ferait une « tente » par-dessus elles et les
    cacherait ; sans elles, le tissu épouse le crâne et les oreilles passent au travers,
    comme si elles sortaient par deux fentes.
    Les oreilles du cheval : |x| entre 0,07 et 0,21, z entre −0,03 et 0,17, au-dessus de
    y = 1,87 (mesuré sur personnage_cheval.fbx)."""
    global _ARBRE_SANS_OREILLES
    if _ARBRE_SANS_OREILLES is not None:
        return _ARBRE_SANS_OREILLES
    from mathutils.bvhtree import BVHTree
    objs = C.importer_fbx(C.CHEVAL_FBX)
    bpy.context.view_layer.update()
    corps = next(o for o in objs if o.name.startswith("Corps"))
    bm = bmesh.new()
    bm.from_mesh(corps.data)
    bm.transform(corps.matrix_world)
    oreilles = []
    for f in bm.faces:
        c = f.calc_center_median()          # Blender : Unity x = −c.x, y = c.z, z = −c.y
        if 0.07 < abs(c.x) < 0.21 and -0.17 < c.y < 0.03 and c.z > 1.87:
            oreilles.append(f)
    bmesh.ops.delete(bm, geom=oreilles, context="FACES")
    _ARBRE_SANS_OREILLES = BVHTree.FromBMesh(bm)
    bm.free()
    for o in objs:
        bpy.data.objects.remove(o, do_unlink=True)
    return _ARBRE_SANS_OREILLES


def tour_cou(hauteur, ecart, n=24, demi_hauteur=0.0, depart=0.0):
    """Un anneau qui épouse le cou (comme l'écharpe) : pour chaque angle a (0 = devant,
    positif = gauche de l'image), on part de l'axe du cou à la hauteur `hauteur(a)` (Unity)
    et on se pose sur la surface la plus éloignée (lissée par-dessus la crinière), décollé
    de `ecart`. `demi_hauteur` > 0 : on mesure aussi au-dessus et en dessous (pour une
    bande haute qui ne doit rentrer nulle part). Renvoie une liste de (point, direction)."""
    res = []
    for i in range(n):
        a = depart + 2 * math.pi * i / n
        y0 = hauteur(a)
        meilleur = None
        for dy in ((0.0,) if demi_hauteur <= 0 else (-demi_hauteur, 0.0, demi_hauteur)):
            centre = (0.0, y0 + dy, AXE_COU_Z)
            p, d = C.sur_cheval_lisse(arbre(), centre, a, 0.0, 0.0, pente=0.6,
                                      fenetre=(0.42, 0.0), pas=0.14)
            r = (p - C.U(*centre)).dot(d)
            meilleur = r if meilleur is None else max(meilleur, r)
        d = C.direction_angles(a)
        res.append((C.U(0.0, y0, AXE_COU_Z) + d * (meilleur + ecart), d))
    return res


def racine_joueur(nom, enfants):
    """Racine d'un « extra » de skin (catalogue §0.4) : posé comme enfant direct de
    `Joueur`, donc origine = la base du cheval (0 ; 0 ; 0)."""
    racine = C.vide(nom, (0, 0, 0))
    for e in enfants:
        if e.parent is None:
            C.parenter(e, racine)
    bpy.context.view_layer.update()
    return racine


# ---------------------------------------------------------------------------------------
# 3. Briques de forme
# ---------------------------------------------------------------------------------------
def matrice_vers(origine, direction, haut=Vector((0, 0, 1))):
    """Repère local : Z local = `direction` (l'axe d'une pièce), Y local au plus près de
    `haut`. Pratique pour poser un tube, un œil ou une sphère dans une direction."""
    z = Vector(direction).normalized()
    h = Vector(haut)
    if abs(z.dot(h.normalized())) > 0.98:
        h = Vector((0, 1, 0))
    x = h.cross(z).normalized()
    y = z.cross(x)
    return Matrix.Translation(Vector(origine)) @ Matrix((x, y, z)).transposed().to_4x4()


def bm_ellipsoide(bm, centre, demi, segments=10, anneaux=7, mat=0, rot=None):
    """Ellipsoïde (demi-axes `demi` = (x, y, z)), tourné par la matrice 3×3 `rot`."""
    v = C.bm_sphere(bm, 1.0, (0, 0, 0), segments=segments, anneaux=anneaux,
                    echelle=demi, mat=mat)
    m = Matrix.Translation(Vector(centre))
    if rot is not None:
        m = m @ rot.to_4x4()
    bmesh.ops.transform(bm, matrix=m, verts=v)
    return v


def oeil_mignon(bm, centre, normale, rayon, mat_noir, mat_reflet, haut=Vector((0, 0, 1))):
    """Gros œil de dessin animé : une boule noire vernie un peu aplatie et deux reflets
    (un gros en haut à gauche de l'image, un petit en bas à droite). Le reflet en haut à
    gauche suit la règle des reflets peints (bible §13.4)."""
    n = Vector(normale).normalized()
    m = C.repere(centre, n, haut)
    v = bm_ellipsoide(bm, (0, 0, 0), (rayon, rayon * 0.55, rayon * 1.12), segments=10,
                      anneaux=6, mat=mat_noir)
    bmesh.ops.transform(bm, matrix=m, verts=v)
    # +X local du repère = gauche de l'image quand l'œil regarde la caméra
    for (dx, dz, r) in ((0.38, 0.42, 0.30), (-0.35, -0.38, 0.14)):
        p = m @ Vector((dx * rayon, rayon * 0.50, dz * rayon))
        v += C.bm_sphere(bm, r * rayon, p, segments=6, anneaux=4, mat=mat_reflet,
                         echelle=(1, 0.5, 1))
    return v


def bm_maillon(bm, centre, axe, plan, longueur=0.11, largeur=0.07, fil=0.018, cotes=4,
               segments=8, mat=0):
    """Un maillon de chaîne (anneau ovale) centré sur `centre`, allongé selon `axe`, couché
    dans le plan qui contient `axe` et `plan`. Les maillons alternent de plan (90°) pour
    s'emboîter. Section à `cotes` côtés : 8 × 4 × 2 = 64 triangles par maillon."""
    a = Vector(axe).normalized()
    b = (Vector(plan) - a * Vector(plan).dot(a)).normalized()
    c = a.cross(b)
    e, r = (longueur - largeur) / 2, largeur / 2
    # contour « stade » : deux demi-cercles (4 points chacun) reliés par deux droites
    angles = [(+1, math.radians(d)) for d in (-90, -30, 30, 90)] + \
             [(-1, math.radians(d)) for d in (90, 150, 210, 270)]
    anneaux = []
    for cote, t in angles:
        rad = a * math.cos(t) + b * math.sin(t)
        p = a * (cote * e) + rad * r
        anneau = []
        for k in range(cotes):
            u = 2 * math.pi * k / cotes + math.pi / cotes
            anneau.append(Vector(centre) + p + rad * fil * math.cos(u) + c * fil * math.sin(u))
        anneaux.append(anneau)
    segments = len(anneaux)
    rangs = [[bm.verts.new(q) for q in an] for an in anneaux]
    faces = []
    for s in range(segments):
        A, B = rangs[s], rangs[(s + 1) % segments]
        for k in range(cotes):
            j = (k + 1) % cotes
            f = bm.faces.new((A[k], B[k], B[j], A[j]))
            f.material_index = mat
            faces.append(f)
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    return [v for r in rangs for v in r]


def bm_chaine(bm, points, longueur=0.11, largeur=0.07, fil=0.018, mat=0, ferme=False,
              hauts=None):
    """Une chaîne de maillons posée le long d'un chemin (liste de points Blender). Les
    maillons sont espacés de 0,8 × leur longueur (ils s'emboîtent) et alternent de plan.
    `hauts` (facultatif, un vecteur par point) : la normale de la surface sous la chaîne,
    pour coucher un maillon sur deux à plat sur le cheval. Renvoie le nombre de maillons."""
    pts = [Vector(p) for p in points]
    if ferme:
        pts = pts + [pts[0]]
        if hauts:
            hauts = list(hauts) + [hauts[0]]
    # longueur totale et échantillonnage régulier
    longs = [0.0]
    for i in range(1, len(pts)):
        longs.append(longs[-1] + (pts[i] - pts[i - 1]).length)
    pas = longueur * 0.8
    n = max(2, int(longs[-1] / pas))
    k = 0
    for j in range(n):
        s = longs[-1] * (j + 0.5) / n
        while k < len(longs) - 2 and longs[k + 1] < s:
            k += 1
        t = (s - longs[k]) / max(1e-6, longs[k + 1] - longs[k])
        p = pts[k].lerp(pts[k + 1], t)
        axe = pts[k + 1] - pts[k]
        if hauts:
            h = Vector(hauts[k]).lerp(Vector(hauts[min(k + 1, len(hauts) - 1)]), t)
        else:
            h = Vector((0, 0, 1))
        plan = h.cross(axe) if j % 2 == 0 else h
        if plan.length < 1e-6:
            plan = Vector((1, 0, 0))
        bm_maillon(bm, p, axe, plan, longueur, largeur, fil, mat=mat)
    return n


def bm_plume(bm, base, direction, longueur, largeur, courbe=Vector((0, 0, 0)), mat=0,
             cotes=6, n=6, haut=Vector((0, 0, 1)), plat=0.35):
    """Plume de chapeau stylisée : un fuseau aplati (large au milieu, pointu au bout) qui
    se courbe vers `courbe`. Renvoie les sommets."""
    d = Vector(direction).normalized()
    chemin, rayons = [], []
    for k in range(n + 1):
        t = k / n
        chemin.append(Vector(base) + d * longueur * t + Vector(courbe) * (t * t))
        rayons.append(max(0.08, math.sin(math.pi * min(1.0, 0.15 + t * 0.95)) ** 0.8))
    avant = set(bm.verts)
    C.bm_tube(bm, chemin, largeur / 2, cotes=cotes, mat=mat, haut=haut, rayons=rayons)
    nouveaux = [v for v in bm.verts if v not in avant]
    # aplatir la plume dans la direction `haut` (une plume est plate, pas ronde)
    for i, p in enumerate(chemin):
        anneau = nouveaux[i * cotes:(i + 1) * cotes]
        t = (chemin[min(i + 1, n)] - chemin[max(i - 1, 0)]).normalized()
        cote = t.cross(Vector(haut)).normalized()
        epais = cote.cross(t).normalized()
        for v in anneau:
            off = v.co - p
            v.co = p + cote * off.dot(cote) + epais * off.dot(epais) * plat
    return nouveaux


def bm_contour_plein(bm, contour, epaisseur, matrice, mat=0):
    """Plaque d'épaisseur `epaisseur` d'un contour 2D (x, z) QUELCONQUE (concave permis),
    devant = +Y local, placée par `matrice`."""
    bas = [bm.verts.new((x, -epaisseur / 2, z)) for x, z in contour]
    haut = [bm.verts.new((x, epaisseur / 2, z)) for x, z in contour]
    n = len(contour)
    aretes = []
    for rang in (bas, haut):
        for i in range(n):
            aretes.append(bm.edges.new((rang[i], rang[(i + 1) % n])))
    res = bmesh.ops.triangle_fill(bm, use_beauty=True, use_dissolve=False, edges=aretes,
                                  normal=(0, 1, 0))
    faces = [g for g in res["geom"] if isinstance(g, bmesh.types.BMFace)]
    for i in range(n):
        j = (i + 1) % n
        faces.append(bm.faces.new((bas[i], bas[j], haut[j], haut[i])))
    for f in faces:
        f.material_index = mat
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    verts = bas + haut
    bmesh.ops.transform(bm, matrix=matrice, verts=verts)
    return verts


# ---------------------------------------------------------------------------------------
# 4. Fin des scripts
# ---------------------------------------------------------------------------------------
def finir_pirate(racine, nom, point, y_min=1.0):
    """Comme commun.finir_v5 (rapport, rendu porté par le cheval vu de la caméra du jeu,
    export FBX dans blender/exports/), après avoir réglé les matières transparentes.
    `point` = "Joueur" pour un extra de skin (origine = la base du cheval)."""
    appliquer_transparence()
    return C.finir_v5(racine, nom, point, y_min=y_min)


# ---------------------------------------------------------------------------------------
# 5. Emblèmes des boîtes Pirates (pour blender/boite_cadeau.py, tableau EMBLEMES)
# ---------------------------------------------------------------------------------------
# Même contrat que les emblèmes de boite_cadeau.py : on dessine dans le repère du médaillon
# (origine au centre de sa face, avant = +Y, haut = +Z, rayon utile 0,09) et on renvoie la
# liste des matières (hex, metallic, roughness) utilisées par les faces (material_index).
def embleme_ancre(bm, couleurs):
    """Ancre « gravée » : verge, jas, anneau, bras en arc et pattes. Boîte de skins Pirates."""
    e = 0.022                           # relief
    y = e / 2
    C.bm_boite(bm, (0.016, e, 0.11), (0, y, 0.0), mat=0)                   # verge
    C.bm_boite(bm, (0.075, e, 0.015), (0, y, 0.036), mat=0)                # jas
    C.bm_tore(bm, 0.016, 0.006, segments=10, segments_tube=4, mat=0,
              matrice=Matrix.Translation((0, y, 0.066)) @ Matrix.Rotation(math.radians(90), 4, "X"))
    arc = [Vector((0.058 * math.sin(a), y, -0.012 - 0.045 * math.cos(a)))
           for a in [math.radians(d) for d in range(-80, 81, 20)]]
    C.bm_tube(bm, arc, 0.009, cotes=5, mat=0)
    for s in (-1, 1):
        a = math.radians(80 * s)
        c = Vector((0.058 * math.sin(a), y, -0.012 - 0.045 * math.cos(a)))
        m = Matrix.Translation(c) @ Matrix.Rotation(math.radians(-38 * s), 4, "Y")
        C.bm_plaque(bm, [(0, 0.03), (-0.02, -0.006), (0.02, -0.006)], e, m, mat=0)
    return couleurs or [("#2A1F3D", 0.0, 0.45)]


def embleme_gouvernail(bm, couleurs):
    """Gouvernail (barre à roue) « gravé » : couronne, 8 rayons à poignées, moyeu.
    Boîte d'accessoires Pirates."""
    e = 0.022
    y = e / 2
    rot = Matrix.Translation((0, y, 0)) @ Matrix.Rotation(math.radians(90), 4, "X")
    C.bm_tore(bm, 0.052, 0.009, segments=16, segments_tube=4, mat=0, matrice=rot)
    C.bm_disque(bm, 0.018, e, Matrix.Translation((0, y, 0)), cotes=8, mat=0, bombe=0.004)
    for k in range(8):
        a = 2 * math.pi * k / 8
        d = Vector((math.cos(a), 0, math.sin(a)))
        C.bm_tube(bm, [Vector((0, y, 0)) + d * 0.015, Vector((0, y, 0)) + d * 0.082], 0.0055,
                  cotes=4, mat=0, rayons=[1.0, 1.0])
        C.bm_sphere(bm, 0.009, Vector((0, y, 0)) + d * 0.085, segments=5, anneaux=3, mat=0)
    return couleurs or [("#2A1F3D", 0.0, 0.45)]
