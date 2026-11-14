"""
rendu_icones.py — Icônes de boutique fabriquées par RENDU BLENDER (et non par Gemini).

Pourquoi ce script ? Les icônes des lots 1 à 6 venaient de Gemini. Quand Gemini n'est pas
disponible, on peut fabriquer une icône très proche en photographiant le vrai modèle 3D :
même fond lavande, même angle 3/4, même lumière que la bible (art-bible.md §9.2).
Avantage : l'icône ressemble EXACTEMENT au modèle du jeu (bible §9.3).

Lancer (Blender n'a pas besoin d'être ouvert) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/rendu_icones.py
Seulement quelques icônes :
    ICONES="truffe ruche" /Applications/Blender.app/.../Blender --background --python ...
Refaire une icône qui existe déjà (sinon le script REFUSE d'écraser) :
    ECRASER=1 ICONES="truffe" ...

Sorties : art/icones/<nom>.png (1024 x 1024, fond uni, sans ombre portée).

COMMENT ÇA MARCHE
  1. On reconstruit le modèle avec SON propre script (animal_<nom>.py ou boite_cadeau.py) :
     on appelle sa fonction construire(), qui fabrique les pièces et leurs matériaux.
     Le cheval sert seulement à mesurer la tête : il est retiré par ces scripts.
  2. On place une caméra : 25° au-dessus, tournée de 20° (l'avant de l'objet regarde un peu
     vers la gauche de l'image), et on la recule / décale jusqu'à ce que l'objet occupe
     ~70 % de la hauteur, bien centré.
  3. Trois lumières (bible §5) : principale chaude #FFF1D6 en haut à gauche, remplissage
     violet doux, liseré lavande #B48CFF derrière. Un ciel en dégradé éclaire les objets
     (ce que reflètent les métaux : sans lui, l'or ressortirait noir ou marron).
  4. Le rendu est fait sur fond TRANSPARENT, puis posé sur la couleur unie du fond :
     c'est la garantie d'un fond parfaitement uni (aucun dégradé, aucune ombre portée).

RÉGLAGES PAR OBJET : tableau ICONES en bas (angle, taille dans le cadre, fond).
"""

import importlib
import math
import os
import sys

import bpy
import numpy as np
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view

DOSSIER_BLENDER = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, DOSSIER_BLENDER)
import commun as C  # noqa: E402

# SORTIE=<dossier> : écrire ailleurs (essais de cadrage sans toucher à art/icones).
DOSSIER_ICONES = os.environ.get("SORTIE") or os.path.join(C.DOSSIER_PROJET, "art", "icones")
# Rendus transparents (même nom), relus par detourer_icones.py
DOSSIER_ALPHA = os.environ.get("SORTIE_ALPHA") or os.path.join(C.DOSSIER_PROJET, "art", "icones_alpha")
TAILLE = 1024                       # carré 1024 x 1024
LAVANDE = "#E6E1EE"                 # fond normal (bible §9.2)
BLEU_GRIS = "#5B6380"               # fond des objets très clairs ou transparents

# Valeurs par défaut du cadrage (bible §9.2)
ELEVATION = 25.0                    # degrés au-dessus de l'horizontale
ROTATION = 20.0                     # degrés : l'avant de l'objet regarde vers la gauche
HAUTEUR = 0.70                      # part de la hauteur de l'image occupée par l'objet
LARGEUR_MAX = 0.80                  # garde-fou pour les objets très larges (ailes…)


# ---------------------------------------------------------------------------------------
# 1. Construire le modèle (avec le script qui l'a créé)
# ---------------------------------------------------------------------------------------
def construire(source):
    """`source` = ("animal", "oreilles_chat") ou ("boite", "boite_skins_pirates").
    Renvoie la racine du modèle, dans une scène neuve."""
    genre, nom = source
    if genre == "module":              # un script qui a sa propre fonction construire()
        racine = importlib.import_module(nom).construire()
    elif genre == "fonction":          # "module.fonction" : ex. objets_lot9.construire_sablier
        module, fonction = nom.split(".")
        racine = getattr(importlib.import_module(module), fonction)()
    elif genre == "animal":
        module = importlib.import_module(f"animal_{nom}")
        racine = module.construire()
        import commun_animaux as A
        A.appliquer_transparence()     # verre du bocal, ailes d'abeille…
    else:
        module = importlib.import_module("boite_cadeau")
        racine = module.construire(nom, module.BOITES[nom])   # couvercle fermé par défaut
    bpy.context.view_layer.update()
    return racine


def maillages(racine):
    """Les pièces visibles du modèle (on ignore les vides, repères d'effets, etc.)."""
    return [o for o in C.descendants(racine) if o.type == "MESH" and not o.hide_render]


def points_monde(objs):
    """Tous les sommets (après modificateurs) en coordonnées monde : sert au cadrage."""
    dg = bpy.context.evaluated_depsgraph_get()
    pts = []
    for o in objs:
        ev = o.evaluated_get(dg)
        me = ev.to_mesh()
        mw = ev.matrix_world
        pts.extend(mw @ v.co for v in me.vertices)
        ev.to_mesh_clear()
    return pts


# ---------------------------------------------------------------------------------------
# 2. Le monde (ce que reflètent les objets) et les lumières
# ---------------------------------------------------------------------------------------
def monde_studio(scene, force=0.45):
    """Ciel en dégradé : lavande clair en haut, violet moyen à l'horizon, violet sombre
    en bas. Les métaux y trouvent un reflet clair au-dessus et sombre en dessous, comme
    un jouet verni posé sous une lampe. La caméra ne voit pas ce ciel (fond transparent)."""
    monde = bpy.data.worlds.new("MondeIcones")
    scene.world = monde
    monde.use_nodes = True
    nt = monde.node_tree
    fond = nt.nodes["Background"]
    coord = nt.nodes.new("ShaderNodeTexCoord")
    separe = nt.nodes.new("ShaderNodeSeparateXYZ")
    carte = nt.nodes.new("ShaderNodeMapRange")          # z de -1..1 -> 0..1
    carte.inputs["From Min"].default_value = -1.0
    rampe = nt.nodes.new("ShaderNodeValToRGB")
    els = rampe.color_ramp.elements
    els[0].position, els[0].color = 0.35, C.hex_vers_lineaire("#3A2850")
    els[1].position, els[1].color = 0.85, C.hex_vers_lineaire("#F4F0EC")
    milieu = els.new(0.52)
    milieu.color = C.hex_vers_lineaire("#8C82A8")
    nt.links.new(coord.outputs["Generated"], separe.inputs["Vector"])
    nt.links.new(separe.outputs["Z"], carte.inputs["Value"])
    nt.links.new(carte.outputs["Result"], rampe.inputs["Fac"])
    nt.links.new(rampe.outputs["Color"], fond.inputs["Color"])
    fond.inputs["Strength"].default_value = force


def lumiere_zone(scene, nom, hexa, energie, position, cible, taille):
    """Lumière de zone (Area) : une grande lampe douce, qui donne des ombres floues
    et un reflet arrondi sur le vernis (l'aspect « jouet »)."""
    d = bpy.data.lights.new(nom, "AREA")
    d.color = C.hex_vers_lineaire(hexa)[:3]
    d.energy = energie
    d.size = taille
    o = bpy.data.objects.new(nom, d)
    o.location = position
    o.rotation_euler = (cible - Vector(position)).to_track_quat("-Z", "Y").to_euler()
    scene.collection.objects.link(o)
    return o


def lumieres(scene, centre, rayon, cam_dir):
    """Les trois lumières de la bible, placées par rapport à la caméra (donc identiques
    sur toutes les icônes, quel que soit l'angle choisi pour l'objet).
    cam_dir = direction de l'objet vers la caméra (à plat)."""
    avant = Vector((cam_dir.x, cam_dir.y, 0)).normalized()
    # « gauche de l'image » vue de la caméra : avant tourné de +90° autour de Z
    gauche = Vector((-avant.y, avant.x, 0))
    d = rayon * 6
    e = (rayon / 0.5) ** 2                  # l'énergie suit la taille de l'objet
    # principale chaude : en haut, à gauche, un peu devant
    lumiere_zone(scene, "Principale", "#FFF1D6", 140 * e,
                 centre + (gauche * 0.8 + avant * 0.7 + Vector((0, 0, 1.1))).normalized() * d,
                 centre, rayon * 3)
    # remplissage violet doux : à droite, plus bas, devant (les ombres ne sont jamais noires)
    lumiere_zone(scene, "Remplissage", "#B8A8E8", 30 * e,
                 centre + (-gauche * 0.9 + avant * 0.6 + Vector((0, 0, 0.1))).normalized() * d,
                 centre, rayon * 4)
    # liseré lavande : derrière et au-dessus, il dessine le contour
    lumiere_zone(scene, "Liseré", "#B48CFF", 160 * e,
                 centre + (-avant * 1.0 + gauche * 0.25 + Vector((0, 0, 0.35))).normalized() * d,
                 centre, rayon * 2)


# ---------------------------------------------------------------------------------------
# 3. Caméra et cadrage
# ---------------------------------------------------------------------------------------
def camera(scene, pts, elevation, rotation, hauteur, largeur_max, focale=85):
    """Place la caméra puis ajuste distance et décalage pour que l'objet occupe
    `hauteur` de l'image et soit centré. Renvoie (caméra, direction vers la caméra)."""
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    centre = (mn + mx) / 2
    rayon = max((p - centre).length for p in pts)
    el, az = math.radians(elevation), math.radians(rotation)
    # Avant de l'objet = +Y Blender. Tourner la caméra vers +X fait regarder l'avant de
    # l'objet vers la GAUCHE de l'image (+X est à gauche quand on regarde l'avant).
    direction = Vector((math.sin(az) * math.cos(el), math.cos(az) * math.cos(el), math.sin(el)))

    cd = bpy.data.cameras.new("CameraIcone")
    cd.lens = focale
    cd.sensor_fit = "VERTICAL"
    cam = bpy.data.objects.new("CameraIcone", cd)
    scene.collection.objects.link(cam)
    scene.camera = cam
    cam.rotation_euler = (-direction).to_track_quat("-Z", "Y").to_euler()

    distance = rayon * 4.0
    for _ in range(6):     # quelques essais suffisent : la mise à l'échelle converge vite
        cam.location = centre + direction * distance
        cd.shift_x = cd.shift_y = 0.0
        bpy.context.view_layer.update()
        ndc = [world_to_camera_view(scene, cam, p) for p in pts]
        xs, ys = [v.x for v in ndc], [v.y for v in ndc]
        h, w = max(ys) - min(ys), max(xs) - min(xs)
        f = max(h / hauteur, w / largeur_max)
        distance *= f
    cam.location = centre + direction * distance
    bpy.context.view_layer.update()
    ndc = [world_to_camera_view(scene, cam, p) for p in pts]
    xs, ys = [v.x for v in ndc], [v.y for v in ndc]
    # décalage de l'objectif pour centrer exactement la boîte englobante à l'image
    cd.shift_x = ((min(xs) + max(xs)) / 2 - 0.5)
    cd.shift_y = ((min(ys) + max(ys)) / 2 - 0.5)
    return cam, direction, centre, rayon


# ---------------------------------------------------------------------------------------
# 4. Rendu et fond uni
# ---------------------------------------------------------------------------------------
def regler_moteur(scene):
    """EEVEE (rapide, ombres douces, reflets du ciel). Couleurs « Standard » : le #hex tapé
    dans le script est exactement celui qu'on retrouve dans l'image."""
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    ee = scene.eevee
    ee.taa_render_samples = 64
    for attr, val in (("use_shadows", True), ("use_raytracing", True),
                      ("shadow_ray_count", 2), ("shadow_step_count", 8)):
        if hasattr(ee, attr):
            setattr(ee, attr, val)
    scene.render.resolution_x = scene.render.resolution_y = TAILLE
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"


def poser_sur_fond(chemin_tmp, chemin, fond_hex):
    """Pose le rendu (RGBA) sur une couleur unie : fond = couleur exacte, partout.
    V7 : on GARDE aussi le rendu transparent dans art/icones_alpha/ : detourer_icones.py
    s'en sert comme transparence exacte (bien meilleure qu'un détourage du fond lavande)."""
    img = bpy.data.images.load(chemin_tmp)
    w, h = img.size
    px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)
    bpy.data.images.remove(img)
    os.makedirs(DOSSIER_ALPHA, exist_ok=True)
    os.replace(chemin_tmp, os.path.join(DOSSIER_ALPHA, os.path.basename(chemin)))
    hexa = fond_hex.lstrip("#")
    fond = np.array([int(hexa[i:i + 2], 16) / 255 for i in (0, 2, 4)], dtype=np.float32)
    # Les pixels lus sont en sRGB (PNG 8 bits) : on mélange dans le même espace.
    a = px[..., 3:4]
    rgb = px[..., :3] * a + fond * (1 - a)
    out = np.concatenate([rgb, np.ones_like(a)], axis=2)
    res = bpy.data.images.new("Icone", w, h, alpha=False)
    res.colorspace_settings.name = "sRGB"
    res.pixels = out.ravel()
    res.filepath_raw = chemin
    res.file_format = "PNG"
    res.save()
    bpy.data.images.remove(res)


def retoucher_matieres(retouches):
    """Petites retouches de matière POUR L'ICÔNE SEULEMENT (le modèle exporté ne change pas).
    Ex. : l'eau du bocal, lumineuse et opaque à 55 %, blanchit le poisson rouge sur une
    image fixe ; on l'éclaircit un peu pour que l'on reconnaisse le poisson orange."""
    for nom, r in retouches.items():
        mat = bpy.data.materials.get(nom)
        if mat is None:
            print(f"  (matière {nom} absente)")
            continue
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if "alpha" in r:
            bsdf.inputs["Alpha"].default_value = r["alpha"]
        if "emission" in r:
            bsdf.inputs["Emission Strength"].default_value = r["emission"]


def rapprocher(cible, objets, facteur):
    """Rapproche des pièces MOBILES de la pièce `cible` (ex. les abeilles qui tournent
    autour du pot de miel). Dans le jeu, le code les fait voyager : sur une icône fixe,
    leur orbite complète rendrait le pot minuscule. On les place donc plus près, comme
    sur une photo prise au bon moment. facteur 0,5 = à mi-chemin."""
    bpy.context.view_layer.update()
    c = bpy.data.objects[cible]
    centre = sum((c.matrix_world @ Vector(b) for b in c.bound_box), Vector()) / 8
    for nom in objets:
        o = bpy.data.objects[nom]
        mw = o.matrix_world.copy()
        mw.translation = centre + (mw.translation - centre) * facteur
        o.matrix_world = mw
    bpy.context.view_layer.update()


def ouvrir_coffre(racine, angle, lueur=None):
    """V7 : entrouvre un coffre (rotation de sa Charniere, comme dans Unity) et, si `lueur`
    est une couleur, pose dedans un disque lumineux : la « lueur dorée » de la spec v3 §854.
    Pour l'icône seulement (le modèle exporté ne change pas)."""
    import coffre_rarete as R
    R.ouvrir(racine, angle)
    if lueur:
        corps = next(o for o in C.descendants(racine) if o.name == "Corps")
        bpy.context.view_layer.update()
        mn = Vector([min((corps.matrix_world @ Vector(b))[i] for b in corps.bound_box) for i in range(3)])
        mx = Vector([max((corps.matrix_world @ Vector(b))[i] for b in corps.bound_box) for i in range(3)])
        # un tas de pièces d'or (cylindres un peu penchés, placés de façon fixe : le rendu
        # est identique à chaque lancement) qui dépasse du bord et brille un peu
        import random
        hasard = random.Random(7)
        cx, cy = (mn.x + mx.x) / 2, (mn.y + mx.y) / 2
        lx, ly = (mx.x - mn.x) / 2 - 0.12, (mx.y - mn.y) / 2 - 0.12
        mat = bpy.data.materials.new("LueurIcone")
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes["Principled BSDF"]
        couleur = C.hex_vers_lineaire(lueur)
        bsdf.inputs["Base Color"].default_value = couleur
        bsdf.inputs["Metallic"].default_value = 0.85
        bsdf.inputs["Roughness"].default_value = 0.28
        bsdf.inputs["Emission Color"].default_value = couleur
        bsdf.inputs["Emission Strength"].default_value = 0.35
        pieces = []
        for couche, (n, r_rel, dz) in enumerate(((16, 1.0, -0.04), (10, 0.65, 0.02), (5, 0.32, 0.08))):
            for k in range(n):
                a = 2 * math.pi * (k + hasard.random() * 0.5) / n
                rr = r_rel * (0.55 + 0.45 * hasard.random())
                pos = (cx + math.cos(a) * lx * rr, cy + math.sin(a) * ly * rr,
                       mx.z + dz + hasard.uniform(-0.01, 0.01))
                bpy.ops.mesh.primitive_cylinder_add(vertices=14, radius=0.085, depth=0.026,
                                                    location=pos)
                p = bpy.context.active_object
                p.rotation_euler = (hasard.uniform(-0.5, 0.5), hasard.uniform(-0.5, 0.5), 0)
                p.data.materials.append(mat)
                pieces.append(p)
        C.selectionner(pieces, pieces[0])
        bpy.ops.object.join()
        disque = bpy.context.active_object
        disque.name = "LueurIcone"
        disque.parent = racine
        disque.matrix_parent_inverse = racine.matrix_world.inverted()
        lampe = bpy.data.lights.new("LueurIcone", "POINT")
        lampe.color = couleur[:3]
        lampe.energy = 60
        o = bpy.data.objects.new("LueurIconeLampe", lampe)
        o.location = disque.location + Vector((0, 0, 0.12))
        bpy.context.scene.collection.objects.link(o)


def rendre_icone(nom, reglage, ecraser=False):
    chemin = os.path.join(DOSSIER_ICONES, f"{nom}.png")
    if os.path.exists(chemin) and not ecraser:
        print(f"IGNORE {nom} : {chemin} existe déjà (ECRASER=1 pour le refaire)")
        return None
    racine = construire(reglage["source"])
    retoucher_matieres(reglage.get("matieres", {}))
    if "rapprocher" in reglage:
        rapprocher(**reglage["rapprocher"])
    if "ouvrir" in reglage:
        ouvrir_coffre(racine, reglage["ouvrir"], reglage.get("lueur"))
    objs = maillages(racine)
    tris = sum(C.triangles(o) for o in objs)
    scene = bpy.context.scene
    regler_moteur(scene)
    pts = points_monde(objs)
    cam, direction, centre, rayon = camera(
        scene, pts, reglage.get("elevation", ELEVATION), reglage.get("rotation", ROTATION),
        reglage.get("hauteur", HAUTEUR), reglage.get("largeur", LARGEUR_MAX))
    monde_studio(scene, reglage.get("ciel", 0.45))
    lumieres(scene, centre, rayon, direction)
    tmp = os.path.join(DOSSIER_ICONES, f"_tmp_{nom}.png")
    scene.render.filepath = tmp
    bpy.ops.render.render(write_still=True)
    poser_sur_fond(tmp, chemin, reglage.get("fond", LAVANDE))
    print(f"ICONE {nom} triangles={tris} -> {chemin}")
    return chemin


# ---------------------------------------------------------------------------------------
# 5. Les icônes. "source" = quel modèle ; le reste est facultatif :
#    elevation / rotation (degrés), hauteur (part de l'image), largeur (maximum),
#    fond (hex), ciel (force du ciel), matieres (retouches), rapprocher (pièces mobiles). Ajouter une ligne pour une nouvelle icône.
# ---------------------------------------------------------------------------------------
def animal(nom, **k):
    return dict(source=("animal", nom), **k)


ICONES = {
    "oreilles_chat": animal("oreilles_chat"),
    "oreilles_lapin": animal("oreilles_lapin", fond=BLEU_GRIS),
    # la truffe est vue presque de face : sinon on ne voit que son dessus noir
    "truffe": animal("truffe", elevation=8, hauteur=0.55, largeur=0.70),
    "collier_medaille": animal("collier_medaille"),
    "moustaches": animal("moustaches", fond=BLEU_GRIS, elevation=12, largeur=0.90),
    "bonnet_grenouille": animal("bonnet_grenouille"),
    "lunettes_hibou": animal("lunettes_hibou"),
    "echarpe_renard": animal("echarpe_renard"),
    # objets du dos : le « dos » du cheval-pièce d'échecs est l'arrière du cou, une surface
    # presque verticale. La carapace y pend debout, dôme vers l'arrière : on la voit de dos.
    "carapace": animal("carapace", rotation=200),
    "ailes_chauve_souris": animal("ailes_chauve_souris"),
    "bois_cerf": animal("bois_cerf"),
    "poussin": animal("poussin"),
    # le chaton regarde vers l'avant, depuis le côté droit du panier : on se place de ce côté
    "chaton_sac": animal("chaton_sac", rotation=-45, elevation=30),
    "ruche": animal("ruche", rapprocher=dict(
        cible="Pot", objets=["Anim_Abeille_1", "Anim_Abeille_2", "Anim_Abeille_3"],
        facteur=0.55)),
    "bocal_poisson": animal("bocal_poisson", fond=BLEU_GRIS,
                            matieres={"M5_EauBocal": {"alpha": 0.32, "emission": 0.1}}),
    # la baleine nage vers +X : vue de 3/4 avant, sa traînée d'étoiles part derrière elle
    "baleine_celeste": animal("baleine_celeste", rotation=40, elevation=20, largeur=0.90),
    # Boîtes : le nom de l'icône (boutique) et le nom du modèle 3D diffèrent.
    "boite_pirates_skins": dict(source=("boite", "boite_skins_pirates")),
    "boite_pirates_accessoires": dict(source=("boite", "boite_accessoires_pirates")),
    "boite_animaux_skins": dict(source=("boite", "boite_skins_animaux")),
    "boite_animaux_accessoires": dict(source=("boite", "boite_accessoires_animaux")),
    # v6.5 : icône « Mes skins » du menu de l'Inventaire (art/prompts/lot8.md)
    "icone_mes_skins": dict(source=("module", "armoire_skins")),
    # V7 : icônes des Améliorations, rendues à partir des VRAIS modèles de coffres du jeu
    # (avant : rendus v3 en 512 px sans contour). Même cadrage que les autres icônes.
    "coffre_bois": dict(source=("module", "coffre"), largeur=0.72),
    "coffre_bronze": dict(source=("module", "coffre_bronze"), largeur=0.72),
    "coffre_argent": dict(source=("module", "coffre_argent"), largeur=0.72),
    "coffre_or": dict(source=("module", "coffre_or"), largeur=0.72),
    "coffre_diamant": dict(source=("module", "coffre_diamant"), largeur=0.72),
    # V7 : le bébé kraken (violet sur une ombre violette) ne pouvait pas être détouré
    # proprement depuis l'image Gemini : on le rend depuis son modèle 3D, sans ombre.
    "kraken_bebe": dict(source=("module", "pirate_kraken_bebe"), elevation=8, rotation=15),
    # V7, lot 9 (art/prompts/lot9.md, images 3 et 4) : rendus au lieu de Gemini
    "sablier_bientot": dict(source=("fonction", "objets_lot9.construire_sablier")),
    "pack_diamants_tresor": dict(source=("fonction", "objets_lot9.construire_pack_tresor"),
                                 fond=BLEU_GRIS, elevation=22, hauteur=0.78, largeur=0.86),
    # « Coffres précieux » (spec v3 §854) : le coffre de diamant entrouvert, lueur dorée dedans
    "amelioration_coffres": dict(source=("module", "coffre_diamant"), ouvrir=45,
                                 lueur="#FFC83D", elevation=32, largeur=0.72),
    # V7, point 15 : icônes NEUTRES des 3 catégories de boîtes (blender/objets_icones_v7.py)
    "categorie_classiques": dict(source=("fonction", "objets_icones_v7.construire_categorie_classiques"),
                                 elevation=12, rotation=15),
    "categorie_pirates": dict(source=("fonction", "objets_icones_v7.construire_categorie_pirates"),
                              elevation=15, rotation=10, largeur=0.84),
    "categorie_animaux": dict(source=("fonction", "objets_icones_v7.construire_categorie_animaux"),
                              elevation=12, rotation=15),
    # V7, lots d'échange (docs/specs/v7.md §1) : la quantité de pièces grandit de l'un à l'autre
    "echange_poignee": dict(source=("fonction", "objets_icones_v7.construire_echange_poignee"), elevation=30, hauteur=0.50),
    "echange_pochette": dict(source=("fonction", "objets_icones_v7.construire_echange_pochette"), elevation=25, hauteur=0.58),
    "echange_sac": dict(source=("fonction", "objets_icones_v7.construire_echange_sac"), elevation=25, hauteur=0.64),
    "echange_seau": dict(source=("fonction", "objets_icones_v7.construire_echange_seau"), elevation=28, hauteur=0.68),
    "echange_coffre": dict(source=("fonction", "objets_icones_v7.construire_echange_coffre"), elevation=28, hauteur=0.70),
    "echange_malle": dict(source=("fonction", "objets_icones_v7.construire_echange_malle"), elevation=28, largeur=0.82),
    "echange_tonneau": dict(source=("fonction", "objets_icones_v7.construire_echange_tonneau"), elevation=28, rotation=30, largeur=0.84),
    "echange_montagne": dict(source=("fonction", "objets_icones_v7.construire_echange_montagne"), elevation=22, largeur=0.86),
}


def main():
    choix = os.environ.get("ICONES", "").split() or list(ICONES)
    ecraser = os.environ.get("ECRASER") == "1"
    for nom in choix:
        rendre_icone(nom, ICONES[nom], ecraser=ecraser)


if __name__ == "__main__":
    main()
