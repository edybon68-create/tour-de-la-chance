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
    "boite_accessoires_1.png", "chapeau_bonnet.png",
    "icone_inventaire.png", "icone_index.png", "icone_boutique.png", "motif_pois.png", "motif_sucre_orge.png",
    "chapeau_capitaine.png", "bocal_poisson.png", "icone_reglages.png", "icone_effets.png", "icone_rotation.png",
    "icone_mes_skins.png"]}
# Objets en anneau ou troués : le fond visible AU MILIEU (trou de l'écharpe, du collier,
# de l'auréole, yeux du masque) n'est pas relié au bord. Pour eux, on rend aussi transparents
# les pixels très proches du fond situés à l'intérieur (seuil serré pour ne pas trouer l'objet).
OBJETS_TROUES = {"echarpe.png", "collier_fleurs.png", "aureole.png", "masque_heros.png",
                 "icone_rebirth.png"}
SEUIL_TROU = 0.035
# v5.1 : épaisseur du contour noir (en pixels, pour une image de 1024 px) et sa couleur
# (#110D1F, le même violet presque noir que les contours de l'interface).
# Mettre 0 pour retrouver les icônes sans contour.
CONTOUR_PX = 30
# Écart de teinte maximal pour qu'un pixel plus sombre que le fond soit pris pour une ombre portée.
SEUIL_OMBRE = 0.06
# Objets gris clair ou argentés : leur côté éclairé ressemble à une « ombre du fond ».
# Pour eux, on désactive la détection des ombres (sinon des morceaux de l'objet disparaissent).
SANS_OMBRE = {"icone_reglages.png", "icone_effets.png", "icone_rotation.png", "matiere_argent.png", "couleur_nacre.png"}
COULEUR_CONTOUR = np.array([0x11, 0x0D, 0x1F], dtype=np.float32) / 255.0
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
    # v5.1 : les ombres portées douces que le générateur ajoute parfois sous l'objet ont la
    # MÊME teinte que le fond, juste plus sombre. On les reconnaît à leur « couleur relative »
    # (couleur divisée par la luminosité) proche de celle du fond, et on les traite comme du fond.
    lum = px[:, :, :3].mean(axis=2) + 1e-4
    lum_fond = float(fond.mean()) + 1e-4
    teinte = px[:, :, :3] / lum[:, :, None] - fond / lum_fond
    ombre = (np.sqrt((teinte ** 2).sum(axis=2)) < SEUIL_OMBRE) & (lum < lum_fond) & (lum > 0.55 * lum_fond)
    if os.path.basename(chemin_entree) in SANS_OMBRE:
        ombre[:] = False
    candidat |= ombre

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
    alpha[atteint & ombre] = 0.0  # ombre portée reliée au fond : entièrement transparente

    # 5. v5.1 : contour noir épais, façon « autocollant » (style des jeux Roblox).
    # On « grossit » la silhouette de CONTOUR_PX pixels (filtre maximum répété), puis on
    # pose l'objet par-dessus une couche sombre de cette silhouette agrandie.
    # Le filtre travaille sur l'alpha en nombres décimaux : le bord reste doux (anti-crénelé).
    if CONTOUR_PX > 0:
        # Les ombres douces (demi-transparentes) sous certains objets ne doivent pas être
        # entourées : on ne garde que les pixels franchement opaques, avec un bord adouci.
        alpha = np.clip((alpha - 0.45) / 0.3, 0.0, 1.0)
        # Objets transparents (bocal, fantôme…) : l'intérieur peu opaque, entièrement
        # entouré par l'objet, n'est pas du fond. On le rend opaque (ses couleurs d'origine
        # contiennent déjà le reflet du fond), sinon le contour noir apparaîtrait au travers.
        # On ne le fait pas pour les objets vraiment troués (anneaux, auréole…).
        if os.path.basename(chemin_entree) not in OBJETS_TROUES:
            bas = alpha < 0.5
            dehors = np.zeros_like(bas)
            dehors[0, :] = dehors[-1, :] = True
            dehors[:, 0] = dehors[:, -1] = True
            dehors &= bas
            while True:
                suivant = dilater(dehors) & bas
                if suivant.sum() == dehors.sum():
                    break
                dehors = suivant
            alpha[bas & ~dehors] = 1.0
        grand = alpha.copy()
        for i in range(CONTOUR_PX):
            g = grand.copy()
            g[1:, :] = np.maximum(g[1:, :], grand[:-1, :])
            g[:-1, :] = np.maximum(g[:-1, :], grand[1:, :])
            g[:, 1:] = np.maximum(g[:, 1:], grand[:, :-1])
            g[:, :-1] = np.maximum(g[:, :-1], grand[:, 1:])
            if i % 2:  # une fois sur deux, aussi en diagonale : contour presque rond
                g[1:, 1:] = np.maximum(g[1:, 1:], grand[:-1, :-1])
                g[:-1, :-1] = np.maximum(g[:-1, :-1], grand[1:, 1:])
                g[1:, :-1] = np.maximum(g[1:, :-1], grand[:-1, 1:])
                g[:-1, 1:] = np.maximum(g[:-1, 1:], grand[1:, :-1])
            grand = g
        a = alpha[:, :, None]
        px[:, :, :3] = px[:, :, :3] * a + COULEUR_CONTOUR * (1.0 - a)
        alpha = grand
    px[:, :, 3] = alpha

    sortie = bpy.data.images.new(os.path.basename(chemin_sortie), w, h, alpha=True)
    sortie.pixels[:] = px.ravel()
    sortie.filepath_raw = chemin_sortie
    sortie.file_format = "PNG"
    sortie.save()
    transparent = float((alpha < 0.5).mean()) * 100
    print(f"OK {os.path.basename(chemin_sortie)} : fond {fond.round(3)}, {transparent:.0f} % transparent")


os.makedirs(SORTIE, exist_ok=True)
SEULES = set((n + ".png") for n in os.environ.get("ICONES", "").split())
for nom in sorted(os.listdir(SOURCE)):
    if nom.endswith(".png") and (not SEULES or nom in SEULES):
        detourer(os.path.join(SOURCE, nom), os.path.join(SORTIE, nom))
