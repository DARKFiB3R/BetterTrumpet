# BetterTrumpet — Windows 11 Fluent Design Specification
## Onboarding & Changelog Modernization

**Date:** 2026-06-08  
**Status:** Design Specification  
**Goal:** Transformer OnboardingWindow & ChangelogWindow en véritables composants Windows 11 Fluent natifs

---

## 🎨 Principes de Design

### 1. Utiliser les ressources système UNIQUEMENT
- ❌ **Interdit:** Couleurs hardcodées (`#3B9EFF`, `#FF101014`)
- ✅ **Correct:** `Theme:Brush.Foreground="SystemAccent"`, `{ThemeResource SystemAccentColor}`

### 2. Typographie Windows 11
- **Headings:** Segoe UI Variable Display (si dispo), sinon Segoe UI SemiBold
- **Body:** Segoe UI Variable Text, FontSize 14 (base)
- **Captions:** Segoe UI Variable Text, FontSize 12

### 3. Spacing System
```
Compact: 4, 8, 12
Default: 16, 24, 32
Spacious: 40, 48, 56
```

### 4. Corner Radius
- Cards/Surfaces: `8dp`
- Buttons: `4dp`
- Dialogs: `8dp` (Windows 11 standard)

---

## 🏗️ Architecture de Couleurs

### Remplacer les couleurs hardcodées par:

```xaml
<!-- ❌ AVANT (hardcodé) -->
<Color x:Key="Onboarding.Accent">#3B9EFF</Color>
<Color x:Key="Onboarding.Background">#FF101014</Color>

<!-- ✅ APRÈS (système) -->
<!-- Utiliser directement Theme:Brush.* ou ThemeResource -->
<Setter Property="Theme:Brush.Foreground" Value="SystemAccent" />
<Setter Property="Theme:Brush.Background" Value="Theme={Theme}ChromeMediumLow" />
```

### Palette Système Windows 11

| Usage | Light Theme | Dark Theme | Référence WPF |
|-------|------------|------------|---------------|
| **Accent** | SystemAccent | SystemAccent | `SystemAccentColor` |
| **Background** | `#F3F3F3` | `#202020` | `Theme={Theme}ChromeMediumLow` |
| **Surface** | `#FFFFFF` | `#2B2B2B` | `Theme={Theme}ChromeLow` |
| **Border Subtle** | `#E5E5E5` | `#3A3A3A` | `Theme={Theme}ChromeDisabledLow` |
| **Text Primary** | `#000000E6` (90%) | `#FFFFFFE6` (90%) | `Theme=ApplicationText{Theme}Theme` |
| **Text Secondary** | `#00000099` (60%) | `#FFFFFF99` (60%) | `Theme={Theme}BaseMedium` |
| **Text Tertiary** | `#00000066` (40%) | `#FFFFFF66` (40%) | `Theme={Theme}BaseMediumLow` |

---

## 🎯 Composants à Refactoriser

### 1. OnboardingComponents.xaml

#### A. Supprimer toutes les couleurs hardcodées (lignes 10-23)

```xaml
<!-- ❌ SUPPRIMER -->
<Color x:Key="Onboarding.Accent">#3B9EFF</Color>
<Color x:Key="Onboarding.Background">#FF101014</Color>
<!-- ... etc -->

<!-- ✅ REMPLACER PAR -->
<!-- Utiliser Theme:Brush.* directement dans les styles -->
```

#### B. Refactoriser les Styles avec ressources système

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
    <Setter Property="FontSize" Value="20"/>
    <Setter Property="FontWeight" Value="SemiBold"/>
    <Setter Property="FontFamily" Value="Segoe UI Variable Display"/>
    <Setter Property="Theme:Brush.Foreground" Value="Theme=ApplicationText{Theme}Theme"/>
    <Setter Property="Margin" Value="0,0,0,12"/>
</Style>
```

#### C. Card Style avec Acrylic natif

**AVANT:**
```xaml
<Style x:Key="Onboarding.Card" TargetType="Border">
    <Setter Property="Background" Value="{StaticResource Onboarding.SurfaceBrush}"/>
    <Setter Property="BorderBrush" Value="{StaticResource Onboarding.BorderBrush}"/>
    <Setter Property="CornerRadius" Value="8"/>
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

#### D. Primary Button — Utiliser SystemAccent

**AVANT:**
```xaml
<Style x:Key="Onboarding.PrimaryButton" TargetType="Border">
    <Setter Property="Background" Value="{StaticResource Onboarding.AccentBrush}"/>
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

#### E. Toggle Switch — Standardiser

Le toggle actuel est presque bon, mais utiliser SystemAccent:

```xaml
<Trigger Property="IsChecked" Value="True">
    <Trigger.EnterActions>
        <BeginStoryboard>
            <Storyboard>
                <!-- ... animations ... -->
                <ColorAnimation Storyboard.TargetName="Track"
                    Storyboard.TargetProperty="(Border.Background).(SolidColorBrush.Color)"
                    To="{DynamicResource SystemAccentColor}" Duration="0:0:0.15"/>
            </Storyboard>
        </BeginStoryboard>
    </Trigger.EnterActions>
</Trigger>
```

---

### 2. OnboardingWindow.xaml

#### A. Background Window

**AVANT:**
```xaml
<Border CornerRadius="12" ClipToBounds="True"
        Background="{StaticResource Onboarding.SurfaceBrush}"
        BorderBrush="#08FFFFFF" BorderThickness="1">
```

**APRÈS:**
```xaml
<Border CornerRadius="8" ClipToBounds="True"
        Theme:Brush.Background="Theme={Theme}ChromeMediumLow"
        Theme:Brush.BorderBrush="Theme={Theme}ChromeDisabledLow"
        BorderThickness="1">
```

#### B. Progress Bar Accent

**AVANT:**
```xaml
<Border x:Name="ProgressBar" Background="{StaticResource Onboarding.AccentBrush}"
        HorizontalAlignment="Left">
```

**APRÈS:**
```xaml
<Border x:Name="ProgressBar" Theme:Brush.Background="SystemAccent"
        HorizontalAlignment="Left">
```

#### C. Version Badge

**AVANT:**
```xaml
<Border Background="#0A3B9EFF" CornerRadius="6" Padding="12,6">
    <TextBlock x:Name="VersionText" FontSize="12" FontWeight="SemiBold"
               Foreground="{StaticResource Onboarding.AccentBrush}"/>
</Border>
```

**APRÈS:**
```xaml
<Border Theme:Brush.Background="SystemAccent/0.1" CornerRadius="6" Padding="12,6">
    <TextBlock x:Name="VersionText" FontSize="12" FontWeight="SemiBold"
               FontFamily="Segoe UI Variable Text"
               Theme:Brush.Foreground="SystemAccent"/>
</Border>
```

#### D. Theme Selection Cards

Utiliser accent dynamique pour la sélection:

```xaml
<Trigger Property="Tag" Value="Selected">
    <Setter Property="Theme:Brush.BorderBrush" Value="SystemAccent"/>
    <Setter Property="Theme:Brush.Background" Value="SystemAccent/0.1"/>
</Trigger>
```

---

### 3. ChangelogWindow.xaml

#### A. Supprimer les brushes statiques du code-behind

**ChangelogWindow.xaml.cs — AVANT (lignes 21-37):**
```csharp
private static readonly Brush _textPrimary;
private static readonly Brush _textSecondary;
// ... etc

static ChangelogWindow()
{
    _textPrimary = new SolidColorBrush(Color.FromArgb(0xF0, 0xFF, 0xFF, 0xFF));
    // ...
}
```

**APRÈS:**
```csharp
// Utiliser les ressources XAML directement
private Brush TextPrimary => (Brush)FindResource("ApplicationTextDarkTheme"); // ou Theme resolver
private Brush TextSecondary => // ... via ThemeManager
```

**OU MIEUX:** Créer les éléments UI en XAML avec binding, pas en code-behind.

#### B. Migration vers DataTemplate

Au lieu de créer les cards en C#, utiliser un `ItemsControl` avec `ItemTemplate`:

```xaml
<ItemsControl ItemsSource="{Binding ChangelogSections}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Border Style="{StaticResource Onboarding.Card}" Margin="0,0,0,16">
                <StackPanel>
                    <!-- Section header -->
                    <StackPanel Orientation="Horizontal" Margin="0,0,0,12">
                        <TextBlock Text="{Binding Glyph}" FontFamily="Segoe MDL2 Assets"
                                   FontSize="15" Theme:Brush.Foreground="SystemAccent"
                                   Margin="0,0,10,0"/>
                        <TextBlock Text="{Binding Title}" FontSize="16" FontWeight="SemiBold"
                                   FontFamily="Segoe UI Variable Display"
                                   Theme:Brush.Foreground="Theme=ApplicationText{Theme}Theme"/>
                    </StackPanel>
                    
                    <!-- Section items -->
                    <ItemsControl ItemsSource="{Binding Items}">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <Grid Margin="0,8">
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="20"/>
                                        <ColumnDefinition Width="*"/>
                                    </Grid.ColumnDefinitions>
                                    <Border Width="6" Height="6" CornerRadius="3"
                                            Theme:Brush.Background="SystemAccent"
                                            VerticalAlignment="Top" Margin="0,7,0,0"/>
                                    <TextBlock Grid.Column="1" Text="{Binding Text}"
                                               FontSize="14" TextWrapping="Wrap"
                                               Theme:Brush.Foreground="Theme={Theme}BaseMedium"/>
                                </Grid>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </StackPanel>
            </Border>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

---

## 🚀 Plan d'Implémentation

### Phase 1: Refactoriser OnboardingComponents.xaml
1. ✅ Supprimer toutes les `<Color>` hardcodées (lignes 10-23)
2. ✅ Remplacer par `Theme:Brush.*` dans chaque style
3. ✅ Ajouter `FontFamily="Segoe UI Variable Display"` aux titres
4. ✅ Ajouter `FontFamily="Segoe UI Variable Text"` au body text
5. ✅ Standardiser les margins (8, 12, 16, 24)

### Phase 2: Adapter OnboardingWindow.xaml
1. ✅ Remplacer `Background="{StaticResource Onboarding.*}"` par `Theme:Brush.*`
2. ✅ Remplacer `Foreground="{StaticResource Onboarding.*}"` par `Theme:Brush.*`
3. ✅ Update CornerRadius: `12` → `8`
4. ✅ Update tous les badges/pills avec SystemAccent

### Phase 3: Refactoriser ChangelogWindow
1. ✅ Supprimer les static brushes du code-behind
2. ✅ Créer ViewModel avec `ObservableCollection<ChangelogSection>`
3. ✅ Migrer le rendering C# vers XAML DataTemplate
4. ✅ Utiliser `Theme:Brush.*` partout

### Phase 4: Testing
1. ✅ Tester en Light Theme
2. ✅ Tester en Dark Theme
3. ✅ Tester avec différentes couleurs d'accent système
4. ✅ Vérifier High Contrast mode
5. ✅ Vérifier que tout scale correctement (125%, 150%, 200% DPI)

---

## 📐 Exemples de Conversion

### Exemple 1: Section Title

**AVANT:**
```xaml
<TextBlock Text="Audio Device" Style="{StaticResource Onboarding.SectionTitle}"/>
```
Avec style hardcodé `Foreground="{StaticResource Onboarding.Text.Primary}"`

**APRÈS:**
```xaml
<TextBlock Text="Audio Device" Style="{StaticResource Onboarding.SectionTitle}"/>
```
Avec style système:
```xaml
<Style x:Key="Onboarding.SectionTitle" TargetType="TextBlock">
    <Setter Property="FontFamily" Value="Segoe UI Variable Display"/>
    <Setter Property="FontSize" Value="20"/>
    <Setter Property="FontWeight" Value="SemiBold"/>
    <Setter Property="Theme:Brush.Foreground" Value="Theme=ApplicationText{Theme}Theme"/>
    <Setter Property="Margin" Value="0,0,0,12"/>
</Style>
```

### Exemple 2: Card Background

**AVANT:**
```xaml
<Border Background="#FF18181E" BorderBrush="#12FFFFFF" BorderThickness="1" CornerRadius="8">
```

**APRÈS:**
```xaml
<Border Theme:Brush.Background="Theme={Theme}ChromeLow"
        Theme:Brush.BorderBrush="Theme={Theme}ChromeDisabledLow"
        BorderThickness="1" CornerRadius="8">
```

### Exemple 3: Accent Color Usage

**AVANT:**
```xaml
<Border Background="#3B9EFF">
    <TextBlock Foreground="White"/>
</Border>
```

**APRÈS:**
```xaml
<Border Theme:Brush.Background="SystemAccent">
    <TextBlock Theme:Brush.Foreground="Light=ApplicationTextLightTheme, Dark=ApplicationTextDarkTheme"/>
</Border>
```

---

## 🎨 Checklist de Validation

Après implémentation, vérifier:

- [ ] Aucune couleur hardcodée restante (`#[0-9A-F]{6,8}`)
- [ ] Tous les `Foreground` utilisent `Theme:Brush.*`
- [ ] Tous les `Background` utilisent `Theme:Brush.*`
- [ ] Typographie: `Segoe UI Variable Display` pour titres, `Segoe UI Variable Text` pour body
- [ ] Spacing cohérent (multiples de 4: 8, 12, 16, 24, 32)
- [ ] CornerRadius: 4 (boutons), 6 (chips), 8 (cards)
- [ ] Animations fluides (CubicEase, durées 100-300ms)
- [ ] Support Light + Dark theme
- [ ] Support High Contrast
- [ ] L'accent color système est respecté partout

---

## 🔗 Références

- [Windows 11 Design Principles](https://learn.microsoft.com/en-us/windows/apps/design/)
- [WinUI 3 Color Resources](https://learn.microsoft.com/en-us/windows/apps/design/style/color)
- [Fluent Design System](https://www.microsoft.com/design/fluent/)
- [Segoe UI Variable](https://learn.microsoft.com/en-us/windows/apps/design/style/typography)

---

## 📝 Notes d'Implémentation

### Fallback pour Segoe UI Variable

Si Segoe UI Variable n'est pas disponible (Windows 10):

```xaml
<Setter Property="FontFamily">
    <Setter.Value>
        <FontFamily>Segoe UI Variable Display, Segoe UI</FontFamily>
    </Setter.Value>
</Setter>
```

WPF accepte la syntaxe fallback native.

### Animation des entrées

Garder les animations existantes (elles sont bonnes), mais vérifier que les durées respectent Fluent:
- **Fast:** 100-150ms (hover states)
- **Normal:** 200-300ms (transitions, entrance)
- **Slow:** 400-500ms (réservé pour emphasis)

### Gestion du ThemeManager

Le projet utilise déjà un `Theme:Manager` custom. Continuer à l'utiliser avec les refs système:

```xaml
<Theme:Ref Key="OnboardingAccent" Value="SystemAccent" />
<Theme:Ref Key="OnboardingText" Value="Theme=ApplicationText{Theme}Theme" />
```

---

**Fin de spécification. Prêt pour l'implémentation.**
