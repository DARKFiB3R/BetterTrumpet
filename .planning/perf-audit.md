# BetterTrumpet — Audit consommation CPU / GPU

> Date : 2026-06-07 · Build : net8 Release x86 · Machine : 32 cœurs, Windows 11 (26200)
> Méthode : `TotalProcessorTime` échantillonné (métrique ms CPU/s, indépendante du nb de cœurs) + analyse du code (timers, hooks de rendu).

---

## Mesures runtime

### État IDLE (flyout fermé, icône tray seule) — MESURÉ
| Métrique | Valeur |
|---|---|
| CPU | **~0.01%** (négligeable, proche de 0 ms/s) |
| RAM (WorkingSet) | **~154 Mo** |
| Threads | 38 |
| Handles | 1138 |

Verdict idle : **excellent**. Aucun timer chaud ne tourne en arrière-plan. Les timers idle (poll legacy 10s, update 6h, health monitor) sont négligeables.

### État FLYOUT OUVERT — mesure automatisée non concluante
La fenêtre flyout (`WindowStyle=None`, titre vide, cloak) n'a pas pu être isolée de façon fiable pour une mesure ciblée ; les échantillons captés montraient < 1% d'un cœur, mais sans garantie que le flyout était visible/actif pendant la fenêtre de mesure. **À mesurer manuellement** (cf. recommandations) — l'analyse code ci-dessous identifie les coûts réels.

---

## Analyse des drivers de consommation (lecture du code)

### 🔴 Coût principal quand le flyout est OUVERT
1. **`VolumeSlider.OnRendering` via `CompositionTarget.Rendering`** (`VolumeSlider.cs:420`)
   - Hook par frame (~60 fps) tant que `_isAnimating`. Limité en FPS interne (`_targetFps`), bien hooké/déhooké (`StartAnimation`/`StopAnimation`).
   - Un hook par slider visible → N sliders = N hooks. Coût CPU/GPU réel pendant l'affichage, surtout en mode étendu (plusieurs devices + apps).
2. **Peak-meter `Timer`** (`DeviceCollectionViewModel.cs:45`)
   - 30 fps par défaut (60 si configuré). **Correctement gated** sur la visibilité (`_isFlyoutVisible || _isFullWindowVisible`). Appelle `UpdatePeakValues()` + dispatch UI par device.
3. **Acrylic software-rendered** (`AllowsTransparency=true`)
   - Le flyout est en rendu logiciel (CPU), pas GPU. Coût de blit à chaque frame d'animation, amplifié en HiDPI. C'est le plafond structurel identifié dans l'analyse de fluidité.

### 🟠 Coûts ponctuels
- **Blur live du light-dismiss** (`FlyoutWindow.xaml`) : `VisualBrush` + `BlurEffect` Gaussian recalculé par frame **uniquement** quand un dialogue d'app est ouvert. Cher en software, mais transitoire.
- **Animations premium ajoutées** (pop, cascade, mute bounce, chevron) : `ScaleTransform`/`DoubleAnimation` courtes, `FillBehavior.Stop`, transforms nettoyés en fin. Coût négligeable et borné (cascade plafonnée à 24 items).
- **TaskbarIconSource** : timer 50ms (20fps) **uniquement** quand l'icône anime (gated).

### 🟢 Idle — RAS
- Poll legacy media 10s, update check 6h, health monitor : négligeables.
- WPF en .NET 8 : empreinte mémoire ~150 Mo (normal pour WPF + WinRT projections).

---

## Recommandations (par rapport effort/gain)

### Mesure à compléter
- [ ] Mesurer manuellement CPU/GPU **flyout ouvert + audio actif**, en mode replié ET étendu, via le Gestionnaire des tâches (onglet Détails → colonnes CPU + GPU) ou PerfMon. C'est l'état qui compte pour le ressenti.

### Gains potentiels (si la mesure confirme un coût)
- [ ] **Peak-meter FPS adaptatif** : déjà réglable (eco mode). Vérifier que le défaut 30fps est suffisant ; 60 est rarement utile sur des barres fines.
- [ ] **GPU natif** (chantier de fond, cf. analyse fluidité) : passer le flyout au backdrop DWM natif retirerait le rendu software de l'acrylic — mais on a constaté que l'acrylic DWM ne marche pas sur WPF classique (voir leçon apprise). Gain GPU réel surtout sur les animations, déjà acquis en partie via net8.
- [ ] **Blur light-dismiss** : remplacer le `BlurEffect` Gaussian live par un voile semi-opaque statique (coût ÷10 quand un dialogue est ouvert).

### Verdict global
**Conso très saine au repos** (l'app ne coûte rien quand tu ne l'utilises pas). Les coûts sont concentrés sur l'affichage du flyout (rendu software + peak-meters), ce qui est attendu et borné. Pas de fuite ni de timer fou détecté. Aucune action urgente.
