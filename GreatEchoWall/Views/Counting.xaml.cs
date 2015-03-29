using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GreatEchoWall.Views
{
    /// <summary>
    /// Counting.xaml 的交互逻辑
    /// </summary>
    public partial class Counting : Window
    {
        public Counting()
        {
            InitializeComponent();
            testc();
        }

        private void testc()
        {
            lineChart.DataContext = new 
            {
                TCP = new List<KeyValuePair<int, int>>
                {
                    new KeyValuePair<int, int>(1, 1),
                    new KeyValuePair<int, int>(2, 3),
                    new KeyValuePair<int, int>(3, 2),
                    new KeyValuePair<int, int>(4, 4),
                },
                UDP = new List<KeyValuePair<int, int>>
                {
                    new KeyValuePair<int, int>(1, 3),
                    new KeyValuePair<int, int>(2, 2),
                    new KeyValuePair<int, int>(3, 4),
                    new KeyValuePair<int, int>(4, 1),
                }
            };
        }
    }
}
