from pathlib import Path
import re


def replace_once(text: str, old: str, new: str, label: str) -> str:
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"{label}: expected one match, got {count}")
    return text.replace(old, new, 1)


# Flexible hand-in candidate rows: override the global centered Button template locally
# so the four canonical columns actually occupy the full row width.
items_path = Path("src/JunhyunHelper.Desktop/Items/ItemsPage.xaml")
items = items_path.read_text(encoding="utf-8")
old = '''                    HorizontalAlignment="Stretch" HorizontalContentAlignment="Stretch"
                    VerticalContentAlignment="Center" Padding="10,6" Margin="4,3">
                <Grid>'''
new = '''                    HorizontalAlignment="Stretch" HorizontalContentAlignment="Stretch"
                    VerticalContentAlignment="Center" Padding="10,6" Margin="4,3">
                <Button.Template>
                    <ControlTemplate TargetType="Button">
                        <Border x:Name="FlexibleCandidateButtonBorder"
                                Background="{TemplateBinding Background}"
                                BorderBrush="{TemplateBinding BorderBrush}"
                                BorderThickness="{TemplateBinding BorderThickness}"
                                CornerRadius="7"
                                Padding="{TemplateBinding Padding}">
                            <ContentPresenter Content="{TemplateBinding Content}"
                                              ContentTemplate="{TemplateBinding ContentTemplate}"
                                              HorizontalAlignment="Stretch"
                                              VerticalAlignment="Center" />
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter TargetName="FlexibleCandidateButtonBorder" Property="Background" Value="{StaticResource BackgroundHoverBrush}" />
                                <Setter TargetName="FlexibleCandidateButtonBorder" Property="BorderBrush" Value="{StaticResource AccentBrush}" />
                            </Trigger>
                            <Trigger Property="IsPressed" Value="True">
                                <Setter TargetName="FlexibleCandidateButtonBorder" Property="Background" Value="{StaticResource BackgroundMediumBrush}" />
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Button.Template>
                <Grid>'''
items = replace_once(items, old, new, "flexible candidate stretch template")
items_path.write_text(items, encoding="utf-8", newline="\n")


# Ammo canonical controls: star only; compact arrow-only detail handle with the requested
# state direction (expanded=down, collapsed=up).
ammo_xaml_path = Path("src/JunhyunHelper.Desktop/Ammo/AmmoPage.xaml")
ammo_xaml = ammo_xaml_path.read_text(encoding="utf-8")
ammo_xaml = replace_once(
    ammo_xaml,
    '''        <Button x:Name="ProductDetailToggleButton" Grid.Row="3"
                Content="▲  탄약 / 수급 경로 상세정보" ToolTip="상세정보 접기"
                HorizontalAlignment="Center" VerticalAlignment="Center"
                MinWidth="260" Height="30" Margin="0,3" Padding="14,2" FontWeight="SemiBold"
                Click="ProductDetailToggleButton_Click" />''',
    '''        <Button x:Name="ProductDetailToggleButton" Grid.Row="3"
                Content="▼" ToolTip="상세정보 접기"
                HorizontalAlignment="Center" VerticalAlignment="Center"
                Width="42" MinWidth="42" MaxWidth="42" Height="30" Margin="0,3" Padding="0" FontWeight="SemiBold"
                Click="ProductDetailToggleButton_Click" />''',
    "ammo arrow-only detail handle",
)
ammo_xaml_path.write_text(ammo_xaml, encoding="utf-8", newline="\n")

ammo_code_path = Path("src/JunhyunHelper.Desktop/Ammo/AmmoPage.xaml.cs")
ammo_code = ammo_code_path.read_text(encoding="utf-8")nammo_old = '''        var caliber = (CaliberComboBox.SelectedItem as CaliberChoice)?.RawCaliber;
        FavoriteCaliberButton.IsEnabled = caliber is not null;
        FavoriteCaliberButton.Content = caliber is not null && _favoriteCalibers.Contains(caliber)
            ? "★ 즐겨찾기"
            : "☆ 즐겨찾기";'''
ammo_new = '''        var caliber = (CaliberComboBox.SelectedItem as CaliberChoice)?.RawCaliber;
        var isFavorite = caliber is not null && _favoriteCalibers.Contains(caliber);
        FavoriteCaliberButton.IsEnabled = caliber is not null;
        FavoriteCaliberButton.Content = isFavorite ? "★" : "☆";
        FavoriteCaliberButton.ToolTip = isFavorite ? "즐겨찾기 해제" : "즐겨찾기 추가";'''
ammo_code = replace_once(ammo_code, nammo_old, nammo_new, "ammo favorite star-only state")
ammo_code_path.write_text(ammo_code, encoding="utf-8", newline="\n")

ammo_product_path = Path("src/JunhyunHelper.Desktop/Ammo/AmmoPage.ProductSearchAndDetails.cs")
ammo_product = ammo_product_path.read_text(encoding="utf-8")
ammo_product = replace_once(
    ammo_product,
    '''            _productDetailToggleButton.Content = "▲  탄약 / 수급 경로 상세정보";
            _productDetailToggleButton.ToolTip = "상세정보 접기";''',
    '''            _productDetailToggleButton.Content = "▼";
            _productDetailToggleButton.ToolTip = "상세정보 접기";''',
    "ammo expanded arrow direction",
)
ammo_product = replace_once(
    ammo_product,
    '''            _productDetailToggleButton.Content = "▼  탄약 / 수급 경로 상세정보";
            _productDetailToggleButton.ToolTip = "상세정보 펼치기";''',
    '''            _productDetailToggleButton.Content = "▲";
            _productDetailToggleButton.ToolTip = "상세정보 펼치기";''',
    "ammo collapsed arrow direction",
)
ammo_product_path.write_text(ammo_product, encoding="utf-8", newline="\n")


# Map Quest sidebar: the handle always occupies the right-edge lane. In collapsed mode
# that is the entire 34px sidebar; in expanded mode it becomes the outside/right handle.
map_path = Path("src/JunhyunHelper.Desktop/Map/LegacyMapQuestV2.cs")
map_code = map_path.read_text(encoding="utf-8")
map_code = replace_once(
    map_code,
    '''        _root = new Grid();
        _root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(CollapsedWidth) });
        _root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0) });''',
    '''        _root = new Grid();
        _root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0) });
        _root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(CollapsedWidth) });''',
    "map sidebar handle columns",
)
map_code = replace_once(map_code, "        Grid.SetColumn(_toggle, 0);", "        Grid.SetColumn(_toggle, 1);", "map toggle right lane")
map_code = replace_once(map_code, "        Grid.SetColumn(_expandedContent, 1);", "        Grid.SetColumn(_expandedContent, 0);", "map content left lane")
map_code = replace_once(
    map_code,
    '''        _root.ColumnDefinitions[1].Width = _expanded
            ? new GridLength(1, GridUnitType.Star)
            : new GridLength(0);''',
    '''        _root.ColumnDefinitions[0].Width = _expanded
            ? new GridLength(1, GridUnitType.Star)
            : new GridLength(0);''',
    "map expanded content width",
)

# Quest names are rendered directly in the fixed third column. The transparent click
# surface sits behind them; this avoids the app-wide Button template that hard-centers
# ContentPresenter regardless of HorizontalContentAlignment.
row_pattern = re.compile(
    r'''        var button = new Button\n        \{\n            Tag = entry\.QuestId,\n            Background = Brushes\.Transparent,\n            BorderThickness = new Thickness\(0\),\n            Padding = new Thickness\(0\),\n            Margin = new Thickness\(0\),\n            HorizontalAlignment = HorizontalAlignment\.Stretch,\n            HorizontalContentAlignment = HorizontalAlignment\.Stretch,\n            VerticalAlignment = VerticalAlignment\.Stretch,\n            VerticalContentAlignment = VerticalAlignment\.Center,\n            Cursor = Cursors\.Hand,\n            Content = CreateQuestContent\(entry\),\n        \};\n        button\.Click \+= QuestButton_Click;\n        Grid\.SetColumn\(button, 2\);\n        grid\.Children\.Add\(button\);'''
)
row_replacement = '''        var button = new Button
        {
            Tag = entry.QuestId,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Margin = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Cursor = Cursors.Hand,
            Content = null,
        };
        button.Click += QuestButton_Click;
        Grid.SetColumn(button, 2);
        grid.Children.Add(button);

        var content = CreateQuestContent(entry);
        content.IsHitTestVisible = false;
        content.Margin = new Thickness(8, 0, 0, 0);
        Grid.SetColumn(content, 2);
        grid.Children.Add(content);'''
map_code, count = row_pattern.subn(row_replacement, map_code, count=1)
if count != 1:
    raise RuntimeError(f"map quest direct title lane: expected one match, got {count}")
map_path.write_text(map_code, encoding="utf-8", newline="\n")


# Make the existing release smoke validate the *rendered* coordinates/state before it
# publishes success, so future source-only fixes cannot pass this regression again.
host_path = Path("src/JunhyunHelper.Desktop/MainWindow.LegacyMapHost.cs")
host = host_path.read_text(encoding="utf-8")
host = replace_once(
    host,
    '''            await VerifyMiniMapProductAsync();
            WriteMapSmokeSuccess();''',
    '''            await VerifyMiniMapProductAsync();
            await VerifyProductUiLayoutAsync();
            WriteMapSmokeSuccess();''',
    "rendered product UI smoke invocation",
)
host_path.write_text(host, encoding="utf-8", newline="\n")


# Static guardrails for the exact strings/structure that regressed in v0.1.11.
assert "★ 즐겨찾기" not in ammo_code and "☆ 즐겨찾기" not in ammo_code
assert 'Content="▼" ToolTip="상세정보 접기"' in ammo_xaml
assert '_productDetailToggleButton.Content = "▼";' in ammo_product
assert '_productDetailToggleButton.Content = "▲";' in ammo_product
assert 'Grid.SetColumn(_toggle, 1);' in map_code
assert 'Grid.SetColumn(_expandedContent, 0);' in map_code
assert 'Content = null,' in map_code
assert '<Button.Template>' in items
print("UI alignment and control patch applied")
