# BetterTrumpet — Visual Validation Guide
## Windows 11 Fluent Design Migration

**Date:** 2026-06-08  
**Purpose:** Guide visuel pour valider la migration Fluent Design

---

## 🎨 Ce qui a Changé Visuellement

### Avant/Après — Résumé

| Aspect | AVANT | APRÈS |
|--------|-------|-------|
| **Couleurs** | Hardcodées (bleu #3B9EFF fixe) | Dynamiques (SystemAccent de l'utilisateur) |
| **Thème** | Dark uniquement | Light + Dark adaptatif |
| **Typographie** | Segoe UI standard | Segoe UI Variable (modern) |
| **Corners** | 12px (non-standard) | 8px (Windows 11 standard) |
| **Accent** | Toujours bleu | Suit l'accent système (bleu/vert/rouge/violet/orange) |

---

## 🔍 Points de Validation Visuels

### 1. Onboarding Window

#### Page 0 (Welcome)
**À vérifier:**
- [ ] Logo container a un fond subtil (pas transparent)
- [ ] "Welcome to" est plus clair que le titre principal
- [ ] "BetterTrumpet" est bold et bien visible
- [ ] Version badge utilise la couleur accent système
- [ ] Tout le texte est lisible en Light ET Dark theme

**Couleurs attendues (Dark theme):**
- Background: Gris très foncé (~#202020)
- "Welcome to": Gris moyen (~60% opacity)
- "BetterTrumpet": Blanc (~90% opacity)
- Version badge background: Accent à 10% opacity
- Version badge text: Accent pleine opacité

**Couleurs attendues (Light theme):**
- Background: Gris très clair (~#F3F3F3)
- "Welcome to": Gris moyen (~60% opacity)
- "BetterTrumpet": Noir (~90% opacity)
- Version badge background: Accent à 10% opacity
- Version badge text: Accent pleine opacité

#### Page 1 (Setup - Audio & Appearance)
**À vérifier:**
- [ ] Cards device list ont hover state visible
- [ ] Selected device a bordure accent + background accent subtil
- [ ] Theme cards preview s'adaptent au thème actuel
- [ ] System theme card montre des shapes grises
- [ ] Custom theme card garde son gradient bleu/violet (hardcodé OK ici)

**Interaction:**
- Hover sur device → fond change légèrement
- Click sur device → bordure accent apparaît
- Animation smooth (150ms)

#### Page 2 (Privacy)
**À vérifier:**
- [ ] Trust badges (No Ads, No Tracking, No Selling) utilisent accent color
- [ ] Toggle switches OFF: gris
- [ ] Toggle switches ON: accent color avec animation fluide
- [ ] Update channel buttons ont bordure accent quand sélectionnés
- [ ] Le texte "Recommended" sous "All" est en accent color

**Toggle Switch Animation:**
- Durée: 150ms
- Thumb se déplace de gauche à droite
- Track change de couleur simultanément
- Smooth, pas saccadé

#### Page 3 (Ready)
**À vérifier:**
- [ ] Check circle background: accent subtil (~15% opacity)
- [ ] Check icon: accent pleine opacité
- [ ] Checkmark items utilisent accent pour les icons
- [ ] Texte des items est lisible (secondary text color)

---

### 2. Changelog Window

#### Header
**À vérifier:**
- [ ] Version badge identique à onboarding (accent 10% bg, accent text)
- [ ] Title en Variable Display (plus moderne que Segoe UI standard)
- [ ] Subtitle en gris secondaire

#### Content (Sections générées dynamiquement)
**À vérifier:**
- [ ] Section headers ont icon accent color à gauche
- [ ] Section title est bold et visible
- [ ] Divider sous header est subtil (pas trop visible)
- [ ] Bullet points sont des dots accent color
- [ ] Texte des bullets est lisible (secondary text)
- [ ] Bold markdown (**text**) est rendu en primary text color

**Glyphs par section:**
- "Fix" / "Fixes" → Wrench (&#xE90F;)
- "New" / "Features" → Star (&#xE710;)
- "Breaking" → Warning (&#xE7BA;)
- "Performance" → Speed (&#xE9F5;)
- "Under the Hood" / "Tech" → Settings (&#xE756;)
- Défaut → Info (&#xE81C;)

#### Entrance Animation
**À vérifier:**
- [ ] Cards apparaissent une par une (stagger 40ms)
- [ ] Fade in: 0 → 1 opacity (300ms)
- [ ] Slide up: 12px → 0 (300ms)
- [ ] Easing: CubicEase EaseOut
- [ ] Pas de flicker ou saccade

---

## 🧪 Test Matrix

### Accent Colors à Tester

| Couleur | Hex | Test |
|---------|-----|------|
| Bleu (défaut) | `#0078D4` | [ ] OK |
| Vert | `#107C10` | [ ] OK |
| Rouge | `#E81123` | [ ] OK |
| Violet | `#881798` | [ ] OK |
| Orange | `#FF8C00` | [ ] OK |

**Comment tester:**
1. Ouvrir Paramètres Windows → Personnalisation → Couleurs
2. Changer "Couleur d'accentuation"
3. Relancer BetterTrumpet
4. Vérifier que tous les éléments accent changent

**Éléments qui DOIVENT changer:**
- Version badge text + background
- Progress bar (onboarding)
- Selected states (borders)
- Toggle switches (ON state)
- Trust badges
- Bullet dots
- Check icons
- Section glyphs
- Primary button background

---

### Thèmes à Tester

#### Light Theme
**Activation:**
Paramètres Windows → Personnalisation → Couleurs → Mode → Clair

**À vérifier:**
- [ ] Background: Blanc/gris très clair
- [ ] Text: Noir avec bon contraste
- [ ] Cards: Blanches avec bordure subtile
- [ ] Hover states visibles mais subtils
- [ ] Aucun texte illisible

**Problèmes potentiels:**
- Blanc sur blanc → vérifier les bordures
- Contraste insuffisant → ajuster opacity si nécessaire

#### Dark Theme
**Activation:**
Paramètres Windows → Personnalisation → Couleurs → Mode → Sombre

**À vérifier:**
- [ ] Background: Gris très foncé
- [ ] Text: Blanc avec bon contraste
- [ ] Cards: Gris foncé avec bordure subtile
- [ ] Hover states visibles
- [ ] Aucun texte illisible

#### High Contrast Mode
**Activation:**
Paramètres Windows → Accessibilité → Contraste → Thèmes à contraste élevé

**À vérifier:**
- [ ] Tous les textes visibles
- [ ] Bordures visibles
- [ ] Icons visibles
- [ ] Pas de dégradation de l'UI
- [ ] Navigation possible au clavier

---

## 🐛 Bugs Potentiels à Surveiller

### 1. Toggle Switch Animation
**Symptôme:** Le toggle ne s'anime pas ou flicker.

**Cause probable:** `DynamicResource` non résolu pour `ColorAnimation`.

**Fix:** Vérifier que le brush a un nom (`x:Name="TrackBrush"`) et que l'animation cible ce nom.

**Code à vérifier:**
```xaml
<Border x:Name="Track">
    <Border.Background>
        <SolidColorBrush x:Name="TrackBrush" Color="{DynamicResource ControlLightBaseLowColor}"/>
    </Border.Background>
</Border>

<ColorAnimation Storyboard.TargetName="TrackBrush"
                Storyboard.TargetProperty="Color"
                To="{DynamicResource SystemAccentColor}"/>
```

### 2. Accent Opacity Syntax
**Symptôme:** `SystemAccent/0.1` ne fonctionne pas, background reste transparent.

**Cause:** Syntaxe custom du ThemeManager pas supportée partout.

**Fix:** Utiliser un `Opacity` sur le Border ou créer une `Theme:Ref`:
```xaml
<!-- Option A -->
<Border Background="{DynamicResource SystemAccentBrush}" Opacity="0.1"/>

<!-- Option B -->
<Theme:Ref Key="AccentSubtle" Value="SystemAccent/0.1"/>
<Border Background="{DynamicResource AccentSubtle}"/>
```

### 3. Font Rendering sur Windows 10
**Symptôme:** Segoe UI Variable ne s'affiche pas correctement.

**Cause:** Font non disponible sur Windows 10.

**Fix:** Le fallback `Segoe UI` devrait prendre le relais automatiquement:
```xaml
FontFamily="Segoe UI Variable Display, Segoe UI"
```

Si le problème persiste, vérifier que la syntaxe fallback est bien présente partout.

### 4. Theme:Brush non résolu en code-behind
**Symptôme:** Exception ou couleur par défaut en code-behind.

**Cause:** `ThemeManager.ResolveRef()` retourne null ou n'existe pas.

**Fix actuel:**
```csharp
private Brush TextPrimary => ThemeManager.ResolveRef("Text");
```

**Fix alternatif si ça ne marche pas:**
```csharp
private Brush TextPrimary => (Brush)FindResource("ApplicationTextDarkTheme");
// ou avec try/catch
private Brush TextPrimary
{
    get
    {
        try { return ThemeManager.ResolveRef("Text"); }
        catch { return Brushes.White; } // fallback
    }
}
```

---

## 📸 Checklist de Screenshots

Pour documenter la migration, prendre des screenshots de:

### Onboarding:
- [ ] Page 0 (Welcome) — Light theme, accent bleu
- [ ] Page 0 (Welcome) — Dark theme, accent vert
- [ ] Page 1 (Setup) — Device selection hover
- [ ] Page 1 (Setup) — Theme selection (system selected)
- [ ] Page 2 (Privacy) — Toggle ON/OFF states
- [ ] Page 2 (Privacy) — Update channel selection
- [ ] Page 3 (Ready) — Final screen

### Changelog:
- [ ] Header avec version badge
- [ ] Section "New Features" avec bullets
- [ ] Section "Bug Fixes" avec bullets
- [ ] Animation entrance (GIF ou vidéo 2-3 sec)

### Comparaisons Avant/Après:
- [ ] Side-by-side: Old blue hardcoded vs New system accent
- [ ] Side-by-side: Old Segoe UI vs New Segoe UI Variable
- [ ] Side-by-side: Light theme support

---

## ✅ Validation Finale

Avant de merger, confirmer:

### Fonctionnel:
- [ ] Onboarding se complète sans erreur
- [ ] Changelog s'affiche correctement
- [ ] Toutes les interactions répondent (hover, click, toggle)
- [ ] Navigation entre pages fonctionne
- [ ] Boutons Back/Next/Skip fonctionnent
- [ ] Fermeture des fenêtres fonctionne

### Visuel:
- [ ] Aucune couleur hardcodée visible
- [ ] Accent color système respecté partout
- [ ] Light theme fonctionne
- [ ] Dark theme fonctionne
- [ ] High Contrast fonctionne
- [ ] Animations fluides (pas de jank)

### Technique:
- [ ] Aucune exception dans les logs
- [ ] Aucun warning XAML dans Output
- [ ] Build réussit en Release
- [ ] Aucune régression fonctionnelle

### Code Quality:
- [ ] Plus de couleurs hardcodées dans le code
- [ ] Toutes les fonts utilisent Variable avec fallback
- [ ] Spacing cohérent (multiples de 4)
- [ ] CornerRadius standardisé (4/6/8)

---

## 🎓 Tips pour les Prochaines UI

### Do's ✅
- Toujours utiliser `Theme:Brush.*` pour les couleurs
- Toujours inclure `FontFamily` avec fallback
- Toujours tester Light + Dark + High Contrast
- Toujours utiliser `SystemAccent` pour les actions primaires
- Toujours utiliser les noms de thème standard (`BaseMedium`, `ChromeLow`, etc.)

### Don'ts ❌
- Ne jamais hardcoder de couleurs hex dans XAML
- Ne jamais utiliser `ColorAnimation` directement sur `Theme:Brush`
- Ne jamais supposer un thème (Dark uniquement)
- Ne jamais oublier le fallback font
- Ne jamais utiliser de corners non-standard (rester sur 4/6/8)

---

**Ready to validate! 🎨**
