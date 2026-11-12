# -*- coding: utf-8 -*-
"""
Nouveaux sons de la v5 (spec docs/specs/v5.md §17.4, chronologie de l'ouverture 3D :
docs/art-bible.md §14.4).

Ils se rangent en quatre familles :
- jeu/   : diamants, reprise, renaissance, et les petits sons des objets animés
           (perroquet, chaton, baleine...) joués au saut de joie ;
- ui/    : offre de reprise, carte d'étage, boutons ronds, info-bulle, onglets, refus ;
- boite/ : les couches de l'ouverture 3D (atterrissage, petits sauts, craquement, nœud,
           couvercle, faisceau) et les touches thématiques des Secrets Pirates / Animaux.

Comme les autres sons du jeu, tout est en fa majeur (la tonalité de la musique). Les sons
d'animaux et de pirates restent TOUJOURS mignons : voix aiguës, glissandos ronds, aucun
grave menaçant, aucune dissonance (le grondement du Kraken est doux et finit par un « blop »).

Équité (règle d'or n°5) : les couches de l'ouverture 3D jouées AVANT la révélation
(atterrit, saut, craque, noeud, couvercle, faisceau) sont les mêmes pour toutes les
raretés. Le faisceau est volontairement « neutre » (quinte à vide fa-do, ni majeur ni
mineur) : il ne laisse rien deviner.

Volumes « à l'oreille » visés (synth.volume_pondere, colonne « oreille » de l'analyse) :
- interface discrète : -16 à -20 ;
- couches de l'ouverture : -16 à -19 (elles se superposent à boite_secoue/boite_ouverture) ;
- diamant gagné : -13,5 à -12,5 (un peu au-dessus du trésor en pièces, c'est rare) ;
- renaissance : -11,5 (le plus grand moment du jeu, comme resultat_encaisse) ;
- objets animés : -15 à -17, le code les joue en plus à 0,25 – 0,4 de volume.

Lancer : <python de Blender> audio/sons_v5.py   (ou generer_tout.py --v5)
"""
import numpy as np

from synth import (adsr, ajoute, boite_a_musique, bruit, clochette, cuivre, echo,
                   ecris_wav, filtre, finalise, glisse, glockenspiel, harpe, hz, n_ech,
                   passe_bas_variable, perc, reverb, scie, scintillement, silence, sinus,
                   temps, triangle, vibrato)


# =========================== petites briques communes ===========================

def _cristal(f, duree=0.5, chute=0.4):
    """
    Un diamant qui tinte : partiels x2,0 / x3,0 / x5,04, plus « purs » qu'une pièce
    (ting, x2,7) et plus brillants que le verre des coffres (x2 ; x4,16). On reconnaît
    ainsi le diamant-monnaie à l'oreille, sans le confondre avec une pièce.
    """
    x = sinus(f, duree) * perc(duree, 0.0008, chute)
    for r, g, c in [(2.0, 0.35, 0.45), (3.0, 0.18, 0.25), (5.04, 0.1, 0.12)]:
        if r * f < 19000:
            x += g * sinus(r * f, duree) * perc(duree, 0.0005, chute * c)
    return x


def _choc_bois(rng, f_corps=110, d=0.2, clarte=600, poids_bruit=1.2):
    """
    Un objet en bois qui touche un socle : sinus grave qui chute (le corps) + trois
    résonances de bois (modes à ~2, 3,7 et 6,3 fois le corps) très brèves + bruit étouffé.
    """
    t_corps = sinus(glisse(f_corps * 1.4, f_corps, d), d) * perc(d, 0.001, d * 0.6)
    modes = np.zeros(n_ech(d))
    for r, g, c in [(2.0, 0.35, 0.10), (3.7, 0.22, 0.06), (6.3, 0.12, 0.035)]:
        modes += g * sinus(r * f_corps, d) * perc(d, 0.0008, c)
    choc = filtre(bruit(d, rng), "passe_bas", clarte, ordre=2) * perc(d, 0.0005, 0.035)
    return t_corps + modes + poids_bruit * choc


def _souffle(rng, d, f0, f1, q=1.4, forme=1.5, passe_haut=None):
    """Un « whoosh » : bruit dont le filtre glisse de f0 à f1, enveloppe en cloche."""
    b = bruit(d, rng)
    if passe_haut:
        b = filtre(b, "passe_haut", passe_haut)
    s = passe_bas_variable(b, glisse(f0, f1, d), q=q)
    t = temps(d)
    return s * np.sin(np.pi * np.clip(t / d, 0, 1)) ** forme


def _voix(f, duree, fc, q=2.5, douceur=3000, rugosite=0.0, rng=None):
    """
    Une petite « voix » d'animal de dessin animé : dents de scie (riche) qui passent dans
    un passe-bas résonant dont la coupure bouge (fc = tableau) : c'est ce qui fait la
    voyelle (« i » quand le filtre est haut, « ou » quand il est bas). `rugosite` ajoute
    un grain (modulation rapide du volume) pour un rugissement… de lionceau.
    """
    x = scie(f, duree, douceur=douceur)
    if rugosite and rng is not None:
        t = temps(duree)
        grain = 1.0 - rugosite * (0.5 + 0.5 * np.sin(2 * np.pi * 38 * t + rng.uniform(0, 6)))
        x = x * grain
    return passe_bas_variable(x, fc, q=q)


def _cloche_douce(t, d):
    """Enveloppe en cloche (0 -> 1 -> 0) sur la durée d, pour les souffles et nappes."""
    return np.sin(np.pi * np.clip(t / d, 0, 1)) ** 2


# =========================== jeu : diamants ===========================

def diamant_gain(rng, palier):
    """
    Un trésor en diamants (§10.5) : arpège montant de cristal, puis un éclat et une pluie
    d'étincelles. Trois paliers (le code choisit selon l'étage, comme le nombre de
    diamants) : palier 1 = 3 notes do6-fa6-la6 ; palier 2 = 4 notes jusqu'au do7 ;
    palier 3 = 5 notes jusqu'au fa7 et plus d'étincelles. Plus il y a de diamants, plus
    l'arpège est long et monte haut : on l'entend sans regarder l'écran.
    Dessous, un « shhh » très aigu qui s'ouvre (le diamant qui brille).
    """
    d = 0.9
    x = silence(d)
    notes = ["C6", "F6", "A6", "C7", "F7"][:2 + palier]
    pas = 0.06
    for i, note in enumerate(notes):
        ajoute(x, _cristal(hz(note), 0.55, 0.45), pas * i, 0.55 + 0.08 * i)
    t_eclat = pas * (len(notes) - 1)
    # éclat : la dernière note doublée à l'octave inférieure (corps) + petite quinte
    ajoute(x, _cristal(hz(notes[-1]) / 2, 0.6, 0.55), t_eclat, 0.35)
    ajoute(x, _cristal(hz(notes[-1]) * 1.5, 0.4, 0.3), t_eclat + 0.02, 0.18)
    ds = 0.5
    brille = passe_bas_variable(filtre(bruit(ds, rng), "passe_haut", 5000), glisse(6000, 12000, ds), q=1.2)
    ajoute(x, brille * _cloche_douce(temps(ds), ds), 0.05, 0.05)
    x += scintillement(d, rng, 8 + 6 * palier, 5500, 9500, debut=t_eclat, fin=0.75,
                       chute=0.03, volume=0.22)
    x = reverb(x, 0.16, 0.7, brillance=8000)
    cible = {1: -13.5, 2: -13.0, 3: -12.5}[palier]
    return finalise(x, d, 0.001, 0.01, cible_oreille=cible)


def diamant_compteur(rng):
    """
    Le compteur de diamants qui change (§10.4) : un seul petit « tink » de cristal, très
    court (le pendant de pieces_tic pour les pièces). À jouer une fois par diamant ajouté
    (au plus 6), en suivant le défilement de 0,4 s.
    """
    d = 0.12
    x = _cristal(hz("A6") * 2, d, 0.09)  # la7, 3 520 Hz
    x += 0.4 * _cristal(hz("E7"), d, 0.06)
    return finalise(x, d, 0.0008, 0.005, cible_oreille=-17.0)


# =========================== reprise ===========================

def reprise_offre(rng):
    """
    La fenêtre « Oh non, un piège ! … Continuer — 1 ◆ ? » (§11.2) : deux notes douces de
    boîte à musique qui MONTENT et restent en suspens (do6 puis ré6, la seconde avec une
    petite remontée de hauteur à la fin, comme l'intonation d'une question : « et si… ? »).
    Rien d'alarmant : notes douces, pas de grave, pas d'urgence.
    """
    d = 0.5
    x = silence(d)
    ajoute(x, boite_a_musique(hz("C6"), 0.3, 0.25), 0.0, 0.8)
    d2 = 0.36
    # ré6, qui remonte de 0,6 demi-ton sur la fin (courbe ^4 : seulement tout au bout)
    f2 =hz("D6") * 2.0 ** ((np.linspace(0, 1, n_ech(d2)) ** 4) * 0.6 / 12)
    note2 = (sinus(f2, d2) + 0.25 * sinus(4 * f2, d2) * perc(d2, 0.001, 0.08)) * perc(d2, 0.002, 0.33)
    ajoute(x, note2, 0.13, 0.85)
    ajoute(x, triangle(hz("F4"), 0.45) * adsr(0.45, 0.03, 0.1, 0.5, 0.2), 0.0, 0.12)
    x = reverb(x, 0.14, 0.5)
    return finalise(x, d, 0.002, 0.02, cible_oreille=-17.0)


def reprise_ok(rng):
    """
    « Continuer — 1 ◆ » accepté (§11.2, spirale bleutée 0,4 s puis coffres qui font
    « pouf ») : un « rembobinage » magique, comme une cassette qu'on remonte : un sifflet
    qui DESCEND vite en tremblotant (0,25 s), puis REMONTE (0,2 s), avec un souffle qui fait
    le même aller-retour ; puis un petit carillon fa6-la6-do7 : « c'est reparti ! ».
    """
    d = 0.8
    x = silence(d)
    d1, d2 = 0.25, 0.2
    f = np.concatenate([glisse(1400, 350, d1), glisse(350, 1100, d2)])
    dd = len(f) / 44100.0
    f = vibrato(f, dd, 22.0, 0.6)  # le tremblement rapide du « rembobinage »
    siffle = sinus(f, dd) + 0.3 * sinus(2 * f, dd)
    env = adsr(dd, 0.02, 0.05, 0.8, 0.08)
    ajoute(x, siffle * env, 0.0, 0.35)
    s1 = passe_bas_variable(bruit(dd, rng), np.concatenate([glisse(5000, 700, d1), glisse(700, 4000, d2)]), q=1.5)
    ajoute(x, s1 * env, 0.0, 0.3)
    for i, note in enumerate(["F6", "A6", "C7"]):
        ajoute(x, clochette(hz(note), 0.35, 0.3, harmonique=3, poids=0.3), 0.46 + 0.06 * i, 0.5)
    x += scintillement(d, rng, 6, 5000, 8000, debut=0.5, fin=0.7, chute=0.025, volume=0.2)
    x = reverb(x, 0.14, 0.5)
    return finalise(x, d, 0.003, 0.01, cible_oreille=-14.5)


# =========================== renaissance ===========================

def renaissance(rng):
    """
    L'animation de renaissance (§13.4), 4,0 s, calée sur les images :
    - 0 -> 1,5 s : montée magique. Trois glissandos de harpe en fa majeur, de plus en plus
      hauts et serrés (les 3 anneaux dorés qui montent), une nappe de « cordes »
      (scies désaccordées) dont le filtre s'ouvre, et des carillons qui accélèrent ;
    - 1,5 s : SOMMET au flash blanc : grand accord de fa majeur (cordes + cuivres doux +
      glockenspiel), petite cymbale qui fleurit, aucune explosion ;
    - 2,1 s : le cheval atterrit : « poum » feutré grave + pluie de 40 étincelles (plumes) ;
    - 2,1 -> 4,0 s : retombée douce, l'accord s'éteint lentement sous une boîte à musique
      qui redescend (fa6-do6-la5-fa5). Le tampon « ×2 » (2,4 s) a son propre son.
    """
    d = 4.0
    x = silence(d)
    t = temps(d)
    # --- 1) harpes montantes : 3 glissandos (0,0 / 0,5 / 0,95 s) ---
    gamme = ["F3", "A3", "C4", "F4", "A4", "C5", "F5", "A5", "C6", "F6", "A6", "C7"]
    for k, (t0, debut, longueur, duree_gl) in enumerate([(0.0, 0, 7, 0.4), (0.5, 2, 8, 0.38),
                                                         (0.95, 4, 8, 0.45)]):
        notes = gamme[debut:debut + longueur]
        for i, note in enumerate(notes):
            ajoute(x, harpe(hz(note), 0.7, 0.6), t0 + duree_gl * i / (len(notes) - 1),
                   (0.25 + 0.1 * k) * (0.7 + 0.3 * i / len(notes)))
    # --- 2) nappe de cordes : do (accord de dominante) qui monte vers fa au flash ---
    nappe = np.zeros(n_ech(d))
    for notes, t0, t1 in [(["C3", "G3", "C4", "E4"], 0.0, 1.55), (["F2", "F3", "A3", "C4", "F4", "A4"], 1.5, 4.0)]:
        dn = t1 - t0
        son = np.zeros(n_ech(dn))
        for note in notes:
            for desacc in (-0.06, 0.06):
                son += scie(hz(note) * 2 ** (desacc / 12), dn, douceur=2500)
        if t0 == 0.0:
            env = (np.linspace(0, 1, n_ech(dn)) ** 1.6) * adsr(dn, 0.01, 0.01, 1.0, 0.06)
        else:
            env = adsr(dn, 0.03, 0.4, 0.55, 2.0)
        ajoute(nappe, son * env / len(notes), t0)
    coupure = np.where(t < 1.5, 300 + 3200 * (t / 1.5) ** 2,
                       3500 * np.exp(-(t - 1.5) / 1.4) + 600)
    x += 0.55 * passe_bas_variable(nappe, coupure, q=0.9)
    # --- 3) carillons qui accélèrent vers le flash ---
    tc, ecart, i = 0.15, 0.22, 0
    notes_c = ["C6", "F6", "A6", "C7"]
    while tc < 1.45:
        ajoute(x, clochette(hz(notes_c[i % 4]), 0.3, 0.25, 3, 0.3), tc, 0.12 + 0.12 * tc)
        tc += ecart
        ecart = max(0.06, ecart * 0.82)
        i += 1
    # souffle qui monte jusqu'au flash
    ds = 1.5
    souffle = passe_bas_variable(filtre(bruit(ds, rng), "passe_haut", 400), glisse(500, 8000, ds), q=1.3)
    ajoute(x, souffle * np.linspace(0, 1, n_ech(ds)) ** 2.5, 0.0, 0.12)
    # --- 4) le sommet : accord au flash (1,5 s) ---
    t_flash = 1.5
    for note, g in [("F3", 0.3), ("C4", 0.22), ("F4", 0.22), ("A4", 0.22), ("C5", 0.2)]:
        ajoute(x, cuivre(hz(note), 1.3, a=0.05, r=0.9, vib=0.1), t_flash, g)
    for i, note in enumerate(["F6", "A6", "C7", "F7"]):
        ajoute(x, glockenspiel(hz(note), 0.9, 0.8), t_flash + 0.03 * i, 0.28)
    dc = 1.2
    cymbale = filtre(bruit(dc, rng), "passe_haut", 6000, ordre=2) * adsr(dc, 0.04, 0.2, 0.3, 0.9)
    ajoute(x, cymbale, t_flash - 0.02, 0.12)
    # --- 5) l'atterrissage (2,1 s) : poum feutré + plumes et étincelles ---
    t_sol = 2.1
    dp = 0.4
    poum = sinus(glisse(110, 55, dp), dp) * perc(dp, 0.003, 0.3)
    poum += 0.6 * filtre(bruit(dp, rng), "passe_bas", 500, ordre=2) * perc(dp, 0.001, 0.08)
    ajoute(x, poum, t_sol, 0.6)
    x += scintillement(d, rng, 40, 4500, 8500, debut=t_sol, fin=3.4, chute=0.04, volume=0.2)
    # --- 6) retombée : boîte à musique qui redescend doucement ---
    for i, note in enumerate(["F6", "C6", "A5", "F5"]):
        ajoute(x, boite_a_musique(hz(note), 0.9, 0.8), 2.25 + 0.28 * i, 0.32 - 0.03 * i)
    x = reverb(x, 0.2, 1.4)
    return finalise(x, d, 0.004, 0.3, cible_oreille=-11.5)


def renaissance_tampon(rng):
    """
    Le tampon « ×2 » qui s'écrase sur le compteur de pièces (§13.4, à 2,4 s) : un « boum »
    feutré (comme un tampon encreur, bois + caoutchouc), un petit « tchac » de papier, puis
    une étincelle qui brille (clochette fa6 + do7). Court et satisfaisant.
    """
    d = 0.4
    x = silence(d)
    dp = 0.2
    boum = sinus(glisse(150, 70, dp), dp) * perc(dp, 0.002, 0.14)
    feutre = filtre(bruit(dp, rng), "passe_bas", 900, ordre=2) * perc(dp, 0.0008, 0.03)
    papier = filtre(bruit(0.05, rng), "passe_bande", 3000, 1.2) * perc(0.05, 0.0005, 0.02)
    ajoute(x, boum + 1.1 * feutre, 0.0)
    ajoute(x, papier, 0.004, 0.35)
    ajoute(x, clochette(hz("F6"), 0.3, 0.25, 4, 0.3), 0.06, 0.3)
    ajoute(x, clochette(hz("C7"), 0.25, 0.2, 3, 0.25), 0.1, 0.22)
    x += scintillement(d, rng, 4, 6000, 8500, debut=0.08, fin=0.2, chute=0.025, volume=0.2)
    x = reverb(x, 0.08, 0.35)
    return finalise(x, d, 0.001, 0.01, cible_oreille=-13.0)


# =========================== ui ===========================

def recap_etage(rng):
    """
    La carte d'étage qui glisse du haut et grandit (§9.3) : un « swoosh » de papier léger
    (bruit dont le filtre monte puis redescend) avec quelques froissements (micro-clics
    filtrés) et une toute petite clochette quand la carte se pose (0,22 s).
    """
    d = 0.3
    x = silence(d)
    ds = 0.24
    sw = passe_bas_variable(filtre(bruit(ds, rng), "passe_haut", 800),
                            np.concatenate([glisse(1500, 5000, ds * 0.6), glisse(5000, 2500, ds * 0.4)])[:n_ech(ds)], q=1.0)
    ajoute(x, sw * np.sin(np.pi * np.linspace(0, 1, n_ech(ds))) ** 1.2, 0.0, 0.5)
    for _ in range(7):
        t0 = rng.uniform(0.03, 0.2)
        c = filtre(bruit(0.012, rng), "passe_bande", rng.uniform(2500, 5000), 3.0) * perc(0.012, 0.0005, 0.006)
        ajoute(x, c, t0, rng.uniform(0.3, 0.7))
    ajoute(x, clochette(hz("C7"), 0.08, 0.07, 3, 0.2), 0.21, 0.12)
    return finalise(x, d, 0.003, 0.01, cible_oreille=-19.0)


def ui_bouton_rond(rng):
    """
    Clic sur un bouton rond d'icône (§7.1 : Boutique, Inventaire, Index, Améliorations,
    Classement, Renaissance) : un « bop » rond comme une bulle qu'on presse (sinus qui
    MONTE 500 -> 850 Hz, plus doux que le « toc » de bois de ui_clic) + une harmonique
    de clochette très brève pour l'éclat.
    """
    d = 0.1
    x = sinus(glisse(500, 850, 0.06).tolist() + [850] * (n_ech(d) - n_ech(0.06)), d) * perc(d, 0.002, 0.085)
    x += 0.2 * sinus(hz("F6"), d) * perc(d, 0.001, 0.04)
    x += 0.4 * filtre(bruit(d, rng), "passe_bas", 1500, ordre=2) * perc(d, 0.0005, 0.008)
    return finalise(x, d, 0.002, 0.005, cible_oreille=-16.0)


def ui_infobulle(rng):
    """
    L'info-bulle qui apparaît au survol (après 0,4 s, §7.1) : un « blip » minuscule et
    doux (sinus 1 300 -> 1 750 Hz, 45 ms). Le plus discret des sons de l'interface : il
    peut revenir souvent sans fatiguer.
    """
    d = 0.08
    x = sinus(np.concatenate([glisse(1300, 1750, 0.03), np.full(n_ech(d) - n_ech(0.03), 1750.0)]), d)
    x = x * perc(d, 0.004, 0.06)
    return finalise(x, d, 0.004, 0.005, cible_oreille=-21.0)


def ui_onglet(rng):
    """
    Changement d'onglet de la Boutique (§15.2 : Boîtes, Pirates, Diamants) : un double
    « tic-tic » de pastille qui glisse (do6 puis fa6, 45 ms d'écart, timbre de boîte à
    musique très court) + un mini souffle entre les deux.
    """
    d = 0.15
    x = silence(d)
    ajoute(x, boite_a_musique(hz("C6"), 0.08, 0.06), 0.0, 0.6)
    ajoute(x, boite_a_musique(hz("F6"), 0.1, 0.08), 0.045, 0.75)
    ajoute(x, _souffle(rng, 0.06, 2000, 6000, passe_haut=1500), 0.005, 0.25)
    return finalise(x, d, 0.002, 0.005, cible_oreille=-17.5)


def ui_refus(rng):
    """
    Bouton grisé (pas assez de pièces, « Bientôt disponible »…) : un « bonk-bonk » de
    dessin animé, deux notes sourdes qui descendent (la4 puis fa4, triangle étouffé).
    Gentil, jamais une alarme : il dit seulement « pas maintenant ».
    """
    d = 0.28
    x = silence(d)
    for t0, note in [(0.0, "A4"), (0.11, "F4")]:
        dn = 0.14
        f = glisse(hz(note) * 1.06, hz(note), dn)
        n = triangle(f, dn) * perc(dn, 0.003, 0.12)
        ajoute(x, filtre(n, "passe_bas", 1500), t0, 0.8)
        ajoute(x, filtre(bruit(0.02, rng), "passe_bas", 900) * perc(0.02, 0.0005, 0.01), t0, 0.3)
    return finalise(x, d, 0.002, 0.01, cible_oreille=-18.0)


# =========================== boîtes 3D : couches de l'ouverture ===========================
# Temps = secondes depuis le clic sur « Ouvrir » (art bible §14.4).

def boite3d_atterrit(rng):
    """
    À 0,24 s (la boîte touche le socle après sa chute) : un « bom » de bois sourd, puis
    deux petits rebonds de plus en plus faibles et rapprochés (0,11 s et 0,18 s), comme
    l'écrasement-détente de l'animation. Le premier choc est au tout début du fichier.
    """
    d = 0.35
    x = silence(d)
    ajoute(x, _choc_bois(rng, 105, 0.25, 700), 0.0, 1.0)
    ajoute(x, _choc_bois(rng, 125, 0.15, 900, 0.8), 0.11, 0.35)
    ajoute(x, _choc_bois(rng, 135, 0.1, 1100, 0.6), 0.18, 0.15)
    x = reverb(x, 0.06, 0.3)
    return finalise(x, d, 0.0008, 0.01, cible_oreille=-16.0)


def boite3d_saut(rng):
    """
    Un des 3 petits sauts de la secousse (à 0,55 / 0,95 / 1,35 s) : la boîte décolle à
    0,00, retombe à 0,10 (petit « tom » de bois), et le couvercle, soulevé de 3 cm, retombe
    juste après (0,15 : « clac » plus aigu). Plus discret que boite_secoue, qu'il complète.
    """
    d = 0.3
    x = silence(d)
    ajoute(x, _choc_bois(rng, 140, 0.15, 900, 0.9), 0.10, 0.6)
    clac = filtre(bruit(0.04, rng), "passe_bande", 1600, 3.0) * perc(0.04, 0.0005, 0.015)
    clac += 0.4 * sinus(glisse(700, 520, 0.04), 0.04) * perc(0.04, 0.001, 0.025)
    ajoute(x, clac, 0.15, 0.7)
    x = reverb(x, 0.05, 0.25)
    return finalise(x, d, 0.002, 0.01, cible_oreille=-19.0)


def boite3d_craque(rng):
    """
    À 1,20 s : la boîte « n'en peut plus ». Un craquement de bois qui travaille (une rafale
    de micro-clics dans un filtre résonant, de plus en plus serrés, comme une planche qui
    grince) et, dessous, la lumière blanche qui grésille doucement par la fente (deux
    sinus aigus très proches qui battent + petit souffle). Aucune note de rareté.
    """
    d = 0.4
    x = silence(d)
    tc, i = 0.0, 0
    while tc < 0.3:
        f = 600 + 900 * tc / 0.3 + rng.uniform(-80, 80)
        c = filtre(bruit(0.02, rng), "passe_bande", f, 6.0) * perc(0.02, 0.0004, 0.012)
        ajoute(x, c, tc, 0.5 + 1.2 * tc)
        tc += max(0.012, 0.045 * (1 - tc / 0.35)) * rng.uniform(0.7, 1.3)
        i += 1
    dl = 0.36
    lumiere = sinus(4200, dl) + sinus(4237, dl) + 0.5 * sinus(6300, dl)
    trem = 0.6 + 0.4 * np.sin(2 * np.pi * 31 * temps(dl))
    env = np.linspace(0, 1, n_ech(dl)) ** 2 * adsr(dl, 0.01, 0.01, 1.0, 0.08)
    ajoute(x, lumiere * trem * env, 0.02, 0.05)
    ajoute(x, _souffle(rng, dl, 2000, 7000, passe_haut=1500, forme=1.0), 0.02, 0.15)
    x = reverb(x, 0.06, 0.3)
    return finalise(x, d, 0.001, 0.01, cible_oreille=-17.0)


def boite3d_noeud(rng):
    """
    À 1,58 s : le nœud s'envole en tournoyant (900°/s). Un « pwing » élastique (le ruban
    qui se détend : sinus 300 -> 900 Hz avec un vibrato rapide) + un froufrou de tissu
    qui bat (bruit filtré, haché à 15 Hz = les boucles qui tournent) et qui s'éloigne
    (filtre qui se ferme, volume qui baisse) jusqu'à 0,5 s.
    """
    d = 0.5
    x = silence(d)
    dp = 0.22
    f = vibrato(glisse(300, 900, dp), dp, 18.0, 0.8)
    pwing = (sinus(f, dp) + 0.3 * sinus(2 * f, dp)) * perc(dp, 0.003, 0.2)
    ajoute(x, pwing, 0.0, 0.4)
    df = 0.48
    tf = temps(df)
    froufrou = passe_bas_variable(filtre(bruit(df, rng), "passe_haut", 600), glisse(4500, 1200, df), q=1.2)
    hache = 0.35 + 0.65 * (0.5 + 0.5 * np.sin(2 * np.pi * 15 * tf)) ** 2
    ajoute(x, froufrou * hache * np.exp(-tf / 0.2) * np.clip(tf / 0.02, 0, 1), 0.01, 0.6)
    return finalise(x, d, 0.002, 0.01, cible_oreille=-18.0)


def boite3d_couvercle(rng):
    """
    À 1,62 s : le couvercle saute et part en arrière en basculant (-360°/s), hors du cadre
    à 2,30 s. Le « pop » est déjà dans boite_ouverture (1,50 s) : ici, seulement le
    « vroum » d'un objet plat qui tournoie et s'éloigne : souffle grave-médium haché au
    rythme de la bascule (2 battements par tour, qui accélèrent un peu), de plus en plus
    sourd et faible, + un petit « fwoump » de départ.
    """
    d = 0.6
    x = silence(d)
    df = 0.08
    fwoump = filtre(bruit(df, rng), "passe_bas", 600, ordre=2) * perc(df, 0.002, 0.06)
    fwoump += 0.5 * sinus(glisse(200, 120, df), df) * perc(df, 0.002, 0.06)
    ajoute(x, fwoump, 0.0, 0.8)
    dt = 0.58
    tt = temps(dt)
    tourne = passe_bas_variable(filtre(bruit(dt, rng), "passe_haut", 250), glisse(2500, 500, dt), q=1.5)
    rythme = np.cumsum(np.full(n_ech(dt), 1.0) * (7.0 + 4.0 * tt / dt)) / 44100.0
    hache = (0.5 + 0.5 * np.cos(2 * np.pi * rythme)) ** 3
    ajoute(x, tourne * hache * np.exp(-tt / 0.22) * np.clip(tt / 0.03, 0, 1), 0.02, 0.8)
    return finalise(x, d, 0.002, 0.02, cible_oreille=-18.0)


def boite3d_faisceau(rng):
    """
    À 1,62 s : le faisceau blanc jaillit (0 -> 3,5 m en 0,13 s). Un « shwiiing » de
    lumière : souffle aigu qui monte très vite (0,13 s, comme le cône qui grandit) puis
    une quinte à vide fa5-do6-fa6 qui scintille (trémolo rapide) et s'éteint en 0,35 s.
    Quinte « à vide » (sans tierce) = ni joyeuse ni triste : un blanc neutre, identique
    pour toutes les raretés. Elle s'éteint au moment de la révélation (1,75 s + 0,1).
    """
    d = 0.5
    x = silence(d)
    ds = 0.16
    shw = passe_bas_variable(filtre(bruit(ds, rng), "passe_haut", 1500), glisse(2000, 11000, ds), q=1.6)
    ajoute(x, shw * np.linspace(0, 1, n_ech(ds)) ** 1.5 * adsr(ds, 0.001, 0.001, 1.0, 0.03), 0.0, 0.6)
    dq = 0.42
    tq = temps(dq)
    quinte = sum(sinus(hz(n) * 2 ** (rng.uniform(-0.04, 0.04) / 12), dq) for n in ["F5", "C6", "F6"])
    trem = 0.75 + 0.25 * np.sin(2 * np.pi * 14 * tq)
    env = adsr(dq, 0.03, 0.05, 0.8, 0.3) * np.exp(-tq / 0.3)
    ajoute(x, quinte * trem * env, 0.1, 0.22)
    x += scintillement(d, rng, 6, 6000, 9500, debut=0.1, fin=0.3, chute=0.025, volume=0.15)
    x = reverb(x, 0.12, 0.4, brillance=8000)
    return finalise(x, d, 0.002, 0.02, cible_oreille=-18.0)


# =========================== boîtes : touches thématiques des Secrets ===========================

def pirate_vague(rng):
    """
    Touche Pirates (§6.6, à R + 3,4 s, 1,2 s d'effet) : une vague qui déferle. Le bruit
    gonfle (filtre qui s'ouvre : l'eau qui monte), se brise vers 0,4 s (bouffée aiguë),
    puis l'écume pétille et retombe (fines bulles) ; quelques gouttes « plic » (sinus qui
    montent vite) pour le côté cartoon.
    """
    d = 1.0
    x = silence(d)
    t = temps(d)
    fc = np.where(t < 0.4, 300 + 2700 * (t / 0.4) ** 2, 3000 * np.exp(-(t - 0.4) / 0.35) + 400)
    eau = passe_bas_variable(bruit(d, rng), fc, q=0.8)
    env = np.where(t < 0.4, (t / 0.4) ** 1.5, np.exp(-(t - 0.4) / 0.3))
    x += 0.9 * eau * env
    ecume = filtre(bruit(d, rng), "passe_haut", 4000, ordre=2)
    grain = (rng.uniform(0, 1, n_ech(d)) > 0.985).astype(float)
    grain = np.convolve(grain, np.exp(-np.arange(80) / 15.0), "same")
    x += 0.25 * ecume * grain * np.where(t < 0.38, 0, np.exp(-(t - 0.38) / 0.35))
    for t0 in sorted(rng.uniform(0.42, 0.85, 5)):
        dg = 0.05
        plic = sinus(glisse(rng.uniform(900, 1300), rng.uniform(2200, 3000), dg), dg) * perc(dg, 0.001, 0.04)
        ajoute(x, plic, t0, 0.25)
    x = reverb(x, 0.1, 0.5)
    return finalise(x, d, 0.005, 0.05, cible_oreille=-15.5)


def animal_pattes(rng):
    """
    Touche Animaux (§6.6, à R + 3,4 s) : 8 empreintes de pattes, 0,1 s d'écart. Chaque
    pas est un petit « pat » rebondi (sinus qui monte un peu + coussinet de bruit doux),
    gauche-droite en alternance de hauteur, la série grimpe d'une gamme (fa majeur)
    comme si un chaton traversait l'écran en trottinant ; une clochette à la fin.
    """
    d = 1.0
    x = silence(d)
    gamme = ["F5", "G5", "A5", "Bb5", "C6", "D6", "E6", "F6"]
    for i, note in enumerate(gamme):
        dp = 0.07
        f = hz(note) * (1.0 if i % 2 == 0 else 1.02)
        pat = sinus(glisse(f * 0.6, f, 0.025).tolist() + [f] * (n_ech(dp) - n_ech(0.025)), dp) * perc(dp, 0.002, 0.06)
        coussin = filtre(bruit(dp, rng), "passe_bas", 1200, ordre=2) * perc(dp, 0.001, 0.015)
        ajoute(x, pat + 0.7 * coussin, 0.1 * i, 0.5 + 0.03 * i)
    ajoute(x, clochette(hz("F6") * 2, 0.3, 0.25, 3, 0.3), 0.82, 0.25)
    x += scintillement(d, rng, 5, 5500, 8500, debut=0.82, fin=0.95, chute=0.025, volume=0.15)
    x = reverb(x, 0.1, 0.4)
    return finalise(x, d, 0.002, 0.03, cible_oreille=-15.5)


def pirate_secret_kraken(rng):
    """
    Révélation du Kraken (catalogue §2, deux tentacules qui jaillissent des coins) : un
    grondement DOUX qui monte (grave filtré + sinus 55 Hz qui ondule, sans dissonance,
    0,6 s), puis un gros « BLOP » rigolo (sinus qui monte en flèche, comme une ventouse
    qui se décolle) et un petit « blop » plus aigu, avec trois bulles. Le rire après la peur
    pour de faux.
    """
    d = 1.2
    x = silence(d)
    dg = 0.7
    tg = temps(dg)
    gronde = sinus(vibrato(55, dg, 3.0, 1.0), dg) + 0.5 * sinus(vibrato(110, dg, 3.0, 1.0), dg)
    gronde += 0.8 * filtre(bruit(dg, rng), "passe_bas", 180, ordre=2)
    env = np.sin(np.pi * np.clip(tg / dg, 0, 1)) ** 1.2
    ajoute(x, gronde * env, 0.0, 0.3)  # discret : le « blop » doit rester la vedette
    for t0, f0, f1, g in [(0.6, 150, 700, 1.0), (0.82, 300, 1200, 0.6)]:
        db_ = 0.12
        blop = sinus(glisse(f0, f1, db_), db_) * perc(db_, 0.002, 0.1)
        ajoute(x, blop, t0, g)
    for t0 in (0.92, 1.0, 1.06):
        dbu = 0.04
        ajoute(x, sinus(glisse(rng.uniform(800, 1100), rng.uniform(1800, 2400), dbu), dbu) * perc(dbu, 0.001, 0.035), t0, 0.3)
    x = reverb(x, 0.12, 0.6)
    return finalise(x, d, 0.006, 0.04, cible_oreille=-14.5)


def pirate_secret_abysses(rng):
    """
    Révélation des Abysses (l'écran s'assombrit, seule la lanterne éclaire) : un chant
    grave et doux, comme une voix « ouuu » au fond de l'eau. Une voix (scie dans un
    passe-bas résonant bas = voyelle « ou ») qui glisse do3 -> fa3 -> la3, doublée à la
    quinte, avec un écho qui revient deux fois et une lueur aiguë (do6) très légère.
    Grave mais rond et majeur : du mystère tranquille, pas de la peur.
    """
    d = 1.2
    x = silence(d)
    dv = 0.85
    tv = temps(dv)
    f = np.where(tv < 0.3, hz("C3") * (hz("F3") / hz("C3")) ** (tv / 0.3),
                 np.where(tv < 0.5, hz("F3"), hz("F3") * (hz("A3") / hz("F3")) ** np.clip((tv - 0.5) / 0.15, 0, 1)))
    f = vibrato(f, dv, 4.5, 0.15)
    voix = _voix(f, dv, 450 + 250 * np.sin(np.pi * tv / dv), q=3.0, douceur=1500)
    voix += 0.5 * _voix(f * 1.5, dv, 500, q=3.0, douceur=1500)
    ajoute(x, voix * adsr(dv, 0.12, 0.1, 0.85, 0.3), 0.0, 0.6)
    ajoute(x, sinus(hz("C6"), 0.9) * adsr(0.9, 0.3, 0.1, 0.6, 0.4), 0.15, 0.05)
    x = echo(x, 0.22, 0.35, 2)
    x = reverb(x, 0.2, 1.0)
    return finalise(x, d, 0.006, 0.08, cible_oreille=-15.5)


def _rugissement(rng, d, f_haut, cuivres=False):
    """
    Rugissement de lionceau (mignon) : une voix aiguë qui monte puis retombe
    (« rraa-ouu »), avec du grain (rugosité) et un filtre qui s'ouvre puis se ferme
    (voyelles « a » puis « ou »). Base commune de lion_rugit et animal_secret_lion.
    """
    tv = temps(d)
    forme = np.sin(np.pi * np.clip(tv / d, 0, 1))
    f = f_haut * (0.62 + 0.38 * forme ** 0.7)
    voix = _voix(f, d, 600 + 2400 * forme ** 1.5, q=2.2, douceur=3500, rugosite=0.5, rng=rng)
    grain = filtre(bruit(d, rng), "passe_bande", 1200, 1.0) * forme ** 2
    return (voix + 0.25 * grain) * adsr(d, 0.03, 0.05, 0.9, 0.12)


def animal_secret_lion(rng):
    """
    Révélation du Lion solaire (empreintes lumineuses puis lever de soleil) : un
    rugissement de lionceau, fier mais mignon, puis un accord de cuivres chaud (fa majeur,
    « le soleil se lève ») qui fleurit et s'éteint, avec quelques étincelles dorées.
    """
    d = 1.2
    x = silence(d)
    ajoute(x, _rugissement(rng, 0.55, 420), 0.0, 0.8)
    for note, g in [("F3", 0.3), ("C4", 0.22), ("F4", 0.22), ("A4", 0.25)]:
        ajoute(x, cuivre(hz(note), 0.65, a=0.12, r=0.4, vib=0.1), 0.5, g)
    ajoute(x, glockenspiel(hz("F6"), 0.6, 0.5), 0.55, 0.2)
    x += scintillement(d, rng, 10, 4500, 7500, debut=0.55, fin=1.0, chute=0.03, volume=0.18)
    x = reverb(x, 0.15, 0.8)
    return finalise(x, d, 0.004, 0.06, cible_oreille=-13.5)


def _chant_baleine(rng, d, notes, voile=0.0):
    """
    Chant de baleine court et doux : un sinus (et son octave légère) qui glisse
    lentement d'une note à l'autre avec un vibrato lent, comme un « ouuu-iii-ouu » très
    rond. `notes` = [(temps relatif 0-1, fréquence), ...].
    """
    tv = temps(d)
    pts_t = [p[0] for p in notes]
    pts_f = [np.log(p[1]) for p in notes]
    f = np.exp(np.interp(tv / d, pts_t, pts_f))
    f = vibrato(f, d, 4.0, 0.25)
    son = sinus(f, d) + 0.25 * sinus(2 * f, d) + 0.08 * sinus(3 * f, d)
    if voile:
        son += voile * filtre(bruit(d, rng), "passe_bande", 800, 2.0)
    return son * adsr(d, 0.12, 0.1, 0.85, 0.35)


def animal_secret_baleine(rng):
    """
    Révélation de la Baleine céleste (bulles d'étoiles qui montent, silhouette qui passe) :
    un chant de baleine doux (la4 -> do5 -> fa5 -> do5), une grande réverbération « ciel »
    et des bulles d'étoiles (petits sinus qui montent) de plus en plus aiguës.
    """
    d = 1.2
    x = silence(d)
    chant = _chant_baleine(rng, 0.95, [(0, hz("A4")), (0.3, hz("C5")), (0.6, hz("F5")), (1.0, hz("C5"))])
    ajoute(x, chant, 0.0, 0.6)
    for k, t0 in enumerate(np.linspace(0.15, 0.95, 8)):
        dbu = 0.05
        f0 = 1200 + 150 * k
        ajoute(x, sinus(glisse(f0, f0 * 1.8, dbu), dbu) * perc(dbu, 0.002, 0.045), t0 + rng.uniform(-0.02, 0.02), 0.18)
    x += scintillement(d, rng, 8, 5000, 8000, debut=0.4, fin=1.0, chute=0.03, volume=0.12)
    x = reverb(x, 0.25, 1.2)
    return finalise(x, d, 0.006, 0.08, cible_oreille=-15.0)


# =========================== jeu : objets animés (saut de joie) ===========================

def pirate_eclair(rng):
    """
    Skin Tempête : un petit éclair parcourt le corps toutes les 2 à 4 s. Un « bzzt » bref
    et pétillant (crépitement de bruit aigu + zip qui descend 2 400 -> 600 Hz), sans aucun
    tonnerre : c'est une étincelle de jouet. Très court et doux : il revient souvent.
    """
    d = 0.3
    x = silence(d)
    tc = 0.0
    while tc < 0.15:
        c = filtre(bruit(0.01, rng), "passe_haut", 3000) * perc(0.01, 0.0003, 0.005)
        ajoute(x, c, tc, rng.uniform(0.4, 1.0))
        tc += rng.uniform(0.006, 0.02)
    dz = 0.12
    zip_ = sinus(glisse(2400, 600, dz), dz) * perc(dz, 0.001, 0.1)
    ajoute(x, zip_, 0.0, 0.35)
    x = reverb(x, 0.1, 0.3)
    return finalise(x, d, 0.001, 0.02, cible_oreille=-18.0)


def perroquet_cri(rng):
    """
    Perroquet (Légendaire) qui bat des ailes : « rouah ! » mignon. Une voix nasillarde
    (scie, filtre résonant élevé) qui monte puis retombe (900 -> 1 400 -> 1 000 Hz), avec
    un peu de grain, et deux froufrous d'ailes.
    """
    d = 0.4
    x = silence(d)
    dv = 0.24
    tv = temps(dv)
    forme = np.sin(np.pi * np.clip(tv / dv, 0, 1))
    f = 900 + 500 * forme - 100 * (tv / dv)
    voix = _voix(f, dv, 1500 + 2500 * forme, q=4.0, douceur=5000, rugosite=0.4, rng=rng)
    ajoute(x, voix * adsr(dv, 0.01, 0.03, 0.9, 0.06), 0.0, 0.7)
    for t0 in (0.22, 0.3):
        da = 0.07
        aile = filtre(bruit(da, rng), "passe_bande", 1800, 1.0) * np.sin(np.pi * np.linspace(0, 1, n_ech(da)))
        ajoute(x, aile, t0, 0.25)
    x = reverb(x, 0.08, 0.3)
    return finalise(x, d, 0.002, 0.02, cible_oreille=-16.0)


def kraken_bebe(rng):
    """
    Petit Kraken (Secret, sur la tête) qui lance sa pièce : « blop-blop » aigu, deux bulles
    rigolotes qui montent (sinus 400 -> 1 000 Hz puis 550 -> 1 400 Hz) + une goutte.
    """
    d = 0.4
    x = silence(d)
    for t0, f0, f1 in [(0.0, 400, 1000), (0.13, 550, 1400)]:
        db_ = 0.1
        ajoute(x, sinus(glisse(f0, f1, db_), db_) * perc(db_, 0.002, 0.085), t0, 0.8)
    dg = 0.05
    ajoute(x, sinus(glisse(1500, 2800, dg), dg) * perc(dg, 0.001, 0.04), 0.27, 0.3)
    x = reverb(x, 0.1, 0.3)
    return finalise(x, d, 0.002, 0.02, cible_oreille=-16.0)


def navire_canon(rng):
    """
    Navire fantôme qui tire une salve de confettis : petit « pouf » de cotillon (bruit
    sourd + sinus qui chute 220 -> 90 Hz, comme un bouchon de jouet), puis une pluie de
    crépitements de papier (confettis) et une clochette.
    """
    d = 0.5
    x = silence(d)
    dp = 0.12
    pouf = filtre(bruit(dp, rng), "passe_bas", 900, ordre=2) * perc(dp, 0.001, 0.07)
    pouf += 0.6 * sinus(glisse(220, 90, dp), dp) * perc(dp, 0.002, 0.09)
    ajoute(x, pouf, 0.0, 1.0)
    for _ in range(14):
        c = filtre(bruit(0.01, rng), "passe_bande", rng.uniform(3000, 6000), 2.0) * perc(0.01, 0.0003, 0.006)
        ajoute(x, c, rng.uniform(0.06, 0.4), rng.uniform(0.15, 0.4))
    ajoute(x, clochette(hz("F6"), 0.2, 0.18, 3, 0.3), 0.08, 0.15)
    x = reverb(x, 0.08, 0.3)
    return finalise(x, d, 0.001, 0.02, cible_oreille=-16.0)


def paon_roue(rng):
    """
    Skin Paon : la roue se déploie (0,35 s) puis « ta-da ». Un froufrou de plumes qui
    s'ouvre (bruit filtré haché à 28 Hz, filtre qui monte) puis deux notes de glockenspiel
    do6 -> fa6 et un accord de harpe léger.
    """
    d = 0.8
    x = silence(d)
    df = 0.36
    tf = temps(df)
    plumes = passe_bas_variable(filtre(bruit(df, rng), "passe_haut", 800), glisse(1500, 6000, df), q=1.0)
    hache = 0.4 + 0.6 * (0.5 + 0.5 * np.sin(2 * np.pi * 28 * tf)) ** 2
    ajoute(x, plumes * hache * np.sin(np.pi * tf / df) ** 1.2, 0.0, 0.45)
    ajoute(x, glockenspiel(hz("C6"), 0.3, 0.25), 0.34, 0.45)
    ajoute(x, glockenspiel(hz("F6"), 0.45, 0.4), 0.44, 0.55)
    for i, note in enumerate(["F4", "A4", "C5"]):
        ajoute(x, harpe(hz(note), 0.35, 0.3), 0.44 + 0.015 * i, 0.25)
    x = reverb(x, 0.12, 0.5)
    return finalise(x, d, 0.003, 0.04, cible_oreille=-15.5)


def lion_rugit(rng):
    """Skin Lion solaire, saut de joie : petit rugissement de lionceau (0,45 s) + étincelles chaudes."""
    d = 0.6
    x = silence(d)
    ajoute(x, _rugissement(rng, 0.45, 380), 0.0, 0.9)
    x += scintillement(d, rng, 5, 4000, 6500, debut=0.2, fin=0.45, chute=0.03, volume=0.15)
    x = reverb(x, 0.1, 0.4)
    return finalise(x, d, 0.003, 0.04, cible_oreille=-15.5)


def poussin_piou(rng):
    """Poussin (Rare) qui piaille : « piou-piou », deux pépiements aigus qui montent puis tombent."""
    d = 0.3
    x = silence(d)
    for t0, f_haut in [(0.0, 3400), (0.12, 3700)]:
        dp = 0.09
        tp = np.linspace(0, 1, n_ech(dp))
        f = 2400 + (f_haut - 2400) * np.sin(np.pi * np.clip(tp * 1.3, 0, 1)) - 300 * tp
        ajoute(x, sinus(f, dp) * np.sin(np.pi * tp) ** 0.8, t0, 0.7)
    x = reverb(x, 0.08, 0.25)
    return finalise(x, d, 0.003, 0.02, cible_oreille=-17.0)


def chaton_miaou(rng):
    """
    Chaton (Super rare) qui lève la patte : « miaou » minuscule. Voix aiguë (scie douce)
    qui monte puis descend (600 -> 950 -> 550 Hz) pendant que le filtre fait « i-a-ou »
    (haut puis plus haut puis bas), avec un léger vibrato.
    """
    d = 0.5
    x = silence(d)
    dv = 0.42
    tv = temps(dv)
    u = tv / dv
    f = np.interp(u, [0, 0.35, 1.0], [600, 950, 550])
    f = vibrato(f, dv, 6.0, 0.2)
    fc = np.interp(u, [0, 0.15, 0.5, 1.0], [2500, 1800, 3200, 900])
    voix = _voix(f, dv, fc, q=3.0, douceur=4000)
    ajoute(x, voix * adsr(dv, 0.04, 0.05, 0.85, 0.15), 0.0, 0.8)
    x = reverb(x, 0.1, 0.35)
    return finalise(x, d, 0.003, 0.03, cible_oreille=-16.5)


def baleine_chant(rng):
    """
    Baleine céleste (Secret, accessoire) au saut de joie : chant court et doux (do5 ->
    fa5 -> ré5) + jet de quelques étoiles. Plus simple et plus court que la touche Secret.
    """
    d = 1.2
    x = silence(d)
    chant = _chant_baleine(rng, 0.9, [(0, hz("C5")), (0.45, hz("F5")), (1.0, hz("D5"))])
    ajoute(x, chant, 0.0, 0.6)
    x += scintillement(d, rng, 8, 5000, 8000, debut=0.5, fin=0.95, chute=0.03, volume=0.15)
    x = reverb(x, 0.22, 1.0)
    return finalise(x, d, 0.006, 0.08, cible_oreille=-16.0)


# =========================== génération ===========================

def genere():
    # Une graine par son : ajouter ou retoucher un son ne change pas les autres.
    print("Sons v5 :")
    g = np.random.default_rng
    for palier in (1, 2, 3):
        ecris_wav("jeu", "diamant_gain_%d" % palier, diamant_gain(g(600 + palier), palier))
    ecris_wav("jeu", "diamant_compteur", diamant_compteur(g(604)))
    ecris_wav("ui", "reprise_offre", reprise_offre(g(605)))
    ecris_wav("jeu", "reprise_ok", reprise_ok(g(606)))
    ecris_wav("jeu", "renaissance", renaissance(g(607)))
    ecris_wav("jeu", "renaissance_tampon", renaissance_tampon(g(608)))
    ecris_wav("ui", "recap_etage", recap_etage(g(611)))
    ecris_wav("ui", "ui_bouton_rond", ui_bouton_rond(g(612)))
    ecris_wav("ui", "ui_infobulle", ui_infobulle(g(613)))
    ecris_wav("ui", "ui_onglet", ui_onglet(g(614)))
    ecris_wav("ui", "ui_refus", ui_refus(g(615)))
    ecris_wav("boite", "boite3d_atterrit", boite3d_atterrit(g(621)))
    ecris_wav("boite", "boite3d_saut", boite3d_saut(g(622)))
    ecris_wav("boite", "boite3d_craque", boite3d_craque(g(623)))
    ecris_wav("boite", "boite3d_noeud", boite3d_noeud(g(624)))
    ecris_wav("boite", "boite3d_couvercle", boite3d_couvercle(g(625)))
    ecris_wav("boite", "boite3d_faisceau", boite3d_faisceau(g(626)))
    ecris_wav("boite", "pirate_vague", pirate_vague(g(631)))
    ecris_wav("boite", "animal_pattes", animal_pattes(g(632)))
    ecris_wav("boite", "pirate_secret_kraken", pirate_secret_kraken(g(633)))
    ecris_wav("boite", "pirate_secret_abysses", pirate_secret_abysses(g(634)))
    ecris_wav("boite", "animal_secret_lion", animal_secret_lion(g(635)))
    ecris_wav("boite", "animal_secret_baleine", animal_secret_baleine(g(636)))
    ecris_wav("jeu", "pirate_eclair", pirate_eclair(g(641)))
    ecris_wav("jeu", "perroquet_cri", perroquet_cri(g(642)))
    ecris_wav("jeu", "kraken_bebe", kraken_bebe(g(643)))
    ecris_wav("jeu", "navire_canon", navire_canon(g(644)))
    ecris_wav("jeu", "paon_roue", paon_roue(g(645)))
    ecris_wav("jeu", "lion_rugit", lion_rugit(g(646)))
    ecris_wav("jeu", "poussin_piou", poussin_piou(g(647)))
    ecris_wav("jeu", "chaton_miaou", chaton_miaou(g(648)))
    ecris_wav("jeu", "baleine_chant", baleine_chant(g(649)))


if __name__ == "__main__":
    genere()
