# Guide d'Implémentation — Windows 11 Fluent Design
## Conversion Pratique OnboardingComponents.xaml

**Companion de:** `FLUENT-DESIGN-SPEC.md`  
**Date:** 2026-06-08

---

## 🎯 Objectif

Convertir **OnboardingComponents.xaml** ligne par ligne de couleurs hardcodées vers ressources système Windows 11.

---

## 📋 Table de Conversion Rapide

| Ancien (Hardcodé) | Nouveau (Système) | Usage |
|-------------------|-------------------|-------|
| `#3B9EFF` | `SystemAccent` | Couleur accent principale |
| `#5BB0FF` | `SystemAccentLight1` | Accent hover |
| `#FF101014` | `Theme={Theme}ChromeMediumLow` | Background dark |
| `#FF18181E` | `Theme={Theme}ChromeLow` | Surface dark |
| `#F0FFFFFF` (90%) | `Theme=ApplicationText{Theme}Theme` | Text primary |
| `#B0FFFFFF` (70%) | `Theme={Theme}BaseMedium` | Text secondary |
| `#70FFFFFF` (44%) | `Theme={Theme}BaseMediumLow` | Text tertiary |
| `#40FFFFFF` (25%) | `Theme={Theme}BaseLow` | Text muted |
| `#12FFFFFF` (7%) | `Theme={Theme}ChromeDisabledLow` | Border subtle |
| `#08FFFFFF` (3%) | `Theme={Theme}ChromeDisabledLow/0.5` | Divider |

---

## 🔧 Conversion Détaillée

### ÉTAPE 1: Supprimer le bloc de couleurs (lignes 9-23)

**Supprimer complètement:**
```xaml
<!-- ═══ COLORS ═══ -->
<Color x:Key="Onboarding.Accent">#3B9EFF</Color>
<Color x:Key="Onboarding.AccentHover">#5BB0FF</Color>
<Color x:Key="Onboarding.Background">#FF101014</Color>
<Color x:Key="Onboarding.Surface">#FF18181E</Color>
<Color x:Key="Onboarding.SurfaceHover">#FF1F2028</Color>
<Color x:Key="Onboarding.Border">#12FFFFFF</Color>

<SolidColorBrush x:Key="Onboarding.AccentBrush" Color="{StaticResource Onboarding.Accent}"/>
<SolidColorBrush x:Key="Onboarding.SurfaceBrush" Color="{StaticResource Onboarding.Surface}"/>
<SolidColorBrush x:Key="Onboarding.BorderBrush" Color="{StaticResource Onboarding.Border}"/>
<SolidColorBrush x:Key="Onboarding.Text.Primary" Color="#F0FFFFFF"/>
<SolidColorBrush x:Key="Onboarding.Text.Secondary" Color="#B0FFFFFF"/>
<SolidColorBrush x:Key="Onboarding.Text.Tertiary" Color="#70FFFFFF"/>
<SolidColorBrush x:Key="Onboarding.Text.Muted" Color="#40FFFFFF"/>
```

**Raison:** Toutes ces couleurs doivent provenir du système pour supporter Light/Dark theme et l'accent color de l'utilisateur.

---

### ÉTAPE 2: Refactoriser SectionTitle (lignes 26-31)

**AVANT:**
```xaml
<Style x:Key="Onboarding.SectionTitle" TargetType="TextBlock">
    <Setter Property="FontSize" Value="20"/>
    <Setter Property="FontWeight" Value="SemiBold"/>
    <Setter Property="Foreground" Value="{StaticResource Onboarding.Text.Primary}"/>
    <Setter Property="Margin" Value="0,0,0,8"/>
</Style>
```

**APRÈS:**
```xaml
<Style x:Key="Onboarding.SectionTitle" TargetType="TextBlock">
    <Setter Property="FontFamily" Value="Segoe UI Variable Display, Segoe UI"/>
    <Setter Property="FontSize" Value="20"/>
    <Setter Property="FontWeight" Value="SemiBold"/>
    <Setter Property="Theme:Brush.Foreground" Value="Theme=ApplicationText{Theme}Theme"/>
    <Setter Property="Margin" Value="0,0,0,12"/>
</Style>
```

**Changements:**
- ✅ Ajout `FontFamily` avec fallback
- ✅ `Foreground` → `Theme:Brush.Foreground`
- ✅ Margin `8` → `12` (standardisation)

---

### ÉTAPE 3: Refactoriser SectionSubtitle (lignes 34-41)

**AVANT:**
```xaml
<Style x:Key="Onboarding.SectionSubtitle" TargetType="TextBlock">
    <Setter Property="FontSize" Value="14"/>
    <Setter Property="FontWeight" Value="Normal"/>
    <Setter Property="Foreground" Value="{StaticResource Onboarding.Text.Secondary}"/>
    <Setter Property="TextWrapping" Value="Wrap"/>
    <Setter Property="LineHeight" Value="22"/>
    <Setter Property="Margin" Value="0,0,0,24"/>
</Style>
```

**APRÈS:**
```xaml
<Style x:Key="Onboarding.SectionSubtitle" TargetType="TextBlock">
    <Setter Property="FontFamily" Value="Segoe UI Variable Text, Segoe UI"/>
    <Setter Property="FontSize" Value="14"/>
    <Setter Property="FontWeight" Value="Normal"/>
    <Setter Property="Theme:Brush.Foreground" Value="Theme={Theme}BaseMedium"/>
    <Setter Property="TextWrapping" Value="Wrap"/>
    <Setter Property="LineHeight" Value="22"/>
    <Setter Property="Margin" Value="0,0,0,24"/>
</Style>
```

**Changements:**
- ✅ Ajout `FontFamily`
- ✅ `Foreground` → `Theme:Brush.Foreground`
- ✅ Utilise `BaseMedium` pour secondary text

---

### ÉTAPE 4: Refactoriser Card (lignes 44-50)

**AVANT:**
```xaml
<Style x:Key="Onboarding.Card" TargetType="Border">
    <Setter Property="Background" Value="{StaticResource Onboarding.SurfaceBrush}"/>
    <Setter Property="BorderBrush" Value="{StaticResource Onboarding.BorderBrush}"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="CornerRadius" Value="8"/>
    <Setter Property="Padding" Value="16,14"/>
</Style>
```

**APRÈS:**
```xaml
<Style x:Key="Onboarding.Card" TargetType="Border">
    <Setter Property="Theme:Brush.Background" Value="Theme={Theme}ChromeLow"/>
    <Setter Property="Theme:Brush.BorderBrush" Value="Theme={Theme}ChromeDisabledLow"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="CornerRadius" Value="8"/>
    <Setter Property="Padding" Value="16"/>
</Style>
```

**Changements:**
- ✅ `Background` → `Theme:Brush.Background`
- ✅ `BorderBrush` → `Theme:Brush.BorderBrush`
- ✅ Padding simplifié (16 au lieu de 16,14)

---

### ÉTAPE 5: Refactoriser SelectableCard (lignes 53-76)

**AVANT:**
```xaml
<Style x:Key="Onboarding.SelectableCard" TargetType="Border">
    <Setter Property="Background" Value="{StaticResource Onboarding.SurfaceBrush}"/>
    <Setter Property="BorderThickness" Value="2"/>
    <Setter Property="BorderBrush" Value="{StaticResource Onboarding.BorderBrush}"/>
    <Setter Property="CornerRadius" Value="8"/>
    <Setter Property="Cursor" Value="Hand"/>
    <Style.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
            <Setter Property="Background">
                <Setter.Value>
                    <SolidColorBrush Color="{StaticResource Onboarding.SurfaceHover}"/>
                </Setter.Value>
            </Setter>
        </Trigger>
        <Trigger Property="Tag" Value="Selected">
            <Setter Property="BorderBrush" Value="{StaticResource Onboarding.AccentBrush}"/>
            <Setter Property="Background">
                <Setter.Value>
                    <SolidColorBrush Color="#1A3B9EFF"/>
                </Setter.Value>
            </Setter>
        </Trigger>
    </Style.Triggers>
</Style>
```

**APRÈS:**
```xaml
<Style x:Key="Onboarding.SelectableCard" TargetType="Border">
    <Setter Property="Theme:Brush.Background" Value="Theme={Theme}ChromeLow"/>
    <Setter Property="BorderThickness" Value="2"/>
    <Setter Property="Theme:Brush.BorderBrush" Value="Theme={Theme}ChromeDisabledLow"/>
    <Setter Property="CornerRadius" Value="8"/>
    <Setter Property="Cursor" Value="Hand"/>
    <Style.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
            <Setter Property="Theme:Brush.Background" Value="Theme={Theme}ListLow"/>
        </Trigger>
        <Trigger Property="Tag" Value="Selected">
            <Setter Property="Theme:Brush.BorderBrush" Value="SystemAccent"/>
            <Setter Property="Theme:Brush.Background" Value="SystemAccent/0.1"/>
        </Trigger>
    </Style.Triggers>
</Style>
```

**Changements:**
- ✅ Hover: utilise `ListLow` (état hover standard Windows 11)
- ✅ Selected: `BorderBrush` → `SystemAccent`
- ✅ Selected background: `#1A3B9EFF` → `SystemAccent/0.1` (10% opacity)

---

### ÉTAPE 6: Refactoriser RadioButton (lignes 79-140)

**ZONES CRITIQUES:**

#### A. Border Background (ligne 88)
```xaml
<!-- AVANT -->
Background="{StaticResource Onboarding.SurfaceBrush}"

<!-- APRÈS -->
Theme:Brush.Background="Theme={Theme}ChromeLow"
```

#### B. Border BorderBrush (ligne 89)
```xaml
<!-- AVANT -->
BorderBrush="{StaticResource Onboarding.BorderBrush}"

<!-- APRÈS -->
Theme:Brush.BorderBrush="Theme={Theme}ChromeDisabledLow"
```

#### C. Check Icon Fill (ligne 102)
```xaml
<!-- AVANT -->
Fill="{StaticResource Onboarding.AccentBrush}"

<!-- APRÈS -->
Theme:Brush.Fill="SystemAccent"
```

#### D. Triggers (lignes 120-136)

**IsChecked Trigger:**
```xaml
<!-- AVANT -->
<Trigger Property="IsChecked" Value="True">
    <Setter TargetName="Root" Property="BorderBrush" Value="{StaticResource Onboarding.AccentBrush}"/>
    <Setter TargetName="Root" Property="Background">
        <Setter.Value>
            <SolidColorBrush Color="#1A3B9EFF"/>
        </Setter.Value>
    </Setter>
    <Setter TargetName="Check" Property="Visibility" Value="Visible"/>
</Trigger>

<!-- APRÈS -->
<Trigger Property="IsChecked" Value="True">
    <Setter TargetName="Root" Property="Theme:Brush.BorderBrush" Value="SystemAccent"/>
    <Setter TargetName="Root" Property="Theme:Brush.Background" Value="SystemAccent/0.1"/>
    <Setter TargetName="Check" Property="Visibility" Value="Visible"/>
</Trigger>
```

**IsMouseOver Trigger:**
```xaml
<!-- AVANT -->
<Trigger Property="IsMouseOver" Value="True">
    <Setter TargetName="Root" Property="Background">
        <Setter.Value>
            <SolidColorBrush Color="{StaticResource Onboarding.SurfaceHover}"/>
        </Setter.Value>
    </Setter>
</Trigger>

<!-- APRÈS -->
<Trigger Property="IsMouseOver" Value="True">
    <Setter TargetName="Root" Property="Theme:Brush.Background" Value="Theme={Theme}ListLow"/>
</Trigger>
```

---

### ÉTAPE 7: Refactoriser Toggle (lignes 143-203)

**ZONES CRITIQUES:**

#### Track Background (ligne 149)
```xaml
<!-- AVANT -->
<Border x:Name="Track" CornerRadius="12" Background="#20FFFFFF"/>

<!-- APRÈS -->
<Border x:Name="Track" CornerRadius="12" Theme:Brush.Background="Theme={Theme}BaseLow"/>
```

#### Thumb Background (ligne 151-152)
```xaml
<!-- AVANT -->
Background="#CCFFFFFF"

<!-- APRÈS -->
Theme:Brush.Background="Theme=ApplicationText{Theme}Theme"
```

#### Animation Checked (lignes 170-175)

**AVANT:**
```xaml
<ColorAnimation Storyboard.TargetName="Track"
    Storyboard.TargetProperty="(Border.Background).(SolidColorBrush.Color)"
    To="{StaticResource Onboarding.Accent}" Duration="0:0:0.15"/>
```

**PROBLÈME:** `ColorAnimation` ne peut pas animer un `Theme:Brush` dynamique.

**SOLUTION:** Utiliser `ThemeResource` pour l'animation ou créer un DynamicResource binding.

**APRÈS:**
```xaml
<ColorAnimation Storyboard.TargetName="Track"
    Storyboard.TargetProperty="(Border.Background).(SolidColorBrush.Color)"
    To="{DynamicResource SystemAccentColor}" Duration="0:0:0.15"/>
```

**Note:** Il faudra peut-être convertir le `Track.Background` en `SolidColorBrush` avec binding au lieu de `Theme:Brush`.

#### Alternative (recommandée):

```xaml
<!-- Déclarer le brush explicitement -->
<Border x:Name="Track" CornerRadius="12">
    <Border.Background>
        <SolidColorBrush x:Name="TrackBrush" Color="{DynamicResource SystemBaseLowColor}"/>
    </Border.Background>
</Border>

<!-- Puis animer -->
<ColorAnimation Storyboard.TargetName="TrackBrush"
    Storyboard.TargetProperty="Color"
    To="{DynamicResource SystemAccentColor}" Duration="0:0:0.15"/>
```

---

### ÉTAPE 8: Refactoriser PrimaryButton (lignes 206-220)

**AVANT:**
```xaml
<Style x:Key="Onboarding.PrimaryButton" TargetType="Border">
    <Setter Property="Background" Value="{StaticResource Onboarding.AccentBrush}"/>
    <Setter Property="CornerRadius" Value="6"/>
    <Setter Property="Padding" Value="24,11"/>
    <Setter Property="Cursor" Value="Hand"/>
    <Style.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
            <Setter Property="Background">
                <Setter.Value>
                    <SolidColorBrush Color="{StaticResource Onboarding.AccentHover}"/>
                </Setter.Value>
            </Setter>
        </Trigger>
    </Style.Triggers>
</Style>
```

**APRÈS:**
```xaml
<Style x:Key="Onboarding.PrimaryButton" TargetType="Border">
    <Setter Property="Theme:Brush.Background" Value="SystemAccent"/>
    <Setter Property="CornerRadius" Value="4"/>
    <Setter Property="Padding" Value="20,10"/>
    <Setter Property="Cursor" Value="Hand"/>
    <Style.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
            <Setter Property="Theme:Brush.Background" Value="SystemAccentLight1"/>
        </Trigger>
    </Style.Triggers>
</Style>
```

**Changements:**
- ✅ CornerRadius `6` → `4` (Windows 11 standard button)
- ✅ Padding `24,11` → `20,10` (multiple de 4)
- ✅ Background → `SystemAccent`
- ✅ Hover → `SystemAccentLight1`

---

### ÉTAPE 9: Refactoriser SecondaryButton (lignes 223-233)

**AVANT:**
```xaml
<Style x:Key="Onboarding.SecondaryButton" TargetType="Border">
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="CornerRadius" Value="6"/>
    <Setter Property="Padding" Value="16,10"/>
    <Setter Property="Cursor" Value="Hand"/>
    <Style.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
            <Setter Property="Background" Value="#10FFFFFF"/>
        </Trigger>
    </Style.Triggers>
</Style>
```

**APRÈS:**
```xaml
<Style x:Key="Onboarding.SecondaryButton" TargetType="Border">
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="CornerRadius" Value="4"/>
    <Setter Property="Padding" Value="16,10"/>
    <Setter Property="Cursor" Value="Hand"/>
    <Style.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
            <Setter Property="Theme:Brush.Background" Value="Theme={Theme}ListLow"/>
        </Trigger>
    </Style.Triggers>
</Style>
```

**Changements:**
- ✅ CornerRadius `6` → `4`
- ✅ Hover: `#10FFFFFF` → `Theme={Theme}ListLow`

---

## 🔄 Résumé des Remplacements Globaux

### Find & Replace Rapide (avec regex)

1. **Supprimer les références StaticResource:**
   - Find: `Value="\{StaticResource Onboarding\.[A-Za-z.]+\}"`
   - Manual replacement (varie par contexte)

2. **Couleurs hardcodées:**
   - Find: `#[0-9A-F]{6,8}`
   - Manual review (certaines peuvent rester si nécessaire)

3. **Foreground → Theme:Brush.Foreground:**
   - Find: `Property="Foreground"`
   - Replace: `Property="Theme:Brush.Foreground"`

4. **Background → Theme:Brush.Background:**
   - Find: `Property="Background"`
   - Replace: `Property="Theme:Brush.Background"` (dans styles uniquement)

---

## ⚠️ Pièges à Éviter

### 1. ColorAnimation avec Theme:Brush
**Problème:** `ColorAnimation` ne peut pas animer `Theme:Brush` directement.

**Solution:** Utiliser `SolidColorBrush` avec `DynamicResource` pour les propriétés animées:
```xaml
<Border.Background>
    <SolidColorBrush Color="{DynamicResource SystemAccentColor}"/>
</Border.Background>
```

### 2. Opacité sur SystemAccent
**Problème:** `SystemAccent/0.1` n'est pas une syntaxe WPF valide.

**Solution:** Utiliser une reference explicite ou Opacity:
```xaml
<!-- Option A: Theme:Ref -->
<Theme:Ref Key="AccentSubtle" Value="SystemAccent/0.1"/>

<!-- Option B: Border Opacity -->
<Border Background="{StaticResource SystemAccentBrush}" Opacity="0.1"/>

<!-- Option C: Converter -->
<Border Background="{Binding Source={StaticResource SystemAccentBrush}, 
                     Converter={StaticResource OpacityConverter}, 
                     ConverterParameter=0.1}"/>
```

### 3. Segoe UI Variable sur Windows 10
**Problème:** Font non disponible sur Windows 10.

**Solution:** Toujours inclure fallback:
```xaml
<Setter Property="FontFamily" Value="Segoe UI Variable Display, Segoe UI"/>
```

---

## ✅ Checklist de Validation

Après refactorisation complète de `OnboardingComponents.xaml`:

- [ ] Plus aucune `<Color x:Key="Onboarding.*">` dans le fichier
- [ ] Plus aucune `<SolidColorBrush x:Key="Onboarding.*">` dans le fichier
- [ ] Tous les `Foreground="..."` sont convertis en `Theme:Brush.Foreground`
- [ ] Tous les `Background="..."` (dans styles) sont `Theme:Brush.Background`
- [ ] Tous les `BorderBrush="..."` sont `Theme:Brush.BorderBrush`
- [ ] Tous les titres ont `FontFamily="Segoe UI Variable Display, Segoe UI"`
- [ ] Tous les body text ont `FontFamily="Segoe UI Variable Text, Segoe UI"`
- [ ] Tous les margins sont multiples de 4 (8, 12, 16, 24, 32)
- [ ] CornerRadius: 4 (buttons), 8 (cards)
- [ ] Animations utilisent `DynamicResource` pour les couleurs système
- [ ] Testé en Light theme
- [ ] Testé en Dark theme
- [ ] Testé avec accent color bleu, vert, rouge

---

## 🚀 Prochaine Étape

Une fois `OnboardingComponents.xaml` refactorisé, passer à:
1. **OnboardingWindow.xaml** — Remplacer toutes les références `{StaticResource Onboarding.*}`
2. **ChangelogWindow.xaml** — Idem
3. **ChangelogWindow.xaml.cs** — Supprimer les brushes statiques, utiliser ThemeManager

Voir `FLUENT-DESIGN-SPEC.md` pour le plan complet.

---

**Prêt à implémenter! 🎨**
