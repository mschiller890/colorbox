using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Schiller_DisV_16
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        //private DesktopAcrylicController? acrylicController;

        public int rValue = 0;
        public int gValue = 0;
        public int bValue = 0;

        // dynamicka collection ktera dava pri zmene notifications
        public ObservableCollection<ColorItem> ColorList { get; set; } = new ObservableCollection<ColorItem>();

        public MainWindow()
        {
            InitializeComponent();

            // to je vec tenhle framework fakt
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(800, 600));
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            // co jsem si to vybral
            if (this.AppWindow.Presenter is OverlappedPresenter presenter)
            {
                //presenter.IsResizable = false;
                //presenter.IsMaximizable = false;
                presenter.PreferredMinimumHeight = 600;
                presenter.PreferredMinimumWidth = 800;
            }

            //acrylicController = new DesktopAcrylicController();
            //acrylicController.TintOpacity = 0.7f;
            //acrylicController.LuminosityOpacity = 1f;
            //SystemBackdrop = new DesktopAcrylicBackdrop();

            ColorHistory.ItemsSource = ColorList;
        }

        // tohle by asi slo zkombinovat do renderPreview co
        private void R_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            rValue = (int)e.NewValue;
            renderPreview();
        }

        private void G_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            gValue = (int)e.NewValue;
            renderPreview();
        }

        private void B_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            bValue = (int)e.NewValue;
            renderPreview();
        }

        private void renderPreview()
        {
            if (ColorText == null || Canvas == null)
                return;

            ColorText.Text = $"{rValue}, {gValue}, {bValue}";

            Canvas.Background = new SolidColorBrush(Color.FromArgb(255, (byte)rValue, (byte)gValue, (byte)bValue));
        }

        // ucili jsme se vubec async
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            TextBox inputTextBox = new TextBox
            {
                PlaceholderText = "Enter a name...",
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            ContentDialog inputDialog = new ContentDialog
            {
                Title = "Name your color", 
                Content = inputTextBox,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                //IsSecondaryButtonEnabled = true,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot // targetuje window layer jako k cemu ten dialog patri nebo tak neco
            };

            // i like this tho
            ContentDialogResult result = await inputDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                //                                                         true              false 
                string name = string.IsNullOrWhiteSpace(inputTextBox.Text) ? "Unnamed Color" : inputTextBox.Text;

                var newSavedColor = new ColorItem
                {
                    ColorText = $"{name} (RGB: {rValue}, {gValue}, {bValue})",
                    //                                                   explicitni conversion pane uciteli!!! jste na me pysny??
                    ColorBrush = new SolidColorBrush(Color.FromArgb(255, (byte)rValue, (byte)gValue, (byte)bValue))
                };

                ColorList.Add(newSavedColor);

                SavedColorsCount.Text = $"{ColorList.Count.ToString()} saved";

                UpdateEmptyState();
            }
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {

            // fuha uz je to nejak moc ne
            if (sender is MenuFlyoutItem menuItem && menuItem.CommandParameter is ColorItem targetColor)
            {
                ColorList.Remove(targetColor);
                SavedColorsCount.Text = $"{ColorList.Count.ToString()} saved";
                UpdateEmptyState();
            }
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // tady neni asi tradicni SaveFileDialog
            // potreboval jsem dost pomoci a dokumentace moc nepomohla
            var savePicker = new Windows.Storage.Pickers.FileSavePicker();

            // jako treba co to je WinRT????? .Interop??????
            // neco o tom kdo/co vlastni ten popup
            // WinUI jsem se chtel naucit a ted kdyz se divam do hloubky, prijde mi strasne bloated...
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, windowHandle);

            // tohle se mi ale libi
            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("Plain Text Color File", new List<string>() { ".txt" });
            savePicker.SuggestedFileName = "SavedColors";

            Windows.Storage.StorageFile file = await savePicker.PickSaveFileAsync();

            if (file != null)
            {
                var lines = ColorList.Select(item => item.ColorText);

                try
                {
                    await Windows.Storage.FileIO.WriteLinesAsync(file, lines);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to save file: {ex.Message}");
                }
            }
        }

        // informuje OS ze tady podporujem drag and drop
        private void ColorHistory_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "Drop to load colors";
            e.DragUIOverride.IsCaptionVisible = true;
        }

        // co jsem si to vybral..., tohle uz nikdy neudelam
        private async void ColorHistory_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                // jestli pocet barev je vetsi nez 0
                // jestli je to skutecny soubor
                // jestli pripona je .txt
                if (items.Count > 0 && items[0] is Windows.Storage.StorageFile file && file.FileType == ".txt")
                {
                    try
                    {
                        var lines = await Windows.Storage.FileIO.ReadLinesAsync(file);

                        foreach (string line in lines)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            // parsuje format "Name (RGB: R, G, B)"
                            // bezpecne najde "RGB:" pro extrtakci
                            int rgbIndex = line.LastIndexOf("(RGB:");
                            if (rgbIndex != -1)
                            {
                                // values se ukladaji ve forme << Name (RGB: R, G, B) >>
                                // RGB: a zavorky odstranime a oceseme whitespace, pote oddelime RGB hodnoty mezi ","
                                string cleanRgb = line.Substring(rgbIndex).Replace("(RGB:", "").Replace(")", "").Trim();
                                string[] parts = cleanRgb.Split(',');

                                // safety
                                if (parts.Length == 3 &&
                                    // nejak nechapu proc bytes, ale microsoft ma vic questionable veci
                                    // future ja: uz chapu, 1B dokaze ulozit az 256 (0-255) hodnot coz
                                    // coincidentally jsou values RGB kanalu
                                    //                             out vytvori novou promennou s vystupem TryParse
                                    byte.TryParse(parts[0].Trim(), out byte r) &&
                                    byte.TryParse(parts[1].Trim(), out byte g) &&
                                    byte.TryParse(parts[2].Trim(), out byte b))
                                {
                                    ColorList.Add(new ColorItem
                                    {
                                        ColorText = line,
                                        ColorBrush = new SolidColorBrush(Color.FromArgb(255, r, g, b))
                                    });
                                    SavedColorsCount.Text = $"{ColorList.Count.ToString()} saved";
                                    UpdateEmptyState();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to load file: {ex.Message}");
                    }
                }
            }
        }

        // tohle je hlavne pro user experience, prazdny misto je takovy eh
        // kdyz v ColorHistory nic neni, tak je EmptyColorsText visible.
        private void UpdateEmptyState()
        {
            EmptyColorsText.Visibility =
                ColorHistory.Items.Count == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }
    }
}