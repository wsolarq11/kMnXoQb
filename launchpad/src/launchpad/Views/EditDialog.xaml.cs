using Launchpad.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Launchpad.Views;

public sealed partial class EditDialog : ContentDialog
{
    public EditViewModel ViewModel { get; }

    public EditDialog(EditViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Title = viewModel.IsNew ? "New Item" : "Edit Item";
        PrimaryButtonText = "Save";
        CloseButtonText = "Cancel";
        SecondaryButtonText = viewModel.IsNew ? null : "Delete";
        DefaultButton = ContentDialogButton.Primary;
        PrimaryButtonClick += OnPrimaryClick;
    }

    /// <summary>Validate before closing; canceling keeps the dialog open with errors shown.</summary>
    private void OnPrimaryClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (!ViewModel.Validate())
        {
            args.Cancel = true;
        }
    }

    private void OnBrowse(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => ViewModel.PickDirectoryCommand.Execute(null);
}
