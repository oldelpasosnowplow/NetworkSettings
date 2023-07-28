using System;
using System.Management;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace Network_Settings
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LanSetting_Load();
        }

        private Dictionary<string, bool> rules = null;
        public Dictionary<string, bool> Rules
        {
            get
            {
                if (rules == null)
                {
                    // 1. use [0-9]{1,3} instead of x to represent any 1-3 digit numeric value
                    // 2. escape dots like such \. 
                    rules = new Dictionary<string, bool>();
                    rules.Add(@"192\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}", true);
                    rules.Add(@"172S\.18\.[0-9]{1,3}\.[0,9]{1,3}", false);
                }
                return rules;
            }
        }



        private void LanSetting_Load()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if ((nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet) || (nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)) //&& (nic.OperationalStatus == OperationalStatus.Up))
                {
                    cbAdapter.Items.Add(nic.Description);
                }
            }
        }

        public void setIP(string ip_address, string subnet_mask, string gateway)
        {
            ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection objMOC = objMC.GetInstances();

            foreach (ManagementObject objMO in objMOC)
            {
                if((bool)objMO["Caption"].ToString().Contains(cbAdapter.SelectedItem.ToString()))
                  {
                    try
                     {
                        if (cbNo.IsChecked == true)
                        {
                            try
                            {
                                ManagementBaseObject setIP;
                                ManagementBaseObject newIP =
                                    objMO.GetMethodParameters("EnableStatic");
                                ManagementBaseObject newGateway =
                                    objMO.GetMethodParameters("SetGateways");

                                newIP["IPAddress"] = new string[] { ip_address };
                                newIP["SubnetMask"] = new string[] { subnet_mask };
                                newGateway["DefaultIPGateway"] = new string[] { gateway };
                                newGateway["GatewayCostMetric"] = new int[] { 1 };

                                setIP = objMO.InvokeMethod("EnableStatic", newIP, null);
                                setIP = objMO.InvokeMethod("SetGateways", newGateway, null);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                            }

                        }
                        else if (cbYes.IsChecked == true)
                        {
                            ManagementBaseObject setIP = objMO.InvokeMethod("EnableDHCP", null, null);
                        }
                        else
                        {
                            MessageBox.Show("No changes were made", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                        MessageBox.Show("IP Changed Successfully", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                     }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    }
                }
            }
        }



        private void btnChange_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cbNo.IsChecked == true)
                {
                    IPAddress address = IPAddress.Parse(tbIPAddress.Text);
                    IPAddress subnetmask = IPAddress.Parse(tbSubnetMask.Text);
                    IPAddress gateway = IPAddress.Parse(tbGateway.Text);
                }

                bool isAuthorizedIP = false;
                foreach (var rule in Rules)
                {
                    if (Regex.IsMatch(tbIPAddress.Text, rule.Key))
                        isAuthorizedIP = rule.Value;
                }

                if (isAuthorizedIP == true || cbYes.IsChecked == true)
                {
                    setIP(tbIPAddress.Text, tbSubnetMask.Text, tbGateway.Text);
                }
                else
                {
                    MessageBox.Show("You cannot set your IP to a 172.x.x.x address please adjust or set to DHCP", "Error", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    tbIPAddress.Focus();
                }
            }

            catch (ArgumentNullException ex)
            {
                MessageBox.Show("Source: " + ex.Source + " Message: " + ex.Message  , "ArgumentNullException caught", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            catch (FormatException ex)
            {
                MessageBox.Show("Source: " + ex.Source + " Message: " + ex.Message, "FormatException caught", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            catch (Exception ex)
            {
                MessageBox.Show("Source: " + ex.Source + " Message: " + ex.Message, "Exception caught", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
            return;
        }

        private void cbAdapter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (UnicastIPAddressInformation ip in nic.GetIPProperties().UnicastAddresses)
                {
                    if (nic.Description == cbAdapter.SelectedItem.ToString())
                    {
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            if (nic.GetIPProperties().GetIPv4Properties().IsDhcpEnabled == true)
                            {
                                cbYes.IsChecked = true;
                                cbYes_Click(sender, e);
                                cbNo.IsChecked = false;
                            }
                            else
                            {
                                cbNo.IsChecked = true;
                                cbNo_Click(sender, e);
                                cbYes.IsChecked = false;
                            }
                            tbIPAddress.Text = ip.Address.ToString();
                            tbSubnetMask.Text = ip.IPv4Mask.ToString();
                            GatewayIPAddressInformation gatewayaddress = nic.GetIPProperties().GatewayAddresses.FirstOrDefault();
                            if (gatewayaddress == null)
                            {
                                tbGateway.Text = "";
                            }
                            else
                            { 
                            tbGateway.Text = gatewayaddress.Address.ToString();
                            }
                        }
                    }
                }
            }
        }

        private void cbYes_Click(object sender, RoutedEventArgs e)
        {
            if (cbYes.IsChecked == true)
            {
                tbIPAddress.IsEnabled = false;
                tbSubnetMask.IsEnabled = false;
                tbGateway.IsEnabled = false;
                cbNo.IsChecked = false;
            }
        }

        private void cbNo_Click(object sender, RoutedEventArgs e)
        {
            if (cbNo.IsChecked == true)
            {
                tbIPAddress.IsEnabled = true;
                tbSubnetMask.IsEnabled = true;
                tbGateway.IsEnabled = true;
                cbYes.IsChecked = false;
            }
        }

        private void tbIPAddress_GotFocus(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)delegate
            {
                tbIPAddress.SelectAll();
            });
        }

        private void tbSubnetMask_GotFocus(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)delegate
            {
                tbSubnetMask.SelectAll();
            });
        }

        private void tbGateway_GotFocus(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)delegate
            {
                tbGateway.SelectAll();
            });
        }
    }
}
