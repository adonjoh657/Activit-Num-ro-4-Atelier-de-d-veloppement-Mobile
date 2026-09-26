using System.Globalization;

namespace CalculatorMauiApp;

public partial class MainPage : ContentPage
{
    // Nombre courant affiché (celui que l'utilisateur est en train de saisir)
    private string _currentInput = "0";

    // Premier opérande mémorisé au moment où un opérateur est pressé
    private double _firstOperand = 0;

    // Opérateur en attente ("+", "−", "×", "÷") ou chaîne vide si aucun
    private string _pendingOperator = string.Empty;

    // Indique si le prochain chiffre saisi doit démarrer un nouveau nombre
    // (par ex. juste après avoir choisi un opérateur ou calculé un résultat)
    private bool _isNewEntry = true;

    // Indique qu'une erreur (ex : division par zéro) est actuellement affichée
    private bool _hasError = false;

    private const int MaxDigits = 12;

    public MainPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateDisplay();
    }

    // ---------------------------------------------------------------
    // Saisie des chiffres
    // ---------------------------------------------------------------
    private void OnDigitClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button) return;

        string digit = button.Text;

        // Après une erreur, toute nouvelle saisie repart de zéro
        if (_hasError)
        {
            ResetAll();
        }

        if (_isNewEntry)
        {
            _currentInput = digit;
            _isNewEntry = false;
        }
        else
        {
            // Empêche les zéros non significatifs (ex: "00" -> "0")
            if (_currentInput == "0")
                _currentInput = digit;
            else if (_currentInput.Length < MaxDigits)
                _currentInput += digit;
        }

        UpdateDisplay();
    }

    // ---------------------------------------------------------------
    // Saisie du séparateur décimal
    // ---------------------------------------------------------------
    private void OnDecimalClicked(object? sender, EventArgs e)
    {
        if (_hasError)
            ResetAll();

        if (_isNewEntry)
        {
            _currentInput = "0.";
            _isNewEntry = false;
        }
        else if (!_currentInput.Contains('.'))
        {
            _currentInput += ".";
        }

        UpdateDisplay();
    }

    // ---------------------------------------------------------------
    // Choix d'un opérateur (+, −, ×, ÷)
    // ---------------------------------------------------------------
    private void OnOperatorClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button) return;
        if (_hasError) return;

        // Si un opérateur est déjà en attente et que l'utilisateur a saisi
        // un second nombre, on calcule d'abord le résultat intermédiaire
        // (permet d'enchaîner : 5 + 3 + 2 = 10)
        if (!string.IsNullOrEmpty(_pendingOperator) && !_isNewEntry)
        {
            ComputeResult(showAsResult: false);
        }
        else
        {
            _firstOperand = ParseCurrentInput();
        }

        _pendingOperator = button.Text;
        _isNewEntry = true;

        ExpressionLabel.Text = $"{FormatNumber(_firstOperand)} {_pendingOperator}";
    }

    // ---------------------------------------------------------------
    // Calcul du résultat (=)
    // ---------------------------------------------------------------
    private void OnEqualsClicked(object? sender, EventArgs e)
    {
        if (_hasError) return;
        if (string.IsNullOrEmpty(_pendingOperator)) return;

        ExpressionLabel.Text = $"{FormatNumber(_firstOperand)} {_pendingOperator} {_currentInput} =";
        ComputeResult(showAsResult: true);
        _pendingOperator = string.Empty;
        _isNewEntry = true;
    }

    private void ComputeResult(bool showAsResult)
    {
        double secondOperand = ParseCurrentInput();
        double result;

        switch (_pendingOperator)
        {
            case "+":
                result = _firstOperand + secondOperand;
                break;
            case "−":
                result = _firstOperand - secondOperand;
                break;
            case "×":
                result = _firstOperand * secondOperand;
                break;
            case "÷":
                // ---- Gestion explicite de la division par zéro ----
                if (secondOperand == 0)
                {
                    ShowError("Erreur : division par zéro");
                    return;
                }
                result = _firstOperand / secondOperand;
                break;
            default:
                result = secondOperand;
                break;
        }

        _firstOperand = result;
        _currentInput = FormatNumber(result);

        if (!showAsResult)
        {
            // Résultat intermédiaire : on garde l'opération visible en haut
            ExpressionLabel.Text = $"{FormatNumber(_firstOperand)} {_pendingOperator}";
        }

        UpdateDisplay();
    }

    // ---------------------------------------------------------------
    // Fonctions utilitaires (AC, ⌫, +/-, %)
    // ---------------------------------------------------------------
    private void OnClearClicked(object? sender, EventArgs e)
    {
        ResetAll();
        UpdateDisplay();
    }

    private void OnBackspaceClicked(object? sender, EventArgs e)
    {
        if (_hasError)
        {
            ResetAll();
            UpdateDisplay();
            return;
        }

        if (_isNewEntry) return;

        if (_currentInput.Length <= 1)
            _currentInput = "0";
        else
            _currentInput = _currentInput[..^1];

        if (_currentInput == "0")
            _isNewEntry = true;

        UpdateDisplay();
    }

    private void OnSignClicked(object? sender, EventArgs e)
    {
        if (_hasError) return;

        double value = ParseCurrentInput();
        value = -value;
        _currentInput = FormatNumber(value);
        UpdateDisplay();
    }

    private void OnPercentClicked(object? sender, EventArgs e)
    {
        if (_hasError) return;

        double value = ParseCurrentInput();
        value /= 100.0;
        _currentInput = FormatNumber(value);
        _isNewEntry = true;
        UpdateDisplay();
    }

    // ---------------------------------------------------------------
    // Aides internes
    // ---------------------------------------------------------------
    private double ParseCurrentInput()
    {
        double.TryParse(_currentInput, NumberStyles.Float, CultureInfo.InvariantCulture, out double value);
        return value;
    }

    private static string FormatNumber(double value)
    {
        // Évite les écritures scientifiques et les longues suites de décimales
        if (double.IsNaN(value) || double.IsInfinity(value))
            return "Erreur";

        string text = value.ToString("0.##########", CultureInfo.InvariantCulture);
        return text;
    }

    private void ShowError(string message)
    {
        _hasError = true;
        _currentInput = "0";
        _pendingOperator = string.Empty;
        _isNewEntry = true;
        ExpressionLabel.Text = message;
        ResultLabel.Text = "Erreur";
    }

    private void ResetAll()
    {
        _currentInput = "0";
        _firstOperand = 0;
        _pendingOperator = string.Empty;
        _isNewEntry = true;
        _hasError = false;
        ExpressionLabel.Text = " ";
    }

    private void UpdateDisplay()
    {
        if (_hasError) return;
        ResultLabel.Text = _currentInput;
    }
}
