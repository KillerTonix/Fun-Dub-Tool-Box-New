using System.Windows.Controls;

namespace FunDub.UI.Views.Controls
{
    /// <summary>
    /// Interaction logic for StatBlock.xaml
    /// </summary>
    public partial class StatBlock : UserControl
    {
        public StatBlock()
        {
            InitializeComponent();
        }

        // Property to set the top label
        public string Label
        {
            get => LabelDisplay.Text;
            set => LabelDisplay.Text = value.ToUpper();
        }

        // Property to set the value (accessible from your FFmpeg logic)
        public string Value
        {
            get => ValueDisplay.Text;
            set => ValueDisplay.Text = value;
        }
    }
}
