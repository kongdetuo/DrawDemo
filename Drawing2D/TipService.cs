using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Drawing2D
{
    class TipService
    {
        public TipService(Control control)
        {
            Popup.Child = ContentControl;
            this.Host = control;
            control.MouseMove += Control_MouseMove;
        }

        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            this.Popup.IsOpen = false;
        }

        Popup Popup = new Popup()
        {
            StaysOpen = true,
            Placement = PlacementMode.Mouse
        };

        ContentControl ContentControl = new ContentControl()
        {
            Margin = new Thickness(0, 0, 5, 5),
            Effect = new System.Windows.Media.Effects.DropShadowEffect()
            {
                BlurRadius = 3,
                ShadowDepth = 3
            }
        };
        private Control Host;

        public async void ShowTip(string tip)
        {
            if (string.IsNullOrEmpty(tip))
                return;
            var p1 = Mouse.GetPosition(this.Host);
            await Task.Delay(500);
            var p2 = Mouse.GetPosition(this.Host);
            if ((p1 - p2).LengthSquared < 1)
            {
                this.Popup.IsOpen = false;
            }
        }
    }

}
