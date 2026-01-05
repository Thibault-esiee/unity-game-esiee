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

## 🌪️ 2. Mise à jour "War Mode"

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

## 🛠️ 3. Correctifs Techniques

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

## 🌪️ 4. Système de Tempête de Sable Volumétrique (Nouveau)

### Implémentation "Ray Marching" 🕶️
*   **Technique** : Création d'un **Custom Shader** (`VolumetricSand.shader`) utilisant le *Ray Marching* (lancer de rayons) pour simuler un volume 3D épais, au lieu d'une simple texture 2D.
*   **Bruit Procédural** : Utilisation d'un bruit de Perlin dynamique pour animer les "vagues" de sable et simuler la turbulence du vent.

### Intégration & Gameplay (`DesertHeatSystem.cs`) 🎮
*   **Suivi du Joueur** : Le cube de volume est scripté pour suivre *exactement* la position du joueur, créant l'illusion d'une tempête infinie tout en optimisant les performances (seule la zone autour du joueur est calculée).
*   **Progression Dramatique** :
    *   **Courbe Exponentielle** : La densité du sable suit désormais une courbe cubique (`t*t*t`). Le début du voyage reste clair, mais la fin devient un "mur" impénétrable juste avant l'évanouissement.
    *   **Paramètres** : Densité Max augmentée à 30 (contre 5 initialement) pour garantir que l'opacité totale est atteinte.

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
    *   Refonte complète des calculs de position en **Double (64-bit)** pour garantir que les chunks s'alignent au millimètre près, même après 100km de course.
    *   Plus aucun trou (Seam) entre les morceaux de terrain.

---

## 💊 6. Séquence de Fin de Niveau "Drug Trip" (Session Actuelle)

### Résolution des Problèmes Git LFS 📦
*   **Problème Critique** : Impossible de push les modifications à cause de fichiers `.unity` dépassant 100MB (`desert.unity`).
*   **Solution** :
    *   Installation et configuration de **Git LFS** (Large File Storage).
    *   Réécriture de l'historique local pour tracker les fichiers `.unity`.
    *   Push réussi des modifications lourdes.

### Implémentation Audio-Visuelle (`DrugTripSequence.cs`) 🌈
*   **Séquence Scénarisée** : À la fin du dialogue "Ending", au lieu d'une coupure brutale :
    1.  Son de "déglutition" (Swallow) joué.
    2.  Attente paramétrable (2.0s).
    3.  Apparition progressive d'une lueur jaune occupant tout l'écran.
    4.  Chargement de la scène suivante (`desert`).
*   **Génération Procédurale** :
    *   Pour éviter les dépendances de fichiers (Sprites manquants), le script **génère automaiquement la texture de glow** par code au démarrage (Texture2D dynamique).
    *   Création autonome du Canvas et de l'Image si absents.

### Transition Fluide (`DesertEntryFade.cs`) ☀️
*   **Continuité Visuelle** :
    *   Au chargement de la scène `desert`, un script créé une image jaune (même code couleur exact R=1, G=0.9, B=0.2).
    *   Disparition progressive (Fade Out) pour révéler le désert, créant une transition invisible depuis le fondu au jaune précédent.

### Outils de Debug ⌨️
*   **Skip Dialogue** : Ajout d'une fonctionnalité dans `StartDialogueTrigger` pour passer instantanément un dialogue en appuyant sur la touche **'K'**, facilitant les tests itératifs.

---

## 🔥 7. Système Audio Immersif (Session Actuelle)

### Son du Feu Intelligent 🔊
*   **Implémentation** : Ajout d'une source audio (`AudioSource`) sur les bâtiments générés pour simuler un incendie.
*   **Système d'Exclusion** :
    *   Logique permettant de définir des **Exceptions** : Les bâtiments intacts (`Building_1`, `2`, `8`, `9`) restent silencieux pour plus de réalisme.
    *   Seuls les bâtiments visuellement "détruits" ou en feu émettent du son.

### Réglages Audio (Bug Fix & Tuning) 🛠️
*   **Problème de "Reverb" (Phasing)** :
    *   *Symptôme* : Le son semblait avoir un écho robotique étrange.
    *   *Cause* : Effet Doppler (changement de pitch avec le mouvement) + Sons identiques joués en même temps.
    *   *Fix* : Désactivation du Doppler + Randomisation du Pitch (0.8 - 1.2) pour chaque feu.
*   **Problème de Distance (Son infini)** :
    *   *Symptôme* : Le feu s'entendait même à l'autre bout de la carte.
    *   *Fix* : Passage du mode *Logarithmic* à **Linear Rolloff**. Le volume atteint désormais le **silence absolu (0.0)** à 80 mètres.
*   **Problème Éditeur** :
    *   *Symptôme* : Le son se lançait en boucle dans la vue Scène de Unity (car le script est `[ExecuteAlways]`).
    *   *Fix* : Ajout d'une protection `if (Application.isPlaying)` pour ne générer l'audio qu'en mode Jeu.

---

## 🌬️ 8. Améliorations de la Chaleur & des Particules (Session Actuelle)

### Système de Vent Dynamique 🎐
*   **Objectif** : Renforcer l'immersion sonore lors de la traversée du désert.
*   **Implémentation (`DesertHeatSystem`)** :
    *   Transition progressive entre deux boucles audio : `Light Wind` (brise légère) et `Strong Wind` (tempête).
    *   Le volume croît en fonction de la distance parcourue (`t`), passant du calme à la tempête juste avant l'évanouissement.

### Effets de "Détresse" Visuelle (Heat Distress) 😵
*   **Objectif** : Simuler la fatigue et la chaleur extrême sur le joueur.
*   **Brouillard (Fog)** :
    *   Augmentation exponentielle de la densité (courbe cubique) pour boucher la vue progressivement.
*   **Flou (Depth of Field)** :
    *   Activation du *Post-Processing* URP.
    *   Le flou focal arrive plus tôt (courbe quadratique) pour désorienter le joueur, jusqu'à devenir presque aveuglant à la fin.

### Tuning des Particules de Fumée 💨
*   **Problème** : La fumée des bâtiments était trop agressive (cubes noirs opaques tournant vite).
*   **Ajustements Esthétiques (`LowPolyDesertChunk`)** :
    *   **Rotation** : Ralentissement drastique (180° -> 45°) pour un effet de dérive lente et calme.
    *   **Couleur & Opacité** : Passage d'un noir pur à un gris moyen (`0.8`) avec une transparence élevée (`0.45`).
*   **Correctif Technique (URP Shader)** :
    *   *Bug* : La fumée apparaissait parfois "Marron" (teintée par le soleil jaune) ou "Noire Solide" (accumulation opaque).
    *   *Fix* :
        1.  Passage au shader **URP Unlit** pour ignorer la lumière du soleil (couleur stable).
        2.  Forçage manuel des modes de fusion (**Blend Modes**) dans le code pour garantir la transparence même lors de la génération procédurale.

---

## 🎥 9. Mise en Scène Introduction (Session Actuelle)

### Zoom Dynamique au Démarrage
*   **Objectif** : Créer un plan rapproché dramatique sur le personnage au début de la scène désert.
*   **Implémentation (`DesertCameraSetup`)** :
    *   **Séquence Scénarisée** :
        1.  La caméra commence en **Gros Plan** (Zoom) sur le joueur (Distance réduite à 5m).
        2.  **Maintien** de la pose pendant 2 secondes pour installer l'ambiance.
        3.  **Travelling Arrière** fluide (Transition de 2s) pour retrouver la distance de jeu normale (15m).
    *   **Technique** : Utilisation d'une Coroutine pour interpoler dynamiquement les paramètres du `CinemachineTransposer` (Distance et Hauteur) sans coupure.

---

## 💬 10. Système de Dialogue & Fin de Séquence (Session Actuelle)

### Intégration de Yarn Spinner 🧶
*   **Narrative Design** :
    *   Implémentation d'une série de **5 dialogues** progressifs (`Desert.yarn`) entre Bob et une entité inconnue ("????").
    *   **Thèmes Psychologiques** : Évolution du déni vers la culpabilité. Bob pense être un agent secret, mais le "Docteur" essaie de lui faire accepter la réalité de ses crimes de guerre.

### Logique de Déclenchement (`DesertHeatSystem.cs`) ⚙️
*   **Triggers de Distance** :
    *   Les dialogues se lancent automatiquement à des pourcentages précis de la marche (5%, 25%, 50%, 75%, 90%) pour rythmer la traversée du désert.
    *   **Auto-Configuration** : Le script détecte et configure automatiquement le `DialogueRunner` et les événements au démarrage, évitant les oublis de setup dans l'Inspecteur.

### Refonte Mécanique "Faint" (Évanouissement) 😵
*   **Synchronisation Narrative** :
    *   *Problème* : Le joueur pouvait s'évanouir (Game Over) en plein milieu d'une phrase.
    *   *Solution* : Le système attend désormais **obligatoirement** la fin du dernier dialogue (`Desert_5`) avant d'autoriser l'évanouissement, même si la distance maximale est dépassée.
*   **Tempo Dramatique** :
    *   Ajout d'un **Délai de 2 secondes** après la dernière réplique.
    *   Permet un moment de silence et de réalisation pour le joueur avant la transition finale.

### Amélioration du Flou (Blur) 📸
*   **Problème de "Blur Invisible"** :
    *   Le flou cinétique n'était pas assez fort ou ne s'activait pas correctement à cause de conflits de Volume Profile.
*   **Solutions Techniques** :
    1.  **Ouverture (Aperture)** : Forçage du paramètre d'ouverture à **f/1.4** par code pour garantir une profondeur de champ très courte (beaucoup de flou).
    2.  **Distance Focale** :
        *   Passage de `minBlurFocusDistance = 2.0` à **`0.1`**.
        *   Résultat : À la fin, la caméra fait le point à 10cm de son objectif, rendant le monde entier (même proche) extrêmement flou et onirique.
    3.  **Start Dynamique** : Le script capture désormais la valeur de flou actuelle au démarrage (au lieu de 10 par défaut) pour une transition fluide sans saut visuel.

### Séquence Finale "Révélation" 🎬
*   **Dialogue de Fin (`Desert_Ending`)** :
    *   **Narration** : Une fois le joueur évanoui, l'écran reste noir. Le "Docteur" apparaît et brise le quatrième mur en révélant que la mission d'agent secret est un délire traumatique de Bob pour échapper à ses souvenirs de crimes de guerre.
    *   **Ambiance** : Bob ne répond plus ("..."), marquant son état de choc/catatonie.
*   **Gestion de la Mort (`PlayerController.cs`)** :
    *   Modification de la méthode `Die()` pour accepter un paramètre `reloadScene`.
    *   Permet de déclencher l'animation de mort et le fondu au noir **sans recharger la scène**, laissant la place au monologue final sur fond noir.
*   **Contrôle Audio (`DesertHeatSystem.cs`)** :
    *   Implémentation d'une Coroutine `FadeAllAudio` qui coupe progressivement tous les sons (Vent, Feu, Pas) pendant le fondu au noir.
    *   Garantit un silence total pour donner plus d'impact aux paroles du Docteur.

### Chargement Dynamique des Portraits (`PortraitManager.cs`) 🖼️
*   **Problème** : Devoir assigner manuellement chaque sprite de personnage dans l'Inspecteur était fastidieux et source d'erreurs.
*   **Solution** :
    *   Ajout d'un système de **Fallback Resources**.
    *   Si un portrait n'est pas trouvé dans le dictionnaire manuel, le script cherche automatiquement dans le dossier `Resources/` (ou `Resources/Portraits/`) avec le nom du personnage.
    *   Simplifie grandement le workflow : il suffit de nommer le fichier image comme le personnage dans le Yarn script.

---

## 🌓 11. Finalisation de la Séquence Désert & Menu (Session Actuelle)

### Séquence de Fin "Onirique" (`DesertHeatSystem.cs`) ✨
*   **Animation de Lumière** :
    *   À la fin du dialogue "Ending", une lueur blanche apparaît au centre de l'écran noir.
    *   **Effet "Sursauts"** : La lumière palpite (fade in/out) et grandit par à-coups, imitant un battement cardiaque visuel ou une porte qui s'ouvre, avant de replonger le joueur dans le noir complet pour la transition.
    *   **Génération Procédurale** : Utilisation d'un sprite "Soft Circle" généré par code pour un rendu doux et organique, remplaçant les carrés blancs initiaux.

### Immersion Audio 💓
*   **Battement de Cœur** :
    *   Intégration du son `heart_beat.mp3`.
    *   **Synchronisation** : Le son commence doucement (50%) pendant le dialogue final, puis accélère et monte en intensité (Pitch 1.0 -> 1.4, Volume -> 100%) en rythme avec les flashs lumineux, créant une tension maximale.

### Transitions & Menu UI 🖥️
*   **Fade-In Menu (`MenuUI.cs`)** :
    *   Au lancement de la scène Menu, l'écran passe du noir au clair progressivement (2s).
    *   La musique du menu suit la même courbe (Fade In), pour une entrée en matière douce.
*   **Continuité** : La transition Désert -> Menu se fait par un "cut" au noir suivi de ce fade-in, assurant une fluidité cinématographique entre les scènes.
