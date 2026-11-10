# -*- coding: utf-8 -*-
"""
Sons d'interface (catégorie « ui ») — spec docs/specs/sons-animations.md §5.2.
Discrets et courts : ils accompagnent un geste sans voler la vedette aux résultats.
Lancer : <python de Blender> audio/sons_ui.py
"""
import numpy as np

from synth import (ajoute, bruit, clochette, ecris_wav, echo, filtre, finalise, glisse,
                   harpe, boite_a_musique, hz, n_ech, passe_bas_variable, perc, reverb,
                   silence, sinus, temps, ting, adsr)


def ui_survol():
    """Un tic léger : sinus 1 800 Hz, attaque 2 ms, chute 40 ms."""
    d = 0.05
    x = sinus(1800, d) * perc(d, 0.002, 0.04)
    return finalise(x, d, 0.002, 0.005)


def ui_clic(rng):
    """Un « toc » de bouton en bois : sinus 900 -> 600 Hz + petit bruit sourd de 10 ms."""
    d = 0.08
    x = sinus(glisse(900, 600, d), d) * perc(d, 0.002, 0.07)
    b = filtre(bruit(d, rng), "passe_bas", 2000, ordre=2) * perc(d, 0.0005, 0.010)
    return finalise(x + 0.9 * b, d, 0.002, 0.005)


def ui_jouer():
    """« C'est parti ! » : do5 - mi5 - sol5 puis do6 tenu, en boîte à musique, petit écho."""
    d = 0.6
    x = silence(d)
    for i, note in enumerate(["C5", "E5", "G5"]):
        ajoute(x, boite_a_musique(hz(note), 0.3, 0.25), 0.07 * i, 0.8)
    ajoute(x, boite_a_musique(hz("C6"), 0.45, 0.4), 0.21, 1.0)
    x = echo(x, 0.11, 0.25, 2)
    return finalise(x, d, 0.003, 0.04)


def ui_etage_monte(rng):
    """
    On s'envole : glissando de harpe sur 2 octaves de fa majeur en 0,6 s
    + souffle dont le filtre s'ouvre de 300 à 3 000 Hz.
    """
    d = 1.0
    x = silence(d)
    gamme = ["F4", "G4", "A4", "Bb4", "C5", "D5", "E5",
             "F5", "G5", "A5", "Bb5", "C6", "D6", "E6", "F6"]
    pas = 0.6 / (len(gamme) - 1)
    for i, note in enumerate(gamme):
        g = 0.55 + 0.45 * i / (len(gamme) - 1)  # crescendo léger vers le haut
        ajoute(x, harpe(hz(note), 0.45, 0.4), i * pas, g)
    souffle = bruit(d, rng)
    ouverture = glisse(300, 3000, d)
    souffle = passe_bas_variable(souffle, ouverture, q=1.2)
    t = temps(d)
    env = np.sin(np.pi * np.clip(t / 0.85, 0, 1)) ** 1.5  # monte puis retombe
    x = x + 0.35 * souffle * env
    x = reverb(x, 0.12, 0.6)
    return finalise(x, d, 0.004, 0.05)


def ui_nouvelle_partie():
    """On redescend, sans tristesse : 1 octave de fa majeur vers le bas en 0,3 s, harpe douce."""
    d = 0.5
    x = silence(d)
    gamme = ["F5", "E5", "D5", "C5", "Bb4", "A4", "G4", "F4"]
    pas = 0.3 / (len(gamme) - 1)
    for i, note in enumerate(gamme):
        ajoute(x, harpe(hz(note), 0.35, 0.3, eclat=0.6), i * pas, 0.8)
    x = reverb(x, 0.1, 0.5)
    return finalise(x, d, 0.004, 0.04)


def ui_boutique(montant=True):
    """Clochette d'entrée : sol5 -> do6 (ouverture) ou do6 -> sol5 (fermeture)."""
    notes = ["G5", "C6"] if montant else ["C6", "G5"]
    d = 0.3 if montant else 0.25
    x = silence(d)
    ajoute(x, clochette(hz(notes[0]), 0.2, 0.15), 0.0, 0.8)
    ajoute(x, clochette(hz(notes[1]), d - 0.08, 0.18), 0.08, 1.0)
    return finalise(x, d, 0.002, 0.02)


def vente(rng):
    """
    Vente d'un doublon (boîtes à tirage) : une petite cascade de pièces qui tombent
    l'une sur l'autre, puis le « pouf » feutré de la bourse qui se referme.

    v3 (correctif B3) : 0,4 s au lieu de 0,5 s. Les 6 pièces tombent en 0,15 s (au lieu
    de 0,2) et la bourse se ferme à 0,18 s ; les queues sont raccourcies pour que tout
    soit éteint avant la fin du fichier.
    """
    d = 0.4
    x = silence(d)
    instants = [0.0, 0.04, 0.075, 0.105, 0.13, 0.15]
    for i, t0 in enumerate(instants):
        f = rng.uniform(2600, 4200) * (1.0 - 0.04 * i)  # les pièces tombent de plus en plus bas
        ajoute(x, ting(f, 0.11, 0.08), t0, 1.0 - 0.08 * i)
    # la bourse en tissu : bruit très sourd et bref
    b = filtre(bruit(0.12, rng), "passe_bas", 600, ordre=2) * perc(0.12, 0.004, 0.07)
    ajoute(x, b, 0.18, 2.5)
    ajoute(x, sinus(glisse(160, 90, 0.1), 0.1) * perc(0.1, 0.003, 0.07), 0.18, 0.5)
    x = reverb(x, 0.08, 0.3)
    # cible « oreille » -14,5 : le même volume ressenti que la version de 0,5 s
    return finalise(x, d, 0.002, 0.005, cible_oreille=-14.5)


def _ecrivain(seulement):
    """Rend une fonction « écrire » qui ignore les sons absents de `seulement` (None = tous)."""
    def ecris(categorie, nom, x):
        if seulement is None or nom in seulement:
            ecris_wav(categorie, nom, x)
    return ecris


def genere(seulement=None):
    """
    Calcule tous les sons du fichier (dans l'ordre : ils partagent le même tirage au
    hasard) mais n'écrit que ceux de `seulement` si on le donne (ex. {"vente"}).
    """
    ecris = _ecrivain(seulement)
    rng = np.random.default_rng(101)
    print("Sons ui :")
    ecris("ui", "ui_survol", ui_survol())
    ecris("ui", "ui_clic", ui_clic(rng))
    ecris("ui", "ui_jouer", ui_jouer())
    ecris("ui", "ui_etage_monte", ui_etage_monte(rng))
    ecris("ui", "ui_nouvelle_partie", ui_nouvelle_partie())
    ecris("ui", "ui_boutique_ouvre", ui_boutique(True))
    ecris("ui", "ui_boutique_ferme", ui_boutique(False))
    ecris("ui", "vente", vente(rng))


if __name__ == "__main__":
    genere()
