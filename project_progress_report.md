# 📋 Rapport d'Avancement Global du Projet

Ce document récapitule l'ensemble des étapes récentes du projet, incluant l'historique des problèmes résolus (Caméra, Terrain) et les dernières fonctionnalités ajoutées ("War Mode").

---

## 📜 1. Historique & Contexte Précédent

### Amélioration de la Caméra (Cinemachine) 🎥
*   **Problème** : La caméra "sautait" ou zoomait violemment vers le joueur lorsqu'elle touchait le sol (comportement par défaut "Pull Camera Forward" de Unity).
*   **Solution Implémentée (`CinemachineGroundLift`)** :
    *   Création d'un script personnalisé pour **soulever** doucement la caméra au lieu de la faire avancer.
    *   **Problème de Rebond** : La première version utilisait un *Raycast* (rayon laser), qui tombait dans les micro-trous entre les triangles du terrain Low Poly, créant des vibrations.
    *   **Correction Finale** : Passage à un **SphereCast** (Sphère invisible). La caméra "roule" maintenant sur les aspérités du terrain comme une roue, garantissant une fluidité totale et une distance minimale (~50cm) avec le sable.

### Stabilisation du Terrain (Phase Initiale) 🏜️
*   **Bug Critique "Pics Infinis"** : Lors des premiers tests, le générateur de terrain créait parfois des murs verticaux ou des pics de hauteur aberrante.
*   **Diagnostic** : Problème identifié dans l'algorithme de génération du biome "Montagne" (bruit mal calibré).
*   **Action Temporaire** : Désactivation complète du biome Montagne dans `LowPolyDesertChunk.cs` pour permettre de tester le reste du jeu sans bugs, le temps de développer une solution robuste (voir section 3).

### Prefabs & Matériaux 🏠
*   **Recherche Visuelle** : Comparaison entre des flammes animées dans Blender vs Unity. Choix des **particules Unity** pour leur dynamisme interactif.
*   **Matériaux** : Création de textures personnalisées (ex: `SandWall`) pour uniformiser le style des bâtiments avec le désert.

---

## 🌪️ 2. Mise à jour "War Mode" (Session Actuelle)

### Amélioration de l'Atmosphère
*   **Génération de Débris** :
    *   Ajout de la méthode `GenerateDebris` pour disperser des cubes physiques autour des bâtiments.
    *   Ajustement des couleurs (Sable/Brique) et enfoncement dans le sol pour le réalisme.
*   **Système de Fumée (Incendies)** :
    *   Objectif : Simuler des dégâts récents.
    *   **Évolution** : Passage de particules "Bulles Noires" (trop rondes) à des **particules Voxels (Cubes)**.
    *   Résultat : Nuages de fumée cubiques, mats, gris/transparents, correspondant parfaitement à la direction artistique Low Poly.

### Limites de la Carte & Retour des Montagnes 🏔️
*   **Frontières Naturelles** :
    *   Remplacement des murs invisibles par des chaînes de montagnes infranchissables.
    *   Implémentation d'une `mapWidth` : Au-delà de cette limite, le biome est forcé en `Mountain`.
*   **Correction Définitive des Montagnes** :
    *   Réécriture complète de l'algorithme de hauteur (`GetHeightForBiome`).
    *   Utilisation d'un **Ridge Noise** (bruit de crête) pour des sommets pointus réalistes.
    *   Ajout d'une zone de **lissage (Lerp)** de 150m pour éviter les transitions brutales (murs verticaux) avec le désert.

---

## 🛠️ 3. Correctifs Techniques Récents

### Anti-Superposition des Bâtiments
*   **Problème** : Bâtiments générés l'un sur l'autre.
*   **Fix** : Ajout d'une vérification de distance (15m) dans `GenerateBuildings` avant chaque placement.

### Shader des Murs (Texture Invisible)
*   **Problème** : La texture de crépi sur les murs `SandWall` n'apparaissait pas.
*   **Cause** : Les meshs procéduraux (Voxels) n'ont pas de coordonnées UV valides pour "coller" l'image.
*   **Solution (Triplanar Mapping)** :
    *   Mise à jour du shader `BatimentVoxelProcedural`.
    *   Projection de la texture en fonction de la **Position Monde** (World Position) au lieu des UVs.
    *   Ajout d'un slider `_TextureScale` pour régler la taille du grain de crépi directement dans l'éditeur.

---

## ✅ État Actuel du Projet
Le jeu dispose maintenant d'un environnement "War Zone" cohérent, avec un terrain stable, borné naturellement, et une atmosphère immersive (fumée, débris, brouillard) sans bugs visuels majeurs.

---

## 🌪️ 4. Système de Tempête de Sable Volumétrique (Nouveau)

### Implémentation "Ray Marching" 🕶️
*   **Technique** : Création d'un **Custom Shader** (`VolumetricSand.shader`) utilisant le *Ray Marching* (lancer de rayons) pour simuler un volume 3D épais, au lieu d'une simple texture 2D.
*   **Bruit Procédural** : Utilisation d'un bruit de Perlin dynamique pour animer les "vagues" de sable et simuler la turbulence du vent.

### Intégration & Gameplay (`DesertHeatSystem.cs`) 🎮
*   **Suivi du Joueur** : Le cube de volume est scripté pour suivre *exactement* la position du joueur, créant l'illusion d'une tempête infinie tout en optimisant les performances (seule la zone autour du joueur est calculée).
*   **Progression Dramatique** :
    *   **Courbe Exponentielle** : La densité du sable suit désormais une courbe cubique (`t*t*t`). Le début du voyage reste clair, mais la fin devient un "mur" impénétrable juste avant l'évanouissement.
    *   **Paramètres** : Densité Max augmentée à 30 (contre 5 initialement) pour garantir une opacité totale.

### Correctifs Visuels Uniques 🛠️
*   **Occlusion du Terrain (`ZTest`)** :
    *   *Problème* : Le sable disparaissait quand le cube passait derrière les dunes.
    *   *Solution* : Activation du `ZTest Always` + calcul manuel de la profondeur (`_CameraDepthTexture`). Le brouillard s'arrête désormais *exactement* à la surface du sable, sans passer à travers le sol.
*   **Sable dans le Ciel** :
    *   *Problème* : Le haut du cube était transparent, laissant voir le ciel bleu au milieu de la tempête.
    *   *Solution* : Suppression du "Height Falloff" (dégradé vertical) dans le shader. Le mur de sable monte maintenant jusqu'au ciel pour cacher le soleil.
*   **Synchronisation du Mouvment** : Correction de l'effet de glissement où le sable "suivait" le cube. Le bruit est maintenant ancré dans l'espace monde (`World Space`), donc traverser le sable donne une vraie sensation de vitesse.

---

## 🏜️ 5. Refonte du Désert & Génération Infinie (Session Actuelle)

### Esthétique des Dunes & Biomes 🎨
*   **Nouvel Algorithme de Terrain** :
    *   Abandon du bruit de Perlin classique pour un mélange **Sine Wave (Vagues)** et **FBM (Collines)**.
    *   Résultat : Des dunes plus douces, organiques et esthétiques (« Rolling Hills »), brisant l'aspect "chaos de pixel" précédent.
*   **Diversité des Biomes** :
    *   Intégration de variations : **Oasis** (dépressions plates), **Cratères**, et **Montagnes** lointaines.
    *   Ajout de **Rochers Procéduraux** dispersés naturellement pour casser la monotonie.
*   **Coloration Dynamique** :
    *   Application d'un **Gradient de Hauteur** (Vallées sombres, Crêtes claires) pour accentuer le relief sans texturing lourd.

### Brouillard & Immersion ("Desert Fog") 🌫️
*   **Problème** : Les chunks lointains apparaissaient brutalement ("pop-in"), brisant l'immersion.
*   **Solution Implémentée** :
    *   Création d'un **Shader Custom** (`DesertDistanceFade`) qui fond la couleur du sol avec celle du ciel à distance.
    *   Contrôleur intelligent (`DesertFogController`) qui ajuste la densité du brouillard pour laisser visible la **Porte Géante** (Objectif final) tout en cachant la fin du monde.

### Caméra "Cinematique" 🎥
*   Installation de **Cinemachine** configurée pour :
    *   Suivre le joueur avec fluidité (Damping).
    *   Regarder en permanence vers l'objectif (La Porte).
    *   Ignorer les mouvements brusques du terrain pour éviter le mal de mer.

### Architecture "Monde Infini" (Technique) ♾️
Pour permettre au joueur de courir éternellement sans bugs :
1.  **Floating Origin (Origine Flottante)** :
    *   Système qui détecte quand le joueur s'éloigne trop (> 5000m).
    *   **Téléporte** instantanément tout le monde (Joueur + Terrain) vers l'origine (0,0,0) de manière invisible.
    *   Permet une exploration infinie sans jamais dépasser les limites de précision d'Unity.
2.  **Double Precision Math** :
    *   Refonte complète des calculs de position en **Double (64-bit)** pour garantir que les chunks s'alignent au millimètre près, même après 100km de course.
    *   Plus aucun trou (Seam) entre les morceaux de terrain.
