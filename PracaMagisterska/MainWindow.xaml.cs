using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Design;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

namespace PracaMagisterska;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private DataLoader? dataLoader;
    Dictionary<string, bool> fieldValidDict = new();

    public MainWindow()
    {
        InitializeComponent();
        AddValidators();
        windowsFormsHost.Child = new Microsoft.Msagl.GraphViewerGdi.GViewer();
    }

    private void ShowLoadedGraph(PracaMagisterska.Graph graph)
    {
        Visualiser vs = new(graph);
        vs.Visualise();
        windowsFormsHost.Child = vs.viewer;
    }

    public void AddValidators()
    {
        List<TextBox> doubleTextboxes = new() {
            BetaTextBox, GammaTextBox, DeltaTextBox, CwTextBox, KwTextBox
        };

        List<TextBox> intTextBoxes = new()
        {
            CostLimitTextBox, TimeLimitTextBox, AlphaTextBox, IterationStrategyTextBox
        };

        foreach (var tb in doubleTextboxes)
        {
            tb.TextChanged += CheckValidDouble;
            fieldValidDict.Add(tb.Name, true);
        }

        foreach (var tb in intTextBoxes)
        {
            tb.TextChanged += CheckValidInt;
            fieldValidDict.Add(tb.Name, true);
        }

        PenaltyStrengthTextBox.TextChanged += CheckValidDoubleMaxLimit;
        fieldValidDict.Add(PenaltyStrengthTextBox.Name, true);
    }

    public void CheckValidInt(object sender, TextChangedEventArgs e)
    {
        var tb = sender as TextBox;
        if (tb == null) return;

        try
        {
            int x = int.Parse(tb.Text);
            if (x <= 0)
                throw new Exception();

            fieldValidDict[tb.Name] = true;
            tb.Foreground = new SolidColorBrush(Colors.Black);
        }
        catch
        {
            fieldValidDict[tb.Name] = false;
            tb.Foreground = new SolidColorBrush(Colors.Red);
        }

        StartEnabledValidator();
    }

    public void CheckValidDouble(object sender, TextChangedEventArgs e)
    {
        var tb = sender as TextBox;
        if (tb == null) return;

        try
        {
            double x = double.Parse(tb!.Text);
            if (x < 0 || x > 1)
                throw new Exception();

            fieldValidDict[tb.Name] = true;
            tb.Foreground = new SolidColorBrush(Colors.Black);
        }
        catch
        {
            fieldValidDict[tb.Name] = false;
            tb!.Foreground = new SolidColorBrush(Colors.Red);
        }

        StartEnabledValidator();
    }

    public void CheckValidDoubleMaxLimit(object sender, TextChangedEventArgs e)
    {
        var tb = sender as TextBox;
        if (tb == null) return;

        try
        {
            double x = double.Parse(tb!.Text);
            if (x < 0)
                throw new Exception();

            fieldValidDict[tb.Name] = true;
            tb.Foreground = new SolidColorBrush(Colors.Black);
        }
        catch
        {
            fieldValidDict[tb.Name] = false;
            tb!.Foreground = new SolidColorBrush(Colors.Red);
        }

        StartEnabledValidator();
    }

    private void TimeLimitCheckbox_Checked(object sender, RoutedEventArgs e)
    {
        TimeLimitLabel.IsEnabled = true;
        TimeLimitTextBox.IsEnabled = true;
    }

    private void TimeLimitCheckbox_Unchecked(object sender, RoutedEventArgs e)
    {
        TimeLimitLabel.IsEnabled = false;
        TimeLimitTextBox.IsEnabled = false;
    }

    private void CostLimitCheckbox_Unchecked(object sender, RoutedEventArgs e)
    {
        CostLimitLabel.IsEnabled = false;
        CostLimitTextBox.IsEnabled = false;
    }

    private void CostLimitCheckbox_Checked(object sender, RoutedEventArgs e)
    {
        CostLimitLabel.IsEnabled = true;
        CostLimitTextBox.IsEnabled = true;
    }


    private void LoadFileButton_Click(object sender, RoutedEventArgs e)
    {
        StartProcessingButton.IsEnabled = false;
        dataLoader = new();

        dataLoader.LoadFromFile();
        if (dataLoader.isDataValid)
        {
            StartProcessingButton.IsEnabled = true;
            ShowLoadedGraph(dataLoader.Graph!);
        }


    }

    private bool IsBlack(TextBox tb)
    {
        SolidColorBrush? scb = (tb.Foreground as SolidColorBrush);
        if (scb == null) return false;

        return scb.Color == Colors.Black;
    }

    private void StartEnabledValidator()
    {
        foreach (var kv in fieldValidDict)
        {
            if (kv.Value == false)
            {
                StartProcessingButton.IsEnabled = false;
                return;
            }
        }

        if (dataLoader == null)
        {
            StartProcessingButton.IsEnabled = false;
            return;
        }

        if (!dataLoader.isDataValid)
            StartProcessingButton.IsEnabled = false;

        StartProcessingButton.IsEnabled = true;
    }

    private bool FieldsValid()
    {
        SolidColorBrush scb = new SolidColorBrush(Colors.Black);
        if (IsBlack(AlphaTextBox) &&
            IsBlack(BetaTextBox) &&
            IsBlack(GammaTextBox) &&
            IsBlack(DeltaTextBox) &&
            IsBlack(KwTextBox) &&
            IsBlack(CwTextBox) &&
            IsBlack(PenaltyStrengthTextBox))

        {
            if (CostLimitCheckbox.IsChecked == true)
                if (!IsBlack(CostLimitTextBox))
                    return false;

            if (TimeLimitCheckbox.IsChecked == true)
                if (!IsBlack(TimeLimitTextBox))
                    return false;

            if (CostLimitCheckbox.IsChecked == false && TimeLimitCheckbox.IsChecked == false)
                return false;

            if (!IsBlack(IterationStrategyTextBox))
                return false;

            return true;
        }

        return false;
    }

    // orginał
    private void StartProcessingButton_Click(object sender, RoutedEventArgs e)
    {
        if (dataLoader == null) return;

        if (dataLoader.isDataValid && FieldsValid())
        {
            ResourcesManager.SetResources(dataLoader.Resources!);
            ChannelsManager.SetChannels(dataLoader.Channels!);
            Graph minTree = dataLoader.Graph!.GetMinimalSpanningTree();
            ShowLoadedGraph(minTree);
            GeneticAlgorithm ga = new(minTree);
            ga.LoadParameters(
                int.Parse(AlphaTextBox.Text),
                double.Parse(BetaTextBox.Text),
                double.Parse(GammaTextBox.Text),
                double.Parse(DeltaTextBox.Text),
                double.Parse(CwTextBox.Text),
                double.Parse(KwTextBox.Text),
                double.Parse(PenaltyStrengthTextBox.Text)
            );

            if (CostLimitCheckbox.IsChecked == true)
                ga.SetCostRestrictions(int.Parse(CostLimitTextBox.Text));

            if (TimeLimitCheckbox.IsChecked == true)
                ga.SetTimeRestrictions(int.Parse(TimeLimitTextBox.Text));

            int iterations = int.Parse(IterationStrategyTextBox.Text);
            if (((ComboBoxItem)IterationStrategyComboBox.SelectedItem).Content.ToString() == "Maksymalna liczba iteracji")
            {
                ga.SetMaxIterations(iterations);
            }
            else
            {
                ga.SetMaxNoImprovementIterations(iterations);
            }

            Graph g = ga.StartProcessing();
            ResultSaver rs = new();
            string? strategyName = ((ComboBoxItem)IterationStrategyComboBox.SelectedItem).Content.ToString();
            if (strategyName == null) return;

            rs.SaveResult2(
                g,
                int.Parse(AlphaTextBox.Text),
                double.Parse(BetaTextBox.Text),
                double.Parse(GammaTextBox.Text),
                double.Parse(DeltaTextBox.Text),
                double.Parse(CwTextBox.Text),
                double.Parse(KwTextBox.Text),
                double.Parse(PenaltyStrengthTextBox.Text),
                strategyName,
                ga.GetTimesIterated());
        }
        else
        {
            MessageBox.Show("Wykryto nieprawidłowe wartości parametrów. Popraw je, aby uruchomić algorytm.");
        }
    }
}