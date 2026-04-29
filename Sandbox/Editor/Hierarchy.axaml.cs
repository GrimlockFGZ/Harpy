using Avalonia.Controls;
using HarpyEngine.Sandbox.Editor.Models;

namespace HarpyEngine.Sandbox.Editor // <-- Make sure this matches your project root/folder structure
{
    public partial class Hierarchy : UserControl // Or the base class your XAML uses
    {
        public HierarchyViewModel ViewModel => (HierarchyViewModel)DataContext!;

        public Hierarchy()
        {
            InitializeComponent();
        }
    }
}