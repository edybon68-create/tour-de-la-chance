"""Détourage des icônes du shop : rend transparent le fond lavande uni (#E6E1EE).

Lancer avec :
  /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/detourer_icones.py

On utilise le Python de Blender parce qu'il contient déjà numpy et sait lire/écrire des PNG.

Méthode (pour ne pas trouer l'objet) :
1. on mesure la couleur du fond dans les 4 coins de l'image ;
2. on marque comme « fond possible » les pixels proches de cette couleur ;
3. on ne garde comme fond QUE ceux reliés au bord de l'image (remplissage depuis les bords) :
   un reflet blanc au milieu d'un œil n'est pas relié au bord, il reste donc opaque ;
4. on adoucit le contour (transparence progressive) pour éviter un liseré dur.
Les fichiers d'origine sont conservés ; les versions détourées vont dans art/icones_detourees/.
"""
import os
import bpy
import numpy as np

RACINE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SOURCE = os.path.join(RACINE, "art", "icones")
SORTIE = os.path.join(RACINE, "art", "icones_detourees")
SEUIL_FOND = 0.16   # distance de couleur (0..1) en dessous de laquelle un pixel ressemble au fond
SEUIL_OPAQUE = 0.26 # au-delà, le pixel est complètement opaque (entre les deux : demi-transparent)
# Réglages plus serrés pour les icônes dont l'objet ressemble au fond
# (le pompon blanc du chapeau de fête a des ombres lavande).
SEUILS_SPECIAUX = {n: (0.035, 0.07) for n in [
    "chapeau_fete.png", "echarpe.png", "collier_fleurs.png", "lunettes_coeur.png",
    "matiere_arc_en_ciel.png", "matiere_cristal.png", "matiere_argent.png", "boite_skins_1.png",
    "boite_accessoires_1.png", "chapeau_bonnet.png"]}
# Objets en anneau ou troués : le fond visible AU MILIEU (trou de l'écharpe, du collier,
# de l'auréole, yeux du masque) n'est pas relié au bord. Pour eux, on rend aussi transparents
# les pixels très proches du fond situés à l'intérieur (seuil serré pour ne pas trouer l'objet).
OBJETS_TROUES = {"echarpe.png", "collier_fleurs.png", "aureole.png", "masque_heros.png"}
SEUIL_TROU = 0.035
# Images qui ne sont pas des icônes du shop : on les laisse de côté.
IGNORER = set()


def dilater(masque):
    """Agrandit un masque d'un pixel dans les 4 directions."""
    m = masque.copy()
    m[1:, :] |= masque[:-1, :]
    m[:-1, :] |= masque[1:, :]
    m[:, 1:] |= masque[:, :-1]
    m[:, :-1] |= masque[:, 1:]
    return m


def detourer(chemin_entree, chemin_sortie):
    img = bpy.data.images.load(chemin_entree)
    w, h = img.size
    px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)

    # 1. Couleur du fond : moyenne de petits carrés dans les 4 coins.
    c = 24
    coins = np.concatenate([
        px[:c, :c, :3].reshape(-1, 3), px[:c, -c:, :3].reshape(-1, 3),
        px[-c:, :c, :3].reshape(-1, 3), px[-c:, -c:, :3].reshape(-1, 3)])
    fond = np.median(coins, axis=0)

    # 2. Distance de chaque pixel à la couleur du fond.
    dist = np.sqrt(((px[:, :, :3] - fond) ** 2).sum(axis=2))
    seuil_fond, seuil_opaque = SEUILS_SPECIAUX.get(os.path.basename(chemin_entree), (SEUIL_FOND, SEUIL_OPAQUE))
    candidat = dist < seuil_opaque

    # 3. Remplissage depuis les bords, limité aux pixels candidats.
    atteint = np.zeros_like(candidat)
    atteint[0, :] = atteint[-1, :] = True
    atteint[:, 0] = atteint[:, -1] = True
    atteint &= candidat
    while True:
        suivant = dilater(atteint) & candidat
        if suivant.sum() == atteint.sum():
            break
        atteint = suivant

    # 3 bis. Trous intérieurs pour les objets en anneau.
    if os.path.basename(chemin_entree) in OBJETS_TROUES:
        atteint |= dist < SEUIL_TROU

    # 4. Transparence progressive sur la zone de fond atteinte.
    alpha = np.ones((h, w), dtype=np.float32)
    rampe = np.clip((dist - seuil_fond) / (seuil_opaque - seuil_fond), 0.0, 1.0)
    alpha[atteint] = rampe[atteint]
    px[:, :, 3] = alpha

    sortie = bpy.data.images.new(os.path.basename(chemin_sortie), w, h, alpha=True)
    sortie.pixels[:] = px.ravel()
    sortie.filepath_raw = chemin_sortie
    sortie.file_format = "PNG"
    sortie.save()
    transparent = float((alpha < 0.5).mean()) * 100
    print(f"OK {os.path.basename(chemin_sortie)} : fond {fond.round(3)}, {transparent:.0f} % transparent")


os.makedirs(SORTIE, exist_ok=True)
for nom in sorted(os.listdir(SOURCE)):
    if nom.endswith(".png"):
        detourer(os.path.join(SOURCE, nom), os.path.join(SORTIE, nom))
