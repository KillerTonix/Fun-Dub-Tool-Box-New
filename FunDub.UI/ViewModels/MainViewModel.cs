using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;

namespace FunDub.UI.ViewModels
{
    public partial class MaterialItem : ObservableObject
    {
        public string Type { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
    }

    public partial class MainViewModel : ObservableObject
    {
        public static ObservableCollection<MaterialItem> Materials { get; } = [];
   

        // Properties for the UI
        [ObservableProperty]
        private string _transparencyValue = "100%";

        [ObservableProperty]
        private bool _isExportToSourceFolder;

        // Collections for the DataGrids
        public ObservableCollection<object> ProcessingQueue { get; } = [];

        // Commands for Buttons
        [RelayCommand]
        private void AddToSequence()
        {
            // Logic to move a material to the sequential processing list
        }

        [RelayCommand]
        private async Task StartProcessing()
        {
            // Trigger your FFmpeg logic here
        }

        [RelayCommand]
        private void SetManualLogo()
        {
            // Open the logo positioning window
        }

        [RelayCommand]
        private void SelectMaterial(string category)
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                // Add logic to save this path as an 'Intro', 'Logo', etc.
                // Update the 'Materials' collection to show it in the list
            }
        }

        [ObservableProperty]
        private bool _isIntroSaved;

        [ObservableProperty]
        private bool _isVideoSaved;

        // Command to handle the actual saving logic
        [RelayCommand]
        private void SaveState(string category)
        {
            // Add logic to save presets to a JSON file here
            // The boolean properties will be updated automatically by the ToggleButton binding
        }

        [RelayCommand]
        private void RemoveMaterial(MaterialItem item)
        {
            if (item != null)
            {
                Materials.Remove(item);
              

            }
        }


       
    }
}