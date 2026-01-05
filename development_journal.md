# Journal de Développement du Jeu

Ce document retrace l'historique complet du développement, étape par étape, depuis la conception du système de terrain jusqu'aux dernières finitions narratives et visuelles.

## 1. Fondation : Système de TerrainManager et Chunks
*(Auteur : Thibault Livran)*

La première et la plus importante étape du projet a été la mise en place d'un monde infini et performant.

### Comment ça marche ?
Le système repose sur une génération procédurale par "Chunks" (morceaux de terrain) gérée dynamiquement autour du joueur.

*   **Génération par Chunks (`LowPolyDesertChunk`)** : [LowPolyDesertChunk.cs](Assets/scripts/desert/LowPolyDesertChunk.cs)
    *   Le monde n'est pas créé en une seule fois. Il est divisé en parcelles carrées qui s'activent ou se désactivent selon la position du joueur.
    *   Chaque chunk génère son propre maillage (Mesh) Low Poly à la volée.
*   **Algorithme de Biomes** :
    *   Initialement basé sur un bruit de Perlin simple, le système utilise désormais un mélange de **Sine Wave** (pour des dunes douces) et de **FBM** (pour les collines).
    *   Le terrain interprète des fonctions mathématiques pour placer des **Montagnes** (en bordure de carte), des **Oasis**, ou des **Cratères**.
*   **Monde Infini & Floating Origin** : [FloatingOrigin.cs](Assets/scripts/desert/FloatingOrigin.cs)
    *   Pour éviter les erreurs de précision (jittering) typiques des moteurs 3D quand on s'éloigne trop de l'origine (0,0,0), un système de **Floating Origin** a été implémenté.
    *   Lorsque le joueur parcourt plus de 5km, tout le monde (Joueur + Terrain) est téléporté instantanément à l'origine (0,0,0) de manière invisible. Cela permet une course théoriquement infinie.

---

## 2. Avancement des Fonctionnalités


### A. Mise à jour "War Mode" (Ambiance Guerre)
*(Auteur : Alois Fournier)*

L'objectif était de transformer le désert vide en un champ de bataille abandonné.
*   **Génération de Débris** : [LowPolyDesertChunk.cs](Assets/scripts/desert/LowPolyDesertChunk.cs)
    *   Dispersion procédurale de cubes physiques (briques, débris) autour des bâtiments pour simuler la destruction.
*   **Système de Fumée** :
    *   Développement de particules "Voxels" (cubiques) gris/transparents pour simuler des incendies récents, remplaçant les particules rondes classiques pour coller au style Low Poly.

### B. Système de Tempête de Sable Volumétrique
*(Auteur : Thibault Livran)*

Pour densifier l'atmosphère et cacher les limites du monde.
*   **Shader Volumétrique** : [VolumetricSand.shader](Assets/shaders/VolumetricSand.shader)
    *   Utilisation du *Ray Marching* dans un shader custom pour créer un brouillard épais et en 3D, pas juste une image plate.
*   **Progression** : [DesertHeatSystem.cs](Assets/scripts/desert/DesertHeatSystem.cs)
    *   La tempête est liée au `DesertHeatSystem`. Plus le joueur avance et souffre de la chaleur, plus la tempête devient opaque (courbe exponentielle).

### C. Système Audio Immersif
*(Auteur : Alois Fournier)*
*   **Feu Intelligent** : [LowPolyDesertChunk.cs](Assets/scripts/desert/LowPolyDesertChunk.cs)
    *   Chaque bâtiment brûlé émet un son spatialisé 3D. Les bâtiments intacts restent silencieux.
*   **Vent Dynamique** : [DesertHeatSystem.cs](Assets/scripts/desert/DesertHeatSystem.cs)
    *   Le bruit du vent change en temps réel, passant d'une brise légère à une tempête violente au fur et à mesure de la progression du joueur.

### D. Mise en Scène & Cinématiques
*(Auteur : Thibault Livran)*
*   **Introduction (Zoom)** : [DesertCameraSetup.cs](Assets/scripts/desert/DesertCameraSetup.cs)
    *   Au lancement, la caméra effectue un travelling arrière fluide (Zoom out) depuis le fond de la scène, posant une ambiance cinématographique.
*   **Storytelling (Yarn Spinner)** : [DesertHeatSystem.cs](Assets/scripts/desert/DesertHeatSystem.cs)
    *   Intégration d'un système de dialogue. Bob discute avec la voix mystérieuse ("????"). Les dialogues se déclenchent automatiquement à 25%, 50%, 75% du trajet.

### E. Séquence "Drug Trip" & Fin
*(Auteur : Alois Fournier)*
*   **Transition Psychédélique** : [DrugTripSequence.cs](Assets/scripts/UI/DrugTripSequence.cs) / [DesertEntryFade.cs](Assets/scripts/UI/DesertEntryFade.cs)
    *   À la fin du niveau précédent, le joueur subit une hallucination. Une lueur jaune envahit l'écran, suivie d'une transition fluide vers le désert.
*   **Révélation Finale** : [DesertHeatSystem.cs](Assets/scripts/desert/DesertHeatSystem.cs)
    *   Une fois le joueur évanoui, un écran noir laisse place à un monologue du "Docteur", révélant la nature traumatique de l'expérience.
*   **Battement de Cœur** :
    *   Un son de cœur qui accélère synchronisé avec des flashs lumineux conclut l'expérience avant le retour au menu.

---

## 🐛 3. Registre des Bugs et Correctifs

Liste exhaustive des problèmes techniques rencontrés et de leurs solutions.

### Graphismes & Rendu
*   **Brouillard Invisible (ZTest)** : [VolumetricSand.shader](Assets/shaders/VolumetricSand.shader)
    *   Le sable volumétrique disparaissait derrière les dunes.
    *   *Correction* : Activation du `ZTest Always` et calcul manuel de la profondeur.
*   **Ciel Transparent** :
    *   Le haut de la tempête laissait voir le ciel bleu.
    *   *Correction* : Suppression du dégradé vertical dans le shader.
*   **Fumée Noire/Marron** : [LowPolyDesertChunk.cs](Assets/scripts/desert/LowPolyDesertChunk.cs)
    *   Les particules de fumée réagissaient mal à la lumière du soleil.
    *   *Correction* : Passage à un shader `Unlit` (sans éclairage) et forçage des modes de fusion pour la transparence.
*   **Texture Mur Invisible** : [BatimentVoxelProcedural.shader](Assets/shaders/BatimentVoxelProcedural.shader)
    *   Le crépi n'apparaissait pas sur les murs.
    *   *Correction* : Utilisation du **Triplanar Mapping** (projection basée sur la position monde) car les UVs étaient absents.

### Gameplay & Physique
*   **Caméra qui saute** : [CinemachineGroundLift.cs](Assets/scripts/desert/CinemachineGroundLift.cs)
    *   La caméra zoomait violemment au contact du sol.
    *   *Correction* : Remplacement du Raycast par un **SphereCast** qui "roule" sur le terrain, et ajout du script `CinemachineGroundLift`.
*   **Pics Infinis** : [LowPolyDesertChunk.cs](Assets/scripts/desert/LowPolyDesertChunk.cs)
    *   Le générateur de terrain créait des murs verticaux aberrants.
    *   *Correction* : Réécriture de l'algorithme de bruit (Ridge Noise) et ajout d'une zone de lissage.
*   **Superposition de Bâtiments** :
    *   Les maisons apparaissaient les unes sur les autres.
    *   *Correction* : Vérification de distance (15m) avant chaque placement.

### Audio
*   **Son "Roborique" (Reverb)** : [LowPolyDesertChunk.cs](Assets/scripts/desert/LowPolyDesertChunk.cs)
    *   Les feux avaient un écho étrange.
    *   *Correction* : Désactivation de l'effet Doppler et randomisation du pitch.
*   **Feu Infini** :
    *   On entendait le feu à 5km.
    *   *Correction* : Passage au `Linear Rolloff` pour couper le son net à 80m.

### Système & Outils
*   **Git LFS** :
    *   Impossible d'envoyer les fichiers `.unity` trop lourds.
    *   *Correction* : Installation de Git Large File Storage.
*   **Mort Prématurée** : [DesertHeatSystem.cs](Assets/scripts/desert/DesertHeatSystem.cs)
    *   Le joueur s'évanouissait pendant qu'un personnage parlait.
    *   *Correction* : Le système attend désormais obligatoirement la fin du dialogue pour lancer la séquence de fin.
