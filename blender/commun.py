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
# Dossier d'attente : modèles prêts mais pas encore intégrés dans Unity (par exemple quand
# l'auteur est en train de jouer dans l'éditeur et qu'il ne faut rien toucher dans Assets/).
DOSSIER_EXPORTS = os.path.join(DOSSIER_PROJET, "blender", "exports")

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
    # Accessoires de la « Boîte d'accessoires n°1 » (docs/specs/boites.md §5.6, bible §2.8).
    # Un 4e nombre (facultatif) = intensité d'émission (lumière propre), bible §4.1 :
    # seulement pour l'auréole (Légendaire) et l'anneau de la planète (Secret).
    "M_LaineTurquoise": ("#2BB8C4", 0.0, 0.85),
    "M_LaineBlanche": ("#F4EEE6", 0.0, 0.85),
    "M_LaineMenthe": ("#3CC48A", 0.0, 0.85),
    "M_VerreSombre": ("#2A2A3A", 0.0, 0.10),
    "M_SatinFonce": ("#8E2A20", 0.0, 0.50),
    "M_VerreRose": ("#FF6FB5", 0.0, 0.20),
    "M_BlancCasse": ("#F4EEE6", 0.0, 0.50),
    "M_PetaleRose": ("#FF6FB5", 0.0, 0.60),
    "M_PetaleBlanc": ("#F4EEE6", 0.0, 0.60),
    "M_CoeurFleur": ("#FFD54A", 0.0, 0.60),
    "M_FeutreViolet": ("#4B2E83", 0.0, 0.80),
    "M_RubanJaune": ("#FFD54A", 0.0, 0.50),
    "M_MasqueBleu": ("#3A5BD9", 0.0, 0.50),
    "M_MasqueBord": ("#22357F", 0.0, 0.50),
    "M_Plume": ("#F4EEE6", 0.0, 0.70),
    "M_PlumeOmbre": ("#D9D2E4", 0.0, 0.70),
    "M_AilePapillon": ("#2BB8C4", 0.0, 0.40),
    "M_TachePapillon": ("#F28C38", 0.0, 0.40),
    "M_BordPapillon": ("#34286A", 0.0, 0.40),
    "M_Aureole": ("#FFF1A8", 0.0, 0.30, 1.5),
    "M_Planete": ("#B48CFF", 0.0, 0.40),
    "M_AnneauPlanete": ("#FFD54A", 0.0, 0.40, 0.5),
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
    hexa, metal, rugosite = MATERIAUX[nom][:3]
    emission = MATERIAUX[nom][3] if len(MATERIAUX[nom]) > 3 else 0.0
    mat = bpy.data.materials.new(nom)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = hex_vers_lineaire(hexa)
    bsdf.inputs["Metallic"].default_value = metal
    bsdf.inputs["Roughness"].default_value = rugosite
    if emission > 0:
        # Lumière propre de la même couleur que la base (bible §4.1). Dans Unity, le
        # programmeur active le mot-clé _EMISSION et règle _EmissionColor = couleur × intensité.
        bsdf.inputs["Emission Color"].default_value = hex_vers_lineaire(hexa)
        bsdf.inputs["Emission Strength"].default_value = emission
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


def exporter_fbx(racine, nom_fichier, dossier=None):
    """Exporte `racine` et ses enfants en FBX dans Assets/Models/.
    Réglages choisis pour Unity :
      - preparer_axes_unity() + axis_forward='-Z', axis_up='Y' : dans Unity, Y en haut,
        l'avant (+Y Blender) vers -Z, et AUCUNE rotation parasite sur les objets.
      - apply_scale_options='FBX_SCALE_ALL' : échelle 1 dans Unity (pas de facteur 100).
      - pas d'os « leaf bones », pas de caméra ni de lumière.
    Attention : modifie les objets (rotation compensée) ; à appeler en dernier, après le
    rapport et la miniature."""
    dossier = dossier or DOSSIER_MODELES  # `dossier` : par ex. DOSSIER_EXPORTS (attente)
    os.makedirs(dossier, exist_ok=True)
    chemin = os.path.join(dossier, nom_fichier)
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
        if o.parent is not None:
            # Rotation locale (par rapport au parent), en degrés, axes Blender du fichier.
            r = o.matrix_local.to_euler()
            ligne += f" rotLocale=({math.degrees(r.x):+.1f};{math.degrees(r.y):+.1f};{math.degrees(r.z):+.1f})"
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


def rendre_miniature(racine, nom_fichier, recul=1.0, direction=None):
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
    # `direction` (facultatif) : d'où l'on regarde, en axes Blender (+Y = devant).
    direction = Vector(direction or (0.35, 1.0, 0.75)).normalized()
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
    soleil, cj = lumieres_jeu(scene)
    regler_rendu(scene, largeur, hauteur)
    os.makedirs(DOSSIER_CAPTURES, exist_ok=True)
    chemin = os.path.join(DOSSIER_CAPTURES, nom_fichier)
    scene.render.filepath = chemin
    bpy.ops.render.render(write_still=True)
    # On retire caméra et lumières : on peut rappeler la fonction pour un autre point de vue.
    for o in (cam, soleil, cj):
        bpy.data.objects.remove(o, do_unlink=True)
    print("Rendu vue jeu :", chemin)
    return chemin


def lumieres_jeu(scene):
    """Soleil et contre-jour placés comme dans Tour.unity. Renvoie (soleil, contre_jour)."""
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
    return soleil, cj


def regler_rendu(scene, largeur, hauteur):
    """Moteur EEVEE, taille de l'image, couleurs « Standard », sortie PNG."""
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


# ---------------------------------------------------------------------------------------
# 8. Accessoires portés (Boîte d'accessoires n°1, docs/specs/boites.md §5.4 et §5.6)
# ---------------------------------------------------------------------------------------
# Où est le personnage dans la scène Tour.unity (Unity) : sa base est en (0 ; 0 ; 2).
JOUEUR_POSITION = (0.0, 0.0, 2.0)

# Points d'accroche : objets vides enfants de `Joueur`, positions LOCALES à Joueur (Unity).
# Chaque accessoire a son origine pile sur son point d'accroche : dans Unity on l'instancie
# comme enfant du point, position et rotation locales à zéro.
ACCROCHES = {
    "tete": ("PointChapeau", (0.0, 2.0, 0.0)),    # sommet de la tête (existant)
    "visage": ("PointVisage", (0.0, 1.6, -0.49)),  # devant, à hauteur des yeux
    "cou": ("PointCou", (0.0, 1.3, 0.0)),          # centre du corps à hauteur du « col »
    "dos": ("PointDos", (0.0, 1.3, 0.5)),          # milieu du dos
}

# Mesures du personnage (blender/personnage.py), en coordonnées Unity locales à Joueur.
TETE_CENTRE_Y = 1.5     # centre de la demi-sphère du haut
TETE_RAYON = 0.5


def rayon_corps(y):
    """Rayon de la capsule à la hauteur y (Unity, local à Joueur). Sert à coller un
    accessoire contre le corps sans le traverser (écharpe, collier, ailes)."""
    if y >= TETE_CENTRE_Y:
        return math.sqrt(max(TETE_RAYON ** 2 - (y - TETE_CENTRE_Y) ** 2, 0.0))
    if y >= 0.775:  # le tronc s'évase de 0,50 à 0,525 en descendant
        return 0.5 + 0.025 * (TETE_CENTRE_Y - y) / 0.725
    if y >= 0.525:
        return 0.525
    return math.sqrt(max(0.525 ** 2 - (0.525 - y) ** 2, 0.0))


def perso(x, y, z):
    """Point donné en Unity LOCAL À JOUEUR -> coordonnées Blender « personnage ».
    Les scripts d'accessoires construisent dans ce repère (plus facile pour coller au
    corps), puis `vers_accroche()` ramène tout sur le point d'accroche."""
    return depuis_unity(x, y, z)


def matrice_accroche(emplacement):
    """Translation qui amène le point d'accroche de l'emplacement à l'origine."""
    return Matrix.Translation(-depuis_unity(*ACCROCHES[emplacement][1]))


def vers_accroche(obj, emplacement):
    """Pour un accessoire en UN seul maillage construit dans le repère « personnage » :
    décale sa géométrie pour que le point d'accroche devienne son origine.
    (Pour un accessoire en plusieurs pièces, on range les pièces sous un objet vide placé
    sur le point d'accroche, puis on ramène ce vide à l'origine : voir `racine_accroche`.)"""
    obj.data.transform(matrice_accroche(emplacement))
    obj.location = (0, 0, 0)
    return obj


def racine_accroche(nom, emplacement, enfants):
    """Crée l'objet vide racine `nom` sur le point d'accroche (repère « personnage »),
    y range les `enfants` sans les déplacer, puis ramène le tout à l'origine : chaque
    pièce garde son pivot, et l'origine de l'ensemble = le point d'accroche."""
    racine = vide(nom, depuis_unity(*ACCROCHES[emplacement][1]))
    for e in enfants:
        parenter(e, racine)
    racine.location = (0, 0, 0)
    bpy.context.view_layer.update()
    return racine


def repere(position, avant, haut=Vector((0, 0, 1))):
    """Matrice d'un petit repère local : axe Y local = `avant` (l'épaisseur d'une plaque),
    axe Z local = au plus près de `haut`, origine = `position`."""
    # Base orthonormée : Y = avant, X = perpendiculaire à avant et haut, Z = le reste.
    y = Vector(avant).normalized()
    x = y.cross(Vector(haut)).normalized()
    z = x.cross(y)
    rot = Matrix((x, y, z)).transposed().to_4x4()
    return Matrix.Translation(Vector(position)) @ rot


def contour_ellipse(rx, rz, n=16, cx=0.0, cz=0.0, depart=0.0):
    """Contour 2D (x, z) d'une ellipse, sens inverse des aiguilles d'une montre."""
    return [(cx + rx * math.cos(depart + 2 * math.pi * k / n),
             cz + rz * math.sin(depart + 2 * math.pi * k / n)) for k in range(n)]


def contour_rectangle_arrondi(largeur, hauteur, rayon, par_coin=3, cx=0.0, cz=0.0):
    """Contour 2D (x, z) d'un rectangle aux coins arrondis, centré sur (cx, cz)."""
    pts = []
    lx, lz = largeur / 2 - rayon, hauteur / 2 - rayon
    for i, (sx, sz) in enumerate(((1, 1), (-1, 1), (-1, -1), (1, -1))):
        a0 = math.pi / 2 * i
        for k in range(par_coin + 1):
            a = a0 + (math.pi / 2) * k / par_coin
            pts.append((cx + sx * lx + rayon * math.cos(a), cz + sz * lz + rayon * math.sin(a)))
    return pts


def bm_plaque(bm, contour, epaisseur, matrice=None, mat=0):
    """Plaque plate : `contour` 2D (x = côté, z = haut) extrudé sur `epaisseur`, centré sur
    y = 0 (l'avant de la plaque regarde +Y local), puis placé par `matrice`."""
    verts = bm_prisme(bm, contour, epaisseur, axe="Y", decalage=-epaisseur / 2, mat=mat)
    if matrice is not None:
        bmesh.ops.transform(bm, matrix=matrice, verts=verts)
    return verts


def bm_tore(bm, grand_rayon, petit_rayon, segments=24, segments_tube=8, mat=0,
            matrice=None, rayon_fn=None):
    """Tore horizontal (anneau, bouée) centré sur l'origine, dans le plan XY Blender.
    `rayon_fn(angle)` (facultatif) fait varier l'épaisseur du tube selon l'angle."""
    anneaux = []
    for s in range(segments):
        a = 2 * math.pi * s / segments
        r_tube = petit_rayon if rayon_fn is None else rayon_fn(a)
        anneau = []
        for t in range(segments_tube):
            b = 2 * math.pi * t / segments_tube
            rr = grand_rayon + r_tube * math.cos(b)
            anneau.append((rr * math.cos(a), rr * math.sin(a), r_tube * math.sin(b)))
        anneaux.append(anneau)
    rangs = [[bm.verts.new(p) for p in anneau] for anneau in anneaux]
    faces = []
    for s in range(segments):
        A, B = rangs[s], rangs[(s + 1) % segments]
        for t in range(segments_tube):
            u = (t + 1) % segments_tube
            f = bm.faces.new((A[t], B[t], B[u], A[u]))
            f.material_index = mat
            faces.append(f)
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    verts = [v for r in rangs for v in r]
    if matrice is not None:
        bmesh.ops.transform(bm, matrix=matrice, verts=verts)
    return verts


def rendre_porte(racine, nom_fichier, emplacement, taille=640, y_min=0.85):
    """Miniature de l'accessoire PORTÉ par le personnage, vue depuis la position de la
    caméra du jeu (0 ; 5 ; −7), avec la lumière de Tour.unity.
    - importe `personnage.fbx` à sa place dans la scène (0 ; 0 ; 2) ;
    - pose l'accessoire sur son point d'accroche ;
    - vise le haut du personnage et resserre le champ de vision (un « zoom » : la
      perspective est exactement celle du joueur, l'image est simplement recadrée).
    Tout est remis comme avant à la fin (l'accessoire revient à l'origine pour l'export)."""
    scene = bpy.context.scene
    bpy.context.view_layer.update()
    depart = racine.location.copy()
    racine.location = depart + depuis_unity(*JOUEUR_POSITION) \
        + depuis_unity(*ACCROCHES[emplacement][1])
    perso_objs = importer_fbx(os.path.join(DOSSIER_MODELES, "personnage.fbx"), JOUEUR_POSITION)
    bpy.context.view_layer.update()
    monde_nuit(scene, ambiance=0.8)

    # Points à faire entrer dans l'image : l'accessoire entier + le haut du personnage
    # (au-dessus de y_min, là où on voit les accessoires depuis la caméra).
    pts = []
    for o in descendants(racine):
        if o.type == "MESH":
            pts += [o.matrix_world @ Vector(c) for c in o.bound_box]
    for o in perso_objs:
        if o.type == "MESH":
            for c in o.bound_box:
                p = o.matrix_world @ Vector(c)
                p.z = max(p.z, y_min)
                pts.append(p)
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    cible = (mn + mx) / 2

    cam_data = bpy.data.cameras.new("CameraPortee")
    cam_data.clip_start = 0.3
    cam = bpy.data.objects.new("CameraPortee", cam_data)
    scene.collection.objects.link(cam)
    cam.location = depuis_unity(*CAMERA_JEU_POSITION)
    regard = (cible - cam.location).normalized()
    cam.rotation_euler = regard.to_track_quat("-Z", "Y").to_euler()
    bpy.context.view_layer.update()
    inverse = cam.matrix_world.inverted()
    m = 0.0
    for p in pts:
        q = inverse @ p
        m = max(m, abs(q.x / -q.z), abs(q.y / -q.z))
    cam_data.sensor_fit = "AUTO"
    cam_data.angle = 2 * math.atan(m * 1.10)
    scene.camera = cam

    soleil, cj = lumieres_jeu(scene)
    regler_rendu(scene, taille, taille)
    os.makedirs(DOSSIER_CAPTURES, exist_ok=True)
    chemin = os.path.join(DOSSIER_CAPTURES, nom_fichier)
    scene.render.filepath = chemin
    bpy.ops.render.render(write_still=True)
    print("Miniature portée :", chemin)

    for o in [cam, soleil, cj] + perso_objs:
        bpy.data.objects.remove(o, do_unlink=True)
    racine.location = depart
    bpy.context.view_layer.update()
    return chemin


def finir_accessoire(racine, nom, emplacement):
    """Fin commune des scripts d'accessoires : rapport, miniature portée, export, contrôle."""
    C_total = rapport(racine)
    rendre_porte(racine, f"blender_{nom}.png", emplacement)
    chemin = exporter_fbx(racine, f"{nom}.fbx")
    verifier_fbx(chemin)
    print(f"RESUME {nom} triangles={C_total}")
    return C_total


def bm_balayage(bm, chemin, section, haut=Vector((0, 0, 1)), mat=0, boucher=True):
    """Fait glisser une `section` 2D (liste de (côté, haut)) le long d'un `chemin` (liste de
    points 3D) : idéal pour une branche de lunettes, un pan d'écharpe, une bande.
    À chaque point, le repère suit la direction du chemin. Bouts bouchés si `boucher`."""
    chemin = [Vector(p) for p in chemin]
    anneaux = []
    for i, p in enumerate(chemin):
        if i == 0:
            t = chemin[1] - chemin[0]
        elif i == len(chemin) - 1:
            t = chemin[-1] - chemin[-2]
        else:
            t = chemin[i + 1] - chemin[i - 1]
        t.normalize()
        cote = t.cross(Vector(haut)).normalized()
        h = cote.cross(t).normalized()
        anneaux.append([p + cote * a + h * b for a, b in section])
    rangs = bm_anneaux(bm, anneaux, mats=[mat] * (len(anneaux) - 1))
    faces = []
    if boucher:
        for r in (rangs[0], rangs[-1]):
            f = bm.faces.new(r)
            f.material_index = mat
            faces.append(f)
    bmesh.ops.recalc_face_normals(bm, faces=list({f for r in rangs for v in r for f in v.link_faces}))
    return [v for r in rangs for v in r]


def repere_verre(x_unity, ecart=0.56, y_unity=1.6):
    """Repère d'un verre de lunettes posé devant l'œil situé en x_unity (±0,18), en
    coordonnées Blender « personnage » : origine au centre du verre, Y local = normale de
    la tête (le verre épouse la courbe du visage), Z local ≈ vers le haut.
    `ecart` = distance au centre de la tête (0,5 = la peau ; les yeux ressortent à 0,53)."""
    centre_tete = perso(0, TETE_CENTRE_Y, 0)
    bx = -x_unity
    dz = y_unity - TETE_CENTRE_Y
    by = math.sqrt(TETE_RAYON ** 2 - bx ** 2 - dz ** 2)
    n = Vector((bx, by, dz)).normalized()
    return repere(centre_tete + n * ecart, n)


# ---------------------------------------------------------------------------------------
# 9. Le personnage « cheval » (cavalier d'échecs, blender/personnage_cheval.py)
# ---------------------------------------------------------------------------------------
# Position du personnage dans la scène du jeu (sur l'estrade), en coordonnées Unity.
CHEVAL_POSITION = (0.0, 0.3, 2.8)
CHEVAL_FBX = os.path.join(DOSSIER_EXPORTS, "personnage_cheval.fbx")


def _bornes(pts):
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return mn, mx


def estrade(position_unity=CHEVAL_POSITION):
    """Petite estrade grise sous le personnage (contexte pour les rendus seulement)."""
    bm = bmesh.new()
    bm_revolution(bm, [(0.0, 0.3), (0.95, 0.3), (1.0, 0.27), (1.0, 0.0), (0.0, 0.0)], 32)
    o = objet_depuis_bmesh("Estrade", bm, ["M_PierreClaire"])
    o.location = depuis_unity(position_unity[0], 0, position_unity[2])
    lisser_angle(o, 30)
    return o


def rendre_camera_jeu(objets, nom_fichier, taille=640, y_min=None, marge=1.10,
                      camera_unity=None):
    """Rend `objets` vus depuis la position de la caméra du jeu (0 ; 5 ; −7), avec la
    lumière de Tour.unity. La caméra VISE les objets et resserre son champ (un « zoom ») :
    la perspective est exactement celle du joueur, l'image est seulement recadrée.
    `y_min` (hauteur Blender z) : ignore ce qui est plus bas (pour cadrer sur la tête)."""
    scene = bpy.context.scene
    bpy.context.view_layer.update()
    pts = []
    for o in objets:
        if o.type != "MESH":
            continue
        for c in o.bound_box:
            p = o.matrix_world @ Vector(c)
            if y_min is not None:
                p.z = max(p.z, y_min)
            pts.append(p)
    mn, mx = _bornes(pts)
    cible = (mn + mx) / 2
    monde_nuit(scene, ambiance=0.8)
    cam_data = bpy.data.cameras.new("CameraJeu")
    cam_data.clip_start = 0.3
    cam = bpy.data.objects.new("CameraJeu", cam_data)
    scene.collection.objects.link(cam)
    cam.location = depuis_unity(*(camera_unity or CAMERA_JEU_POSITION))
    cam.rotation_euler = (cible - cam.location).normalized().to_track_quat("-Z", "Y").to_euler()
    bpy.context.view_layer.update()
    inverse = cam.matrix_world.inverted()
    m = max(max(abs(q.x / -q.z), abs(q.y / -q.z)) for q in (inverse @ p for p in pts))
    cam_data.sensor_fit = "AUTO"
    cam_data.angle = 2 * math.atan(m * marge)
    scene.camera = cam
    soleil, cj = lumieres_jeu(scene)
    regler_rendu(scene, taille, taille)
    os.makedirs(DOSSIER_CAPTURES, exist_ok=True)
    chemin = os.path.join(DOSSIER_CAPTURES, nom_fichier)
    scene.render.filepath = chemin
    bpy.ops.render.render(write_still=True)
    for o in (cam, soleil, cj):
        bpy.data.objects.remove(o, do_unlink=True)
    print("Rendu caméra du jeu :", chemin)
    return chemin


def poser_sur_point(racines, point, echelle=1.0, decalage=(0, 0, 0)):
    """Pose un accessoire (ses objets racines, origine = point d'accroche) sur l'objet vide
    `point` du cheval, avec une échelle uniforme et un décalage LOCAL donné en Unity —
    exactement ce que fera Unity : enfant du point, localPosition = décalage,
    localScale = échelle."""
    # On ne reprend que la POSITION du point : les objets importés d'un FBX portent déjà
    # une rotation de conversion d'axes, qu'il ne faut pas appliquer deux fois.
    bpy.context.view_layer.update()
    position = point.matrix_world.translation + direction_depuis_unity(*decalage)
    for r in racines:
        r.matrix_world = (Matrix.Translation(position)
                          @ Matrix.Diagonal((echelle, echelle, echelle, 1.0)) @ r.matrix_world)
    bpy.context.view_layer.update()


def importer_cheval(position_unity=CHEVAL_POSITION):
    """Importe le cheval exporté (blender/exports) à sa place sur l'estrade.
    Renvoie (objets, dictionnaire nom -> objet)."""
    objs = importer_fbx(CHEVAL_FBX, position_unity)
    bpy.context.view_layer.update()
    return objs, {o.name: o for o in objs}


def rendre_porte_cheval(racine, nom_fichier, point, echelle=1.0, decalage=(0, 0, 0),
                        y_min=1.0):
    """Miniature d'un accessoire en cours de construction PORTÉ par le cheval, vue de la
    caméra du jeu. L'accessoire est remis à l'origine après le rendu (pour l'export)."""
    depart = racine.matrix_world.copy()
    objs, par_nom = importer_cheval()
    poser_sur_point([racine], par_nom[point], echelle, decalage)
    hauteur_estrade = CHEVAL_POSITION[1]
    chemin = rendre_camera_jeu(objs + descendants(racine), nom_fichier,
                               y_min=hauteur_estrade + y_min)
    for o in objs:
        bpy.data.objects.remove(o, do_unlink=True)
    racine.matrix_world = depart
    bpy.context.view_layer.update()
    return chemin


def finir_accessoire_cheval(racine, nom, point, echelle=1.0, decalage=(0, 0, 0)):
    """Fin des scripts d'accessoires « version cheval » : rapport, miniature portée par le
    cheval (docs/captures/cheval_<nom>.png), export dans blender/exports/ (attente)."""
    total = rapport(racine)
    rendre_porte_cheval(racine, f"cheval_{nom}.png", point, echelle, decalage)
    chemin = exporter_fbx(racine, f"{nom}.fbx", dossier=DOSSIER_EXPORTS)
    verifier_fbx(chemin)
    print(f"RESUME {nom} triangles={total}")
    return total


# Points d'accroche du cheval, en Unity LOCAL à Joueur. Valeurs MESURÉES sur le modèle par
# personnage_cheval.py (il les affiche à chaque lancement) : si on change le cheval, on
# recopie ici les nouvelles valeurs, puis on relance les accessoires « version cheval ».
CHEVAL_ACCROCHES = {
    "PointChapeau": (0.0, 1.86, 0.03),     # dessus du crâne, entre les oreilles
    "PointVisage": (0.0, 1.68, -0.253),    # devant les yeux, sur le front
    "PointCou": (0.0, 1.05, 0.01),         # centre du cou, sous la mâchoire
    "PointDos": (0.0, 0.95, 0.296),        # surface arrière du cou, à hauteur d'épaules
}
CHEVAL_YEUX_X = 0.165          # yeux en Unity x = ±0,165 ...
CHEVAL_YEUX_Y = 1.68           # ... et y = 1,68
CHEVAL_YEUX_AVANT = -0.262     # le devant des yeux (Unity z)
CHEVAL_CRANE_CENTRE = (0.0, 1.58, 0.03)   # centre du crâne (Unity)


def vers_accroche_cheval(obj, point):
    """Comme vers_accroche(), pour un accessoire construit dans le repère du cheval
    (base du cheval à l'origine) : son origine devient le point d'accroche `point`."""
    obj.data.transform(Matrix.Translation(-depuis_unity(*CHEVAL_ACCROCHES[point])))
    obj.location = (0, 0, 0)
    return obj


def arbre_cheval():
    """Charge le cheval (blender/exports) le temps de mesurer sa surface, et renvoie un
    « arbre de recherche » (BVH) de son Corps, en coordonnées Blender, base à l'origine.
    Les accessoires s'en servent pour se coller à la vraie forme de la tête par des rayons.
    Le cheval est retiré de la scène ensuite (il n'est jamais exporté avec l'accessoire)."""
    from mathutils.bvhtree import BVHTree
    objs = importer_fbx(CHEVAL_FBX)
    bpy.context.view_layer.update()
    corps = next(o for o in objs if o.name.startswith("Corps"))
    bm = bmesh.new()
    bm.from_mesh(corps.data)
    bm.transform(corps.matrix_world)
    arbre = BVHTree.FromBMesh(bm)
    bm.free()
    for o in objs:
        bpy.data.objects.remove(o, do_unlink=True)
    return arbre


def sur_cheval(arbre, angle, y_unity, ecart, centre=CHEVAL_CRANE_CENTRE):
    """Point de la surface du cheval vu depuis l'axe vertical qui passe par `centre`
    (Unity), dans la direction `angle` (radians, 0 = droit devant, positif = vers la
    gauche de l'image), à la hauteur `y_unity`, décollé de `ecart` mètres vers l'extérieur.
    Renvoie (point Blender, direction horizontale vers l'extérieur)."""
    d = Vector((math.sin(angle), math.cos(angle), 0.0))
    axe = depuis_unity(centre[0], y_unity, centre[2])
    hit, _, _, _ = arbre.ray_cast(axe + d * 3.0, -d)
    if hit is None:
        hit = axe
    return hit + d * ecart, d
