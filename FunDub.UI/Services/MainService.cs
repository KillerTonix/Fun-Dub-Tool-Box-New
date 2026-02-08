using FunDub.UI.ViewModels;
using Microsoft.Win32;
using System.Collections.ObjectModel;

namespace FunDub.UI.Services
{
    public class MainService
    {
        public ObservableCollection<MaterialItem> _materials = MainViewModel.Materials;

        public void OpenFileSelector(string category)
        {
            var dialog = new OpenFileDialog { Multiselect = false };
            switch (category)
            {
                case "Intro":
                case "Video":
                case "Outro":
                    dialog.Filter = "Video Files (*.mp4;*.avi;*.mkv;*.mpeg;*.mpg;*.mov;*.webm;*.ts;*.m4v)| *.mp4; *.avi; *.mkv; *.mpeg; *.mpg; *.mov; *.webm; *.ts; *.m4v";
                    dialog.Title = "Select Video File";
                    break;
                case "Logo":
                    dialog.Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp;*.webp) | *.png; *.jpg; *.jpeg; *.bmp; *.webp";
                    dialog.Title = "Select Logo Image File";
                    break;
                case "Subtitles":
                    dialog.Filter = "Subtitle Files (*.srt; *.ass)| *.srt; *.ass";
                    dialog.Title = "Select Subtitles File";
                    break;
                case "Audio":
                    dialog.Filter = "Audio Files (*.mp3;*.ogg;*.wav;*.FLAC;*.aac;*.wma;*.m4a) | *.mp3; *.ogg; *.wav; *.FLAC; *.aac; *.wma; *.m4a";
                    dialog.Title = "Select Audio Files";
                    break;
                default:
                    dialog.Filter = "All Files|*.*";
                    dialog.Title = "Select File";
                    break;
            }
            if (dialog.ShowDialog() == true)
            {
                _materials.Add(new MaterialItem { Type = category, Path = dialog.FileName });
            }
        }

        public void RemoveFileSelector(string category)
        {
            var itemToRemove = _materials.FirstOrDefault(m => m.Type == category);
            if (itemToRemove != null) _materials.Remove(itemToRemove);

        }

    }
}
