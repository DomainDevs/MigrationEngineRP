using System;

public partial class DesignerForm : Form
{
    public DesignerForm()
    {
        InitializeComponent();

        // Cargar conexiones globales
        GlobalConnections.Load();

        // Llenar dropdowns con alias
        foreach (var alias in GlobalConnections.GetAliases())
        {
            origenDropdown.Items.Add(alias);
            destinoDropdown.Items.Add(alias);
        }
    }
}