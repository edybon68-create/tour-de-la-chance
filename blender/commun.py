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
    # v3/v5 : masque du cheval, framboise à galon jaune (arbitrage du DA, bible §2.8)
    "M_MasqueFramboise": ("#D63A6E", 0.0, 0.50),
    "M_MasqueGalon": ("#FFD54A", 0.0, 0.50),
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
# Les scripts d'accessoires v5 mettent ce drapeau à True (rendus avec le monde en dégradé).
MONDE_DEGRADE = False


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
    if MONDE_DEGRADE:
        # Bible §13.4 : pour juger le métal, l'ambiance est un DÉGRADÉ (ciel #8C7AB8 en haut,
        # sol #2A2030 en bas) au lieu d'une couleur unie : l'or reflète un ciel clair au-dessus
        # et un sol sombre en dessous, comme un vrai objet doré, au lieu de ressortir marron.
        coord = nt.nodes.new("ShaderNodeTexCoord")
        separe = nt.nodes.new("ShaderNodeSeparateXYZ")
        rampe = nt.nodes.new("ShaderNodeValToRGB")
        rampe.color_ramp.elements[0].position = 0.30
        rampe.color_ramp.elements[0].color = hex_vers_lineaire("#2A2030")
        rampe.color_ramp.elements[1].position = 0.50
        rampe.color_ramp.elements[1].color = hex_vers_lineaire("#8C7AB8")
        carte = nt.nodes.new("ShaderNodeMapRange")       # z de -1..1 -> 0..1
        carte.inputs["From Min"].default_value = -1.0
        nt.links.new(coord.outputs["Generated"], separe.inputs["Vector"])
        nt.links.new(separe.outputs["Z"], carte.inputs["Value"])
        nt.links.new(carte.outputs["Result"], rampe.inputs["Fac"])
        nt.links.new(rampe.outputs["Color"], eclairage.inputs["Color"])
        eclairage.inputs["Strength"].default_value = ambiance * 1.2
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


def direction_angles(angle, elevation=0.0):
    """Direction (Blender) donnée par deux angles, comme sur une carte du monde :
    `angle` = longitude (0 = droit devant, positif = vers la gauche de l'image),
    `elevation` = latitude (0 = horizontal, positif = vers le haut)."""
    ce = math.cos(elevation)
    return Vector((math.sin(angle) * ce, math.cos(angle) * ce, math.sin(elevation)))


def rayon_cheval(arbre, centre_blender, d):
    """Distance entre `centre_blender` et la surface la plus EXTÉRIEURE du cheval dans la
    direction `d` (on lance le rayon depuis loin dehors, vers le centre : on touche donc la
    crinière, une oreille ou le museau s'ils sont sur le chemin)."""
    hit, _, _, _ = arbre.ray_cast(centre_blender + d * 3.0, -d)
    return 0.0 if hit is None else (hit - centre_blender).dot(d)


def sur_cheval_lisse(arbre, centre_unity, angle, elevation, ecart, pente=0.5,
                     fenetre=(0.35, 0.21), pas=0.07):
    """Comme sur_cheval(), mais vu depuis un POINT (on peut viser vers le haut ou le bas) et
    surtout « LISSÉ » : un tissu ou un masque rigide ne rentre pas dans chaque creux, il
    passe par-dessus les bosses voisines (crinière, toupet, racine du museau).

    Comment : on mesure la surface autour de la direction voulue (une petite grille
    d'angles, `fenetre` = demi-largeurs en radians) et on garde le point le plus « haut »,
    en retirant `pente` × la distance (en mètres) jusqu'à ce voisin. Résultat : une
    enveloppe en forme de tente qui ne touche jamais l'intérieur du cheval.
    Renvoie (point Blender, direction vers l'extérieur)."""
    c = depuis_unity(*centre_unity)
    d = direction_angles(angle, elevation)
    r0 = rayon_cheval(arbre, c, d)
    meilleur = r0
    na, ne = int(round(fenetre[0] / pas)), int(round(fenetre[1] / pas))
    for i in range(-na, na + 1):
        for j in range(-ne, ne + 1):
            if i == 0 and j == 0:
                continue
            da, de = i * pas, j * pas
            r = rayon_cheval(arbre, c, direction_angles(angle + da, elevation + de))
            # distance approximative sur la surface entre les deux points visés
            dist = r0 * math.sqrt((da * math.cos(elevation)) ** 2 + de ** 2)
            meilleur = max(meilleur, r - pente * dist)
    return c + d * (meilleur + ecart), d


def bm_balayage_surface(bm, chemin, hauts, section, mat=0, ferme=False):
    """Fait glisser une `section` 2D (liste de (côté, hauteur)) le long d'un `chemin` 3D,
    avec un vecteur « haut » PAR POINT (`hauts`, en général la direction qui sort de la
    surface). Sert aux galons posés à plat sur une surface courbe (bord du masque) :
    la largeur reste couchée sur la surface, l'épaisseur pointe vers l'extérieur.
    ferme=True : boucle fermée (le dernier point rejoint le premier, pas de bouchons)."""
    pts = [Vector(p) for p in chemin]
    n = len(pts)
    anneaux = []
    for i, p in enumerate(pts):
        if ferme:
            t = pts[(i + 1) % n] - pts[i - 1]
        elif i == 0:
            t = pts[1] - pts[0]
        elif i == n - 1:
            t = pts[-1] - pts[-2]
        else:
            t = pts[i + 1] - pts[i - 1]
        t.normalize()
        cote = t.cross(Vector(hauts[i])).normalized()
        h = cote.cross(t).normalized()
        anneaux.append([bm.verts.new(p + cote * a + h * b) for a, b in section])
    m = len(section)
    faces = []
    for i in range(n if ferme else n - 1):
        A, B = anneaux[i], anneaux[(i + 1) % n]
        for s in range(m):
            u = (s + 1) % m
            f = bm.faces.new((A[s], B[s], B[u], A[u]))
            f.material_index = mat
            faces.append(f)
    if not ferme:
        for r in (anneaux[0], anneaux[-1]):
            f = bm.faces.new(r)
            f.material_index = mat
            faces.append(f)
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    return anneaux


# ---------------------------------------------------------------------------------------
# 10. Accessoires v5 « plus jeu moderne » (docs/art-bible.md §13)
# ---------------------------------------------------------------------------------------
# Pourquoi des matériaux NEUFS (préfixe M5_) au lieu de retoucher les anciens ?
#   Dans Unity, l'import d'un modèle retrouve ses matériaux PAR LEUR NOM dans
#   Assets/Models/Materials. Si on changeait seulement la brillance de « M_Satin », Unity
#   garderait l'ancien réglage (le .mat existe déjà). Un nom neuf = un .mat neuf, réglé
#   comme ici. Préréglages de la bible §13.4 (roughness Blender = 1 − smoothness Unity) :
#   mat doux 0,85 · satin 0,50 · carton brillant 0,35 · verni 0,15 · pierre 0,05 ·
#   nacre (metallic 0,2) 0,15 · doré déco (metallic 1) 0,20.
# Les noms que le CODE Unity cherche gardent leur nom d'origine : M_Aureole (émission de
# l'auréole) et M_AnneauPlanete (émission de l'anneau de la planète).
MATERIAUX.update({
    "M5_Reflet": ("#FFFFFF", 0.0, 0.10),
    "M5_DoreDeco": ("#E8B830", 1.0, 0.20),
    "M5_Nacre": ("#F4EEE6", 0.2, 0.15),
    "M5_BlancVerni": ("#F4EEE6", 0.0, 0.15),
    "M5_BlancSatin": ("#F4EEE6", 0.0, 0.50),
    "M5_BlancMat": ("#F4EEE6", 0.0, 0.85),
    "M5_JauneVerni": ("#FFD54A", 0.0, 0.15),
    # casquette
    "M5_CasquetteRouge": ("#E04040", 0.0, 0.80),
    "M5_CasquetteVisiere": ("#A02828", 0.0, 0.50),
    # chapeau de fête
    "M5_CartonRose": ("#FF6FB5", 0.0, 0.35),
    "M5_CartonJaune": ("#FFD54A", 0.0, 0.35),
    # bonnet
    "M5_LaineTurquoise": ("#2BB8C4", 0.0, 0.85),
    "M5_TurquoiseVerni": ("#2BB8C4", 0.0, 0.15),
    # lunettes de soleil
    "M5_MontureSombre": ("#2A2A3A", 0.0, 0.50),
    "M5_VerreNuit": ("#34286A", 0.0, 0.05),
    # nœud papillon, haut-de-forme
    "M5_SatinRouge": ("#C0392B", 0.0, 0.45),
    "M5_SatinFonce": ("#8E2A20", 0.0, 0.70),
    "M5_FeutreSoie": ("#1E1E1E", 0.0, 0.60),
    # lunettes cœur
    "M5_CoeurVerni": ("#FF6FB5", 0.0, 0.10),
    "M5_MontureBlanche": ("#F4EEE6", 0.0, 0.70),
    # collier de fleurs
    "M5_PetaleRose": ("#FF6FB5", 0.0, 0.70),
    "M5_PetaleBlanc": ("#F4EEE6", 0.0, 0.70),
    # sorcier
    "M5_FeutreViolet": ("#4B2E83", 0.0, 0.80),
    "M5_RubanJaune": ("#FFD54A", 0.0, 0.50),
    "M5_Rapiece": ("#6A4C9C", 0.0, 0.80),
    # masque
    "M5_GalonVerni": ("#FFD54A", 0.0, 0.20),
    # couronne
    "M5_Rubis": ("#D62839", 0.0, 0.05, 0.2),
    "M5_Velours": ("#4B2E83", 0.0, 0.90),
    # papillon
    "M5_AilePapillon": ("#2BB8C4", 0.0, 0.35),
    "M5_BordPapillon": ("#34286A", 0.0, 0.70),
    "M5_TachePapillon": ("#F28C38", 0.0, 0.35, 0.4),
    # planète
    "M5_Planete": ("#B48CFF", 0.0, 0.40),
    "M5_BandePlanete": ("#8E5BD6", 0.0, 0.40),
    "M5_LuneMat": ("#F4EEE6", 0.0, 0.85),
})


def U(x, y, z):
    """Raccourci : point donné en Unity (local à Joueur, base du cheval) -> Blender."""
    return depuis_unity(x, y, z)


def biseau_v5(obj, largeur=0.05, segments=2, angle=30.0):
    """Biseau « jouet en vinyle » de la bible §13.3 : large et rond (2 segments, profil 0,5),
    normales durcies (Harden Normals), puis normales pondérées (Weighted Normal, poids 50,
    Keep Sharp, Face Area). Les grandes faces restent bien plates (reflet net) et la
    lumière ne « tourne » que dans le biseau : un coup de lumière sur chaque bord."""
    for p in obj.data.polygons:      # les normales durcies demandent un lissage
        p.use_smooth = True
    mod = obj.modifiers.new("Biseau", "BEVEL")
    mod.width = largeur
    mod.segments = segments
    mod.profile = 0.5
    mod.limit_method = "ANGLE"
    mod.angle_limit = math.radians(angle)
    mod.use_clamp_overlap = True
    mod.harden_normals = True
    mod.miter_outer = "MITER_ARC"
    wn = obj.modifiers.new("NormalesPonderees", "WEIGHTED_NORMAL")
    wn.weight = 50
    wn.keep_sharp = True
    wn.mode = "FACE_AREA"
    return mod


def piece(nom, bm, mats, biseau=0.0, segments=2, angle=30.0, lissage="tout", pivot=None):
    """Crée une pièce d'accessoire v5 à partir d'un bmesh.
    - biseau > 0 : biseau_v5 de cette largeur (puis appliqué) ;
    - lissage : "tout" (objet mou, rond) ou "angle" (objet dur, arêtes > 35° nettes) ;
      ignoré si biseau > 0 (les normales durcies s'en chargent) ;
    - pivot : point Blender où mettre l'origine (la charnière d'une pièce animée)."""
    o = objet_depuis_bmesh(nom, bm, mats)
    if biseau > 0:
        biseau_v5(o, biseau, segments, angle)
        appliquer(o)
    elif lissage == "angle":
        lisser_angle(o, 35)
    else:
        lisser_tout(o)
    if pivot is not None:
        deplacer_origine(o, pivot)
    return o


def racine_v5(nom, point, enfants):
    """Objet vide racine `nom` posé sur le point d'accroche du cheval `point` ; les
    `enfants` (déjà construits à leur place sur le cheval) y sont rangés sans bouger,
    puis le tout revient à l'origine. Résultat : origine de l'accessoire = point
    d'accroche, donc dans Unity attachScale = 1 et attachOffset = (0 ; 0 ; 0)."""
    racine = vide(nom, depuis_unity(*CHEVAL_ACCROCHES[point]))
    for e in enfants:
        if e.parent is None:
            parenter(e, racine)
    racine.location = (0, 0, 0)
    bpy.context.view_layer.update()
    return racine


def reperes_effets(eclats=(), fx=None, parent=None):
    """Objets vides que le code cherche par leur nom (bible §13.5) : `Eclat_1..3` (où
    peut apparaître une étoile de reflet) et `Fx_Emetteur` (où naissent les particules).
    Positions en Blender. Renvoie la liste des vides créés."""
    res = []
    for i, p in enumerate(eclats):
        res.append(vide(f"Eclat_{i + 1}", Vector(p), parent))
    if fx is not None:
        res.append(vide("Fx_Emetteur", Vector(fx), parent))
    for o in res:
        o.empty_display_size = 0.05
    return res


def finir_v5(racine, nom, point, y_min=1.0):
    """Fin commune des accessoires v5 : rapport (triangles), rendu porté par le cheval
    (docs/captures/v5_cheval_<nom>.png, monde en dégradé), export FBX dans
    blender/exports/ (le dossier d'attente : on ne touche pas au projet Unity)."""
    global MONDE_DEGRADE
    MONDE_DEGRADE = True
    total = rapport(racine)
    rendre_porte_cheval(racine, f"v5_cheval_{nom}.png", point, y_min=y_min)
    chemin = exporter_fbx(racine, f"{nom}.fbx", dossier=DOSSIER_EXPORTS)
    verifier_fbx(chemin)
    print(f"RESUME {nom} triangles={total}")
    return total


# --- Briques de forme v5 ---------------------------------------------------------------
def contour_etoile(rayon, branches=5, creux=0.45, depart=math.pi / 2):
    """Contour 2D (x, z) d'une étoile : `branches` pointes, creux à `creux` × rayon."""
    pts = []
    for k in range(2 * branches):
        r = rayon if k % 2 == 0 else rayon * creux
        a = depart + math.pi * k / branches
        pts.append((r * math.cos(a), r * math.sin(a)))
    return pts


def contour_coeur(taille, n=24):
    """Contour 2D (x, z) d'un cœur de `taille` de large, centré, pointe en bas."""
    pts = []
    for k in range(n):
        t = 2 * math.pi * k / n
        x = 16 * math.sin(t) ** 3
        z = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        pts.append((x, z + 2.5))
    pts.reverse()                      # sens inverse des aiguilles d'une montre
    k = taille / 32.0
    return [(x * k, z * k) for x, z in pts]


def bm_coussin(bm, contour, e_bord, e_centre, matrice=None, mat=0, mat_dos=None,
               anneaux=2, dos_plat=False):
    """Plaque BOMBÉE comme un coussin (verres en cœur, ailes de nœud, pétales, taches) :
    épaisse de `e_bord` sur le bord et de `e_centre` au milieu. Le devant regarde +Y local.
    `contour` 2D (x, z) autour de (0, 0). dos_plat=True : seul le devant est bombé."""
    n = len(contour)
    mat_dos = mat if mat_dos is None else mat_dos

    def couche(signe, bombe):
        rangs = []
        for k in range(anneaux):
            t = k / anneaux
            s = math.cos(t * math.pi / 2)
            y = e_bord / 2 + (e_centre - e_bord) / 2 * math.sin(t * math.pi / 2) * bombe
            rangs.append([bm.verts.new((x * s, signe * y, z * s)) for x, z in contour])
        y = e_bord / 2 + (e_centre - e_bord) / 2 * bombe
        return rangs, bm.verts.new((0, signe * y, 0))

    devant, sommet_av = couche(+1, 1.0)
    dos, sommet_ar = couche(-1, 0.0 if dos_plat else 1.0)
    faces = []

    def relier(rangs, sommet, m):
        for k in range(len(rangs) - 1):
            A, B = rangs[k], rangs[k + 1]
            for i in range(n):
                j = (i + 1) % n
                f = bm.faces.new((A[i], A[j], B[j], B[i]))
                f.material_index = m
                faces.append(f)
        last = rangs[-1]
        for i in range(n):
            f = bm.faces.new((last[i], last[(i + 1) % n], sommet))
            f.material_index = m
            faces.append(f)

    relier(devant, sommet_av, mat)
    relier(dos, sommet_ar, mat_dos)
    for i in range(n):                 # la tranche
        j = (i + 1) % n
        f = bm.faces.new((devant[0][i], dos[0][i], dos[0][j], devant[0][j]))
        f.material_index = mat_dos
        faces.append(f)
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    verts = list({v for f in faces for v in f.verts})
    if matrice is not None:
        bmesh.ops.transform(bm, matrix=matrice, verts=verts)
    return verts


def bm_reflet(bm, matrice, mat, echelle=1.0):
    """Reflet peint (bible §13.4) : une barre en diagonale + un point, blancs, en relief de
    1 cm, à poser EN HAUT À GAUCHE d'une surface vernie. `matrice` = repère de la surface
    (Y local = la normale, +X local = la gauche de l'image). Coordonnées locales en mètres."""
    e = echelle
    verts = []
    barre = [(-0.035 * e, -0.009 * e), (0.035 * e, -0.009 * e),
             (0.035 * e, 0.009 * e), (-0.035 * e, 0.009 * e)]
    rot = Matrix.Rotation(math.radians(-40), 4, "Y")
    verts += bm_plaque(bm, barre, 0.012, matrice @ rot, mat=mat)
    point = contour_ellipse(0.012 * e, 0.012 * e, n=6)
    verts += bm_plaque(bm, point, 0.012,
                       matrice @ Matrix.Translation((0.045 * e, 0, 0.03 * e)), mat=mat)
    return verts


def bm_pompon(bm, centre, rayon, mat=0, bosses=0.20, graine_=3, boules=0):
    """Pompon « duveteux » : une icosphère dont les 12 sommets d'origine sont poussés vers
    l'extérieur (12 bosses), 80 triangles seulement. `bosses` = profondeur relative.
    boules=7 : à la place, 7 petites boules groupées (1 au centre, 6 autour), ~250 triangles."""
    rnd = random.Random(graine_)
    if boules:
        verts = []
        rb = rayon * 0.52
        pos = [Vector((0, 0, 0))]
        for k in range(boules - 1):
            a = 2 * math.pi * k / (boules - 1)
            pos.append(Vector((math.cos(a), math.sin(a), 0.45 * (-1) ** k)).normalized()
                       * (rayon - rb))
        for p in pos:
            verts += bm_sphere(bm, rb * rnd.uniform(0.92, 1.05), Vector(centre) + p,
                               segments=6, anneaux=4, mat=mat)
        return verts
    res = bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1.0)
    verts = res["verts"]
    for v in verts:
        bosse = len(v.link_edges) == 5             # les 12 sommets d'origine
        r = 1.0 if bosse else 1.0 - bosses
        r *= 1.0 + rnd.uniform(-0.04, 0.04)
        v.co = v.co.normalized() * r * rayon + Vector(centre)
    for v in verts:
        for f in v.link_faces:
            f.material_index = mat
    return verts


def bm_tube(bm, chemin, rayon, cotes=6, mat=0, haut=Vector((0, 0, 1)), boucher=True,
            rayons=None):
    """Tube rond (section à `cotes` côtés) le long d'un chemin 3D. `rayons` (facultatif) :
    un facteur par point, multiplié par `rayon` (pour un bout qui s'affine)."""
    if rayons is None:
        section = [(rayon * math.cos(2 * math.pi * k / cotes),
                    rayon * math.sin(2 * math.pi * k / cotes)) for k in range(cotes)]
        return bm_balayage(bm, chemin, section, haut=haut, mat=mat, boucher=boucher)
    avant = set(bm.verts)
    section = [(math.cos(2 * math.pi * k / cotes), math.sin(2 * math.pi * k / cotes))
               for k in range(cotes)]
    bm_balayage(bm, chemin, section, haut=haut, mat=mat, boucher=boucher)
    nouveaux = [v for v in bm.verts if v not in avant]
    for i, p in enumerate(chemin):           # chaque anneau est réduit à son rayon
        anneau = nouveaux[i * cotes:(i + 1) * cotes]
        for v in anneau:
            v.co = Vector(p) + (v.co - Vector(p)) * rayons[i] * rayon
    return nouveaux


def bm_disque(bm, rayon, epaisseur, matrice, cotes=8, mat=0, bombe=0.0):
    """Petit disque (bouton, œillet, pois, perle plate) : devant +Y local, bombé de
    `bombe` mètres au centre si demandé."""
    contour = contour_ellipse(rayon, rayon, n=cotes)
    if bombe > 0:
        return bm_coussin(bm, contour, epaisseur, epaisseur + 2 * bombe, matrice, mat,
                          anneaux=2, dos_plat=True)
    return bm_plaque(bm, contour, epaisseur, matrice, mat=mat)


def bm_etoile(bm, rayon, epaisseur, matrice, mat=0, bombe=0.0, branches=5, creux=0.45):
    """Étoile en relief (plaque, ou bombée si `bombe` > 0). Devant = +Y local."""
    contour = contour_etoile(rayon, branches, creux)
    if bombe > 0:
        return bm_coussin(bm, contour, epaisseur, epaisseur + 2 * bombe, matrice, mat,
                          anneaux=1)
    return bm_plaque(bm, contour, epaisseur, matrice, mat=mat)


# Base commune des chapeaux sur le cheval (mesurée par rayons sur personnage_cheval.fbx) :
# à y = 1,80 la tête tient dans un cercle de rayon 0,31 centré en z = 0,04 (le front est à
# z = −0,23, la crinière à z = +0,34). Au-dessus, les oreilles (x ≈ ±0,15) et le toupet
# montent jusqu'à y ≈ 2,02 : l'intérieur d'un chapeau doit les contenir.
CHAPEAU_BASE = (0.0, 1.80, 0.04)
CHAPEAU_RAYON = 0.31


def arbre_bm(bm):
    """« Arbre de recherche » (BVH) d'un bmesh en cours de construction : sert à poser des
    détails pile SUR une forme déjà faite (un œillet sur la calotte, une étoile sur le cône)."""
    from mathutils.bvhtree import BVHTree
    bm.normal_update()
    return BVHTree.FromBMesh(bm)


def poser_sur(arbre, centre, direction, ecart=0.0):
    """Point de la surface de `arbre` vu depuis `centre` dans `direction` (rayon lancé depuis
    l'extérieur vers le centre), décollé de `ecart`. Renvoie (point, normale de la face)."""
    d = Vector(direction).normalized()
    hit, normale, _, _ = arbre.ray_cast(Vector(centre) + d * 3.0, -d)
    if hit is None:
        return Vector(centre), d
    return hit + normale * ecart, normale


# --- V7 (point 17, qualité 3D) : matériaux neufs (préfixe M7_) ------------------------
# Même raison que pour les M5_ : un nom neuf = un .mat neuf dans Unity, réglé comme ici.
# M7_OrVif : un or CLAIR et lumineux, moins « miroir » (metallic 0,55) que M_Or. Dans un
# décor de nuit, un or 100 % métal ne reflète que du sombre et paraît bronze ; celui-ci
# garde la couleur jaune d'or des icônes.
MATERIAUX.update({
    "M7_OrVif": ("#FFC53A", 0.55, 0.28),
    "M7_OrChaine": ("#E3A52C", 0.5, 0.35),       # chaîne de l'ancre : 2e ton d'or
    "M7_PlumeBlanche": ("#FFFFFF", 0.0, 0.55),   # ailes d'ange : blanc pur, satiné
    "M7_PlumeOmbre": ("#E4DDF0", 0.0, 0.60),     # rangée du bas : lavande très clair
})
