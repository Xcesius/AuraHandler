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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Utility;

namespace GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }


        private static bool Flask1Enabled
        {
            get
            {
                var flask1enable = ConfigFile.ReadValue("AuraHandler", "Flask1 Enabled").Trim();
                return flask1enable != "" && Convert.ToBoolean(flask1enable);
            }
            set { ConfigFile.WriteValue("AuraHandler", "Flask1 Enabled", value.ToString()); }
        }

        private void Flask1_Checked(object sender, EventArgs e)
        {
            Flask1Enabled = true;
        }

        private void Flask1_Unchecked(object sender, EventArgs e)
        {
            Flask1Enabled = false;
        }

        private static bool Flask2Enabled
        {
            get
            {
                var flask1enable = ConfigFile.ReadValue("AuraHandler", "Flask1 Enabled").Trim();
                return flask1enable != "" && Convert.ToBoolean(flask1enable);
            }
            set { ConfigFile.WriteValue("AuraHandler", "Flask1 Enabled", value.ToString()); }
        }

        private void Flask2_Checked(object sender, EventArgs e)
        {
            Flask2Enabled = true;
        }

        private void Flask2_Unchecked(object sender, EventArgs e)
        {
            Flask2Enabled = false;
        }

        private static bool Flask3Enabled
        {
            get
            {
                var flask1enable = ConfigFile.ReadValue("AuraHandler", "Flask1 Enabled").Trim();
                return flask1enable != "" && Convert.ToBoolean(flask1enable);
            }
            set { ConfigFile.WriteValue("AuraHandler", "Flask1 Enabled", value.ToString()); }
        }

        private void Flask3_Checked(object sender, EventArgs e)
        {
            Flask3Enabled = true;
        }

        private void Flask3_Unchecked(object sender, EventArgs e)
        {
            Flask3Enabled = false;
        }

        private static bool Flask4Enabled
        {
            get
            {
                var flask1enable = ConfigFile.ReadValue("AuraHandler", "Flask1 Enabled").Trim();
                return flask1enable != "" && Convert.ToBoolean(flask1enable);
            }
            set { ConfigFile.WriteValue("AuraHandler", "Flask1 Enabled", value.ToString()); }
        }

        private void Flask4_Checked(object sender, EventArgs e)
        {
            Flask4Enabled = true;
        }

        private void Flask4_Unchecked(object sender, EventArgs e)
        {
            Flask4Enabled = false;
        }

        private static bool Flask5Enabled
        {
            get
            {
                var flask1enable = ConfigFile.ReadValue("AuraHandler", "Flask1 Enabled").Trim();
                return flask1enable != "" && Convert.ToBoolean(flask1enable);
            }
            set { ConfigFile.WriteValue("AuraHandler", "Flask1 Enabled", value.ToString()); }
        }

        private void Flask5_Checked(object sender, EventArgs e)
        {
            Flask5Enabled = true;
        }

        private void Flask5_Unchecked(object sender, EventArgs e)
        {
            Flask5Enabled = false;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Flask1.IsChecked = Flask1Enabled;
            Flask2.IsChecked = Flask2Enabled;
            Flask3.IsChecked = Flask3Enabled;
            Flask4.IsChecked = Flask4Enabled;
            Flask5.IsChecked = Flask5Enabled;
            Close();
        }
    }
}
