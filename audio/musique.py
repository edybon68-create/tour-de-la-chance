# -*- coding: utf-8 -*-
"""
Les deux musiques de Tour de la Chance (catégorie « musique ») — spec §5.2.

- musique_ambiance : valse lente de boîte à musique + orgue de barbarie, fa majeur,
  120 battements par minute, 3 temps par mesure, 32 mesures = exactement 48 s.
- musique_tension : une COUCHE qui se superpose à l'ambiance (même tempo, même durée,
  même grille d'accords) : cœur qui bat, basse pizzicato, nappe en trémolo rapide.

Comment la boucle reste invisible :
1. on calcule la musique dans un tampon plus long que 48 s (48 s + 4 s de marge) ;
2. tout ce qui dépasse 48 s (notes qui résonnent encore, queue de réverbération) est
   RECOPIÉ par-dessus le début du fichier ;
3. on garde exactement 48 s. Quand Unity reboucle, la fin de la mesure 32 enchaîne sur
   la mesure 1 qui contient déjà la « suite » du son de la fin : aucune coupure.
Pour la même raison, PAS de fondu au début et à la fin des musiques (un fondu créerait un
petit trou de volume à chaque tour de boucle) : le raccord est assuré par le point 2.

Lancer : <python de Blender> audio/musique.py
"""
import numpy as np

from synth import (adsr, ajoute, boite_a_musique, ecris_wav, filtre, glockenspiel, hz,
                   n_ech, normalise, orgue, panoramique, perc, reverb, scie, sinus,
                   glisse, temps, triangle, SR)

BPM = 120
TEMPS = 60.0 / BPM          # durée d'un temps : 0,5 s
MESURE = 3 * TEMPS          # valse : 3 temps = 1,5 s
NB_MESURES = 32
DUREE = NB_MESURES * MESURE  # 48 s
MARGE = 4.0                  # place pour les queues de notes et de réverbération

# Grille d'accords : une mesure par accord. Forme A - A' - B - A.
GRILLE_A = ["F", "F", "Bb", "F", "C", "C", "F", "C"]
GRILLE_B = ["Dm", "Dm", "Bb", "Bb", "Gm", "Gm", "C", "C"]
GRILLE = GRILLE_A + GRILLE_A + GRILLE_B + GRILLE_A

# Pour chaque accord : basse (temps 1), accord d'orgue (temps 2 et 3), nappe aiguë (tension)
ACCORDS = {
    "F":  {"basse": "F2",  "orgue": ["A3", "C4", "F4"],  "nappe": ["F5", "A5", "C6"]},
    "Bb": {"basse": "Bb2", "orgue": ["Bb3", "D4", "F4"], "nappe": ["F5", "Bb5", "D6"]},
    "C":  {"basse": "C3",  "orgue": ["G3", "C4", "E4"],  "nappe": ["E5", "G5", "C6"]},
    "Dm": {"basse": "D3",  "orgue": ["A3", "D4", "F4"],  "nappe": ["D5", "F5", "A5"]},
    "Gm": {"basse": "G2",  "orgue": ["G3", "Bb3", "D4"], "nappe": ["D5", "G5", "Bb5"]},
}

# Mélodie : pour chaque mesure, une liste de (note, durée en temps). Entre do5 et do6.
MELODIE_A = [
    [("C5", 1), ("F5", 1), ("A5", 1)],
    [("C6", 2), ("A5", 1)],
    [("Bb5", 1), ("A5", 1), ("G5", 1)],
    [("A5", 3)],
    [("G5", 1), ("E5", 1), ("G5", 1)],
    [("Bb5", 2), ("A5", 1)],
    [("A5", 1), ("F5", 1), ("A5", 1)],
    [("G5", 2), ("E5", 1)],
]
MELODIE_A2 = [
    [("C5", 1), ("F5", 1), ("A5", 1)],
    [("C6", 1), ("A5", 1), ("F5", 1)],
    [("D5", 1), ("F5", 1), ("Bb5", 1)],
    [("A5", 2), ("F5", 1)],
    [("G5", 1), ("A5", 1), ("Bb5", 1)],
    [("C6", 2), ("Bb5", 1)],
    [("A5", 1), ("G5", 1), ("F5", 1)],
    [("E5", 2), ("G5", 1)],
]
MELODIE_B = [
    [("A5", 2), ("F5", 1)],
    [("D5", 2), ("F5", 1)],
    [("Bb5", 2), ("F5", 1)],
    [("D5", 2), ("F5", 1)],
    [("G5", 2), ("Bb5", 1)],
    [("A5", 1), ("Bb5", 1), ("G5", 1)],
    [("E5", 1), ("G5", 1), ("C6", 1)],
    [("Bb5", 2), ("G5", 1)],
]
MELODIE = MELODIE_A + MELODIE_A2 + MELODIE_B + MELODIE_A


def debut_mesure(m):
    return m * MESURE


def raccorde_boucle(x):
    """Recopie ce qui dépasse 48 s sur le début, puis coupe à 48 s pile (boucle parfaite)."""
    n = n_ech(DUREE)
    sortie = x[:n].copy()
    reste = x[n:]
    while len(reste) > 0:  # (au cas où la queue dépasserait 48 s, ce qui n'arrive pas ici)
        k = min(len(reste), n)
        sortie[:k] += reste[:k]
        reste = reste[k:]
    return sortie


def musique_ambiance(rng):
    tampon = np.zeros((n_ech(DUREE + MARGE), 2))

    # --- Mélodie en boîte à musique (légèrement à droite) ---
    for m, mesure in enumerate(MELODIE):
        t = debut_mesure(m)
        for note, nb_temps in mesure:
            vel = rng.uniform(0.85, 1.0) * (1.0 if t % MESURE == 0 else 0.9)
            son = boite_a_musique(hz(note), 1.4, 0.8 + 0.15 * nb_temps)
            ajoute(tampon, panoramique(son, 0.25), t, 0.55 * vel)
            t += nb_temps * TEMPS
    # Dernier A : un glockenspiel très doux double la mélodie à l'octave (petit « plus »)
    for m in range(24, 32):
        t = debut_mesure(m)
        for note, nb_temps in MELODIE[m]:
            son = glockenspiel(2 * hz(note), 0.8, 0.5)
            ajoute(tampon, panoramique(son, 0.4), t, 0.09)
            t += nb_temps * TEMPS

    # --- Accompagnement d'orgue de barbarie : « boum - tchak - tchak » (un peu à gauche) ---
    for m, nom in enumerate(GRILLE):
        acc = ACCORDS[nom]
        t = debut_mesure(m)
        basse = orgue(hz(acc["basse"]), 0.45, a=0.01, r=0.12, s=0.7, coupure=1500)
        ajoute(tampon, panoramique(basse, -0.1), t, 0.32)
        for temps_n in (1, 2):
            for note in acc["orgue"]:
                son = orgue(hz(note), 0.32, a=0.012, r=0.1, s=0.6, coupure=1500)
                ajoute(tampon, panoramique(son, -0.3), t + temps_n * TEMPS, 0.09)

    # Filtre passe-bas général de l'orgue à 1 500 Hz déjà dans le timbre ; on adoucit
    # aussi un peu l'ensemble pour l'effet « au loin, la nuit ».
    tampon = filtre(tampon, "passe_bas", 9000)
    sortie = reverb(tampon, mix=0.22, duree=1.8, graine=11, brillance=5000)
    sortie = raccorde_boucle(sortie)
    sortie -= sortie.mean(axis=0)
    return normalise(sortie, -1.0)


def musique_tension(rng):
    tampon = np.zeros((n_ech(DUREE + MARGE), 2))
    for m, nom in enumerate(GRILLE):
        acc = ACCORDS[nom]
        t = debut_mesure(m)
        # 1) cœur « boum-boum » sur les temps 1 et 2 : sinus 60 Hz, chute rapide
        for temps_n, g in ((0, 1.0), (1, 0.75)):
            d = 0.35
            coeur = (sinus(glisse(72, 55, d), d) + 0.3 * sinus(glisse(144, 110, d), d))
            coeur *= perc(d, 0.004, 0.25)
            ajoute(tampon, panoramique(coeur, 0.0), t + temps_n * TEMPS, 0.6 * g)
        # 2) basse pizzicato (triangle, note courte) sur le temps 1
        d = 0.3
        pizz = triangle(hz(acc["basse"]), d) * perc(d, 0.003, 0.22)
        ajoute(tampon, panoramique(pizz, -0.15), t, 0.4)
        # 3) nappe aiguë en trémolo rapide (16 frappes par mesure de 1,5 s... soit 8 Hz,
        #    calé sur le tempo : 4 ondulations par temps), deux scies désaccordées filtrées.
        d = MESURE + 0.12
        nappe = np.zeros(n_ech(d))
        for note in acc["nappe"]:
            f = hz(note)
            nappe += scie(f * 1.005, d, douceur=2000) + scie(f * 0.995, d, douceur=2000)
        tt = temps(d) + t  # temps ABSOLU : le trémolo reste calé d'une mesure à l'autre
        tremolo = 0.55 + 0.45 * np.cos(2 * np.pi * 8.0 * tt)
        nappe *= tremolo * adsr(d, 0.06, 0.1, 0.9, 0.14)
        nappe = filtre(nappe, "passe_bas", 2200)
        # gauche et droite légèrement différentes (gauche un peu plus sombre) : de l'espace
        ajoute(tampon, panoramique(nappe, 0.2), t, 0.05)
        ajoute(tampon, panoramique(filtre(nappe, "passe_bas", 1400), -0.3), t, 0.04)
    sortie = reverb(tampon, mix=0.2, duree=1.5, graine=13, brillance=5000)
    sortie = raccorde_boucle(sortie)
    sortie -= sortie.mean(axis=0)
    return normalise(sortie, -1.0)


def genere():
    print("Musiques :")
    ecris_wav("musique", "musique_ambiance", musique_ambiance(np.random.default_rng(505)))
    ecris_wav("musique", "musique_tension", musique_tension(np.random.default_rng(606)))


if __name__ == "__main__":
    genere()
