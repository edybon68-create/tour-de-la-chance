"""
commun.py — Boîte à outils partagée par tous les scripts de modélisation de Tour de la Chance.

Pourquoi un fichier commun ?
    Chaque objet (coffre, personnage, chapeaux…) a son propre script, mais ils doivent tous
    utiliser les MÊMES couleurs, les MÊMES réglages de biseau et le MÊME export FBX. Si on
    recopiait ces réglages dans chaque script, un jour l'un d'eux serait différent et les
    modèles ne s'emboîteraient plus dans Unity. Ici, on les écrit une seule fois.

Comment l'utiliser dans un script d'objet :
    import os, sys
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    import commun as C

Lancer un script (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/coffre.py

CONVENTION D'AXES (très important, vérifiée sur les exports) :
    Dans Blender, Z est vers le haut. Dans Unity, Y est vers le haut.
    On modélise l'AVANT de chaque objet (serrure, yeux, visière) tourné vers +Y dans Blender.
    Avec les réglages d'export ci-dessous, on obtient dans Unity :
        Unity.x = -Blender.x     Unity.y = Blender.z     Unity.z = -Blender.y
    donc l'avant (+Y Blender) arrive vers Z négatif dans Unity, face à la caméra.
    La fonction `depuis_unity()` fait la conversion inverse pour placer un point donné
    en coordonnées Unity (comme dans la bible de style).
"""

import math
import os
import random

import bmesh
import bpy
from mathutils import Matrix, Vector

# Dossier racine du projet (ce fichier est dans <projet>/blender/).
DOSSIER_PROJET = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DOSSIER_MODELES = os.path.join(DOSSIER_PROJET, "TourDeLaChance-Unity", "Assets", "Models")
DOSSIER_CAPTURES = os.path.join(DOSSIER_PROJET, "docs", "captures")

# ---------------------------------------------------------------------------------------
# 1. Palette et matériaux (repris de docs/art-bible.md §2 et §4)
# ---------------------------------------------------------------------------------------
# nom : (couleur hex sRGB, metallic, roughness). Roughness = 1 - smoothness Unity.
MATERIAUX = {
    "M_BoisCorps": ("#8C5933", 0.0, 0.75),
    "M_BoisCouvercle": ("#6B4226", 0.0, 0.75),
    "M_BoisClair": ("#A8703F", 0.0, 0.70),
    "M_Interieur": ("#3A2416", 0.0, 0.90),
    "M_MetalSombre": ("#4A4E5A", 0.8, 0.55),
    "M_Laiton": ("#C99A1E", 1.0, 0.40),
    "M_TrouSerrure": ("#1A1208", 0.0, 0.90),
    "M_Or": ("#FFC83A", 1.0, 0.20),
    "M_PersoCorps": ("#4A90D9", 0.0, 0.70),
    "M_Yeux": ("#111111", 0.0, 0.15),
    "M_Reflet": ("#FFFFFF", 0.0, 0.10),
    "M_TissuRouge": ("#E04040", 0.0, 0.80),
    "M_TissuRougeFonce": ("#A02828", 0.0, 0.80),
    "M_Blanc": ("#FFFFFF", 0.0, 0.80),
    "M_CartonRose": ("#FF6FB5", 0.0, 0.55),
    "M_CartonJaune": ("#FFD54A", 0.0, 0.55),
    "M_Feutre": ("#1E1E1E", 0.0, 0.85),
    "M_Satin": ("#C0392B", 0.0, 0.50),
    "M_Rubis": ("#D62839", 0.0, 0.10),
    # Décor : le sol de l'étage (bible §2.1 et §4)
    "M_PierreDalle": ("#5B6380", 0.0, 0.85),
    "M_PierreDalleVariante": ("#535A75", 0.0, 0.85),
    "M_PierreJoint": ("#3C4258", 0.0, 0.95),
    "M_PierreClaire": ("#8A90B0", 0.0, 0.75),
    "M_PierreFlanc": ("#434A63", 0.0, 0.90),
    "M_Incrustation": ("#6A4C9C", 0.3, 0.50),
}


def hex_vers_lineaire(hexa):
    """Convertit '#RRGGBB' (sRGB, ce qu'on tape dans le champ Hex) en couleur linéaire.
    Pourquoi : le champ Base Color du Principled BSDF attend des valeurs linéaires en
    Python ; sans conversion, toutes les couleurs sortiraient trop claires."""
    hexa = hexa.lstrip("#")
    resultat = []
    for i in (0, 2, 4):
        c = int(hexa[i:i + 2], 16) / 255.0
        resultat.append(c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4)
    return (*resultat, 1.0)


def materiau(nom):
    """Renvoie le matériau `nom` (le crée s'il n'existe pas encore).
    Un seul matériau par rôle : les objets d'un même rôle le partagent."""
    if nom in bpy.data.materials:
        return bpy.data.materials[nom]
    hexa, metal, rugosite = MATERIAUX[nom]
    mat = bpy.data.materials.new(nom)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = hex_vers_lineaire(hexa)
    bsdf.inputs["Metallic"].default_value = metal
    bsdf.inputs["Roughness"].default_value = rugosite
    # Couleur d'affichage dans la vue 3D (mode Solid) : pratique pour s'y retrouver.
    mat.diffuse_color = hex_vers_lineaire(hexa)
    return mat


# ---------------------------------------------------------------------------------------
# 2. Scène et objets
# ---------------------------------------------------------------------------------------
def nouvelle_scene():
    """Repart d'une scène vide (on est en mode --background : ce n'est jamais la scène
    de l'utilisateur, c'est une session neuve). Garantit que le script est relançable."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0  # 1 unité Blender = 1 mètre
    return scene


def depuis_unity(x, y, z):
    """Convertit un point donné en coordonnées Unity (Y en haut, avant = -Z) en
    coordonnées Blender (Z en haut, avant = +Y)."""
    return Vector((-x, -z, y))


def objet_depuis_bmesh(nom, bm, mats):
    """Crée un objet maillage à partir d'un bmesh. `mats` = liste de noms de matériaux ;
    l'indice de matériau de chaque face (face.material_index) pointe dans cette liste."""
    me = bpy.data.meshes.new(nom)
    bm.to_mesh(me)
    bm.free()
    for m in mats:
        me.materials.append(materiau(m))
    obj = bpy.data.objects.new(nom, me)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def vide(nom, position=(0, 0, 0), parent=None):
    """Objet vide (Empty) : un simple repère, sans géométrie. Dans Unity il devient un
    GameObject vide (un Transform) : parfait pour une charnière ou un point d'accroche."""
    obj = bpy.data.objects.new(nom, None)
    obj.empty_display_type = "PLAIN_AXES"
    obj.empty_display_size = 0.2
    obj.location = position
    bpy.context.scene.collection.objects.link(obj)
    if parent is not None:
        parenter(obj, parent)
    return obj


def parenter(enfant, parent):
    """Range `enfant` sous `parent` SANS le déplacer dans le monde.
    On calcule la position locale nous-mêmes au lieu d'utiliser la « matrice inverse »
    cachée de Blender, qui ne survit pas proprement à l'export FBX."""
    bpy.context.view_layer.update()
    monde = enfant.matrix_world.copy()
    enfant.parent = parent
    enfant.matrix_parent_inverse = Matrix.Identity(4)
    enfant.matrix_world = monde


def deplacer_origine(obj, point_monde):
    """Place l'origine (le pivot) de `obj` sur `point_monde`, sans bouger la géométrie."""
    decalage = Vector(point_monde) - obj.location
    obj.data.transform(Matrix.Translation(-decalage))
    obj.location = Vector(point_monde)


def selectionner(objs, actif=None):
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = actif or (objs[0] if objs else None)


def joindre(objs, nom):
    """Fusionne plusieurs objets maillages en UN seul (les matériaux sont conservés)."""
    selectionner(objs, objs[0])
    bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.name = nom
    obj.data.name = nom
    return obj


# ---------------------------------------------------------------------------------------
# 3. Briques de géométrie (toutes en bmesh, coordonnées Blender)
# ---------------------------------------------------------------------------------------
def bm_boite(bm, taille, centre, mat=0):
    """Ajoute une boîte (taille x, y, z en mètres) centrée sur `centre`."""
    res = bmesh.ops.create_cube(bm, size=1.0)
    m = Matrix.Translation(Vector(centre)) @ Matrix.Diagonal((*taille, 1.0))
    bmesh.ops.transform(bm, matrix=m, verts=res["verts"])
    for f in {f for v in res["verts"] for f in v.link_faces}:
        f.material_index = mat
    return res["verts"]


def boite(nom, taille, centre, mat):
    bm = bmesh.new()
    bm_boite(bm, taille, centre)
    return objet_depuis_bmesh(nom, bm, [mat])


def bm_prisme(bm, contour, epaisseur, axe="Y", decalage=0.0, mat=0):
    """Extrude un contour plat (liste de points 2D) sur une épaisseur.
    axe='Y' : contour dans le plan XZ, extrudé de `decalage` à `decalage+epaisseur` en Y.
    axe='X' : contour dans le plan YZ (points = (y, z)), extrudé en X.
    Le contour doit être donné dans le sens inverse des aiguilles d'une montre vu depuis
    le côté positif de l'axe (sinon les normales sont retournées : on les recalcule)."""
    def p3(a, b, t):
        if axe == "Y":
            return Vector((a, t, b))
        return Vector((t, a, b))

    bas = [bm.verts.new(p3(a, b, decalage)) for a, b in contour]
    haut = [bm.verts.new(p3(a, b, decalage + epaisseur)) for a, b in contour]
    faces = [bm.faces.new(bas[::-1]), bm.faces.new(haut)]
    n = len(contour)
    for i in range(n):
        j = (i + 1) % n
        faces.append(bm.faces.new((bas[i], bas[j], haut[j], haut[i])))
    for f in faces:
        f.material_index = mat
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    return bas + haut


def bm_anneaux(bm, anneaux, mats=None, fermer=True):
    """Relie une suite d'« anneaux » de sommets (listes de même longueur) par des quads.
    fermer=True : chaque anneau est une boucle (le dernier sommet rejoint le premier).
    Pratique pour un cadre (4 coins) ou une bande épaisse dont on connaît les contours."""
    rangs = [[bm.verts.new(p) for p in anneau] for anneau in anneaux]
    faces = []
    n = len(rangs[0])
    for i in range(len(rangs) - 1):
        A, B = rangs[i], rangs[i + 1]
        for s in range(n if fermer else n - 1):
            t = (s + 1) % n
            f = bm.faces.new((A[s], B[s], B[t], A[t]))
            f.material_index = 0 if mats is None else mats[i]
            faces.append(f)
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    return rangs


def bm_revolution(bm, profil, segments, mats=None, angle_depart=0.0, fermer_haut=False,
                  fermer_bas=False, rayon_fn=None):
    """Fait tourner un profil (liste de (rayon, hauteur)) autour de l'axe Z.
    - `mats` : un indice de matériau par tronçon du profil (len(profil)-1 valeurs).
    - un point de rayon 0 devient un pôle (triangles en éventail, pas de faces écrasées).
    - `fermer_haut/bas` : bouche le premier / dernier anneau par un polygone plat.
    - `rayon_fn(angle, i, r, z)` (facultatif) : permet de déformer un anneau selon l'angle.
    Renvoie la liste des anneaux de sommets."""
    anneaux = []
    for i, (r, z) in enumerate(profil):
        if r < 1e-6:
            anneaux.append([bm.verts.new((0, 0, z))])
            continue
        anneau = []
        for s in range(segments):
            a = angle_depart + 2 * math.pi * s / segments
            rr, zz = (r, z) if rayon_fn is None else rayon_fn(a, i, r, z)
            anneau.append(bm.verts.new((rr * math.cos(a), rr * math.sin(a), zz)))
        anneaux.append(anneau)
    faces = []
    for i in range(len(anneaux) - 1):
        A, B = anneaux[i], anneaux[i + 1]
        mi = 0 if mats is None else mats[i]
        for s in range(segments):
            t = (s + 1) % segments
            if len(A) == 1:
                f = bm.faces.new((A[0], B[s], B[t]))
            elif len(B) == 1:
                f = bm.faces.new((A[s], B[0], A[t]))
            else:
                f = bm.faces.new((A[s], B[s], B[t], A[t]))
            f.material_index = mi
            faces.append(f)
    if fermer_haut and len(anneaux[0]) > 1:
        f = bm.faces.new(anneaux[0])
        f.material_index = 0 if mats is None else mats[0]
        faces.append(f)
    if fermer_bas and len(anneaux[-1]) > 1:
        f = bm.faces.new(anneaux[-1])
        f.material_index = 0 if mats is None else mats[-1]
        faces.append(f)
    # On ne sait pas d'avance dans quel sens tourne le profil : Blender recalcule
    # des normales cohérentes vers l'extérieur.
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    return anneaux


def bm_sphere(bm, rayon, centre, segments=8, anneaux=6, echelle=(1, 1, 1), mat=0):
    """Sphère UV basse définition (pour yeux, perles, boutons)."""
    profil = [(0, rayon)]
    for k in range(1, anneaux):
        a = math.pi * k / anneaux
        profil.append((rayon * math.sin(a), rayon * math.cos(a)))
    profil.append((0, -rayon))
    avant = set(bm.verts)
    bm_revolution(bm, profil, segments, mats=[mat] * (len(profil) - 1))
    nouveaux = [v for v in bm.verts if v not in avant]
    m = Matrix.Translation(Vector(centre)) @ Matrix.Diagonal((*echelle, 1.0))
    bmesh.ops.transform(bm, matrix=m, verts=nouveaux)
    return nouveaux


# ---------------------------------------------------------------------------------------
# 4. Modificateurs et lissage
# ---------------------------------------------------------------------------------------
def biseau(obj, largeur=0.03, segments=1, angle=30.0, poids=False):
    """Ajoute un modificateur Bevel (arêtes adoucies, aspect « jouet verni »).
    - angle : seules les arêtes plus vives que cet angle sont biseautées.
    - poids=True : seules les arêtes marquées d'un poids de biseau (bevel_weight_edge)."""
    mod = obj.modifiers.new("Biseau", "BEVEL")
    mod.width = largeur
    mod.segments = segments
    mod.limit_method = "WEIGHT" if poids else "ANGLE"
    mod.angle_limit = math.radians(angle)
    mod.use_clamp_overlap = True
    mod.miter_outer = "MITER_ARC"
    return mod


def appliquer(obj):
    """Applique tous les modificateurs et les transformations (position/rotation/échelle
    « gravées » dans la géométrie). Après ça, ce qu'on voit = ce qui est exporté."""
    selectionner([obj], obj)
    for mod in list(obj.modifiers):
        bpy.ops.object.modifier_apply(modifier=mod.name)


def lisser_angle(obj, angle=30.0):
    """Lissage « Smooth by Angle » : faces lisses, mais arêtes plus vives que `angle`
    gardées nettes. Pour les objets durs (coffre, chapeaux rigides)."""
    selectionner([obj], obj)
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(angle), keep_sharp_edges=True)


def lisser_tout(obj):
    """Lissage complet (Shade Smooth) : pour les objets ronds (personnage, yeux, pompon)."""
    selectionner([obj], obj)
    bpy.ops.object.shade_smooth()


def triangles(obj):
    """Nombre de triangles du maillage (une fois triangulé, comme dans Unity)."""
    if obj.type != "MESH":
        return 0
    me = obj.evaluated_get(bpy.context.evaluated_depsgraph_get()).to_mesh()
    me.calc_loop_triangles()
    n = len(me.loop_triangles)
    obj.evaluated_get(bpy.context.evaluated_depsgraph_get()).to_mesh_clear()
    return n


def descendants(racine):
    res = [racine]
    for enfant in racine.children:
        res += descendants(enfant)
    return res


def rapport(racine):
    """Affiche la hiérarchie, les pivots et le nombre de triangles."""
    bpy.context.view_layer.update()
    total = 0
    print("\n=== Rapport :", racine.name, "===")
    for o in descendants(racine):
        t = triangles(o)
        total += t
        p = o.matrix_world.translation
        u = (-p.x, p.z, -p.y)
        mats = [m.name for m in o.data.materials] if o.type == "MESH" else []
        print(f"  {o.name:<16} {o.type:<6} pivot Unity=({u[0]:+.3f}; {u[1]:+.3f}; {u[2]:+.3f})"
              f"  tris={t:<5} {mats}")
    print(f"  TOTAL triangles = {total}\n")
    return total


# ---------------------------------------------------------------------------------------
# 5. Export FBX
# ---------------------------------------------------------------------------------------
# Rotation qui passe du repère Blender (Z en haut) au repère FBX/Unity (Y en haut) :
# -90° autour de X. (x, y, z) Blender devient (x, z, -y).
ROTATION_Y_HAUT = Matrix.Rotation(math.radians(-90.0), 4, "X")


def preparer_axes_unity(racine):
    """Passe toute la hiérarchie en « Y vers le haut » SANS rien changer à ce qu'on voit.

    Pourquoi ne pas simplement cocher `bake_space_transform` à l'export ?
        Nous l'avons essayé : ça marche pour les enfants directs de la racine, mais un
        petit-enfant (le Couvercle sous la Charniere) ressort avec une position et une
        rotation fausses dans Unity (rotation 90°, position décalée). On fait donc la
        conversion nous-mêmes, proprement :
        1. chaque maillage est tourné de -90° sur X (ses sommets sont alors en Y-haut) ;
        2. chaque objet reçoit une rotation de +90° qui compense, pour que la scène Blender
           ait exactement la même allure ;
        3. à l'export, Blender ajoute sa conversion d'axes (-90°) à la racine : +90 - 90 = 0.
    Résultat dans Unity : aucune rotation parasite sur aucun objet, positions locales en
    Y-haut (ex. Charniere en (0 ; 0,8 ; 0,5)) et maillages déjà dans le bon sens."""
    bpy.context.view_layer.update()
    objets = descendants(racine)  # du parent vers les enfants
    inverse = ROTATION_Y_HAUT.inverted()
    cibles = {o.name: o.matrix_world @ inverse for o in objets}
    for o in objets:
        if o.type == "MESH":
            o.data.transform(ROTATION_Y_HAUT)
    for o in objets:  # parents d'abord, puis enfants
        o.matrix_world = cibles[o.name]
        bpy.context.view_layer.update()


def exporter_fbx(racine, nom_fichier):
    """Exporte `racine` et ses enfants en FBX dans Assets/Models/.
    Réglages choisis pour Unity :
      - preparer_axes_unity() + axis_forward='-Z', axis_up='Y' : dans Unity, Y en haut,
        l'avant (+Y Blender) vers -Z, et AUCUNE rotation parasite sur les objets.
      - apply_scale_options='FBX_SCALE_ALL' : échelle 1 dans Unity (pas de facteur 100).
      - pas d'os « leaf bones », pas de caméra ni de lumière.
    Attention : modifie les objets (rotation compensée) ; à appeler en dernier, après le
    rapport et la miniature."""
    os.makedirs(DOSSIER_MODELES, exist_ok=True)
    chemin = os.path.join(DOSSIER_MODELES, nom_fichier)
    preparer_axes_unity(racine)
    selectionner(descendants(racine), racine)
    bpy.ops.export_scene.fbx(
        filepath=chemin,
        use_selection=True,
        object_types={"EMPTY", "MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=False,
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        # Triangulation faite par Blender : certains polygones sont concaves, et on veut
        # être sûr que Unity reçoive exactement les triangles que l'on a comptés.
        use_triangles=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
    )
    print("Exporté :", chemin)
    return chemin


def verifier_fbx(chemin):
    """Réimporte le FBX dans une scène vide et affiche, pour chaque objet, sa position
    et sa taille en coordonnées Unity. Sert à vérifier l'orientation et l'échelle."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=chemin, axis_forward="-Z", axis_up="Y")
    bpy.context.view_layer.update()
    print("\n=== Vérification du FBX réimporté :", os.path.basename(chemin), "===")
    for o in sorted(bpy.context.scene.objects, key=lambda o: o.name):
        p = o.matrix_world.translation
        ligne = f"  {o.name:<16} {o.type:<6} pos Unity=({-p.x:+.3f}; {p.z:+.3f}; {-p.y:+.3f})"
        ligne += f" echelle={tuple(round(s, 3) for s in o.matrix_world.to_scale())}"
        if o.type == "MESH":
            pts = [o.matrix_world @ v.co for v in o.data.vertices]
            mn = Vector((min(v.x for v in pts), min(v.y for v in pts), min(v.z for v in pts)))
            mx = Vector((max(v.x for v in pts), max(v.y for v in pts), max(v.z for v in pts)))
            # Bornes converties en Unity : x -> -x, y <- z, z -> -y
            ligne += (f"  Unity x[{-mx.x:+.3f};{-mn.x:+.3f}] y[{mn.z:+.3f};{mx.z:+.3f}]"
                      f" z[{-mx.y:+.3f};{-mn.y:+.3f}]")
        print(ligne)
    print()


# ---------------------------------------------------------------------------------------
# 6. Miniature de rendu (vue de 3/4 avant, comme la caméra du jeu)
# ---------------------------------------------------------------------------------------
def monde_nuit(scene, ambiance=1.0):
    """Monde : fond violet nuit uni (ce que voit la caméra) + lumière d'ambiance douce
    violette (ce que « voient » les objets), comme le ciel en Gradient de la scène Unity."""
    monde = bpy.data.worlds.new("Monde")
    scene.world = monde
    monde.use_nodes = True
    nt = monde.node_tree
    eclairage = nt.nodes["Background"]  # ce que « voient » les objets (ambiance)
    eclairage.inputs["Color"].default_value = hex_vers_lineaire("#8C7AB8")
    eclairage.inputs["Strength"].default_value = ambiance
    visible = nt.nodes.new("ShaderNodeBackground")  # ce que voit la caméra (fond uni)
    visible.inputs["Color"].default_value = hex_vers_lineaire("#1F1730")
    chemin_rayon = nt.nodes.new("ShaderNodeLightPath")
    melange = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(chemin_rayon.outputs["Is Camera Ray"], melange.inputs["Fac"])
    nt.links.new(eclairage.outputs["Background"], melange.inputs[1])
    nt.links.new(visible.outputs["Background"], melange.inputs[2])
    nt.links.new(melange.outputs["Shader"], nt.nodes["World Output"].inputs["Surface"])
    return monde


def rendre_miniature(racine, nom_fichier, recul=1.0):
    """Rend une image 640x640 de l'objet, vu de devant, d'au-dessus (≈35° comme la caméra
    du jeu) et un peu de côté (3/4). Fond violet nuit #1F1730, lumière chaude en haut à
    gauche, petit contre-jour violet, comme dans la bible de style."""
    scene = bpy.context.scene
    bpy.context.view_layer.update()
    objs = [o for o in descendants(racine) if o.type == "MESH"]
    pts = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    centre = (mn + mx) / 2
    taille = (mx - mn).length

    monde_nuit(scene)

    # Caméra : avant = +Y Blender. Légèrement décalée vers la gauche de l'image (+X Blender
    # quand on regarde l'avant) pour la vue de 3/4.
    direction = Vector((0.35, 1.0, 0.75)).normalized()
    cam_data = bpy.data.cameras.new("Camera")
    cam_data.lens = 50
    cam = bpy.data.objects.new("Camera", cam_data)
    scene.collection.objects.link(cam)
    distance = taille * 2.2 * recul
    cam.location = centre + direction * distance
    cam.rotation_euler = (-direction).to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam

    def lumiere(nom, type_, hexa, energie, pos):
        d = bpy.data.lights.new(nom, type_)
        d.color = hex_vers_lineaire(hexa)[:3]
        d.energy = energie
        o = bpy.data.objects.new(nom, d)
        o.location = pos
        o.rotation_euler = (centre - Vector(pos)).to_track_quat("-Z", "Y").to_euler()
        scene.collection.objects.link(o)
        return o

    # Lumière principale : en haut, à gauche de l'image (+X Blender), devant.
    lumiere("Soleil", "SUN", "#FFF1D6", 3.0, centre + Vector((3, 3, 5)))
    # Contre-jour violet derrière et au-dessus.
    lumiere("ContreJour", "POINT", "#B48CFF", 150.0 * taille, centre + Vector((-1, -3, 3)) * taille)

    # Moteur de rendu : EEVEE (rapide). Reflets du ciel pour que le métal ne soit pas noir.
    for moteur in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE"):
        try:
            scene.render.engine = moteur
            break
        except TypeError:
            continue
    scene.render.resolution_x = 640
    scene.render.resolution_y = 640
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "Standard"
    scene.render.image_settings.file_format = "PNG"
    os.makedirs(DOSSIER_CAPTURES, exist_ok=True)
    chemin = os.path.join(DOSSIER_CAPTURES, nom_fichier)
    scene.render.filepath = chemin

    # Cadrage automatique : la caméra recule jusqu'à voir tout l'objet.
    selectionner(objs, objs[0])
    try:
        ctx = {"scene": scene, "camera": cam}
        with bpy.context.temp_override(**ctx):
            bpy.ops.view3d.camera_to_view_selected()
        cam.location = cam.location + direction * taille * 0.25 * recul
    except Exception as e:  # en mode background, l'opérateur peut ne pas être disponible
        print("Cadrage auto indisponible, cadrage par distance :", e)

    bpy.ops.render.render(write_still=True)
    print("Miniature :", chemin)
    return chemin


def graine(n=7):
    """Graine fixe pour l'aléatoire : le même script donne toujours le même modèle."""
    random.seed(n)


# ---------------------------------------------------------------------------------------
# 7. Rendu « vue du jeu » (même caméra et même lumière que la scène Unity Tour.unity)
# ---------------------------------------------------------------------------------------
# Valeurs relevées dans Tour.unity (en coordonnées Unity) :
CAMERA_JEU_POSITION = (0.0, 5.0, -7.0)
CAMERA_JEU_INCLINAISON = 35.0      # degrés vers le bas
CAMERA_JEU_CHAMP = 60.0            # champ de vision vertical (field of view)
# Rotation de la Directional Light (quaternion Unity x, y, z, w).
SOLEIL_JEU_ROTATION = (0.40821788, -0.23456968, 0.10938163, 0.8754261)


def direction_depuis_unity(x, y, z):
    """Comme depuis_unity(), mais pour une direction (un vecteur, pas un point)."""
    return Vector((-x, -z, y))


def rendre_vue_jeu(nom_fichier, largeur=1280, hauteur=720, camera_unity=None,
                   inclinaison=None):
    """Rend la scène Blender ENTIÈRE telle que la verrait la caméra du jeu.
    Pourquoi : un sol se juge depuis le point de vue du joueur (ce qui est caché par les
    coffres, ce qui sort de l'écran…), pas en vue de 3/4 générique.
    `camera_unity` / `inclinaison` permettent un point de vue plus large pour un aperçu."""
    scene = bpy.context.scene
    monde_nuit(scene, ambiance=0.8)

    pos = camera_unity or CAMERA_JEU_POSITION
    incl = math.radians(inclinaison if inclinaison is not None else CAMERA_JEU_INCLINAISON)
    cam_data = bpy.data.cameras.new("CameraJeu")
    cam_data.sensor_fit = "VERTICAL"          # Unity donne le champ de vision VERTICAL
    cam_data.angle_y = math.radians(CAMERA_JEU_CHAMP)
    cam_data.clip_start = 0.3
    cam_data.clip_end = 1000
    cam = bpy.data.objects.new("CameraJeu", cam_data)
    scene.collection.objects.link(cam)
    cam.location = depuis_unity(*pos)
    # Regard Unity : vers +Z, penché de `incl` vers le bas.
    regard = direction_depuis_unity(0.0, -math.sin(incl), math.cos(incl))
    cam.rotation_euler = regard.to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam

    # Soleil : même orientation que la Directional Light de Unity (elle éclaire vers son +Z).
    from mathutils import Quaternion
    qx, qy, qz, qw = SOLEIL_JEU_ROTATION
    d_unity = Quaternion((qw, qx, qy, qz)) @ Vector((0, 0, 1))
    d = direction_depuis_unity(*d_unity)
    soleil_data = bpy.data.lights.new("Soleil", "SUN")
    soleil_data.color = hex_vers_lineaire("#FFF1D6")[:3]
    soleil_data.energy = 3.0
    soleil_data.angle = math.radians(3)       # ombres douces
    soleil = bpy.data.objects.new("Soleil", soleil_data)
    soleil.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
    scene.collection.objects.link(soleil)
    # Contre-jour violet derrière le personnage, comme dans Tour.unity (0 ; 3,5 ; 4).
    cj_data = bpy.data.lights.new("ContreJour", "POINT")
    cj_data.color = hex_vers_lineaire("#B48CFF")[:3]
    cj_data.energy = 300.0
    cj = bpy.data.objects.new("ContreJour", cj_data)
    cj.location = depuis_unity(0, 3.5, 4)
    scene.collection.objects.link(cj)

    for moteur in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE"):
        try:
            scene.render.engine = moteur
            break
        except TypeError:
            continue
    scene.render.resolution_x = largeur
    scene.render.resolution_y = hauteur
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "Standard"
    scene.render.image_settings.file_format = "PNG"
    os.makedirs(DOSSIER_CAPTURES, exist_ok=True)
    chemin = os.path.join(DOSSIER_CAPTURES, nom_fichier)
    scene.render.filepath = chemin
    bpy.ops.render.render(write_still=True)
    # On retire caméra et lumières : on peut rappeler la fonction pour un autre point de vue.
    for o in (cam, soleil, cj):
        bpy.data.objects.remove(o, do_unlink=True)
    print("Rendu vue jeu :", chemin)
    return chemin


def importer_fbx(chemin, position_unity=(0, 0, 0)):
    """Importe un FBX déjà exporté (ex. coffre.fbx) et le place à une position Unity.
    Sert uniquement aux rendus de contrôle (jamais exporté avec l'objet en cours).
    Renvoie la liste des objets importés."""
    avant = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=chemin, axis_forward="-Z", axis_up="Y")
    nouveaux = [o for o in bpy.data.objects if o not in avant]
    for o in nouveaux:
        if o.parent is None:
            o.location = depuis_unity(*position_unity) + o.location
    return nouveaux
