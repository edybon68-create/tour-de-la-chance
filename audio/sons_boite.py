# -*- coding: utf-8 -*-
"""
Sons des boîtes à tirage aléatoire (catégorie « boite »).

- boite_secoue : la boîte tremble, suspense ;
- boite_ouverture : le couvercle saute ;
- revelation_* : six révélations, de la plus simple (commun) à la plus spectaculaire
  (légendaire), plus une « à part » et mystérieuse (secret).

L'échelle des révélations monte sur plusieurs axes à la fois, pour qu'on la reconnaisse
même sans regarder l'écran : nombre de notes (1, 2, 3, 5...), durée, hauteur, densité
d'étincelles, réverbération, et arrivée progressive de nouveaux instruments (nappe,
cuivres, timbale). Toutes en fa majeur (la tonalité de la musique), sauf « secret », qui
sort volontairement de la tonalité.

Lancer : <python de Blender> audio/sons_boite.py
"""
import numpy as np

from synth import (adsr, ajoute, bruit, clochette, cuivre, ecris_wav, filtre, finalise,
                   glisse, glockenspiel, harpe, hz, n_ech, passe_bas_variable, perc, reverb,
                   scie, scintillement, silence, sinus, temps)


# --------------------------- secouer / ouvrir ---------------------------

def _choc(rng, centre, q, chute, d=0.05):
    b = bruit(d, rng) * perc(d, 0.0005, chute)
    return filtre(b, "passe_bande", centre, q) * 3.0


def boite_secoue(rng):
    """
    La boîte tremble : des secousses par paires (gauche-droite, comme une boîte qu'on agite)
    de plus en plus rapides ; à chaque secousse, les parois de bois cognent (grave, ~500 Hz)
    et les objets à l'intérieur cliquettent (aigu, 2,5-4,5 kHz, au hasard). Dessous, un
    souffle qui s'éclaircit fait monter le suspense.
    """
    d = 1.0
    x = silence(d)
    t0 = 0.0
    intervalle = 0.11
    k = 0
    while t0 < 0.9:
        g = 0.45 + 0.55 * (t0 / 0.9)
        # paroi de la boîte : bois grave + petit « toc » sinus
        ajoute(x, _choc(rng, rng.uniform(450, 600), 4, 0.012), t0, g)
        ajoute(x, sinus(glisse(210, 150, 0.04), 0.04) * perc(0.04, 0.001, 0.03), t0, 0.4 * g)
        # objets qui s'entrechoquent dedans, juste après la secousse
        for _ in range(rng.integers(2, 4)):
            dt = rng.uniform(0.008, 0.05)
            ajoute(x, _choc(rng, rng.uniform(2500, 4500), 8, 0.006, 0.03), t0 + dt,
                   rng.uniform(0.25, 0.55) * g)
        t0 += intervalle * (1.0 if k % 2 == 0 else 0.8)
        intervalle = max(0.055, intervalle * 0.95)  # les secousses accélèrent
        k += 1
    souffle = passe_bas_variable(bruit(d, rng), glisse(400, 2200, d), q=1.5)
    t = temps(d)
    x = x + 0.18 * souffle * (t / d) ** 1.5
    return finalise(x, d, 0.004, 0.02)


def boite_ouverture(rng):
    """
    Le couvercle saute : « pop » feutré (sinus qui chute très vite + bruit sourd), puis un
    souffle magique qui monte vers l'aigu, avec une petite traînée d'étincelles.
    """
    d = 0.6
    x = silence(d)
    dp = 0.08
    pop = sinus(glisse(420, 110, dp, "exp"), dp) * perc(dp, 0.001, 0.07)
    feutre = filtre(bruit(dp, rng), "passe_bas", 1200, ordre=2) * perc(dp, 0.001, 0.03)
    ajoute(x, pop + 0.8 * feutre, 0.0)
    ds = 0.55
    souffle = filtre(bruit(ds, rng), "passe_haut", 500)
    souffle = passe_bas_variable(souffle, glisse(900, 7000, ds), q=1.8)
    ts = temps(ds)
    env = np.sin(np.pi * np.clip(ts / ds, 0, 1)) ** 2
    ajoute(x, 0.35 * souffle * env, 0.03)
    # trois notes de clochette qui montent avec le souffle (le côté « magique »)
    for i, note in enumerate(["C6", "F6", "A6"]):
        ajoute(x, clochette(hz(note), 0.3, 0.25), 0.1 + 0.08 * i, 0.18)
    x += scintillement(d, rng, 10, 5000, 8000, debut=0.15, fin=0.45, chute=0.025, volume=0.15)
    x = reverb(x, 0.12, 0.5)
    return finalise(x, d, 0.001, 0.05)


# --------------------------- révélations ---------------------------

def nappe(notes, duree, attaque=0.25, rng=None, volume=1.0):
    """Nappe douce et chatoyante : deux dents de scie désaccordées par note, filtrées."""
    x = silence(duree)
    env = adsr(duree, attaque, 0.2, 0.8, duree * 0.4)
    for note in notes:
        f = hz(note)
        son = scie(f * 1.004, duree, douceur=1800) + scie(f * 0.996, duree, douceur=1800)
        x += son
    x = filtre(x, "passe_bas", 2500)
    return x * env * volume / max(len(notes), 1)


def revelation_commun():
    """Commun : un seul « ding » simple et propre (do6), petite réverb."""
    d = 0.5
    x = clochette(hz("C6"), d, 0.42, harmonique=3, poids=0.25)
    x = reverb(x, 0.08, 0.4)
    return finalise(x, d, 0.002, 0.04, cible_db=-19.0)


def revelation_peu_commun(rng):
    """Peu commun : deux notes qui montent (la5 -> do6), un peu plus brillantes, 3 étincelles."""
    d = 0.8
    x = silence(d)
    ajoute(x, glockenspiel(hz("A5"), 0.5, 0.4), 0.0, 0.75)
    ajoute(x, glockenspiel(hz("C6"), 0.7, 0.6), 0.1, 1.0)
    x += scintillement(d, rng, 3, 4500, 5500, debut=0.15, fin=0.4, chute=0.03, volume=0.25)
    x = reverb(x, 0.12, 0.6)
    return finalise(x, d, 0.002, 0.05, cible_db=-17.5)


def revelation_rare(rng):
    """Rare : arpège de 3 notes (fa5-la5-do6) en glockenspiel + harpe, étincelles."""
    d = 1.1
    x = silence(d)
    for i, note in enumerate(["F5", "A5", "C6"]):
        ajoute(x, glockenspiel(hz(note), 0.8, 0.7), 0.09 * i, 0.7 + 0.15 * i)
        ajoute(x, harpe(hz(note) / 2, 0.8, 0.7), 0.09 * i, 0.35)
    x += scintillement(d, rng, 8, 4000, 6500, debut=0.2, fin=0.7, chute=0.035, volume=0.3)
    x = reverb(x, 0.16, 0.8)
    return finalise(x, d, 0.002, 0.08, cible_db=-16.0)


def revelation_super_rare(rng):
    """
    Super rare : arpège rapide de 5 notes jusqu'au la6, une nappe chatoyante qui gonfle
    dessous, un souffle montant et une pluie d'étincelles.
    """
    d = 1.5
    x = silence(d)
    for i, note in enumerate(["F5", "A5", "C6", "F6", "A6"]):
        ajoute(x, glockenspiel(hz(note), 1.0, 0.8), 0.07 * i, 0.6 + 0.1 * i)
    ajoute(x, nappe(["F4", "A4", "C5", "F5"], 1.3, attaque=0.2, volume=0.9), 0.05)
    souffle = passe_bas_variable(bruit(0.6, rng), glisse(600, 5000, 0.6), q=1.4)
    ajoute(x, 0.15 * souffle * np.sin(np.pi * np.linspace(0, 1, n_ech(0.6))) ** 2, 0.0)
    x += scintillement(d, rng, 20, 4000, 7000, debut=0.3, fin=1.2, chute=0.04, volume=0.3)
    x = reverb(x, 0.2, 1.0)
    return finalise(x, d, 0.003, 0.12, cible_db=-14.5)


def revelation_legendaire(rng):
    """
    Légendaire : petite fanfare de cuivres (trois notes d'appel do5-fa5-la5 puis un accord
    de fa majeur tenu), timbale grave sur l'accord, glockenspiel qui monte jusqu'au do7,
    nappe et cascade d'étincelles. Le son le plus « triomphal » du jeu.
    """
    d = 2.0
    x = silence(d)
    # appel de cuivres (triolet)
    for i, note in enumerate(["C5", "F5", "A5"]):
        ajoute(x, cuivre(hz(note), 0.12, a=0.015, r=0.04, vib=0), 0.1 * i, 0.5)
    # accord tenu
    t_acc = 0.3
    for note, g in [("F3", 0.45), ("C4", 0.35), ("F4", 0.35), ("A4", 0.3), ("C5", 0.3), ("F5", 0.32)]:
        ajoute(x, cuivre(hz(note), 1.15, a=0.05, r=0.45), t_acc, g)
    # timbale : sinus grave qui baisse un peu + coup de bruit
    dt = 0.8
    timbale = sinus(glisse(98, 82, dt), dt) * perc(dt, 0.002, 0.7)
    timbale += filtre(bruit(dt, rng), "passe_bas", 400) * perc(dt, 0.001, 0.08) * 0.8
    ajoute(x, timbale, t_acc, 0.9)
    # glockenspiel qui grimpe pendant l'accord
    for i, note in enumerate(["F5", "A5", "C6", "F6", "A6", "C7"]):
        ajoute(x, glockenspiel(hz(note), 0.9, 0.7), t_acc + 0.06 + 0.06 * i, 0.3 + 0.04 * i)
    ajoute(x, nappe(["A5", "C6", "F6"], 1.4, attaque=0.3, volume=0.5), t_acc)
    x += scintillement(d, rng, 40, 4000, 8000, debut=0.4, fin=1.8, chute=0.04, volume=0.28)
    x = reverb(x, 0.22, 1.3, brillance=7000)
    return finalise(x, d, 0.003, 0.2)


def revelation_secret(rng):
    """
    Secret : à part de toutes les autres. Un souffle « à l'envers » qui gonfle (comme si le
    son arrivait de loin), puis un accord INATTENDU hors de la tonalité du jeu : ré bémol
    majeur avec septième majeure et quarte augmentée (ré♭-fa-la♭-do-sol), couleur « lydienne »,
    féerique et suspendue, qui ne se résout pas. Des cloches de verre descendent en cascade,
    un grave très doux tient le tout, et des étincelles glissent vers l'aigu.
    Mystérieux, jamais inquiétant : l'accord est majeur et lumineux.
    """
    d = 2.5
    x = silence(d)
    # 1) souffle qui gonfle (0 -> 0,45 s) puis coupe net sur l'accord
    dg = 0.45
    souffle = passe_bas_variable(bruit(dg, rng), glisse(300, 6000, dg), q=2.5)
    ajoute(x, 0.3 * souffle * np.linspace(0, 1, n_ech(dg)) ** 2.5, 0.0)
    # petite montée aiguë en glissando continu (sinus) dans le souffle
    ajoute(x, sinus(glisse(800, 2600, dg), dg) * np.linspace(0, 1, n_ech(dg)) ** 3, 0.0, 0.12)
    t_acc = 0.45
    accord = ["Db4", "Ab4", "F5", "C6", "G6"]
    # 2) l'accord en nappe chatoyante (deux scies légèrement désaccordées par note)
    ajoute(x, nappe(accord, 1.95, attaque=0.08, volume=1.3), t_acc)
    # grave doux qui tient (ré bémol 2)
    dgrave = 1.9
    grave = sinus(hz("Db2"), dgrave) * adsr(dgrave, 0.05, 0.3, 0.6, 1.0)
    ajoute(x, grave, t_acc, 0.45)
    # 3) cloches de verre : partiels non entiers, descendent en cascade sur l'accord
    def verre(f, duree=1.0):
        return (sinus(f, duree) * perc(duree, 0.001, 0.9)
                + 0.4 * sinus(f * 3.01, duree) * perc(duree, 0.001, 0.4)
                + 0.2 * sinus(f * 5.43, duree) * perc(duree, 0.001, 0.2))
    cascade = ["G6", "F6", "C6", "Ab5", "G5", "F5", "Db5"]
    for i, note in enumerate(cascade):
        ajoute(x, verre(hz(note)), t_acc + 0.02 + 0.11 * i, 0.32)
    # 4) étincelles qui « glissent » vers l'aigu (signature unique du secret)
    for _ in range(18):
        t0 = rng.uniform(t_acc + 0.1, 2.0)
        f0 = rng.uniform(3000, 5000)
        dd = 0.12
        ajoute(x, sinus(glisse(f0, f0 * 1.5, dd), dd) * perc(dd, 0.002, 0.1),
               t0, rng.uniform(0.06, 0.14))
    x = reverb(x, 0.28, 1.6, brillance=7500)
    return finalise(x, d, 0.004, 0.3)


def genere():
    rng = np.random.default_rng(404)
    print("Sons boite :")
    ecris_wav("boite", "boite_secoue", boite_secoue(rng))
    ecris_wav("boite", "boite_ouverture", boite_ouverture(rng))
    ecris_wav("boite", "revelation_commun", revelation_commun())
    ecris_wav("boite", "revelation_peu_commun", revelation_peu_commun(rng))
    ecris_wav("boite", "revelation_rare", revelation_rare(rng))
    ecris_wav("boite", "revelation_super_rare", revelation_super_rare(rng))
    ecris_wav("boite", "revelation_legendaire", revelation_legendaire(rng))
    ecris_wav("boite", "revelation_secret", revelation_secret(rng))


if __name__ == "__main__":
    genere()
